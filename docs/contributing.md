# 🤝 Contributing Guidelines

Thank you for considering contributing to MyApp! This document provides guidelines and instructions for contributing.

---

## Getting Started

### 1. Fork & Clone

```bash
git clone https://github.com/your-username/bug-free-dollop.git
cd bug-free-dollop
```

### 2. Set Up Development Environment

```bash
# Copy environment template
cp .env.example .env

# Start infrastructure services
docker compose up -d

# Or start everything including API and Dashboard
docker compose up -d
```

### 3. Verify Everything Works

```bash
# Check services are healthy
docker compose ps

# API health
curl http://localhost:5000/health

# Dashboard
open http://localhost:3000
```

---

## Development Workflow

### Branch Naming

| Type | Pattern | Example |
|------|---------|---------|
| Feature | `feature/<description>` | `feature/user-profile-page` |
| Bug fix | `fix/<description>` | `fix/login-redirect-loop` |
| Documentation | `docs/<description>` | `docs/api-reference-update` |
| Refactor | `refactor/<description>` | `refactor/auth-service` |
| Chore | `chore/<description>` | `chore/update-dependencies` |

### Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]
```

**Types:**
| Type | Description |
|------|-------------|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation changes |
| `style` | Formatting, no logic change |
| `refactor` | Code restructuring, no behavior change |
| `test` | Adding or updating tests |
| `chore` | Build, CI, dependency changes |

**Scopes:** `api`, `dashboard`, `mobile`, `docker`, `ci`, `docs`

**Examples:**
```
feat(api): add user profile update endpoint
fix(dashboard): resolve login redirect loop on token expiry
docs(api): update authentication endpoint examples
refactor(mobile): extract shared widgets to core module
chore(ci): update Flutter to 3.24.x in CI workflow
```

---

## Code Style

### API (C# / .NET)

- Follow [Microsoft C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use `var` when the type is obvious
- Use records for DTOs and commands
- Keep controllers thin — business logic belongs in Application layer handlers
- Use `async/await` consistently
- Add XML documentation for public interfaces

### Dashboard (TypeScript / Next.js)

- Follow the existing ESLint configuration
- Use TypeScript strict mode
- Use `interface` for object types, `type` for unions/intersections
- Use named exports
- Use `react-hook-form` + `zod` for all forms
- Use Zustand for global state

### Mobile (Dart / Flutter)

- Follow [Effective Dart](https://dart.dev/effective-dart) guidelines
- Use BLoC pattern for state management
- Keep screens in `features/<name>/presentation/screens/`
- Keep data models in `core/models/` when shared across features
- Use `context.go()` for navigation (GoRouter)

---

## Adding a New Feature

### Backend Feature

1. **Domain entity** (if needed):
   ```
   src/api/MyApp.Domain/Entities/YourEntity.cs
   src/api/MyApp.Domain/Interfaces/IYourEntityRepository.cs
   ```

2. **Application command/query**:
   ```
   src/api/MyApp.Application/Features/YourFeature/
   ├── Commands/
   │   └── CreateYourEntityCommand.cs    # Command + Handler + Validator
   ├── Queries/
   │   └── GetYourEntityQuery.cs         # Query + Handler
   └── DTOs/
       └── YourEntityDto.cs
   ```

3. **Infrastructure implementation**:
   ```
   src/api/MyApp.Infrastructure/Data/Configurations/YourEntityConfiguration.cs
   src/api/MyApp.Infrastructure/Data/Repositories/YourEntityRepository.cs
   ```

4. **API controller**:
   ```
   src/api/MyApp.API/Controllers/YourEntityController.cs
   ```

5. **Register in DI** (if new repository/service):
   ```
   src/api/MyApp.Infrastructure/DependencyInjection.cs
   ```

### Dashboard Feature

1. **Add page**:
   ```
   src/dashboard/app/dashboard/your-feature/page.tsx
   ```

2. **Add API service** (in `lib/services.ts`):
   ```typescript
   export const yourFeatureService = {
     getAll: () => apiClient.get('/your-feature'),
     create: (data: CreateRequest) => apiClient.post('/your-feature', data),
   }
   ```

3. **Add types** (in `types/index.ts`):
   ```typescript
   export interface YourEntity {
     id: string
     name: string
     // ...
   }
   ```

4. **Add navigation link** (in `app/dashboard/layout.tsx`)

### Mobile Feature

1. **Create feature structure**:
   ```
   src/mobile/lib/features/your_feature/
   ├── data/
   │   └── your_feature_repository.dart
   └── presentation/
       ├── bloc/
       │   └── your_feature_bloc.dart
       └── screens/
           └── your_feature_screen.dart
   ```

2. **Add BLoC provider** (in `main.dart`)

3. **Add route** (in `router.dart`)

---

## Testing

### Before Submitting a PR

```bash
# API tests
cd src/api
dotnet test MyApp.sln --verbosity normal

# Dashboard checks
cd src/dashboard
npm run type-check
npm run lint

# Mobile checks
cd src/mobile
flutter analyze
flutter test

# Docker build validation
docker compose build
```

### Writing Tests

**API:**
- Unit tests for domain entities and application handlers
- Use mocks (e.g., Moq) for infrastructure dependencies
- Place tests in `MyApp.Domain.Tests/` and `MyApp.Application.Tests/`

**Dashboard:**
- Component tests with React Testing Library
- Integration tests for API service functions

**Mobile:**
- Unit tests for BLoC (events → states)
- Widget tests for screen components

---

## Pull Request Process

### Creating a PR

1. Ensure all tests pass locally
2. Push your branch and create a PR against `main`
3. Fill in the PR template with:
   - **Description**: What changed and why
   - **Type of change**: Feature, bug fix, etc.
   - **Testing**: How you tested the changes
   - **Checklist**: All items checked

### PR Review Criteria

- [ ] Code follows the project's style guidelines
- [ ] Changes are minimal and focused
- [ ] New features have corresponding tests
- [ ] Documentation is updated if needed
- [ ] No unnecessary dependencies added
- [ ] CI pipeline passes (build, test, lint)
- [ ] No security vulnerabilities introduced

### After Merge

- PRs to `main` trigger the CD pipeline (build + push images)
- Tagged releases (`v*.*.*`) deploy to production

---

## Release Process

### Versioning

We follow [Semantic Versioning](https://semver.org/):

| Version | When |
|---------|------|
| `MAJOR` (v2.0.0) | Breaking API changes |
| `MINOR` (v1.1.0) | New features, backward compatible |
| `PATCH` (v1.0.1) | Bug fixes, backward compatible |

### Creating a Release

```bash
# Tag the release
git tag -a v1.0.0 -m "Release v1.0.0: Initial stable release"
git push origin v1.0.0
```

This triggers the CD pipeline to:
1. Build Docker images
2. Push to GitHub Container Registry with semver tags
3. Deploy to production

---

## Reporting Issues

When reporting a bug, please include:

1. **Description**: Clear description of the issue
2. **Steps to reproduce**: Minimal steps to trigger the bug
3. **Expected behavior**: What should happen
4. **Actual behavior**: What actually happens
5. **Environment**: OS, browser/device, Docker version
6. **Logs**: Relevant log output (check Seq, Docker logs)
7. **Screenshots**: If applicable

---

## Questions?

- Check existing [issues](https://github.com/ezekielncm/bug-free-dollop/issues) for similar questions
- Open a new issue with the `question` label
- Review the [documentation](../docs/) for architecture and setup guides
