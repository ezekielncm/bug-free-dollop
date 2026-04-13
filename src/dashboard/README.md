# 🌐 MyApp Dashboard — Next.js 14, TypeScript, App Router

The admin web dashboard built with **Next.js 14**, **TypeScript**, and **Tailwind CSS**. It provides authentication, user management, real-time notifications via SignalR, and a responsive admin interface.

---

## 🚀 Getting Started

### Prerequisites

- [Node.js 20+](https://nodejs.org/)
- npm (included with Node.js)

### Installation

```bash
cd src/dashboard

# Install dependencies
npm install

# Configure environment
cp .env.example .env.local

# Start development server
npm run dev
```

The dashboard starts at **http://localhost:3000**.

### Using Docker

From the repository root:

```bash
docker compose up -d dashboard
```

---

## 📁 Project Structure

```
src/dashboard/
├── app/                                # Next.js App Router pages
│   ├── layout.tsx                      # Root layout (metadata, global CSS, toast)
│   ├── page.tsx                        # Landing page (/)
│   ├── globals.css                     # Tailwind CSS imports + global styles
│   ├── auth/
│   │   ├── login/
│   │   │   └── page.tsx                # Login form with email/password
│   │   └── register/
│   │       └── page.tsx                # Registration form
│   └── dashboard/
│       ├── layout.tsx                  # Protected layout with sidebar navigation
│       ├── page.tsx                    # Dashboard overview with live notifications
│       ├── users/
│       │   └── page.tsx                # User management table
│       └── settings/
│           └── page.tsx                # Application settings (placeholder)
├── hooks/
│   └── useSignalR.ts                   # React hook for SignalR real-time connection
├── lib/
│   ├── api-client.ts                   # Axios instance with JWT interceptors
│   ├── services.ts                     # API service functions (auth, users)
│   └── store.ts                        # Zustand auth state store with persistence
├── types/
│   └── index.ts                        # TypeScript interfaces (User, Auth, etc.)
├── .env.example                        # Environment variable template
├── next.config.ts                      # Next.js configuration (standalone output)
├── tailwind.config.js                  # Tailwind CSS configuration with custom colors
├── postcss.config.js                   # PostCSS configuration
├── tsconfig.json                       # TypeScript configuration
└── package.json                        # Dependencies and scripts
```

---

## 📦 Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `next` | 14.2.29 | React framework with App Router |
| `react` | ^18.3.1 | UI library |
| `typescript` | ^5 | Type safety |
| `axios` | ^1.8.4 | HTTP client |
| `zustand` | ^5.0.3 | State management |
| `@microsoft/signalr` | ^8.0.7 | Real-time communication |
| `react-hook-form` | ^7.55.0 | Form state management |
| `zod` | ^3.24.2 | Schema validation |
| `@hookform/resolvers` | ^3.10.0 | Zod ↔ react-hook-form bridge |
| `tailwindcss` | ^3.4.17 | Utility-first CSS |
| `lucide-react` | ^0.503.0 | Icon library |
| `react-hot-toast` | ^2.5.2 | Toast notifications |
| `date-fns` | ^4.1.0 | Date formatting utilities |

---

## ⚙️ Configuration

### Environment Variables

Create `.env.local` from `.env.example`:

| Variable | Description | Default |
|----------|-------------|---------|
| `NEXT_PUBLIC_API_URL` | Backend API base URL | `http://localhost:5000` |
| `NEXT_PUBLIC_SIGNALR_URL` | SignalR hub base URL | `http://localhost:5000` |

### `next.config.ts`

```typescript
{
  output: 'standalone',   // Optimized for Docker deployment
  env: {
    NEXT_PUBLIC_API_URL: 'http://localhost:5000',
    NEXT_PUBLIC_SIGNALR_URL: 'http://localhost:5000'
  }
}
```

### Tailwind Theme

Custom primary color palette (sky blue):

```javascript
// tailwind.config.js
colors: {
  primary: {
    50: '#f0f9ff',
    100: '#e0f2fe',
    500: '#0ea5e9',
    600: '#0284c7',
    700: '#0369a1',
  }
}
```

---

## 🔐 Authentication

### Auth Flow

```
1. User submits login/register form
2. API returns { accessToken, refreshToken, userId, email, role }
3. Tokens + user data stored in Zustand (persisted to localStorage)
4. All subsequent API calls include Authorization: Bearer <accessToken>
5. On 401 response → automatic refresh token rotation
6. On refresh failure → logout & redirect to /auth/login
```

### Zustand Auth Store (`lib/store.ts`)

```typescript
interface AuthState {
  user: User | null
  accessToken: string | null
  refreshToken: string | null
  setAuth: (user: User, accessToken: string, refreshToken: string) => void
  logout: () => void
  isAuthenticated: () => boolean
}
```

**Persistence**: State is persisted to `localStorage` under the key `auth-storage` using Zustand's `persist` middleware. The store only writes when values actually change — no duplicate writes.

### Axios Interceptors (`lib/api-client.ts`)

**Request interceptor:**
- Reads access token from the Zustand-persisted `localStorage` entry
- Attaches `Authorization: Bearer <token>` header to every request

**Response interceptor (401 handling):**
1. Catches `401 Unauthorized` responses
2. Reads refresh token from `localStorage`
3. Calls `POST /api/auth/refresh` with the refresh token
4. On success: patches the Zustand `localStorage` store with new tokens
5. Retries the original failed request with the new access token
6. On failure: clears auth state and redirects to `/auth/login`

> The interceptor reads and patches the `localStorage` entry directly to avoid circular dependencies with the Zustand store import.

---

## 📄 Pages

### Landing Page (`/`)

Home page with navigation links to login and dashboard.

### Login (`/auth/login`)

| Field | Validation | Type |
|-------|-----------|------|
| Email | Required, valid email format | `email` |
| Password | Required | `password` |

- **Library**: `react-hook-form` with `zod` resolver
- **On success**: Sets auth state → redirects to `/dashboard`
- **On error**: Shows toast notification with error message
- **Navigation**: Link to `/auth/register`

### Register (`/auth/register`)

| Field | Validation | Type |
|-------|-----------|------|
| Username | Required, min 3 characters | `text` |
| Email | Required, valid email format | `email` |
| Password | Required, min 8 characters | `password` |
| First Name | Optional | `text` |
| Last Name | Optional | `text` |

- **On success**: Sets auth state → redirects to `/dashboard`
- **Navigation**: Link to `/auth/login`

### Dashboard Layout (`/dashboard/*`)

**Protected route**: Checks `isAuthenticated()` on mount; redirects to `/auth/login` if not authenticated.

**Sidebar navigation:**
- 📊 Overview → `/dashboard`
- 👥 Users → `/dashboard/users`
- ⚙️ Settings → `/dashboard/settings`
- 🚪 Logout button (clears auth state, redirects to `/auth/login`)

### Dashboard Overview (`/dashboard`)

Displays:
- **Welcome message**: "Welcome back, {firstName || username}!"
- **Stats grid**: Online status, user role, real-time connection status
- **Live notifications**: Real-time notifications received via SignalR (last 20 messages with timestamps)

### Users (`/dashboard/users`)

Displays a users table with columns:
- Username
- Email
- Role (badge)
- Status — Active (green) / Inactive (red)
- Created date

> Currently shows the authenticated user's profile. Extend `userService` for admin-level user listing.

### Settings (`/dashboard/settings`)

Placeholder page for application settings. Ready to be extended with user preferences, theme settings, etc.

---

## ⚡ Real-Time — `useSignalR` Hook

The `useSignalR` hook (`hooks/useSignalR.ts`) manages the SignalR connection lifecycle:

```typescript
const { connection, connect, disconnect, on, invoke } = useSignalR()
```

### API

| Method | Parameters | Description |
|--------|-----------|-------------|
| `connect()` | — | Establishes connection to the SignalR hub |
| `disconnect()` | — | Closes the connection |
| `on<T>(method, callback)` | `method: string, callback: (data: T) => void` | Subscribe to server events; returns unsubscribe function |
| `invoke(method, ...args)` | `method: string, ...args: any[]` | Call a server method |
| `connection` | — | Raw `HubConnection` reference |

### Connection Details

- **Hub URL**: `${NEXT_PUBLIC_SIGNALR_URL}/hubs/notifications`
- **Auth**: Access token factory reads from Zustand store
- **Auto-reconnect**: Enabled via `withAutomaticReconnect()`
- **Logging**: Warning level
- **Lifecycle**: Auto-connects on mount (when authenticated), auto-disconnects on unmount

### Usage Example

```typescript
// In a React component
const { on, invoke } = useSignalR()

useEffect(() => {
  const unsubscribe = on<{ message: string; timestamp: string }>(
    'Notification',
    (data) => {
      console.log('Received:', data.message)
    }
  )
  return unsubscribe
}, [on])

// Join a group
invoke('JoinGroup', 'announcements')
```

---

## 🔗 API Services (`lib/services.ts`)

### `authService`

```typescript
authService.login(email: string, password: string): Promise<AuthResponse>
authService.register(username, email, password, firstName?, lastName?): Promise<AuthResponse>
authService.refresh(refreshToken: string): Promise<AuthResponse>
```

### `userService`

```typescript
userService.getMe(): Promise<User>
userService.getById(id: string): Promise<User>
```

---

## 📝 TypeScript Types (`types/index.ts`)

```typescript
interface User {
  id: string
  username: string
  email: string
  firstName?: string
  lastName?: string
  role: string
  isActive: boolean
  createdAt: string
}

interface AuthResponse {
  accessToken: string
  refreshToken: string
  userId: string
  email: string
  role: string
}

interface LoginRequest {
  email: string
  password: string
}

interface RegisterRequest {
  username: string
  email: string
  password: string
  firstName?: string
  lastName?: string
}
```

---

## 🛠️ Available Scripts

```bash
# Development server with hot reload
npm run dev

# TypeScript type checking
npm run type-check

# ESLint linting
npm run lint

# Production build
npm run build

# Start production server
npm start
```

---

## 🐳 Docker Build

The dashboard uses a multi-stage Dockerfile (`docker/dashboard/Dockerfile`):

| Stage | Base Image | Purpose |
|-------|-----------|---------|
| `deps` | `node:20-alpine` | Install npm dependencies |
| `builder` | `node:20-alpine` | Build Next.js application |
| `runner` | `node:20-alpine` | Production runtime (non-root user) |

**Output**: Standalone Next.js server running as non-root `nextjs` user on port 3000.

```bash
# Build manually
docker build -f docker/dashboard/Dockerfile -t myapp-dashboard src/dashboard/

# Run
docker run -p 3000:3000 -e NEXT_PUBLIC_API_URL=http://api:5000 myapp-dashboard
```

---

## 🐞 Troubleshooting

| Problem | Solution |
|---------|----------|
| **CORS errors** | Ensure API's `AllowedOrigins` includes `http://localhost:3000` |
| **API connection refused** | Verify `NEXT_PUBLIC_API_URL` points to running API |
| **Login redirects back** | Check browser console for 401 errors; clear `auth-storage` from localStorage |
| **SignalR won't connect** | Verify `NEXT_PUBLIC_SIGNALR_URL` and that API is running |
| **Stale auth state** | Clear `auth-storage` key in browser localStorage |
| **Build fails** | Run `npm run type-check` to identify TypeScript errors |
