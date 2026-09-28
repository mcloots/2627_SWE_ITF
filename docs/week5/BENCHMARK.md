# Measured fan-out results — 28 September 2026

Both runs used the actual HTTP API, PostgreSQL, RabbitMQ and containerized workers. Each run seeded a new author with 10,000 followers, published 100 posts and verified 1,000,000 persisted feed references. Delivery was stopped during publication, then started after all 2,000 delivery batches were queued.

| Measurement | 1 delivery worker | 4 delivery workers |
|---|---:|---:|
| Verified feed references | 1,000,000 | 1,000,000 |
| Delivery batches | 2,000 | 2,000 |
| Drain time, including worker startup | 30.65 s | 23.54 s |
| Feed references / second | 32,629 | 42,476 |
| HTTP acceptance p50 | 20.67 ms | 19.11 ms |
| HTTP acceptance p95 | 31.48 ms | 28.48 ms |
| HTTP publish concurrency | 16 | 16 |
| Consumer concurrency per worker | 8 | 8 |
| Prefetch per worker | 16 | 16 |

Observed drain throughput ratio: **42,476 / 32,629 = 1.30×**. Four replicas improved delivery throughput in these runs; they did not produce a fourfold improvement. The measurements alone do not identify whether storage, broker processing, database contention or startup dominated the limit. That requires profiling.

HTTP figures describe acceptance with delivery workers stopped, not HTTP latency under simultaneous fan-out. The two columns' HTTP differences are not a consequence of delivery-worker count because those workers were not running during acceptance.

## Environment and limitations

- Windows host: AMD Ryzen 7 PRO 250, 16 logical CPUs, 31.28 GiB RAM.
- Docker Desktop Linux engine: 16 CPUs, 16,384,679,936 bytes of available memory (about 15.26 GiB); no per-service CPU or memory limit was set.
- One PostgreSQL 17 container and one RabbitMQ 4.1 management container, backed by persistent Docker volumes.
- Release builds on .NET 10; MassTransit 8.5.7; a separate planner process.
- One recorded run per configuration, one-worker first. The second run used the same database with the first run's data still present. Cache state, data growth and other host activity were not controlled.
- Drain timing starts before launching worker containers and ends when the author-scoped database count reaches one million. It includes startup and up to 500 ms of polling delay, plus query execution time.
- No synchronous baseline, sustained arrival-rate test, cold-disk test, repeated statistical analysis or multi-node availability test was performed.
- These results demonstrate real backlog processing at this volume on this machine. They are not a production capacity or latency guarantee.

## Raw evidence

- [One worker](results/load-1workers-1790622927987.json)
- [Four workers](results/load-4workers-1790623187886.json)

Reproduce using the commands in the [Week 5 guide](README.md). Repeat in alternating order under fixed resource limits before drawing stronger scaling conclusions.

## Verification completed

- Full .NET solution build: no warnings or errors.
- Domain tests: 6 passed.
- Application tests: 8 passed.
- Architecture tests: 4 passed.
- Angular production build: passed.
- Angular tests: 5 passed. The existing custom environment builder emits an Angular test-builder compatibility warning, but the tests complete successfully.
- Integration: 1,001-follower multi-page distribution, duplicate follow, invalid input, feed reading/pagination, delivery replay with fresh message IDs, broker outage plus API restart, delivery-worker termination/restart and poison-message isolation all passed.
- Both million-entry runs completed without queued error/skipped messages.

No interactive browser session was available, so the UI was checked by build and component/HTTP tests rather than an interactive browser run.
