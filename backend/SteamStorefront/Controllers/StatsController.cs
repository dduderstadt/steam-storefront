using Microsoft.AspNetCore.Mvc;
using SteamStorefront.Models.Dtos;
using SteamStorefront.Services;

namespace SteamStorefront.Controllers;

[ApiController]
[Route("api/v1/stats")]
public class StatsController(IStatsService stats) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<StatsDto>> GetStats(CancellationToken ct)
    {
        var result = await stats.GetLatestStatsAsync(ct);
        if (result is null) return NotFound("No stats available yet — trigger a sync first.");
        return Ok(result);
    }
}
