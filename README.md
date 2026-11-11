# School Admission System

A Docker-ready school admission application with a .NET 8 REST API, PostgreSQL persistence, and a React/TypeScript student portal and admin dashboard.

## Features

- Browse classes, shifts, and per-shift seat availability.
- Submit an admission application with a 17-digit Birth Registration Number.
- Reserve a seat for a configurable period (600 seconds by default).
- Confirm a simulated payment before the server-side hold expires.
- Expire pending applications in a background service and release their seats.
- Browse a student's applications and application status after refresh.
- Admin dashboard with real seat statistics, application search/filters, class and shift setup, and capacity editing.
- PostgreSQL row/advisory locks and database constraints protect seat allocation.

## Technology and architecture

- **API:** C#, ASP.NET Core 8 Web API, Swagger/OpenAPI.
- **Domain:** DTOs, entities, enums, and service interfaces in `SchoolAdmission.Core`.
- **Infrastructure:** EF Core, Npgsql, PostgreSQL migrations, admission services, and the expiration worker.
- **Tests:** xUnit and PostgreSQL Testcontainers integration tests.
- **Frontend:** React, TypeScript, Vite, React Router, Tailwind CSS.
- **Runtime:** Docker Compose with PostgreSQL persistence in a named volume.

```text
backend/
  SchoolAdmission.API/             Controllers, middleware, startup
  SchoolAdmission.Core/            Domain, DTOs, service contracts
  SchoolAdmission.Infrastructure/   EF Core, migrations, services
  SchoolAdmission.Tests/            PostgreSQL-backed xUnit tests
frontend/
  src/                              Student portal and admin dashboard
docker-compose.yml
```

## Requirements

- Docker Desktop with Docker Compose (recommended).
- For local development: .NET 8 SDK, Node.js 20+, npm, and PostgreSQL 15+.

## Run the complete application

From the project root, copy `.env.example` to `.env`, choose a local database password, and start the services:

```powershell
Copy-Item .env.example .env
docker compose up --build
```

Compose waits for PostgreSQL's health check before starting the API. The API applies EF Core migrations at startup. PostgreSQL data is kept in the `postgres_data` named volume when containers restart.

Default local URLs:

- Student portal and admin dashboard: <http://localhost:5173>
- API: <http://localhost:5286>
- Swagger UI: <http://localhost:5286/swagger>
- PostgreSQL: `localhost:5432`

Without a `.env` file, Compose uses `postgres` as a local-development password to support a fresh setup. Set a private value in `.env`; do not commit `.env` or use development credentials in a public deployment.

## Local development

### Backend

Start a local PostgreSQL database, set the connection string in the current PowerShell session, then run:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=schooladmission;Username=postgres;Password=<your-local-password>"
dotnet run --project backend\SchoolAdmission.API\SchoolAdmission.API.csproj
```

The API applies migrations during startup. The connection string is intentionally not stored in `appsettings.json`.

### Frontend

```powershell
cd frontend
npm ci
$env:VITE_API_URL = "http://localhost:5286"
npm run dev
```

Vite runs at <http://localhost:5173>. `VITE_API_URL` is also configurable as a Docker build argument through `.env`.

## API overview

Swagger is the executable API reference at `/swagger`. The main routes are:

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/classes` | List classes and aggregate seat counts |
| `GET` | `/api/classes/{classId}` | Retrieve class details |
| `GET` | `/api/shifts` | List shifts |
| `GET` | `/api/classes/availability?classId={id}` | View class/shift seat availability |
| `POST` | `/api/classes` | Create a class |
| `POST` | `/api/classes/with-shift` | Create a class and its initial shift/capacity in one transaction |
| `POST` | `/api/shifts` | Create a shift |
| `POST` | `/api/class-shifts` | Configure a class/shift seat capacity |
| `PUT` | `/api/class-shifts/{id}/capacity` | Update capacity without reducing occupied seats |
| `POST` | `/api/applications` | Create an application and temporary hold |
| `GET` | `/api/applications/{id}` | Retrieve application details |
| `GET` | `/api/applications/{id}/status` | Retrieve application status |
| `GET` | `/api/students/{studentId}/applications` | Retrieve a student's applications |
| `POST` | `/api/payments/{applicationId}/confirm` | Simulate payment confirmation |
| `GET` | `/api/admin/dashboard` | Retrieve database-backed dashboard statistics |
| `GET` | `/api/admin/applications` | List applications; supports `classId`, `shiftId`, `status`, and `search` filters |

Application creation requires `studentName`, `birthRegistrationNumber` (exactly 17 ASCII digits), and `classShiftId`. The response contains the application ID, student ID, Pending status, and server-generated hold expiration time. Business conflicts (including unavailable seats or expired holds) return HTTP 409.

## Configuration

| Variable | Default | Description |
|---|---:|---|
| `ConnectionStrings__DefaultConnection` | — | PostgreSQL connection string (required by the API) |
| `HOLD_DURATION_SECONDS` | `600` | Server-side seat-hold duration |
| `EXPIRATION_POLLING_SECONDS` | `15` | Background expiration polling interval |
| `VITE_API_URL` | `http://localhost:5286` | API URL embedded in the frontend build |

## Database migrations

The API applies migrations at startup. To apply them manually from the project root:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=schooladmission;Username=postgres;Password=<your-local-password>"
dotnet ef database update --project backend\SchoolAdmission.Infrastructure\SchoolAdmission.Infrastructure.csproj --startup-project backend\SchoolAdmission.API\SchoolAdmission.API.csproj
```

If the EF command is not installed, install the matching .NET 8 tool once:

```powershell
dotnet tool install --global dotnet-ef --version 8.0.33
```

## Tests and builds

The tests use real PostgreSQL through Testcontainers; Docker must be running to execute the test suite.

```powershell
dotnet build backend\SchoolAdmission.sln
dotnet test backend\SchoolAdmission.sln
npm ci --prefix frontend
npm run build --prefix frontend
```

The xUnit suite covers application creation, payment idempotency, concurrent seat requests, registration-number validation, duplicate active holds, expired payments, capacity constraints, and automatic expiration. A successful build alone does not verify database runtime behavior; run the tests with Docker available.

## Troubleshooting

- **API cannot connect to PostgreSQL:** verify `ConnectionStrings__DefaultConnection`, the database is running, and the host/port are correct.
- **Docker tests fail to start:** start Docker Desktop and wait for the engine to become ready.
- **Frontend API requests fail:** verify the API is running at the configured `VITE_API_URL`; the browser must be able to reach that host.
- **A hold expires:** the server is authoritative; expired Pending applications cannot be paid and their seat is released by the background service.

## Screenshots

No screenshots are included yet.

## Security note

This assignment/demo does not include admin authentication. Do not expose the admin endpoints or dashboard publicly without adding an appropriate access-control mechanism.
