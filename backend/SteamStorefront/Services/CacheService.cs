using System.Text.Json;
using StackExchange.Redis;

namespace SteamStorefront.Services;

/// <summary>
/// Redis-backed implementation of <see cref="ICacheService"/>.
/// All values are serialized to JSON strings before storage and deserialized on retrieval.
/// </summary>
public class CacheService(IConnectionMultiplexer redis) : ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();

    /// <summary>
    /// Fetches the value at <paramref name="key"/> from Redis and deserializes it.
    /// Returns null on a cache miss rather than throwing.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<T>((string)value!);
    }

    /// <summary>Serializes <paramref name="value"/> and stores it in Redis with the given TTL.</summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, ttl);
    }

    /// <summary>
    /// Deletes all provided keys in a single Redis call.
    /// Early-returns if no keys are given to avoid an empty batch command.
    /// </summary>
    public async Task InvalidateAsync(params string[] keys)
    {
        if (keys.Length == 0) return;
        await _db.KeyDeleteAsync(keys.Select(k => (RedisKey)k).ToArray());
    }
}
