using System;
using System.Collections.Generic;

namespace MapManager.GUI.Models;

public class GlobalScoreCacheEntry
{
    public long Id { get; set; }
    public long? BestId { get; set; }
    public long UserId { get; set; }
    public double Accuracy { get; set; }
    public List<string> Mods { get; set; } = [];
    public long TotalScore { get; set; }
    public int MaxCombo { get; set; }
    public GlobalScoreStatsCacheEntry Statistics { get; set; } = new();
    public double? PerformancePoints { get; set; }
    public string Rank { get; set; } = "";
    public long? GlobalRank { get; set; }
    public GlobalUserCacheEntry User { get; set; } = new();
}

public class GlobalScoreStatsCacheEntry
{
    public int Count50 { get; set; }
    public int Count100 { get; set; }
    public int Count300 { get; set; }
    public int CountGeki { get; set; }
    public int CountKatu { get; set; }
    public int CountMiss { get; set; }
}

public class GlobalUserCacheEntry
{
    public string Username { get; set; } = "";
    public long Id { get; set; }
    public string CountryCode { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public bool IsActive { get; set; }
    public bool IsBot { get; set; }
    public bool IsOnline { get; set; }
    public bool IsSupporter { get; set; }
    public DateTimeOffset? LastVisit { get; set; }
    public bool PmFriendsOnly { get; set; }
    public string? ProfileColour { get; set; }
    public string? DefaultGroup { get; set; }
}
