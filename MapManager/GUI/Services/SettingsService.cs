using MapManager;
using System.Diagnostics;
using System;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;

public class SettingsService
{
    private readonly AppSettings _appSettings;

    public SettingsService(AppSettings appSettings)
    {
        _appSettings = appSettings;
    }

    public string? OsuClientSecret => _appSettings.OsuClientSecret;
    public string? OsuClientId => _appSettings.OsuClientId.ToString();
    public string? OsuDirPath => _appSettings.OsuDirectory;

    public void UpdateSettings(string? clientId, string? clientSecret, string? dirPath)
    {
        _appSettings.OsuClientId = int.TryParse(clientId, out int value) ? value : 0;
        _appSettings.OsuClientSecret = clientSecret;
        _appSettings.OsuDirectory = dirPath;
        SaveAsync();
    }

    public void GoGetOsuApiKey()
    {
        try
        {
            ProcessStartInfo psi = new()
            {
                FileName = "https://osu.ppy.sh/p/api",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не удалось открыть URL: {ex.Message}");
        }
    }

    private async Task SaveAsync()
    {
        await AppSettingsManager.SaveSettingsAsync(_appSettings);
    }
}
