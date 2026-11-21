# Nobat (نوبت)

A modern, responsive online appointment booking platform for barbershops and hair salons.

Built with a clean Modular Monolith architecture — ASP.NET Core 10 backend + Nuxt 4 frontend, full Persian language and RTL support, Jalali calendar, light/dark modes.

## Tech Stack

### Backend
- ASP.NET Core 10 Web API
- Entity Framework Core 10 + PostgreSQL
- ASP.NET Core Identity + JWT Authentication
- FluentValidation
- xUnit testing

### Frontend
- Nuxt 4 + Vue 3 + TypeScript
- Tailwind CSS 4 + Nuxt UI
- Lucide Icons
- Pinia
- dayjs + jalaliday (Jalali calendar)
- Vitest + Playwright testing

### DevOps
- Docker & Docker Compose
- GitHub Actions CI/CD

## Quick Start

### Using Docker Compose (recommended)

```bash
# Clone the repo
git clone https://github.com/mohammadhashemlou/nobat.git
cd nobat

# Copy environment files
cp backend/.env.example backend/.env
cp frontend/.env.example frontend/.env

# Start all services
docker compose up -d
```

Services:
- Frontend: http://localhost:3000
- Backend API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger
- PostgreSQL: localhost:5432

### Manual Development Setup

#### Backend

```bash
cd backend
dotnet restore
dotnet tool restore
dotnet ef database update --project src/Nobat.Infrastructure --startup-project src/Nobat.Api
dotnet run --project src/Nobat.Api
```

#### Frontend

```bash
cd frontend
npm install
npm run dev
```

## Architecture

```
nobat/
├── backend/          # ASP.NET Core 10 Web API (Modular Monolith)
│   └── src/
│       ├── Nobat.Domain/          # Entities, enums, domain logic
│       ├── Nobat.Application/     # Use cases, DTOs, validators
│       ├── Nobat.Infrastructure/  # EF Core, repositories, external services
│       └── Nobat.Api/             # Controllers, middleware, DI
├── frontend/         # Nuxt 4 + Vue 3 + TypeScript
├── docker/           # Docker configs
├── docs/             # Architecture docs, ERD, API spec
└── docker-compose.yml
```

## Features

- **Authentication & RBAC** — Admin, Barber, Customer roles
- **Public salon page** — services, prices, barbers
- **Admin dashboard** — manage services, barbers, schedules, holidays, appointments
- **Smart booking** — automatic availability based on working hours, service duration, breaks
- **Double-booking prevention** — enforced at both application and database level
- **Appointment management** — status tracking, cancellation, history
- **Persian RTL** — full Jalali calendar, 24-hour time, Vazirmatn font
- **Light & dark modes**
- **Fully responsive** — mobile-first design

## License

MIT
