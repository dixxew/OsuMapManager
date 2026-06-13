using MapManager;
using MapManager.GUI.Services;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using static MapManager.GUI.Services.NavigationService;

namespace MapManager.GUI.ViewModels;

public class SetupViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly NavigationService _navigationService;
    private readonly AppSettings _appSettings;
    private readonly AppStartupGate _gate;

    public SetupViewModel(SettingsService settingsService, NavigationService navigationService, AppSettings appSettings, AppStartupGate gate)
    {
        _settingsService = settingsService;
        _navigationService = navigationService;
        _appSettings = appSettings;
        _gate = gate;

        // Pre-fill: existing setting, else auto-detect, else empty
        var existingPath = _settingsService.OsuDirPath;
        OsuPath = IsValidOsuDir(existingPath) ? existingPath : AutoDetectOsuPath() ?? existingPath ?? "";
        ClientId = _settingsService.OsuClientId ?? "";
        ClientSecret = _settingsService.OsuClientSecret ?? "";
        IrcNickname = _settingsService.IrcNickname ?? "";
        IrcPassword = _settingsService.IrcPassword ?? "";

        SaveAndContinueCommand = ReactiveCommand.CreateFromTask(SaveAndContinueAsync);
        SkipCommand            = ReactiveCommand.CreateFromTask(SkipAsync);
        OpenApiPageCommand     = ReactiveCommand.Create(OpenApiPage);
        OpenIrcPageCommand     = ReactiveCommand.Create(OpenIrcPage);
    }

    // ── Properties ──────────────────────────────────────────────────────────

    private string? _osuPath;
    public string? OsuPath
    {
        get => _osuPath;
        set
        {
            this.RaiseAndSetIfChanged(ref _osuPath, value);
            this.RaisePropertyChanged(nameof(OsuPathValid));
            this.RaisePropertyChanged(nameof(OsuPathInvalid));
            this.RaisePropertyChanged(nameof(OsuPathStatusText));
        }
    }

    public bool OsuPathValid   => IsValidOsuDir(OsuPath);
    public bool OsuPathInvalid => !string.IsNullOrWhiteSpace(OsuPath) && !OsuPathValid;
    public string OsuPathStatusText =>
        OsuPathValid   ? "✓  osu! found" :
        OsuPathInvalid ? "✗  osu!.exe not found at this path" :
        "";

    private string? _clientId;
    public string? ClientId
    {
        get => _clientId;
        set => this.RaiseAndSetIfChanged(ref _clientId, value);
    }

    private string? _clientSecret;
    public string? ClientSecret
    {
        get => _clientSecret;
        set => this.RaiseAndSetIfChanged(ref _clientSecret, value);
    }

    private string? _ircNickname;
    public string? IrcNickname
    {
        get => _ircNickname;
        set => this.RaiseAndSetIfChanged(ref _ircNickname, value);
    }

    private string? _ircPassword;
    public string? IrcPassword
    {
        get => _ircPassword;
        set => this.RaiseAndSetIfChanged(ref _ircPassword, value);
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    public ReactiveCommand<Unit, Unit> SaveAndContinueCommand { get; }
    public ReactiveCommand<Unit, Unit> SkipCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenApiPageCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenIrcPageCommand { get; }

    // Called from code-behind after folder picker resolves
    public void ApplyBrowsedPath(string path) => OsuPath = path;

    // ── Handlers ─────────────────────────────────────────────────────────────

    private async Task SaveAndContinueAsync()
    {
        // Apply non-empty values via SettingsService so events (API reconnect, IRC reconnect) fire
        if (!string.IsNullOrWhiteSpace(OsuPath))       _settingsService.OsuDirPath     = OsuPath;
        if (!string.IsNullOrWhiteSpace(ClientId))      _settingsService.OsuClientId     = ClientId;
        if (!string.IsNullOrWhiteSpace(ClientSecret))  _settingsService.OsuClientSecret = ClientSecret;
        if (!string.IsNullOrWhiteSpace(IrcNickname))   _settingsService.IrcNickname     = IrcNickname;
        if (!string.IsNullOrWhiteSpace(IrcPassword))   _settingsService.IrcPassword     = IrcPassword;

        await FinishSetupAsync();
    }

    private async Task SkipAsync()
    {
        await FinishSetupAsync();
    }

    private async Task FinishSetupAsync()
    {
        _appSettings.SetupCompleted = true;
        await AppSettingsManager.SaveSettingsAsync(_appSettings);

        _gate.SignalReady();

        // MainViewModel constructor registers the MainBlockContent subscriber,
        // so we must set MainContent first, then MainBlockContent.
        _navigationService.SetContent(NavigationTarget.MainContent, typeof(MainViewModel));
        _navigationService.SetContent(NavigationTarget.MainBlockContent, typeof(MainBlockBeatmapViewModel));
    }

    private static void OpenApiPage()
    {
        try { Process.Start(new ProcessStartInfo("https://osu.ppy.sh/home/account/edit#oauth") { UseShellExecute = true }); }
        catch { }
    }

    private static void OpenIrcPage()
    {
        try { Process.Start(new ProcessStartInfo("https://osu.ppy.sh/p/irc") { UseShellExecute = true }); }
        catch { }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsValidOsuDir(string? path) =>
        !string.IsNullOrWhiteSpace(path) && File.Exists(Path.Combine(path, "osu!.exe"));

    private static string? AutoDetectOsuPath()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osu!"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "osu!"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "osu!"),
            @"C:\osu!",
        };
        return Array.Find(candidates, p => File.Exists(Path.Combine(p, "osu!.exe")));
    }
}
