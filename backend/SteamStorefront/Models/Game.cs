using System.ComponentModel.DataAnnotations;

namespace SteamStorefront.Models;

/// <summary>
/// The EF Core entity for the Games table, one row per game in the Steam library, populated by SyncService.
/// </summary>
public class Game
{
    /// <summary>
    /// Steam's own ID for the game, used as the PK instead of a generated int.
    /// We own this key because Steam owns the canonical ID.
    /// </summary>
    [Key]
    public int AppId { get; set; }

    /// <summary>
    /// [Required] prevents null in the DB column and [MaxLength(500)] maps to varchar(500)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Nullable because store details aren't fetched until the game's first sync pass.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Nullable because store details aren't fetched until the game's first sync pass.
    /// </summary>
    public string? HeaderImageUrl { get; set; }

    /// <summary>
    /// Stored as a Postgres text[] array column. This lets us filter by
    /// genre with a native array `Contains` query instead of a join table.
    /// 
    /// Typical relational approach for genres would be two extra tables:
    /// a Genres table and a GameGenres table.
    /// To query "all Action games" you'd write a SQL JOIN:
    /// SELECT g* FROM Games g
    /// JOIN GameGenres gg ON g.AppId = gg.AppId
    /// JOIN Genres genre ON gg.GenreId = genre.Id
    /// WHERE genre.Name = 'Action'
    /// 
    /// ...with text[] in Postgres, genres live directly on the Games row as an array column.
    /// The same query becomes:
    /// SELECT * FROM Games WHERE 'Action' = ANY(Genres)
    /// 
    /// One table, no join. In EF Core with Npgsql, g.Genres.Contains(query.Genre) translates to exactly
    /// that ANY expression.
    /// 
    /// Tradeoff:
    /// - Give up some relational purity
    /// - Can't easily query "what genres exist across all games" without unnesting the array
    /// - Can't enforce genre values against a reference table
    /// 
    /// If this were a multi-tenant app where users could define their own genres, a join table would be the right call.
    /// </summary>
    public string[] Genres { get; set; } = [];
    /// <summary>
    /// Stored in minutes, exactly as Steam returns them. The conversion to hours
    /// happens in the frontend.
    /// </summary>
    public int PlaytimeForever { get; set; }
    /// <summary>
    /// Stored in minutes, exactly as Steam returns them. The conversion to hours
    /// happens in the frontend.
    /// </summary>
    public int PlaytimeTwoWeeks { get; set; }

    /// <summary>
    /// Nullable because games with zero playtime have never been launched and Steam returns null.
    /// </summary>
    public DateTime? LastPlayed { get; set; }
    /// <summary>
    /// Lets us know when we first saw the game and when we last updated it. Set once on insert.
    /// </summary>
    public DateTime FirstSyncedAt { get; set; }
    /// <summary>
    /// Lets us know when we first saw the game and when we last updated it. Updated on every sync.
    /// </summary>
    public DateTime LastSyncedAt { get; set; }
}
