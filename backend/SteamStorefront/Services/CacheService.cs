using System.Text.Json;
using StackExchange.Redis;

namespace SteamStorefront.Services;

public class CacheService(IConnectionMultiplexer redis) : ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<T>((string)value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, ttl);
    }

    public async Task InvalidateAsync(params string[] keys)
    {
        if (keys.Length == 0) return;
        await _db.KeyDeleteAsync(keys.Select(k => (RedisKey)k).ToArray());
    }
}
