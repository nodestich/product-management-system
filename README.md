# Product Management System

- `backend/` - ASP.NET Core 10 API, EF Core 10, PostgreSQL 16
- `frontend/` - Angular 22 application using Angular Material

The API exposes product CRUD, stock adjustments, name search and stock-level filtering. The UI consumes those endpoints directly.

## Run with Docker

```
docker compose up --build
```

Open <http://localhost:8080>.

Compose starts PostgreSQL, runs the migrations in a one-shot `migrate` container, waits for it to finish, then starts the API and the web server. Only the web server is published; the API and database are reachable only inside the compose network.

| URL | |
| --- | --- |
| <http://localhost:8080> | Application |
| <http://localhost:8080/scalar/v1> | Interactive API documentation |
| <http://localhost:8080/health> | API health, including database connectivity |

Seed data is inserted the first time the API starts. To reset everything, including the database volume:

```
docker compose down -v
```
## Requirements

- .NET SDK 10.0
- Node.js 22
- Docker Desktop (one-command run and integration tests)
- PostgreSQL 16 if you skip Docker

## Run manually

1. Start PostgreSQL 16 and create a database named `productmanagement`.
2. Set the connection string in `backend/src/ProductManagement.Api/appsettings.Development.json`, key `ConnectionStrings:Default`.
3. Apply the migrations:

```
cd backend
dotnet tool restore
dotnet ef database update --project src/ProductManagement.Infrastructure --startup-project src/ProductManagement.Api
```

4. Start the API:

```
dotnet run --project src/ProductManagement.Api
```

5. Start the frontend in a second terminal:

```
cd frontend
npm ci
npm start
```

The API listens on <http://localhost:5218> and the UI on <http://localhost:4200>. The Angular dev server proxies `/api`, `/scalar` and `/openapi` to the API, so the browser only ever talks to one origin and no CORS configuration is required.

## Endpoints

| Method | Route | Success | Errors |
| --- | --- | --- | --- |
| GET | `/api/products` | 200 | |
| GET | `/api/products/{id}` | 200 | 404 |
| POST | `/api/products` | 201 + `Location` | 400 |
| PUT | `/api/products/{id}` | 200 | 400, 404 |
| DELETE | `/api/products/{id}` | 204 | 404 |
| POST | `/api/products/{id}/decrement-stock/{quantity}` | 200 | 400, 404, 409 |
| POST | `/api/products/{id}/add-to-stock/{quantity}` | 200 | 400, 404, 409 |
| GET | `/api/products/search?name={name}` | 200 | 400 |
| GET | `/api/products/stock-level?min={min}&max={max}` | 200 | 400 |

The two filtered list endpoints are paginated and accept `page` (default 1) and `pageSize` (default 10, maximum 50). They return an envelope rather than a bare array:

```json
{
  "items": [ { "id": 100004, "name": "Mouse pad", "price": 12.00, "stock": 3, ... } ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 14,
  "totalPages": 2,
  "hasPrevious": false,
  "hasNext": true
}
```

`GET /api/products` is deliberately not paginated and still returns a plain array.

Stock changes return the updated product. Errors use RFC 7807 `ProblemDetails` with a `traceId` that matches the API log.

## Authentication

Writes require a signed-in user; the four `GET` endpoints stay anonymous.

| Endpoint | |
| --- | --- |
| `POST /api/auth/login` | `{ "email": "...", "password": "..." }`, sets the auth cookie |
| `POST /api/auth/logout` | clears the cookie |
| `GET /api/auth/me` | returns the current user |

Seeded demo user: `admin@example.com` / `Admin123!`. In Scalar, run the login request once and the following requests reuse the cookie.

Authentication is ASP.NET Core Identity with a cookie, no JWT. The cookie is `HttpOnly` and `SameSite=Strict`, so the browser sends it automatically, JavaScript cannot read it, and the frontend stores no token. `SameSite=Strict` is also the CSRF mitigation: the cookie is never attached to a cross-site request, and the app is served from a single origin.

The persistence side derives from `IdentityUserContext<ApplicationUser, Guid>` instead of `IdentityDbContext`. There are no roles in this application, so the role tables would sit unused; this keeps the migration to users, claims, logins and tokens only.

## Product model

| Field | Type | Rules |
| --- | --- | --- |
| `id` | int | 100000-999999, assigned by the database |
| `name` | string | required, 2-100 characters, trimmed |
| `price` | decimal | `>= 0`, stored as `numeric(18,2)` |
| `stock` | int | 0-1000, always returned |
| `createdAt` / `updatedAt` | timestamp with time zone | UTC, set by the API |

## Seed data

Inserted only when the table is empty, in Development:

| ID | Name | Price | Stock |
| --- | --- | --- | --- |
| 100000 | Monitor 32 inches | 249.99 | 12 |
| 100001 | Mechanical keyboard | 189.99 | 8 |
| 100002 | Wireless mouse | 39.99 | 25 |
| 100003 | HDMI cable | 4.75 | 100 |
| 100004 | Mouse pad | 12.00 | 3 |

## Tests

```
cd backend
dotnet test
```

69 tests across three projects:

- **UnitTests** - 20 tests over the `Product` entity (factory rules, name and price validation) and the hand-written `ProductMapper`.
- **IntegrationTests** - 39 tests that run the API against a `postgres:16-alpine` container started by Testcontainers. They cover all nine endpoints, the 400/404/409 paths, pagination, and a concurrency test that fires 25 parallel creates and asserts the IDs are the contiguous range 100000-100024. Writes log in as the seeded demo user first.
- **Specs** - 10 Gherkin scenarios executed with Reqnroll, kept as executable documentation of the stock and catalogue behaviour.

The `Specs` project shares one container across all scenarios and truncates the `Products` table before each one, so collection parallelism is switched off in `xunit.runner.json`. The Gherkin code-behind is regenerated on every build and is not checked in.

```
cd frontend
npm test -- --watch=false
```

13 unit tests covering the product list (including the paginated mode), both dialog modes and the shell component.

## Decisions

### Product IDs are assigned by the database

The requirement is a unique six-digit identifier that stays unique when several instances run at once. The application never generates it. `Id` is an identity column backed by a PostgreSQL sequence:

```sql
"Id" integer GENERATED BY DEFAULT AS IDENTITY (START WITH 100000) ...
```

`nextval()` on a sequence is atomic, so concurrent inserts each receive a distinct value without any coordination between API instances. Sequence values are also never reused: if an insert fails, that number is consumed and skipped.

The result is that gaps are possible but duplicates are not.

### Stock changes are single atomic statements

Decrementing reads and writes in one statement rather than loading the entity, validating in C# and saving:

```csharp
.Where(p => p.Id == id && p.Stock >= quantity)
.ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - quantity) ...)
```

Two instances decrementing the last unit cannot oversell. The second statement waits on the row lock, re-checks Stock >= quantity against the committed row, matches nothing and returns 409.

### PostgreSQL instead of SQL Server

EF Core migrations are provider-agnostic, so the choice mostly affects whoever has to run this. PostgreSQL has small native images, which keeps `docker compose up` fast.

It also expresses the ID requirement directly in the schema through identity and sequence options. Swapping to SQL Server would be a `UseNpgsql` -> `UseSqlServer` change plus regenerated migrations, because the code avoids raw SQL and provider-specific types.

### No repository layer

`DbContext` is already a unit of work and `DbSet<T>` is already a repository. `IApplicationDbContext` exists only so the Application layer does not depend on Infrastructure, which keeps the service testable.

### Validation

Two different things can go wrong with a request, and they are handled in different places.

A malformed body, a wrong type or a non-numeric id never reaches the application code. ASP.NET model binding rejects it first and returns a 400 with an `errors` object keyed by field.

Everything else is a business rule, and those live on the entity itself. `Product.Create` and `Product.Update` throw `DomainException` for an empty name, a negative price or an out-of-range value, and the API turns that into a 400 with a single `detail` message. Keeping the rules next to the data they protect means there is one place to change them.

The catch is that these errors are not attached to a field, and only the first broken rule is reported per request.

### The frontend uses relative URLs

nginx proxies `/api`, `/scalar` and `/openapi` to the API, so the built bundle contains `/api/products` rather than a host name. The same image works on any host and port, there is no CORS configuration, and the Angular dev server proxy makes development use the identical code path.

### Pagination is offset-based and only on the filtered lists

`search` and `stock-level` accept `page` and `pageSize` and return a `PagedResult<T>`. The unfiltered list stays a plain array, because it is the one call a reviewer uses to confirm the API works and an envelope there would only add noise.

Filtering, ordering, counting and slicing all run in SQL: the query stays an `IQueryable` and is only materialised after `Skip`/`Take`, so at most `pageSize` rows are read. `CountAsync` issues a separate `SELECT COUNT(*)` and materialises nothing.

Both queries order by `updatedAt` and then by `id`. The second key matters: seeded and bulk-created products share a timestamp, and without a tiebreaker the database may return tied rows in a different order for each page, so a row can appear twice or not at all.

Offset pagination costs a second round trip for the total and re-evaluates the filter twice. That is the price of knowing `totalPages` up front; keyset pagination would avoid it but cannot jump to page 4.

## Assumptions

- `PUT` replaces name and price only. Stock is changed through the two stock endpoints, which keeps concurrent stock updates atomic.
- `DELETE` is permanent; there is no soft delete.
- Lists are unpaginated and return every matching row.
- Maximum stock of 1000 is an assumption, not part of the specification; it gives the stock-overflow path something to test.
- The six-digit ID space allows 900,000 products. Running out raises an error rather than producing a longer id.
- The compose stack runs the API in Development so the seed data and API documentation are available. A production deployment would use Production and supply secrets through the environment.