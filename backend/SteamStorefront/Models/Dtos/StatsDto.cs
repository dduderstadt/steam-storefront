namespace SteamStorefront.Models.Dtos;

/// <summary>
/// Slim projection used only inside <see cref="StatsDto"/>. Carries just enough data
/// to render a ranked playtime entry — no image, genres, or description needed.
/// </summary>
public record GamePlaytimeStat(int AppId, string Name, int PlaytimeMinutes);

/// <summary>
/// The full stats payload returned by GET /api/v1/stats. Deserialized from the latest
/// <see cref="SteamStorefront.Models.StatsSnapshot"/> row — never computed at query time.
/// PlaytimeByGenre is a Dictionary because the genre set isn't fixed; it's whatever genres
/// exist in the library at the time of the last sync.
/// </summary>
public record StatsDto(
int TotalGames,
int TotalPlaytimeMinutes,
Dictionary<string, int> PlaytimeByGenre,
List<GamePlaytimeStat> TopGames,
DateTime ComputedAt,
DateTime LastSyncedAt
);