namespace SteamStorefront.Services;

public interface ISyncService
{
    Task<DateTime> SyncAsync(CancellationToken ct = default);
}
