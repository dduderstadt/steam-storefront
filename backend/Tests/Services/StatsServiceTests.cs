using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using FluentAssertions;
using SteamStorefront.Data;
using SteamStorefront.Models;
using SteamStorefront.Models.Dtos;
using SteamStorefront.Services;

namespace SteamStorefront.Tests.Services;

/// <summary>
/// Unit tests for <see cref="StatsService"/>.
/// Each test uses an isolated in-memory database and a mocked cache.
/// </summary>
public class StatsServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<ICacheService> _cache;
    private readonly StatsService _sut;

    public StatsServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _cache = new Mock<ICacheService>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Steam:SteamId"] = "76561198000000001"
            })
            .Build();

        _sut = new StatsService(_db, _cache.Object, config);
    }

    [Fact]
    public async Task GetLatestStatsAsync_ReturnsNull_WhenNoSnapshots()
    {
        var result = await _sut.GetLatestStatsAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestStatsAsync_ReturnsCachedResult_WhenCacheHit()
    {
        var cached = MakeStatsDto();
        _cache
            .Setup(c => c.GetAsync<StatsDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await _sut.GetLatestStatsAsync();

        result.Should().Be(cached);
    }

    [Fact]
    public async Task GetLatestStatsAsync_ReturnsDeserializedSnapshot_OnCacheMiss()
    {
        var dto = MakeStatsDto(totalGames: 5, totalPlaytimeMinutes: 300);
        await SeedSnapshotAsync(dto);

        var result = await _sut.GetLatestStatsAsync();

        result.Should().NotBeNull();
        result!.TotalGames.Should().Be(5);
        result.TotalPlaytimeMinutes.Should().Be(300);
    }

    [Fact]
    public async Task GetLatestStatsAsync_ReturnsLatestSnapshot_WhenMultipleExist()
    {
        var older = MakeStatsDto(totalGames: 1);
        var newer = MakeStatsDto(totalGames: 10);

        _db.StatsSnapshots.Add(new StatsSnapshot
        {
            Data = JsonSerializer.Serialize(older),
            ComputedAt = DateTime.UtcNow.AddHours(-1)
        });
        _db.StatsSnapshots.Add(new StatsSnapshot
        {
            Data = JsonSerializer.Serialize(newer),
            ComputedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GetLatestStatsAsync();

        result!.TotalGames.Should().Be(10);
    }

    [Fact]
    public async Task GetLatestStatsAsync_PopulatesCache_OnCacheMiss()
    {
        await SeedSnapshotAsync(MakeStatsDto());

        await _sut.GetLatestStatsAsync();

        _cache.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<StatsDto>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecomputeAsync_CreatesSnapshot()
    {
        _db.Games.Add(MakeGame(1, "Half-Life", playtimeMinutes: 120));
        await _db.SaveChangesAsync();

        await _sut.RecomputeAsync();

        var snapshot = await _db.StatsSnapshots.FirstOrDefaultAsync();
        snapshot.Should().NotBeNull();
    }

    [Fact]
    public async Task RecomputeAsync_CalculatesTotalGamesAndPlaytime()
    {
        _db.Games.AddRange(
            MakeGame(1, "Half-Life", playtimeMinutes: 120),
            MakeGame(2, "Portal", playtimeMinutes: 240));
        await _db.SaveChangesAsync();

        await _sut.RecomputeAsync();

        var snapshot = await _db.StatsSnapshots.FirstAsync();
        var dto = JsonSerializer.Deserialize<StatsDto>(snapshot.Data)!;
        dto.TotalGames.Should().Be(2);
        dto.TotalPlaytimeMinutes.Should().Be(360);
    }

    [Fact]
    public async Task RecomputeAsync_CalculatesPlaytimeByGenre()
    {
        _db.Games.AddRange(
            MakeGame(1, "Half-Life", genres: ["Action"], playtimeMinutes: 120),
            MakeGame(2, "Stardew Valley", genres: ["RPG"], playtimeMinutes: 240),
            MakeGame(3, "Celeste", genres: ["Action", "Indie"], playtimeMinutes: 60));
        await _db.SaveChangesAsync();

        await _sut.RecomputeAsync();

        var snapshot = await _db.StatsSnapshots.FirstAsync();
        var dto = JsonSerializer.Deserialize<StatsDto>(snapshot.Data)!;
        dto.PlaytimeByGenre["Action"].Should().Be(180); // Half-Life + Celeste
        dto.PlaytimeByGenre["RPG"].Should().Be(240);
        dto.PlaytimeByGenre["Indie"].Should().Be(60);
    }

    [Fact]
    public async Task RecomputeAsync_InvalidatesCache()
    {
        await _sut.RecomputeAsync();

        _cache.Verify(c => c.InvalidateAsync(It.IsAny<string[]>()), Times.Once);
    }

    [Fact]
    public async Task RecomputeAsync_HandlesEmptyLibrary()
    {
        await _sut.RecomputeAsync();

        var snapshot = await _db.StatsSnapshots.FirstOrDefaultAsync();
        snapshot.Should().NotBeNull();
        var dto = JsonSerializer.Deserialize<StatsDto>(snapshot!.Data)!;
        dto.TotalGames.Should().Be(0);
        dto.TotalPlaytimeMinutes.Should().Be(0);
        dto.PlaytimeByGenre.Should().BeEmpty();
        dto.TopGames.Should().BeEmpty();
    }

    private async Task SeedSnapshotAsync(StatsDto dto)
    {
        _db.StatsSnapshots.Add(new StatsSnapshot
        {
            Data = JsonSerializer.Serialize(dto),
            ComputedAt = dto.ComputedAt
        });
        await _db.SaveChangesAsync();
    }

    private static StatsDto MakeStatsDto(int totalGames = 1, int totalPlaytimeMinutes = 60) =>
        new(
            TotalGames: totalGames,
            TotalPlaytimeMinutes: totalPlaytimeMinutes,
            NeverPlayedCount: 0,
            AveragePlaytimeMinutes: totalGames > 0 ? totalPlaytimeMinutes / totalGames : 0,
            RecentlyPlayedCount: 0,
            PlaytimeByGenre: new Dictionary<string, int>(),
            TopGames: [],
            ComputedAt: DateTime.UtcNow,
            LastSyncedAt: DateTime.UtcNow
        );

    private static Game MakeGame(int appId, string name, string[]? genres = null, int playtimeMinutes = 0) =>
        new()
        {
            AppId = appId,
            Name = name,
            Genres = genres ?? [],
            PlaytimeForever = playtimeMinutes,
            FirstSyncedAt = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow
        };

    public void Dispose() => _db.Dispose();
}
