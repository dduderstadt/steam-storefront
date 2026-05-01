namespace SteamStorefront.Services;

/// <summary>
/// Orchestrates a full library sync: fetches owned games from Steam, upserts new records
/// into the database, and triggers stats recomputation.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Runs a full sync and returns the UTC timestamp of when it completed.
    /// The timestamp is surfaced to callers so the API can report when data was last refreshed.
    /// </summary>
    Task<DateTime> SyncAsync(CancellationToken ct = default);
}
