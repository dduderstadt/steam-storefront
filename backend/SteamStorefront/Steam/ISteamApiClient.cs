namespace SteamStorefront.Steam;

public interface ISteamApiClient
{
    Task<IReadOnlyList<OwnedGame>> GetOwnedGamesAsync(string steamId, CancellationToken ct = default);
    Task<GameDetails?> GetGameDetailsAsync(int appId, CancellationToken ct = default);
}
