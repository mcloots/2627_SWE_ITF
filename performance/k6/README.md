---
order: 1
---

# Workshop: load testing ITF Pulse with k6

We compare open-source load-testing tools, then use k6 to test reading and publishing posts in ITF Pulse. We send a controlled request rate, check response correctness, inspect p50/p95/p99, and compare results before changing the application. We also observe publishing in the browser with a slow connection, both with and without API load.

You need the ITF Pulse repository, the .NET 10 SDK, k6, and a disposable PostgreSQL database for the API tests. The browser exercise also uses the repository's Angular frontend, Node.js 24, npm, and Chrome DevTools.

## Quick start on Windows

Start Docker Desktop and install k6 as described in section 2. After installing k6, open a new PowerShell terminal so `k6 version` works. From the repository root, start the dedicated database, apply migrations, and run the API:

```powershell
./performance/k6/start-workshop.ps1
```

Leave that terminal open. The script creates or reuses only `itfpulse-k6-db` on `127.0.0.1:55432`, checks its settings, and configures the local API/CORS settings from section 2. It does not reset existing workshop data. Use `-PrepareOnly` to prepare the database and apply migrations without starting the API.

In a second terminal, verify the complete API path before starting the exercises:

```powershell
./performance/k6/smoke-test.ps1
$env:BASE_URL = 'http://localhost:5212'
```

The smoke test creates a fixture, checks POST/GET, unknown-ID `404` and browser CORS, then runs each k6 target at 1 request/s for five seconds. It saves summaries in `performance/k6/results/` and fails on a contract or threshold failure. It adds a few workshop posts; these results are a readiness check, not a capacity baseline.

For the browser exercise, start the frontend in a third terminal:

```powershell
$env:NG_APP_API_BASE_URL = 'http://localhost:5212/api'
npm --prefix frontend ci
npm --prefix frontend start
```

Open `http://localhost:6510/posts/create`, then follow sections 3–7 below. The manual setup in section 2 remains available if you prefer to run each preparation command yourself. Stop the API/frontend with Ctrl+C and stop the database with `docker stop itfpulse-k6-db` when finished.

## Compare open-source load-testing tools

Several widely used open-source tools can test this API. Choose based on the workload, the team's skills, and the evidence you need. The comparison below concerns their open-source tools; a hosted service or commercial edition is not required for this workshop. The trade-offs describe their suitability for this exercise, rather than a ranking of performance.

| Tool and approach | Advantages | Disadvantages for this workshop | When would we choose it? |
| --- | --- | --- | --- |
| **[Apache JMeter](https://jmeter.apache.org/)**: Java application with a visual test-plan editor | Build and record test plans visually; supports HTTP, JDBC, JMS, and other protocols; generates HTML reports. | Requires Java and familiarity with its test-plan structure. XML test-plan changes can be harder to review than a short script. Use the GUI to design/debug, then [CLI mode for load generation](https://jmeter.apache.org/usermanual/get-started.html). | A team prefers visual test design or needs several protocols, such as HTTP and JMS. |
| **[Locust](https://locust.io/)**: user behaviour written in Python | Flexible Python scenarios, an interactive web UI, and built-in support for distributed workers. | Adds a Python environment to this workshop. In the usual user/task model, each user waits for its task to finish: a fixed user count does not guarantee a fixed request rate as responses slow down. See the [task lifecycle](https://docs.locust.io/en/stable/writing-a-locustfile.html). | A Python team needs stateful user journeys or distributed load generation. |
| **[Gatling Community Edition](https://gatling.io/download-gatling-community-edition)**: scenarios written using a code DSL | Supports JavaScript/TypeScript as well as JVM languages; provides structured scenarios, assertions, and HTML reports. | Students must learn its scenario DSL and [project/tooling setup](https://docs.gatling.io/reference/deploy/install-local/). For our small API tests, this adds preparation compared with running one k6 script. | A team wants structured performance scenarios in its preferred language and build workflow. |
| **[wrk](https://github.com/wg/wrk)**: command-line HTTP benchmark with optional Lua scripts | A compact way to generate substantial HTTP traffic and inspect throughput and latency. | Its standard options control threads/connections, rather than a target arrival rate. Response validation and pass/fail rules need custom scripting or surrounding tooling; these are central to our exercise. | A developer wants a quick HTTP endpoint benchmark with limited scenario logic. |
| **[Grafana k6](https://grafana.com/docs/k6/latest/)**: JavaScript/TypeScript scenarios run by the k6 CLI | Keeps requests, response checks, traffic settings, and performance thresholds in a reviewable script. Includes arrival-rate executors and a local web dashboard. | Its runtime differs from Node.js: npm packages that depend on Node APIs cannot simply be reused. Server CPU/database observations still need separate tooling. | This workshop needs a controlled API request rate, correctness checks, and explicit performance budgets in a small script. |

### Why we use k6 here

Our choice follows the learning goals and the existing JavaScript/TypeScript context of the Angular project:

1. **A small, reviewable test.** Students can read the request, payload, checks, and thresholds together, commit them to Git, and discuss changes in a pull request. The syntax is familiar, although [k6 modules and runtime differ from Node.js](https://grafana.com/docs/k6/latest/using-k6/modules/).
2. **An explicit traffic model.** With [`constant-arrival-rate`](https://grafana.com/docs/k6/latest/using-k6/scenarios/executors/constant-arrival-rate/), we plan 20 or 50 iterations per second independently of response time. Because this script makes one request per iteration, that means 20 or 50 planned requests/s. Enough VUs and generator capacity are still required; dropped iterations show when k6 cannot start the planned work.
3. **Correctness and speed together.** We validate `201`, `postId`, and `Location`, then apply [thresholds](https://grafana.com/docs/k6/latest/using-k6/thresholds/) to latency and failures. A failed threshold produces a nonzero exit code, which also supports a later CI exercise.
4. **A local classroom workflow.** One CLI command runs the test, and the [built-in dashboard](https://grafana.com/docs/k6/latest/results-output/web-dashboard/) shows results without a hosted account. The practical needs neither Node.js for the test script nor a cloud subscription.

These requirements can also be implemented with other tools; k6 gives us a convenient combination for this short practical. We are choosing a workflow, not claiming that k6 generates the most load or proves where the server bottleneck is. This script tests HTTP API behaviour; it does not measure Angular rendering or a complete browser journey.

## 1. Test reading and publishing in ITF Pulse

We test two operations with real PostgreSQL access: **publishing a text post**, which returns `201 Created`, and **reading a saved post by ID**, which returns `200 OK`. Keep their measurements separate: inserting a new row and looking up an existing row perform different work.

| What we do | Why |
| --- | --- |
| Run a baseline at 20 requests/s, then increase to 50 requests/s. | Observe how response times and failures change as traffic increases. These are synthetic test settings, not measured ITF Pulse usage. |
| Check the status, post ID, and response content; check `Location` for writes. | A fast response is useful only when it also has the expected status and content. |
| Read p50/p95/p99 and count failed responses and dropped iterations. | Show both typical and slower responses, and verify whether k6 generated the planned traffic. |
| Observe API resources, logs, and database activity. | Gather evidence about the limiting resource before choosing a change. |
| Repeat with the same traffic, data, and resources. | Make a fair comparison and check whether a change actually helps. |

### What we test

The workshop backend has `GET /api/health`, `POST /api/posts`, and `GET /api/posts/{postId}`. A health response does not test database access; reading a saved post does. An unknown post ID returns `404`. There is no feed-list endpoint, queue, or notification worker.

The k6 HTTP duration includes the time the API spends waiting for PostgreSQL before responding. Use server instrumentation to measure database time separately. In section 7, we observe browser behaviour separately; the k6 script itself does not measure Angular rendering or photo uploads.

The practical measures **performance under a defined workload**. It gives observations that can guide a later scaling experiment. Idempotency, an outbox, and worker recovery require their own implementation and failure tests.

## 2. Prepare a local API

Install k6 if necessary, then open a new PowerShell terminal:

```powershell
winget install k6 --source winget
k6 version
```

Use the [official installation instructions](https://grafana.com/docs/k6/latest/set-up/install-k6/) for other platforms. k6 executes JavaScript using its own runtime; you do not run these scripts with Node or install an npm k6 package.

Run all remaining commands from the **ITF Pulse repository root**. Start Docker Desktop, then create a dedicated workshop database:

```powershell
docker run --name itfpulse-k6-db -e POSTGRES_USER=workshop -e POSTGRES_PASSWORD=workshop -e POSTGRES_DB=itfpulse_k6 -p 127.0.0.1:55432:5432 -d postgres:18
docker exec itfpulse-k6-db pg_isready -U workshop -d itfpulse_k6
```

Wait until PostgreSQL reports that it is accepting connections. The credentials above are only for this local exercise. If the named container already exists, use `docker start itfpulse-k6-db` instead. A locally installed PostgreSQL with a separate workshop database is also suitable; adapt the connection string.

In terminal A, configure the connection, apply the committed migrations, and start the API:

```powershell
$env:ITFPULSE_ConnectionStrings__ITFPulse = 'Host=localhost;Port=55432;Database=itfpulse_k6;Username=workshop;Password=workshop'
$env:ITFPULSE_Hosting__HttpsTerminatedAtProxy = 'true'
$env:ITFPULSE_Cors__AllowedOrigins__0 = 'http://localhost:6510'
dotnet run --project backend/src/ITFPulse.Api --configuration Release --no-launch-profile -- --migrate
dotnet run --project backend/src/ITFPulse.Api --configuration Release --no-launch-profile -- --urls http://localhost:5212
```

Check that migration succeeds before starting the API. The hosting flag disables HTTPS redirection for this local HTTP demo. The CORS setting allows the Angular frontend on port 6510 to call this API. Environment variables override the project's configuration. Keep this terminal running. Do not point the test at a shared class database or deployed production API: the publishing exercise inserts real rows. Each run adds approximately `RATE × duration in seconds` posts; 50/s for 60 seconds is about 3,000 rows. A timed-out POST may still have saved its row, so the script does not retry.

In terminal B:

```powershell
$env:BASE_URL = 'http://localhost:5212'
Invoke-RestMethod "$env:BASE_URL/api/health"
k6 run -e TARGET=health -e RATE=1 -e DURATION=15s performance/k6/project-load.js
```

Expected: HTTP 200, health contract checks pass, zero failed responses. If posts fail later while health succeeds, inspect PostgreSQL connectivity and migrations.

## 3. Read and run the publishing script

Open `performance/k6/project-load.js`. Find these five parts:

1. `__ENV` reads settings such as `BASE_URL`, `RATE`, and `DURATION`.
2. `options.scenarios.api` uses `constant-arrival-rate` to start iterations at a fixed rate.
3. `http.post` sends the DTO expected by `CreatePostRequest`: `authorId` and `content`.
4. `check` verifies `201`, a nonempty GUID, and a matching Location header.
5. `thresholds` make k6 exit unsuccessfully when the agreed targets fail.

There is exactly **one HTTP request per iteration**, with redirects and retries disabled. Thus 20 iterations/s means a planned 20 requests/s. A VU is an available worker running an iteration; it is not a request rate or a logged-in user. With 50 requests/s and a five-second response time, roughly 250 concurrent workers would be needed. The defaults allocate 20 VUs up front and allow at most 100. There is no `sleep`: the executor controls pacing.

Warm up the publishing path, then run a baseline:

```powershell
k6 run -e TARGET=posts -e ALLOW_WRITES=true -e RATE=1 -e DURATION=15s performance/k6/project-load.js
New-Item -ItemType Directory -Force performance/k6/results | Out-Null
k6 run -e TARGET=posts -e ALLOW_WRITES=true -e RATE=20 -e DURATION=60s --summary-export performance/k6/results/baseline.json performance/k6/project-load.js
$LASTEXITCODE
```

The script uses a synthetic nonempty author GUID because this slice has no authentication or author lookup. If your later implementation adds those rules, provision a test user and adapt the test. No application secrets belong in the script.

Our **illustrative workshop targets** are p95 below 500 ms, fewer than 1% HTTP failures, fewer than 1% failed-or-wrong responses, and zero dropped iterations. We choose these budgets to practise defining and checking performance requirements. They are not measured capacity or agreed product requirements.

`check` records individual assertions. A `201` with a wrong body still fails the custom `failed_responses` metric. That metric adds exactly one boolean sample per response, so one response with three failed checks still counts as one wrong response. A check alone does not fail a k6 run; a threshold does. See [checks](https://grafana.com/docs/k6/latest/using-k6/checks/) and [thresholds](https://grafana.com/docs/k6/latest/using-k6/thresholds/).

Verify one saved row directly (the returned DTO alone cannot prove stored content):

```powershell
docker exec itfpulse-k6-db psql -U workshop -d itfpulse_k6 -c "SELECT id, author_id, content, created_at FROM itfpulse.posts WHERE content LIKE 'k6-workshop%' ORDER BY created_at DESC LIMIT 1;"
```

## 4. Read a saved post under load

Create one known post before the read test. The setup writes one row; the read load test itself only sends GET requests. Keep using the disposable workshop database.

```powershell
$env:AUTHOR_ID = '11111111-1111-4111-8111-111111111111'
$env:EXPECTED_CONTENT = 'k6-workshop read fixture'
$payload = @{ authorId = $env:AUTHOR_ID; content = $env:EXPECTED_CONTENT } | ConvertTo-Json
$created = Invoke-RestMethod -Method Post -Uri "$env:BASE_URL/api/posts" -ContentType 'application/json' -Body $payload
$env:POST_ID = $created.postId
Invoke-RestMethod "$env:BASE_URL/api/posts/$env:POST_ID"
```

Check that the GET returns the same `postId`, `authorId`, and `content`, plus a `createdAt` timestamp. The POST's `Location` now points to this implemented read route. Recreate the fixture after resetting the database and update `POST_ID`.

Warm up the read path, run a baseline, then increase traffic:

```powershell
k6 run -e TARGET=read -e RATE=1 -e DURATION=15s performance/k6/project-load.js
New-Item -ItemType Directory -Force performance/k6/results | Out-Null
k6 run -e TARGET=read -e RATE=20 -e DURATION=60s --summary-export performance/k6/results/read-baseline.json performance/k6/project-load.js
k6 run -e TARGET=read -e RATE=50 -e DURATION=60s --summary-export performance/k6/results/read-load.json performance/k6/project-load.js
```

The script requires `POST_ID` for `TARGET=read`. It checks `200`, the requested ID, the expected author, nonempty content, and a valid creation timestamp. With `EXPECTED_CONTENT` set, the content must match exactly. Reads use the same latency/error/drop thresholds as writes. Each iteration sends exactly one GET; it does not create or verify a post through an additional request.

The read query filters by the primary key, uses `AsNoTracking`, and selects the response fields without loading comments. There is no application response cache. Repeatedly reading the same post can warm database buffers: this measures a repeated single-post lookup, rather than a full feed or a broad set of cold reads.

Use reads alone when you want repeatable traffic without adding rows during the measurement. Compare each operation against its own baseline; a faster GET does not demonstrate an improvement to publishing.

## 5. Increase publishing traffic and interpret results

Before running, predict what changes at 50 requests/s. Then run:

```powershell
$env:K6_WEB_DASHBOARD = 'true'
$env:K6_WEB_DASHBOARD_EXPORT = 'performance/k6/results/load.html'
k6 run -e TARGET=posts -e ALLOW_WRITES=true -e RATE=50 -e DURATION=60s --summary-export performance/k6/results/load.json performance/k6/project-load.js
```

Open `http://localhost:5665` during the run. Close the dashboard tab when finished so k6 can exit. The exported HTML can be opened afterward. For unattended runs, set `$env:K6_WEB_DASHBOARD_PORT = '-1'` to export without listening. See the [dashboard documentation](https://grafana.com/docs/k6/latest/results-output/web-dashboard/).

| Result | Interpretation |
| --- | --- |
| `http_reqs` count | Actual HTTP requests sent; compare with the nominal 3,000 for 50/s × 60s |
| `iterations` | Completed iterations; inspect interrupted iterations as well |
| `med`, `p(95)`, `p(99)` | p50, p95, p99 response time; p99 is not the maximum |
| `http_req_failed` | Transport errors or unexpected HTTP status |
| `failed_responses` | HTTP or response-contract failure counted per response |
| `dropped_iterations` | Planned iterations k6 could not start |

The summary's per-second rate can include the time spent finishing in-flight work, so also compare counts and the configured scheduling duration. `http_req_duration` excludes initial connection establishment; read connection metrics too if those matter. A short run has too few observations for a stable p99. Repeat measurements.

Observe API CPU/memory in Task Manager and database activity/query timings separately. k6 does not collect server CPU or database timings for you. SQL logging at high volume can itself affect results. A slow request is evidence to investigate; it does not identify the bottleneck.

If iterations drop, investigate both the load generator and the application. Were there enough free VUs? Was k6 itself CPU-bound? Did the API or database slow down? Try the **same rate** with `-e PRE_ALLOCATED_VUS=100 -e MAX_VUS=100` after checking available resources. More VUs enable more concurrent requests; they do not repair a slow database. See [arrival rate](https://grafana.com/docs/k6/latest/using-k6/scenarios/executors/constant-arrival-rate/) and [dropped iterations](https://grafana.com/docs/k6/latest/using-k6/scenarios/concepts/dropped-iterations/).

Prove that a threshold controls the exit code by deliberately setting an unrealistically small latency budget:

```powershell
Remove-Item Env:K6_WEB_DASHBOARD_EXPORT -ErrorAction SilentlyContinue
$env:K6_WEB_DASHBOARD = 'false'
k6 run -e TARGET=posts -e ALLOW_WRITES=true -e RATE=1 -e DURATION=15s -e P95_MS=0.001 performance/k6/project-load.js
$LASTEXITCODE
```

The expected nonzero exit code demonstrates a failed performance budget; inspect the failed threshold to explain it. Successful HTTP checks can coexist with a failed latency threshold.

## 6. Make a fair comparison and report evidence

Repeat the baseline at 20/s for the operation you are investigating (`TARGET=read` or `TARGET=posts`). Compare it with that operation's first 20/s baseline, **not** its 50/s run, before discussing any change. Keep the same machine, API build configuration, request payload shape, author, read fixture, database starting state, rate, duration, VU settings, and warm-up. Run sequentially and repeat at least twice. Reset or restore the disposable database to the same starting state for a controlled write-test comparison; every publishing run otherwise changes it.

| Run/build and target | Rate / seconds | Planned / sent / completed | p50 / p95 / p99 | HTTP failure % | Failed response % | Dropped | API/DB observations |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Baseline 1 | | | | | | | |
| Baseline 2 | | | | | | | |
| Higher traffic | | | | | | | |
| After one justified change | | | | | | | |

Choose one suspected cause from observations. Inspect validation, database calls, and logging to identify unnecessary work in the tested path. If you make a change, rerun with the **same operation, baseline traffic, and database state**. Keep persistence, response content, and validation intact so that both runs perform the same operation. Test additional API replicas as a separate scaling experiment.

Complete the table and record what the test proves, what may be limiting capacity, and the next measurement or single change you would try. If all targets pass at 50/s, say exactly that; you have not found maximum capacity or proved horizontal scaling.

### Connect publishing to reliability

In ITF Pulse, **post saved** and **notification delivered** are separate outcomes. If publishing waits synchronously for a slow notification provider, that wait can increase publishing latency and occupy more concurrent workers.

An outbox saved in the same database transaction as the post, followed by asynchronous processing, could let `201` confirm saved content while notification delivery remains pending. This project does not yet implement that boundary. A separate future test must stop/restart the worker and verify pending jobs, safe duplicate handling, and eventual outcomes. Fast POST responses alone cannot prove delivery, idempotency, or recovery.

## 7. Observe publishing with browser throttling

We use browser throttling to investigate the experience of one user with a slower connection. We then combine that with k6 traffic to see how publishing behaves while the API handles other requests.

### Connect the browser to the same API

Use the Angular frontend in the **ITF Pulse repository**, connected to the same disposable .NET API and database as k6. Keep terminal A running with the CORS setting from section 2. In terminal C, from the ITF Pulse repository root:

```powershell
$env:NG_APP_API_BASE_URL = 'http://localhost:5212/api'
npm --prefix frontend ci
npm --prefix frontend start
```

Open `http://localhost:6510/posts/create`. The environment setting is read when Angular starts; restart the frontend after changing it. The API URL includes `/api`, whereas k6's `BASE_URL` does not. The browser calls the .NET API on port 5212.

Publish one post and verify in DevTools **Network** that `POST http://localhost:5212/api/posts` returns `201`. Use the composer directly: this backend supports reading one post by ID, but has no feed-list endpoint. Resolve connection, CORS, or contract errors before comparing performance.

### Compare normal and slow connections

1. Load the composer, open DevTools, and select **Network**. Start with **No throttling**, CPU throttling disabled, and no k6 run in progress. Keep the cache setting unchanged throughout the comparison.
2. Publish a short text post using **Create Post**. Inspect the POST's status and duration, and observe the feedback from **Creating...** until **Post created** appears.
3. Select a slow network preset, such as **Slow 4G**, in the Network throttling menu. Record the profile name and its latency/upload/download settings. Keep DevTools open and publish another post of the same length.
4. Check whether the interface shows that publishing is in progress, prevents duplicate submissions, and remains responsive. Record whether the draft stays available while the request is pending. Inspect POST counts; CORS OPTIONS requests are not duplicate posts.

For a small text post, added network latency can matter more than bandwidth. The browser request duration includes the emulated network conditions; it is not a measurement of server processing time alone. See [DevTools network throttling](https://developer.chrome.com/docs/devtools/network/reference#throttling).

### Add API load while keeping browser conditions fixed

Keep the same slow network profile and CPU setting. In the k6 terminal, run:

```powershell
k6 run -e TARGET=posts -e ALLOW_WRITES=true -e RATE=50 -e DURATION=120s performance/k6/project-load.js
```

While k6 is running, publish another comparable post through the browser. Compare it with the slow-network observation without k6 load. Record browser behaviour alongside k6 latency, errors, and dropped iterations. Browser submissions add real rows and a small amount of traffic beyond k6's configured rate; keep this experiment separate from the controlled API baselines.

To use background read traffic instead, run the same command with `TARGET=read` after preparing `POST_ID`, `AUTHOR_ID`, and `EXPECTED_CONTENT` in that terminal. Record which operation generates the background load and keep it unchanged within each comparison.

**DevTools throttling applies to the inspected browser, not the separate k6 CLI process.** On a shared machine, the browser, API, database, and k6 can still compete for CPU and memory. Observe resource use before attributing a slowdown to the server.

| Observation | Network profile | CPU setting | k6 rate | Browser POST status / duration | Pending feedback / duplicate prevention / responsiveness |
| --- | --- | --- | --- | --- | --- |
| Browser baseline | No throttling | No throttling | No run | | |
| Slow connection | Recorded slow profile | No throttling | No run | | |
| Slow connection with API load | Same slow profile | No throttling | 50/s | | |

### Try CPU throttling separately

Stop the k6 run or let it finish, restore **No throttling** for the network, then use the **Performance** panel to record the same interaction with CPU throttling disabled and with a slowdown preset. Keep other settings equal. Compare typing, clicking, and rendering; a small composer may show little difference. CPU throttling is especially useful when investigating a larger feed with more JavaScript and rendering work. See [DevTools performance recording and CPU throttling](https://developer.chrome.com/docs/devtools/performance/overview).

Restore network and CPU settings to **No throttling** when finished. Record which condition changed the experience and what you would measure next; a few manual interactions cannot establish a browser p95 or maximum server capacity.

## Troubleshooting and cleanup

Common problems: connection refused means check the API URL/listening port; 307/308 means check HTTPS redirection; 500 on publishing means inspect the API log and migration/database setup; 404 means verify the actual route and do not include `/api` in `BASE_URL`. A JSON parse failure becomes a failed response rather than terminating the script. Use `k6 inspect performance/k6/project-load.js` to inspect the default scenario.

Stop the API with Ctrl+C, then stop the disposable database:

```powershell
docker stop itfpulse-k6-db
```

To reuse it, start the container again. To reset the exercise, remove **only this disposable workshop container** (`docker rm -v itfpulse-k6-db` after stopping it), then repeat setup and migrations. This deletes its workshop data. Results in `performance/k6/results/` are ignored by Git; keep any reports needed for comparison before cleaning them up.

CI follow-up: a k6 threshold can fail a job, but a repeatable CI performance test needs its own API and disposable database, migrations, readiness checks, and controlled resources. Keep this classroom exercise manual until that environment exists; the deployment workflow does not run these load tests.
