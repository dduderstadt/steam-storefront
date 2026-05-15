using SteamStorefront.Models.Dtos;

namespace SteamStorefront.Services;

/// <summary>
/// Handles all read operations against the game library.
/// Sits between the controller and the database; applies filters, pagination, and caching.
/// </summary>
public interface ILibraryService
{
    /// <summary>
    /// Returns a paginated, filtered list of games. Results are cached in Redis;
    /// cache is invalidated after each sync.
    /// </summary>
    Task<PagedResult<GameDto>> GetGamesAsync(LibraryQueryParams query, CancellationToken ct = default);

    /// <summary>
    /// Returns a single game by Steam AppId, or null if not found.
    /// Returning null (rather than throwing) lets the controller call NotFound() cleanly.
    /// </summary>
    Task<GameDto?> GetGameAsync(int appId, CancellationToken ct = default);

    /// <summary>
    /// Returns a list of all unique genres in the library.
    /// Used to populate auto-complete for genre filter.
    /// </summary>
    /// <returns>A list of all unique genres.</returns>
    Task<IReadOnlyList<string>> GetGenresAsync(CancellationToken ct = default);
}
