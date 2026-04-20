using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using FluentAssertions;
using SteamStorefront.Data;
using SteamStorefront.Models;
using SteamStorefront.Services;
using SteamStorefront.Steam;

namespace SteamStorefront.Tests.Services;

/// <summary>
/// Unit tests for <see cref="SyncService"/>.
/// The Steam API client and stats service are mocked so tests verify sync logic
/// without making real HTTP calls or triggering stats recomputation.
/// </summary>
public class SyncServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<ISteamApiClient> _steamApi;
    private readonly Mock<IStatsService> _stats;
    private readonly SyncService _sut;

    /// <summary>
    /// Sets up a fresh in-memory database and all required mocks before each test.
    /// <see cref="NullLogger"/> is used so log output doesn't clutter test results.
    /// </summary>
    public SyncServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _steamApi = new Mock<ISteamApiClient>();
        _stats = new Mock<IStatsService>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Steam:SteamId"] = "76561198000000001"
            })
            .Build();

        _sut = new SyncService(_db, _steamApi.Object, _stats.Object, config, NullLogger<SyncService>.Instance);
    }

    /// <summary>
    /// When Steam returns a game that doesn't exist in the database, the sync
    /// should fetch its details and insert it as a new record.
    /// </summary>
    [Fact]
    public async Task SyncAsync_InsertsNewGames()
    {
        _steamApi
            .Setup(s => s.GetOwnedGamesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new OwnedGame(1, "Half-Life", 120, 0, null)]);

        _steamApi
            .Setup(s => s.GetGameDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GameDetails(1, "Half-Life", "A shooter.", "http://img.com/1.jpg", ["Action"]));

        await _sut.SyncAsync();

        var games = await _db.Games.ToListAsync();
        games.Should().HaveCount(1);
        games[0].Name.Should().Be("Half-Life");
        games[0].Genres.Should().Contain("Action");
    }

    /// <summary>
    /// When Steam returns a game that already exists in the database, the sync
    /// should not fetch its details from the store API.
    /// Fetching details is expensive (rate-limited) and only needed for new games.
    /// Skipped: ExecuteUpdateAsync is a relational-only EF Core feature not supported
    /// by the InMemory provider. This behavior is covered by integration tests against
    /// a real database.
    /// </summary>
    [Fact(Skip = "ExecuteUpdateAsync is not supported by the InMemory provider")]
    public async Task SyncAsync_DoesNotFetchDetails_ForExistingGame()
    {
        _db.Games.Add(new Game
        {
            AppId = 1,
            Name = "Half-Life",
            Genres = ["Action"],
            PlaytimeForever = 60,
            FirstSyncedAt = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _steamApi
            .Setup(s => s.GetOwnedGamesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new OwnedGame(1, "Half-Life", 180, 30, null)]);

        await _sut.SyncAsync();

        _steamApi.Verify(s => s.GetGameDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// After syncing game data, the service must trigger stats recomputation
    /// so the stats snapshot reflects the latest library state.
    /// </summary>
    [Fact]
    public async Task SyncAsync_CallsRecomputeAsync_AfterSync()
    {
        _steamApi
            .Setup(s => s.GetOwnedGamesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _sut.SyncAsync();

        _stats.Verify(s => s.RecomputeAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Disposes the database context after each test to release the in-memory store.
    /// </summary>
    public void Dispose() => _db.Dispose();
}
