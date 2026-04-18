using SteamStorefront.Models.Dtos;

namespace SteamStorefront.Services;

public interface ILibraryService
{
    Task<PagedResult<GameDto>> GetGamesAsync(LibraryQueryParams query, CancellationToken ct = default);
    Task<GameDto?> GetGameAsync(int appId, CancellationToken ct = default);
}
