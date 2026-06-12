using Testcontainers.PostgreSql;

namespace SteamStorefront.Tests.Fixtures;

/// <summary>
/// Shared fixture for all integration tests. Starts one Postgres container and one
/// web application factory for the entire collection — startup overhead is paid once
/// rather than once per test class.
///
/// xUnit runs all classes in the same [Collection] serially and shares this fixture
/// instance, so the container and server are reused across all integration test classes.
/// </summary>
public class IntegrationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .Build();

    public TestWebApplicationFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        Factory = new TestWebApplicationFactory(_postgres.GetConnectionString());
        // Accessing Factory.Server triggers ASP.NET Core startup, which runs EF Core
        // migrations via Program.cs. Migrations must complete before any test's
        // InitializeAsync tries to clear tables.
        _ = Factory.Server;
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}

/// <summary>
/// Defines the xUnit collection that shares <see cref="IntegrationFixture"/> across
/// all integration test classes. All classes marked with [Collection("Integration")]
/// run serially and receive the same container and factory instance.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<IntegrationFixture> { }
