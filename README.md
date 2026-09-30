# VisionAiChrono

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?logo=microsoftsqlserver&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-DC382D?logo=redis&logoColor=white)
![SignalR](https://img.shields.io/badge/SignalR-Real--time-512BD4)
![Hangfire](https://img.shields.io/badge/Hangfire-Background%20Jobs-2C3E50)

**VisionAiChrono** is an ASP.NET Core backend for running **AI video/image detection pipelines**. Users upload images or videos, choose what to detect (**person**, **weapon**, **fire**), and the API forwards the media to a Python vision-detection service, stores every result, and produces an Excel report for each run.

Jobs can run synchronously or in the background (Hangfire), and users get real-time notifications through SignalR.

---

## Table of Contents

- [How It Works](#how-it-works)
- [Features](#features)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [API Reference](#api-reference)
- [Data Model](#data-model)
- [Project Structure](#project-structure)
- [CI](#ci)

---

## How It Works

```
Client ──► VisionAiChrono API ──► Python Vision Service
              │    │                   POST /detect/{person|weapon|fire}
              │    │                   GET  /download/{videoId}
              │    ├──► SQL Server  (users, pipelines, runs, results, Excel exports)
              │    ├──► Redis       (distributed cache)
              │    ├──► Hangfire    (background execution + email jobs)
              │    └──► SignalR     (real-time notifications)
              └──► App_Data/media   (uploaded files on disk)
```

**A pipeline run, step by step:**

1. A user creates a **Pipeline** (a named, reusable workflow).
2. They call `POST /api/Pipeline/{id}/execute` (or `/execute-async`) with media files and a `tasks` field, for example `0,1,2`.
   Task indexes: **0 = person**, **1 = weapon**, **2 = fire**.
3. The API validates the task list (`TaskIndexParser` accepts `,` space `;` `|` separators and removes duplicates), saves each file under `App_Data/media/yyyy/MM/`, and creates a `PipelineRun` with its videos and tasks.
4. `PipelineExecutionService` runs the run in its **own DI scope** (its own DbContext). For every file and every selected task it streams the file to the Python service (`POST /detect/{task}`) and stores the JSON response as a `PipelineResult`, including the result type (image or video), detection count and average confidence.
5. A failure on one file/task does **not** stop the run. It is recorded as a failed result and the run continues.
6. When finished, the run is marked `Succeeded` (or `Failed`), an **Excel report** is generated with ClosedXML and stored in the database, and a summary is returned: total executions, succeeded/failed, total detections and average confidence.

For videos, the Python service returns a `videoId` and `downloadUrl`. The API can proxy the annotated video back through `GET /api/Vision/download/{videoId}`.

---

## Features

**Detection pipelines**
- Person, weapon and fire detection on images **and** videos
- Multiple files and multiple tasks per run
- Synchronous execution (`execute`) or background execution (`execute-async`, returns `202 Accepted`)
- Run history, per-run detail, raw results and Excel export download
- Run statuses: `Pending`, `Running`, `Succeeded`, `Failed`, `Cancelled`
- Favorite pipelines per user
- CRUD for AI models and video/media records

**Authentication & users**
- Email/password registration with a **6-digit OTP** (valid 5 minutes, resend throttled)
- JWT access tokens + refresh tokens (10 day lifetime), refresh and revoke endpoints
- **Logout blacklists the access token** (checked by `TokenBlacklistMiddleware`)
- Forgot/reset password via OTP, change password
- Account deletion by OTP or for the current user
- **Google sign-in** (OAuth)
- Roles `USER` and `ADMIN`, with admin endpoints for users and roles
- Admin user and roles are **seeded on startup** from configuration

**Infrastructure**
- Hangfire (SQL Server storage, 5 workers) for pipeline runs and emails; dashboard at `/hangfire` (protected by an authorization filter)
- Emails sent through MailKit/SMTP and queued as Hangfire jobs
- SignalR hub at `/hubs/notifications` (JWT can be passed as `?access_token=`)
- Redis distributed cache with demo/health endpoints
- Global exception-handling middleware mapping custom exceptions (NotFound, Conflict, Validation, Unauthorized, Forbidden, BadRequest, ExternalService) to proper HTTP responses
- FluentValidation on request DTOs, with a validation filter
- Swagger UI with JWT support at `/swagger`
- Uploads: up to **500 MB** per request on pipeline endpoints; default per-file limit 200 MB (configurable)

---

## Architecture

The solution follows **Clean Architecture**:

| Project | Responsibility |
| --- | --- |
| `VisionAiChrono.Domain` | Entities, enums, repository contracts (`IGenericRepository`, `IUnitOfWork`), `Result` |
| `VisionAiChrono.Application` | Services, DTOs, validators, exceptions, service contracts, detection models |
| `VisionAiChrono.Infrastructure` | EF Core `AppDbContext`, configurations, migrations, repositories, seeding |
| `VisionAiChrono.Api` | Controllers, middlewares, filters, SignalR hub, Hangfire jobs, DI setup, static demo pages |

---

## Tech Stack

- **.NET 9**, ASP.NET Core Web API
- **EF Core 9** + **SQL Server**, ASP.NET Core Identity
- **JWT Bearer** + **Google OAuth**
- **Hangfire** (SQL Server storage)
- **SignalR**
- **Redis** (`Microsoft.Extensions.Caching.StackExchangeRedis`)
- **FluentValidation**, **ClosedXML**, **MailKit**, **Swashbuckle**
- Python vision-detection service (separate, exposes `/detect/*`)

---

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB or full)
- A Redis instance
- The Python vision-detection service (exposes `/detect/{task}` and `/download/{videoId}`)

### Run

```bash
git clone https://github.com/OsamaMousa11/VisionAiChrono.git
cd VisionAiChrono

# 1. Create src/VisionAiChrono.Api/appsettings.json (see Configuration below)

# 2. Apply the database migrations
dotnet ef database update --project src/VisionAiChrono.Infrastructure --startup-project src/VisionAiChrono.Api

# 3. Run the API
dotnet run --project src/VisionAiChrono.Api
```

The API listens on **port 8080**:

| Path | What |
| --- | --- |
| `/swagger` | Swagger UI |
| `/hangfire` | Hangfire dashboard |
| `/hubs/notifications` | SignalR hub |

On first start the app seeds the roles and the admin account defined in `AdminUser`.

---

## Configuration

`appsettings.json` is git-ignored, so create it yourself at `src/VisionAiChrono.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "connstr": "Server=.;Database=VisionAiChrono;Trusted_Connection=True;TrustServerCertificate=True",
    "Redis": "your-redis-host:6379"
  },
  "JWT": {
    "Key": "a-long-random-secret-key",
    "Issuer": "VisionAiChrono",
    "Audience": "VisionAiChronoUsers",
    "DurationInDays": 1
  },
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret",
      "CallbackPath": "/signin-google"
    }
  },
  "MailSettings": {
    "Email": "you@example.com",
    "DisplayName": "VisionAiChrono",
    "Password": "app-password",
    "Host": "smtp.gmail.com",
    "Port": 587,
    "SenderEmail": "you@example.com"
  },
  "AdminUser": {
    "Email": "admin@example.com",
    "Password": "change-me",
    "Name": "Administrator"
  },
  "AI": {
    "BaseUrl": "http://your-vision-service:8000"
  },
  "MediaStorage": {
    "RootPath": "App_Data/media",
    "MaxFileSizeBytes": 209715200
  }
}
```

Notes:
- `connstr`, `Authentication:Google:*` and `AI:BaseUrl` are **required**. The app throws at startup if any is missing.
- `MediaStorage` values are optional.
- Never commit real secrets.

---

## API Reference

All routes are under `/api`. 🔒 = requires a JWT. 👑 = requires the `ADMIN` role.

### Account (`/api/Account`)

| Method | Route | Description |
| --- | --- | --- |
| POST | `/register` | Register a new user (sends OTP email) |
| POST | `/verify-otp` | Verify the 6-digit OTP |
| POST | `/resend-otp` | Resend the OTP |
| POST | `/login` | Returns JWT and refresh token |
| POST | `/logout` 🔒 | Blacklists the access token, invalidates refresh token |
| GET | `/refresh-token` | Get a new access token |
| POST | `/revoke-token` | Revoke a refresh token |
| POST | `/forgot-password` | Send a reset OTP |
| POST | `/reset-password` | Reset password with OTP |
| POST | `/change-password` 🔒 | Change password |
| GET | `/me` 🔒 | Current user info |
| POST | `/me-by-token` | Resolve a user from a token |
| DELETE | `/me` 🔒 | Delete the current account |
| POST | `/send-delete-otp` | Send OTP to confirm deletion |
| DELETE | `/delete-account` | Delete an account by email + OTP |
| POST | `/admin/add-role` 👑 | Add a role to a user |
| GET | `/admin/roles` 👑 | List roles |
| DELETE | `/admin/roles/{roleName}` 👑 | Delete a role |
| GET | `/admin/users` 👑 | List users |
| GET / PUT / DELETE | `/users/{id}` 👑 | Get, update or delete a user |

### Google Auth (`/api/Auth`)

| Method | Route | Description |
| --- | --- | --- |
| GET | `/google` | Start Google sign-in |
| GET | `/google-callback` | OAuth callback |

### Pipelines (`/api/Pipeline`) 🔒

| Method | Route | Description |
| --- | --- | --- |
| POST | `/` | Create a pipeline (`name`, `description`) |
| GET | `/` | List pipelines |
| GET | `/{id}` | Get a pipeline |
| POST | `/{id}/execute` | Run now. `multipart/form-data`: `tasks` (e.g. `0,1,2`) + `files` |
| POST | `/{id}/execute-async` | Queue the run in Hangfire (returns 202) |
| GET | `/{pipelineId}/runs` | Run history (paged) |

### Pipeline Runs (`/api/PipelineRun`) 🔒

| Method | Route | Description |
| --- | --- | --- |
| GET | `/{id}/detail` | Run detail (status, media, tasks) |
| GET | `/{id}/results` | Detection results |
| GET | `/{id}/export` | Download the Excel report |

### Other resources 🔒

| Controller | Routes |
| --- | --- |
| `/api/AiModel` | `POST`, `GET`, `GET /{id}`, `PUT /{id}`, `DELETE /{id}` |
| `/api/Video` | `POST`, `GET`, `GET /{id}`, `DELETE /{id}` |
| `/api/Favorite` | `POST`, `GET`, `GET /check/{pipelineId}`, `DELETE /{pipelineId}` |

### Direct vision detection (`/api/Vision`)

| Method | Route | Description |
| --- | --- | --- |
| POST | `/detect/{task}` | One-off detection (`person`, `weapon` or `fire`) on one uploaded image/video (max 50 MB) |
| GET | `/download/{videoId}` | Download the annotated video via the backend |

### Utilities

| Controller | Routes |
| --- | --- |
| `/api/Redis` | `GET /demo`, `GET /health`, `GET /user/{userId}`, `POST /set`, `GET /get`, `DELETE /cache/{key}` |
| `/api/NotificationTest` | `POST /broadcast`, `POST /test`, `GET /stats` |

### Example: run a pipeline

```bash
curl -X POST "http://<host>:8080/api/Pipeline/<pipeline-id>/execute" \
  -H "Authorization: Bearer <token>" \
  -F "tasks=0,2" \
  -F "files=@street.mp4" \
  -F "files=@photo.jpg"
```

Response summary includes `status`, `totalExecutions`, `succeeded`, `failed`, `totalDetections`, `averageConfidence`, `exportFileName` and per-file `results`.

---

## Data Model

| Entity | Purpose |
| --- | --- |
| `ApplicationUser` / `ApplicationRole` | Identity users and roles |
| `RefreshToken`, `EmailOtp` | Refresh tokens and OTP codes |
| `Pipeline` | Named, reusable detection workflow |
| `AiModel`, `PipelineModel` | Models and their link to a pipeline |
| `PipelineRun` | One execution: who, when, status |
| `PipelineRunModel` | The tasks selected for the run, with order |
| `Video` / `PipelineRunVideo` | Uploaded media and its per-run processing status |
| `PipelineResult` | Result per media per task: JSON, type, confidence, status |
| `PipelineRunExport` | Excel report stored with the run |
| `Favorite` | A user's favorite pipelines |

---

## Project Structure

```
VisionAiChrono/
├── .github/workflows/dotnet-ci.yml
├── src/
│   ├── VisionAiChrono.Api/            # Controllers, Hubs, Middlewares, BackgroundJobs, wwwroot demo pages
│   ├── VisionAiChrono.Application/    # Services, DTOs, Validators, Exceptions, VisionDetection
│   ├── VisionAiChrono.Domain/         # Entities, Enums, Contracts
│   └── VisionAiChrono.Infrastructure/ # DbContext, Migrations, Repositories, Seeding
├── docker-compose.yml
└── VisionAiChrono.sln
```

The `wwwroot` folder contains small demo pages (dashboard, Google login test, real-time and pagination demos) for trying the API.

---

## CI

GitHub Actions builds the solution with .NET 9 and runs the tests on every push and pull request to `main` and `development`.
