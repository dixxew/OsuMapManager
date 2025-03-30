using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;
public class AuxiliaryService
{
    private readonly SettingsService _settingsService;

    public AuxiliaryService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void OpenBeatmapInOsu(int beatmapId)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = $"{_settingsService.OsuDirPath}\\osu!.exe",
            Arguments = $"\"osu://b/{beatmapId}\"",
            UseShellExecute = false
        };
        Process.Start(processStartInfo);
    }
}
