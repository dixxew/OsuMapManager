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

        OsuClientId = _appSettings.OsuClientId.ToString();
        OsuClientSecret = _appSettings.OsuClientSecret;
        OsuDirPath = _appSettings.OsuDirectory;
        IrcServer = _appSettings.IrcServer;
        IrcPort = _appSettings.IrcPort;
        IrcNickname = _appSettings.IrcNickname;
        IrcPassword = _appSettings.IrcPassword;
    }


    public string AppVersion => typeof(App).Assembly.GetName().Version.ToString();
    public string? OsuClientSecret
    {
        get => _appSettings.OsuClientSecret;
        set
        {
            _appSettings.OsuClientSecret = value;
            OsuApiSettingsChanged();
        }
    }
    public string? OsuClientId
    {
        get => _appSettings.OsuClientId.ToString();
        set
        {
            _appSettings.OsuClientId = long.TryParse(value, out var parsed) ? parsed : _appSettings.OsuClientId;
            OsuApiSettingsChanged();
        }
    }
    public string? OsuDirPath
    {
        get => _appSettings.OsuDirectory;
        set
        {
            _appSettings.OsuDirectory = value;
        }
    }
    public string? IrcServer
    {
        get => _appSettings.IrcServer;
        set
        {
            _appSettings.IrcServer = value;
            IrcSettingsChanged();
        }
    }
    public int? IrcPort
    {
        get => _appSettings.IrcPort;
        set
        {
            _appSettings.IrcPort = value;
            IrcSettingsChanged();
        }
    }
    public string? IrcNickname
    {
        get => _appSettings.IrcNickname;
        set
        {
            _appSettings.IrcNickname = value;
            IrcSettingsChanged();
        }
    }
    public string? IrcPassword
    {
        get => _appSettings.IrcPassword;
        set
        {
            _appSettings.IrcPassword = value; 
            IrcSettingsChanged();
        }
    }

    public async Task UpdateSettings(string propName, object? value)
    {
        var property = typeof(SettingsService).GetProperty(propName);
        if (property is null || !property.CanWrite) return;

        var convertedValue = Convert.ChangeType(value, property.PropertyType);
        property.SetValue(this, convertedValue);

        await SaveAsync();
    }

    public void OpenWebPageOsuApiKey()
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

    public Action OnOsuApiSettingsChanged;
    private void OsuApiSettingsChanged()
    {
        OnOsuApiSettingsChanged?.Invoke();
    }


    public Action OnIrcSettingsChanged;
    private void IrcSettingsChanged()
    {
        OnIrcSettingsChanged?.Invoke();
    }
}
