using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SteamStorefront.Models;

/// <summary>
/// This is a historical log - every sync writes a new row recording the playtime
/// for each game at that point in time.
/// </summary>
public class PlaytimeRecord
{
    /// <summary>
    /// Id - long not int because this table grows unbounded.
    /// Every sync, every game gets a new row.
    /// A library of 500 games syncing every 30 minutes generates thousands
    /// of rows per day. int would last years, but long is the right habit for append-only log tables.
    /// </summary>
    [Key]
    public long Id { get; set; }
    /// <summary>
    /// FK to Games.
    /// No navigation property on the entity itself because we only ever query playtime
    /// records by AppId directly - we don't need EF to traverse the relationship.
    /// </summary>
    public int AppId { get; set; }
    public int PlaytimeMinutes { get; set; }
    /// <summary>
    /// The composite index on (AppId, RecordedAt) in AppDbContext makes time-range queries
    /// per game fast, e.g. "playtime trend for game X over the last 30 days".
    /// 
    /// The current implementation of SyncService doesn't actually write PlaytimeRecord rows
    /// yet - it only upserts the Game entity. Writing the history log is something we'd add as a next iteration.
    /// The table and entity are there, the sync job just needs a line to insert a record after
    /// each upsert.
    /// </summary>
    public DateTime RecordedAt { get; set; }
}