using Microsoft.AspNetCore.Mvc;
using SteamStorefront.Services;

namespace SteamStorefront.Controllers;

/// <summary>
/// Exposes a manual sync trigger. The background job runs on a schedule automatically,
/// but this endpoint allows triggering a sync on demand (e.g. after adding new games to the library).
/// </summary>
[ApiController]
[Route("api/v1/sync")]
public class SyncController(ISyncService sync) : ControllerBase
{
    /// <summary>
    /// POST /api/v1/sync
    /// Triggers a full library sync and returns the UTC timestamp of when it completed.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> TriggerSync(CancellationToken ct)
    {
        var syncedAt = await sync.SyncAsync(ct);
        return Ok(new { syncedAt });
    }
}
