namespace MapManager.GUI.Models;

public class OsuPpsEntry
{
    public int Rank { get; set; }
    public int BeatmapId { get; set; }
    public int BeatmapSetId { get; set; }
    public int Mods { get; set; }
    public string Artist { get; set; } = "";
    public string Title { get; set; } = "";
    public string Version { get; set; } = "";
    public double FarmScore { get; set; }
    public double Pp99 { get; set; }
    public int LengthSeconds { get; set; }
    public double StarRating { get; set; }
    public bool IsLocal { get; set; }
    public BeatmapSet? LocalBeatmapSet { get; set; }
}
