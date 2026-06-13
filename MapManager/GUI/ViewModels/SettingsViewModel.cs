using ReactiveUI;
using System.Diagnostics;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive;
using System.Text.Json;
using System.Threading.Tasks;
using MapManager.GUI.Services;
using System.Collections.Generic;
using Avalonia.Media;
using SukiUI.Models;
using SukiUI;
using System.Linq;

namespace MapManager.GUI.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly CacheService _cacheService;

    private static readonly HttpClient _http = new();

    private const string ReleasesApiUrl =
        "https://api.github.com/repos/dixxew/OsuMapManager/releases/latest";
    private const string ReleasesPageUrl =
        "https://github.com/dixxew/OsuMapManager/releases/latest";

    public ReactiveCommand<Unit, Unit> InvalidateCacheCommand { get; }
    public ReactiveCommand<Unit, Unit> CheckUpdateCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenReleasePageCommand { get; }

    public SettingsViewModel(SettingsService settingsService, CacheService cacheService)
    {
        _settingsService = settingsService;
        _cacheService = cacheService;

        OsuClientSecret = _settingsService.OsuClientSecret;
        OsuClientId = _settingsService.OsuClientId;
        OsuDirPath = _settingsService.OsuDirPath;
        IrcNickname = _settingsService.IrcNickname;
        IrcPassword = _settingsService.IrcPassword;

        InvalidateCacheCommand = ReactiveCommand.CreateFromTask(InvalidateCacheAsync);
        CheckUpdateCommand     = ReactiveCommand.CreateFromTask(CheckUpdateAsync);
        OpenReleasePageCommand = ReactiveCommand.Create(OpenReleasePage);

        CreateThemes();
        _ = CheckUpdateAsync();
    }

    private bool _cacheInvalidated;
    public bool CacheInvalidated
    {
        get => _cacheInvalidated;
        private set => this.RaiseAndSetIfChanged(ref _cacheInvalidated, value);
    }

    private async Task InvalidateCacheAsync()
    {
        await _cacheService.InvalidateAllAsync();
        CacheInvalidated = true;
    }



    private string? _osuClientSecret;
    private string? _osuClientId;
    private string? _osuDirPath;
    private string? _ircNickname;
    private string? _ircPassword;

    public string AppVersion => _settingsService.AppVersion;

    // ── Update check ─────────────────────────────────────────────────────────

    private bool _isCheckingUpdate;
    public bool IsCheckingUpdate
    {
        get => _isCheckingUpdate;
        private set => this.RaiseAndSetIfChanged(ref _isCheckingUpdate, value);
    }

    private string? _latestVersion;
    public string? LatestVersion
    {
        get => _latestVersion;
        private set
        {
            this.RaiseAndSetIfChanged(ref _latestVersion, value);
            this.RaisePropertyChanged(nameof(HasUpdate));
            this.RaisePropertyChanged(nameof(IsUpToDate));
        }
    }

    public bool HasUpdate =>
        Version.TryParse(LatestVersion, out var latest) &&
        Version.TryParse(AppVersion, out var current) &&
        latest > current;

    public bool IsUpToDate => LatestVersion != null && !HasUpdate;

    private async Task CheckUpdateAsync()
    {
        IsCheckingUpdate = true;
        LatestVersion = null;
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, ReleasesApiUrl);
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("OsuMapManager", AppVersion));
            var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var tag = doc.RootElement.GetProperty("tag_name").GetString()?.TrimStart('v');
            LatestVersion = tag;
        }
        catch { /* no network / API error — silently ignore */ }
        finally
        {
            IsCheckingUpdate = false;
        }
    }

    private static void OpenReleasePage()
    {
        try { Process.Start(new ProcessStartInfo(ReleasesPageUrl) { UseShellExecute = true }); }
        catch { }
    }

    public List<Color> ThemeColors => new()
    {
        Colors.Bisque,
        Colors.Cyan,
        Colors.DarkGray,
        Colors.DarkOliveGreen,
        Colors.DarkOrchid,
        Colors.DarkSlateGray,
        Colors.DeepSkyBlue,
        Colors.Firebrick,
        Colors.GreenYellow,
        Colors.IndianRed,
        Colors.Khaki,
        Colors.LightPink,
        Colors.MediumAquamarine,
        Colors.MediumPurple,
        Colors.MediumSeaGreen,
        Colors.MediumSlateBlue,
        Colors.NavajoWhite,
        Colors.PaleGoldenrod,
        Colors.PeachPuff,
        Colors.Salmon,
        Colors.SeaGreen,
        Colors.SkyBlue,
        Colors.SlateBlue,
        Colors.Wheat
    };

    private List<SukiColorTheme> Themes = new();

    public string? OsuClientSecret
    {
        get => _osuClientSecret;
        set
        {
            this.RaiseAndSetIfChanged(ref _osuClientSecret, value);
            NotifySettingsChanged(nameof(OsuClientSecret), value);
        }
    }
    public string? OsuClientId
    {
        get => _osuClientId;
        set
        {
            this.RaiseAndSetIfChanged(ref _osuClientId, value);
            NotifySettingsChanged(nameof(OsuClientId), value);
        }
    }
    public string? OsuDirPath
    {
        get => _osuDirPath;
        set
        {
            this.RaiseAndSetIfChanged(ref _osuDirPath, value);
            NotifySettingsChanged(nameof(OsuDirPath), value);
        }
    }
    public string? IrcNickname
    {
        get => _ircNickname;
        set
        {
            this.RaiseAndSetIfChanged(ref _ircNickname, value);
            NotifySettingsChanged(nameof(IrcNickname), value);
        }
    }
    public string? IrcPassword
    {
        get => _ircPassword;
        set
        {
            this.RaiseAndSetIfChanged(ref _ircPassword, value);
            NotifySettingsChanged(nameof(IrcPassword), value);
        }
    }

    public void SetThemeColor(object color)
    {
        SukiTheme.GetInstance().ChangeColorTheme(Themes.Where(t => t.DisplayName == color.ToString()).First());
    }

    public void OpenWebPageOsuApiKey()
    {
        _settingsService.OpenWebPageOsuApiKey();
    }


    private void CreateThemes()
    {
        ThemeColors.ForEach(c =>
        {
            var theme = new SukiColorTheme(c.ToString(), c, Colors.DarkBlue);
            Themes.Add(theme);
            SukiTheme.GetInstance().AddColorTheme(theme);
        });
    }
    private void NotifySettingsChanged(string propName, object? value)
    {
        Task.Run(() => _settingsService.UpdateSettings(propName, value));
    }
}
