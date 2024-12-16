using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapManager;
public class AppSettings
{
    public string OsuClientSecret { get; set; } = "XnR2bWHI4f3qEeCN0jsAdMA54jixORj40s7x43Rd";
    public int OsuClientId { get; set; } = 36783;
    public string OsuDirectory { get; set; } = @"D:/osu!";
    public int RefreshInterval { get; set; }
}

