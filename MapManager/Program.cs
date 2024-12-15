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
            return Host.CreateDefaultBuilder(args)
                .ConfigureOsuSharp((ctx, options) => options.Configuration = new OsuClientConfiguration
                {
                    ClientId = 36783,
                    ClientSecret = "XnR2bWHI4f3qEeCN0jsAdMA54jixORj40s7x43Rd"
                })
                .ConfigureServices((context, services) =>
                {
                    // Регистрация других зависимостей
                    services.AddTransient<OsuService>();
                })
                .Build();
        }
    }
}
