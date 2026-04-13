# 🔧 MyApp API — ASP.NET Core 8, Clean Architecture

The backend API built with **ASP.NET Core 8** following **Clean Architecture** principles. It provides REST endpoints, real-time communication via SignalR, background job processing with Hangfire, and full observability with OpenTelemetry.

---

## 📐 Architecture

The solution follows a **4-layer Clean Architecture** pattern where dependencies flow inward:

```
┌───────────────────────────────────────────────────────────────┐
│                    MyApp.API (Presentation)                    │
│  Controllers · SignalR Hub · Middleware · Filters · Swagger    │
├───────────────────────────────────────────────────────────────┤
│                  MyApp.Application (Use Cases)                 │
│  Commands · Queries · DTOs · Validators · Behaviors            │
├───────────────────────────────────────────────────────────────┤
│                     MyApp.Domain (Core)                        │
│  Entities · Enums · Events · Repository Interfaces             │
├───────────────────────────────────────────────────────────────┤
│               MyApp.Infrastructure (External)                  │
│  EF Core · JWT · Redis · RabbitMQ · Hangfire · OpenTelemetry   │
└───────────────────────────────────────────────────────────────┘
```

**Dependency rule**: Inner layers never reference outer layers. The Domain layer has zero external dependencies.

### Project References

```
MyApp.API
├── → MyApp.Application
├── → MyApp.Infrastructure
└── → MyApp.Domain

MyApp.Infrastructure
├── → MyApp.Application
└── → MyApp.Domain

MyApp.Application
└── → MyApp.Domain

MyApp.Domain
└── (no project references)
```

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server or PostgreSQL (or use Docker Compose from root)
- Redis
- RabbitMQ

### Local Development

```bash
cd src/api

# Restore dependencies
dotnet restore MyApp.sln

# Build
dotnet build MyApp.sln -c Debug

# Run
dotnet run --project MyApp.API
```

The API starts at **http://localhost:5000** with Swagger at **http://localhost:5000/swagger**.

### Using Docker (Recommended)

From the repository root:

```bash
docker compose up -d
```

This starts the API with all required infrastructure (database, Redis, RabbitMQ, etc.).

---

## 📁 Solution Structure

```
src/api/
├── MyApp.sln
│
├── MyApp.Domain/                          # Core business logic
│   ├── Common/
│   │   └── BaseEntity.cs                  # Base class: Id, CreatedAt, UpdatedAt, DomainEvents
│   ├── Entities/
│   │   └── User.cs                        # User aggregate root
│   ├── Enums/
│   │   └── UserRole.cs                    # User, Admin, SuperAdmin
│   ├── Events/
│   │   ├── IDomainEvent.cs                # Domain event marker interface
│   │   └── UserCreatedEvent.cs            # Raised when a user registers
│   └── Interfaces/
│       ├── IUserRepository.cs             # User data access contract
│       └── IUnitOfWork.cs                 # Transaction boundary
│
├── MyApp.Application/                     # Use cases / business rules
│   ├── Common/
│   │   ├── Behaviors/
│   │   │   ├── ValidationBehavior.cs      # MediatR pipeline: FluentValidation
│   │   │   └── LoggingBehavior.cs         # MediatR pipeline: request/response logging
│   │   ├── Exceptions/
│   │   │   ├── NotFoundException.cs       # → HTTP 404
│   │   │   ├── UnauthorizedException.cs   # → HTTP 401
│   │   │   ├── ConflictException.cs       # → HTTP 409
│   │   │   └── ValidationException.cs     # → HTTP 400
│   │   └── Interfaces/
│   │       ├── IJwtService.cs             # JWT token generation & validation
│   │       ├── IPasswordHasher.cs         # Password hashing (BCrypt)
│   │       ├── ICacheService.cs           # Redis cache abstraction
│   │       ├── IMessageBroker.cs          # RabbitMQ publish/subscribe
│   │       └── INotificationService.cs    # SignalR notification abstraction
│   ├── Features/
│   │   ├── Auth/
│   │   │   ├── Commands/
│   │   │   │   ├── RegisterCommand.cs     # User registration + token generation
│   │   │   │   ├── LoginCommand.cs        # Authentication + token generation
│   │   │   │   └── RefreshTokenCommand.cs # Refresh token rotation
│   │   │   └── DTOs/
│   │   │       └── AuthResponse.cs        # { accessToken, refreshToken, userId, email, role }
│   │   ├── Users/
│   │   │   ├── Queries/
│   │   │   │   └── GetUserByIdQuery.cs    # Fetch user by ID
│   │   │   └── DTOs/
│   │   │       └── UserDto.cs             # User data transfer object
│   │   └── Notifications/
│   │       └── Commands/
│   │           └── SendNotificationCommand.cs  # Push notification via SignalR
│   └── DependencyInjection.cs             # MediatR + FluentValidation registration
│
├── MyApp.Infrastructure/                  # External concerns implementation
│   ├── Data/
│   │   ├── AppDbContext.cs                # EF Core context with domain event dispatch
│   │   ├── Configurations/
│   │   │   └── UserConfiguration.cs       # EF Core entity config (indexes, constraints)
│   │   ├── Repositories/
│   │   │   └── UserRepository.cs          # IUserRepository implementation
│   │   └── UnitOfWork.cs                  # IUnitOfWork implementation
│   ├── Identity/
│   │   ├── JwtService.cs                  # JWT generation (HMAC-SHA256) & validation
│   │   └── PasswordHasher.cs              # BCrypt hashing
│   ├── Caching/
│   │   └── RedisCacheService.cs           # ICacheService with GetOrSetAsync
│   ├── Messaging/
│   │   ├── RabbitMqMessageBroker.cs       # Topic exchange, persistent delivery
│   │   └── RabbitMqOptions.cs             # Connection configuration
│   ├── BackgroundJobs/
│   │   ├── JobScheduler.cs                # Hangfire recurring job setup
│   │   └── SampleRecurringJob.cs          # Example job with retry policy
│   └── DependencyInjection.cs             # All infrastructure service registration
│
└── MyApp.API/                             # Presentation layer
    ├── Program.cs                         # Application startup & middleware pipeline
    ├── appsettings.json                   # Configuration (DB, JWT, Redis, RabbitMQ, OTel)
    ├── Controllers/
    │   ├── AuthController.cs              # /api/auth — login, register, refresh
    │   └── UsersController.cs             # /api/users — user profile & admin queries
    ├── Hubs/
    │   └── NotificationHub.cs             # SignalR hub for real-time notifications
    ├── Middleware/
    │   └── GlobalExceptionMiddleware.cs   # Maps domain exceptions to HTTP responses
    ├── Filters/
    │   └── HangfireAuthorizationFilter.cs # Admin/SuperAdmin dashboard access
    └── Services/
        └── SignalRNotificationService.cs  # INotificationService → HubContext adapter
```

---

## 🌐 API Endpoints

### Authentication (`/api/auth`)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `POST` | `/api/auth/register` | Register a new user | No |
| `POST` | `/api/auth/login` | Login and receive tokens | No |
| `POST` | `/api/auth/refresh` | Refresh an expired access token | No |

#### `POST /api/auth/register`

**Request:**
```json
{
  "username": "johndoe",
  "email": "john@example.com",
  "password": "SecurePass123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Validation rules:**
- `username`: required, 3–50 characters
- `email`: required, valid email format
- `password`: required, minimum 8 characters

**Response** `200 OK`:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "a1b2c3d4e5f6...",
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "john@example.com",
  "role": "User"
}
```

**Errors:**
- `409 Conflict` — Email already registered

#### `POST /api/auth/login`

**Request:**
```json
{
  "email": "john@example.com",
  "password": "SecurePass123!"
}
```

**Errors:**
- `404 Not Found` — User not found
- `401 Unauthorized` — Invalid password or account deactivated

#### `POST /api/auth/refresh`

**Request:**
```json
{
  "refreshToken": "a1b2c3d4e5f6..."
}
```

**Errors:**
- `401 Unauthorized` — Invalid or expired refresh token

### Users (`/api/users`)

| Method | Endpoint | Description | Auth Required | Roles |
|--------|----------|-------------|---------------|-------|
| `GET` | `/api/users/me` | Get current user profile | Yes | Any |
| `GET` | `/api/users/{id}` | Get user by ID | Yes | Admin, SuperAdmin |

#### `GET /api/users/me`

**Response** `200 OK`:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "username": "johndoe",
  "email": "john@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "role": "User",
  "isActive": true,
  "createdAt": "2024-01-15T10:30:00Z"
}
```

### Health & Monitoring

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/health` | Detailed health check (DB, Redis, RabbitMQ) |
| `GET` | `/health/live` | Liveness probe (always healthy) |
| `GET` | `/metrics` | Prometheus metrics scrape endpoint |
| `GET` | `/swagger` | OpenAPI / Swagger UI |

### SignalR Hub (`/hubs/notifications`)

**Connection:** Requires JWT authentication via query string:
```
ws://localhost:5000/hubs/notifications?access_token=eyJhbGci...
```

**Client → Server Methods:**

| Method | Parameters | Description |
|--------|-----------|-------------|
| `JoinGroup` | `groupName: string` | Subscribe to a notification group |
| `LeaveGroup` | `groupName: string` | Unsubscribe from a group |
| `SendToGroup` | `groupName: string, message: string` | Broadcast to a group |

**Server → Client Events:**

| Event | Payload | Description |
|-------|---------|-------------|
| `ReceiveMessage` | `userId: string, message: string` | Group message received |
| `Notification` | `{ message: string, timestamp: string }` | User-specific notification |

**Auto-created groups:**
- `user:{userId}` — Each authenticated user is automatically added to their personal group on connection.

---

## 🏛️ Domain Layer

### Entities

#### `User` (Aggregate Root)

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Primary key (auto-generated) |
| `Username` | `string` | Unique, 3–50 characters |
| `Email` | `string` | Unique, max 255 characters |
| `PasswordHash` | `string` | BCrypt-hashed password |
| `FirstName` | `string?` | Optional, max 100 characters |
| `LastName` | `string?` | Optional, max 100 characters |
| `Role` | `UserRole` | User / Admin / SuperAdmin |
| `IsActive` | `bool` | Account active status |
| `RefreshToken` | `string?` | Current refresh token |
| `RefreshTokenExpiry` | `DateTime?` | Refresh token expiration |
| `CreatedAt` | `DateTime` | Creation timestamp (UTC) |
| `UpdatedAt` | `DateTime?` | Last update timestamp (UTC) |

**Methods:**
- `User.Create(username, email, passwordHash, role)` — Factory method (raises `UserCreatedEvent`)
- `UpdateRefreshToken(token, expiry)` — Sets new refresh token
- `RevokeRefreshToken()` — Clears refresh token
- `Deactivate()` — Sets `IsActive = false`

### Enums

```csharp
public enum UserRole
{
    User = 0,
    Admin = 1,
    SuperAdmin = 2
}
```

### Domain Events

| Event | Properties | Trigger |
|-------|-----------|---------|
| `UserCreatedEvent` | `UserId`, `Email` | New user registration |

### Repository Interfaces

#### `IUserRepository`

```csharp
Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
Task<User?> GetByEmailAsync(string email, CancellationToken ct)
Task<User?> GetByUsernameAsync(string username, CancellationToken ct)
Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct)
Task<IEnumerable<User>> GetAllAsync(CancellationToken ct)
Task AddAsync(User user, CancellationToken ct)
void Update(User user)
void Delete(User user)
```

#### `IUnitOfWork`

```csharp
IUserRepository Users { get; }
Task<int> SaveChangesAsync(CancellationToken ct)
```

---

## ⚙️ Application Layer

### CQRS Pattern (MediatR)

All use cases are implemented as **Commands** (write operations) or **Queries** (read operations) dispatched through MediatR:

```csharp
// Sending a command from a controller
var result = await _mediator.Send(new LoginCommand(email, password));
```

### Pipeline Behaviors

| Behavior | Order | Description |
|----------|-------|-------------|
| `ValidationBehavior` | 1st | Runs all FluentValidation validators; throws `ValidationException` on failure |
| `LoggingBehavior` | 2nd | Logs request name, properties, and completion via Serilog |

### Application Interfaces

| Interface | Implementation | Description |
|-----------|---------------|-------------|
| `IJwtService` | `JwtService` | Generate/validate JWT access & refresh tokens |
| `IPasswordHasher` | `PasswordHasher` | BCrypt hash & verify |
| `ICacheService` | `RedisCacheService` | Redis cache with `GetOrSetAsync` pattern |
| `IMessageBroker` | `RabbitMqMessageBroker` | Topic exchange publish/subscribe |
| `INotificationService` | `SignalRNotificationService` | Real-time push via SignalR |

### Custom Exceptions

| Exception | HTTP Status | Usage |
|-----------|-------------|-------|
| `NotFoundException` | 404 | User not found, resource not found |
| `UnauthorizedException` | 401 | Invalid credentials, expired token, inactive account |
| `ConflictException` | 409 | Duplicate email on registration |
| `ValidationException` | 400 | FluentValidation failures (returns error list) |

---

## 🔧 Infrastructure Layer

### Database (EF Core)

**Provider switching** via `DatabaseProvider` config/env:

```csharp
// appsettings.json
"DatabaseProvider": "SqlServer"  // or "PostgreSQL"
```

- **SQL Server**: `UseSqlServer()` with `Microsoft.EntityFrameworkCore.SqlServer`
- **PostgreSQL**: `UseNpgsql()` with `Npgsql.EntityFrameworkCore.PostgreSQL`

**Auto-migration** in development: `context.Database.Migrate()` runs on startup.

**Domain event dispatch**: `SaveChangesAsync()` automatically dispatches domain events from modified entities.

### JWT Authentication

| Setting | Default | Description |
|---------|---------|-------------|
| `Jwt:SecretKey` | env `JWT_SECRET_KEY` | HMAC-SHA256 signing key (≥ 32 chars) |
| `Jwt:Issuer` | `MyApp` | Token issuer |
| `Jwt:Audience` | `MyApp` | Token audience |
| `Jwt:ExpiryMinutes` | `60` | Access token lifetime |

**Access token claims:** `sub` (userId), `email`, `role`, `jti` (unique ID), `iat` (issued at)

**Refresh token:** 64-byte cryptographically random value, 7-day expiry, stored on User entity.

### Redis Cache

Registered as `ICacheService` singleton:

```csharp
// Cache-aside pattern
var user = await _cache.GetOrSetAsync(
    $"user:{id}",
    () => _repo.GetByIdAsync(id, ct),
    TimeSpan.FromMinutes(10),
    ct
);
```

### RabbitMQ

- **Exchange type**: Topic (durable)
- **Delivery mode**: Persistent
- **Connection recovery**: Automatic
- **Serialization**: JSON (System.Text.Json)

```csharp
// Publishing
await _broker.PublishAsync("events", "user.created", new UserCreatedEvent(userId, email), ct);

// Subscribing
await _broker.SubscribeAsync<UserCreatedEvent>("user-events", async (evt) => {
    // Handle event
}, ct);
```

### Hangfire

- **Dashboard**: `/hangfire` (requires `Admin` or `SuperAdmin` role)
- **Storage**: Auto-selects SQL Server or PostgreSQL based on `DatabaseProvider`
- **Sample job**: `SampleRecurringJob` runs every minute with 3 retry attempts

### OpenTelemetry

| Signal | Exporter | Target |
|--------|----------|--------|
| Traces | OTLP gRPC | `otel-collector:4317` → Jaeger |
| Metrics | Prometheus | `/metrics` endpoint |

**Instrumentation**: ASP.NET Core, HTTP Client, Entity Framework Core.

---

## ⚙️ Configuration Reference

### `appsettings.json`

```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "DefaultConnection": "Server=db,1433;Database=MyAppDb;User Id=sa;Password=...;TrustServerCertificate=True;",
    "Redis": "redis:6379",
    "RabbitMQ": "amqp://rabbitmq:rabbitmq@rabbitmq:5672/"
  },
  "Jwt": {
    "SecretKey": "super-secret-key-change-in-production-min-32-chars!",
    "Issuer": "MyApp",
    "Audience": "MyApp",
    "ExpiryMinutes": 60
  },
  "RabbitMQ": {
    "Host": "rabbitmq",
    "Port": 5672,
    "Username": "rabbitmq",
    "Password": "rabbitmq",
    "VirtualHost": "/"
  },
  "OpenTelemetry": {
    "ServiceName": "MyApp.API",
    "OtlpEndpoint": "http://otel-collector:4317"
  },
  "Serilog": {
    "SeqUrl": "http://seq:5341"
  },
  "AllowedOrigins": [
    "http://localhost:3000",
    "https://your-dashboard-domain.com"
  ]
}
```

### Middleware Pipeline Order

```
1. Serilog Request Logging
2. Global Exception Middleware
3. CORS
4. Authentication
5. Authorization
6. Controllers / SignalR Hub / Health Checks / Hangfire / Metrics
```

---

## 🧪 Testing

```bash
# Run all tests
dotnet test MyApp.sln --verbosity normal

# Run with code coverage
dotnet test MyApp.sln --collect:"XPlat Code Coverage"

# Run specific project tests
dotnet test MyApp.Domain.Tests/
```

---

## 🐞 Troubleshooting

| Problem | Solution |
|---------|----------|
| **Database connection fails** | Check `ConnectionStrings:DefaultConnection` and ensure DB container is running |
| **JWT token invalid** | Verify `Jwt:SecretKey` is ≥ 32 characters and matches across API instances |
| **Redis connection refused** | Ensure Redis is running; check `ConnectionStrings:Redis` |
| **RabbitMQ connection fails** | Verify RabbitMQ is running and credentials match `RabbitMQ:*` settings |
| **SignalR 401** | Ensure JWT token is passed as `?access_token=` query parameter |
| **Hangfire dashboard 403** | User must have `Admin` or `SuperAdmin` role |
| **CORS errors** | Add your frontend URL to `AllowedOrigins` in `appsettings.json` |
