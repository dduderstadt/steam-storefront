using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SteamStorefront.Data;
using SteamStorefront.Steam;
using SteamStorefront.Tests.Fixtures;

namespace SteamStorefront.Tests.Controllers;

/// <summary>
/// Integration tests for <see cref="SteamStorefront.Controllers.SyncController"/>.
/// </summary>
[Collection("Integration")]
public class SyncControllerTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SyncControllerTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Reset mock so each test starts with the default empty-library behaviour.
        _fixture.Factory.MockSteamApi.Reset();
        _fixture.Factory.MockSteamApi
            .Setup(s => s.GetOwnedGamesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Games.ExecuteDeleteAsync();
        await db.StatsSnapshots.ExecuteDeleteAsync();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task TriggerSync_ReturnsOk_WithSyncedAt()
    {
        var response = await _client.PostAsync("/api/v1/sync", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.TryGetProperty("syncedAt", out var syncedAt).Should().BeTrue();
        syncedAt.GetDateTime().Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task TriggerSync_InsertsNewGames_FromSteam()
    {
        _fixture.Factory.MockSteamApi
            .Setup(s => s.GetOwnedGamesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new OwnedGame(570, "Dota 2", PlaytimeForever: 1000, PlaytimeTwoWeeks: 60, RtimeLastPlayed: null)]);

        _fixture.Factory.MockSteamApi
            .Setup(s => s.GetGameDetailsAsync(570, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GameDetails(570, "Dota 2", "A MOBA.", HeaderImage: null, Genres: ["Strategy"]));

        await _client.PostAsync("/api/v1/sync", null);

        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var game = await db.Games.FindAsync(570);
        game.Should().NotBeNull();
        game!.Name.Should().Be("Dota 2");
        game.Genres.Should().Contain("Strategy");
    }
}
