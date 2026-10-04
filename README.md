# ITF Pulse  

**Deployment setup:** see [CI/CD and environments](#cicd-and-environments) below for the complete GitHub, Cloudflare, Render, Neon, and Docker checklist.
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

## CI/CD and environments

[`.github/workflows/ci-cd.yml`](.github/workflows/ci-cd.yml) builds/tests pull requests and deploys pushes using this mapping:

| Git branch | GitHub environment | Cloudflare Pages project (example) | Render service (example) | Neon project (example) |
| --- | --- | --- | --- | --- |
| `main` | `prod` | `itf-pulse-prod` | `itf-pulse-api-prod` | `itf-pulse-prod` |
| `develop` | `dev` | `itf-pulse-dev` | `itf-pulse-api-dev` | `itf-pulse-dev` |
| `test-*` or `test/*` | `test` | `itf-pulse-test` | `itf-pulse-api-test` | `itf-pulse-test` |

Replace the example names with your available names. All test branches share **one** test environment and database; this does not provision per-branch previews. Deleting a test branch does not delete the deployed environment or reset data. All branches run CI and Semgrep on pushes, but only the branches in the table deploy.

### What the pipeline does

1. Install frontend dependencies from `frontend/package-lock.json`, run Angular tests, and build Angular with production optimizations.
2. Build the .NET 10 solution and run all three backend test projects. Domain/Application use executable xUnit v3 runners; Architecture uses VSTest, so a solution-wide `dotnet test` is not appropriate for the current mix.
3. Verify the Linux API Docker image builds. Pull requests stop after checks and have no deployment secrets.
4. Run **Frontend tests**, **Backend tests**, **Lint**, **Build**, and **Semgrep security scan** in parallel. The **Merge gate** succeeds only when every job succeeds. Publish the API to `ghcr.io/<owner>/<repository>/api` only after both succeed, with a unique commit/run tag. Deployments use its immutable SHA256 digest, recorded in the Actions summary.
5. Rebuild Angular with the chosen GitHub environment's public variables, then run the API image with `--migrate` against that environment's Neon database.
6. Trigger Render through its API and poll that exact deployment until it is live (up to 20 minutes); check `/api/health`. This runs directly in GitHub Actions using Bash, `curl`, and `jq`, available on the Ubuntu runner; no Python scripts are needed.
7. Upload `frontend/dist/itf-pulse/browser` to the selected Cloudflare Pages project.

Deploy jobs are serialized per environment and running deploys are not automatically canceled. GitHub may replace a pending deployment with a newer pending run; concurrency does not guarantee FIFO ordering. Avoid pushing multiple competing test branches simultaneously. Releases across the database, API, and frontend are not atomic: use backwards-compatible migrations/API changes. The existing health endpoint checks API availability, not database connectivity.

### Semgrep security scanning

The **Semgrep security scan** job in [`.github/workflows/ci-cd.yml`](.github/workflows/ci-cd.yml) runs on every branch push, pull request (including forks and Dependabot), and manual run. It uses Semgrep Community Edition without an account or token. A full repository scan runs each time, rather than scanning only the PR diff.

The scanner version is pinned to `semgrep/semgrep:1.179.0`. The registry rulesets `p/csharp`, `p/typescript`, and `p/github-actions` check backend C#, frontend TypeScript, and GitHub workflows. These rulesets are downloaded at scan time and can evolve independently of the scanner version. `.semgrepignore` excludes dependencies and generated files. Metrics and version checks are disabled. Workflow actions are pinned to full commit SHAs; update those pins when upgrading actions.

`--error` fails the job on any reported finding; `--strict` also fails on scan warnings such as parsing errors. Scanner/configuration failures also fail the job. Image publishing requires the Merge gate to succeed, so scan failures block migrations and deployment. Findings appear in the Actions log and the `semgrep-report` SARIF artifact, retained for 14 days when generated.

Activate the required-check ruleset described below to block merges when any CI check fails. No scanner account or token is needed.

Semgrep CE scans for security patterns; it does not provide a coverage quality gate or dependency vulnerability scanning. Its C# engine has limited support for modern language syntax and analysis across functions/files. Review scan errors rather than treating skipped code as scanned. See [Semgrep CE language support](https://semgrep.dev/docs/semgrep-ce-languages) and the [CLI reference](https://semgrep.dev/docs/cli-reference).

To run the same scan locally from the repository root with Docker:

```sh
docker run --rm -v $PWD:/src -w /src semgrep/semgrep:1.179.0 semgrep scan --config p/csharp --config p/typescript --config p/github-actions --error --strict --metrics=off --disable-version-check --sarif-output=semgrep.sarif .
```

### Blocking merges until every check passes

A failing workflow blocks deployment but does **not** automatically block merges. GitHub repository rules must also be activated. [`.github/required-checks.ruleset.json`](.github/required-checks.ruleset.json) is an importable ruleset for `main`, `develop`, `test-*`, and `test/**` deployment branches. The file alone does not activate rules on GitHub.

1. Push the updated workflow to a feature branch and open a PR so the new check names are registered in GitHub.
2. Open **Settings > Rules > Rulesets > New ruleset > Import a ruleset** and select `.github/required-checks.ruleset.json`.
3. Verify **Enforcement status: Active**, the target branches, and an **empty Bypass list**, then save. Replace obsolete required checks such as **Build and test** in existing protections.
4. Confirm the following six checks are required, with **GitHub Actions** as their expected source (the ruleset pins integration ID `15368`): **Frontend tests**, **Backend tests**, **Lint**, **Build**, **Semgrep security scan**, and **Merge gate**.
5. Verify a PR with a failing check cannot merge. After correcting it and pushing a new commit, all checks must run successfully. If the target branch advances, update the PR branch and rerun its checks.

The ruleset requires PRs, up-to-date status checks, and blocks force pushes. It adds no bypass actors, including administrators. The workflow runs on every push, PR, manual run, and merge-group check event. There are no path filters or optional test/lint/security jobs. The **Merge gate** uses `always()` and explicitly rejects any failed, canceled, or skipped dependency; this prevents a skipped job from satisfying the aggregate check. Pending required checks also prevent merging.

Frontend lint uses recommended ESLint, TypeScript, Angular, and template accessibility rules, including inline templates. `npm run lint` treats warnings as failures. Backend lint first builds with `--warnaserror` to reject compiler and analyzer warnings, then verifies analyzer code fixes with `dotnet format analyzers --verify-no-changes --severity warn`; it does not impose whitespace formatting. All three backend test projects run, using their existing runners. To check locally:

```sh
cd frontend
npm run lint
npm test -- --watch=false
cd ..
dotnet format analyzers backend/ITFPulse.slnx --verify-no-changes --severity warn
dotnet build backend/ITFPulse.slnx --configuration Release
dotnet run --project backend/tests/ITFPulse.Domain.Tests --configuration Release --no-build
dotnet run --project backend/tests/ITFPulse.Application.Tests --configuration Release --no-build
dotnet test backend/tests/ITFPulse.Architecture.Tests --configuration Release --no-build
```

GitHub repository administrators can still edit or remove rules; this ruleset controls merging while active. Ruleset availability depends on repository visibility and GitHub plan. See [GitHub ruleset setup](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/creating-rulesets-for-a-repository).

### 1. GitHub setup

Enable GitHub Actions for this repository and permit the actions used in the workflow. The image job requests `packages: write` on GitHub's automatically supplied `GITHUB_TOKEN`; you do not create that secret yourself. Organization policies must permit package publishing. If a package already exists, grant this repository Actions access in the package settings.

Under **Settings → Environments**, create exactly `dev`, `test`, and `prod`. Add the following values separately to **each** environment:

| Kind | Name | Example / purpose |
| --- | --- | --- |
| Variable | `NG_APP_API_BASE_URL` | `https://itf-pulse-api-dev.onrender.com/api`; HTTPS, includes `/api`, no trailing slash |
| Variable | `NG_APP_APP_NAME` | Optional, e.g. `ITF Pulse Dev`; defaults to `ITF Pulse` |
| Variable | `CLOUDFLARE_ACCOUNT_ID` | Your Cloudflare account ID |
| Variable | `CLOUDFLARE_PAGES_PROJECT` | The matching Pages project name, e.g. `itf-pulse-dev` |
| Variable | `RENDER_SERVICE_ID` | The matching Render service ID, e.g. `srv-...` |
| Secret | `CLOUDFLARE_API_TOKEN` | Cloudflare token with **Account → Cloudflare Pages → Edit**, scoped to your account |
| Secret | `RENDER_API_KEY` | Render account API key with access to the service |
| Secret | `DATABASE_CONNECTION_STRING` | Direct/unpooled Neon **Npgsql-format** connection string for migrations |

Restrict deployment branches for `prod` to `main`, and `dev` to `develop`. For `test`, allow `test-*` and `test/*` (add patterns for any deeper slash levels you use). Only trusted contributors should be able to change code/workflows on deployment branches, since those jobs receive secrets. Where your GitHub plan supports it, require a reviewer for prod; deployment then waits for approval. Activate the required-check ruleset below and promote through pull requests.

The workflow lives in the repository; merge it to the default branch so **Actions → CI/CD → Run workflow** becomes available. Manual runs use the selected branch's environment. Other branches run checks and analysis without deployment.

### 2. Neon: three isolated databases

Create separate Neon projects for dev, test, and prod, preferably in a region near Render. Use a separate database/role in each project. Copy the host, database, user and password from Neon's connection details into an Npgsql connection string:

```text
Host=ep-EXAMPLE.REGION.aws.neon.tech;Port=5432;Database=neondb;Username=YOUR_ROLE;Password=YOUR_PASSWORD;SSL Mode=VerifyFull;Channel Binding=Require
```

Use the **direct/unpooled** hostname (no `-pooler`) in GitHub's `DATABASE_CONNECTION_STRING`; the migration role needs schema/table creation and alteration permissions. Npgsql expects semicolon-separated key/value pairs, not a `postgresql://...` URL. Quote/escape special characters according to Npgsql connection-string syntax. Do not put literal surrounding quotes around the entire value in dashboard fields.

On Render, use a connection string for the **same database**, optionally with Neon's pooled hostname. The workflow applies the committed EF Core migrations using the published container before deploying it; no paid Render pre-deploy command or EF CLI installation is needed. It does not generate migrations or copy production data to test. Commit new migration files with application changes.

A failed migration stops deployment. A later deployment failure does not undo a successful migration. Review migrations, use additive changes while old code is live, and arrange database recovery before destructive production changes.

### 3. Cloudflare Pages: three Direct Upload projects

Create three **Pages Direct Upload** projects, one per environment. Do not enable separate Git-triggered builds for these projects: GitHub Actions owns deployment. One CLI option is:

```sh
npx wrangler@4 login
npx wrangler@4 pages project create itf-pulse-dev --production-branch main
npx wrangler@4 pages project create itf-pulse-test --production-branch main
npx wrangler@4 pages project create itf-pulse-prod --production-branch main
```

All three projects deliberately use `main` as their Cloudflare production branch. The workflow selects the **project** based on the Git branch, then deploys with `--branch=main`, so dev/test also get stable `https://<project>.pages.dev` URLs. This is independent of the source Git branch mapping.

Create the API token described above and copy the account ID/project names into each GitHub environment. No Cloudflare build command, root directory, or build-time variables are needed: Actions uploads prebuilt files. `NG_APP_*` values are embedded in browser JavaScript and are public. Database passwords/API tokens must never be frontend variables. Changing an Angular variable requires a new build/deployment.

Pages provides SPA fallback when there is no top-level `404.html`, so Angular route refreshes work without a custom redirect rule. See [Direct Upload CI](https://developers.cloudflare.com/pages/how-to/use-direct-upload-with-continuous-integration/) and [SPA routing](https://developers.cloudflare.com/pages/configuration/serving-pages/).

### 4. Bootstrap GHCR, then create Render services

Render needs an existing image before you can create an image-backed service:

1. Merge this workflow to your default branch. Under **Actions → CI/CD → Run workflow**, choose `main` or `develop` and enable **publish_only**. This runs checks and publishes an image without requiring cloud credentials or deploying anything. An initial automatic deployment before setup may fail for missing settings; this bootstrap run is intentional.
2. Copy the full `ghcr.io/.../api@sha256:...` reference from the **Publish API image** summary.
3. Choose package visibility. For a public image, explicitly make the GHCR package public after its first publication. For a private image, create a GitHub **PAT (classic)** with `read:packages`, authorize SSO if required, and configure a Render registry credential using your GitHub username and that token. GitHub Actions uses `GITHUB_TOKEN`; Render needs its own persistent pull credential. See [GHCR authentication](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry).
4. In Render, create three **Web Services → Existing Image** services using that image reference. Choose the desired region and Free instance type if appropriate. Do not create Git-source Docker services: this workflow deploys prebuilt GHCR images.
5. Set each service's **Health Check Path** to `/api/health`. Leave the Docker command unset so the image entrypoint runs. Disable any separate automatic deployment mechanism. No deploy hook is needed; the workflow uses the Render API.
6. Add the runtime environment settings below, save them, and copy each Render service ID/public URL to its matching GitHub environment. Create a Render API key and store it as `RENDER_API_KEY`.

| Render environment variable | Value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` for **all** hosted environments; keeps Swagger/development diagnostics disabled |
| `PORT` | `8080` |
| `ASPNETCORE_HTTP_PORTS` | `8080` (also the Docker image default) |
| `ITFPULSE_Hosting__HttpsTerminatedAtProxy` | `true`; Render handles public HTTPS and forwards HTTP to the container |
| `ITFPULSE_ConnectionStrings__ITFPulse` | Npgsql connection string for this environment's Neon database |
| `ITFPULSE_CORS__ALLOWEDORIGINS__0` | Exact frontend origin, e.g. `https://itf-pulse-dev.pages.dev`; **no trailing slash or path** |
| `ITFPULSE_SERVICE__NAME` | e.g. `ITF Pulse API Dev` |

For a custom frontend domain, add it using `ITFPULSE_CORS__ALLOWEDORIGINS__1` (and subsequent indices), or replace the original origin. CORS is explicit; arbitrary Cloudflare preview URLs are not allowed. The prefix `ITFPULSE_` is stripped by the API configuration provider; double underscores represent nested keys.

All services use the same GHCR repository path. The workflow supplies a different immutable image digest on every deployment. Render requires the image host/repository to match the service's configured image. See [image-backed services](https://render.com/docs/deploying-an-image) and [deployment API](https://api-docs.render.com/reference/create-deploy).

### 5. Deploy and promote

After all three GitHub environments and cloud services are configured, run **CI/CD** on `develop` with `publish_only` unchecked. Confirm the workflow succeeds, the Pages site loads, `/api/health` responds, and browser API requests pass CORS. The initial Render service can start before tables exist; the first full workflow applies migrations.

Create a test branch from dev locally:

```sh
git switch develop
git pull --ff-only
git switch -c test-release-1
git push -u origin test-release-1
```

`test/release-1` also works. Subsequent pushes redeploy the shared test environment. Branch ancestry is a team convention, not enforced by CI: Git does not store a permanent “created from” relationship. Merge tested changes through your normal PR process into `main` to deploy prod. All environments use Angular production optimization; `dev` is a deployment destination, not an unoptimized Angular build.

### Local development and Docker

Use Node.js 24, npm, .NET SDK 10, and Docker Desktop with Linux containers when testing Docker. Frontend scripts live in `frontend/package.json`.

Copy `frontend/.env.example` to `frontend/.env` and `backend/src/ITFPulse.Api/.env.example` to `.env` in that same API directory. Set a real development database connection and the API URL reported by its launch profile. Keep local `ITFPULSE_Hosting__HttpsTerminatedAtProxy=false`. Run the API from its project directory so `DotNetEnv` discovers that `.env`; run `npm ci` and `npm start` from `frontend`. Local Angular runs on port 6510.

To build and run the API container from the repository root:

```sh
docker build -t itf-pulse-api:local backend
docker run --rm --env-file backend/src/ITFPulse.Api/.env itf-pulse-api:local --migrate
docker run --rm -p 8080:8080 --env-file backend/src/ITFPulse.Api/.env -e ASPNETCORE_ENVIRONMENT=Production -e ITFPULSE_Hosting__HttpsTerminatedAtProxy=true itf-pulse-api:local
```

For this HTTP-only local Docker smoke test, point the frontend at `http://localhost:8080/api`. Use raw `KEY=value` lines without shell-style wrapping quotes in a Docker env file. A database on your host uses `host.docker.internal`, not `localhost` inside Docker. The Docker build excludes all `.env` files, uses a multi-stage build, and runs the API as a non-root user. No secrets are baked into the image.

### Failures, retries, and rollback

- **Missing environment setting:** verify the names and kinds in the GitHub table, including the selected `dev`/`test`/`prod` environment.
- **GHCR 403 / Render pull denied:** check package Actions access, package visibility, or Render's `read:packages` registry credential and expiry.
- **Migration failure:** confirm the direct Neon host, TLS settings, credentials and role permissions. Check the failed migration before retrying.
- **Render failure/timeout:** inspect that deployment's logs, port 8080, connection string and health path. The workflow step fails if Render fails or does not become live within 20 minutes (an in-flight request/retry can finish slightly later). A timeout/manual workflow cancellation does not cancel the remote Render deployment; inspect its state before another run.
- **API healthy but browser fails:** check `NG_APP_API_BASE_URL` includes `/api`, and CORS matches the browser's exact origin. A frontend URL change needs a rebuild.
- **Cloudflare fails after API deploy:** the API/database already changed; fix the token/project and rerun. Keep API changes compatible with the previous frontend.
- **Rollback:** preferably revert the Git commit through a PR to the environment's branch and let CI redeploy. For an emergency, redeploy a recorded earlier image digest in Render and restore the matching Pages deployment. Keep GHCR images needed for rollback. Neither approach reverses database migrations; database recovery is a separate deliberate operation.

### Cost expectations

| Part | Provider | Cost expectation |
| --- | --- | --- |
| Source / CI | GitHub / GitHub Actions | Within applicable repository/account allowances |
| Angular hosting | Cloudflare Pages | Free plan within limits |
| API hosting | Render Docker Web Services | Free plan subject to shared hours and idle shutdown |
| PostgreSQL | Neon | Free plan within current project/compute/storage limits |
| Images | GitHub Container Registry | Subject to GitHub Packages allowances and current billing policy |

This is suitable for a course/demo setup; **three always-on APIs are not guaranteed to cost €0**. Render currently grants **750 free instance hours per workspace per month**, shared by all services; idle services spin down after 15 minutes and can take about a minute to wake. Exhausting those hours suspends free services for the rest of the month. Render advises against Free instances for production applications. See [Render Free limits](https://render.com/docs/free). Check [Neon pricing](https://neon.com/pricing), [Cloudflare Pages limits](https://developers.cloudflare.com/pages/platform/limits/) and [GitHub billing](https://docs.github.com/en/billing) for current allowances. Uploaded user files also need durable object storage; Render's container filesystem is ephemeral.
