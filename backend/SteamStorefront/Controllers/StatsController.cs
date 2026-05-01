using Microsoft.AspNetCore.Mvc;
using SteamStorefront.Models.Dtos;
using SteamStorefront.Services;

namespace SteamStorefront.Controllers;

/// <summary>
/// Exposes the pre-computed stats snapshot. This endpoint is always O(1) —
/// it reads the latest row from StatsSnapshots, never aggregates at query time.
/// </summary>
[ApiController]
[Route("api/v1/stats")]
public class StatsController(IStatsService stats) : ControllerBase
{
    /// <summary>
    /// GET /api/v1/stats
    /// Returns the latest stats snapshot. Returns 404 with a human-readable message
    /// if no sync has run yet and the snapshot table is empty.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<StatsDto>> GetStats(CancellationToken ct)
    {
        var result = await stats.GetLatestStatsAsync(ct);
        if (result is null) { return NotFound("No stats available yet — trigger a sync first."); }
        return Ok(result);
    }
}
