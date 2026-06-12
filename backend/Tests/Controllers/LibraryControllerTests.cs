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
/// Integration tests for <see cref="SteamStorefront.Controllers.LibraryController"/>.
/// All tests run against a real Postgres container (via <see cref="IntegrationFixture"/>)
/// so Postgres-specific query features — array ANY for genre filtering, unnest for genre
/// aggregation — work correctly.
/// </summary>
[Collection("Integration")]
public class LibraryControllerTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public LibraryControllerTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Games.ExecuteDeleteAsync();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetLibrary_ReturnsOk_WithEmptyLibrary()
    {
        var response = await _client.GetAsync("/api/v1/library");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await DeserializeAsync<PagedResult<GameDto>>(response);
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLibrary_ReturnsAllGames_WhenLibraryPopulated()
    {
        await SeedAsync(MakeGame(1, "Half-Life"), MakeGame(2, "Portal"));

        var response = await _client.GetAsync("/api/v1/library");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await DeserializeAsync<PagedResult<GameDto>>(response);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetLibrary_ReturnsFilteredResults_ByGenre()
    {
        await SeedAsync(
            MakeGame(1, "Half-Life", genres: ["Action"]),
            MakeGame(2, "Stardew Valley", genres: ["RPG"]));

        var response = await _client.GetAsync("/api/v1/library?genre=RPG");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await DeserializeAsync<PagedResult<GameDto>>(response);
        result.TotalCount.Should().Be(1);
        result.Items[0].Name.Should().Be("Stardew Valley");
    }

    [Fact]
    public async Task GetLibrary_ReturnsFilteredResults_ByMultipleGenres()
    {
        await SeedAsync(
            MakeGame(1, "Half-Life", genres: ["Action"]),
            MakeGame(2, "Stardew Valley", genres: ["RPG"]),
            MakeGame(3, "Celeste", genres: ["Indie"]));

        var response = await _client.GetAsync("/api/v1/library?genre=Action&genre=RPG");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await DeserializeAsync<PagedResult<GameDto>>(response);
        result.TotalCount.Should().Be(2);
        result.Items.Select(g => g.Name).Should().BeEquivalentTo(["Half-Life", "Stardew Valley"]);
    }

    [Fact]
    public async Task GetGame_ReturnsNotFound_WhenGameDoesNotExist()
    {
        var response = await _client.GetAsync("/api/v1/library/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetGame_ReturnsOk_WhenGameExists()
    {
        await SeedAsync(MakeGame(1, "Half-Life"));

        var response = await _client.GetAsync("/api/v1/library/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await DeserializeAsync<GameDto>(response);
        result.Name.Should().Be("Half-Life");
    }

    [Fact]
    public async Task GetGenres_ReturnsDistinctSortedList()
    {
        await SeedAsync(
            MakeGame(1, "Half-Life", genres: ["Action", "Shooter"]),
            MakeGame(2, "Stardew Valley", genres: ["RPG", "Action"]));

        var response = await _client.GetAsync("/api/v1/library/genres");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await DeserializeAsync<List<string>>(response);
        result.Should().Equal("Action", "RPG", "Shooter");
    }

    private async Task SeedAsync(params Game[] games)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Games.AddRange(games);
        await db.SaveChangesAsync();
    }

    private static async Task<T> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(body, JsonOptions)!;
    }

    private static Game MakeGame(int appId, string name, string[]? genres = null) =>
        new()
        {
            AppId = appId,
            Name = name,
            Genres = genres ?? [],
            FirstSyncedAt = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow
        };
}
