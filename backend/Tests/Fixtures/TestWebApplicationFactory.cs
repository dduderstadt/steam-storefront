using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StackExchange.Redis;
using SteamStorefront.Data;
using SteamStorefront.Jobs;
using SteamStorefront.Services;
using SteamStorefront.Steam;

namespace SteamStorefront.Tests.Fixtures;

/// <summary>
/// Configures a test version of the full ASP.NET Core application pipeline.
/// Replaces production dependencies (Redis, Steam API, background sync) with mocked
/// equivalents so tests run without external infrastructure.
///
/// Pass a Postgres connection string (from Testcontainers) to use a real database;
/// omit it to get an isolated in-memory database for lightweight unit-style tests.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string? _connectionString;

    /// <summary>
    /// Exposed so tests can configure return values for specific scenarios
    /// (e.g. sync tests that need GetOwnedGamesAsync to return a known game list).
    /// </summary>
    public Mock<ISteamApiClient> MockSteamApi { get; } = new();

    /// <param name="connectionString">
    /// Postgres connection string. When provided, the factory uses real Npgsql so Postgres-specific
    /// queries (array operations, ExecuteUpdateAsync) work correctly. When null, falls back to an
    /// isolated EF Core InMemory database for tests that don't need Postgres semantics.
    /// </param>
    public TestWebApplicationFactory(string? connectionString = null)
    {
        _connectionString = connectionString;
        MockSteamApi
            .Setup(s => s.GetOwnedGamesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Steam:ApiKey", "test-key");
        builder.UseSetting("Steam:SteamId", "76561198000000001");
        builder.UseSetting("Redis:ConnectionString", "localhost");

        builder.ConfigureServices(services =>
        {
            ReplaceDatabase(services);

            // Replace Redis with a mock — tests don't exercise caching behavior.
            var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
            if (redisDescriptor is not null) { services.Remove(redisDescriptor); }

            var mockMultiplexer = new Mock<IConnectionMultiplexer>();
            mockMultiplexer
                .Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(new Mock<IDatabase>().Object);
            services.AddSingleton<IConnectionMultiplexer>(mockMultiplexer.Object);

            // Replace ICacheService so cache hits/misses don't affect controller test results.
            var cacheDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICacheService));
            if (cacheDescriptor is not null) { services.Remove(cacheDescriptor); }
            services.AddSingleton(new Mock<ICacheService>().Object);

            // Replace the Steam API client with the shared mock so tests can configure it.
            var steamDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ISteamApiClient));
            if (steamDescriptor is not null) { services.Remove(steamDescriptor); }
            services.AddSingleton(MockSteamApi.Object);

            // Remove the background sync job so it doesn't fire and perturb database state.
            var syncJobDescriptor = services.SingleOrDefault(d => d.ImplementationType == typeof(LibrarySyncJob));
            if (syncJobDescriptor is not null) { services.Remove(syncJobDescriptor); }
        });
    }

    private void ReplaceDatabase(IServiceCollection services)
    {
        var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
        if (dbDescriptor is not null) { services.Remove(dbDescriptor); }

        if (_connectionString is not null)
        {
            // Real Postgres — Postgres-specific query features (array ANY, ExecuteUpdateAsync,
            // SelectMany/unnest) work correctly. EF Core migrations run on first server start.
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(_connectionString));
        }
        else
        {
            // InMemory — register pre-built options directly as a singleton rather than going
            // through AddDbContext to prevent EF Core from registering InMemory provider services
            // alongside the already-registered Npgsql services in the outer DI container.
            services.AddSingleton(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        }
    }
}
