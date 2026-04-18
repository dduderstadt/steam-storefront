using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SteamStorefront.Data;
using SteamStorefront.Models;
using SteamStorefront.Models.Dtos;

namespace SteamStorefront.Services;

public class StatsService(AppDbContext db, ICacheService cache, IConfiguration config) : IStatsService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private readonly string _cacheKey = $"steam:{config["Steam:SteamId"]}:stats";

    public async Task<StatsDto?> GetLatestStatsAsync(CancellationToken ct = default)
    {
        var cached = await cache.GetAsync<StatsDto>(_cacheKey, ct);
        if (cached is not null) return cached;

        var snapshot = await db.StatsSnapshots
            .OrderByDescending(s => s.ComputedAt)
            .FirstOrDefaultAsync(ct);

        if (snapshot is null) return null;

        var dto = JsonSerializer.Deserialize<StatsDto>(snapshot.Data);
        if (dto is not null)
            await cache.SetAsync(_cacheKey, dto, CacheTtl, ct);

        return dto;
    }

    public async Task RecomputeAsync(CancellationToken ct = default)
    {
        var games = await db.Games.ToListAsync(ct);

        var playtimeByGenre = games
            .SelectMany(g => g.Genres.Select(genre => (genre, g.PlaytimeForever)))
            .GroupBy(x => x.genre)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.PlaytimeForever));

        var topGames = games
            .OrderByDescending(g => g.PlaytimeForever)
            .Take(10)
            .Select(g => new GamePlaytimeStat(g.AppId, g.Name, g.PlaytimeForever))
            .ToList();

        var lastSyncedAt = games.Count > 0 ? games.Max(g => g.LastSyncedAt) : DateTime.UtcNow;

        var dto = new StatsDto(
            TotalGames: games.Count,
            TotalPlaytimeMinutes: games.Sum(g => g.PlaytimeForever),
            PlaytimeByGenre: playtimeByGenre,
            TopGames: topGames,
            ComputedAt: DateTime.UtcNow,
            LastSyncedAt: lastSyncedAt
        );

        db.StatsSnapshots.Add(new StatsSnapshot
        {
            Data = JsonSerializer.Serialize(dto),
            ComputedAt = dto.ComputedAt
        });
        await db.SaveChangesAsync(ct);

        await cache.InvalidateAsync(_cacheKey);
    }
}
