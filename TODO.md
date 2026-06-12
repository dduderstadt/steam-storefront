# TODO — App Improvement Recommendations

## Error Handling

### ~~Error & Not-Found Pages (Frontend)~~ ✓ Done
~~Next.js App Router supports `error.tsx` and `not-found.tsx` conventions, but neither exists. Add:~~
- ~~`frontend/src/app/error.tsx` — error boundary for unexpected failures with a user-facing recovery UI~~
- ~~`frontend/src/app/not-found.tsx` — styled 404 page~~

### ~~Global Exception Middleware (Backend)~~ ✓ Done
~~Controllers return ad-hoc status codes with no standardized error response shape. Add a global exception-handling middleware in the backend that catches unhandled exceptions and returns a consistent JSON error body (e.g. `{ "error": "...", "status": 500 }`).~~

## Loading States

### ~~Skeleton Loaders~~ ✓ Done
~~The stats page renders plain "Loading stats..." text and no `loading.tsx` exists for the app directory. Add:~~
- ~~`frontend/src/app/loading.tsx` — top-level loading fallback used by Next.js Suspense~~
- ~~Skeleton loader components for the game grid (home page) and the stats dashboard~~

## Testing

### ~~Backend Tests~~ ✓ Done
~~`backend/Tests/UnitTest1.cs` is an empty placeholder. xUnit, Moq, and FluentAssertions are already configured in the `.csproj` but completely unused.~~
- ~~Unit tests for `LibraryService`, `StatsService`, and `SyncService` with `ISteamClient` mocked via Moq~~
- ~~Integration tests for the API controllers using `WebApplicationFactory` and a test database~~

### ~~Frontend Business Logic Tests~~ ✓ Done
~~vitest is installed but no tests or config exist.~~
- ~~`frontend/vitest.config.ts` — test environment configured~~
- ~~`lib/utils.ts` — `formatPlaytime` and `getTopGenres` extracted and tested~~
- ~~`lib/api.ts` — `getLibrary` URL construction tested~~

---

## v2

### Architecture — Shared Page Layout Template
All three pages (`/`, `/stats`, `/game/[appId]`) duplicate the same `max-w-7xl mx-auto px-4 py-8` container pattern. Extract a `PageLayout` component (`frontend/src/components/PageLayout.tsx`) that accepts a `title` prop and `children`, and replace the inline wrapper divs in each page.

### Architecture — Shared UI Primitives
Tailwind styling is duplicated across components:
- Button styles repeated in `Pagination.tsx` and `FilterBar.tsx`
- Genre badge styles repeated in `GameCard.tsx` and the game detail page

Create reusable primitives in `frontend/src/components/ui/`:
- `Button.tsx`
- `Badge.tsx`
- `Input.tsx`

### ~~Documentation — Update ARCHITECTURE.md with Testing Strategy~~ ✓ Done
~~`ARCHITECTURE.md` is a portfolio deliverable that must stay current. The testing approach involves a deliberate decision worth documenting — integration tests run against a real Postgres container (Testcontainers) rather than an in-memory provider, because EF Core InMemory cannot translate Postgres-specific array operations used by genre filtering. Add a decision record explaining the testing strategy and why InMemory was ruled out.~~

### Testing — Automated E2E / UI Tests
No end-to-end test suite exists. Add Playwright (preferred) or Cypress to cover the full user journey against the running app:
- Library browsing and genre/playtime filtering
- Game detail page navigation
- Stats dashboard data loading
- Wire into `docker compose` so E2E tests can run against the full stack
