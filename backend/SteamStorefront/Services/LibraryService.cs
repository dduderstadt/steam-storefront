using Microsoft.EntityFrameworkCore;
using SteamStorefront.Data;
using SteamStorefront.Models.Dtos;

namespace SteamStorefront.Services;

/// <summary>
/// Implements game library queries with Redis caching.
/// Cache keys are namespaced by Steam ID and include all query parameters so different
/// filter combinations each get their own cache entry.
/// </summary>
public class LibraryService(AppDbContext db, ICacheService cache, IConfiguration config) : ILibraryService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private readonly string _steamId = config["Steam:SteamId"] ?? string.Empty;

    /// <summary>
    /// Applies genre, playtime, and sort filters, then paginates.
    /// Returns a cached result if one exists for the exact query params; otherwise queries
    /// the database and writes the result to cache before returning.
    /// </summary>
    public async Task<PagedResult<GameDto>> GetGamesAsync(LibraryQueryParams query, CancellationToken ct = default)
    {
        var cacheKey = $"steam:{_steamId}:library:{query.Genre}:{query.MinPlaytime}:{query.Sort}:{query.Page}:{query.PageSize}";
        var cached = await cache.GetAsync<PagedResult<GameDto>>(cacheKey, ct);
        if (cached is not null) return cached;

        var q = db.Games.AsQueryable();

        if (!string.IsNullOrEmpty(query.Genre))
            q = q.Where(g => g.Genres.Contains(query.Genre));

        // MinPlaytime arrives in hours from the frontend; convert to minutes for the DB query.
        if (query.MinPlaytime.HasValue)
            q = q.Where(g => g.PlaytimeForever >= query.MinPlaytime.Value * 60);

        q = query.Sort switch
        {
            "playtime" => q.OrderByDescending(g => g.PlaytimeForever),
            "lastPlayed" => q.OrderByDescending(g => g.LastPlayed),
            _ => q.OrderBy(g => g.Name)
        };

        var totalCount = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(g => new GameDto(g.AppId, g.Name, g.Description, g.HeaderImageUrl, g.Genres, g.PlaytimeForever, g.PlaytimeTwoWeeks, g.LastPlayed))
            .ToListAsync(ct);

        var result = new PagedResult<GameDto>(items, totalCount, query.Page, query.PageSize);
        await cache.SetAsync(cacheKey, result, CacheTtl, ct);
        return result;
    }

    /// <summary>
    /// Looks up a single game by AppId. Returns null if not found so the controller
    /// can return a 404 without throwing an exception.
    /// </summary>
    public async Task<GameDto?> GetGameAsync(int appId, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([appId], ct);
        if (game is null) return null;
        return new GameDto(game.AppId, game.Name, game.Description, game.HeaderImageUrl, game.Genres, game.PlaytimeForever, game.PlaytimeTwoWeeks, game.LastPlayed);
    }
}
