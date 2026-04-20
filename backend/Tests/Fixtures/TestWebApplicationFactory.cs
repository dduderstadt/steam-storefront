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
/// Replaces production dependencies (PostgreSQL, Redis, Steam API, background sync)
/// with in-memory or mocked equivalents so integration tests run without
/// external infrastructure.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Overrides the web host configuration before the test server starts.
    /// Each real dependency is removed from the DI container and replaced
    /// with a test double.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide required config values so services that read from IConfiguration don't throw.
        builder.UseSetting("Steam:ApiKey", "test-key");
        builder.UseSetting("Steam:SteamId", "76561198000000001");
        builder.UseSetting("Redis:ConnectionString", "localhost");

        builder.ConfigureServices(services =>
        {
            // Replace PostgreSQL with an in-memory database.
            // In-memory doesn't require a running Postgres instance and resets between test runs.
            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbDescriptor is not null) { services.Remove(dbDescriptor); }

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));

            // Replace the real Redis connection with a mock.
            // Tests don't need caching behavior — a mock that does nothing is sufficient.
            var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
            if (redisDescriptor is not null) { services.Remove(redisDescriptor); }

            var mockMultiplexer = new Mock<IConnectionMultiplexer>();
            mockMultiplexer
                .Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(new Mock<IDatabase>().Object);
            services.AddSingleton<IConnectionMultiplexer>(mockMultiplexer.Object);

            // Replace ICacheService with a mock so cache hits/misses don't affect test results.
            var cacheDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICacheService));
            if (cacheDescriptor is not null) { services.Remove(cacheDescriptor); }
            services.AddSingleton(new Mock<ICacheService>().Object);

            // Replace the Steam API client with a mock.
            // Tests should never make real HTTP calls to Steam.
            var steamDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ISteamApiClient));
            if (steamDescriptor is not null) { services.Remove(steamDescriptor); }
            services.AddSingleton(new Mock<ISteamApiClient>().Object);

            // Remove the background sync job so it doesn't fire during tests
            // and interfere with the test database state.
            var syncJobDescriptor = services.SingleOrDefault(d => d.ImplementationType == typeof(LibrarySyncJob));
            if (syncJobDescriptor is not null) { services.Remove(syncJobDescriptor); }
        });
    }
}