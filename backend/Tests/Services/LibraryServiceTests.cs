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
/// Unit tests for <see cref="LibraryService"/>.
/// Each test uses an isolated in-memory database and a mocked cache so results
/// are deterministic and independent of external infrastructure.
/// </summary>
public class LibraryServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<ICacheService> _cache;
    private readonly LibraryService _sut;

    /// <summary>
    /// Sets up a fresh in-memory database, mock cache, and LibraryService instance
    /// for each test. Using <see cref="Guid.NewGuid"/> as the database name ensures
    /// no state leaks between tests.
    /// </summary>
    public LibraryServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _cache = new Mock<ICacheService>();

        // Build a minimal IConfiguration with just the Steam ID the service requires.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Steam:SteamId"] = "76561198000000001"
            })
            .Build();

        _sut = new LibraryService(_db, _cache.Object, config);
    }

    /// <summary>
    /// When the cache contains a result for the given query, the service should
    /// return it immediately without hitting the database or writing back to cache.
    /// </summary>
    [Fact]
    public async Task GetGamesAsync_ReturnsCachedResult_WhenCacheHit()
    {
        var cached = new PagedResult<GameDto>([], 0, 1, 24);
        _cache
            .Setup(c => c.GetAsync<PagedResult<GameDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await _sut.GetGamesAsync(new LibraryQueryParams());

        result.Should().Be(cached);

        // Verify the service did not write back to cache — it already had a valid result.
        _cache.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<PagedResult<GameDto>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// With no filters applied, all games in the database should be returned.
    /// </summary>
    [Fact]
    public async Task GetGamesAsync_ReturnsAllGames_WhenNoFilters()
    {
        _db.Games.AddRange(MakeGame(1, "Half-Life"), MakeGame(2, "Portal"));
        await _db.SaveChangesAsync();

        var result = await _sut.GetGamesAsync(new LibraryQueryParams());

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    /// <summary>
    /// When a genre filter is provided, only games whose Genres array contains
    /// that genre should be returned.
    /// </summary>
    [Fact]
    public async Task GetGamesAsync_FiltersByGenre()
    {
        _db.Games.AddRange(
            MakeGame(1, "Half-Life", genres: ["Action"]),
            MakeGame(2, "Stardew Valley", genres: ["RPG"]));
        await _db.SaveChangesAsync();

        var result = await _sut.GetGamesAsync(new LibraryQueryParams { Genre = "RPG" });

        result.TotalCount.Should().Be(1);
        result.Items[0].Name.Should().Be("Stardew Valley");
    }

    /// <summary>
    /// MinPlaytime is provided in hours by the caller and converted to minutes
    /// internally. Only games meeting the threshold should be returned.
    /// </summary>
    [Fact]
    public async Task GetGamesAsync_FiltersByMinPlaytime()
    {
        _db.Games.AddRange(
            MakeGame(1, "Half-Life", playtimeMinutes: 120),
            MakeGame(2, "Portal", playtimeMinutes: 600));
        await _db.SaveChangesAsync();

        // MinPlaytime = 5 hours = 300 minutes — only Portal (600m) should match.
        var result = await _sut.GetGamesAsync(new LibraryQueryParams { MinPlaytime = 5 });

        result.TotalCount.Should().Be(1);
        result.Items[0].Name.Should().Be("Portal");
    }

    /// <summary>
    /// Querying a game that does not exist should return null rather than throwing.
    /// </summary>
    [Fact]
    public async Task GetGameAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _sut.GetGameAsync(999);

        result.Should().BeNull();
    }

    /// <summary>
    /// Querying a game that exists should return a populated <see cref="GameDto"/>.
    /// </summary>
    [Fact]
    public async Task GetGameAsync_ReturnsGame_WhenFound()
    {
        _db.Games.Add(MakeGame(1, "Half-Life"));
        await _db.SaveChangesAsync();

        var result = await _sut.GetGameAsync(1);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Half-Life");
    }

    /// <summary>
    /// Helper that creates a minimal valid <see cref="Game"/> entity for use in tests.
    /// </summary>
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

    /// <summary>
    /// Disposes the database context after each test to release the in-memory store.
    /// </summary>
    public void Dispose() => _db.Dispose();
}
