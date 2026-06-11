using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using MapManager.GUI.ViewModels;
using MapManager.GUI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SukiUI.Models;
using SukiUI;
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
                var mainWindowVM = AppHost.Services.GetRequiredService<MainWindowViewModel>();
                desktop.MainWindow = new MainWindow()
                {
                    DataContext = mainWindowVM,
                };

                desktop.MainWindow.Closed += (_, _) =>
                {
                    AppHost.Services.GetRequiredService<Services.AudioPlayerService>().Stop();
                    AppHost.Services.GetRequiredService<Services.ChatService>().Disconnect();
                    AppHost.StopAsync().Wait(TimeSpan.FromSeconds(2));
                    desktop.Shutdown();
                    // добиваем не-фоновые потоки (NAudio/IRC), которые иначе держат процесс живым
                    Environment.Exit(0);
                };
            }
            ThreadPool.QueueUserWorkItem(_ =>
            {
                AppHost.Run();
            });
            base.OnFrameworkInitializationCompleted();

            var PurpleTheme = new SukiColorTheme("Purple", Colors.MediumPurple, Colors.DarkBlue);
            SukiTheme.GetInstance().AddColorTheme(PurpleTheme);
            SukiTheme.GetInstance().ChangeColorTheme(PurpleTheme);
        }

    }
}