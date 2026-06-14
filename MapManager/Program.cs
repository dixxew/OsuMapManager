using AutoMapper;
using Avalonia;
using Avalonia.ReactiveUI;
using MapManager.GUI;
using MapManager.GUI.Services;
using MapManager.GUI.Services.Logging;
using MapManager.GUI.ViewModels;
using MapManager.GUI.Views;
using Meebey.SmartIrc4net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OsuSharp;
using OsuSharp.Extensions;
using OsuSharp.Legacy;
using System;
using System.Net.Http;
using System.Runtime.InteropServices;

namespace MapManager
{
    internal sealed class Program
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SetCurrentProcessExplicitAppUserModelID(string AppID);

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            SetCurrentProcessExplicitAppUserModelID("MapManager.App");
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

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureOsuSharp((ctx, options) => options.Configuration = new OsuClientConfiguration
                {
                    ClientId = 0,
                    ClientSecret = ""
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddLogging(b =>
                    {
                        b.ClearProviders();
                        b.AddFileLogger(LogLevel.Trace);       // основной sink — файл (см. FileLoggerProvider)
                        b.AddDebug();                          // дублируем в Debug-вывод для Rider
                        b.SetMinimumLevel(LogLevel.Trace);     // альфа: пишем максимально подробно
                        b.AddFilter("Microsoft", LogLevel.Warning);     // заглушить хостинг-шум
                        b.AddFilter("System", LogLevel.Warning);
                        b.AddFilter("OsuSharp", LogLevel.Warning);
                        // Наш код (MapManager.*) пишется на Trace без ограничений.
                    });

                    services.AddSingleton(appSettings);

                    services.AddSingleton<CacheService>();
                    services.AddSingleton<AppStartupGate>();
                    services.AddSingleton<OsuApiService>();
                    services.AddSingleton<SettingsService>();
                    services.AddSingleton<AppInitializationService>();
                    services.AddSingleton<AuxiliaryService>();
                    services.AddSingleton<AudioPlayerService>();
                    services.AddSingleton<RankingService>();
                    services.AddSingleton<BeatmapDataService>();
                    services.AddSingleton<BeatmapService>();
                    services.AddSingleton<CollectionService>();
                    services.AddSingleton<NavigationService>();
                    services.AddSingleton<OsuDataService>();
                    services.AddSingleton<ThumbnailService>();
                    services.AddSingleton<IrcClient>();
                    services.AddSingleton<ChatService>();
                    services.AddSingleton<AvatarService>();
                    services.AddSingleton<NotificationService>();
                    services.AddSingleton<OsuPpsService>();
                    services.AddSingleton<OsuPpsFiltersViewModel>();
                    services.AddSingleton<OsuPpsViewModel>();
                    services.AddSingleton<SetupViewModel>();
                    services.AddSingleton<BeatmapDownloadService>();
                    services.AddSingleton<DownloadManagerViewModel>();
                    
                    

                    services.AddSingleton<ViewLocator>(sp =>
                    {
                        var locator = new ViewLocator();

                        locator.Register<GreetingsViewModel, GreetingsControl>();
                        locator.Register<MainViewModel, MainControl>();
                        locator.Register<ChatViewModel, ChatControl>();
                        locator.Register<MainBlockBeatmapViewModel, MainBlockBeatmapControl>();
                        locator.Register<SetupViewModel, SetupControl>();

                        return locator;
                    });



                    services.AddSingleton<AppInitializationService>();
                    services.AddHostedService(sp => sp.GetRequiredService<AppInitializationService>());
                    services.AddSingleton<SettingsViewModel>();

                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<AudioPlayerViewModel>();
                    services.AddSingleton<SearchFiltersViewModel>();
                    services.AddSingleton<SearchOptionsViewModel>();
                    services.AddSingleton<BeatmapsSearchViewModel>();
                    services.AddSingleton<BeatmapBlockCollectionsViewModel>();
                    services.AddSingleton<BeatmapsViewModel>();
                    services.AddSingleton<CollectionsSearchViewModel>();
                    services.AddSingleton<CollectionsViewModel>();
                    services.AddSingleton<BeatmapInfoViewModel>();
                    services.AddSingleton<LocalScoresViewModel>();
                    services.AddSingleton<GlobalScoresViewModel>();
                    services.AddSingleton<BeatmapCommentsViewModel>();
                    services.AddSingleton<GreetingsViewModel>();
                    services.AddSingleton<ChatViewModel>();
                    services.AddSingleton<MainBlockBeatmapViewModel>();

                    services.AddScoped<HttpClient>();
                    services.AddAutoMapper(typeof(MappingProfile));


                    services.AddSingleton<MainWindowViewModel>();
                })
                .Build();

            // Прокидываем логгер в статические хелперы, у которых нет DI-конструктора.
            var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            AppSettingsManager.Logger = loggerFactory.CreateLogger("MapManager.AppSettingsManager");
            FavoriteBeatmapManager.Logger = loggerFactory.CreateLogger("MapManager.GUI.Services.FavoriteBeatmapManager");

            var bootLog = loggerFactory.CreateLogger("MapManager.Program");
            bootLog.LogInformation("Host built; logs at {LogDir}", FileLoggerProvider.LogDirectory);

            return host;
        }
    }
}
