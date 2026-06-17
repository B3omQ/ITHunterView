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

### Frontend Tests (TypeScript with Jest)

- **unit/**: Component and utility function tests
- **integration/**: Cross-component feature tests (auth flows, etc.)
- **e2e/**: Full application end-to-end tests
- **fixtures/**: Mock data and test utilities

Tests use:
- [Jest](https://jestjs.io/) - Testing framework
- [React Testing Library](https://testing-library.com/react) - Component testing
- [Playwright or Cypress](https://playwright.dev/) - E2E testing (optional)

## Getting Started

### Backend
```bash
cd backend
dotnet restore
dotnet test
```

### Frontend
```bash
cd Frontend
npm install
npm test
```

## Development

- Backend: .NET 10.0
- Frontend: Next.js with TypeScript
- API: RESTful API with JWT authentication
