using SteamStorefront.Models.Dtos;

namespace SteamStorefront.Services;

public interface IStatsService
{
    Task<StatsDto?> GetLatestStatsAsync(CancellationToken ct = default);
    Task RecomputeAsync(CancellationToken ct = default);
}
