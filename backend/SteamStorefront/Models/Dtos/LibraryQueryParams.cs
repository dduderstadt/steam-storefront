namespace SteamStorefront.Models.Dtos;

/// <summary>
/// This is the object that represents the query string parameters from the frontend when it
/// calls `GET /api/v1/library`.
/// 
/// /api/v1/library?genre=Action&minPlaytime=10&sort=playtime&page=2
/// ASP.NET Core automatically maps those query string values onto this class via
/// [FromQuery] in the controller — you don't write any parsing code yourself.
/// </summary>
public class LibraryQueryParams
{
    public string? Genre { get; set; }
    /// <summary>
    /// The frontend deals in hours because that's what users think in. The service layer
    /// converts to minutes when querying the database (query.MinPlaytime.Value * 60),
    /// because that's how Steam stores it. The conversion happens in exactly one place —
    /// LibraryService — so if it ever needs to change, there's only one place to update.
    /// </summary>
    public int? MinPlaytime { get; set; }
    /// <summary>Accepted values: "name", "playtime", "lastPlayed". Defaults to "name".</summary>
    public string Sort { get; set; } = "name";
    /// <summary>1-indexed. Defaults to the first page.</summary>
    public int Page { get; set; } = 1;
    /// <summary>Number of items per page. Defaults to 50; the storefront overrides this to 24.</summary>
    public int PageSize { get; set; } = 50;
}