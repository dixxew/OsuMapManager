using AutoMapper;
using Avalonia;
using Avalonia.ReactiveUI;
using MapManager.GUI;
using MapManager.GUI.Services;
using MapManager.GUI.ViewModels;
using MapManager.GUI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OsuSharp;
using OsuSharp.Extensions;
using OsuSharp.Legacy;
using System;
using System.Net.Http;

namespace MapManager
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                     .UsePlatformDetect()
                     .WithInterFont()
                     .LogToTrace()
                     .UseReactiveUI();
        }

        public static IHost BuildHost(string[] args)
        {
            var appSettings = AppSettingsManager.LoadSettingsAsync();

            return Host.CreateDefaultBuilder(args)
                .ConfigureOsuSharp((ctx, options) => options.Configuration = new OsuClientConfiguration
                {
                    ClientId = 0,
                    ClientSecret = ""
                })
                .ConfigureServices((context, services) =>
                {

                    services.AddSingleton(appSettings);

                    services.AddSingleton<OsuApiService>();
                    services.AddSingleton<SettingsService>();
                    services.AddSingleton<AppInitializationService>();
                    services.AddSingleton<AuxiliaryService>();
                    services.AddSingleton<AudioPlayerService>();
                    services.AddSingleton<RankingService>();
                    services.AddSingleton<BeatmapDataService>();
                    services.AddSingleton<BeatmapService>();
                    services.AddSingleton<NavigationService>();
                    services.AddSingleton<OsuDataService>();

                    services.AddSingleton<ViewLocator>(sp =>
                    {
                        var locator = new ViewLocator();

                        locator.Register<GreetingsViewModel, GreetingsControl>();
                        locator.Register<MainViewModel, MainControl>();

                        return locator;
                    });



                    services.AddHostedService<AppInitializationService>();
                    services.AddSingleton<SettingsViewModel>();

                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<AudioPlayerViewModel>();
                    services.AddSingleton<SearchFiltersViewModel>();
                    services.AddSingleton<BeatmapsSearchViewModel>();
                    services.AddSingleton<BeatmapBlockCollectionsViewModel>();
                    services.AddSingleton<BeatmapsViewModel>();
                    services.AddSingleton<CollectionsSearchViewModel>();
                    services.AddSingleton<CollectionsViewModel>();
                    services.AddSingleton<BeatmapInfoViewModel>();
                    services.AddSingleton<LocalScoresViewModel>();
                    services.AddSingleton<GlobalScoresViewModel>();
                    services.AddSingleton<GreetingsViewModel>();


                    services.AddSingleton(_ => new LegacyOsuClient(new LegacyOsuSharpConfiguration
                        {
                            ApiKey = "c9f024e60c2551bea39b507163405098ed8fbd85"
                        }));
                    services.AddScoped<HttpClient>();
                    services.AddAutoMapper(typeof(MappingProfile));


                    services.AddSingleton<MainWindowViewModel>();
                })
                .Build();
        }
    }
}
