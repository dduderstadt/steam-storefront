namespace SteamStorefront.Models.Dtos;

public record GamePlaytimeStat(int AppId, string Name, int PlaytimeMinutes);
public record StatsDto(
int TotalGames,
int TotalPlaytimeMinutes,
Dictionary<string, int> PlaytimeByGenre,
List<GamePlaytimeStat> TopGames,
DateTime ComputedAt,
DateTime LastSyncedAt
);