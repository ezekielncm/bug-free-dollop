# 🚀 MyApp – Multi-Tier Architecture Template

A production-ready, reusable full-stack template combining:

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **API** | ASP.NET Core 8 (Clean Architecture) | Backend REST API + SignalR hub |
| **Dashboard** | Next.js 14 (TypeScript) | Web admin dashboard |
| **Mobile** | Flutter 3 | iOS & Android app |
| **Infra** | Docker Compose | Local + production orchestration |
| **Monitoring** | Prometheus + Grafana + Jaeger + Seq | Observability stack |
| **CI/CD** | GitHub Actions | Automated build, test, deploy |

---

## 📐 Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                          Client Layer                               │
│   ┌──────────────────┐          ┌──────────────────────────────┐    │
│   │  Next.js Dashboard│          │      Flutter Mobile App      │    │
│   │  (port 3000)      │          │      (iOS / Android)         │    │
│   └────────┬─────────┘          └──────────────┬───────────────┘    │
└────────────┼────────────────────────────────────┼───────────────────┘
             │  REST + SignalR (JWT)               │
┌────────────▼────────────────────────────────────▼───────────────────┐
│                        API Layer (port 5000)                        │
│                   ASP.NET Core 8 – Clean Architecture               │
│   ┌──────────┐  ┌───────────┐  ┌────────────┐  ┌────────────────┐  │
│   │Controllers│  │ SignalR   │  │  Hangfire  │  │  Health Checks │  │
│   │(Auth,Users│  │  Hub     │  │  Dashboard │  │  /health       │  │
│   └────────┬─┘  └─────┬─────┘  └──────┬─────┘  └────────────────┘  │
│            │           │              │                             │
│   ┌────────▼───────────▼──────────────▼─────────────────────────┐  │
│   │                   Application Layer (MediatR)                │  │
│   │   Auth Commands | User Queries | Validation | CQRS           │  │
│   └────────────────────────────┬───────────────────────────────┘  │
│                                │                                    │
│   ┌────────────────────────────▼───────────────────────────────┐  │
│   │                Infrastructure Layer                         │  │
│   │  EF Core | JWT | Redis Cache | RabbitMQ | Hangfire | OTel  │  │
│   └──────────┴──┬──┴─────────────┴─────┬────┴────────┬─────────┘  │
└─────────────────┼──────────────────────┼─────────────┼────────────┘
                  │                      │             │
     ┌────────────▼───┐    ┌─────────────▼──┐  ┌──────▼──────┐
     │  SQL Server /  │    │     Redis      │  │  RabbitMQ   │
     │  PostgreSQL    │    │(Cache+SignalR) │  │  (Messages) │
     └────────────────┘    └────────────────┘  └─────────────┘
```

---

## ✨ Features

### 🔐 Authentication & Authorization
- **JWT Bearer** access tokens (configurable expiry)
- **Refresh token** rotation (7-day sliding window)
- **Role-based access control** (User / Admin / SuperAdmin)
- BCrypt password hashing

### ⚡ Real-Time (SignalR)
- Hub with JWT authentication
- Redis backplane for horizontal scaling
- Group-based notifications
- Flutter + Next.js clients included

### 🗄️ Multi-Provider Database
| Provider | Config value | Notes |
|----------|-------------|-------|
| SQL Server | `SqlServer` (default) | Includes Hangfire support |
| PostgreSQL | `PostgreSQL` | via Npgsql EF Core provider |

Switch by setting `DatabaseProvider` in `appsettings.json` or the `DB_PROVIDER` env var.

### 🔄 Background Jobs (Hangfire)
- Dashboard at `/hangfire`
- Automatic retry on failure
- Provider-aware storage (SQL Server or PostgreSQL)
- Sample `SampleRecurringJob` (extend as needed)

### 📨 Message Broker (RabbitMQ)
- Topic exchange pattern
- Automatic reconnect
- Typed publish/subscribe via `IMessageBroker`

### 📊 Caching (Redis)
- `ICacheService` with `GetOrSetAsync` pattern
- Distributed cache via `IDistributedCache`
- Configurable TTL per cache entry

### 🔭 Observability
| Concern | Tool | Endpoint |
|---------|------|----------|
| Structured logs | Serilog → Seq | http://localhost:5341 |
| Metrics | OpenTelemetry → Prometheus → Grafana | http://localhost:3001 |
| Tracing | OpenTelemetry → Jaeger | http://localhost:16686 |
| Health checks | ASP.NET Health Checks | /health, /health/live |
| Prometheus scrape | Custom endpoint | /metrics |

---

## 🚀 Quick Start

### Prerequisites
- Docker & Docker Compose
- .NET 8 SDK (for local development)
- Node.js 20+ (for dashboard local dev)
- Flutter 3.24+ (for mobile local dev)

### 1. Clone & Configure
```bash
git clone https://github.com/your-org/myapp.git
cd myapp
cp .env.example .env
# Edit .env with your secrets
```

### 2. Start All Services
```bash
docker compose up -d
```

### 3. Access Services
| Service | URL |
|---------|-----|
| API (Swagger) | http://localhost:5000/swagger |
| Dashboard | http://localhost:3000 |
| Hangfire | http://localhost:5000/hangfire |
| RabbitMQ UI | http://localhost:15672 |
| Seq Logs | http://localhost:5341 |
| Jaeger Tracing | http://localhost:16686 |
| Grafana | http://localhost:3001 |
| Prometheus | http://localhost:9090 |

---

## 🐳 Docker Compose Profiles

```bash
# Default (SQL Server)
docker compose up -d

# With PostgreSQL instead of SQL Server
DB_PROVIDER=PostgreSQL docker compose --profile postgres up -d

# Production (no dev overrides)
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

---

## ��️ Project Structure

```
.
├── src/
│   ├── api/                          # ASP.NET Core solution
│   │   ├── MyApp.Domain/             # Entities, interfaces, domain events
│   │   ├── MyApp.Application/        # CQRS, MediatR, validators
│   │   ├── MyApp.Infrastructure/     # EF Core, JWT, Redis, RabbitMQ, Hangfire, OTel
│   │   └── MyApp.API/                # Controllers, SignalR Hub, Middleware
│   ├── dashboard/                    # Next.js 14 app
│   │   ├── app/                      # App Router pages
│   │   ├── hooks/                    # useSignalR, custom hooks
│   │   ├── lib/                      # API client, services, Zustand store
│   │   └── types/                    # TypeScript types
│   └── mobile/                       # Flutter app
│       └── lib/
│           ├── core/                 # Network, storage, constants
│           └── features/             # Auth, Dashboard, Notifications (BLoC)
├── docker/
│   ├── api/Dockerfile
│   └── dashboard/Dockerfile
├── monitoring/
│   ├── prometheus/prometheus.yml
│   ├── grafana/                      # Datasources + dashboards
│   └── otel/otel-collector-config.yml
├── .github/workflows/
│   ├── ci.yml                        # Build + test on PR
│   └── cd.yml                        # Build, push, deploy on main/tag
├── docker-compose.yml
├── docker-compose.override.yml       # Dev overrides
├── docker-compose.prod.yml           # Production overrides
└── .env.example
```

---

## 🔧 Local Development

### API
```bash
cd src/api
dotnet restore MyApp.sln
dotnet run --project MyApp.API
```

### Dashboard
```bash
cd src/dashboard
npm install
cp .env.example .env.local
npm run dev
```

### Mobile
```bash
cd src/mobile
flutter pub get
flutter run
```

---

## 🔑 Environment Variables

Copy `.env.example` to `.env` and fill in the values:

| Variable | Description | Default |
|----------|-------------|---------|
| `DB_PROVIDER` | `SqlServer` or `PostgreSQL` | `SqlServer` |
| `DB_PASSWORD` | SQL Server SA password | `YourStrong@Passw0rd` |
| `POSTGRES_PASSWORD` | PostgreSQL password | `myapp_password` |
| `JWT_SECRET_KEY` | JWT signing key (≥32 chars) | dev value |
| `RABBITMQ_PASSWORD` | RabbitMQ password | `rabbitmq` |
| `REDIS_PASSWORD` | Redis password (empty = no auth) | empty |
| `GRAFANA_PASSWORD` | Grafana admin password | `admin` |

---

## 📦 Adding a New Feature

1. **Domain** – Add entity/event in `MyApp.Domain`
2. **Application** – Add CQRS command/query in `MyApp.Application/Features/<Feature>/`
3. **Infrastructure** – Add repository/service implementation
4. **API** – Add controller endpoint
5. **Dashboard** – Add page + API service call
6. **Mobile** – Add BLoC + screen

---

## 🤝 License

MIT
