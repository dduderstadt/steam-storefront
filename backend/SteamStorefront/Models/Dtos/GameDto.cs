namespace SteamStorefront.Models.Dtos;

public record GameDto(int AppId, string Name, string? ShortDescription, string? HeaderImageUrl, string[] Genres, int PlaytimeForever, int PlaytimeTwoWeeks, DateTime? LastPlayed);