# 🏛️ Architecture & Design Decisions

This document describes the architecture of MyApp, the design decisions behind it, and the rules that govern each layer.

---

## Overview

MyApp is a **multi-tier application** composed of three client-facing applications backed by a single API:

```
┌────────────────┐  ┌────────────────┐  ┌────────────────┐
│  Next.js 14    │  │  Flutter 3     │  │  Future Client │
│  Dashboard     │  │  Mobile App    │  │  (extensible)  │
└───────┬────────┘  └───────┬────────┘  └───────┬────────┘
        │  REST + SignalR    │                   │
        └───────────┬───────┘───────────────────┘
                    │
        ┌───────────▼───────────┐
        │  ASP.NET Core 8 API   │
        │  (Clean Architecture) │
        └───────────┬───────────┘
                    │
   ┌────────────────┼──────────────────┐
   │                │                  │
┌──▼──┐        ┌───▼───┐        ┌─────▼─────┐
│ DB  │        │ Redis │        │ RabbitMQ  │
└─────┘        └───────┘        └───────────┘
```

---

## API — Clean Architecture

The backend follows **Clean Architecture** (also known as Onion Architecture or Hexagonal Architecture) as described by Robert C. Martin. The core principle: **dependencies point inward**.

### Layer Responsibilities

#### 1. Domain Layer (`MyApp.Domain`)

The innermost layer. Contains the core business logic with **zero external dependencies**.

| Component | Description |
|-----------|-------------|
| **Entities** | Business objects with identity and behavior (e.g., `User`) |
| **Value Objects** | Immutable objects defined by their values |
| **Enums** | Domain enumerations (e.g., `UserRole`) |
| **Domain Events** | Signals that something happened (e.g., `UserCreatedEvent`) |
| **Repository Interfaces** | Contracts for data access (e.g., `IUserRepository`) |
| **BaseEntity** | Common properties: `Id`, `CreatedAt`, `UpdatedAt`, `DomainEvents` |

**Rules:**
- No NuGet packages (except possibly a shared kernel)
- No references to other projects
- All behavior lives on entities (rich domain model)
- Entities are created through factory methods that raise domain events

#### 2. Application Layer (`MyApp.Application`)

Contains use cases that orchestrate domain entities. Implements **CQRS** (Command/Query Responsibility Segregation) via MediatR.

| Component | Description |
|-----------|-------------|
| **Commands** | Write operations (e.g., `RegisterCommand`, `LoginCommand`) |
| **Queries** | Read operations (e.g., `GetUserByIdQuery`) |
| **DTOs** | Data transfer objects (e.g., `AuthResponse`, `UserDto`) |
| **Validators** | FluentValidation rules for each command/query |
| **Behaviors** | Cross-cutting concerns in the MediatR pipeline |
| **Interfaces** | Abstractions for infrastructure services |
| **Exceptions** | Business exceptions mapped to HTTP status codes |

**Rules:**
- References only the Domain layer
- Defines interfaces that Infrastructure implements (Dependency Inversion)
- No direct database access or external service calls
- All cross-cutting concerns handled via MediatR pipeline behaviors

#### 3. Infrastructure Layer (`MyApp.Infrastructure`)

Implements all external concerns. This is where "real" implementations live.

| Component | Implementation |
|-----------|---------------|
| **Data Access** | EF Core `AppDbContext`, repositories, `UnitOfWork` |
| **Authentication** | `JwtService` (HMAC-SHA256), `PasswordHasher` (BCrypt) |
| **Caching** | `RedisCacheService` with `GetOrSetAsync` |
| **Messaging** | `RabbitMqMessageBroker` with topic exchange |
| **Background Jobs** | Hangfire `JobScheduler`, `SampleRecurringJob` |
| **Telemetry** | OpenTelemetry configuration (traces + metrics) |

**Rules:**
- References Application and Domain layers
- Implements interfaces defined in Application
- All third-party library integrations live here
- Can be swapped out without changing domain or application logic

#### 4. API Layer (`MyApp.API`)

The outermost layer — the entry point for all HTTP requests.

| Component | Description |
|-----------|-------------|
| **Controllers** | REST endpoints, delegates to MediatR |
| **SignalR Hub** | Real-time notification hub |
| **Middleware** | Global exception handling, request logging |
| **Filters** | Hangfire dashboard authorization |
| **Services** | `SignalRNotificationService` (INotificationService adapter) |
| **Program.cs** | DI container setup, middleware pipeline |

**Rules:**
- References all other layers (composition root)
- Controllers are thin — only map HTTP to MediatR requests
- No business logic in controllers
- Exception handling is centralized in middleware

### Dependency Rule Diagram

```
API → Application → Domain ← Infrastructure
 └──────────────────────────→ Infrastructure
```

- ✅ API references Application, Infrastructure, Domain
- ✅ Infrastructure references Application, Domain
- ✅ Application references Domain
- ❌ Domain references nothing
- ❌ Application never references Infrastructure

---

## CQRS Pattern

Commands and queries are separated for clarity and scalability:

```
Controller                  MediatR Pipeline                  Handler
    │                            │                               │
    │  Send(LoginCommand)        │                               │
    ├───────────────────────────>│  ValidationBehavior            │
    │                            │  LoggingBehavior               │
    │                            ├──────────────────────────────>│
    │                            │                               │ → Repository
    │                            │                               │ → JwtService
    │                            │<──────────────────────────────│
    │<───────────────────────────│  AuthResponse                 │
    │                            │                               │
```

**Commands** (write operations):
- `RegisterCommand` — Creates user, generates tokens
- `LoginCommand` — Validates credentials, generates tokens
- `RefreshTokenCommand` — Rotates refresh token
- `SendNotificationCommand` — Pushes notification via SignalR

**Queries** (read operations):
- `GetUserByIdQuery` — Fetches user profile

**Pipeline behaviors** (executed in order for every request):
1. `ValidationBehavior` — Runs FluentValidation, throws `ValidationException` on failure
2. `LoggingBehavior` — Logs request name and execution

---

## Authentication Design

### Token Strategy

```
Client                   API                    Database
  │                       │                        │
  │  POST /auth/login     │                        │
  ├──────────────────────>│  Verify BCrypt hash     │
  │                       ├───────────────────────>│
  │                       │<───────────────────────│
  │                       │  Generate JWT (60 min)  │
  │                       │  Generate Refresh (7d)  │
  │                       │  Store refresh in DB    │
  │  { accessToken,       │                        │
  │    refreshToken }     │                        │
  │<──────────────────────│                        │
  │                       │                        │
  │  GET /api/users/me    │                        │
  │  Authorization: Bearer│                        │
  ├──────────────────────>│  Validate JWT claims    │
  │                       │                        │
  │  POST /auth/refresh   │                        │
  │  { refreshToken }     │                        │
  ├──────────────────────>│  Validate + Rotate     │
  │  { new tokens }       │  (old token revoked)   │
  │<──────────────────────│                        │
```

**Why refresh token rotation?**
- Short-lived access tokens (60 min) limit exposure window
- Refresh tokens enable seamless session continuity
- Rotation invalidates old refresh tokens on each use (detects token theft)
- 7-day expiry provides a reasonable session window

### Role-Based Access Control

| Role | Permissions |
|------|------------|
| `User` | Access own profile, receive notifications |
| `Admin` | Access any user profile, Hangfire dashboard |
| `SuperAdmin` | Full access, Hangfire dashboard |

Roles are stored as claims in the JWT and checked via `[Authorize(Roles = "Admin,SuperAdmin")]`.

---

## Database Strategy

### Multi-Provider Support

The API supports both SQL Server and PostgreSQL through a configuration-driven approach:

```csharp
var provider = configuration["DatabaseProvider"]; // "SqlServer" or "PostgreSQL"

if (provider == "PostgreSQL")
    services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
else
    services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString));
```

This applies to:
- EF Core database context
- Hangfire job storage
- Health checks

**Why multi-provider?** Teams can choose the database that fits their infrastructure. PostgreSQL is free and open-source; SQL Server may be preferred in Microsoft-centric environments.

### Domain Event Dispatch

`AppDbContext.SaveChangesAsync()` automatically dispatches domain events from modified entities before persisting changes. This enables decoupled side effects without cluttering business logic.

---

## Real-Time Communication Design

### SignalR Architecture

```
┌──────────────────────────────────────────────────┐
│                  SignalR Hub                       │
│  ┌────────────┐  ┌───────────┐  ┌─────────────┐  │
│  │ JoinGroup  │  │ LeaveGroup│  │ SendToGroup │  │
│  └────────────┘  └───────────┘  └─────────────┘  │
│                                                    │
│  Groups: user:{userId}, custom-group-name          │
│                                                    │
│  Redis Backplane (horizontal scaling)              │
└──────────────────────────────────────────────────┘
         ↕                    ↕
┌─────────────┐      ┌─────────────┐
│  Dashboard  │      │   Mobile    │
│  (signalr)  │      │ (signalr_   │
│             │      │  netcore)   │
└─────────────┘      └─────────────┘
```

**Redis backplane**: When running multiple API instances, SignalR uses Redis pub/sub to ensure messages reach all connected clients regardless of which instance they're connected to.

**User groups**: Each user automatically joins `user:{userId}` on connection, enabling targeted notifications without the sender needing to know which server the recipient is connected to.

---

## Caching Strategy

The `ICacheService` implements the **cache-aside** pattern:

```
1. Check cache for key
2. If found → return cached value
3. If not found → call factory function
4. Store result in cache with TTL
5. Return result
```

This is exposed as `GetOrSetAsync`:
```csharp
var user = await _cache.GetOrSetAsync(
    $"user:{id}",
    () => _repo.GetByIdAsync(id, ct),
    TimeSpan.FromMinutes(10),
    ct
);
```

---

## Message Broker Design

RabbitMQ uses a **topic exchange** pattern for event-driven communication:

```
Publisher                Exchange              Queue              Consumer
   │                    (topic)                 │                    │
   │  user.created       │                     │                    │
   ├────────────────────>│  route: user.*       │                    │
   │                     ├─────────────────────>│  user-events       │
   │                     │                     ├───────────────────>│
   │                     │                     │                    │
```

**Why topic exchange?**
- Flexible routing with wildcard patterns (`user.*`, `#`)
- Multiple queues can bind to the same exchange with different routing keys
- Easy to add new consumers without modifying publishers

---

## Frontend Architecture

### Dashboard (Next.js)

```
Zustand Store (persisted) ←→ React Components
      ↑                            ↓
      │                      Axios Client
      │                            ↓
      └── Token Refresh ←── API Response (401)
```

**Key decision**: The Axios interceptor reads/patches the Zustand-persisted `localStorage` entry directly rather than importing the store. This avoids circular dependencies and ensures the token refresh flow works independently of React's render cycle.

### Mobile (Flutter)

```
BLoC (State Machine) ←→ Screens (Widgets)
      ↑                       
      │                       
 Repository → ApiClient (Dio) → API
      ↑
 SecureStorage (encrypted)
```

**Key decision**: Using `FlutterSecureStorage` instead of `SharedPreferences` for tokens — tokens are encrypted at rest on both iOS (Keychain) and Android (EncryptedSharedPreferences).

---

## Observability Design

### Three Pillars

| Pillar | Collection | Storage | Visualization |
|--------|-----------|---------|---------------|
| **Logs** | Serilog | Seq | Seq UI |
| **Metrics** | OpenTelemetry SDK | Prometheus | Grafana |
| **Traces** | OpenTelemetry SDK | Jaeger | Jaeger UI |

### Data Flow

```
API Application
  │
  ├── Serilog ──────────────────────────→ Seq (port 5341)
  │
  ├── OTel Traces ──→ OTLP Collector ──→ Jaeger (port 16686)
  │
  └── OTel Metrics ──→ /metrics ──→ Prometheus ──→ Grafana (port 3001)
```

See [monitoring.md](monitoring.md) for detailed setup and configuration.

---

## Extensibility Points

| Extension | How To |
|-----------|--------|
| Add a new entity | Domain → entity + repo interface; Infrastructure → EF config + repo; Application → commands/queries |
| Add a new API endpoint | Application → command/query; API → controller |
| Add a new background job | Infrastructure → implement job class; `JobScheduler` → register recurring |
| Add a new notification type | Application → command; use `INotificationService` |
| Switch database provider | Set `DatabaseProvider` env var; add migrations if needed |
| Add a new dashboard page | Dashboard → `app/dashboard/<page>/page.tsx` |
| Add a new mobile feature | Mobile → `lib/features/<feature>/` with BLoC + screens |
| Add a new cache entry | Use `ICacheService.GetOrSetAsync` in any handler |
| Publish a domain event | Call `entity.AddDomainEvent(new MyEvent())` before saving |
