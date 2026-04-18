namespace SteamStorefront.Steam;

public record OwnedGame(
    int AppId,
    string Name,
    int PlaytimeForever,
    int PlaytimeTwoWeeks,
    long? RtimeLastPlayed);

public record GameDetails(
    int AppId,
    string Name,
    string? ShortDescription,
    string? HeaderImage,
    string[] Genres);
