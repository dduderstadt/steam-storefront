using SteamStorefront.Services;

namespace SteamStorefront.Jobs;

/// <summary>
/// Background service that runs a library sync on a configurable interval (default: 30 minutes).
/// Registered as a hosted service in Program.cs so ASP.NET Core manages its lifetime.
/// </summary>
public class LibrarySyncJob(
    IServiceProvider services,
    IConfiguration config,
    ILogger<LibrarySyncJob> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(
        config.GetValue("Steam:SyncIntervalMinutes", 30));

    /// <summary>
    /// Runs an immediate sync on startup, then repeats on the configured interval.
    /// PeriodicTimer is used instead of Task.Delay because it doesn't accumulate drift —
    /// each tick fires at a fixed interval regardless of how long the sync took.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Sync job started — interval: {Interval}", _interval);

        await RunSyncAsync(stoppingToken);

        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunSyncAsync(stoppingToken);
    }

    /// <summary>
    /// Creates a new DI scope for each sync run. SyncService and its dependencies are scoped,
    /// so they can't be injected directly into a singleton-lifetime hosted service — a new
    /// scope is required per invocation to avoid captive dependency issues.
    /// </summary>
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
            // App shutting down — normal exit, not an error.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Sync job failed");
        }
    }
}
