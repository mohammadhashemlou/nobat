# Nobat Architecture

## Overview

Nobat is a barbershop/salon appointment booking platform built with a clean Modular Monolith architecture.

## Architecture Layers

### Backend (ASP.NET Core 10)

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `Nobat.Domain` | Entities, enums, domain events, pure business logic |
| Application | `Nobat.Application` | Use cases (CQRS via MediatR), DTOs, validators (FluentValidation), interfaces |
| Infrastructure | `Nobat.Infrastructure` | EF Core DbContext, repositories, ASP.NET Identity, JWT, external services |
| API | `Nobat.Api` | Controllers, middleware, DI composition, Swagger |

**Dependency flow**: `Api → Infrastructure → Application → Domain`

The Domain layer has zero external dependencies. Application depends only on Domain. Infrastructure implements Application interfaces. Api composes everything.

### Frontend (Nuxt 4 + Vue 3 + TypeScript)

- Nuxt 4 with SSR
- Tailwind CSS 4 + Nuxt UI
- Pinia for state management
- dayjs for timezone-aware time formatting; Jalali↔Gregorian conversion is implemented
  arithmetically in `app/composables/useJalali.ts` (the `jalaliday` plugin drifts by a day for
  some dates and miscomputes `.startOf('day')` on timezone-shifted instances)
- Lucide icons
- Vazirmatn font
- Full RTL support
- Light/dark mode

## Database Schema

### Users (ASP.NET Core Identity)
- Id, UserName, Email, FullName, PhoneNumber
- Role: Admin | Barber | Customer
- LockoutEnd, EmailConfirmed, PhoneNumberConfirmed

### Salons
- Id, Name, Slug, Address, Phone, Description
- DefaultTimezone (default: Asia/Tehran)
- CreatedAt, UpdatedAt

### Barbers
- Id, UserId (FK → Users), SalonId (FK → Salons)
- FullName, Bio, AvatarUrl, IsActive
- CreatedAt, UpdatedAt

### Services
- Id, SalonId (FK → Salons)
- Name, Description, DurationMinutes, Price
- IsActive, CreatedAt, UpdatedAt

### BarberServices (M:N junction)
- BarberId (FK → Barbers), ServiceId (FK → Services)
- PK: (BarberId, ServiceId)

### WorkingHours
- Id, BarberId (FK → Barbers)
- DayOfWeek (0=Saturday ... 6=Friday for Iran)
- StartTime (TimeOnly), EndTime (TimeOnly)
- BreakStart (TimeOnly?), BreakEnd (TimeOnly?)

### Holidays
- Id, SalonId (FK → Salons)
- Date (DateOnly), Description
- CreatedAt

### Appointments
- Id, SalonId, BarberId, CustomerId, ServiceId
- StartAt (UTC), EndAt (UTC)
- Status: Pending | Confirmed | Completed | Cancelled | NoShow
- Notes, CreatedAt, UpdatedAt, CancelledAt
- TimeRange (generated `tstzrange`, maintained by a trigger)

**Double-booking prevention** — two layers, both required:

1. **Application-level** (`CreateAppointmentCommand`): an overlap query returns a friendly
   `409 SLOT_ALREADY_BOOKED` before the insert is attempted.
2. **Database-level** (the source of truth): a PostgreSQL exclusion constraint

   ```sql
   ALTER TABLE "Appointments"
     ADD CONSTRAINT "ck_appointments_nooverlap"
     EXCLUDE USING GIST ("BarberId" WITH =, "TimeRange" WITH &&)
     WHERE ("Status" NOT IN (3, 4));   -- Cancelled, NoShow
   ```

   A trigger keeps `TimeRange` in sync with `StartAt`/`EndAt`, and the `btree_gist` extension
   allows `=` on the UUID column inside a GiST index. Two customers racing for the same slot
   therefore cannot both succeed: the loser gets SQLSTATE `23P01`, which the handler converts
   into the same `SLOT_ALREADY_BOOKED` response. Cancelling frees the slot because cancelled
   rows fall outside the partial predicate.

### AppointmentStatusHistory
- Id, AppointmentId (FK → Appointments)
- Status, ChangedByUserId (FK → Users), CreatedAt (UTC)

## Time Handling

- All timestamps stored in UTC
- Business time uses `Asia/Tehran` timezone
- API accepts/returns ISO 8601 UTC timestamps
- Frontend converts to Jalali for display

## Authentication

- JWT Bearer tokens
- Access token: 15 minutes
- Refresh token: 7 days
- Roles: Admin, Barber, Customer
- ASP.NET Core Identity for user management

## Module Organization (Application)

```
Nobat.Application/
├── Modules/
│   ├── Identity/        # Register, Login, Refresh, Profile
│   ├── Salons/          # CRUD, public salon page data
│   ├── Barbers/         # CRUD, barber schedules
│   ├── Services/        # CRUD
│   ├── Schedules/       # Working hours, holidays
│   └── Appointments/    # Booking, availability, status, history
├── Common/              # Shared DTOs, interfaces, behaviors
└── DependencyInjection.cs
```

Each module contains:
- `Commands/` (CQRS writes via MediatR)
- `Queries/` (CQRS reads)
- `DTOs/`
- `Validators/`
- `Handlers/`

## API Conventions

- RESTful endpoints
- `/api/v1/` prefix
- Centralized error handling with ProblemDetails (RFC 7807)
- Consistent error response shape
- Pagination via cursor/offset
- OpenAPI/Swagger documentation
