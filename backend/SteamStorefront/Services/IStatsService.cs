using SteamStorefront.Models.Dtos;

namespace SteamStorefront.Services;

/// <summary>
/// Manages the pre-computed stats snapshot. Stats are never aggregated at query time —
/// they are computed once after each sync and stored in the StatsSnapshots table.
/// </summary>
public interface IStatsService
{
    /// <summary>
    /// Reads the latest snapshot row and deserializes it into a <see cref="StatsDto"/>.
    /// Returns null if no snapshot exists yet (e.g. before the first sync).
    /// </summary>
    Task<StatsDto?> GetLatestStatsAsync(CancellationToken ct = default);

    /// <summary>
    /// Aggregates the current library state and writes a new snapshot row.
    /// Called by <see cref="ISyncService"/> at the end of each sync.
    /// </summary>
    Task RecomputeAsync(CancellationToken ct = default);
}
