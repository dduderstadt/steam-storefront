# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Purpose

A personal Steam library storefront — a portfolio project built to demonstrate senior engineering judgment. Pulls the owner's Steam library via the Steam Web API and presents it as a polished web app with filtering, a `/stats` dashboard, and personalization. Deployable via a single `docker compose up`.

## Tech Stack

| Layer | Choice |
|---|---|
| Backend | ASP.NET Core Web API (C#) |
| ORM | Entity Framework Core (PostgreSQL) |
| Cache | Redis (StackExchange.Redis) |
| Frontend | Next.js + TypeScript |
| Testing | xUnit + WebApplicationFactory (backend), Vitest (frontend) |
| Infra | Docker Compose (postgres, redis, backend, frontend) |

## Commands

```bash
# Start full stack
docker compose up --build

# Backend (from /backend)
dotnet run                  # dev server
dotnet test                 # all tests
dotnet test --filter <name> # single test
dotnet ef migrations add <Name>
dotnet ef database update

# Frontend (from /frontend)
npm run dev
npm test
npm run build
npm run lint
```

## Architecture

### Core Design Principle: Backend Owns the Data

The backend does **not** proxy the Steam API on demand. It syncs Steam data into PostgreSQL on a schedule and serves its own data layer. This decouples the frontend from Steam API rate limits and enables rich querying, stats pre-computation, and resilience when Steam is down.

```
steam-storefront/
├── backend/
│   ├── Controllers/        # Thin route handlers — delegate to services
│   ├── Services/           # Business logic (LibraryService, StatsService, SyncService)
│   ├── Steam/              # Steam API client (isolated, mockable)
│   ├── Models/             # EF Core entities + DTOs
│   ├── Jobs/               # Background sync (IHostedService)
│   └── Tests/
├── frontend/
│   └── src/
│       ├── app/            # Next.js app router pages
│       │   ├── page.tsx        # Storefront (SSR)
│       │   ├── stats/page.tsx  # Stats dashboard (CSR)
│       │   └── game/[appId]/   # Game detail
│       ├── components/
│       └── hooks/
├── docker-compose.yml
└── ARCHITECTURE.md
```

### Caching Strategy (Two Layers)

1. **PostgreSQL** — persistent store for all game data, playtime history, and pre-computed stats. Source of truth after each sync.
2. **Redis** — short-lived cache (TTL ~5 min) for expensive query results. Keys namespaced as `steam:{userId}:{resource}`.

Cache is invalidated when the sync job completes, not purely on TTL. The sync job writes `lastSyncedAt`; API responses include it so the frontend can show data freshness.

### Sync Job

A background `IHostedService` (`Jobs/LibrarySyncJob.cs`) runs on a configurable interval (default: 30 min):
1. Fetches owned games + playtime from `IPlayerService/GetOwnedGames`
2. Fetches game details from `ISteamApps` for any new `appId`s (one request per appId — Steam has no bulk endpoint)
3. Upserts into Postgres via EF Core
4. Invalidates relevant Redis keys
5. Triggers stats recomputation

### Stats Pipeline

Stats are **pre-computed and stored** after each sync, not calculated at query time. `StatsService` writes to a `StatsSnapshots` table. The `/api/stats` endpoint reads the latest snapshot — always O(1).

### Frontend Rendering Strategy

- **Storefront (`/`)** — SSR. Game grid is rendered server-side for fast initial paint.
- **Stats (`/stats`)** — CSR. Data-heavy dashboard fetches client-side after load.
- This split is an intentional tradeoff documented in `ARCHITECTURE.md`.

### API Design

REST under `/api/v1/`:

```
GET  /api/v1/library              # paginated, filterable game list
GET  /api/v1/library/{appId}      # single game detail
GET  /api/v1/stats                # latest stats snapshot
POST /api/v1/sync                 # trigger manual sync
```

Filter logic (`?genre=RPG&minPlaytime=10&sort=playtime`) lives in `LibraryService`, not in controllers.

### Testing Approach

- **Unit tests**: Services with mocked dependencies (Steam client always mocked)
- **Integration tests**: Controllers via `WebApplicationFactory` hitting a real test database
- Test fixtures live in `Tests/Fixtures/`

## Code Style

- All `if` statements must use curly braces, even single-line bodies.

## Key Constraints

- `STEAM_API_KEY` and `STEAM_ID` (owner's 64-bit Steam ID) are env vars — never hardcoded.
- Steam's `GetAppDetails` has no bulk endpoint — the sync job must rate-limit individual calls.
- `ARCHITECTURE.md` must stay current as decisions evolve — it is a portfolio deliverable.
