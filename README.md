# Products API

RESTful API for managing products, built with ASP.NET Core (.NET 10), EF Core (code-first) and SQL Server LocalDB, following Clean Architecture principles.

## Solution Structure

- `src/Products.Domain` — entities and domain exceptions (no dependencies)
- `src/Products.Application` — DTOs, service interfaces/implementation, FluentValidation validators
- `src/Products.Infrastructure` — EF Core DbContext, repository, migrations, seeding
- `src/Products.Api` — controllers, exception middleware, DI composition root
- `tests/Products.UnitTests` — xUnit + NSubstitute + FluentAssertions (33 tests)
- `tests/Products.IntegrationTests` — WebApplicationFactory against LocalDB (21 tests)

Dependency direction: Api -> Application <- Infrastructure, Application -> Domain. The Application layer defines `IProductRepository`; Infrastructure implements it. Controllers are thin — all business logic lives in `ProductService`.

## Endpoints (9)

| Method | Route | Description |
|--------|-------|-------------|
| GET    | `/api/products` | List all products (stock included) |
| POST   | `/api/products` | Create a product (201 + Location header) |
| GET    | `/api/products/{id}` | Get a product by id |
| PUT    | `/api/products/{id}` | Update a product |
| DELETE | `/api/products/{id}` | Delete a product (204) |
| POST   | `/api/products/{id}/decrement-stock/{quantity}` | Atomically decrement stock |
| POST   | `/api/products/{id}/add-to-stock/{quantity}` | Atomically increment stock |
| GET    | `/api/products/search?name={name}` | Partial, case-insensitive name search |
| GET    | `/api/products/stock-level?min={min}&max={max}` | Products within a stock range |

Swagger UI is available at `/swagger` in Development.

## Key Architectural Decisions

### Unique 6-digit ID generation (distributed-safe)
Product IDs come from a SQL Server SEQUENCE (`ProductIdSequence`, range 100000-999999, NO CYCLE), bound via `HasDefaultValueSql("NEXT VALUE FOR ProductIdSequence")`. The database allocates each number exactly once, so multiple application instances can never generate duplicates — no application-level coordination needed.

Trade-off: the range holds 900,000 IDs. When exhausted, SQL Server raises an error (surfaced as `ProductIdExhaustedException`). This is an accepted operational limit imposed by the 6-digit requirement. Sequences may produce gaps (e.g. on rollback), which is harmless for identifiers.

### Concurrency-safe stock operations
Stock changes use `ExecuteUpdateAsync` — a single atomic UPDATE where the availability check is part of the WHERE clause:

```sql
UPDATE Products SET Stock = Stock - @qty WHERE Id = @id AND Stock >= @qty
```

There is no read-modify-write window, so concurrent decrements can never oversell. When 0 rows are affected the service distinguishes not found (404) from insufficient stock (422) with a follow-up existence check. An integration test fires 20 parallel decrements against a stock of 10 and asserts exactly 10 succeed and stock ends at 0. A database CHECK constraint (`Stock >= 0`) provides defence in depth.

### Error handling — custom exceptions to ProblemDetails (RFC 7807)

| Exception | Status |
|-----------|--------|
| `ProductNotFoundException` | 404 |
| `InsufficientStockException` | 422 |
| `InvalidStockOperationException` | 400 |
| any other `DomainException` | 400 |
| unexpected exceptions | 500 (message sanitized, details logged) |

All error responses use `application/problem+json`.

### Validation
FluentValidation validators run through an MVC action filter, returning 400 ValidationProblemDetails with per-field errors. Rules: Name required/max 200 chars, Description max 1000 chars, Price > 0, Stock >= 0. Route/query invariants (positive quantities, valid min/max range) are enforced in the service.

### EF Core practices
- `AsNoTracking` on all read queries
- `ExecuteUpdateAsync`/`ExecuteDeleteAsync` for set-based writes
- Fully asynchronous data access with CancellationToken propagation
- `EnableRetryOnFailure` for transient fault resilience
- Migrations + seeding applied automatically at startup in Development only

### Integration tests use real SQL Server (LocalDB)
The ID SEQUENCE and `ExecuteUpdateAsync` semantics are SQL Server features; an in-memory provider would not exercise the real behaviour. Each test run creates an isolated `ProductsDb_Tests_{guid}` database and drops it afterwards.

## Prerequisites

- .NET 10 SDK
- SQL Server LocalDB (bundled with Visual Studio; verify with `sqllocaldb info MSSQLLocalDB`)

## Running Locally

```powershell
cd ProductsApi
dotnet run --project src/Products.Api
```

On first run (Development) the database `ProductsDb` is created, migrated and seeded with 6 sample products automatically. Browse to the shown localhost URL + `/swagger`.

Connection string lives in `src/Products.Api/appsettings.json` (`ConnectionStrings:ProductsDb`).

### Managing migrations manually

```powershell
dotnet tool install --global dotnet-ef
dotnet ef database update --project src/Products.Infrastructure --startup-project src/Products.Api
dotnet ef migrations add <Name> --project src/Products.Infrastructure --startup-project src/Products.Api --output-dir Persistence/Migrations
```

## Running Tests

```powershell
dotnet test                                  # all 54 tests
dotnet test tests/Products.UnitTests         # unit tests only (no DB needed)
dotnet test tests/Products.IntegrationTests  # requires LocalDB
```

## Assumptions

- Stock quantities in the increment/decrement endpoints must be positive integers (quantity <= 0 returns 400).
- `stock-level` defaults: min=0, max=int.MaxValue; min > max or negative values return 400.
- Name search requires a non-empty `name` query parameter (missing returns 400).
- Timestamps are UTC; `UpdatedAtUtc` is null until the first update.
