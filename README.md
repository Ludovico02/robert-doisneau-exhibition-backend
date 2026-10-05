# Robert Doisneau Museum — Backend

[![CI](https://github.com/Ludovico02/robert-doisneau-exhibition-backend/actions/workflows/ci.yml/badge.svg)](https://github.com/Ludovico02/robert-doisneau-exhibition-backend/actions/workflows/ci.yml)

Backend for a museum website dedicated to the photographer Robert Doisneau: user accounts, exhibition browsing, ticket purchases and a photo gallery. Built with **.NET 10 minimal APIs**, **PostgreSQL**, **Dapper** and **JWT authentication stored in an HttpOnly cookie**, as three small services sharing one database.

Originally a team project from my ITS course; this repository is the hardened version, with a schema, tests and CI added (see [Project history](#project-history)).

## What it does

| Service | HTTPS port | Routes |
| --- | ---: | --- |
| Login API | 7198 | `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/logout` |
| Cart API | 7224 | `GET /api/exhibitions`, `POST /api/checkout/buy`, `GET /api/tickets/my-tickets` |
| Gallery API | 7164 | `GET /api/gallery`, `GET /api/gallery/{id}` |

Login and Cart share the `DBDoisneau` database, because tickets reference users through a foreign key. The Gallery API uses the same PostgreSQL instance.

## Design highlights

**Checkout never oversells.** A purchase runs in a single transaction. Each exhibition row is locked with `SELECT ... FOR UPDATE`, and locks are always taken in ascending exhibition-ID order, so two carts containing the same exhibitions can't deadlock each other. If any item is sold out or missing, the whole order rolls back. The price always comes from the database, never from the client, and `CHECK` constraints on the table (`availability BETWEEN 0 AND total_capacity`, `price >= 0`) are a second line of defence behind the application logic.

**Authentication.**
- Passwords are hashed with BCrypt. Registration rejects passwords over BCrypt's 72-byte input limit.
- Login runs a dummy hash verification when the username doesn't exist, so response time doesn't reveal which usernames are registered. Wrong username and wrong password return the same `401`.
- The JWT is set as an `HttpOnly`, `Secure` cookie, so browser JavaScript can't read it.
- Login, registration and gallery routes are rate limited per IP.

**Input validation.** Usernames are alphanumeric (3–50 characters), emails are parsed and normalized, and the cart is validated (positive IDs, quantity 1–10 per exhibition, at most 20 exhibitions per order, duplicate lines merged). Unique-constraint races at registration are caught and returned as `409`.

**Other.** All SQL is parameterized. Ticket codes come from a cryptographic RNG. Secrets live in .NET user-secrets, not in committed config (and CI scans for leaked secrets), and the services refuse to start with a missing or short (<32 characters) JWT key.

## Verification

Checked on a fresh clone (October 2026):

| Check | Result |
| --- | --- |
| Setup script on a fresh clone | Succeeded for all three APIs |
| Release build | 0 errors |
| Test suite (`dotnet test`, Release) | All passing, 0 failed, 0 skipped |
| GitHub Actions (build + test) | Green |
| Live endpoint run via `curl` (register, login, logout, exhibitions, purchase, my-tickets, gallery) | All returned `200 OK` |
| Login cookie flags | `Secure`, `HttpOnly`, `SameSite=None` |
| 20 simultaneous purchase requests against an exhibition with 5 seats left | 5 succeeded, 15 sold out, availability ended at 0, exactly 5 tickets issued |

## Run it locally

Requirements: .NET 10 SDK and Docker. The companion frontend is a separate private repository (see below); the APIs can be exercised on their own through Swagger UI or `curl`.

1. **Start PostgreSQL** from the repository root:

   ```sh
   docker compose up -d
   ```

   Compose creates the `DBDoisneau` database from `db/schema.sql` and `db/seed.sql` and publishes it on `127.0.0.1:5433`. The default password is the development value `change-me`; to change it, copy `.env.example` to `.env` and set `DB_PASSWORD`.

   > If you get a name or port conflict (container `doisneau-db` or port 5433 already in use), stop the old container or change the host port in `docker-compose.yml` and in the connection strings.

2. **Configure user-secrets** for all three services (database password and one JWT signing key shared by the services):

   ```powershell
   .\scripts\setup-dev.ps1 -DbPassword "change-me"
   ```

   ```sh
   ./scripts/setup-dev.sh "change-me"
   ```

3. **Run the APIs**, each in its own terminal:

   ```sh
   dotnet run --project RobertDoisneau.WebApi/RobertDoisneau.Login.WebApi/RobertDoisneau.Login.WebApi.csproj --launch-profile https
   dotnet run --project RobertDoisneau.WebApi/RobertDoisneau.Cart.V2.WebApi/RobertDoisneau.Cart.V2.WebApi.csproj --launch-profile https
   dotnet run --project RobertDoisneau.WebApi/RobertDoisneau.WebApi.GalleryAPI/RobertDoisneau.WebApi.GalleryAPI.csproj --launch-profile https
   ```

   Run `dotnet dev-certs https --trust` once so browsers accept the local certificate. In Development, OpenAPI is served at `/openapi/v1.json` and Swagger UI at `/swagger`.

CORS allows `http://127.0.0.1:5500` and `http://localhost:5500` by default (the frontend was served with VS Code Live Server). Change `Cors:AllowedOrigins` in each service's `appsettings.json` for another origin.

### Quick try with curl

```sh
curl -k -c jar.txt -H "Content-Type: application/json" \
  -d '{"username":"demo1","email":"demo1@example.com","password":"a-long-password"}' \
  https://localhost:7198/api/auth/register

curl -k -c jar.txt -H "Content-Type: application/json" \
  -d '{"username":"demo1","password":"a-long-password"}' \
  https://localhost:7198/api/auth/login

curl -k https://localhost:7224/api/exhibitions
curl -k -b jar.txt -H "Content-Type: application/json" \
  -d '[{"ticketCategoryId":1,"quantity":2}]' \
  https://localhost:7224/api/checkout/buy
curl -k -b jar.txt https://localhost:7224/api/tickets/my-tickets
```

(Field names are as defined in the request models; check Swagger if one differs.)

## Tests

```sh
dotnet test RobertDoisneau.WebApi/RobertDoisneau.WebApi.slnx
```

Docker must be running: the integration tests start a disposable PostgreSQL container with Testcontainers and apply the repository's `db/schema.sql`.

- **Login API** (integration, via `WebApplicationFactory`): registration and login success, the `HttpOnly` cookie flag, wrong password, duplicate username and email, validation errors, login rate limiting.
- **Cart API**: unit tests for cart normalization and PostgreSQL integration tests for the checkout transaction.

CI (`.github/workflows/ci.yml`) runs a gitleaks secret scan over the full history, then builds and tests the solution in Release, on every push and pull request.

## Known limitations

- `SameSite=None` is needed for a frontend on a different origin, which means CSRF deserves consideration. JSON-only endpoints plus CORS preflight mitigate it; `Lax`/`Strict` would be better if frontend and API shared a site.
- Logout only deletes the cookie; the stateless JWT stays valid until it expires (30 minutes). There is no token revocation and no refresh tokens.
- All three services share one symmetric JWT signing key, and Login and Cart share one database. Both are deliberate simplifications for a small project.
- Rate limits are per IP, so users behind one NAT share a quota.
- Registration reports "username taken" and "email taken" separately, which allows account enumeration. This is a deliberate UX trade-off; login responses are uniform.
- No payment integration and no per-user purchase caps.

**What I'd do in production:** revoke tokens with a distributed blocklist (for example Redis, keyed by the token's `jti` with a TTL equal to its remaining lifetime) checked in the auth middleware; asymmetric signing keys so only the Login service can issue tokens; separate databases or schemas per service.

## Project history

This started as a team project during my ITS course:

- **Ludovico (me):** Login API (registration, BCrypt, JWT in an HttpOnly cookie, logout, login rate limiting), the transactional checkout and purchased-tickets access, security, and the registration, cart and tickets pages of the frontend.
- **Alessio Mondini:** Gallery API, the exhibitions listing, the first Docker/database setup, and the gallery and ticket-shop pages.
- **Francesco and Daniele:** the remaining frontend work.

In the second year of course, after what I learnt after submitting the project, during my internship and with further self-studied material, I hardened the project with the help of an AI coding assistant (GitHub Copilot) to have a very quick way to polish what was previously done. I wrote a prompt and reviewed the results: secrets moved to user-secrets, SQL schema and seed data, input validation, timing-safe login, deadlock-safe lock ordering in checkout, extra rate limiting, the test suites and the CI workflow. The commit history shows which changes were made at each stage and where copilot came into play.

The frontend repository is private and not linked here.
