using Microsoft.AspNetCore.Mvc;
using SteamStorefront.Services;

namespace SteamStorefront.Controllers;

[ApiController]
[Route("api/v1/sync")]
public class SyncController(ISyncService sync) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> TriggerSync(CancellationToken ct)
    {
        var syncedAt = await sync.SyncAsync(ct);
        return Ok(new { syncedAt });
    }
}
