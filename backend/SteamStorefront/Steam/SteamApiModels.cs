namespace SteamStorefront.Steam;

// Record is a C# type for immutable data containers. It auto-generated
// a constructor, equality comparison, and ToString() from its properties.
// Think of it as a lightweight class designed purely for holding data
// with no behavior.

/// <summary>
/// Projection of the relevant fields from IPlayerService/GetOwnedGames.
/// RtimeLastPlayed is a Unix timestamp (seconds since epoch); nullable because
/// games with zero playtime return null or 0 from the API.
/// </summary>
public record OwnedGame(
    int AppId,
    string Name,
    int PlaytimeForever,
    int PlaytimeTwoWeeks,
    long? RtimeLastPlayed);

/// <summary>
/// Projection of the relevant fields from the store appdetails endpoint.
/// All fields except AppId are nullable because the detail call can return partial data
/// or fail silently — sync falls back to the OwnedGame name if details are unavailable.
/// </summary>
public record GameDetails(
    int AppId,
    string Name,
    string? ShortDescription,
    string? HeaderImage,
    string[] Genres);

/// <summary>
/// Projection of the relevant field from the store /profile endpoint.
/// All fields except DisplayName are nullable.
/// </summary>
/// <param name="DisplayName"></param>
/// <param name="AvatarUrl"></param>
public record PlayerSummary(
    string DisplayName,
    string? AvatarUrl
);