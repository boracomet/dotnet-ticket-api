# Ticket API — .NET 8

Layered ticket management API with JWT auth, **Admin/User** roles, pagination, filtering, and a strict status workflow.

> **TR:** Katmanlı ticket API — JWT, Admin/User rolleri, sayfalama/filtreleme ve durum geçiş kuralları.

## Architecture / Mimari

```
TicketApi.Api            → Controllers, JWT, Swagger
TicketApi.Application    → Services, DTOs, interfaces
TicketApi.Domain         → Entities & enums
TicketApi.Infrastructure → EF Core, PostgreSQL/SQLite, JWT + password hashing
web/                     → Vite + React + TypeScript UI (Nginx)
```

## Features / Özellikler

- Register / login with JWT
- Tickets CRUD
- Pagination + filter by `status`, `priority`, `search`
- Status workflow: `Open → InProgress → Resolved → Closed` (illegal transitions → `422`)
- Role rules: users see own/assigned tickets; admins see all

**Seed users (first run)**

| Email | Password | Role |
|-------|----------|------|
| admin@ticket.local | Admin123! | Admin |
| user@ticket.local | User1234! | User |

## Quick start — Docker Compose

```bash
cp .env.example .env
docker compose up --build
```

- **Web UI:** http://localhost:3000  
- API: http://localhost:8080  
- Swagger: http://localhost:8080/swagger  

> Ports via `WEB_PORT` / `APP_PORT` / `POSTGRES_PORT`.  
> Run this stack **or** Mini Wallet at a time if both use defaults 8080/3000/5432.

## Local (SQLite)

```bash
cd src/TicketApi.Api
dotnet run
# Development profile uses SQLite (appsettings.Development.json)
```

## Local (PostgreSQL)

```bash
docker compose up -d postgres
export ConnectionStrings__Default='Host=localhost;Port=5432;Database=ticket_api;Username=ticket;Password=ticket_secret_change_me'
export Database__Provider=Postgres
dotnet run --project src/TicketApi.Api
```



## Web UI (React)

Soft professional Turkish UI: login/register, ticket board with status/priority filters, pagination, create ticket, workflow status changes, and delete (creator or admin).

### Local development

```bash
# Terminal 1
cd src/TicketApi.Api && dotnet run

# Terminal 2
cd web && npm install && npm run dev
# → http://localhost:5173
```

### Docker

`docker compose up --build` serves the UI on **:3000** (Nginx → API).

Screenshots: `web/docs/screenshots/`.

CORS is enabled for `http://localhost:3000` and `:5173`. Enums serialize as strings (`Open`, `High`, …) via `JsonStringEnumConverter`.

## Tests

```bash
dotnet build
dotnet test
```

## Example

```bash
TOKEN=$(curl -s -X POST http://localhost:8080/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"user@ticket.local","password":"User1234!"}' | jq -r .accessToken)

curl -s -X POST http://localhost:8080/api/v1/tickets \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"title":"Login bug","description":"Cannot login on Safari","priority":"High"}'
```

Screenshots: `docs/screenshots/`.

## License

MIT — portfolio demo by Bora Ata Türkoğlu.
