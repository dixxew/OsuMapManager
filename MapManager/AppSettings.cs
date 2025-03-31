using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapManager;
public class AppSettings
{
    public string OsuClientSecret { get; set; } = "";
    public long OsuClientId { get; set; } = 0;
    public string OsuDirectory { get; set; } = @"D:/osu!";
    public int RefreshInterval { get; set; }
}

