using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MapManager.GUI.Views;
using Microsoft.Extensions.Hosting;
using System;

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
                desktop.MainWindow = new MainWindow
                {
                    DataContext = AppStore.MainWindowVM,
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

    }
}