# Architecture

This document explains the key architectural decisions made in this project and the tradeoffs considered. It is a living document — updated as decisions are revisited.

## Overview

A personal Steam library storefront that syncs game data from the Steam Web API into a local PostgreSQL database and serves it through a typed REST API to a Next.js frontend. The system is deployable via a single `docker compose up`.

---

## Decision 1: Backend Owns the Data (Sync vs. Proxy)

**Decision:** The backend syncs Steam data into its own PostgreSQL database on a schedule rather than proxying Steam API requests on demand.

**Alternatives considered:**
- *Proxy on demand*: Simpler to implement — no database, no sync job. Every frontend request hits Steam directly.

**Why sync wins:**
- Steam's API has rate limits and occasional downtime. A proxy creates a hard dependency on Steam availability.
- Storing data locally enables rich SQL queries, aggregations, and stats that aren't possible against the Steam API directly.
- Response times are consistent regardless of Steam API latency.
- It demonstrates a deliberate backend design rather than a thin API wrapper — relevant for the portfolio goal.

**Tradeoff accepted:** Data can be up to 30 minutes stale. This is acceptable for a personal library that doesn't change frequently. The API response includes `lastSyncedAt` so the UI can surface this to the user.

---

## Decision 2: Two-Layer Caching (Redis + PostgreSQL)

**Decision:** PostgreSQL is the persistent source of truth; Redis provides a short-lived cache (5 min TTL) for expensive query results like filtered library views and stats aggregations.

**Why not Redis-only:**
- Redis is volatile. A restart or eviction would require a full re-sync from Steam.

**Why not PostgreSQL-only:**
- Stats aggregations and complex filtered queries on a large library are expensive to recompute on every request.

**Cache invalidation:** TTL-based expiry plus explicit invalidation when the sync job completes. This avoids serving stale data immediately after a sync while not requiring cache-aside logic throughout the codebase.

---

## Decision 3: Stats Pre-Computation

**Decision:** Stats (playtime trends, genre breakdowns, etc.) are computed after each sync and written to a `StatsSnapshots` table. The `/api/stats` endpoint reads the latest snapshot.

**Alternative:** Compute stats on demand from raw game/playtime data.

**Why pre-compute:**
- Aggregation queries across a full library are expensive and identical for every caller (this is a single-user app).
- On-demand computation ties response time to library size.
- Pre-computation makes the stats endpoint O(1) and trivially cacheable.

**Tradeoff accepted:** Stats reflect the last sync, not real-time data. Acceptable given the sync cadence and use case.

---

## Decision 4: Frontend Rendering Strategy (Next.js SSR + CSR split)

**Decision:** The storefront (`/`) is server-side rendered; the stats dashboard (`/stats`) is client-side rendered.

**Why SSR for the storefront:**
- The game grid is the primary landing experience. SSR delivers a fast initial paint with content already present.
- Demonstrates intentional use of Next.js rendering modes rather than defaulting to one or the other.

**Why CSR for stats:**
- The stats dashboard is data-heavy and interactive. Users navigate to it deliberately — a loading state is acceptable.
- Avoids blocking the server-side render on multiple aggregation queries.

---

## Decision 5: Tech Stack

**Backend — ASP.NET Core Web API (C#)**
- Primary language for this developer. A portfolio piece should demonstrate depth in a known stack, not breadth across unfamiliar ones.
- Strong typing, excellent tooling, and first-class support for background services (`IHostedService`) make it well-suited to this architecture.

**Frontend — Next.js + TypeScript**
- Chosen over plain React for the SSR/CSR tradeoff opportunities it enables (see Decision 4).
- TypeScript enforces contract between frontend and API responses.

**Database — PostgreSQL**
- Relational model fits the data well (games, genres, playtime records have clear relationships).
- EF Core provides typed migrations and query building without raw SQL for common operations.

**Cache — Redis**
- Industry-standard for this use case. StackExchange.Redis is mature and well-documented in the .NET ecosystem.

---

## Known Limitations

- **Single user:** The system is designed for one Steam account. Multi-user support would require per-user sync jobs, scoped cache keys, and auth — out of scope for v1.
- **Steam API rate limits:** `GetAppDetails` has no bulk endpoint. The sync job throttles requests to avoid hitting limits, which means initial sync of a large library is slow.
- **No real-time updates:** Playtime is only as fresh as the last sync. Steam does not offer webhooks.
