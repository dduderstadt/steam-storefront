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

    public LibraryServiceTests()
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

        _sut = new LibraryService(_db, _cache.Object, config);
    }

    [Fact]
    public async Task GetGamesAsync_ReturnsCachedResult_WhenCacheHit()
    {
        var cached = new PagedResult<GameDto>([], 0, 1, 24);
        _cache
            .Setup(c => c.GetAsync<PagedResult<GameDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await _sut.GetGamesAsync(new LibraryQueryParams());

        result.Should().Be(cached);
        _cache.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<PagedResult<GameDto>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetGamesAsync_ReturnsAllGames_WhenNoFilters()
    {
        _db.Games.AddRange(MakeGame(1, "Half-Life"), MakeGame(2, "Portal"));
        await _db.SaveChangesAsync();

        var result = await _sut.GetGamesAsync(new LibraryQueryParams());

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

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

    [Fact]
    public async Task GetGamesAsync_SortsByPlaytimeDescending_WhenSortIsPlaytime()
    {
        _db.Games.AddRange(
            MakeGame(1, "Half-Life", playtimeMinutes: 120),
            MakeGame(2, "Portal", playtimeMinutes: 600),
            MakeGame(3, "Celeste", playtimeMinutes: 300));
        await _db.SaveChangesAsync();

        var result = await _sut.GetGamesAsync(new LibraryQueryParams { Sort = "playtime" });

        result.Items.Select(g => g.Name).Should().Equal("Portal", "Celeste", "Half-Life");
    }

    [Fact]
    public async Task GetGamesAsync_SortsByNameAlphabetically_ByDefault()
    {
        _db.Games.AddRange(
            MakeGame(1, "Portal"),
            MakeGame(2, "Celeste"),
            MakeGame(3, "Half-Life"));
        await _db.SaveChangesAsync();

        var result = await _sut.GetGamesAsync(new LibraryQueryParams());

        result.Items.Select(g => g.Name).Should().Equal("Celeste", "Half-Life", "Portal");
    }

    [Fact]
    public async Task GetGamesAsync_PaginatesResults()
    {
        _db.Games.AddRange(
            MakeGame(1, "Celeste"),
            MakeGame(2, "Half-Life"),
            MakeGame(3, "Portal"));
        await _db.SaveChangesAsync();

        var result = await _sut.GetGamesAsync(new LibraryQueryParams { Page = 2, PageSize = 2 });

        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Portal");
    }

    [Fact]
    public async Task GetGamesAsync_WritesCacheOnMiss()
    {
        _db.Games.Add(MakeGame(1, "Half-Life"));
        await _db.SaveChangesAsync();

        await _sut.GetGamesAsync(new LibraryQueryParams());

        _cache.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<PagedResult<GameDto>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetGameAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _sut.GetGameAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetGameAsync_ReturnsGame_WhenFound()
    {
        _db.Games.Add(MakeGame(1, "Half-Life"));
        await _db.SaveChangesAsync();

        var result = await _sut.GetGameAsync(1);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Half-Life");
    }

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
