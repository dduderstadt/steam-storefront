namespace SteamStorefront.Models.Dtos;

/// <summary>
/// Read-only projection of a <see cref="SteamStorefront.Models.Game"/> entity sent to the frontend.
/// A record is used instead of a class because DTOs are immutable data carriers — no behavior, no setters.
/// Nullable fields mirror the entity: ShortDescription and HeaderImageUrl may not be populated yet,
/// LastPlayed is null for games that have never been launched.
/// </summary>
public record GameDto(int AppId, string Name, string? ShortDescription, string? HeaderImageUrl, string[] Genres, int PlaytimeForever, int PlaytimeTwoWeeks, DateTime? LastPlayed);