# Robert Doisneau Museum Backend

Robert Doisneau is a museum website backend built with .NET 10 minimal APIs, PostgreSQL, Dapper, and JWT authentication in an HttpOnly cookie. It provides account registration and login, exhibition browsing and ticket purchases, access to purchased tickets, and a photo gallery API.

## My contributions

- **Security layer:** Login and registration protections include BCrypt password hashing, input limits, timing-equalized login verification, and per-IP rate limiting.
- **HttpOnly cookie JWTs:** Authentication tokens are attached as `HttpOnly`, `Secure` cookies rather than exposed to browser JavaScript or stored in local storage.
- **Concurrency-safe checkout:** Purchase processing uses PostgreSQL transactions and `SELECT ... FOR UPDATE` row locks (acquired in sorted exhibition-ID order) to prevent concurrent purchases from overselling ticket stock.
- **Purchased-ticket access:** Authenticated users can retrieve tickets associated with their account.

## Architecture

| Service | HTTPS port | Routes |
| --- | ---: | --- |
| Login API | 7198 | `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/logout` |
| Cart API | 7224 | `GET /api/exhibitions`, `POST /api/checkout/buy`, `GET /api/tickets/my-tickets` |
| Gallery API | 7164 | `GET /api/gallery`, `GET /api/gallery/{id}` |

Login and Cart intentionally share the `DBDoisneau` database. Purchased tickets reference users through a foreign key. The Gallery API uses the same local PostgreSQL instance for gallery data.

## Team and contributions

Contribution areas were reviewed against Git history and confirmed by the project owner.

- **Ludovico** — Login API, including registration, BCrypt password hashing, JWT authentication in an HttpOnly cookie, logout, and login rate limiting; Cart checkout and purchased-ticket access; security layer; frontend registration, cart, and purchased-ticket pages.
- **Alessio Mondini** — Gallery API, exhibitions listing in the Cart API, Docker/database setup, and frontend gallery and ticket-shop pages.
- **Francesco and Daniele** — the remaining frontend work.

The frontend repository is private and is not linked here.

## Interesting parts

- **Concurrency-safe checkout:** purchases run in one database transaction, lock exhibition rows with `SELECT ... FOR UPDATE`, acquire locks in sorted exhibition-ID order, and roll back as a unit if any requested item cannot be fulfilled.
- **Server-side pricing and stock constraints:** the price comes from PostgreSQL, not the client. Database `CHECK` constraints provide a final guard on capacity, availability, and non-negative prices.
- **Cookie-based JWT authentication:** tokens are stored in an HttpOnly cookie rather than browser local storage, reducing exposure to JavaScript-based token theft.
- **Password protection:** passwords are hashed with BCrypt, registration rejects values above BCrypt's 72-byte limit, and login performs a dummy hash verification for unknown usernames to reduce timing-based username enumeration.
- **Rate limiting:** login and registration have per-IP limits; gallery requests are limited per IP.
- **Parameterized SQL:** Dapper queries pass user-provided values as parameters.

## Run it locally

Requirements: .NET 10 SDK and Docker Desktop.

1. Start PostgreSQL from the repository root:

   ```powershell
   docker compose up -d
   ```

   Compose initializes the `DBDoisneau` database with `db/schema.sql` and `db/seed.sql`. By default, PostgreSQL uses the local development password `REDACTED`. To use a different password, set `DB_PASSWORD` in a local `.env` file based on `.env.example`, then use the same value in the next step.

2. Configure local user-secrets for all three services. On Windows:

   ```powershell
   .\scripts\setup-dev.ps1 -DbPassword "REDACTED"
   ```

   On macOS or Linux:

   ```sh
   ./scripts/setup-dev.sh "REDACTED"
   ```

   Pass the same database password configured for Compose. The setup script generates one JWT signing key shared by all three services and stores both values in .NET user-secrets, not in committed configuration.

3. Run each API from the repository root, in a separate terminal:

   ```sh
   dotnet run --project RobertDoisneau.WebApi/RobertDoisneau.Login.WebApi/RobertDoisneau.Login.WebApi.csproj --launch-profile https
   dotnet run --project RobertDoisneau.WebApi/RobertDoisneau.Cart.V2.WebApi/RobertDoisneau.Cart.V2.WebApi.csproj --launch-profile https
   dotnet run --project RobertDoisneau.WebApi/RobertDoisneau.WebApi.GalleryAPI/RobertDoisneau.WebApi.GalleryAPI.csproj --launch-profile https
   ```

The configured CORS origins are `http://127.0.0.1:5500` and `http://localhost:5500`; edit `Cors:AllowedOrigins` in each service's `appsettings.json` if your frontend uses a different origin. In Development, OpenAPI documents are available at `/openapi/v1.json` and Swagger UI at `/swagger`.

## Tests

Run the Login API integration tests:

```sh
dotnet test RobertDoisneau.WebApi/RobertDoisneau.Login.WebApi.Tests/RobertDoisneau.Login.WebApi.Tests.csproj
```

The test host runs in memory through `WebApplicationFactory`; the tests use a disposable PostgreSQL container, so Docker must be running. The suite covers successful registration and verifies that successful login returns an `HttpOnly` `X-Access-Token` cookie.

Run the existing full solution test suite:

```sh
dotnet test RobertDoisneau.WebApi/RobertDoisneau.WebApi.slnx
```

Cart tests include unit coverage for request normalization and PostgreSQL integration tests using Testcontainers. Docker must be running for the integration tests.

## Known limitations

- `SameSite=None` cookies require CSRF consideration. JSON-only endpoints and CORS preflight provide mitigation, but `Lax` or `Strict` would be preferable if the frontend and API shared a site.
- Logout deletes the browser cookie; the stateless JWT remains valid until it expires.
- JWT revocation is not currently implemented.
- Rate limits are per IP, so users behind a shared NAT can affect one another; all attempts count toward the limit.
- All three services share a symmetric JWT signing key.
- There are no refresh tokens, payment integration, or per-user ticket purchase caps.
- Login and Cart intentionally share one database.

## Production considerations

In production, immediate JWT revocation can be implemented with a distributed blocklist such as Redis. On logout or account-security events, store the token's `jti` (or a hash of the token) with a time-to-live no longer than the token's remaining lifetime. Authentication middleware should check that blocklist for each validated token, with a defined cache-availability policy and monitoring. This keeps the API stateless with respect to token contents while allowing all service instances to honor revocation consistently. The current implementation only deletes the browser cookie; the JWT itself remains valid until expiration.
