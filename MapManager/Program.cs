using Avalonia;
using Avalonia.ReactiveUI;
using MapManager.GUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using OsuSharp.Exceptions;
using OsuSharp.Extensions;
using OsuSharp;
using MapManager.OSU;
using OsuSharp.Legacy;
using MapManager.GUI.ViewModels;
using System.Net.Http;
using AutoMapper;

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

            BuildHost(args).Run();
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
            var appSettings = AppSettingsManager.LoadSettingsAsync().GetAwaiter().GetResult();

            return Host.CreateDefaultBuilder(args)
                .ConfigureOsuSharp((ctx, options) => options.Configuration = new OsuClientConfiguration
                {
                    ClientId = 36783,
                    ClientSecret = "XnR2bWHI4f3qEeCN0jsAdMA54jixORj40s7x43Rd"
                })
                .ConfigureServices(async (context, services) =>
                {
                    services.AddSingleton(appSettings);

                    services.AddTransient<OsuService>();
                    services.AddSingleton(_ => new LegacyOsuClient(new LegacyOsuSharpConfiguration
                    {
                        ApiKey = "c9f024e60c2551bea39b507163405098ed8fbd85"
                    })); 
                    services.AddSingleton<SettingsViewModel>();
                    services.AddSingleton<OsuDataReader>();
                    services.AddSingleton<AudioPlayerViewModel>();
                    services.AddSingleton<SearchFiltersViewModel>();
                    services.AddScoped<HttpClient>(); 
                    services.AddScoped<Mapper>(provider =>
                    {
                        var configuration = new MapperConfig().MapperConfiguration;
                        return new Mapper(configuration);
                    });
                    services.AddSingleton<MainWindowViewModel>();
                })
                .Build();
        }
    }
}
