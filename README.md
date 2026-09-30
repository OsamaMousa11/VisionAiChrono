# VisionAiChrono

![.NET](https://img.shields.io/badge/.NET-512BD4?logo=dotnet&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-DC382D?logo=redis&logoColor=white)
![CI](https://img.shields.io/badge/CI-GitHub%20Actions-2088FF?logo=githubactions&logoColor=white)

> A .NET Web API for **[one-line description of what the project does — e.g. AI-powered vision analysis with time/chronology tracking]**, containerized with Docker and backed by Redis caching.

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Run with Docker Compose](#run-with-docker-compose)
  - [Run Locally](#run-locally)
- [Configuration](#configuration)
- [API Usage](#api-usage)
- [CI/CD](#cicd)
- [Contributing](#contributing)
- [License](#license)

---

## Overview

VisionAiChrono is a backend service built with **.NET** that exposes a REST API (`VisionAiChrono.Api`) and uses **Redis** as a caching layer. The whole stack can be started with a single `docker compose up` command.

<!-- TODO: Add 2–3 sentences about the problem the project solves and who it's for. -->

## Features

- RESTful API built with ASP.NET Core
- Redis caching for fast responses
- Fully containerized (API + Redis) with Docker Compose
- Continuous integration with GitHub Actions
- <!-- TODO: add your real features here -->

## Tech Stack

| Layer                | Technology                |
| -------------------- | ------------------------- |
| Language / Framework | C# / ASP.NET Core (.NET)  |
| Caching              | Redis (`redis:alpine`)    |
| Containerization     | Docker, Docker Compose    |
| CI                   | GitHub Actions            |

## Project Structure

```
VisionAiChrono/
├── .github/workflows/        # GitHub Actions workflows
├── src/
│   └── VisionAiChrono.Api/   # ASP.NET Core Web API (+ Dockerfile)
├── .dockerignore
├── .gitignore
├── docker-compose.yml        # API + Redis services
├── dotnet-ci.yml             # .NET CI pipeline definition
└── VisionAiChrono.sln        # Visual Studio solution
```

## Getting Started

### Prerequisites

- [Docker](https://www.docker.com/get-started) and Docker Compose
- [.NET SDK](https://dotnet.microsoft.com/download) (only if running without Docker)

### Run with Docker Compose

```bash
# 1. Clone the repository
git clone https://github.com/OsamaMousa11/VisionAiChrono.git
cd VisionAiChrono

# 2. Build and start the services
docker compose up --build
```

Once running:

| Service | URL / Port            |
| ------- | --------------------- |
| API     | http://localhost:5000 |
| Redis   | localhost:6379        |

To stop everything:

```bash
docker compose down
```

### Run Locally

Make sure a Redis instance is running on `localhost:6379`, then:

```bash
dotnet restore
dotnet run --project src/VisionAiChrono.Api
```

## Configuration

The API reads its settings from environment variables / `appsettings.json`.

| Variable                   | Description             | Default (Docker)   |
| -------------------------- | ----------------------- | ------------------ |
| `ConnectionStrings__Redis` | Redis connection string | `cache-redis:6379` |

<!-- TODO: Document any other settings (API keys, model endpoints, etc.). -->

## API Usage

<!-- TODO: Replace with your real endpoints. -->

```http
GET http://localhost:5000/api/<your-endpoint>
```

If Swagger is enabled, the interactive docs will be available at `http://localhost:5000/swagger`.

## CI/CD

The repository includes a GitHub Actions workflow that builds and tests the solution on every push and pull request. See [`.github/workflows`](.github/workflows) for details.

## Contributing

Contributions are welcome!

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Commit your changes: `git commit -m "Add my feature"`
4. Push the branch: `git push origin feature/my-feature`
5. Open a Pull Request

## License

<!-- TODO: Choose a license (e.g. MIT) and add a LICENSE file. -->
This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

Made by [Osama Mousa](https://github.com/OsamaMousa11)
