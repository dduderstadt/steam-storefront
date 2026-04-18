using Microsoft.EntityFrameworkCore;
using SteamStorefront.Models;

namespace SteamStorefront.Data;

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
            e.Property(g => g.Genres).HasColumnType("text[]");
        });

        modelBuilder.Entity<PlaytimeRecord>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => new { r.AppId, r.RecordedAt });
            e.HasOne<Game>().WithMany().HasForeignKey(r => r.AppId);
        });

        modelBuilder.Entity<StatsSnapshot>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Data).HasColumnType("jsonb");
        });
    }
}
