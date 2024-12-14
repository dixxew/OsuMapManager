using MapManager.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapManager;

public static class AppStore
{
    public static string OsuDirectory { get; set; } = "D:\\osu!";
    public static MainWindowViewModel MainWindowVM { get; set; } = new();
    public static AudioPlayerViewModel AudioPlayerVM { get; set; } = new();
}
