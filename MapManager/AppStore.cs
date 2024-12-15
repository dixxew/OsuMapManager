using AutoMapper;
using MapManager.GUI;
using MapManager.GUI.ViewModels;
using MapManager.OSU;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OsuSharp;
using OsuSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MapManager;

public static class AppStore
{
    public static string OsuDirectory => SettingsVM.OsuDirPath;
    public static SettingsViewModel SettingsVM { get; set; } = new();
    public static MainWindowViewModel MainWindowVM { get; set; } = new();
    public static AudioPlayerViewModel AudioPlayerVM { get; set; } = new();
    public static OsuService OsuService => App.AppHost.Services.GetRequiredService<OsuService>();
    public static HttpClient SharedHttpClient => new();
    public static Mapper Mapper = new(new MapperConfig().MapperConfiguration);
}
