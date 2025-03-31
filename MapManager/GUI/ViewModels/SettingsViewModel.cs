using ReactiveUI;
using System.Diagnostics;
using System;
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


    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;

        OsuClientSecret = _settingsService.OsuClientSecret;
        OsuClientId = _settingsService.OsuClientId;
        OsuDirPath = _settingsService.OsuDirPath;

        CreateThemes();
    }

    

    private string? _osuClientSecret;
    private string? _osuClientId;
    private string? _osuDirPath;

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
