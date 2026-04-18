namespace SteamStorefront.Models.Dtos;

public class LibraryQueryParams
{
    public string? Genre {get;set;}
    public int? MinPlaytime {get;set;}
    public string Sort {get;set;} = "name";
    public int Page {get;set;} = 1;
    public int PageSize {get;set;} = 50;
}