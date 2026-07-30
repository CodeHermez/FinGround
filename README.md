# FinGround

A secure, high-performance API that simulates a banking environment, allowing third-party apps to check balances or initiate transfers.

Built with ASP.NET Core 10 and Clean Architecture (`Domain` / `Application` / `Infrastructure` / `API`), CQRS via MediatR, JWT auth, and PostgreSQL via EF Core.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

- [PostgreSQL](https://www.postgresql.org/download/) running locally (or reachable from your machine)

- [`dotnet-ef`](https://learn.microsoft.com/ef/core/cli/dotnet) CLI tool  install with:
  
  ```
  dotnet tool install --global dotnet-ef
  ```

## 1. Clone and restore

```
git clone https://github.com/CodeHermez/FinGround.git
cd FinGround
dotnet restore FinGround.slnx
```

All NuGet packages listed below are restored automatically by this command, there is nothing else to install package wise except the `dotnet-ef` global tool above.

## 2. Create the database

Create an empty PostgreSQL database named **`heliumdb`** using whichever client (I used pgAdmin 4) you have available, e.g. with `psql`:

```
psql -U postgres -c "CREATE DATABASE heliumdb;"
```

or with pgAdmin / any other PostgreSQL GUI, just create a new database named `heliumdb`.

Then open [`API/appsettings.json`](API/appsettings.json) and update the `ConnectionStrings:DefaultConnection` value to match your local PostgreSQL credentials (host, port, username, password):

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=heliumdb;Username=<your-user>;Password=<your-password>;SSL Mode=Disable"
}
```

Also check the `Jwt` section in the same file  `SecretKey`, `Issuer`, `Audience`, and `ExpiryMinutes` are already filled in with working sandbox defaults, but you can change `SecretKey` to your own value (must be at least 32 characters).

## 3. Apply migrations

The initial schema migration (`InitialCreate`) is already checked into [`Infrastructure/Persistence/Migrations`](Infrastructure/Persistence/Migrations). Apply it to your new database:

```
dotnet ef database update --project Infrastructure --startup-project API
```

This creates the `Users`, `Accounts`, `Transactions`, and `AuditLogs` tables (plus EF's `__EFMigrationsHistory` tracking table).

If you ever change an entity in `Domain/Entities`, generate a new migration with:

```
dotnet ef migrations add <MigrationName> --project Infrastructure --startup-project API -o Persistence/Migrations
```

## 4. Run the API

```
cd API
dotnet run
```

On startup, the app automatically runs any pending migrations and seeds demo data (see below). If the database is empty, you don't really need a separate seed command.

The API listens on **http://localhost:5000** (configured via `Urls` in `appsettings.json`); you can change it if that port is already running another process, or kill the process already running (there will be a conflict or contention causing the sandbox not to run) on that port using cmd on admin mode, you can look up the cmd commands for that.

## 5. API docs

Once running, open the interactive Swagger UI at:

**http://localhost:5000/swagger**

The raw OpenAPI document is served at `http://localhost:5000/swagger/v1/swagger.json`.

All endpoints require a JWT bearer token except `/api/health` and `/api/health/detailed`. To authorize in Swagger UI: call `/api/auth/login`, copy the `token` value from the response, click **Authorize** at the top of the page, and enter `Bearer <token>`.

## Demo accounts

A pre-seeded admin account and two funded accounts are created automatically the first time you run the app against an empty DB:

| Credential                               | Role      | Notes                             |
| ---------------------------------------- | --------- | --------------------------------- |
| `demo@banking-sandbox.dev` / `Demo1234!` | **Admin** | Can call `/api/admin/*` endpoints |

Pre-seeded bank accounts (owned by the demo data, visible via `/api/accounts`):

| Account number | Type     | Balance    |
| -------------- | -------- | ---------- |
| `CHK-000001`   | Checking | R5,000.00  |
| `SAV-000001`   | Savings  | R12,500.00 |

A R2,500 transfer from checking to savings is also seeded, dated 7 days before the first run, so the transaction/audit-log/reconciliation endpoints return meaningful data immediately.

To create your own login instead of using the demo one, call `POST /api/auth/register` with `{ "email": "...", "password": "...", "fullName": "..." }` a self-registered users get the `User` role (not `Admin`) and cannot call `/api/admin/*`. Bank accounts aren't owned by individual users in this sandbox, so any authenticated user (including a self-registered one) can see and use the pre-seeded `CHK-000001`/`SAV-000001` accounts via `GET /api/accounts`, or create a new one via `POST /api/accounts`.

## Endpoints overview

| Area         | Route                                                                                               | Notes                                            |
| ------------ | --------------------------------------------------------------------------------------------------- | ------------------------------------------------ |
| Auth         | `POST /api/auth/register`                                                                           | 3 registrations / 10 min per IP                  |
| Auth         | `POST /api/auth/login`                                                                              | 5 attempts / 60 sec per IP (via sliding window) |
| Accounts     | `GET /api/accounts`, `GET /api/accounts/{id}`, `POST /api/accounts`                                 |                                                  |
| Accounts     | `POST /api/accounts/{id}/deposit`, `POST /api/accounts/{id}/withdraw`                               |                                                  |
| Transactions | `GET /api/transactions/account/{accountId}`, `POST /api/transactions/transfer`                      |                                                  |
| Audit logs   | `GET /api/auditlogs`, `GET /api/auditlogs/accounts/{accountId}`                                     |                                                  |
| Audit logs   | `GET /api/auditlogs/reconcile/all`, `GET /api/auditlogs/accounts/{accountId}/reconcile`             | Replays audit trail and verifies stored balances |
| Admin        | `GET /api/admin/users`, `GET /api/admin/users/{userId}`                                             | Admin role only                                  |
| Admin        | `POST /api/admin/users/{userId}/unlock`                                                             | Unlocks an account after 5 failed logins         |
| Admin        | `GET /api/admin/accounts/{accountId}/transactions`, `GET /api/admin/accounts/{accountId}/auditlogs` |                                                  |
| Health       | `GET /api/health`, `GET /api/health/detailed`                                                       | No auth required                                 |

Five consecutive failed login attempts lock a user's account for 15 minutes; an admin can unlock it early via `POST /api/admin/users/{userId}/unlock`.

## Packages used

Installed automatically via `dotnet restore`  listed here for reference, grouped by project:

**API**

- MediatR `14.2.0`
- Microsoft.AspNetCore.Authentication.JwtBearer `10.0.10`
- Microsoft.AspNetCore.Authentication.Negotiate `10.0.10`
- Microsoft.EntityFrameworkCore.Design `10.0.10` (design-time only, powers `dotnet ef`)
- Microsoft.OpenApi `2.7.5`
- Swashbuckle.AspNetCore `10.2.3`

**Application**

- MediatR `14.2.0`
- Microsoft.EntityFrameworkCore `10.0.10`

**Infrastructure**

- BCrypt.Net-Next `4.2.0`
- Microsoft.EntityFrameworkCore `10.0.10`
- Npgsql.EntityFrameworkCore.PostgreSQL `10.0.3`
- System.IdentityModel.Tokens.Jwt `8.21.0`

**Domain**

- No third-party dependencies.

## Docker (optional)

A `Dockerfile` is provided at [`API/Dockerfile`](API/Dockerfile) for containerized builds; it still requires a reachable PostgreSQL instance and the same environment config described above.
