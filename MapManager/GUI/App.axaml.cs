using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MapManager.GUI.ViewModels;
using MapManager.GUI.Views;
using MapManager.OSU;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;

namespace MapManager.GUI
{
    public partial class App : Application
    {
        public static IHost AppHost { get; private set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            AppHost = Program.BuildHost(Array.Empty<string>());

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var settingsViewModel = AppHost.Services.GetRequiredService<SettingsViewModel>();

                //settingsViewModel.OnSettingsChanged += () =>
                //{
                //    var appSettings = AppHost.Services.GetRequiredService<AppSettings>();
                //    // Services that need runtime settings changing (Should implement UpdateSettings(AppSettings settings))
                //    var osuService = AppHost.Services.GetRequiredService<OsuService>();

                //    //Add here services for runtime settings changing
                //    osuService.UpdateSettings(appSettings);
                //};
                settingsViewModel.InitSettings();
                var mainWindowVM = AppHost.Services.GetRequiredService<MainWindowViewModel>();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainWindowVM,
                };
            }
            ThreadPool.QueueUserWorkItem(_ =>
            {
                AppHost.Run();
            });
            base.OnFrameworkInitializationCompleted();
        }

    }
}