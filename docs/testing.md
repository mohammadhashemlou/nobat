# Testing

The project has three test layers. All of them run in CI (`.github/workflows/ci.yml`).

| Layer | Tool | Location | Count |
| --- | --- | --- | --- |
| Domain + application unit tests | xUnit | `backend/tests/Nobat.Domain.Tests`, `backend/tests/Nobat.Application.Tests` | 77 |
| API integration tests | xUnit + `WebApplicationFactory` + PostgreSQL | `backend/tests/Nobat.Api.Tests` | 37 |
| Frontend unit/component tests | Vitest + `@vue/test-utils` | `frontend/tests/unit` | 55 |
| End-to-end | Playwright | `frontend/tests/e2e` | 12 |

## Backend

```bash
cd backend
dotnet test                       # everything
dotnet test tests/Nobat.Domain.Tests        # pure domain rules only
dotnet test tests/Nobat.Application.Tests   # availability engine + validators
dotnet test tests/Nobat.Api.Tests           # requires PostgreSQL
```

### Integration tests and the database

`Nobat.Api.Tests` boots the real API pipeline against a **throw-away database**:

- a uniquely named database (`nobat_test_<random>`) is created at startup and dropped on teardown;
- the connection string comes from `NOBAT_TEST_DB`, then `ConnectionStrings__Default`, then the
  local development default (`localhost:5433`, user `nobat`);
- the database user needs `CREATE DATABASE` and `CREATE EXTENSION btree_gist`
  (the exclusion constraint depends on it).

If PostgreSQL is unreachable the tests **skip** with an explanatory message rather than failing,
so `dotnet test` stays usable on a machine without a database. In CI a `postgres:17` service is
provided, so they always run there.

```bash
# Point the suite at a specific server
NOBAT_TEST_DB="Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres" \
  dotnet test tests/Nobat.Api.Tests
```

### What the integration suite proves

- registration, login, refresh-token rotation (the old refresh token is revoked);
- Persian, field-level validation errors instead of the English Identity defaults;
- the full booking flow: availability → book → slot disappears → cancel → slot returns;
- every rejection path (`PAST_TIME`, `HOLIDAY`, `OUTSIDE_WORKING_HOURS`, `BREAK_OVERLAP`,
  `NOT_WORKING_DAY`, `SLOT_ALREADY_BOOKED`, `FORBIDDEN`);
- **database-level** double-booking prevention: rows are inserted directly with SQL, bypassing
  the application check, and PostgreSQL rejects them with `23P01` on `ck_appointments_nooverlap`;
- role scoping (a customer only sees their own appointments, cannot change status, cannot create services).

## Frontend

```bash
cd frontend
npm run test          # Vitest, single run
npm run test:watch    # Vitest, watch mode
```

The unit suite covers:

- `useJalali` — Jalali↔Gregorian conversion against authoritative anchors, decade round-trips,
  month lengths, leap years, Persian digits, 24-hour time formatting, API date helpers;
- `normalizeError` — ProblemDetails → single Persian message;
- `StatusBadge` — every status maps to the right Persian label and colour.

> **Why the calendar logic is hand-written.** `jalaliday` drifts by a day for some dates
> (e.g. 2024-01-05 renders as 1402/10/16 instead of 1402/10/15), and `.startOf('day')` on a
> timezone-shifted Jalali instance corrupts the date. The composable therefore performs both
> conversion directions arithmetically from epoch milliseconds and never relies on a plugin.
>
> These tests are also **host-timezone independent** — they must pass in UTC (as CI runs them),
> in Tehran and anywhere else. Earlier they only passed locally because the developer machine
> happened to be in Iran; running `TZ=UTC npm run test` is the quickest way to reproduce a
> timezone regression.

```bash
TZ=UTC npm run test
TZ=America/New_York npm run test
```

## End-to-end

Playwright drives the real Nuxt app against the real API.

```bash
cd frontend
npx playwright install chromium   # once

# Both servers must be running (API on :5080, Nuxt on :3000)…
npm run test:e2e

# …or let Playwright start the frontend itself and point it at a running API
E2E_API_URL=http://localhost:5080 npx playwright test

# Against an already-deployed environment
E2E_BASE_URL=https://staging.example.com E2E_API_URL=https://api.example.com npx playwright test
```

Each run seeds its own service, barber and working hours through the API and registers a fresh
customer, so runs do not collide. The suite asserts that the booking journey produces **zero
browser console errors**.

## Coverage

```bash
cd backend
dotnet test --collect:"XPlat Code Coverage"
```
