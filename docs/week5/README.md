# Week 5 — Messaging, queues and events

**Use case:** an author publishes a post; hundreds or thousands of followers receive a reference to it in their personal feeds. Publishing must not wait for every feed write.

## Run the complete demo

Requirements: Docker Desktop with Linux containers, Node.js 22+, and available local ports 5155, 5545, 5675 and 15675. The demo uses its own Compose project and persistent volumes; it does not use the existing API `.env` database.

From the repository root:

```powershell
docker compose -f compose.week5.yml up -d --build
docker compose -f compose.week5.yml logs initialize
```

The one-shot initializer applies EF migrations and declares all exchanges, queues and bindings **before** the API starts. This is essential: publishing an event before a subscription exists can otherwise lose that subscriber's copy. Initializer success is a deployment prerequisite, not a check performed on every request.

- API and Swagger: <http://localhost:5155/swagger>
- RabbitMQ management: <http://localhost:15675> — `pulse` / `local-pulse-demo`
- PostgreSQL: localhost:5545, database/user `pulse`, password `local-pulse-demo`
- The demo binds published ports to loopback. Credentials are intentionally local demo values.

To run Angular against this stack, set `NG_APP_API_BASE_URL=http://localhost:5155/api` in your frontend environment, then run `npm start` from `frontend`. The normal app's environment file is not overwritten by this demo.

1. Open **Create Post** and copy the test author ID.
2. In another tab, open **Your feed**, paste the author ID and click **Follow author**.
3. Publish a post in the first tab.
4. Refresh the follower's feed. The post appears after asynchronous delivery.
5. Stop delivery workers, publish again, and show that the API still responds. Restart workers and refresh the feed.

```powershell
docker compose -f compose.week5.yml stop worker
docker compose -f compose.week5.yml start worker
```

The test author stays the same across posts. Test IDs are manually selectable because authentication has not yet been introduced in this course repository. Production authorization must derive the acting user from their identity rather than trust a supplied UUID.

## Follow the code

```text
HTTP POST /api/posts
  CreatePostHandler -> Post.Create -> PostCreated (domain event)
  PostRepository -> PostCreatedV1 (integration event)
  SaveChanges: post + bus outbox, one database transaction
  outbox delivery service -> RabbitMQ PostCreatedV1 exchange
  itfpulse-post-created-v1 -> PostCreatedConsumer
  itfpulse-plan-fanout-v1 -> PlanPostFanoutHandler
    read <=500 followers using an indexed cursor
    atomically enqueue delivery batch + next planner page
  itfpulse-deliver-feed-v1 -> DeliverPostToFollowersHandler
    insert <=500 feed references in one SQL statement
HTTP GET /api/feeds/{followerId}
  indexed feed references joined to the original posts
```

| Responsibility | Location | Why it belongs here |
|---|---|---|
| Post rules and `PostCreated` fact | Domain/Posts | Business behavior without broker or persistence dependencies |
| Follow invariants | Domain/Followers | Reject empty identities and following yourself |
| Plan/deliver use cases and ports | Application/Feeds | Express bounded work without choosing RabbitMQ or SQL |
| Versioned messages | Contracts/Messaging | Explicit process boundary; never serialize a tracked aggregate |
| RabbitMQ consumers, retries and outboxes | Infrastructure/Messaging | Transport is an adapter around application use cases |
| Set-based writes and indexed reads | Infrastructure/Persistence | Database-specific performance decisions stay outside the domain |
| HTTP and process composition | Api and Worker | Independently deployable entry points, sharing the inner layers |

This is an evolving modular application with separate worker processes, **not** independently owned microservices: the processes still share a database and deployment-compatible contracts.

## Why these decisions?

**A queue for fan-out, not for the core post validation.** Creating the post is a short, strongly consistent operation. Distributing it is much larger, delay-tolerant work. A `201` response means the post and its durable publication intent committed; it does not promise that every feed is already updated.

**Domain event versus integration event.** `PostCreated` is a fact recorded by the aggregate. `PostCreatedV1` is a small, versioned message published outside the request. The repository adapter explicitly translates the event before saving. It throws for unknown event types instead of silently dropping them. `ClearDomainEvents` runs only after a successful save. For larger aggregates/use cases, move the same mapping/commit boundary to a dedicated unit of work.

**Transactional outbox instead of a database write followed by a broker publish.** Those two independent writes cannot be made atomic by placing them next to each other. The EF bus outbox persists the post and the message in one transaction, then retries transport delivery separately. We use the scoped `IPublishEndpoint`, never a direct `IBus.Publish` in the request.

**Two kinds of messages.** Publish `PostCreatedV1`, a past-tense fact that may have multiple subscriptions. Send `PlanPostFanoutV1` and `DeliverPostToFollowersV1`, commands owned by one queue. Four delivery workers share one queue and divide work; four different subscriber queues would each receive their own copy.

**Two worker stages.** The planner scans the follower relationship in pages and emits compact batches. Delivery workers process batches independently, including different batches of the same post. One message per follower would add unnecessary serialization, broker and inbox overhead; one message containing all followers would create unbounded payloads and expensive retries. The fixed 500-person batch is a tunable starting point, not a universal optimum.

**Atomic continuation.** Every planner page sends its delivery batch and, when needed, its next-page command through the EF consumer outbox. The transaction and inbox prevent an acknowledged page from losing its continuation. An empty final page for an exact multiple of 500 is harmless.

**Idempotent effects.** RabbitMQ delivery and crash recovery can repeat a message. The consumer inbox suppresses repeated transport message IDs within its retention window. Independently, the `(follower_id, post_id)` database key and `ON CONFLICT DO NOTHING` protect the business effect permanently while that feed reference exists, including duplicates with a new message ID. This is not an end-to-end exactly-once delivery claim.

**Efficient storage.** Each batch uses parameterized `INSERT ... SELECT FROM unnest(...)`; it does not load and track 500 EF entities. Feed rows contain post references, not copies of the 2,000-character body. Author/cursor and follower/cursor indexes support bounded reads. Database connections are pooled. The post table remains the source of truth.

**Bounded concurrency.** Default delivery concurrency is 8 and prefetch is 16 per process. These are explicit bounds, not a `Task.WhenAll` over every follower. More workers increase potential concurrency and database connections; they do not create unlimited I/O capacity. Tune with measurements. Sustained incoming work above delivery capacity grows the backlog and eventually consumes disk; queues buy time rather than remove capacity limits.

**Retries and error queues.** Three short retry intervals (100 ms, 500 ms, 1 s) handle brief failures. Invalid commands are not retried. After retries are exhausted, MassTransit moves failed messages to the queue's `_error` queue with exception metadata. Investigate and fix the cause before a controlled replay. Long outages should use delayed redelivery/circuit breaking and operational intervention; do not keep increasing immediate retries.

**RabbitMQ and MassTransit.** RabbitMQ fits competing work consumers and visible classroom operations. MassTransit supplies transport confirmations, lifecycle, retry/error handling and transactional outbox/inbox plumbing; implementing those correctly by hand would obscure the lesson. This example pins MassTransit 8.5.7 rather than silently moving to a different major version. Review support and licensing before a production adoption or upgrade. Kafka would be an alternative when retained event logs, replay and partitioned stream processing are primary requirements; it is not needed to demonstrate this work queue.

**Durability is not the same as high availability.** Quorum queues and persistent Docker volumes are configured. The local demo has only one broker node and one database. It demonstrates process restart recovery, not survival of disk loss or broker-node failure. A production deployment needs multiple broker nodes, database HA/backups, observability and tested recovery procedures.

## Delivery semantics and scope

- Feed delivery is eventually consistent. Different posts/batches may complete out of order.
- The UI intentionally orders by **delivery sequence**, not original post creation time. `createdAt` still shows the post's original time. A cursor gives efficient paging; concurrent commits can require a refresh to see newly arriving items.
- The planner includes relationships visible when each page is read whose `FollowedAt <= CreatedAt`. New follows do not backfill old posts. This is **not** a globally atomic historical snapshot: an in-flight follow transaction or clock skew across hosts can affect inclusion. The load fixture completes all follows before posting. Strict publish-time membership would need a versioned relationship/snapshot policy or stronger coordination.
- Unfollow, account deletion, moderation, privacy changes and feed retention are separate use cases. They need delivery-time authorization/retraction rules before this becomes a production social network.
- Consumer inbox cleanup is configured for one hour. Outbox rows are removed by delivery after success. Feed data has no automatic retention yet; introduce retention/partitioning as volume grows.
- HTTP retries are different from message retries. Retrying a `POST` after an ambiguous client timeout can create another post; a client idempotency key is a separate feature.
- At celebrity scale, writing to millions of feeds per post can cost too much. A hybrid feed can precompute ordinary authors and merge high-fan-out authors on read. That is an extension, not implemented here.

## Reproducible volume demonstration

See [the measured results and their limitations](BENCHMARK.md): both one-million-entry runs completed, with 30.65 s drain time for one worker and 23.54 s for four workers.

```powershell
node scripts/week5/load.mjs --followers=10000 --posts=100 --workers=1
node scripts/week5/load.mjs --followers=10000 --posts=100 --workers=4
```

Each run creates a new author through the real follow/post API, stops delivery workers, accepts 100 posts, waits for 2,000 queued delivery batches, then starts the selected worker count and verifies **1,000,000 unique feed references** for that author. The script also checks a sample feed and waits for queues to drain without error/skipped messages. Results are written to `docs/week5/results/`.

Measurements include HTTP p50/p95 latency, accepted posts/second, total drain time and delivered references/second. Drain timing includes worker startup and up to 500 ms of progress polling. This is a backlog-drain experiment; HTTP latency is measured with delivery stopped. It is not a measured speedup over a synchronous implementation or a sustained-load capacity test.

Repeat runs with one and four workers in alternating order. Use equal Docker CPU/memory limits, record storage type, account for database/cache growth, and compare medians and variation. More workers can be slower when the shared database or broker is the bottleneck. Never hide that result: it is part of the engineering lesson.

To vary per-worker concurrency in PowerShell:

```powershell
$env:FEED_CONCURRENCY = '4'
$env:FEED_PREFETCH = '8'
node scripts/week5/load.mjs --followers=10000 --posts=100 --workers=4
```

Observe `messages_ready`, `messages_unacknowledged`, consumer count and delivery/ack rates in RabbitMQ, alongside `docker stats`. Ready messages show queued work; unacknowledged messages are in flight. Both matter. The development-only `/api/demo/fanout/{authorId}` endpoint counts persisted results. Counting grows more expensive with data volume and is for this lesson, not a production monitoring design.

## Verify correctness and failure recovery

```powershell
dotnet build backend/ITFPulse.slnx -m:1
dotnet run --no-build --project backend/tests/ITFPulse.Domain.Tests
dotnet run --no-build --project backend/tests/ITFPulse.Application.Tests
dotnet test backend/tests/ITFPulse.Architecture.Tests
node scripts/week5/verify.mjs
```

The existing repository mixes xUnit v3 executable runners and xUnit v2/VSTest. Use the commands above; a single `dotnet test` over the solution is not compatible with that mixed setup on the current .NET 10 SDK.

The integration script verifies three-page fan-out (1,001 followers), duplicate follow requests, database idempotency with new transport IDs, read/pagination, broker outage plus API restart, delivery-worker termination/restart, and poison-message isolation. It stops/restarts only this Compose project's services. It consumes its one deliberate poison message after checking it. Run tests and load demonstrations sequentially.

Stop the demo without deleting its data:

```powershell
docker compose -f compose.week5.yml down
```

## Sources for the lesson

- [MassTransit transactional outbox](https://masstransit.io/documentation/patterns/transactional-outbox)
- [MassTransit outbox configuration](https://masstransit.io/documentation/configuration/middleware/outbox)
- [MassTransit RabbitMQ configuration](https://masstransit.io/documentation/configuration/transports/rabbitmq)
- [MassTransit exception/retry handling](https://masstransit.io/documentation/concepts/exceptions)
- [RabbitMQ acknowledgements and publisher confirms](https://www.rabbitmq.com/docs/confirms)
- [RabbitMQ quorum queues](https://www.rabbitmq.com/docs/quorum-queues)
- [PostgreSQL INSERT / ON CONFLICT](https://www.postgresql.org/docs/17/sql-insert.html)
- [MassTransit 8.5.7 package](https://www.nuget.org/packages/MassTransit.EntityFrameworkCore/8.5.7)

Current online MassTransit documentation also describes newer major versions. The compiled implementation and integration tests are the reference for the pinned version used in this repository.
