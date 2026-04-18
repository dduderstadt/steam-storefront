using Microsoft.EntityFrameworkCore;
using SteamStorefront.Data;
using SteamStorefront.Models;
using SteamStorefront.Steam;

namespace SteamStorefront.Services;

public class SyncService(
    AppDbContext db,
    ISteamApiClient steamApi,
    IStatsService stats,
    IConfiguration config,
    ILogger<SyncService> logger) : ISyncService
{
    private readonly string _steamId = config["Steam:SteamId"]
        ?? throw new InvalidOperationException("Steam:SteamId is not configured.");

    public async Task<DateTime> SyncAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Starting library sync for Steam ID {SteamId}", _steamId);

        var ownedGames = await steamApi.GetOwnedGamesAsync(_steamId, ct);
        var existingAppIds = await db.Games.Select(g => g.AppId).ToHashSetAsync(ct);
        var now = DateTime.UtcNow;

        foreach (var owned in ownedGames)
        {
            ct.ThrowIfCancellationRequested();

            var lastPlayed = owned.RtimeLastPlayed.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(owned.RtimeLastPlayed.Value).UtcDateTime
                : (DateTime?)null;

            if (existingAppIds.Contains(owned.AppId))
            {
                await db.Games
                    .Where(g => g.AppId == owned.AppId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(g => g.PlaytimeForever, owned.PlaytimeForever)
                        .SetProperty(g => g.PlaytimeTwoWeeks, owned.PlaytimeTwoWeeks)
                        .SetProperty(g => g.LastPlayed, lastPlayed)
                        .SetProperty(g => g.LastSyncedAt, now), ct);
            }
            else
            {
                var details = await steamApi.GetGameDetailsAsync(owned.AppId, ct);
                db.Games.Add(new Game
                {
                    AppId = owned.AppId,
                    Name = details?.Name ?? owned.Name,
                    Description = details?.ShortDescription,
                    HeaderImageUrl = details?.HeaderImage,
                    Genres = details?.Genres ?? [],
                    PlaytimeForever = owned.PlaytimeForever,
                    PlaytimeTwoWeeks = owned.PlaytimeTwoWeeks,
                    LastPlayed = lastPlayed,
                    FirstSyncedAt = now,
                    LastSyncedAt = now
                });
            }
        }

        await db.SaveChangesAsync(ct);
        await stats.RecomputeAsync(ct);

        logger.LogInformation("Sync complete — {Count} games processed", ownedGames.Count);
        return now;
    }
}
