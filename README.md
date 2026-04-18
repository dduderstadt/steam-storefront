# steam-storefront
A storefront for my steam library

## Tools
**Claude Code** to assist with boilerplate code so that I could focus on architecture decisions and design

## Backend

### Backend Notes
`Models/Dtos/GameDto.cs`
- A DTO - Data Transfer Object - is a separate class whose only job is to define the shape of data that **leaves** your API. It's distinct from the entity (`Game.cs`) on purpose

The key question is: **why not just return the `Game` entity directly from the controller?**

A few reasons:
- **You expose more than you intend to. `Game` has `FirstSyncedAt`, `LastSyncedAt` - internal bookkeeping columns the frontend has no use for. Returning the entity leaks implementation details.
- **Your API shape becomes coupled to your database schema.** If you rename a column or split a table, your API contract breaks for callers. The DTO is a stable contract you control independently.
- **Serialization surprises.** EF entities can have lazy-loaded navigation properties that cause infinite loops or unexpected queries when the serializer touches them.

`GameDto` is a positional `record` - immutable by default, value equality, built in, and the constructor is the parameter list itself. Good fit for something that's just data  moving in one direction.

The one design decision worth noting: `HeaderImageUrl` and `LastPlayed` are nullable here, matchingn reality - a game may not have an image yet if Steam's detail fetch failed, and a game mmight never have been played.

**Lazy Loading**
When EF Core loads a `Game` entity, it doesn't automatically load related data - like `PlaytimeRecords`. It waits until something _touches_ that property, then fires a second database query behind the scenes to fetch it. That's lazy-loading - queries happen implicitly, on demand.

The problem with serializing entities directly: the JSON serializer touches every property while building the response. If it hits a lazy-loaded navigation property, EF fires an unexpected query mid-serialization. Worse, if `Game` had a reference back to something that referenced `Game` again, the serializer would follow that loop forever and either crash or produce garbage JSON.

DTOs don't have navigation properties - just plain values - so the serializer has nothing unexpected to chase.

**Immutability**
```c#
    // A mutable object lets you change its properties after creation:
    var dto = new GameDto();
    dto.Name = "Half-Life"; // allowed
    dto.Name = "Halo";      // also allowed — state can drift

    // An immutable object's state is set once at construction and can never change:
    var dto = new GameDto(1, "Half-Life", ...);
    dto.Name = "Halo"; // compiler error

```
**Positional records** are immutable by default - propertoes are `init`-only. This matters for DTOs because a response object _should_ be fixed - you build it, you return it, nothing should be mutating it in transit. Immutability makes that guarantee explicit rather than just a convention.

```c#
// Classes are mutable
public class GameDto {
    public string Name {get;set;} // mutable
}

// Records are immutable
public record GameDto(string Name); // immutable
```
Note: you _can_ make a record mutable by explicitly using `set` instead of `init`, and you can make a class property init-only by writing `{get; init;}`. So it's not strictly "classes = mutable, records = immutable" - it's about what the property defintion says.

But positional records give you immutability by default, which is why they're the natural fit for DTOs.

**Class v Record**
| Feature | Class | Record |
| --- | --- | --- |
| Default mutability | Mutable (`get; set;`) | Immutable (`get; init;`) |
| Equality | Reference equality - two objects are equal only if they point to the same instance in memory | Value equality - two objects are equal if all of their properties match |
| Primary use case | Entities, services, objects with behavior | Data transfer, immutable snapshots |
| Positional syntax | No | Yes - `record Foo(string Name, int Age)` |
| Inheritance | Yes | Yes, but only record-to-record |
| with expression | `No` | Yes - `var b = a with { Name = "New" }` creates a copy with one property changed |
| ToString() | Default prints type name | Default prints all property values |
| Can have methods | Yes |  Yes |

---

`Models/Dtos/LibraryQueryParams.cs`

This is the object that represents the query string parameters from the frontend when it calls `GET /api/v1/library`. For example:

```
/api/v1/library?genre=Action&minPlaytime=10&sort=playtime&page=2
```

ASP.NET Core automatically maps those query string values onto this class via `[FromQuery]` in the controller — you don't write any parsing code yourself.

**Why a class and not a record?**
Query params are a mutable input object — ASP.NET Core's model binder needs to create an empty instance and then *set* properties one by one as it reads the query string. A positional record doesn't fit that pattern cleanly. Classes with `{ get; set; }` are the right tool here.

**`MinPlaytime` is in hours, not minutes**
The frontend deals in hours because that's what users think in. The service layer converts to minutes when querying the database (`query.MinPlaytime.Value * 60`), because that's how Steam stores it. The conversion happens in exactly one place — `LibraryService` — so if it ever needs to change, there's only one place to update.

**Defaults**
`Sort` defaults to `"name"`, `Page` to `1`, `PageSize` to `50`. If the frontend sends no query params at all, the user still gets a sensible paginated response sorted alphabetically.

---

`Models/Dtos/PagedResult<T>.cs`

This is the wrapper that every paginated API response uses. Instead of returning a raw list of games, the library endpoint returns:

```json
{
  "items": [...],
  "totalCount": 847,
  "page": 1,
  "pageSize": 50
}
```

**Why paginate at all?**
A Steam library can have hundreds or thousands of games. Returning all of them in one response is wasteful — slow to serialize, slow to send, slow to render. Pagination lets the frontend request only what it needs to display right now.

**Why include `totalCount`?**
The frontend needs to know how many pages exist to render pagination controls — "Page 1 of 17" or a next/previous button. Without `totalCount`, the client has no way to know if there are more results. This means the service runs two queries per request: one `COUNT(*)` and one `SELECT` with `SKIP`/`TAKE`. That's an accepted cost of pagination.

**Why a generic `PagedResult<T>` and not `PagedResult<GameDto>` specifically?**
The same pagination wrapper works for any resource. If we later add a paginated endpoint for playtime history or something else, `PagedResult<T>` handles it without writing a new wrapper class. The `T` is resolved at compile time — no runtime cost.

**Why a positional record?**
Same reason as `GameDto` — it's outbound data, built once and returned. Immutability is the right default.

---

`Models/Dtos/StatsDto.cs`

Two records in one file — `GamePlaytimeStat` and `StatsDto`. This is the shape of the response from `GET /api/v1/stats`.

```json
{
  "totalGames": 847,
  "totalPlaytimeMinutes": 284920,
  "playtimeByGenre": { "Action": 120400, "RPG": 84200 },
  "topGames": [
    { "appId": 570, "name": "Dota 2", "playtimeMinutes": 48000 }
  ],
  "computedAt": "2026-04-18T03:16:28Z",
  "lastSyncedAt": "2026-04-18T03:00:00Z"
}
```

```csharp
public record GamePlaytimeStat(int AppId, string Name, int PlaytimeMinutes);

public record StatsDto(
    int TotalGames,
    int TotalPlaytimeMinutes,
    Dictionary<string, int> PlaytimeByGenre,
    List<GamePlaytimeStat> TopGames,
    DateTime ComputedAt,
    DateTime LastSyncedAt
);
```

**Why two records in one file?**
`GamePlaytimeStat` only exists to describe an item inside `StatsDto.TopGames`. It has no use anywhere else in the codebase. Keeping it in the same file makes that relationship obvious — they're coupled by design, so they live together.

**Why `Dictionary<string, int>` for `PlaytimeByGenre`?**
Genre names are the keys, total playtime minutes are the values. A dictionary serializes to a JSON object naturally in .NET, which is exactly the shape the frontend needs to iterate over and render a chart or list. A `List<(string Genre, int Minutes)>` would work too but requires more ceremony on the frontend to look up a specific genre.

**`ComputedAt` vs `LastSyncedAt`**
These are two different timestamps and the distinction matters:
- `LastSyncedAt` — when Steam data was last fetched. The freshness of the raw game data.
- `ComputedAt` — when the stats aggregation was run. Could theoretically differ from `LastSyncedAt` if stats computation failed after a successful sync.

The frontend can show both — "Data synced at X, stats computed at Y" — so users understand exactly how fresh what they're seeing is.

---

`Data/AppDbContext.cs`

This is the EF Core database context — the central class that represents your connection to the database and owns all the configuration for how your C# objects map to database tables.

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
```

**Primary constructor**
This uses C# 12's primary constructor syntax — instead of writing a constructor body, the parameter list sits next to the class name and is passed directly to the base `DbContext` class. `DbContextOptions` carries the connection string and provider (Npgsql/Postgres in our case), injected by the DI container from `Program.cs`.

**`DbSet` properties**
```csharp
public DbSet<Game> Games => Set<Game>();
public DbSet<PlaytimeRecord> PlaytimeRecords => Set<PlaytimeRecord>();
public DbSet<StatsSnapshot> StatsSnapshots => Set<StatsSnapshot>();
```
Each `DbSet<T>` represents a table. This is how you query — `db.Games.Where(...)`, `db.StatsSnapshots.OrderByDescending(...)`. The `=> Set<Game>()` syntax is a computed property that delegates to EF Core's internal set tracking, which is slightly safer than auto-properties for `DbSet`.

**`OnModelCreating`**
This is where Fluent API configuration lives — things annotations can't express, like Postgres-specific column types:
```csharp
e.Property(g => g.Genres).HasColumnType("text[]");
e.Property(s => s.Data).HasColumnType("jsonb");
```
EF Core reads this when building the model and when generating migrations. The migration we created (`InitialCreate`) was generated entirely from what `OnModelCreating` describes.

**Why Fluent API over annotations for column types?**
Annotations like `[Column(TypeName = "text[]")]` would work, but they put Postgres-specific knowledge on the model class itself. If you ever swapped databases, you'd have to touch every entity. Fluent API keeps that provider-specific config in one place — the context.

---

`Steam/ISteamApiClient.cs`

This is the interface that defines the contract for talking to Steam's API. It has exactly two methods — one to fetch the owner's game library, one to fetch details for a single game.

```csharp
public interface ISteamApiClient
{
    Task<IReadOnlyList<OwnedGame>> GetOwnedGamesAsync(string steamId, CancellationToken ct = default);
    Task<GameDetails?> GetGameDetailsAsync(int appId, CancellationToken ct = default);
}
```

**Why an interface and not just the class directly?**
Two reasons:

- **Dependency injection.** Services that need to talk to Steam (`SyncService`) depend on `ISteamApiClient`, not `SteamApiClient`. The DI container resolves the concrete class at runtime. This means you can swap implementations — a real HTTP client in production, a fake in tests — without changing any consuming code.
- **Testability.** In unit tests you never want to make real HTTP calls to Steam. Because `SyncService` depends on the interface, you can give it a mock that returns canned data. If it depended on the concrete class, you'd be stuck making real network calls in tests.

---

`Steam/SteamApiModels.cs`

Two positional records that represent Steam API responses — `OwnedGame` and `GameDetails`. These are internal to the Steam layer and never leave it.

```csharp
public record OwnedGame(
    int AppId,
    string Name,
    int PlaytimeForever,
    int PlaytimeTwoWeeks,
    long? RtimeLastPlayed);

public record GameDetails(
    int AppId,
    string Name,
    string? ShortDescription,
    string? HeaderImage,
    string[] Genres);
```

**Why separate from the DTOs?**
`OwnedGame` and `GameDetails` mirror Steam's API response shape — property names like `RtimeLastPlayed` are Steam's naming, not ours. The DTOs (`GameDto`) use our naming conventions and only carry what the frontend needs. `SyncService` translates between the two, which is the only place that knows about both shapes.

This keeps the Steam API's quirks (Unix timestamps, Steam's naming conventions, missing bulk endpoints) isolated to the `Steam/` folder. Nothing outside it needs to know how Steam structures its responses.

---

`Steam/SteamApiClient.cs`

The concrete implementation of `ISteamApiClient`. This is where the actual HTTP calls to Steam happen.

**Two base URLs**
```csharp
private const string BaseUrl = "https://api.steampowered.com";   // authenticated API
private const string StoreUrl = "https://store.steampowered.com"; // public store API
```
Steam has two separate APIs. The authenticated Web API (`api.steampowered.com`) requires your API key and is used to fetch owned games. The Store API (`store.steampowered.com`) is public and used to fetch game details like genres and images. They're separate services with different rate limits.

**Rate limiting**
```csharp
private static readonly TimeSpan RateLimit = TimeSpan.FromMilliseconds(1500);

// Inside GetGameDetailsAsync:
await Task.Delay(RateLimit, ct);
```
Steam's `appdetails` endpoint has no bulk version — you must call it once per game. For a library of 500 games, that's 500 sequential HTTP calls. Without a delay, Steam will rate-limit you and start returning errors. 1.5 seconds between calls is conservative but safe. This is why an initial sync of a large library is slow — an intentional tradeoff documented in `ARCHITECTURE.md`.

**`JsonNode` for parsing**
```csharp
var games = JsonNode.Parse(response)?["response"]?["games"]?.AsArray() ?? [];
```
We use `JsonNode` (part of `System.Text.Json`) instead of defining a full deserialization class for Steam's response. Steam's JSON is deeply nested and inconsistently shaped — some fields are missing for games with no playtime, some responses wrap data differently. `JsonNode` lets us navigate the tree with `?` null-conditional operators and extract only what we need, without creating a brittle deserialization class that breaks when Steam changes a field.

**Graceful failure on `GetGameDetailsAsync`**
```csharp
catch (Exception ex)
{
    logger.LogWarning(ex, "Failed to fetch details for appId {AppId}", appId);
    return null;
}
```
If Steam's detail fetch fails for one game — network blip, rate limit, game delisted — we log a warning and return `null`. The sync job continues processing the rest of the library. A single bad game detail response never aborts the entire sync.

---

`Services/ICacheService.cs`

The cache interface defines three operations — get, set, and invalidate. Everything that touches Redis goes through this contract.

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);
    Task InvalidateAsync(params string[] keys);
}
```

**Why abstract the cache behind an interface?**
Same reason as `ISteamApiClient` — testability and swappability. Services like `LibraryService` and `StatsService` depend on `ICacheService`, not `CacheService`. In tests you can pass a no-op mock and the service behaves as if the cache always misses. In production Redis handles it. The services never know which.

**`where T : class`**
The generic constraint on `GetAsync` restricts `T` to reference types. This is necessary because the return type is `T?` — nullable. In C#, nullable works differently for value types (`int?` is `Nullable<int>`) vs reference types (`string?` is just `string` with a null annotation). By constraining to `class`, we guarantee `T?` means a nullable reference, which is what we want when a cache miss returns `null`.

**`params string[] keys` on `InvalidateAsync`**
The `params` keyword lets callers pass keys individually without building an array:
```csharp
await cache.InvalidateAsync("key1", "key2", "key3"); // clean
// vs
await cache.InvalidateAsync(new[] { "key1", "key2", "key3" }); // without params
```

---

`Services/CacheService.cs`

The concrete Redis implementation of `ICacheService`.

```csharp
public class CacheService(IConnectionMultiplexer redis) : ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();
}
```

**`IConnectionMultiplexer`**
This is StackExchange.Redis's connection object. It's registered as a singleton in `Program.cs` — one connection shared across the entire application lifetime. Redis connections are expensive to create, so you create one and reuse it everywhere. `GetDatabase()` is a lightweight call that returns a handle to issue commands against.

**Serialization**
```csharp
public async Task SetAsync<T>(string key, T value, TimeSpan ttl, ...)
{
    var json = JsonSerializer.Serialize(value);
    await _db.StringSetAsync(key, json, ttl);
}
```
Redis stores strings. We serialize objects to JSON before writing and deserialize on read. The TTL (time-to-live) is passed directly to Redis — when it expires, Redis automatically evicts the key. No cleanup code needed.

**Cache key naming**
Keys follow the pattern `steam:{steamId}:{resource}` — for example `steam:76561198012345678:stats`. Namespacing by Steam ID means if this ever expanded to multiple users, cache entries wouldn't collide.

---

`Services/LibraryService.cs`

This is the service that handles all game library queries. It sits between the controller and the database, applying filters, sorting, pagination, and caching.

```csharp
public class LibraryService(AppDbContext db, ICacheService cache, IConfiguration config) : ILibraryService
```

**Cache-aside pattern**
Every read method follows the same structure:
```csharp
var cached = await cache.GetAsync<PagedResult<GameDto>>(cacheKey, ct);
if (cached is not null) return cached;

// ... query the database ...

await cache.SetAsync(cacheKey, result, CacheTtl, ct);
return result;
```
1. Check the cache first
2. On a miss, hit the database
3. Store the result in cache before returning

This is called the **cache-aside** pattern (as opposed to write-through, where the cache is always populated on writes). It's the right choice here because reads are far more frequent than syncs.

**Cache key includes all query params**
```csharp
var cacheKey = $"steam:{_steamId}:library:{query.Genre}:{query.MinPlaytime}:{query.Sort}:{query.Page}:{query.PageSize}";
```
Each unique combination of filters gets its own cache entry. `?genre=Action&sort=playtime&page=1` and `?genre=RPG&sort=playtime&page=1` are cached separately. This means the cache is granular — a request with different params never gets a wrong result.

**LINQ deferred execution**
```csharp
var q = db.Games.AsQueryable();

if (!string.IsNullOrEmpty(query.Genre))
    q = q.Where(g => g.Genres.Contains(query.Genre));

if (query.MinPlaytime.HasValue)
    q = q.Where(g => g.PlaytimeForever >= query.MinPlaytime.Value * 60);

q = query.Sort switch
{
    "playtime"   => q.OrderByDescending(g => g.PlaytimeForever),
    "lastPlayed" => q.OrderByDescending(g => g.LastPlayed),
    _            => q.OrderBy(g => g.Name)
};
```
The query is built up incrementally — nothing executes against the database until `CountAsync` or `ToListAsync` is called. This is LINQ's **deferred execution**. You're composing an expression tree, not running SQL. EF Core translates the final expression into a single optimized SQL query.

**Two database calls per paginated request**
```csharp
var totalCount = await q.CountAsync(ct);
var items = await q.Skip(...).Take(...).ToListAsync(ct);
```
`CountAsync` runs `SELECT COUNT(*)` to get the total for pagination controls. `ToListAsync` runs `SELECT ... LIMIT ... OFFSET ...` for the actual page. This is unavoidable with pagination — you need both the data and the total.

---

`Services/StatsService.cs`

This service has two responsibilities: serve the latest stats snapshot from cache or database, and recompute stats after every sync.

```csharp
public class StatsService(AppDbContext db, ICacheService cache, IConfiguration config) : IStatsService
```

**`GetLatestStatsAsync` — reading stats**
```csharp
var snapshot = await db.StatsSnapshots
    .OrderByDescending(s => s.ComputedAt)
    .FirstOrDefaultAsync(ct);
```
Always reads the most recent row from `StatsSnapshots`. Because stats are pre-computed and stored as JSON, this is a single indexed read — no aggregation, no joins. The stats endpoint is always O(1) regardless of library size.

**`RecomputeAsync` — writing stats**
This runs after every sync. It loads all games into memory and aggregates in C# rather than SQL:
```csharp
var playtimeByGenre = games
    .SelectMany(g => g.Genres.Select(genre => (genre, g.PlaytimeForever)))
    .GroupBy(x => x.genre)
    .ToDictionary(g => g.Key, g => g.Sum(x => x.PlaytimeForever));
```

**Why aggregate in C# and not SQL?**
For a single-user app with a bounded library size, loading all games into memory is cheap. SQL aggregations across array columns (`text[]`) require unnesting, which is more complex to write and harder to maintain. The C# LINQ version is readable and testable. If this were a multi-user app with millions of rows, the answer would be different.

**Why store the result as a JSON blob?**
```csharp
db.StatsSnapshots.Add(new StatsSnapshot
{
    Data = JsonSerializer.Serialize(dto),
    ComputedAt = dto.ComputedAt
});
```
The entire `StatsDto` is serialized and stored in the `jsonb` column. This means reading stats back is just one deserialize call — no reconstruction from multiple columns. The shape of stats can evolve without a migration as long as we handle missing fields gracefully on read.

**Cache invalidation after recompute**
```csharp
await cache.InvalidateAsync(_cacheKey);
```
After writing the new snapshot, the stats cache key is explicitly deleted. The next read will miss the cache, hit the new snapshot, and re-populate the cache. This ensures stats are never stale immediately after a sync.

---

`Services/SyncService.cs`

This is the most complex service — it orchestrates the full sync from Steam into the database. Every other service is read-only; this one writes.

```csharp
public class SyncService(
    AppDbContext db,
    ISteamApiClient steamApi,
    IStatsService stats,
    IConfiguration config,
    ILogger<SyncService> logger) : ISyncService
```

**The sync flow**
1. Fetch all owned games from Steam via `GetOwnedGamesAsync`
2. Load existing `AppId`s from the database into a `HashSet`
3. For each owned game — update if it exists, insert if it's new
4. Save all changes to the database
5. Trigger stats recomputation

**Update path — existing games**
```csharp
await db.Games
    .Where(g => g.AppId == owned.AppId)
    .ExecuteUpdateAsync(s => s
        .SetProperty(g => g.PlaytimeForever, owned.PlaytimeForever)
        .SetProperty(g => g.PlaytimeTwoWeeks, owned.PlaytimeTwoWeeks)
        .SetProperty(g => g.LastPlayed, lastPlayed)
        .SetProperty(g => g.LastSyncedAt, now), ct);
```
`ExecuteUpdateAsync` translates directly to a SQL `UPDATE` without loading the entity into memory first. For a library of 500 games that already exist, this avoids loading 500 objects just to update three columns each.

**Insert path — new games**
```csharp
var details = await steamApi.GetGameDetailsAsync(owned.AppId, ct);
db.Games.Add(new Game { ... });
```
New games get a detail fetch from Steam's Store API to populate genres and the header image. This is the slow path — each new game costs one extra HTTP call with a 1.5s rate limit delay. Existing games skip this entirely.

**Why `HashSet` for existing IDs?**
```csharp
var existingAppIds = await db.Games.Select(g => g.AppId).ToHashSetAsync(ct);
```
`HashSet.Contains()` is O(1). For each of Steam's returned games we check `existingAppIds.Contains(owned.AppId)`. If we used a `List`, that check would be O(n) — for 500 games that's 250,000 comparisons per sync. The `HashSet` makes it 500 comparisons total.

**`CancellationToken` threading through the loop**
```csharp
foreach (var owned in ownedGames)
{
    ct.ThrowIfCancellationRequested();
    ...
}
```
If the app shuts down mid-sync, the cancellation token fires. `ThrowIfCancellationRequested()` at the top of each loop iteration means the sync stops cleanly at the next game boundary rather than halfway through a database write.

**Returns `DateTime`**
```csharp
public async Task<DateTime> SyncAsync(CancellationToken ct = default)
```
The sync timestamp is returned to the caller so the `SyncController` can include it in the response — `{ "syncedAt": "2026-04-18T..." }`. The controller doesn't need to know when the sync ran; the service tells it.

#### Controllers

`Controllers/LibraryController.cs`

Controllers in ASP.NET Core are thin by design — they handle HTTP concerns only and delegate all business logic to services.

```csharp
[ApiController]
[Route("api/v1/library")]
public class LibraryController(ILibraryService library) : ControllerBase
```

**`[ApiController]`**
This attribute enables several automatic behaviors:
- Model validation runs automatically — if `LibraryQueryParams` fails validation, the controller returns a `400 Bad Request` without you writing any validation code
- `[FromQuery]` and `[FromBody]` are inferred — you don't have to explicitly annotate every parameter
- Problem details format is used for error responses automatically

**`[Route("api/v1/library")]`**
The `v1` in the route is API versioning. If we ever need to ship breaking changes, we add a `v2` route alongside `v1` without removing the old one. Callers on `v1` keep working; new callers use `v2`.

**`ControllerBase` not `Controller`**
`ControllerBase` is for API controllers that return data. `Controller` adds view rendering on top — Razor views, MVC patterns. We're building a REST API, so `ControllerBase` is correct. Using `Controller` here would pull in unnecessary dependencies.

**The two endpoints**
```csharp
[HttpGet]
public async Task<ActionResult<PagedResult<GameDto>>> GetLibrary(
    [FromQuery] LibraryQueryParams query, CancellationToken ct)

[HttpGet("{appId:int}")]
public async Task<ActionResult<GameDto>> GetGame(int appId, CancellationToken ct)
```
`{appId:int}` is a route constraint — it only matches if `appId` is a valid integer. A request to `/api/v1/library/abc` returns `404` automatically without reaching your code.

`ActionResult<T>` lets you return either a typed result (`Ok(result)`) or an HTTP status (`NotFound()`) from the same method. The generic parameter `T` tells Swagger what the success response shape looks like.

---

`Controllers/StatsController.cs` and `Controllers/SyncController.cs`

These follow the same pattern — thin wrappers that delegate to services.

```csharp
[HttpGet]
public async Task<ActionResult<StatsDto>> GetStats(CancellationToken ct)
{
    var result = await stats.GetLatestStatsAsync(ct);
    if (result is null) return NotFound("No stats available yet — trigger a sync first.");
    return Ok(result);
}
```

```csharp
[HttpPost]
public async Task<IActionResult> TriggerSync(CancellationToken ct)
{
    var syncedAt = await sync.SyncAsync(ct);
    return Ok(new { syncedAt });
}
```

`IActionResult` on `TriggerSync` instead of `ActionResult<T>` — the sync response is an anonymous object `{ syncedAt }`, not a named DTO. It's a one-off response shape that doesn't warrant its own record type.

**`CancellationToken ct` on every action**
ASP.NET Core automatically binds the request's cancellation token to this parameter. If the client disconnects mid-request, the token is cancelled and propagates down through the service and database calls — stopping work that would otherwise complete for nobody.

---

`Jobs/LibrarySyncJob.cs`

This is the background service that runs the sync on a schedule without any user interaction.

```csharp
public class LibrarySyncJob(
    IServiceProvider services,
    IConfiguration config,
    ILogger<LibrarySyncJob> logger) : BackgroundService
```

**`BackgroundService`**
This is ASP.NET Core's base class for long-running background tasks. It implements `IHostedService` and manages the lifecycle — it starts when the app starts and stops when the app shuts down. You only need to override one method: `ExecuteAsync`.

**Why inject `IServiceProvider` instead of `ISyncService` directly?**
`SyncService` is registered as `Scoped` — it's created once per request and disposed at the end. `LibrarySyncJob` is a `Singleton` — it lives for the entire application lifetime. A singleton can't hold a reference to a scoped service directly; the scoped service would never be disposed and its `DbContext` would accumulate state across syncs.

The fix is to inject `IServiceProvider` and create a new scope for each sync run:
```csharp
private async Task RunSyncAsync(CancellationToken ct)
{
    using var scope = services.CreateScope();
    var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();
    await syncService.SyncAsync(ct);
}
```
Each sync gets a fresh `SyncService`, a fresh `AppDbContext`, and a fresh database connection. When the `using` block exits, everything is disposed cleanly.

**`PeriodicTimer`**
```csharp
using var timer = new PeriodicTimer(_interval);
while (await timer.WaitForNextTickAsync(stoppingToken))
    await RunSyncAsync(stoppingToken);
```
`PeriodicTimer` is the modern .NET way to run something on an interval. Unlike `Task.Delay` in a loop, it doesn't drift — if a sync takes 45 seconds and the interval is 30 minutes, the next tick fires 30 minutes after the previous tick started, not 30 minutes after it finished.

**Initial sync on startup**
```csharp
await RunSyncAsync(stoppingToken);

using var timer = new PeriodicTimer(_interval);
```
The first sync runs immediately before the timer starts. Without this, a fresh deployment would serve an empty library for up to 30 minutes while waiting for the first scheduled tick.

---

`Program.cs`

This is the application entry point and the DI composition root — the one place where all dependencies are wired together.

**Service registration order**
```csharp
// Infrastructure first
builder.Services.AddDbContext<AppDbContext>(...);
builder.Services.AddSingleton<IConnectionMultiplexer>(...);
builder.Services.AddSingleton<ICacheService, CacheService>();

// External clients
builder.Services.AddHttpClient<ISteamApiClient, SteamApiClient>();

// Business logic
builder.Services.AddScoped<ILibraryService, LibraryService>();
builder.Services.AddScoped<IStatsService, StatsService>();
builder.Services.AddScoped<ISyncService, SyncService>();

// Background jobs
builder.Services.AddHostedService<LibrarySyncJob>();
```
The DI container resolves dependencies at runtime — registration order doesn't technically matter for resolution. But organizing it this way makes the dependency graph readable at a glance.

**Lifetimes**
| Registration | Lifetime | Created |
|---|---|---|
| `AddSingleton` | Once per app | `IConnectionMultiplexer`, `ICacheService` |
| `AddScoped` | Once per request | `ILibraryService`, `IStatsService`, `ISyncService`, `AppDbContext` |
| `AddHostedService` | Singleton | `LibrarySyncJob` |

**Auto-migrate on startup**
```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
```
On every startup, pending EF migrations are applied automatically. In `docker compose up`, the backend waits for Postgres to be healthy before starting — so by the time this runs, the database is guaranteed to be available.

**CORS**
```csharp
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p
        .WithOrigins(builder.Configuration["Frontend:Origin"] ?? "http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()));
```
The frontend origin is configurable via `appsettings.json` or environment variable. In Docker, the backend and frontend are on different ports — without CORS, the browser would block all API calls from the frontend.
