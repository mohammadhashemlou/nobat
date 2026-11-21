# Nobat (نوبت)

A production-minded online appointment booking platform for barbershops and hair salons.

Clean Modular Monolith — **ASP.NET Core 10** API + **Nuxt 4** frontend, fully Persian and RTL,
Jalali calendar with 24-hour time, light and dark modes.

---

## Highlights

- **Booking that cannot double-book.** Availability is computed from working hours, service
  duration, breaks, existing appointments and a minimum lead time — and the database itself
  refuses overlaps through a PostgreSQL exclusion constraint on a `tstzrange`, so a race
  between two customers still cannot produce two appointments in the same slot.
- **Correct time handling.** Everything is stored in UTC; the business timezone (Asia/Tehran)
  is injected through `IBusinessTimeProvider`, never hard-coded.
- **Correct Persian calendar.** Jalali conversion is implemented arithmetically and pinned by
  tests, because the popular `jalaliday` plugin drifts by a day for some dates.
- **Typed, structured errors.** Domain exceptions and FluentValidation failures are mapped to
  RFC 7807 `ProblemDetails` with a stable `code` and a field-level `errors` map, all in Persian.
- **Tested at three levels.** 114 backend tests, 55 frontend unit tests and 12 Playwright
  end-to-end tests. See [docs/testing.md](docs/testing.md).

## Tech Stack

| Layer | Technology |
| --- | --- |
| Frontend | Nuxt 4, Vue 3, TypeScript, Tailwind CSS 4, Nuxt UI, Pinia, Lucide icons |
| Backend | ASP.NET Core 10 Web API, MediatR (CQRS-lite), FluentValidation |
| Data | EF Core 10, PostgreSQL 17, ASP.NET Core Identity |
| Auth | JWT access token (15 min) + rotating refresh token (7 days) |
| Testing | xUnit, Vitest, Playwright |
| DevOps | Docker, Docker Compose, GitHub Actions |

## Quick Start

### Docker Compose (recommended)

```bash
git clone https://github.com/mohammadhashemlou/nobat.git
cd nobat
cp .env.example .env
docker compose up --build
```

| Service | URL |
| --- | --- |
| Frontend | http://localhost:3000 |
| API | http://localhost:5000 |
| Swagger UI | http://localhost:5000/swagger |
| Health | http://localhost:5000/health |
| PostgreSQL | localhost:5432 |

The API migrates and seeds the database on startup. A default administrator is created:

```
admin@nobat.local / Admin@123456
```

> Change this password (and `JWT_SECRET`) before exposing the app to anyone.

### Manual development

Requirements: .NET 10 SDK, Node.js 22, PostgreSQL 17.

```bash
# 1. Database
docker compose up -d postgres
#    …or any PostgreSQL 17 instance; then point the API at it:
#    export ConnectionStrings__Default="Host=localhost;Port=5432;Database=nobat;Username=nobat;Password=nobat_dev_password"

# 2. API  (creates the schema and seeds on first run)
cd backend
dotnet restore
dotnet run --project src/Nobat.Api          # http://localhost:5080, Swagger at /swagger

# 3. Frontend
cd frontend
npm install
npm run dev                                 # http://localhost:3000
```

The frontend reads the API base URL from `NUXT_PUBLIC_API_BASE`
(default `http://localhost:5080`, see `frontend/.env.example`).

## Testing

```bash
cd backend  && dotnet test        # 114 tests (integration tests need PostgreSQL)
cd frontend && npm run test       # 55 Vitest tests
cd frontend && npm run test:e2e   # 12 Playwright tests (both servers running)
```

Full details, including how the throw-away test database works and why the Jalali conversion is
hand-written: **[docs/testing.md](docs/testing.md)**.

## Production

```bash
cp .env.example .env      # set POSTGRES_PASSWORD, JWT_SECRET, FRONTEND_ORIGIN, PUBLIC_API_BASE
docker compose -f docker-compose.prod.yml up -d --build
```

Both images are multi-stage and run as non-root users with container health checks:

- `backend/Dockerfile` — publishes Release output onto the ASP.NET Core runtime (`:8080`);
- `frontend/Dockerfile` — builds Nuxt to `.output/` and serves it with Node (`:3000`).

`docker-compose.prod.yml` refuses to start if a required secret is missing, and gates the
frontend on the backend's health check.

## Project Structure

```
nobat/
├── backend/
│   ├── src/
│   │   ├── Nobat.Domain/          # Entities, enums, domain exceptions — no dependencies
│   │   ├── Nobat.Application/     # Use cases (MediatR), DTOs, validators, availability engine
│   │   ├── Nobat.Infrastructure/  # EF Core, migrations, Identity, JWT, time provider
│   │   └── Nobat.Api/             # Controllers, middleware, DI, Swagger
│   └── tests/
│       ├── Nobat.Domain.Tests/
│       ├── Nobat.Application.Tests/
│       └── Nobat.Api.Tests/       # integration tests against a real PostgreSQL
├── frontend/
│   ├── app/                       # pages, components, composables, stores, layouts
│   ├── tests/unit/                # Vitest
│   └── tests/e2e/                 # Playwright
├── docs/
│   ├── architecture.md
│   └── testing.md
├── docker-compose.yml             # development (hot reload)
├── docker-compose.prod.yml        # production images
└── .github/workflows/ci.yml
```

Dependency rule: `Domain ← Application ← Infrastructure ← Api`. Business logic never lives in
controllers; the Application layer talks to the database through `IApplicationDbContext`, so it
has no EF Core or ASP.NET dependency.

## Features

- **Authentication & RBAC** — Admin, Barber and Customer roles; short-lived access tokens with
  refresh-token rotation.
- **Public salon page** — services, prices and barbers, with a four-step booking wizard.
- **Admin dashboard** — services, barbers, working hours, holidays and appointments.
- **Availability engine** — working hours, service duration, breaks, lead time and existing
  bookings, with a 15-minute slot step.
- **Double-booking prevention** — application-level check for a friendly `409`, plus a database
  exclusion constraint as the source of truth.
- **Appointment lifecycle** — pending, confirmed, completed, cancelled, no-show, with a full
  status history.
- **Customer area** — appointment history and cancellation.
- **Persian RTL** — Jalali calendar, 24-hour time, Vazirmatn font, Persian digits.
- **Light & dark modes**, mobile-first responsive layout, loading/empty/error states everywhere.

## API

Interactive documentation is served at `/swagger` in development. The API is versioned under
`/api/v1`:

| Area | Endpoints |
| --- | --- |
| Auth | `POST /auth/register`, `POST /auth/login`, `POST /auth/refresh`, `GET /auth/me` |
| Salons | `GET /salons/{slug}`, `GET /salons/{id}`, `PUT /salons/{id}` |
| Services | `GET/POST /services`, `PUT/DELETE /services/{id}` |
| Barbers | `GET/POST /barbers`, `PUT /barbers/{id}`, `GET/PUT /barbers/{id}/working-hours` |
| Holidays | `GET/POST /holidays`, `DELETE /holidays/{id}` |
| Appointments | `GET /appointments/availability`, `GET/POST /appointments`, `PATCH /appointments/{id}/status`, `POST /appointments/{id}/cancel` |

## License

MIT
