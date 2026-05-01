using Microsoft.EntityFrameworkCore;
using SteamStorefront.Data;
using SteamStorefront.Models;
using SteamStorefront.Steam;

namespace SteamStorefront.Services;

/// <summary>
/// Orchestrates the full library sync pipeline: fetch owned games from Steam,
/// upsert into the database, then trigger stats recomputation.
/// </summary>
public class SyncService(
    AppDbContext db,
    ISteamApiClient steamApi,
    IStatsService stats,
    IConfiguration config,
    ILogger<SyncService> logger) : ISyncService
{
    // Throws at startup rather than at first sync if the config key is missing.
    private readonly string _steamId = config["Steam:SteamId"]
        ?? throw new InvalidOperationException("Steam:SteamId is not configured.");

    /// <summary>
    /// Fetches the owner's current game list from Steam and upserts each entry.
    /// Existing games get playtime and LastPlayed updated via a bulk ExecuteUpdateAsync
    /// (no change-tracking overhead). New games get a full detail fetch from the store API
    /// before insert — this is rate-limited by Steam, so only new AppIds trigger a detail call.
    /// </summary>
    public async Task<DateTime> SyncAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Starting library sync for Steam ID {SteamId}", _steamId);

        var ownedGames = await steamApi.GetOwnedGamesAsync(_steamId, ct);
        // Pre-load all existing AppIds into a HashSet for O(1) lookup inside the loop.
        var existingAppIds = await db.Games.Select(g => g.AppId).ToHashSetAsync(ct);
        var now = DateTime.UtcNow;

        foreach (var owned in ownedGames)
        {
            ct.ThrowIfCancellationRequested();

            // Steam returns last_played as a Unix timestamp; convert to UTC DateTime.
            var lastPlayed = owned.RtimeLastPlayed.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(owned.RtimeLastPlayed.Value).UtcDateTime
                : (DateTime?)null;

            if (existingAppIds.Contains(owned.AppId))
            {
                // Update only mutable fields — skip detail fetch, it's already been done.
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
                // New game — fetch store details (name, description, image, genres).
                // Falls back to the owned-game name if the detail call returns null.
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
