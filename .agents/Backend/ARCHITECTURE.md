# Backend Architecture

ITHunterview backend is a .NET 10 REST API organized as a 3-layer architecture. This document describes structure and dependency flow only. Coding conventions live in `CODING_RULES.md`.

## Project Structure

```text
backend/
|-- ITHunterview_V1.slnx
|-- ITHunterview.Domain/
|   |-- BaseEntity.cs
|   |-- Entities/
|   |   |-- RefreshToken.cs
|   |   `-- User.cs
|   `-- Enums/
|       `-- UserRole.cs
|-- ITHunterview.Service/
|   |-- Config/
|   |-- Constant/
|   |-- DTOs/
|   |-- Infrastructure/Persistence/
|   |-- Interface/
|   |   |-- Persistence/
|   |   |-- Service/
|   |   `-- UseCase/
|   |-- Service/
|   |-- UseCase/
|   `-- Utils/
`-- ITHunterview.WebAPI/
    |-- Controllers/
    |-- Middlewares/
    |-- Program.cs
    `-- appsettings.json
```

## Layers

```text
+------------------------------------------+
| ITHunterview.WebAPI                         |
| HTTP endpoints, middleware, app startup  |
+---------------------+--------------------+
                      |
                      | references
                      v
+------------------------------------------+
| ITHunterview.Service                        |
| DTOs, use cases, interfaces, DI, EF Core |
+---------------------+--------------------+
                      |
                      | references
                      v
+------------------------------------------+
| ITHunterview.Domain                         |
| Entities, enums, shared domain base types|
+------------------------------------------+
```

### ITHunterview.Domain

Owns business data shapes and shared domain types.

- `BaseEntity.cs`: common audit fields.
- `Entities/`: persisted domain entities such as `User` and `RefreshToken`.
- `Enums/`: domain enum values such as `UserRole`.

This project has no project references.

### ITHunterview.Service

Owns application behavior and technical implementations used by the API.

- `DTOs/`: request/response contracts and wrappers such as `ResponseBase<T>` and `PageResult<T>`.
- `Interface/Persistence/`: repository abstractions such as `IUserRepository`.
- `Interface/UseCase/`: application entry-point contracts such as `IAuthUseCase`.
- `Interface/Service/`: external-service contracts when integrations are added.
- `UseCase/`: business workflows such as auth and user operations.
- `Infrastructure/Persistence/`: EF Core `ITHunterviewContext`, migrations, and repository implementations.
- `Config/`: DI registration plus JWT/CORS configuration helpers.
- `Utils/`: small shared helpers for claims, password hashing, and cookies.

This project references `ITHunterview.Domain`.

### ITHunterview.WebAPI

Owns HTTP concerns and application startup.

- `Controllers/`: translate HTTP requests into use case calls and return HTTP responses.
- `Middlewares/`: cross-cutting HTTP pipeline behavior such as exception handling.
- `Program.cs`: service registration, EF migration execution, Swagger/OpenAPI, CORS, auth, and middleware pipeline.
- `appsettings.json`: runtime configuration such as connection strings and JWT settings.

This project references `ITHunterview.Service`.

## Dependency Direction

```text
Allowed:

ITHunterview.WebAPI -----> ITHunterview.Service -----> ITHunterview.Domain

Not allowed:

ITHunterview.Domain -----> ITHunterview.Service
ITHunterview.Domain -----> ITHunterview.WebAPI
ITHunterview.Service ----> ITHunterview.WebAPI
```

Project references enforce the same direction:

```text
ITHunterview.WebAPI.csproj
`-- ProjectReference: ITHunterview.Service

ITHunterview.Service.csproj
`-- ProjectReference: ITHunterview.Domain

ITHunterview.Domain.csproj
`-- no project references
```

## Request Flow

```text
HTTP request
    |
    v
Controller
    | calls
    v
IUseCase
    | implemented by
    v
UseCase
    | uses
    |-- IRepository -> Repository -> ITHunterviewContext -> PostgreSQL
    `-- IService     -> External service implementation
    |
    v
DTO / ResponseBase<T>
    |
    v
HTTP response
```

## Runtime Composition

```text
Program.cs
|-- AddControllers()
|-- AddCorsConfig()
|-- AddJwtConfig()
|-- AddDbContext<ITHunterviewContext>(UseNpgsql)
|-- AddApplicationServices()
|-- Swagger/OpenAPI
`-- middleware pipeline
    |-- ExceptionMiddleware
    |-- CORS
    |-- Authentication
    |-- Authorization
    `-- MapControllers()
```

`AddApplicationServices()` is the main DI composition point for repositories and use cases inside `ITHunterview.Service/Config/ServiceCollectionExtensions.cs`.

## Persistence

```text
ITHunterview.Service/Infrastructure/Persistence/
|-- ITHunterviewContext.cs
|-- UserRepository.cs
|-- TokenRepository.cs
`-- Migrations/
```

The API configures `ITHunterviewContext` with PostgreSQL through `UseNpgsql(...)` in `Program.cs`. Migrations are stored beside the context under `Infrastructure/Persistence/Migrations`.
