using Microsoft.Extensions.Logging;
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
    private readonly ILogger<AuxiliaryService> _logger;

    public AuxiliaryService(SettingsService settingsService, ILogger<AuxiliaryService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public void OpenBeatmapInOsu(int beatmapId)
    {
        _logger.LogInformation("Opening beatmap {BeatmapId} in osu!", beatmapId);
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = $"{_settingsService.OsuDirPath}\\osu!.exe",
                Arguments = $"\"osu://b/{beatmapId}\"",
                UseShellExecute = false
            };
            Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open beatmap {BeatmapId} in osu!", beatmapId);
        }
    }
}
