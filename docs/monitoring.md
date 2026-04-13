# 🔭 Monitoring & Observability Guide

MyApp includes a complete observability stack covering the three pillars: **logs**, **metrics**, and **traces**.

---

## Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        API Application                           │
│                                                                   │
│  Serilog ──────────────────────────────────────→ Seq (logs)      │
│                                                                   │
│  OpenTelemetry SDK                                                │
│    ├── Traces ──→ OTLP gRPC ──→ OTEL Collector ──→ Jaeger       │
│    └── Metrics ──→ /metrics ──→ Prometheus ──→ Grafana           │
│                                                                   │
│  Health Checks ──→ /health, /health/live                         │
└─────────────────────────────────────────────────────────────────┘
```

### Service URLs

| Service | URL | Purpose |
|---------|-----|---------|
| **Seq** | http://localhost:5341 | Structured log search & analysis |
| **Prometheus** | http://localhost:9090 | Metrics storage & queries |
| **Grafana** | http://localhost:3001 | Metrics dashboards & alerts |
| **Jaeger** | http://localhost:16686 | Distributed trace visualization |

---

## Structured Logging (Serilog → Seq)

### Configuration

Serilog is configured in `Program.cs`:

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .WriteTo.Seq(seqUrl)
    .CreateLogger();
```

**Enrichers:**
- `FromLogContext` — Adds scoped properties
- `WithMachineName` — Container/host name
- `WithThreadId` — Thread identification

**Sinks:**
- `Console` — stdout (visible in `docker compose logs`)
- `Seq` — Structured log aggregation at `http://seq:5341`

### Using Seq

1. Open http://localhost:5341
2. Use the search bar to filter logs:
   - `@Level = 'Error'` — All errors
   - `RequestPath like '/api/auth%'` — Auth endpoint logs
   - `SourceContext = 'MyApp.API.Middleware.GlobalExceptionMiddleware'` — Exception logs
3. Create saved searches and dashboards for common queries

### Log Levels

| Level | Usage |
|-------|-------|
| `Verbose` | Detailed debugging |
| `Debug` | Internal diagnostics |
| `Information` | Normal operations (request start/end) |
| `Warning` | Unexpected but handled situations |
| `Error` | Failures requiring attention |
| `Fatal` | Application crashes |

### Request Logging

All HTTP requests are automatically logged via `UseSerilogRequestLogging()`:

```
HTTP GET /api/users/me responded 200 in 12.3456ms
```

---

## Metrics (OpenTelemetry → Prometheus → Grafana)

### How It Works

1. **OpenTelemetry SDK** in the API collects metrics (request duration, counts, etc.)
2. **Prometheus** scrapes the `/metrics` endpoint every 5 seconds
3. **Grafana** queries Prometheus to display dashboards

### Prometheus Configuration

Located at `monitoring/prometheus/prometheus.yml`:

```yaml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']

  - job_name: 'myapp-api'
    scrape_interval: 5s
    metrics_path: '/metrics'
    static_configs:
      - targets: ['api:8080']

  - job_name: 'otel-collector'
    static_configs:
      - targets: ['otel-collector:8888']

  - job_name: 'redis'
    static_configs:
      - targets: ['redis:6379']

  - job_name: 'rabbitmq'
    static_configs:
      - targets: ['rabbitmq:15692']
```

### Key Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `http_server_request_duration_seconds` | Histogram | HTTP request latency |
| `http_server_active_requests` | Gauge | Currently processing requests |
| `http_server_request_duration_seconds_count` | Counter | Total requests processed |
| `kestrel_connections` | Gauge | Active connections |
| `db_client_operation_duration` | Histogram | Database query duration |

### Grafana Dashboards

Grafana is auto-provisioned with:

**Datasources** (`monitoring/grafana/provisioning/datasources/datasources.yml`):
- **Prometheus** — `http://prometheus:9090` (default)
- **Jaeger** — `http://jaeger:16686`

**Dashboard: API Overview** (`monitoring/grafana/provisioning/dashboards/api-overview.json`):

| Panel | Visualization | Query |
|-------|--------------|-------|
| HTTP Request Rate | Stat | `rate(http_server_request_duration_seconds_count[5m])` |
| Error Rate | Stat (%) | `rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[5m])` |
| Request Duration P95 | Time series | `histogram_quantile(0.95, rate(http_server_request_duration_seconds_bucket[5m]))` |

### Accessing Grafana

1. Open http://localhost:3001
2. Login: `admin` / password from `.env` (`GRAFANA_PASSWORD`)
3. Navigate to **Dashboards** → **API Overview**

### Custom PromQL Queries

In Prometheus (http://localhost:9090/graph) or Grafana:

```promql
# Request rate (requests/second)
rate(http_server_request_duration_seconds_count[5m])

# Error rate percentage
100 * rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[5m])
/ rate(http_server_request_duration_seconds_count[5m])

# P95 latency by route
histogram_quantile(0.95,
  sum(rate(http_server_request_duration_seconds_bucket[5m])) by (le, http_route)
)

# Active connections
kestrel_connections

# Database query duration P99
histogram_quantile(0.99,
  rate(db_client_operation_duration_bucket[5m])
)
```

---

## Distributed Tracing (OpenTelemetry → Jaeger)

### How It Works

1. **OpenTelemetry SDK** in the API creates spans for requests, DB queries, HTTP calls
2. Traces are sent via **OTLP gRPC** to the **OTEL Collector**
3. The collector exports traces to **Jaeger** for visualization

### Instrumentation

The API includes automatic instrumentation for:

| Component | What's Traced |
|-----------|--------------|
| ASP.NET Core | HTTP request spans (method, route, status) |
| Entity Framework Core | Database queries (SQL, duration) |
| HTTP Client | Outgoing HTTP calls (URL, status) |

### OTEL Collector Configuration

Located at `monitoring/otel/otel-collector-config.yml`:

```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317    # gRPC receiver
      http:
        endpoint: 0.0.0.0:4318    # HTTP receiver

processors:
  batch:
    timeout: 1s
    send_batch_size: 1024
  memory_limiter:
    limit_mib: 400
    check_interval: 5s

exporters:
  otlp:
    endpoint: jaeger:14250         # Traces to Jaeger
    tls:
      insecure: true
  prometheus:
    endpoint: 0.0.0.0:8889        # Metrics for Prometheus
  debug:
    verbosity: normal

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [otlp]           # → Jaeger
    metrics:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [prometheus]     # → Prometheus scrape
    logs:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [debug]          # → Console
```

### Using Jaeger

1. Open http://localhost:16686
2. Select **Service**: `MyApp.API`
3. Click **Find Traces**
4. Click on a trace to see the span waterfall:

```
POST /api/auth/login (200) — 45ms
  ├── middleware — 0.5ms
  ├── MediatR — 44ms
  │   ├── ValidationBehavior — 1ms
  │   ├── LoginCommandHandler — 43ms
  │   │   ├── EF Core: SELECT from Users — 12ms
  │   │   ├── BCrypt Verify — 28ms
  │   │   └── JWT Generate — 2ms
  └── Response — 0.5ms
```

### Trace Context Propagation

Traces propagate across service boundaries via W3C Trace Context headers:
- `traceparent` — Trace ID and span ID
- `tracestate` — Vendor-specific data

---

## Health Checks

### Endpoints

| Endpoint | Purpose | Checks |
|----------|---------|--------|
| `GET /health` | Full dependency check | Database, Redis, RabbitMQ |
| `GET /health/live` | Liveness probe | Process is running |

### Response Format

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "results": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.0567890"
    },
    "redis": {
      "status": "Healthy",
      "duration": "00:00:00.0012345"
    },
    "rabbitmq": {
      "status": "Healthy",
      "duration": "00:00:00.0234567"
    }
  }
}
```

### Container Orchestration

Use these endpoints in Docker/Kubernetes health checks:

```yaml
# Docker Compose
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health/live"]
  interval: 30s
  timeout: 10s
  retries: 3

# Kubernetes
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 10
```

---

## Hangfire Dashboard

The Hangfire dashboard provides visibility into background jobs.

**URL**: http://localhost:5000/hangfire

**Access**: Requires `Admin` or `SuperAdmin` role (enforced by `HangfireAuthorizationFilter`).

**Features:**
- View recurring jobs and their schedules
- Monitor job execution history
- Retry failed jobs
- View job queues and processing servers

---

## Alerting (Grafana)

To set up alerts in Grafana:

1. Navigate to a dashboard panel
2. Click **Edit** → **Alert** tab
3. Define conditions:

**Example: High Error Rate Alert**
```
WHEN avg() OF query(A) IS ABOVE 5
FOR 5m

Query A: 100 * rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[5m])
         / rate(http_server_request_duration_seconds_count[5m])
```

**Example: High Latency Alert**
```
WHEN avg() OF query(A) IS ABOVE 2
FOR 5m

Query A: histogram_quantile(0.95, rate(http_server_request_duration_seconds_bucket[5m]))
```

Configure notification channels (email, Slack, PagerDuty, etc.) in **Grafana** → **Alerting** → **Contact points**.

---

## Troubleshooting

| Issue | Check | Solution |
|-------|-------|---------|
| No logs in Seq | `docker compose logs seq` | Verify Seq is running and `Serilog:SeqUrl` is correct |
| No metrics in Prometheus | http://localhost:9090/targets | Check target status; verify API `/metrics` endpoint |
| No traces in Jaeger | `docker compose logs otel-collector` | Verify OTLP endpoint config in API |
| Grafana "No data" | Test query in Prometheus first | Ensure Prometheus datasource is configured |
| Health check failing | `curl http://localhost:5000/health` | Check which dependency is unhealthy |
| Collector memory issues | `docker stats otel-collector` | Increase `memory_limiter` in collector config |
