namespace SteamStorefront.Models.Dtos;

/// <summary>
/// Generic wrapper for any paginated response. The frontend uses TotalCount and PageSize
/// to calculate how many pages exist without the backend sending every record.
/// IReadOnlyList enforces that callers can't mutate the items collection after construction.
/// </summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);