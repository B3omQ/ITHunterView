# Backend Coding Rules
Use these rules when adding or changing backend features. They are based on the current source code, not the README.

## Core Rules

- Keep dependency direction: `ITHunterview.WebAPI -> ITHunterview.Service -> ITHunterview.Domain`.
- Declare new persisted entities in `ITHunterview.Domain/Entities/`.
- Declare new domain enums in `ITHunterview.Domain/Enums/`.
- Put business actions in use cases: `ITHunterview.Service/Interface/UseCase/` and `ITHunterview.Service/UseCase/`.
- Put database queries and EF Core access in `ITHunterview.Service/Infrastructure/Persistence/`.
- Controllers may use use case interfaces only. Do not inject repositories or `ITHunterviewContext` into controllers.
- Controllers handle HTTP request/response concerns: route, body/query/cookie extraction, auth attributes, response wrapping.
- Exceptions may be thrown from any layer. `ExceptionMiddleware` maps known exceptions to HTTP responses.
- Register repositories and use cases in `ITHunterview.Service/Config/ServiceCollectionExtensions.cs`.

## New Feature Flow

```text
1. Entity
   ITHunterview.Domain/Entities/{Entity}.cs

2. Database mapping
   ITHunterview.Service/Infrastructure/Persistence/ITHunterviewContext.cs

3. Migration
   dotnet ef migrations add {MigrationName} \
     --project ITHunterview.Service \
     --startup-project ITHunterview.WebAPI \
     --context ITHunterviewContext \
     --output-dir Infrastructure/Persistence/Migrations

4. Apply migration
   dotnet ef database update \
     --project ITHunterview.Service \
     --startup-project ITHunterview.WebAPI \
     --context ITHunterviewContext

5. DTOs
   ITHunterview.Service/DTOs/{Feature}/

6. Persistence interface
   ITHunterview.Service/Interface/Persistence/I{Entity}Repository.cs

7. Persistence implementation
   ITHunterview.Service/Infrastructure/Persistence/{Entity}Repository.cs

8. Use case interface
   ITHunterview.Service/Interface/UseCase/I{Feature}UseCase.cs

9. Use case implementation
   ITHunterview.Service/UseCase/{Feature}UseCase.cs

10. DI registration
    ITHunterview.Service/Config/ServiceCollectionExtensions.cs

11. Controller endpoint
    ITHunterview.WebAPI/Controllers/{Feature}Controller.cs
```

## Layer Responsibilities

### Domain

- Contains entities, enums, and base domain types.
- Does not contain request/response DTOs.
- Does not contain database query logic.
- Does not contain controller or HTTP concerns.

### Service

- Contains use cases, DTOs, repository interfaces, repository implementations, EF Core context, migrations, config helpers, and utilities.
- Use cases orchestrate business actions.
- Use cases call repository interfaces for data access.
- Repository implementations contain EF Core queries and `SaveChangesAsync()`.
- External integrations should use `Interface/Service/` for contracts and `Service/` for implementations.

### WebAPI

- Controllers receive HTTP input and call use cases.
- Controllers wrap successful responses with `ResponseBase<T>` where appropriate.
- Controllers may read cookies, route values, query params, body DTOs, and `ClaimsPrincipal`.
- Controllers should not contain business rules or EF Core queries.
- Middleware handles cross-cutting HTTP concerns.

## Exception Handling

```text
Any layer
    |
    v
throw Exception
    |
    v
ExceptionMiddleware
    |
    v
ResponseBase<object?>
```

Current exception mapping:

```text
KeyNotFoundException        -> 404 NOT_FOUND
ArgumentException           -> 400 BAD_REQUEST
UnauthorizedAccessException -> 401 UNAUTHORIZED
Other Exception             -> 500 INTERNAL_ERROR
```

Do not catch exceptions in controllers just to convert them to responses. Let `ExceptionMiddleware` handle that unless the controller has a specific HTTP concern to handle.

## Controller Pattern

```csharp
[Route("api/items")]
[ApiController]
public class ItemController : ControllerBase
{
    private readonly IItemUseCase _itemUseCase;

    public ItemController(IItemUseCase itemUseCase)
    {
        _itemUseCase = itemUseCase;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ResponseBase<ItemDto>>> Get(Guid id)
    {
        var item = await _itemUseCase.GetById(id);
        return new ResponseBase<ItemDto>(item);
    }
}
```

## Use Case Pattern

```csharp
public class ItemUseCase : IItemUseCase
{
    private readonly IItemRepository _itemRepository;

    public ItemUseCase(IItemRepository itemRepository)
    {
        _itemRepository = itemRepository;
    }

    public async Task<ItemDto> GetById(Guid id)
    {
        var item = await _itemRepository.GetById(id);
        if (item == null)
        {
            throw new KeyNotFoundException("Item not found");
        }

        return new ItemDto
        {
            Id = item.Id,
            Name = item.Name
        };
    }
}
```

## Repository Pattern

```csharp
public class ItemRepository : IItemRepository
{
    private readonly ITHunterviewContext _context;

    public ItemRepository(ITHunterviewContext context)
    {
        _context = context;
    }

    public Task<Item?> GetById(Guid id)
    {
        return _context.Items.FirstOrDefaultAsync(x => x.Id == id);
    }
}
```

## DI Registration

Register new implementations in `ITHunterview.Service/Config/ServiceCollectionExtensions.cs`.

```csharp
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    services.AddScoped<IItemRepository, ItemRepository>();
    services.AddScoped<IItemUseCase, ItemUseCase>();

    return services;
}
```

## Migration Notes

- Create a migration after changing entities or `ITHunterviewContext` mappings.
- Apply the migration locally after creating it.
- Keep generated migration files under `ITHunterview.Service/Infrastructure/Persistence/Migrations/`.
- The application also runs `context.Database.Migrate()` on startup outside the `Testing` environment.
