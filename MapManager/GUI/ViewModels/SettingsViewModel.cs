using ReactiveUI;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using MapManager.GUI.Services;

namespace MapManager.GUI.ViewModels;

public class SettingsViewModel : ReactiveObject
{
    private readonly SettingsService _settingsService;
    private bool _isInitialized;

    private string? _osuClientSecret;
    private string? _osuClientId;
    private string? _osuDirPath;

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;

        OsuClientSecret = _settingsService.OsuClientSecret;
        OsuClientId = _settingsService.OsuClientId;
        OsuDirPath = _settingsService.OsuDirPath;

        _isInitialized = true;
    }

    public string? OsuClientSecret
    {
        get => _osuClientSecret;
        set
        {
            this.RaiseAndSetIfChanged(ref _osuClientSecret, value);
            NotifySettingsChanged();
        }
    }

    public string? OsuClientId
    {
        get => _osuClientId;
        set
        {
            this.RaiseAndSetIfChanged(ref _osuClientId, value);
            NotifySettingsChanged();
        }
    }

    public string? OsuDirPath
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
        _settingsService.GoGetOsuApiKey();
    }

    public void InitSettings()
    {
        NotifySettingsChanged();
    }

    private void NotifySettingsChanged()
    {
        if (!_isInitialized) return;

        _settingsService.UpdateSettings(OsuClientId, OsuClientSecret, OsuDirPath);
    }
}
