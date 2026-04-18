using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SteamStorefront.Models;

public class PlaytimeRecord
{
    [Key]
    public long Id {get;set;}
    public int AppId {get;set;}
    public int PlaytimeMinutes {get;set;}
    public DateTime RecordedAt {get;set;}
}