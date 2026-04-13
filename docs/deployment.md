# 🐳 Deployment Guide

This guide covers Docker Compose deployment, CI/CD pipelines, and production deployment strategies for MyApp.

---

## Docker Compose

### Development Stack

The default `docker-compose.yml` + `docker-compose.override.yml` provides a full development environment:

```bash
# Start all services
docker compose up -d

# View logs
docker compose logs -f api

# Stop all services
docker compose down

# Stop and remove volumes (reset data)
docker compose down -v
```

### Services Overview

| Service | Image | Port(s) | Description |
|---------|-------|---------|-------------|
| `api` | Custom build | `5000:8080` | ASP.NET Core API |
| `dashboard` | Custom build | `3000:3000` | Next.js dashboard |
| `db` | `mcr.microsoft.com/mssql/server:2022-latest` | `1433:1433` | SQL Server |
| `postgres` | `postgres:16-alpine` (profile) | `5432:5432` | PostgreSQL (optional) |
| `redis` | `redis:7-alpine` | `6379:6379` | Cache + SignalR backplane |
| `rabbitmq` | `rabbitmq:3-management-alpine` | `5672, 15672` | Message broker |
| `seq` | `datalust/seq:latest` | `5341:80` | Log aggregation |
| `otel-collector` | `otel/opentelemetry-collector-contrib` | `4317, 4318` | Telemetry collector |
| `jaeger` | `jaegertracing/all-in-one` | `16686` | Distributed tracing |
| `prometheus` | `prom/prometheus` | `9090:9090` | Metrics storage |
| `grafana` | `grafana/grafana` | `3001:3000` | Metrics dashboards |

### Development Overrides (`docker-compose.override.yml`)

The override file adds development conveniences:

- **API**: Source code mounted as volume for hot-reload via `dotnet watch`
- **Dashboard**: Source code mounted with `npm run dev` for hot module replacement
- **Database ports**: Exposed for direct local access

### Database Provider Selection

```bash
# SQL Server (default)
docker compose up -d

# PostgreSQL
DB_PROVIDER=PostgreSQL docker compose --profile postgres up -d
```

The `postgres` profile activates the PostgreSQL service and configures the API to use Npgsql.

### Networks & Volumes

**Network**: `myapp-network` (bridge) — all services communicate internally.

**Persistent volumes:**
- `sqlserver-data` — SQL Server databases
- `postgres-data` — PostgreSQL databases
- `redis-data` — Redis persistence
- `rabbitmq-data` — RabbitMQ queues and exchanges
- `seq-data` — Seq log storage
- `grafana-data` — Grafana dashboards and settings

### Health Checks

Docker health checks are configured for infrastructure services:

| Service | Health Check | Interval |
|---------|-------------|----------|
| SQL Server | `sqlcmd -Q "SELECT 1"` | 10s |
| PostgreSQL | `pg_isready` | 10s |
| Redis | `redis-cli ping` | 10s |
| RabbitMQ | `rabbitmq-diagnostics ping` | 10s |

---

## Production Deployment

### Production Docker Compose (`docker-compose.prod.yml`)

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

Production overrides include:

| Feature | Configuration |
|---------|--------------|
| **Images** | Pre-built from registry (`${DOCKER_REGISTRY}/myapp-api:${IMAGE_TAG}`) |
| **Replicas** | 2 instances each for API and Dashboard |
| **Resource limits** | API: 1 CPU, 512 MB RAM |
| **Port hiding** | Database, Redis ports not exposed externally |
| **Restart policy** | `on-failure` for all services |

### Environment Variables for Production

```bash
# .env (production)
DB_PROVIDER=PostgreSQL
DB_PASSWORD=<strong-random-password>
POSTGRES_PASSWORD=<strong-random-password>
JWT_SECRET_KEY=<cryptographically-random-32-char-minimum>
RABBITMQ_PASSWORD=<strong-random-password>
REDIS_PASSWORD=<strong-random-password>
GRAFANA_PASSWORD=<strong-random-password>
DOCKER_REGISTRY=ghcr.io/your-org
IMAGE_TAG=v1.0.0
API_URL=https://api.yourdomain.com
```

> ⚠️ **Never use default passwords in production.** Generate strong random values for all secrets.

### Multi-Stage Dockerfiles

#### API Dockerfile (`docker/api/Dockerfile`)

```
Stage 1: build     → .NET SDK 8.0, restore + build
Stage 2: publish   → dotnet publish -c Release
Stage 3: final     → .NET ASP.NET 8.0 runtime (minimal image)
```

| Stage | Base Image | Size |
|-------|-----------|------|
| Build | `mcr.microsoft.com/dotnet/sdk:8.0` | ~800 MB |
| Runtime | `mcr.microsoft.com/dotnet/aspnet:8.0` | ~220 MB |

#### Dashboard Dockerfile (`docker/dashboard/Dockerfile`)

```
Stage 1: deps      → node:20-alpine, npm ci (dependencies only)
Stage 2: builder   → npm run build (Next.js standalone output)
Stage 3: runner    → node:20-alpine, non-root user
```

| Stage | Base Image | Size |
|-------|-----------|------|
| Build | `node:20-alpine` | ~180 MB |
| Runtime | `node:20-alpine` | ~130 MB |

The dashboard runs as a non-root `nextjs` user (UID 1001) for security.

---

## CI/CD Pipelines

### Continuous Integration (`.github/workflows/ci.yml`)

**Triggers:** `push` and `pull_request` to `main` and `develop` branches.

```
┌─────────────────────────────────────────────┐
│                CI Pipeline                   │
├─────────────────────────────────────────────┤
│                                             │
│  ┌──────────┐  ┌───────────┐  ┌──────────┐ │
│  │ API Job  │  │ Dashboard │  │ Mobile   │ │
│  │          │  │ Job       │  │ Job      │ │
│  │ .NET 8   │  │ Node 20   │  │ Flutter  │ │
│  │ restore  │  │ npm ci    │  │ pub get  │ │
│  │ build    │  │ type-check│  │ analyze  │ │
│  │ test     │  │ lint      │  │ test     │ │
│  │ coverage │  │ build     │  │ build APK│ │
│  └──────────┘  └───────────┘  └──────────┘ │
│                                             │
│  ┌──────────────────────────────────────┐   │
│  │          Docker Build Job            │   │
│  │  Build API image (no push)           │   │
│  │  Build Dashboard image (no push)     │   │
│  └──────────────────────────────────────┘   │
└─────────────────────────────────────────────┘
```

#### Jobs Detail

**1. API (ASP.NET Core)**
- Runner: `ubuntu-latest`
- SDK: `.NET 8.0.x`
- Steps: checkout → setup .NET → restore → build (Release) → test (with code coverage) → upload coverage artifact

**2. Dashboard (Next.js)**
- Runner: `ubuntu-latest`
- Runtime: `Node.js 20` with npm cache
- Steps: checkout → setup Node → npm ci → type-check → lint → build

**3. Mobile (Flutter)**
- Runner: `ubuntu-latest`
- SDK: `Flutter 3.24.x` (stable)
- Steps: checkout → setup Flutter → pub get → analyze → test → build APK (debug)

**4. Docker Build Validation**
- Runner: `ubuntu-latest`
- Steps: setup Docker Buildx → build API image → build Dashboard image (no push)

### Continuous Deployment (`.github/workflows/cd.yml`)

**Triggers:**
- Push to `main` branch → build + push images + deploy to staging
- Push tag `v*.*.*` → build + push images + deploy to production

```
┌─────────────────────────────────────────────┐
│                CD Pipeline                   │
├─────────────────────────────────────────────┤
│                                             │
│  ┌──────────────────────────────────────┐   │
│  │        Build & Push Images           │   │
│  │                                      │   │
│  │  Matrix: [api, dashboard]            │   │
│  │  1. Login to GHCR                    │   │
│  │  2. Extract metadata (tags)          │   │
│  │  3. Build Docker image               │   │
│  │  4. Push to ghcr.io                  │   │
│  └──────────────┬───────────────────────┘   │
│                 │                            │
│        ┌────────┴────────┐                  │
│        │                 │                  │
│  ┌─────▼──────┐  ┌──────▼───────┐          │
│  │  Deploy    │  │  Deploy      │          │
│  │  Staging   │  │  Production  │          │
│  │ (on main)  │  │ (on v*.*.*)  │          │
│  └────────────┘  └──────────────┘          │
└─────────────────────────────────────────────┘
```

#### Image Tagging Strategy

| Trigger | Tag Examples |
|---------|-------------|
| Push to `main` | `ghcr.io/org/myapp-api:main`, `:sha-abc123` |
| Push tag `v1.2.3` | `ghcr.io/org/myapp-api:v1.2.3`, `:1.2.3`, `:1.2`, `:1` |

#### GitHub Container Registry (GHCR)

Images are pushed to `ghcr.io` using the GitHub Actions token:

```yaml
permissions:
  contents: read
  packages: write
```

No additional registry credentials needed — the `GITHUB_TOKEN` handles authentication.

#### Deploy Jobs

The staging and production deploy jobs are **placeholder templates**. Customize them for your deployment target:

```yaml
# Example: Deploy to Kubernetes
- name: Deploy to K8s
  run: |
    kubectl set image deployment/api api=${{ env.IMAGE }}
    kubectl rollout status deployment/api

# Example: Deploy to Azure Container Apps
- name: Deploy to Azure
  uses: azure/container-apps-deploy-action@v1
  with:
    imageToDeploy: ${{ env.IMAGE }}

# Example: Deploy via SSH
- name: Deploy via SSH
  run: |
    ssh deploy@server "docker pull ${{ env.IMAGE }} && docker compose up -d"
```

---

## Production Checklist

### Security

- [ ] Change all default passwords in `.env`
- [ ] Generate a cryptographically random JWT secret (≥ 32 characters)
- [ ] Enable HTTPS/TLS with valid certificates
- [ ] Configure CORS to allow only your frontend domains
- [ ] Remove Swagger UI in production (or restrict access)
- [ ] Restrict database ports (not exposed externally)
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`

### Infrastructure

- [ ] Use managed database services (Azure SQL, RDS, Cloud SQL) if possible
- [ ] Use managed Redis (ElastiCache, Azure Cache) for high availability
- [ ] Configure RabbitMQ clustering for message durability
- [ ] Set up persistent volumes for stateful services
- [ ] Configure container health checks and restart policies

### Monitoring

- [ ] Verify Prometheus is scraping `/metrics`
- [ ] Configure Grafana alerts for error rate and latency
- [ ] Set up Seq/Serilog log retention policies
- [ ] Verify Jaeger traces are flowing through OTEL Collector
- [ ] Set up uptime monitoring for `/health/live` endpoint

### CI/CD

- [ ] Configure deployment secrets in GitHub repository settings
- [ ] Set up staging and production environments in GitHub
- [ ] Add deployment approval rules for production
- [ ] Configure image vulnerability scanning in GHCR
- [ ] Set up rollback procedures

### Scaling

- [ ] API: Horizontal scaling with multiple replicas (SignalR uses Redis backplane)
- [ ] Dashboard: Horizontal scaling (stateless Next.js standalone)
- [ ] Database: Read replicas for query-heavy workloads
- [ ] Redis: Cluster mode for high throughput
- [ ] Load balancer: Configure health check endpoints
