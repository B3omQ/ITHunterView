# ITHunterview

Interview Preparation Platform - A comprehensive solution for interview preparation and practice

## Project Structure

```
ITHunterview/
├── backend/                              # .NET Backend
│   ├── ITHunterview.Domain/             # Domain entities and business logic
│   │   └── Entities/
│   │       ├── User.cs
│   │       └── RefreshToken.cs
│   │
│   ├── ITHunterview.Service/            # Services, repositories, and use cases
│   │   ├── Config/
│   │   ├── Constant/
│   │   ├── DTOs/
│   │   ├── Infrastructure/
│   │   ├── Interface/
│   │   ├── Service/
│   │   ├── UseCase/
│   │   └── Utils/
│   │
│   ├── ITHunterview.WebAPI/             # ASP.NET Core API controllers
│   │   ├── Controllers/
│   │   ├── Middlewares/
│   │   └── Program.cs
│   │
│   ├── ITHunterview.Domain.Tests/       # Domain unit tests
│   │   └── Entities/
│   │
│   ├── ITHunterview.Service.Tests/      # Service layer unit tests
│   │   ├── UseCase/
│   │   ├── Repository/
│   │   ├── Utils/
│   │   └── TestFixtures/
│   │
│   └── ITHunterview.WebAPI.Tests/       # WebAPI integration & controller tests
│       ├── Controllers/
│       ├── Integration/
│       └── TestFixtures/
│
└── Frontend/                             # Next.js Frontend
    ├── src/
    │   ├── app/                         # App pages and layout
    │   ├── components/                  # React components
    │   ├── hooks/                       # Custom React hooks
    │   ├── lib/                         # Utilities and helpers
    │   ├── store/                       # State management
    │   ├── types/                       # TypeScript types
    │   └── api/                         # API integration
    │
    ├── __tests__/                       # Frontend tests
    │   ├── unit/                        # Unit tests
    │   │   ├── components/              # Component tests
    │   │   │   └── ui/                  # UI component tests
    │   │   ├── hooks/                   # Hook tests
    │   │   ├── utils/                   # Utility function tests
    │   │   └── lib/                     # Library function tests
    │   │
    │   ├── integration/                 # Integration tests
    │   │   └── auth/                    # Authentication flow tests
    │   │
    │   ├── e2e/                         # End-to-end tests
    │   │
    │   └── fixtures/                    # Mock data and test utilities
    │
    ├── package.json
    ├── next.config.ts
    ├── tsconfig.json
    └── jest.config.js
```

## Test Structure

### Backend Tests (.NET with xUnit)

- **ITHunterview.Domain.Tests**: Domain entity tests and business logic validation
- **ITHunterview.Service.Tests**: Service layer, repository, and use case tests
- **ITHunterview.WebAPI.Tests**: API controller and integration tests

Tests use:
- [xUnit](https://xunit.net/) - Testing framework
- [Moq](https://github.com/moq/moq4) - Mocking library
- [FluentAssertions](https://fluentassertions.com/) - Assertion syntax

### Frontend Tests (TypeScript with Vitest)

- **unit/**: Component and utility function tests
- **integration/**: Cross-component feature tests (auth flows, etc.)
- **e2e/**: Full application end-to-end tests
- **fixtures/**: Mock data and test utilities

Tests use:
- [Vitest](https://vitest.dev/) - Testing framework
- [React Testing Library](https://testing-library.com/react) - Component testing
- [Playwright or Cypress](https://playwright.dev/) - E2E testing (optional)

## Reproducible Setup

The repository includes a coding-agent/developer harness that restores locked
dependencies, checks runtimes, runs the verified baseline, and records work
state between sessions.

On Windows:

```powershell
.\init.ps1 -Install
.\scripts\verify.ps1
.\scripts\verify.ps1 -Mode Full
```

On Bash:

```bash
bash ./init.sh --install
bash ./scripts/verify.sh quick all
bash ./scripts/verify.sh full all
```

Read `AGENTS.md`, `agent-progress.md`, and `feature_list.json` before beginning
long-running work. Detailed verification behavior is documented in
`docs/verification.md`.

## Getting Started

### Backend
```bash
cd backend
dotnet restore
dotnet test
```

### Frontend
```bash
cd frontend
npm ci
npm test
```

## Development

- Backend: .NET 10.0
- Frontend: Next.js with TypeScript
- API: RESTful API with JWT authentication
