using System.ComponentModel.DataAnnotations;

namespace SteamStorefront.Models;
public class StatsSnapshot
{
    [Key]
    public int Id {get;set;}
    public string Data {get;set;} = "{}";
    public DateTime ComputedAt {get;set;}
}