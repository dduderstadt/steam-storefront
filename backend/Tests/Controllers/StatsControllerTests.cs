using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SteamStorefront.Data;
using SteamStorefront.Models;
using SteamStorefront.Models.Dtos;
using SteamStorefront.Tests.Fixtures;

namespace SteamStorefront.Tests.Controllers;

/// <summary>
/// Integration tests for <see cref="SteamStorefront.Controllers.StatsController"/>.
/// </summary>
[Collection("Integration")]
public class StatsControllerTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public StatsControllerTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.StatsSnapshots.ExecuteDeleteAsync();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetStats_ReturnsNotFound_WhenNoSnapshot()
    {
        var response = await _client.GetAsync("/api/v1/stats");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetStats_ReturnsOk_WhenSnapshotExists()
    {
        await SeedSnapshotAsync(totalGames: 42, totalPlaytimeMinutes: 1000);

        var response = await _client.GetAsync("/api/v1/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<StatsDto>(body, JsonOptions)!;
        result.TotalGames.Should().Be(42);
        result.TotalPlaytimeMinutes.Should().Be(1000);
    }

    private async Task SeedSnapshotAsync(int totalGames, int totalPlaytimeMinutes)
    {
        var dto = new StatsDto(
            TotalGames: totalGames,
            TotalPlaytimeMinutes: totalPlaytimeMinutes,
            NeverPlayedCount: 0,
            AveragePlaytimeMinutes: 0,
            RecentlyPlayedCount: 0,
            PlaytimeByGenre: new Dictionary<string, int>(),
            TopGames: [],
            ComputedAt: DateTime.UtcNow,
            LastSyncedAt: DateTime.UtcNow
        );

        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.StatsSnapshots.Add(new StatsSnapshot
        {
            Data = JsonSerializer.Serialize(dto),
            ComputedAt = dto.ComputedAt
        });
        await db.SaveChangesAsync();
    }
}
