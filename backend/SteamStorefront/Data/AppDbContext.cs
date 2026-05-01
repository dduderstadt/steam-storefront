using Microsoft.EntityFrameworkCore;
using SteamStorefront.Models;

namespace SteamStorefront.Data;

/// <summary>
/// EF Core database context. Defines the three tables and configures any schema details
/// that can't be expressed through data annotations alone (column types, indexes, FK relationships).
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<PlaytimeRecord> PlaytimeRecords => Set<PlaytimeRecord>();
    public DbSet<StatsSnapshot> StatsSnapshots => Set<StatsSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Game>(e =>
        {
            e.HasKey(g => g.AppId);
            e.Property(g => g.Name).IsRequired().HasMaxLength(500);
            // text[] maps to a native Postgres array column, enabling ANY() queries for genre filtering.
            e.Property(g => g.Genres).HasColumnType("text[]");
        });

        modelBuilder.Entity<PlaytimeRecord>(e =>
        {
            e.HasKey(r => r.Id);
            // Composite index makes time-range queries per game fast (e.g. playtime trend over 30 days).
            e.HasIndex(r => new { r.AppId, r.RecordedAt });
            // FK without a navigation property — we only query by AppId directly, never traverse the relationship.
            e.HasOne<Game>().WithMany().HasForeignKey(r => r.AppId);
        });

        modelBuilder.Entity<StatsSnapshot>(e =>
        {
            e.HasKey(s => s.Id);
            // jsonb stores the serialized StatsDto as binary JSON in Postgres,
            // enabling indexing and querying individual fields if needed in future.
            e.Property(s => s.Data).HasColumnType("jsonb");
        });
    }
}
