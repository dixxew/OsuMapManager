using MapManager;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;

public class SettingsService
{
    private readonly AppSettings _appSettings;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(AppSettings appSettings, ILogger<SettingsService> logger)
    {
        _appSettings = appSettings;
        _logger = logger;

        OsuClientId = _appSettings.OsuClientId.ToString();
        OsuClientSecret = _appSettings.OsuClientSecret;
        OsuDirPath = _appSettings.OsuDirectory;
        IrcServer = _appSettings.IrcServer;
        IrcPort = _appSettings.IrcPort;
        IrcNickname = _appSettings.IrcNickname;
        IrcPassword = _appSettings.IrcPassword;
    }


    public string AppVersion
    {
        get
        {
            var ver = typeof(App).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "unknown";
            var plus = ver.IndexOf('+');
            return plus >= 0 ? ver[..plus] : ver;
        }
    }
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

    public List<string>? OpenedChannels
    {
        get => _appSettings.OpenedChannels;
        set
        {
            _appSettings.OpenedChannels = value;
            _ = SaveAsync();
        }
    }

    public bool NotificationsEnabled
    {
        get => _appSettings.NotificationsEnabled;
        set { _appSettings.NotificationsEnabled = value; _ = SaveAsync(); }
    }

    public bool NotificationSoundEnabled
    {
        get => _appSettings.NotificationSoundEnabled;
        set { _appSettings.NotificationSoundEnabled = value; _ = SaveAsync(); }
    }

    public List<string> HighlightKeywords
    {
        get => _appSettings.HighlightKeywords ??= [];
        set { _appSettings.HighlightKeywords = value; _ = SaveAsync(); }
    }

    public List<string> MutedUsers
    {
        get => _appSettings.MutedUsers ??= [];
        set { _appSettings.MutedUsers = value; _ = SaveAsync(); }
    }

    public int MaxConcurrentDownloads
    {
        get => _appSettings.MaxConcurrentDownloads;
        set { _appSettings.MaxConcurrentDownloads = value; _ = SaveAsync(); }
    }

    public string PreferredMirror
    {
        get => _appSettings.PreferredMirror;
        set { _appSettings.PreferredMirror = value; _ = SaveAsync(); }
    }

    public async Task UpdateSettings(string propName, object? value)
    {
        var property = typeof(SettingsService).GetProperty(propName);
        if (property is null || !property.CanWrite)
        {
            _logger.LogWarning("UpdateSettings: property '{Prop}' not found or read-only", propName);
            return;
        }

        var convertedValue = Convert.ChangeType(value, property.PropertyType);
        property.SetValue(this, convertedValue);
        _logger.LogInformation("Setting '{Prop}' updated", propName);

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
            _logger.LogWarning(ex, "Failed to open osu! API key page");
        }
    }

    private async Task SaveAsync()
    {
        await AppSettingsManager.SaveSettingsAsync(_appSettings);
    }

    public Action OnOsuApiSettingsChanged;
    private void OsuApiSettingsChanged()
    {
        _logger.LogDebug("osu! API settings changed");
        OnOsuApiSettingsChanged?.Invoke();
    }


    public Action OnIrcSettingsChanged;
    private void IrcSettingsChanged()
    {
        _logger.LogDebug("IRC settings changed");
        OnIrcSettingsChanged?.Invoke();
    }
}
