using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapManager;
public class AppSettings
{
    public string? OsuClientSecret { get; set; } = "";
    public long? OsuClientId { get; set; } = 0;
    public string? OsuDirectory { get; set; } = @"D:/osu!";
    public int? RefreshInterval { get; set; }
    public string? IrcServer { get; set; } = "irc.ppy.sh";
    public int? IrcPort { get; set; } = 6667;
    public string? IrcNickname { get; set; }
    public string? IrcPassword { get; set; }
    public DateTime? CacheInvalidatedAt { get; set; }
    public List<string>? OpenedChannels { get; set; }

    public bool SetupCompleted { get; set; } = false;

    public bool NotificationsEnabled { get; set; } = true;
    public bool NotificationSoundEnabled { get; set; } = true;
    public List<string> HighlightKeywords { get; set; } = [];
    public List<string> MutedUsers { get; set; } = [];

    public int MaxConcurrentDownloads { get; set; } = 2;
    public string PreferredMirror { get; set; } = "catboy.best";
}

