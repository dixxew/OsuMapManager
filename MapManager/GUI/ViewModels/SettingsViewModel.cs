using ReactiveUI;
using System.Diagnostics;
using System;

namespace MapManager.GUI.ViewModels;

public class SettingsViewModel : ReactiveObject
{
    private string _osuApiKey;
    private string _osuDirPath = "D:\\osu!";


    public string OsuApiKey
    {
        get => _osuApiKey;
        set => this.RaiseAndSetIfChanged(ref _osuApiKey, value);
    }

    public string OsuDirPath
    {
        get => _osuDirPath;
        set => this.RaiseAndSetIfChanged(ref _osuDirPath, value);
    }

    public void GoGetOsuApiKey()
    {
        try
        {
            ProcessStartInfo psi = new()
            {
                FileName = "https://osu.ppy.sh/p/api",
                UseShellExecute = true // Это важно для открытия URL через системный браузер
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не удалось открыть URL: {ex.Message}");
        }
    }

}
