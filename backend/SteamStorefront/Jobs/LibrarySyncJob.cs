using SteamStorefront.Services;

namespace SteamStorefront.Jobs;

public class LibrarySyncJob(
    IServiceProvider services,
    IConfiguration config,
    ILogger<LibrarySyncJob> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(
        config.GetValue("Steam:SyncIntervalMinutes", 30));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Sync job started — interval: {Interval}", _interval);

        await RunSyncAsync(stoppingToken);

        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunSyncAsync(stoppingToken);
    }

    private async Task RunSyncAsync(CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();
        try
        {
            await syncService.SyncAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // App shutting down — normal exit
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Sync job failed");
        }
    }
}
