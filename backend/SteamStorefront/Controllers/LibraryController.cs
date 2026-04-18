using Microsoft.AspNetCore.Mvc;
using SteamStorefront.Models.Dtos;
using SteamStorefront.Services;

namespace SteamStorefront.Controllers;

[ApiController]
[Route("api/v1/library")]
public class LibraryController(ILibraryService library) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<GameDto>>> GetLibrary(
        [FromQuery] LibraryQueryParams query,
        CancellationToken ct)
    {
        var result = await library.GetGamesAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{appId:int}")]
    public async Task<ActionResult<GameDto>> GetGame(int appId, CancellationToken ct)
    {
        var game = await library.GetGameAsync(appId, ct);
        if (game is null) return NotFound();
        return Ok(game);
    }
}
