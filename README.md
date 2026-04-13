# 🚀 MyApp — Full-Stack Multi-Tier Architecture Template

[![CI](https://github.com/ezekielncm/bug-free-dollop/actions/workflows/ci.yml/badge.svg)](https://github.com/ezekielncm/bug-free-dollop/actions/workflows/ci.yml)
[![CD](https://github.com/ezekielncm/bug-free-dollop/actions/workflows/cd.yml/badge.svg)](https://github.com/ezekielncm/bug-free-dollop/actions/workflows/cd.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A **production-ready**, greenfield template for bootstrapping projects with a modern multi-tier architecture. Clone it, configure it, and start building — authentication, real-time messaging, background jobs, observability, CI/CD, and containerized deployment are already wired up.

| Layer | Technology | Description |
|-------|-----------|-------------|
| **API** | ASP.NET Core 8 — Clean Architecture | REST API, SignalR hub, CQRS/MediatR, Hangfire |
| **Dashboard** | Next.js 14 — TypeScript, App Router | Admin web dashboard with Zustand & SignalR |
| **Mobile** | Flutter 3 — BLoC | iOS & Android app with Dio & SignalR |
| **Infrastructure** | Docker Compose | Full dev & production orchestration |
| **Monitoring** | Prometheus · Grafana · Jaeger · Seq | Complete observability stack |
| **CI/CD** | GitHub Actions | Automated build, test, and deploy to GHCR |

---

## 📐 Architecture Overview

```
┌──────────────────────────────────────────────────────────────────────────┐
│                             Clients                                      │
│    ┌─────────────────────┐               ┌─────────────────────────┐     │
│    │  Next.js Dashboard  │               │   Flutter Mobile App    │     │
│    │  (port 3000)        │               │   (iOS / Android)       │     │
│    └─────────┬───────────┘               └────────────┬────────────┘     │
└──────────────┼────────────────────────────────────────┼──────────────────┘
               │  REST + SignalR (JWT Bearer)            │
┌──────────────▼────────────────────────────────────────▼──────────────────┐
│                       API Layer (port 5000)                               │
│                  ASP.NET Core 8 — Clean Architecture                      │
│                                                                           │
│  ┌────────────┐  ┌──────────┐  ┌───────────┐  ┌───────────────────────┐  │
│  │ Controllers │  │ SignalR  │  │ Hangfire  │  │ Health · Metrics ·    │  │
│  │ Auth, Users │  │ Hub     │  │ Dashboard │  │ Swagger               │  │
│  └──────┬──────┘  └────┬────┘  └─────┬─────┘  └───────────────────────┘  │
│         └───────────────┼────────────┘                                    │
│  ┌──────────────────────▼──────────────────────────────────────────────┐  │
│  │              Application Layer (MediatR CQRS)                       │  │
│  │  Commands · Queries · Validators (FluentValidation) · Behaviors     │  │
│  └──────────────────────┬──────────────────────────────────────────────┘  │
│  ┌──────────────────────▼──────────────────────────────────────────────┐  │
│  │                     Domain Layer                                     │  │
│  │  Entities · Value Objects · Domain Events · Repository Interfaces    │  │
│  └──────────────────────┬──────────────────────────────────────────────┘  │
│  ┌──────────────────────▼──────────────────────────────────────────────┐  │
│  │                   Infrastructure Layer                               │  │
│  │  EF Core · JWT · BCrypt · Redis · RabbitMQ · Hangfire · OTel        │  │
│  └──────┬───────────────┬────────────────────┬─────────────────────────┘  │
└─────────┼───────────────┼────────────────────┼───────────────────────────┘
          │               │                    │
  ┌───────▼────────┐ ┌───▼──────────┐  ┌──────▼───────┐
  │  SQL Server /  │ │    Redis     │  │  RabbitMQ    │
  │  PostgreSQL    │ │ Cache+SignalR │  │  Messages    │
  └────────────────┘ └──────────────┘  └──────────────┘
```

> 📖 See [docs/architecture.md](docs/architecture.md) for detailed design decisions and layer responsibilities.

---

## ✨ Key Features

### 🔐 Authentication & Authorization
- **JWT access tokens** with configurable expiry (default 60 min)
- **Refresh token rotation** with 7-day sliding window
- **Role-based access control** — `User`, `Admin`, `SuperAdmin`
- **BCrypt** password hashing with automatic salt
- SignalR hub authenticated via JWT query string parameter

### ⚡ Real-Time Communication (SignalR)
- `/hubs/notifications` hub with `[Authorize]`
- **Redis backplane** for horizontal scale-out
- Group-based fan-out (`JoinGroup`, `LeaveGroup`, `SendToGroup`)
- Auto-created user groups (`user:{userId}`) for targeted notifications
- Client implementations in **Next.js** (`useSignalR` hook) and **Flutter** (`signalr_netcore`)

### 🗄️ Multi-Provider Database
| Provider | `DatabaseProvider` value | Notes |
|----------|------------------------|-------|
| SQL Server | `SqlServer` (default) | Full support including Hangfire storage |
| PostgreSQL | `PostgreSQL` | Via Npgsql; activate with `--profile postgres` |

Switch at runtime with the `DB_PROVIDER` environment variable — both EF Core and Hangfire storage auto-configure.

### 🔄 Background Jobs (Hangfire)
- Admin dashboard at `/hangfire` (restricted to `Admin` / `SuperAdmin`)
- `SampleRecurringJob` with `[AutomaticRetry(Attempts = 3)]`
- Provider-aware storage (SQL Server or PostgreSQL)

### 📨 Message Broker (RabbitMQ)
- Topic exchange with durable queues
- Typed `IMessageBroker.PublishAsync<T>` / `SubscribeAsync<T>`
- Persistent delivery, automatic connection recovery

### 📊 Caching (Redis)
- `ICacheService` with `GetOrSetAsync` cache-aside pattern
- Configurable TTL per entry
- JSON serialization with camelCase policy

### 🔭 Observability
| Concern | Tool | Access |
|---------|------|--------|
| Structured logs | Serilog → **Seq** | http://localhost:5341 |
| Metrics | OpenTelemetry → **Prometheus** → **Grafana** | http://localhost:3001 |
| Tracing | OpenTelemetry → **Jaeger** | http://localhost:16686 |
| Health checks | ASP.NET Health Checks | `/health` · `/health/live` |
| Prometheus scrape | Built-in exporter | `/metrics` |

> 📖 See [docs/monitoring.md](docs/monitoring.md) for the full observability setup guide.

---

## 🏗️ Project Structure

```
.
├── src/
│   ├── api/                            # ASP.NET Core 8 solution (Clean Architecture)
│   │   ├── MyApp.Domain/               #   Entities, enums, events, repository interfaces
│   │   ├── MyApp.Application/          #   CQRS commands/queries, validators, DTOs, behaviors
│   │   ├── MyApp.Infrastructure/       #   EF Core, JWT, Redis, RabbitMQ, Hangfire, OTel
│   │   ├── MyApp.API/                  #   Controllers, SignalR Hub, middleware, filters
│   │   └── MyApp.sln
│   ├── dashboard/                      # Next.js 14 (TypeScript, App Router)
│   │   ├── app/                        #   Pages: auth/login, auth/register, dashboard/*
│   │   ├── hooks/                      #   useSignalR real-time hook
│   │   ├── lib/                        #   Axios client, Zustand store, API services
│   │   └── types/                      #   TypeScript interfaces
│   └── mobile/                         # Flutter 3 (BLoC, GoRouter)
│       └── lib/
│           ├── core/                   #   ApiClient, SecureStorage, UserModel, constants
│           └── features/               #   auth/, dashboard/, notifications/
├── docker/
│   ├── api/Dockerfile                  # Multi-stage .NET 8 build
│   └── dashboard/Dockerfile            # Multi-stage Node 20 build
├── monitoring/
│   ├── prometheus/prometheus.yml        # Scrape config
│   ├── grafana/                         # Auto-provisioned datasources & dashboards
│   └── otel/otel-collector-config.yml   # OTLP → Jaeger + Prometheus pipelines
├── docs/                                # Extended documentation
│   ├── architecture.md                  # Architecture & design decisions
│   ├── api-reference.md                 # API endpoints reference
│   ├── deployment.md                    # Docker, CI/CD & production deployment
│   ├── monitoring.md                    # Observability & monitoring guide
│   └── contributing.md                  # Contributing guidelines
├── .github/workflows/
│   ├── ci.yml                           # Build + test on every PR
│   └── cd.yml                           # Build, push GHCR, deploy on main/tags
├── docker-compose.yml                   # Full dev stack (14 services)
├── docker-compose.override.yml          # Dev hot-reload overrides
├── docker-compose.prod.yml              # Production resource limits & replicas
└── .env.example                         # Environment variable template
```

Each sub-project has its own detailed README:
- **[src/api/README.md](src/api/README.md)** — API architecture, endpoints, configuration
- **[src/dashboard/README.md](src/dashboard/README.md)** — Dashboard setup, pages, state management
- **[src/mobile/README.md](src/mobile/README.md)** — Mobile app setup, BLoC, navigation

---

## 🚀 Quick Start

### Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| Docker & Docker Compose | Latest | Run all services |
| .NET SDK | 8.0+ | API local development |
| Node.js | 20+ | Dashboard local development |
| Flutter | 3.24+ | Mobile local development |

### 1. Clone & Configure

```bash
git clone https://github.com/ezekielncm/bug-free-dollop.git
cd bug-free-dollop
cp .env.example .env
```

Edit `.env` with secure values for production — the defaults work for local development.

### 2. Start All Services

```bash
# Default stack with SQL Server
docker compose up -d

# Or with PostgreSQL
DB_PROVIDER=PostgreSQL docker compose --profile postgres up -d
```

### 3. Access Services

| Service | URL | Credentials |
|---------|-----|-------------|
| **API** (Swagger) | http://localhost:5000/swagger | — |
| **Dashboard** | http://localhost:3000 | Register a new account |
| **Hangfire** | http://localhost:5000/hangfire | Requires Admin role |
| **RabbitMQ** Management | http://localhost:15672 | `rabbitmq` / `rabbitmq` |
| **Seq** (Logs) | http://localhost:5341 | — |
| **Jaeger** (Traces) | http://localhost:16686 | — |
| **Grafana** (Dashboards) | http://localhost:3001 | `admin` / from `.env` |
| **Prometheus** | http://localhost:9090 | — |

### 4. Register Your First User

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "email": "admin@example.com",
    "password": "SecurePass123!",
    "firstName": "Admin",
    "lastName": "User"
  }'
```

---

## 🐳 Docker Compose Profiles

```bash
# Development (default) — SQL Server + hot-reload
docker compose up -d

# Development with PostgreSQL
DB_PROVIDER=PostgreSQL docker compose --profile postgres up -d

# Production mode (pre-built images, replicas, resource limits)
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Rebuild a specific service
docker compose build api
docker compose up -d api
```

> 📖 See [docs/deployment.md](docs/deployment.md) for full deployment and CI/CD documentation.

---

## 🔧 Local Development (Without Docker)

### API

```bash
cd src/api
dotnet restore MyApp.sln
dotnet run --project MyApp.API
# → http://localhost:5000/swagger
```

> Requires external services (SQL Server/PostgreSQL, Redis, RabbitMQ) running locally or via Docker.

### Dashboard

```bash
cd src/dashboard
npm install
cp .env.example .env.local
npm run dev
# → http://localhost:3000
```

### Mobile

```bash
cd src/mobile
flutter pub get
flutter run
# Android emulator connects to API via 10.0.2.2:5000
```

---

## 🔑 Environment Variables

Copy `.env.example` → `.env` and configure:

| Variable | Description | Default |
|----------|-------------|---------|
| `DB_PROVIDER` | Database engine: `SqlServer` or `PostgreSQL` | `SqlServer` |
| `DB_PASSWORD` | SQL Server SA password | `YourStrong@Passw0rd` |
| `DB_CONNECTION_STRING` | Full connection string (overrides defaults) | see `.env.example` |
| `POSTGRES_PASSWORD` | PostgreSQL password | `myapp_password` |
| `JWT_SECRET_KEY` | JWT HMAC-SHA256 signing key (≥ 32 characters) | dev value |
| `RABBITMQ_PASSWORD` | RabbitMQ password | `rabbitmq` |
| `REDIS_PASSWORD` | Redis password (empty = no auth) | *(empty)* |
| `GRAFANA_PASSWORD` | Grafana admin password | `admin` |
| `DOCKER_REGISTRY` | Container registry for CD pipeline | `ghcr.io/your-org` |
| `IMAGE_TAG` | Image tag for production deployment | `latest` |
| `API_URL` | Public API URL for dashboard (production) | `https://api.yourdomain.com` |

> ⚠️ **Security**: Always change default passwords before deploying to any shared or production environment.

---

## 📦 Adding a New Feature

Follow the Clean Architecture flow from inside out:

| Step | Layer | Action |
|------|-------|--------|
| 1 | **Domain** | Add entity, enum, or domain event in `MyApp.Domain` |
| 2 | **Application** | Add command/query + validator in `MyApp.Application/Features/<Feature>/` |
| 3 | **Infrastructure** | Add repository or service implementation in `MyApp.Infrastructure` |
| 4 | **API** | Add controller endpoint in `MyApp.API/Controllers/` |
| 5 | **Dashboard** | Add page in `app/dashboard/<feature>/` + API service in `lib/services.ts` |
| 6 | **Mobile** | Add BLoC + screen in `lib/features/<feature>/` |

> 📖 See [docs/architecture.md](docs/architecture.md) for detailed layer responsibilities and dependency rules.

---

## 🧪 Testing

```bash
# API — .NET unit & integration tests
cd src/api && dotnet test MyApp.sln --verbosity normal

# Dashboard — type checking & linting
cd src/dashboard && npm run type-check && npm run lint

# Mobile — static analysis & unit tests
cd src/mobile && flutter analyze && flutter test
```

CI runs all of these automatically on every pull request. See [docs/deployment.md](docs/deployment.md) for CI/CD details.

---

## 📖 Documentation

| Document | Description |
|----------|-------------|
| **[src/api/README.md](src/api/README.md)** | API — Clean Architecture, endpoints, domain model, configuration |
| **[src/dashboard/README.md](src/dashboard/README.md)** | Dashboard — Setup, pages, Zustand store, SignalR hook, Axios interceptors |
| **[src/mobile/README.md](src/mobile/README.md)** | Mobile — Flutter setup, BLoC pattern, Dio client, GoRouter navigation |
| **[docs/architecture.md](docs/architecture.md)** | Architecture decisions, layer responsibilities, dependency rules |
| **[docs/api-reference.md](docs/api-reference.md)** | Complete API endpoints reference with request/response schemas |
| **[docs/deployment.md](docs/deployment.md)** | Docker, CI/CD pipelines, production deployment guide |
| **[docs/monitoring.md](docs/monitoring.md)** | Observability stack — Prometheus, Grafana, Jaeger, Seq, OTEL Collector |
| **[docs/contributing.md](docs/contributing.md)** | Contributing guidelines, code style, PR process |

---

## 🤝 Contributing

Contributions are welcome! Please read [docs/contributing.md](docs/contributing.md) for guidelines on:
- Branch naming and commit conventions
- Code style and linting
- Pull request process
- Development environment setup

---

## 📄 License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
