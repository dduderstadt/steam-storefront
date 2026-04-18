using System.ComponentModel.DataAnnotations;

namespace SteamStorefront.Models;

public class Game
{
    [Key]
    public int AppId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? HeaderImageUrl { get; set; }
    public string[] Genres { get; set; } = [];
    public int PlaytimeForever { get; set; }
    public int PlaytimeTwoWeeks { get; set; }
    public DateTime? LastPlayed { get; set; }
    public DateTime FirstSyncedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
}
