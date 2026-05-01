namespace SteamStorefront.Steam;

/// <summary>
/// Abstraction over the Steam Web API and Store API.
/// Isolated behind an interface so SyncService never takes a direct dependency on HTTP;
/// tests can mock this without making real network calls.
/// </summary>
public interface ISteamApiClient
{
    /// <summary>
    /// Returns all games owned by the given Steam ID, including playtime and last-played data.
    /// Calls IPlayerService/GetOwnedGames.
    /// </summary>
    Task<IReadOnlyList<OwnedGame>> GetOwnedGamesAsync(string steamId, CancellationToken ct = default);

    /// <summary>
    /// Fetches store details (description, image, genres) for a single AppId.
    /// Returns null if the app doesn't exist or the call fails — sync continues without it.
    /// Rate-limited internally: Steam's appdetails endpoint has no bulk variant.
    /// </summary>
    Task<GameDetails?> GetGameDetailsAsync(int appId, CancellationToken ct = default);
}
