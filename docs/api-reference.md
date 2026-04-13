# 📡 API Endpoints Reference

Complete reference for all MyApp API endpoints.

**Base URL**: `http://localhost:5000/api`

---

## Authentication

### Register

Create a new user account.

```
POST /api/auth/register
```

**Request body:**

| Field | Type | Required | Validation |
|-------|------|----------|-----------|
| `username` | `string` | Yes | 3–50 characters |
| `email` | `string` | Yes | Valid email format |
| `password` | `string` | Yes | Minimum 8 characters |
| `firstName` | `string` | No | Max 100 characters |
| `lastName` | `string` | No | Max 100 characters |

**Example request:**

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "johndoe",
    "email": "john@example.com",
    "password": "SecurePass123!",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

**Success response** (`200 OK`):

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6...",
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "john@example.com",
  "role": "User"
}
```

**Error responses:**

| Status | Condition | Body |
|--------|-----------|------|
| `400` | Validation failed | `{ "status": 400, "error": "Validation failed", "errors": [...] }` |
| `409` | Email already registered | `{ "status": 409, "error": "User with this email already exists" }` |

---

### Login

Authenticate and receive tokens.

```
POST /api/auth/login
```

**Request body:**

| Field | Type | Required | Validation |
|-------|------|----------|-----------|
| `email` | `string` | Yes | Valid email format |
| `password` | `string` | Yes | Not empty |

**Example request:**

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "SecurePass123!"
  }'
```

**Success response** (`200 OK`):

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "q1w2e3r4t5y6u7i8o9p0a1s2d3f4g5h6...",
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "john@example.com",
  "role": "User"
}
```

**Error responses:**

| Status | Condition | Body |
|--------|-----------|------|
| `400` | Validation failed | `{ "status": 400, "error": "..." }` |
| `401` | Invalid password | `{ "status": 401, "error": "Invalid credentials" }` |
| `401` | Account deactivated | `{ "status": 401, "error": "Account is deactivated" }` |
| `404` | User not found | `{ "status": 404, "error": "User not found" }` |

---

### Refresh Token

Exchange a refresh token for new access and refresh tokens.

```
POST /api/auth/refresh
```

**Request body:**

| Field | Type | Required |
|-------|------|----------|
| `refreshToken` | `string` | Yes |

**Example request:**

```bash
curl -X POST http://localhost:5000/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6..."
  }'
```

**Success response** (`200 OK`):

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "new-refresh-token-value...",
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "john@example.com",
  "role": "User"
}
```

> **Note**: The old refresh token is invalidated upon successful rotation. Use the new refresh token for the next refresh.

**Error responses:**

| Status | Condition | Body |
|--------|-----------|------|
| `401` | Invalid refresh token | `{ "status": 401, "error": "Invalid refresh token" }` |
| `401` | Expired refresh token | `{ "status": 401, "error": "Refresh token has expired" }` |

---

## Users

All user endpoints require authentication.

### Get Current User

Get the profile of the currently authenticated user.

```
GET /api/users/me
Authorization: Bearer <accessToken>
```

**Example request:**

```bash
curl http://localhost:5000/api/users/me \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."
```

**Success response** (`200 OK`):

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

**Error responses:**

| Status | Condition |
|--------|-----------|
| `401` | No or invalid token |
| `404` | User not found (deleted account) |

---

### Get User by ID

Get a user's profile by their ID. Restricted to `Admin` and `SuperAdmin` roles.

```
GET /api/users/{id}
Authorization: Bearer <accessToken>
```

**Path parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | `GUID` | User ID |

**Example request:**

```bash
curl http://localhost:5000/api/users/550e8400-e29b-41d4-a716-446655440000 \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."
```

**Success response** (`200 OK`): Same schema as Get Current User.

**Error responses:**

| Status | Condition |
|--------|-----------|
| `401` | No or invalid token |
| `403` | Insufficient role (not Admin/SuperAdmin) |
| `404` | User not found |

---

## Health & Monitoring

These endpoints do not require authentication.

### Health Check

Returns detailed health status of all dependencies.

```
GET /health
```

**Response** (`200 OK` or `503 Service Unavailable`):

```json
{
  "status": "Healthy",
  "results": {
    "database": { "status": "Healthy" },
    "redis": { "status": "Healthy" },
    "rabbitmq": { "status": "Healthy" }
  }
}
```

### Liveness Probe

Simple liveness check for container orchestrators.

```
GET /health/live
```

**Response** (`200 OK`):

```json
{
  "status": "Healthy"
}
```

> This endpoint always returns healthy if the process is running. Use `/health` for dependency checks.

### Prometheus Metrics

Exposes metrics in Prometheus text format.

```
GET /metrics
```

**Response** (`200 OK`, `text/plain`):

```
# HELP http_server_request_duration_seconds Duration of HTTP server requests
# TYPE http_server_request_duration_seconds histogram
http_server_request_duration_seconds_bucket{...} 42
...
```

### Swagger / OpenAPI

Interactive API documentation.

```
GET /swagger
```

Opens the Swagger UI with all endpoints documented.

---

## SignalR Hub

### Connection

```
WebSocket: ws://localhost:5000/hubs/notifications?access_token=<JWT>
```

The SignalR hub requires JWT authentication. Pass the access token as a query parameter.

### Client → Server Methods

#### JoinGroup

Subscribe to a notification group.

```json
{ "method": "JoinGroup", "arguments": ["group-name"] }
```

#### LeaveGroup

Unsubscribe from a notification group.

```json
{ "method": "LeaveGroup", "arguments": ["group-name"] }
```

#### SendToGroup

Broadcast a message to all members of a group.

```json
{ "method": "SendToGroup", "arguments": ["group-name", "Hello, group!"] }
```

### Server → Client Events

#### ReceiveMessage

Received when a group message is broadcast.

```json
{ "type": 1, "target": "ReceiveMessage", "arguments": ["user-id", "Hello, group!"] }
```

#### Notification

Received when a targeted notification is sent to the user.

```json
{ "type": 1, "target": "Notification", "arguments": [{ "message": "You have a new alert", "timestamp": "2024-01-15T10:30:00Z" }] }
```

### Automatic Groups

When a user connects, they are automatically added to the group `user:{userId}`. This enables targeted notifications:

```csharp
// Server-side: Send to a specific user
await _notificationService.SendToUserAsync(userId, "Notification", payload, ct);
```

---

## Error Response Format

All API errors follow a consistent format:

```json
{
  "status": 400,
  "error": "Human-readable error message"
}
```

For validation errors:

```json
{
  "status": 400,
  "error": "Validation failed",
  "errors": [
    "Email is required",
    "Password must be at least 8 characters"
  ]
}
```

### HTTP Status Codes

| Code | Meaning | When |
|------|---------|------|
| `200` | Success | Request completed successfully |
| `400` | Bad Request | Validation failed |
| `401` | Unauthorized | Missing/invalid token, wrong credentials |
| `403` | Forbidden | Insufficient role for the endpoint |
| `404` | Not Found | Resource doesn't exist |
| `409` | Conflict | Duplicate resource (e.g., email already registered) |
| `500` | Internal Error | Unexpected server error |

---

## JWT Token Format

### Access Token Claims

| Claim | Key | Example |
|-------|-----|---------|
| Subject (User ID) | `sub` | `550e8400-e29b-41d4-a716-446655440000` |
| Email | `email` | `john@example.com` |
| Role | `role` | `User` |
| Token ID | `jti` | `a1b2c3d4-e5f6-7890-abcd-ef1234567890` |
| Issued At | `iat` | `1705312200` |
| Issuer | `iss` | `MyApp` |
| Audience | `aud` | `MyApp` |
| Expiry | `exp` | `1705315800` (60 minutes from `iat`) |

### Token Lifetime

| Token | Lifetime | Storage |
|-------|----------|---------|
| Access token | 60 minutes | Client memory/localStorage |
| Refresh token | 7 days | Client secure storage, server database |
