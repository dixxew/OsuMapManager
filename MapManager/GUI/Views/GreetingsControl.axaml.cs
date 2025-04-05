using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using MapManager.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MapManager.GUI.Views;

public partial class GreetingsControl : UserControl
{
    public GreetingsControl()
    {
        InitializeComponent();
    }


    List<Bitmap> frames = Directory
    .GetFiles("GUI/Assets/greetings", "*.png")
    .OrderBy(f => f)
    .Select(f => new Bitmap(f))
    .ToList();



    private void GreetingsLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        int currentFrame = 0;
        DispatcherTimer timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(8) // ~30fps
        };

        var IntroImage = this.FindControl<Image>("IntroImage");
        timer.Tick += (_, _) =>
        {
            if (currentFrame >= frames.Count)
            {
                timer.Stop();
                var vm = DataContext as GreetingsViewModel;
                if (vm != null)                
                    vm.NavigateToMainControl();
                
                return;
            }

            IntroImage.Source = frames[currentFrame];
            currentFrame++;
        };

        timer.Start();

    }
}