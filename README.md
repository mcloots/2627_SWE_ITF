# ITF Pulse  
**The heartbeat of the IT Factory community.**

Welcome to **ITF Pulse**, the official Software Engineering project platform used throughout this course.

This repository contains the **starter structure** for a modern full-stack social media platform where students will design, build, and evolve a scalable application using professional engineering practices.

This project is intentionally structured using:

- Domain Driven Design (DDD)
- Clean Architecture
- Reactive Angular (Signals-based)
- GitFlow branching strategy
- CI/CD-ready repository structure
- Data-intensive design principles

You are expected to **understand the structure**, not just use it.

---

# Project Overview

## Run locally

Run the backend stack in Docker and Angular on your machine. RabbitMQ **and PostgreSQL**
are included in `compose.week5.yml`, together with the API and background workers.

Requirements:

- Docker Desktop running with Linux containers and Docker Compose.
- Node.js 22.12+ in the Node 22 line (or a compatible newer Angular 21 runtime), with npm.
- Free local ports: `5155`, `5545`, `5675`, `15675` and `6510`.
- The .NET 10 SDK is only needed if you want to build or test the backend outside Docker.

### 1. Start the backend, database and message broker

From the repository root:

```powershell
docker compose -f compose.week5.yml up -d --build
docker compose -f compose.week5.yml ps -a
docker compose -f compose.week5.yml logs initialize
```

The first build downloads the images and restores the .NET packages. The `initialize`
container applies database migrations and creates RabbitMQ exchanges, queues and bindings.
It should finish with exit code `0`; the API, planner and delivery worker then start.
No manual migrations or backend `.env` file are needed for this Docker setup.

| Service | Local address | Credentials / purpose |
|---|---|---|
| API / Swagger | <http://localhost:5155/swagger> | Try the HTTP endpoints |
| RabbitMQ management | <http://localhost:15675> | `pulse` / `local-pulse-demo` |
| RabbitMQ AMQP | `localhost:5675` | Message transport, not a web page |
| PostgreSQL | `localhost:5545` | Database/user: `pulse`; password: `local-pulse-demo` |
| Angular (next step) | <http://localhost:6510> | Web application |

Inside Docker, services use `rabbitmq:5672` and `postgres:5432`. The published host ports
above are for tools running on your machine. Compose supplies the connection settings;
its database and RabbitMQ data are kept in named Docker volumes.

### 2. Start the frontend

Open another PowerShell terminal at the repository root:

```powershell
npm ci
Set-Location frontend
npm ci
$env:NG_APP_API_BASE_URL = 'http://localhost:5155/api'
npm start
```

Install dependencies in **both** directories: the root package provides `@ngx-env/builder`,
which the Angular configuration uses, and `frontend` provides the application dependencies.
The environment variable applies to this terminal and selects the Docker API. If you configure
the URL in `frontend/.env` instead, use the same value. Restart `npm start` after changing it.
The frontend runs on port `6510`, which is already allowed by the Docker API's CORS settings.

### 3. Try the complete message queue flow

1. Open **Create Post** and copy the test author ID.
2. Open **Your feed** in another tab, paste that author ID and click **Follow author**.
3. Create a post in the first tab, then refresh the follower's feed.
4. Open RabbitMQ management and inspect the queues listed below. With workers running,
   messages can be consumed too quickly to see a backlog.

```text
POST /api/posts
  -> Post.Create records a domain event
  -> PostRepository saves the post + PostCreatedV1 in the database outbox
  -> the outbox delivery service publishes to RabbitMQ
  -> itfpulse-post-created-v1: start planning
  -> itfpulse-plan-fanout-v1: read follower pages, at most 500 per page
  -> itfpulse-deliver-feed-v1: insert feed references for each batch
GET /api/feeds/{followerId}
  -> read the delivered references joined to the original posts
```

The API's `201 Created` means the post and its outgoing event are saved. Feed delivery
is asynchronous. The planner commits each delivery command and its next-page command
through a consumer outbox. Repeated delivery cannot create duplicate follower/post feed
references because the database enforces a unique key.

To observe queued work, stop the delivery worker from the repository root, create another
post for an author with followers, and inspect `itfpulse-deliver-feed-v1`:

```powershell
docker compose -f compose.week5.yml stop worker
# Create a post in the UI, then inspect the queue and refresh the follower's feed.
docker compose -f compose.week5.yml start worker
```

Refresh the feed again after restarting the worker to see the delayed delivery.

### Logs, troubleshooting and shutdown

```powershell
docker compose -f compose.week5.yml logs --tail=100 api planner worker
docker compose -f compose.week5.yml logs --tail=100 initialize rabbitmq postgres
docker compose -f compose.week5.yml down
```

`down` stops the stack while keeping the named data volumes. Stop Angular with `Ctrl+C`.
Run the startup commands again to resume.

- If Docker reports that `dockerDesktopLinuxEngine` cannot be found, start Docker Desktop
  and wait for its Linux engine to be ready.
- If the API does not start, check `initialize` first: migrations and broker topology must
  complete successfully before the application services start.
- If Angular cannot find `@ngx-env/builder`, run `npm ci` at the repository root as well.
- If the UI cannot reach the API, verify the API URL includes `/api`, restart Angular,
  and open the UI at `http://localhost:6510`.
- If posts exist but feeds stay empty, follow the author **before** creating a new post,
  check that both `planner` and `worker` are running, and inspect worker logs and RabbitMQ
  `_error` queues. Following an author does not backfill older posts.

## Week 5: distribute posts to followers

The project now includes RabbitMQ messaging, transactional outboxes, bounded fan-out workers,
and an eventually consistent personal feed. See the [English Week 5 guide](docs/week5/README.md)
for architecture decisions, Docker startup, failure-recovery tests and a one-million-feed-entry load demo.

ITF Pulse is a **social media platform** where users can:

- Create posts  
- Share photos  
- Interact with the IT Factory community  
- Receive notifications  
- Manage profiles  

Over the coming weeks, you will incrementally design and implement the system.

You are not just writing code —  
you are **building software as engineers**.

---

# Repository Structure

```text
itf-pulse/
├─ backend/
│  ├─ src/
│  │  ├─ ITFPulse.Api/
│  │  ├─ ITFPulse.Application/
│  │  ├─ ITFPulse.Domain/
│  │  ├─ ITFPulse.Infrastructure/
│  │  ├─ ITFPulse.Worker/
│  │  └─ ITFPulse.Contracts/
│  │
│  ├─ tests/
│  │  ├─ ITFPulse.Domain.Tests/
│  │  ├─ ITFPulse.Application.Tests/
│  │  └─ ITFPulse.Architecture.Tests/
│
├─ frontend/
│  ├─ src/
│  │  ├─ app/
│  │  │  ├─ core/
│  │  │  ├─ shared/
│  │  │  ├─ features/
│  │  │  └─ layout/
│
├─ .gitignore
├─ README.md
```
