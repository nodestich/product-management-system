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

## Run on Kubernetes

The release images are published to `ghcr.io/nodestich/product-management-system/{api,web}`. They are private, and the cluster is never given registry credentials: minikube runs its own container runtime, so the images are pulled on the host, where Docker is already logged in, and handed over with `minikube image load`.

```
docker pull ghcr.io/nodestich/product-management-system/api:feature-ci-cd
docker pull ghcr.io/nodestich/product-management-system/web:feature-ci-cd
minikube image load ghcr.io/nodestich/product-management-system/api:feature-ci-cd
minikube image load ghcr.io/nodestich/product-management-system/web:feature-ci-cd
```

If Docker is not logged in to GHCR yet, run `docker login ghcr.io -u <username>` with a token that has the `read:packages` scope.

The chart in [`ops/helm/product-management`](ops/helm/product-management) deploys the compose stack's shape: PostgreSQL on a 1Gi volume, the migrations as an init container, then the API and the web server. The API Service is named `api` because the web image's nginx proxies to `http://api:8080`, so run one release per namespace.

```
helm install pms ops/helm/product-management --namespace pms --create-namespace
kubectl -n pms port-forward svc/web 8080:80
```

The same URLs as compose are then available, seed data and the demo login included, because the API runs with `ASPNETCORE_ENVIRONMENT=Development`.

| URL | |
| --- | --- |
| <http://localhost:8080> | Application |
| <http://localhost:8080/scalar/v1> | Interactive API documentation |
| <http://localhost:8080/health> | API health, including database connectivity |

After CI publishes a new image, pull and load it again, then `kubectl -n pms rollout restart deploy/pms-product-management-api` (or `deploy/pms-product-management-web`). To reset everything, including the database volume:

```
helm uninstall pms --namespace pms
kubectl delete namespace pms
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

For `stock-level`, `min` and `max` are both inclusive and must be `>= 0`; a negative bound or `min > max` returns 400.

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

## Tests

```
cd backend
dotnet test
```

71 tests across three projects:

- **UnitTests** - 20 tests over the `Product` entity (factory rules, name and price validation) and the hand-written `ProductMapper`.
- **IntegrationTests** - 41 tests that run the API against a `postgres:16-alpine` container started by Testcontainers. They cover all nine endpoints, the 400/404/409 paths, pagination, and a concurrency test that fires 25 parallel creates and asserts the IDs are the contiguous range 100000-100024. Writes log in as the seeded demo user first.
- **Specs** - 10 Gherkin scenarios executed with Reqnroll, kept as executable documentation of the stock and catalogue behaviour.

The `Specs` project shares one container across all scenarios and truncates the `Products` table before each one, so collection parallelism is switched off in `xunit.runner.json`. The Gherkin code-behind is regenerated on every build and is not checked in.

```
cd frontend
npm test -- --watch=false
```

18 unit tests covering the product list (filters, pagination and the re-query after a mutation), both dialog modes and the shell component.

## Continuous integration

`.github/workflows/ci.yml` builds and tests everything **inside Docker images**, so the runner never provides a
.NET or Node toolchain. It runs on pushes to `feature/ci-cd` and on every pull request.

```
test ──┬── sonar
       └── publish   (pushes to feature/ci-cd only)
```

- **test** - builds `Dockerfile.ci` from the commit and runs `dotnet build` + `dotnet test` in Release inside
  that image, then uploads the coverage reports as an artifact. The integration and specs tests start their own
  Postgres container, so `/var/run/docker.sock` is mounted into the test container.
- **sonar** - starts a throwaway SonarQube (`docker compose --profile sonar`), generates a token for that run,
  runs `scripts/analyze-sonar.sh` in the same image, and tears everything down. Because the instance is
  created and destroyed per run there is **no analysis history and no pull-request decoration** - the gate is
  enforced by the job's exit code. CI sets `SONAR_IMAGE` to the public upstream image, because `dhi.io`
  requires `docker login` and a fresh runner has no credentials; both images are SonarQube 26.10.0.133078, so
  the analysis is identical. Locally the hardened image remains the default.
- **publish** - builds the Release images and pushes them to GHCR. Pull requests build them but never push.

Image names and tags (deliberately not `latest` while publishing from a feature branch):

```
ghcr.io/<owner>/product-management-system/api:sha-<short>   and  :feature-ci-cd
ghcr.io/<owner>/product-management-system/web:sha-<short>   and  :feature-ci-cd
```

After the first successful push the two packages are **private**; set them to public under
*Packages -> package -> Package settings* if anonymous pulls are wanted.

To move publishing to the default branch later, change three things in the workflow: `on.push.branches`, the
`publish` job's `if`, and the tag `feature-ci-cd`.

## Static analysis

SonarQube Community Build runs locally in Docker behind the `sonar` profile, so a plain `docker compose up`
is unaffected.

One-time setup:

1. Start the server: `docker compose --profile sonar up -d sonar`
2. Open <http://localhost:9000>, sign in with `admin` / `admin` and change the password.
3. Generate a token at <http://localhost:9000/account/security> (avatar menu > Account > Security), then
   `copy .env.example .env` and paste it in.

Run an analysis:

```
docker compose --profile sonar run --rm sonar-scan
```

That single command starts `sonar-db` and `sonar` if they are not running, then boots a throwaway container
built from `Dockerfile.ci`. The image carries the .NET SDK, so **no .NET installation is needed on the host** -
only Docker. Inside it, the scanner brackets a Release build, runs the unit, integration and specs
tests with coverage, uploads the result and waits for the quality gate. A failing gate makes the command exit
non-zero. Results are at <http://localhost:9000/dashboard?id=product-management-system>.

The coverage files land in `backend/TestResults` so they can be inspected locally (for example with Coverage
Gutters). They are cleared at the start of every run; the analysis history itself lives in the server's
volumes and is unaffected. To wipe that history too, use `docker compose --profile sonar down -v`.

The image is pinned by digest and can be swapped with `SONAR_IMAGE`:

```
SONAR_IMAGE=sonarqube:26.10.0.133078-community docker compose --profile sonar up -d sonar
```

`dhi.io` requires `docker login dhi.io` (any free Docker ID), which is why CI overrides the variable with the
public upstream image. To use the hardened image in CI as well, log in with a token saved as a secret before
starting the stack.

Coverage comes from Coverlet's Cobertura output. Migrations, `Program.cs` and the model snapshot are excluded.

Things worth knowing:

- The container mounts `/var/run/docker.sock` because the integration tests start their own Postgres container.
  That is host-root access - acceptable for a local tool, not for a shared runner.
- Rebuild after code changes: `docker compose --profile sonar run --rm --build sonar-scan`.
- The image is pinned by digest. To bump it, pull the tag and re-pin:
  `docker inspect --format "{{index .RepoDigests 0}}" dhi.io/sonarqube:26-debian-dev`
- Community Build analyses a single branch and cannot comment on pull requests, so the gate is enforced by
  the command's exit code. New code is defined by number of days, not by a reference branch.

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

PostgreSQL has small native images, which keeps `docker compose up` fast, and it expresses the ID requirement directly in the schema through identity and sequence options.

Migrations are *not* portable, though. They are written in the provider's dialect and the column types are declared explicitly (`timestamp with time zone`, `numeric(18,2)`), and the product search index is raw SQL because EF Core cannot model indexes over expressions. Moving to SQL Server would mean `UseNpgsql` -> `UseSqlServer`, re-mapping those column types, regenerating the migrations, and redesigning the search index: SQL Server has no `pg_trgm` equivalent and `LIKE '%term%'` cannot use an index there either, so the choice becomes full-text search (word and prefix matching, not infix) or a sequential scan. The Domain and Application layers would not change at all.

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

### Product search is backed by a trigram index

Search is a case-insensitive "contains" match, which no btree index can serve: the term has no usable prefix and the query wraps the column in `lower()`. `pg_trgm` supplies the missing piece, a GIN index over the exact indexed expression:

```sql
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE INDEX "IX_Products_Name_Trgm" ON "Products" USING gin (lower("Name") gin_trgm_ops);
```

[`ProductConfiguration`](backend/src/ProductManagement.Infrastructure/Persistence/ProductConfiguration.cs) therefore declares no `Name` index — EF Core cannot model an index over an expression, so `AddProductNameTrigramIndex` carries it as raw SQL. Two consequences worth knowing: terms shorter than three characters cannot be split into trigrams and fall back to a sequential scan, and the GIN index is larger and slower to write than the btree it replaced, which is the right trade for a table read far more often than written.

## Assumptions

- `PUT` replaces name and price only. Stock is changed through the two stock endpoints, which keeps concurrent stock updates atomic.
- `DELETE` is permanent; there is no soft delete.
- The unfiltered product list is unpaginated and returns every row; the two filtered lists are paginated.
- Maximum stock of 1000 is an assumption, not part of the specification; it gives the stock-overflow path something to test.
- The six-digit ID space allows 900,000 products. Running out raises an error rather than producing a longer id.
- The compose stack runs the API in Development so the seed data and API documentation are available. A production deployment would use Production and supply secrets through the environment.