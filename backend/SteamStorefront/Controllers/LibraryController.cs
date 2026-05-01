using Microsoft.AspNetCore.Mvc;
using SteamStorefront.Models.Dtos;
using SteamStorefront.Services;

namespace SteamStorefront.Controllers;

/// <summary>
/// Thin route handler for the game library. All business logic lives in <see cref="ILibraryService"/>;
/// the controller is responsible only for mapping HTTP requests to service calls and returning the correct status codes.
/// </summary>
[ApiController]
[Route("api/v1/library")]
public class LibraryController(ILibraryService library) : ControllerBase
{
    /// <summary>
    /// GET /api/v1/library
    /// Returns a paginated, filtered list of games. Query params are bound automatically
    /// by ASP.NET Core from the URL via [FromQuery].
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<GameDto>>> GetLibrary(
        [FromQuery] LibraryQueryParams query,
        CancellationToken ct)
    {
        var result = await library.GetGamesAsync(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/library/{appId}
    /// Returns a single game. Returns 404 if the AppId isn't in the database —
    /// the service returns null rather than throwing so the controller decides the HTTP status.
    /// </summary>
    [HttpGet("{appId:int}")]
    public async Task<ActionResult<GameDto>> GetGame(int appId, CancellationToken ct)
    {
        var game = await library.GetGameAsync(appId, ct);
        if (game is null) { return NotFound(); }
        return Ok(game);
    }
}
