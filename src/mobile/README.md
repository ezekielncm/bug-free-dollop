# 📱 MyApp Mobile — Flutter 3, BLoC, GoRouter

The mobile application built with **Flutter 3** using the **BLoC** pattern for state management, **Dio** for HTTP requests, **GoRouter** for navigation, and **signalr_netcore** for real-time notifications.

---

## 🚀 Getting Started

### Prerequisites

- [Flutter SDK 3.24+](https://flutter.dev/docs/get-started/install) (stable channel)
- Android Studio or VS Code with Flutter extensions
- Android emulator or iOS simulator (or a physical device)

### Installation

```bash
cd src/mobile

# Install dependencies
flutter pub get

# Run the app
flutter run

# Or build an APK
flutter build apk --debug
```

### Environment Configuration

The API base URL is configured in `lib/core/constants/app_constants.dart`:

```dart
static const String apiBaseUrl = 'http://10.0.2.2:5000/api';
static const String signalRUrl = 'http://10.0.2.2:5000';
```

> **Note**: `10.0.2.2` is the Android emulator's alias for the host machine's `localhost`. For iOS simulator, use `localhost` directly. For physical devices, use your machine's network IP.

**Override at build time:**
```bash
flutter run --dart-define=API_BASE_URL=http://192.168.1.100:5000/api
```

---

## 📁 Project Structure

```
src/mobile/
├── lib/
│   ├── main.dart                              # App entry point, BLoC providers
│   ├── router.dart                            # GoRouter configuration
│   │
│   ├── core/                                  # Shared utilities & services
│   │   ├── constants/
│   │   │   └── app_constants.dart             # API URLs, storage keys
│   │   ├── network/
│   │   │   └── api_client.dart                # Dio HTTP client with JWT interceptor
│   │   ├── storage/
│   │   │   └── secure_storage.dart            # Flutter Secure Storage wrapper
│   │   └── models/
│   │       └── user_model.dart                # Shared User data model
│   │
│   └── features/                              # Feature modules
│       ├── auth/
│       │   ├── data/
│       │   │   └── auth_repository.dart       # Auth API calls (login, register, etc.)
│       │   └── presentation/
│       │       ├── bloc/
│       │       │   └── auth_bloc.dart         # AuthBloc (events, states, logic)
│       │       └── screens/
│       │           ├── login_screen.dart       # Login UI
│       │           └── register_screen.dart    # Registration UI
│       ├── dashboard/
│       │   ├── data/
│       │   │   └── dashboard_repository.dart  # Dashboard API calls
│       │   └── presentation/
│       │       └── screens/
│       │           └── dashboard_screen.dart   # Main dashboard UI
│       └── notifications/
│           └── notification_service.dart       # SignalR notification service
│
└── pubspec.yaml                               # Flutter dependencies
```

---

## 📦 Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `dio` | ^5.8.0+ | HTTP client with interceptors |
| `flutter_secure_storage` | ^9.2.4 | Encrypted token storage |
| `flutter_bloc` | ^8.1.6 | BLoC state management |
| `go_router` | ^14.8.1 | Declarative routing |
| `signalr_netcore` | ^1.3.4 | SignalR client for .NET |
| `json_serializable` | ^6.9.4 | JSON serialization code generation |
| `freezed` | ^2.5.7 | Immutable data classes |
| `intl` | ^0.20.2 | Internationalization & date formatting |
| `equatable` | — | Value equality for BLoC states/events |

---

## 🏛️ Architecture

The app follows a **feature-first** architecture with the **BLoC pattern**:

```
┌──────────────────────────────────────────────┐
│                  Presentation                 │
│   Screens (Widgets) ←→ BLoC (State Machine)  │
├──────────────────────────────────────────────┤
│                     Data                      │
│   Repository → ApiClient → Backend API        │
├──────────────────────────────────────────────┤
│                     Core                      │
│   Models · Storage · Network · Constants      │
└──────────────────────────────────────────────┘
```

**Data flow**: UI dispatches **Events** to BLoC → BLoC calls **Repository** → Repository calls **ApiClient** → BLoC emits new **State** → UI rebuilds.

---

## 🔑 Core Services

### API Client (`core/network/api_client.dart`)

The Dio HTTP client handles all API communication with automatic JWT token management.

**Configuration:**

| Setting | Value |
|---------|-------|
| Base URL | `AppConstants.apiBaseUrl` |
| Connect timeout | 10 seconds |
| Receive timeout | 30 seconds |
| Content-Type | `application/json` |

**Request interceptor:**
- Reads access token from secure storage
- Attaches `Authorization: Bearer <token>` header

**Error interceptor (401 auto-refresh):**
1. Catches `401` responses
2. Reads refresh token from secure storage
3. Calls `POST /auth/refresh`
4. Saves new tokens to secure storage
5. Retries the original request with new access token
6. On failure: clears storage (triggers re-login)

**Available methods:**

```dart
Future<Response> get<T>(String path, {Map<String, dynamic>? queryParams})
Future<Response> post<T>(String path, {dynamic data})
Future<Response> put<T>(String path, {dynamic data})
Future<Response> delete<T>(String path)
```

### Secure Storage (`core/storage/secure_storage.dart`)

Wraps `FlutterSecureStorage` for encrypted token persistence:

```dart
Future<void> saveAccessToken(String token)
Future<void> saveRefreshToken(String token)
Future<String?> getAccessToken()
Future<String?> getRefreshToken()
Future<void> deleteAll()    // Clears both tokens (logout)
```

**Storage keys:**
- `access_token` — JWT access token
- `refresh_token` — JWT refresh token

### User Model (`core/models/user_model.dart`)

Shared data model used across all features:

```dart
class UserModel {
  final String id;
  final String username;
  final String email;
  final String? firstName;
  final String? lastName;
  final String role;
  final bool isActive;
  final DateTime createdAt;

  String get displayName =>
    (firstName != null && lastName != null)
      ? '$firstName $lastName'
      : username;

  factory UserModel.fromJson(Map<String, dynamic> json);
}
```

---

## 🔐 Authentication Feature

### Auth Repository (`features/auth/data/auth_repository.dart`)

```dart
class AuthRepository {
  Future<UserModel> login(String email, String password)
    // POST /auth/login → saves tokens → returns getMe()

  Future<UserModel> register({
    required String username,
    required String email,
    required String password,
    String? firstName,
    String? lastName,
  })
    // POST /auth/register → saves tokens → returns getMe()

  Future<UserModel> getMe()
    // GET /users/me → returns UserModel

  Future<bool> isLoggedIn()
    // Checks if access token exists in storage

  Future<void> logout()
    // Clears all tokens from storage
}
```

### AuthBloc (`features/auth/presentation/bloc/auth_bloc.dart`)

The central authentication state machine.

**Events:**

| Event | Trigger | Description |
|-------|---------|-------------|
| `AuthCheckStatusEvent` | App startup | Checks if user is already logged in |
| `AuthLoginEvent` | Login form submit | Authenticates with email/password |
| `AuthRegisterEvent` | Register form submit | Creates account and authenticates |
| `AuthLogoutEvent` | Logout button | Clears session |

**States:**

| State | Description | Data |
|-------|-------------|------|
| `AuthInitialState` | App starting, no check done yet | — |
| `AuthLoadingState` | Request in progress | — |
| `AuthenticatedState` | User is logged in | `UserModel user` |
| `UnauthenticatedState` | No valid session | — |
| `AuthErrorState` | Authentication failed | `String message` |

**State Machine:**

```
App Start
  └→ AuthCheckStatusEvent
       └→ AuthLoadingState
            ├→ (has token + getMe succeeds) → AuthenticatedState
            └→ (no token or error) → UnauthenticatedState

Login
  └→ AuthLoginEvent(email, password)
       └→ AuthLoadingState
            ├→ (success) → AuthenticatedState(user)
            └→ (error) → AuthErrorState(message)

Register
  └→ AuthRegisterEvent(username, email, password, ...)
       └→ AuthLoadingState
            ├→ (success) → AuthenticatedState(user)
            └→ (error) → AuthErrorState(message)

Logout
  └→ AuthLogoutEvent
       └→ UnauthenticatedState
```

### Login Screen (`features/auth/presentation/screens/login_screen.dart`)

| Field | Keyboard | Validation |
|-------|----------|-----------|
| Email | `emailAddress` | Must contain `@` |
| Password | `text` (obscured) | Must not be empty |

**Features:**
- Password visibility toggle
- Loading spinner during authentication
- Error message via `SnackBar` on `AuthErrorState`
- Automatic navigation to `/dashboard` on `AuthenticatedState`
- Link to registration page

### Register Screen (`features/auth/presentation/screens/register_screen.dart`)

| Field | Validation | Required |
|-------|-----------|----------|
| Username | Min 3 characters | Yes |
| Email | Valid email format | Yes |
| Password | Min 8 characters | Yes |
| First Name | — | No |
| Last Name | — | No |

**Features:**
- Same UX patterns as login screen
- Automatic navigation to `/dashboard` on success
- Link to login page

---

## 📊 Dashboard Feature

### Dashboard Screen (`features/dashboard/presentation/screens/dashboard_screen.dart`)

**Displays:**

1. **Welcome card**: Shows `user.displayName` and email with role chip
2. **Status row**: Online status (green) and Real-time connection status (purple)
3. **Live notifications**: Scrollable list of last 20 real-time notifications with timestamps

**Actions:**
- **Logout** (AppBar action) → dispatches `AuthLogoutEvent`
- **Pull-to-refresh** → re-fetches user data
- **Auto-connect SignalR** on screen mount
- **Auto-disconnect** on screen dispose

---

## ⚡ Real-Time — Notification Service

### `NotificationService` (`features/notifications/notification_service.dart`)

Manages the SignalR connection for receiving real-time notifications.

```dart
class NotificationService {
  Future<void> connect(String accessToken)
    // Establishes SignalR connection with JWT auth

  void disconnect()
    // Closes the connection

  void onNotification(Function(dynamic) callback)
    // Registers a listener for 'Notification' events

  void onReceiveMessage(Function(dynamic) callback)
    // Registers a listener for 'ReceiveMessage' events
}
```

**Connection details:**
- **Hub URL**: `${AppConstants.signalRUrl}/hubs/notifications`
- **Auth**: JWT access token passed in connection headers
- **Transport**: WebSockets (preferred), with fallback

---

## 🧭 Navigation — GoRouter

### Routes (`lib/router.dart`)

| Path | Screen | Auth Required |
|------|--------|---------------|
| `/login` | LoginScreen | No |
| `/register` | RegisterScreen | No |
| `/dashboard` | DashboardScreen | Yes |

**Navigation pattern**: Uses `context.go('/path')` for declarative navigation throughout the app (replaces the entire navigation stack).

### BLoC-Driven Navigation

The router listens to `AuthBloc` state changes:

```dart
// In screens, navigation happens in BlocListener:
BlocListener<AuthBloc, AuthState>(
  listener: (context, state) {
    if (state is AuthenticatedState) {
      context.go('/dashboard');
    } else if (state is UnauthenticatedState) {
      context.go('/login');
    }
  },
)
```

---

## 🏗️ App Entry Point (`main.dart`)

```dart
void main() {
  // 1. Initialize services
  final secureStorage = SecureStorageService();
  final apiClient = ApiClient(secureStorage: secureStorage);
  final authRepository = AuthRepository(apiClient: apiClient, storage: secureStorage);

  // 2. Run app with BLoC providers
  runApp(
    MultiBlocProvider(
      providers: [
        BlocProvider(create: (_) => AuthBloc(authRepository)
          ..add(AuthCheckStatusEvent())),  // Auto-check auth on startup
      ],
      child: MyApp(),
    ),
  );
}
```

**Startup flow:**
1. Services are instantiated (storage → API client → repositories)
2. BLoC providers wrap the app
3. `AuthCheckStatusEvent` is dispatched immediately
4. App renders login or dashboard based on auth state

---

## 🛠️ Development Commands

```bash
# Install/update dependencies
flutter pub get

# Run on connected device/emulator
flutter run

# Hot reload (press 'r' in terminal)
# Hot restart (press 'R' in terminal)

# Static analysis
flutter analyze

# Run unit tests
flutter test

# Build debug APK
flutter build apk --debug

# Build release APK
flutter build apk --release

# Build iOS (macOS only)
flutter build ios --release

# Generate code (json_serializable, freezed)
flutter pub run build_runner build --delete-conflicting-outputs
```

---

## 📱 Platform-Specific Notes

### Android

- **Minimum SDK**: Defined in `android/app/build.gradle`
- **Internet permission**: Required in `AndroidManifest.xml` (default)
- **Emulator localhost**: Use `10.0.2.2` to reach host machine

### iOS

- **Minimum version**: Defined in `ios/Podfile`
- **App Transport Security**: May need exception for HTTP in development
- **Simulator localhost**: Use `localhost` or `127.0.0.1` directly

---

## 🐞 Troubleshooting

| Problem | Solution |
|---------|----------|
| **Connection refused** | Verify API URL in `app_constants.dart`; use `10.0.2.2` for Android emulator |
| **Login fails** | Check API is running: `curl http://localhost:5000/health` |
| **Token refresh loop** | Clear app data or call `secureStorage.deleteAll()` |
| **SignalR won't connect** | Ensure `signalRUrl` is correct and API supports WebSockets |
| **Build errors** | Run `flutter clean && flutter pub get` |
| **Code generation issues** | Run `flutter pub run build_runner build --delete-conflicting-outputs` |
| **Android emulator slow** | Enable hardware acceleration in Android Studio |
