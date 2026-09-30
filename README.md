# VisionAiChrono

**AI video & image analysis platform** - upload media, run detection tasks, get results and an Excel report.

Built with **.NET 9** using **Clean Architecture**, background jobs via **Hangfire**, **SQL Server**, **Redis**, **SignalR** and **JWT** auth.

---

## Table of Contents

- [What it does](#what-it-does)
- [Architecture](#architecture)
- [Tech stack](#tech-stack)
- [Quick start](#quick-start)
- [Configuration](#configuration)
- [The Vision AI service](#the-vision-ai-service)
- [API reference](#api-reference)
- [How execution works](#how-execution-works)
- [Excel export](#excel-export)
- [Background jobs](#background-jobs)
- [Project layout](#project-layout)
- [Docker](#docker)
- [CI](#ci)
- [Security notes](#security-notes)

---

## What it does

1. **Create a pipeline** - a named container for your analysis config.
2. **Run it** - upload one or more images/videos and pick which tasks to apply.
3. **Collect results** - every task against every file produces a stored detection result.
4. **Export** - each run automatically generates a 3-sheet Excel report you can download.

Tasks are simple integers:

| `tasks` value | Task | External endpoint |
| :--- | :--- | :--- |
| `0` | Person | `/detect/person` |
| `1` | Weapon | `/detect/weapon` |
| `2` | Fire | `/detect/fire` |

Runs can be **synchronous** (results come back in the response) or **asynchronous** (queued in Hangfire, poll for results).

---

## Architecture

```
+-------------------------------------------------------------+
|  VisionAiChrono.Api                                          |
|  Controllers - Middlewares - Hangfire Jobs - SignalR Hub      |
+----------------------------+--------------------------------+
                             |
+----------------------------v--------------------------------+
|  VisionAiChrono.Application                                  |
|  Services - DTOs - Validators - VisionDetection             |
+----------------------------+--------------------------------+
                             |
+----------------------------v--------------------------------+
|  VisionAiChrono.Domain                                       |
|  Entities - Enums - Repository contracts                     |
+----------------------------+--------------------------------+
                             |
+----------------------------v--------------------------------+
|  VisionAiChrono.Infrastructure                              |
|  EF Core - AppDbContext - Migrations - Redis                 |
+-------------------------------------------------------------+
```

Dependencies point **inward only**. `Domain` has no external dependencies, `Application` depends on `Domain`, and `Infrastructure` and `Api` depend on both.

---

## Tech stack

| Layer | Technology |
| :--- | :--- |
| Runtime | .NET 9 (C# 13) |
| Web | ASP.NET Core Web API, Kestrel (port 8080) |
| Auth | JWT bearer tokens, token blacklist, OTP, Google OAuth |
| ORM | EF Core 9, Code-First Migrations |
| Database | SQL Server |
| Background jobs | Hangfire (SQL Server storage) |
| Cache | StackExchange.Redis |
| Real-time | SignalR (`/hubs/notifications`) |
| Email | MailKit / MimeKit via SMTP |
| Validation | FluentValidation 12 |
| Excel | ClosedXML |
| Docs | Swagger / Swashbuckle |
| Docker | Multi-stage Dockerfile + Compose |

---

## Quick start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server (local or remote)
- Optional: the Python vision service on port `8000`
- Optional: Redis on `6379`

### 1. Clone

```bash
git clone https://github.com/OsamaMousa11/VisionAiChrono.git
cd VisionAiChrono
```

### 2. Create `appsettings.json`

This file is **git-ignored** (it holds secrets), so you must create it yourself:

```bash
cd src/VisionAiChrono.Api
cp appsettings.Development.json appsettings.json
```

Then open it and fill in your values. See [Configuration](#configuration).

### 3. Apply migrations

```bash
dotnet ef database update --project src/VisionAiChrono.Infrastructure --startup-project src/VisionAiChrono.Api
```

Or just run the app. The database is created on startup if missing.

### 4. Run

```bash
dotnet run --project src/VisionAiChrono.Api
```

The API starts on **http://localhost:8080**.

- Swagger UI: http://localhost:8080/swagger
- Hangfire dashboard: http://localhost:8080/hangfire

### 5. Log in

On first start the seeder creates your `ADMIN` user from the `AdminUser` section of `appsettings.json`.

```bash
curl -X POST http://localhost:8080/api/Account/login \
  -H "Content-Type: application/json" \
  -d '{"email":"you@example.com","password":"your-password"}'
```

Use the returned `data.token` as a Bearer token:

```bash
curl http://localhost:8080/api/Pipeline \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 6. Run a pipeline

```bash
# create
curl -X POST http://localhost:8080/api/Pipeline \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Nightly scan","description":"Main entrance"}'

# execute (tasks = person, weapon, fire)
curl -X POST http://localhost:8080/api/Pipeline/YOUR_PIPELINE_ID/execute \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "tasks=0,1,2" \
  -F "files=@image.png" \
  -F "files=@clip.mp4"
```

---

## Configuration

All settings live in `src/VisionAiChrono.Api/appsettings.json`.

> **Never commit this file.** It is already in `.gitignore`. Use environment variables, user-secrets or a secret manager in production.

```json
{
  "ConnectionStrings": {
    "connstr": "Server=...;Database=VisionAiChrono;User Id=...;Password=...;TrustServerCertificate=True",
    "Redis": "localhost:6379"
  },
  "JWT": {
    "Key": "<at-least-32-characters>",
    "Issuer": "VisionAiChrono",
    "Audience": "VisionAiChrono",
    "DurationInDays": 7
  },
  "MailSettings": {
    "Email": "smtp-user@example.com",
    "SenderEmail": "noreply@example.com",
    "DisplayName": "VisionAiChrono",
    "Password": "<smtp-password>",
    "Host": "smtp.example.com",
    "Port": 587
  },
  "AdminUser": {
    "Email": "admin@example.com",
    "Password": "<strong-password>",
    "Name": "Administrator"
  },
  "Authentication": {
    "Google": {
      "ClientId": "<google-client-id>",
      "ClientSecret": "<google-client-secret>",
      "CallbackPath": "/signin-google"
    }
  },
  "AI": {
    "BaseUrl": "http://127.0.0.1:8000"
  },
  "Hangfire": {
    "Dashboard": { "Path": "/hangfire" }
  }
}
```

### Notes

- **Database** - the key is literally `connstr`. It is used for both EF Core and Hangfire storage.
- **Redis** is optional. Without a `Redis` connection string the app falls back to `localhost:6379`.
- **Google OAuth** is optional. Leave it blank to disable social login.
- **AdminUser** is seeded at every startup. The seeder is idempotent: it creates the user and the `ADMIN` role only if missing.
- Uploaded media is stored under `src/VisionAiChrono.Api/App_Data/media/`, partitioned by year and month.

> **`App_Data` is not git-ignored** and 18 test uploads are currently tracked in the repository. Add `src/VisionAiChrono.Api/App_Data/` to `.gitignore` and untrack the files with `git rm -r --cached src/VisionAiChrono.Api/App_Data` before going public.

---

## The Vision AI service

This API does **not** run detection itself. It forwards each file to an external Python service that owns the models.

Default location: **http://127.0.0.1:8000**, registered as a typed `HttpClient` in `Program.cs`.

### Expected endpoints

| Method | Path | Purpose |
| :--- | :--- | :--- |
| `GET` | `/health` | Liveness probe |
| `POST` | `/detect/person` | Person detection |
| `POST` | `/detect/weapon` | Weapon detection |
| `POST` | `/detect/fire` | Fire detection |
| `GET` | `/download/{video_id}` | Download a processed video |

### Request

`multipart/form-data` with a single `file` part. The content type is derived from the file extension:

```
image/png   image/jpeg   image/webp   image/bmp
video/mp4   video/quicktime   video/x-msvideo   video/x-matroska   video/webm
```

### Response

```json
{
  "type": "image",
  "task": "person",
  "count": 0,
  "detections": []
}
```

For videos the same shape is returned with `"type": "video"`, plus `videoId`, `durationSeconds` and a `downloadUrl`.

Detection boxes carry `label`, `confidence` and `box` (`x`, `y`, `width`, `height`).

### Failure handling

Any non-2xx response is wrapped in an `ExternalServiceException`:

```
Vision detection service returned an error (404): {"detail":"Not Found"}
```

Connection failures produce:

```
Vision detection service is unavailable. Make sure the Python service is running.
```

The request timeout is **3 minutes**, so prefer `execute-async` for long videos.

---

## API reference

All endpoints are prefixed with `/api`. Swagger has the full, always-current list.

### Pipeline

| Method | Route | Auth | Description |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/Pipeline` | Bearer | Create a pipeline |
| `POST` | `/api/Pipeline/{id}/execute` | Bearer | Run tasks synchronously |
| `POST` | `/api/Pipeline/{id}/execute-async` | Bearer | Queue a background run |
| `GET` | `/api/Pipeline` | Bearer | List pipelines |
| `GET` | `/api/Pipeline/{id}` | Bearer | Get one pipeline |
| `GET` | `/api/Pipeline/{id}/runs` | Bearer | Run history (paged, filter by status) |

### Pipeline run

| Method | Route | Auth | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/PipelineRun/{id}/detail` | Bearer | Full run detail including media and results |
| `GET` | `/api/PipelineRun/{id}/results` | Bearer | Flat list of detection results |
| `GET` | `/api/PipelineRun/{id}/export` | Bearer | Download the Excel report |

### Vision

| Method | Route | Auth | Description |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/Vision/detect/{task}` | none | Single detection passthrough (`person` / `weapon` / `fire`) |
| `GET` | `/api/Vision/download/{videoId}` | none | Download a processed video |

### Auth and account

| Method | Route | Auth | Description |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/Account/register` | Anonymous | Register and send OTP |
| `POST` | `/api/Account/verify-otp` | Anonymous | Verify OTP code |
| `POST` | `/api/Account/resend-otp` | Anonymous | Resend OTP |
| `POST` | `/api/Account/login` | Anonymous | Log in, returns JWT |
| `POST` | `/api/Account/logout` | Bearer | Log out (blacklists the token) |
| `GET` | `/api/Account/refresh-token` | Bearer | Refresh the JWT |
| `POST` | `/api/Account/revoke-token` | Bearer | Revoke a refresh token |
| `POST` | `/api/Account/forgot-password` | Anonymous | Send reset OTP |
| `POST` | `/api/Account/reset-password` | Anonymous | Reset password with OTP |
| `POST` | `/api/Account/change-password` | Bearer | Change own password |
| `GET` | `/api/Account/me` | Bearer | Current user |
| `POST` | `/api/Account/me-by-token` | Bearer | Resolve a user from a token |
| `POST` | `/api/Account/send-delete-otp` | Bearer | OTP before account deletion |
| `DELETE` | `/api/Account/delete-account` | Bearer | Delete by email and OTP |
| `DELETE` | `/api/Account/me` | Bearer | Delete own account |
| `POST` | `/api/Account/admin/add-role` | ADMIN | Assign a role |
| `GET` | `/api/Account/admin/roles` | ADMIN | List roles |
| `DELETE` | `/api/Account/admin/roles/{roleName}` | ADMIN | Delete a role |
| `GET` | `/api/Account/admin/users` | ADMIN | List users |
| `GET` | `/api/Account/users/{id}` | ADMIN | Get a user |
| `PUT` | `/api/Account/users/{id}` | ADMIN | Update a user |
| `DELETE` | `/api/Account/users/{id}` | ADMIN | Delete a user |
| `GET` | `/api/Auth/google-login` | Anonymous | Start Google OAuth |
| `GET` | `/api/Auth/google-callback` | Anonymous | Google OAuth callback |

### Resources

| Controller | Routes | Description |
| :--- | :--- | :--- |
| `AiModel` | `GET` `POST` `PUT` `DELETE` | AI model registry CRUD |
| `Video` | `GET` `POST` `DELETE` | Video records CRUD |
| `Favorite` | `GET` `POST` `DELETE` | Favourite pipelines |

### Response envelope

Every JSON response uses the same wrapper:

```json
{
  "success": true,
  "message": "Pipeline created successfully.",
  "data": {}
}
```

Validation errors return `400` with a descriptive `message`.

---

## How execution works

Both execution modes share the same core. The difference is only *when* the work runs.

### Synchronous - `POST /api/Pipeline/{id}/execute`

Returns `200 OK` with the detections already in `data.results`. Best for images and short clips.

### Asynchronous - `POST /api/Pipeline/{id}/execute-async`

Returns `202 Accepted` with a `runId` and a Hangfire `jobId`, then you poll:

```
GET /api/PipelineRun/{runId}/detail     -> status
GET /api/PipelineRun/{runId}/results    -> detections
GET /api/PipelineRun/{runId}/export     -> Excel
```

### The `tasks` field

A plain string of task indices. Separators `,` `;` `|` and spaces are all accepted, order is preserved and duplicates are collapsed.

```
"0,1,2"     -> Person, Weapon, Fire
"2 0"       -> Fire, Person
"0,0,1"     -> Person, Weapon   (deduplicated)
"9"         -> 400 Task 9 is not supported. Use 0 (person), 1 (weapon) or 2 (fire).
"abc"       -> 400 'abc' is not a valid task. Use 0 (person), 1 (weapon) or 2 (fire).
```

Upload limit is **500 MB** per request.

### Run lifecycle

```
Pending --> Running --> Succeeded
               |
               +----> Failed
```

| Status | Value |
| :--- | :--- |
| `Pending` | 0 |
| `Running` | 1 |
| `Succeeded` | 2 |
| `Failed` | 3 |
| `Cancelled` | 4 |

Each media file gets its own row with its own status, so a single bad file does not fail the whole run.

### Execution context isolation

Run execution resolves its dependencies inside a **dedicated DI scope**, which gives it a fresh `DbContext`.

This matters. The run is created and saved in the caller's scope first, so a shared `DbContext` would then reject the freshly-read entities with:

```
The instance of entity type 'PipelineRun' cannot be tracked because another
instance with the same key value for {'Id'} is already being tracked.
```

The isolated scope makes each execution independent, which is also why the Hangfire job path and the inline path behave identically.

---

## Excel export

Every successful run generates a workbook, stored as a BLOB in `PipelineRunExports` and served from `GET /api/PipelineRun/{id}/export`.

File names look like:

```
pipeline-run-20260930-191812-ef09eb4133f6459046d508df1f2791ea.xlsx
```

### Sheet 1 - Summary

Run information and totals: run ID, pipeline, status, who started it, timings, duration, media count, task count, result count, succeeded and failed counts, average confidence, and total detections.

### Sheet 2 - Results

One row per detection:

| RunId | Pipeline | MediaFile | TaskIndex | Task | ResultType | Detections | Confidence | Status | ProcessedAt | ResultJson |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |

### Sheet 3 - Media

Per-file processing state: file name, size, duration, content type, status, processed time, and any notes (for example a missing file).

---

## Background jobs

Hangfire uses the **same SQL Server database** as the app (`[HangFire]` schema) with **5 workers**.

| Job | Trigger |
| :--- | :--- |
| `EmailJob` | OTP verification, welcome, password reset, delete-account confirmation |
| `PipelineExecutionJob` | `execute-async` requests |

Both use the **default queue**; they are not separated. If you need different retry or throughput behaviour per job type, give each its own queue via `.Enqueue<T>(..., new Job { Queue = "..." })`.

Dashboard: **`/hangfire`**, restricted to loopback requests. If you need remote access, put it behind your own authentication.

> If you deploy multiple instances, give each a distinct `ServerName` in `AddHangfireServer` so servers do not fight over the same queues.

---

## Project layout

```
src/
  VisionAiChrono.Api/              # Controllers, middlewares, jobs, hubs
    BackgroundJobs/                # EmailJob, PipelineExecutionJob
    Controllers/                   # Pipeline, PipelineRun, Account, Vision, ...
    Extensions/                    # DI and Hangfire wiring
    Filters/                       # ValidationFilter
    Hubs/                          # SignalR notification hub
    Middlewares/                   # Exception handling, token blacklist
    App_Data/media/                # Uploaded media (see note above)

  VisionAiChrono.Application/      # Services, DTOs, validators
    Dtos/                          # Request/response contracts
    ServiceContract/               # Interfaces
    Services/                      # Pipeline, PipelineRun, Execution, Excel
    Validators/                    # FluentValidation
    VisionDetection/               # Task mapping and external client models

  VisionAiChrono.Domain/           # Entities, enums, repository contracts

  VisionAiChrono.Infrastructure/   # EF Core, migrations, Redis, repositories
    Migrations/
    Persistence/Configurations/    # Fluent API mappings
    Repositories/
    VisionDetection/               # HTTP client for the AI service

docker-compose.yml
VisionAiChrono.sln
```

---

## Docker

```bash
# API + Redis
docker compose up --build

# API only
docker build -f src/VisionAiChrono.Api/Dockerfile -t visionaichrono .
docker run -p 8080:8080 --env-file .env visionaichrono
```

The image is a **multi-stage build** (`sdk:9.0` then `aspnet:9.0`) and runs as `VisionAiChrono.Api.dll` on port `8080`.

SQL Server and the Python vision service are **not** included in the compose file. Point the API at your own instances.

---

## CI

`.github/workflows/dotnet-ci.yml` runs on every push and PR to `main` and `development`:

1. Checkout
2. Install .NET 9
3. Cache NuGet packages
4. `dotnet restore`
5. `dotnet build --configuration Release`
6. `dotnet test`

> **There is currently no test project in the solution**, so the `dotnet test` step passes trivially without testing anything. Verification so far has been manual end-to-end runs against the API. Add a test project before relying on CI as a safety net.

---

## Security notes

Before deploying this to production, address the following. These are known gaps, not oversights in the design:

- **`appsettings.json` holds live secrets.** It is git-ignored, but move JWT keys, SMTP and DB passwords to environment variables or a secret manager.
- **Rotate credentials that were used during development.** Change the JWT key, SMTP password, Google client secret and the admin password. If this repo was ever public, rewrite history to purge them.
- **`refresh-token` and `revoke-token` are missing `[Authorize]`.** Add it.
- **Role checks are incomplete.** Add `[Authorize(Roles = "...")]` where an endpoint is admin-only instead of relying on convention.
- **`NotificationTestController`, `RedisController` and `VisionController` are weakly protected.** Remove them or lock them down in production.
- **`GET /api/PipelineRun/{id}/*` has no ownership check.** Any authenticated user can read any run by guessing the GUID. Add a `StartedById` comparison.
- **CORS is `AllowAll`.** Restrict it to your real front-end origins.
- **`App_Data` is not git-ignored**, so uploaded media can end up in version control.

---

## Contributing

1. Fork the repository
2. Create a branch off `development`
3. Keep the Clean Architecture boundaries - `Domain` must stay dependency-free
4. Add FluentValidation validators for any new request DTO
5. Add or update migrations when entities change
6. Open a PR against `development`

---

## License

No license file has been added yet. Add one before making the repository public.

---

<div align="center">

**VisionAiChrono** - AI video & image analysis platform

</div>
