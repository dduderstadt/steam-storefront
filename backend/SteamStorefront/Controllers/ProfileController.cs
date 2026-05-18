using Microsoft.AspNetCore.Mvc;
using SteamStorefront.Steam;

namespace SteamStorefront.Controllers;

[ApiController]
[Route("api/v1/profile")]
public class ProfileController(ISteamApiClient steamApi, IConfiguration config) : ControllerBase
{
    private readonly string _steamId = config["Steam:SteamId"] ?? throw new InvalidOperationException("Steam:SteamId is not configured.");

    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var profile = await steamApi.GetPlayerSummaryAsync(_steamId, ct);
        if (profile is null)
        {
            return NotFound();
        }
        else
        {
            return Ok(profile);
        }
    }
}