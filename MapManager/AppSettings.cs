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
}

