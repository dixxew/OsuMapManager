using ReactiveUI;
using System.Diagnostics;
using System;
using DynamicData;
using OsuSharp;
using System.Threading.Tasks;

namespace MapManager.GUI.ViewModels;

public class SettingsViewModel : ReactiveObject
{
    private bool _isInitialized = false;

    private string _osuClientSecret;
    private int _osuClientId;
    private string _osuDirPath = "D:\\osu!";
    private readonly AppSettings _appSettings;

    public SettingsViewModel(AppSettings appSettings)
    {
        _appSettings = appSettings;
        OsuClientSecret = _appSettings.OsuClientSecret;
        OsuClientId = _appSettings.OsuClientId;
        OsuDirPath = _appSettings.OsuDirectory;

        _isInitialized = true;
    }

    public string OsuClientSecret
    {
        get => _osuClientSecret;
        set
        {
            this.RaiseAndSetIfChanged(ref _osuClientSecret, value);
            NotifySettingsChanged();
        }
    }
    public int OsuClientId
    {
        get => _osuClientId;
        set
        {
            this.RaiseAndSetIfChanged(ref _osuClientId, value);
            NotifySettingsChanged();
        }
    }
    public string OsuDirPath
    {
        get => _osuDirPath;
        set
        {
            this.RaiseAndSetIfChanged(ref _osuDirPath, value);
            NotifySettingsChanged();
        }
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

    public void InitSettings()
    {
        NotifySettingsChanged();
    }

    public event Action OnSettingsChanged;

    private void NotifySettingsChanged()
    {
        if (!_isInitialized) return;

        _appSettings.OsuClientId = OsuClientId;
        _appSettings.OsuClientSecret = OsuClientSecret;
        _appSettings.OsuDirectory = OsuDirPath;
        OnSettingsChanged?.Invoke();
        SaveAsync();
    }
    public async Task SaveAsync()
    {
        await AppSettingsManager.SaveSettingsAsync(_appSettings);
    }
}
