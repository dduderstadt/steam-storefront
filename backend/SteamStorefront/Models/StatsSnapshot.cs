using System.ComponentModel.DataAnnotations;

namespace SteamStorefront.Models;
/// <summary>
/// This is the EF Core entity for the StatsSnapshots table. It stores pre-computed stats data as a JSON blob in the Data column.
/// StatsService writes a new row after each sync; the /api/v1/stats endpoint reads the latest row. This makes stats reads 0(1) -
/// no aggregation at query time. O(1) stats reads.
///</summary>
public class StatsSnapshot
{
    /// <summary>
    /// Standard generated int PK
    /// </summary>
    [Key]
    public int Id { get; set; }
    /// <summary>
    /// the serialized StatsDto as a JSON string. Storing the full stats payload as a blob is a deliberate tradeoff; it's simple
    /// and fast to read, but the schema is opaque to SQL - you can't query individual fields without parsing. Acceptable here because
    /// stats are always read as a whole, never partially.
    /// </summary>
    public string Data { get; set; } = "{}";
    /// <summary>
    /// Timestamp of when this snapshot was generated. Returned to the frontend so it can show data freshness.
    /// </summary>
    public DateTime ComputedAt { get; set; }
}