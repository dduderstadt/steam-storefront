namespace SteamStorefront.Services;

/// <summary>
/// Abstraction over Redis so the rest of the codebase never takes a direct dependency on StackExchange.Redis.
/// Mocked in tests to eliminate cache behavior from unit test results.
/// </summary>
public interface ICacheService
{
    /// <summary>Returns the cached value for <paramref name="key"/>, or null on a cache miss.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;

    /// <summary>Stores <paramref name="value"/> under <paramref name="key"/> with the given TTL.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>Removes one or more keys from the cache. Called by SyncService after each sync completes.</summary>
    Task InvalidateAsync(params string[] keys);
}
