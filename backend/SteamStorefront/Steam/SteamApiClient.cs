using System.Text.Json.Nodes;

namespace SteamStorefront.Steam;

public class SteamApiClient(HttpClient http, IConfiguration config, ILogger<SteamApiClient> logger) : ISteamApiClient
{
    private const string BaseUrl = "https://api.steampowered.com";
    private const string StoreUrl = "https://store.steampowered.com";
    private static readonly TimeSpan RateLimit = TimeSpan.FromMilliseconds(1500);

    private readonly string _apiKey = config["Steam:ApiKey"]
        ?? throw new InvalidOperationException("Steam:ApiKey is not configured.");

    public async Task<IReadOnlyList<OwnedGame>> GetOwnedGamesAsync(string steamId, CancellationToken ct = default)
    {
        var url = $"{BaseUrl}/IPlayerService/GetOwnedGames/v1/?key={_apiKey}&steamid={steamId}&include_appinfo=1&format=json";
        var response = await http.GetStringAsync(url, ct);

        var games = JsonNode.Parse(response)?["response"]?["games"]?.AsArray() ?? [];

        return games.Select(g => new OwnedGame(
            g!["appid"]!.GetValue<int>(),
            g["name"]?.GetValue<string>() ?? string.Empty,
            g["playtime_forever"]?.GetValue<int>() ?? 0,
            g["playtime_2weeks"]?.GetValue<int>() ?? 0,
            g["rtime_last_played"]?.GetValue<long>()
        )).ToList();
    }

    public async Task<GameDetails?> GetGameDetailsAsync(int appId, CancellationToken ct = default)
    {
        // Steam's appdetails endpoint has no bulk API — rate limit individual calls
        await Task.Delay(RateLimit, ct);

        try
        {
            var url = $"{StoreUrl}/api/appdetails?appids={appId}&filters=basic,genres";
            var response = await http.GetStringAsync(url, ct);
            var root = JsonNode.Parse(response)?[appId.ToString()];

            if (root?["success"]?.GetValue<bool>() != true) return null;

            var data = root["data"]!;
            var genres = data["genres"]?.AsArray()
                .Select(g => g!["description"]!.GetValue<string>())
                .ToArray() ?? [];

            return new GameDetails(
                appId,
                data["name"]!.GetValue<string>(),
                data["short_description"]?.GetValue<string>(),
                data["header_image"]?.GetValue<string>(),
                genres
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch details for appId {AppId}", appId);
            return null;
        }
    }
}
