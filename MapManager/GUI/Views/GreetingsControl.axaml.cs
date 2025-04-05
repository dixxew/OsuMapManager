using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using MapManager.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MapManager.GUI.Views;

public partial class GreetingsControl : UserControl
{
    private List<Bitmap> frames;
    private int currentFrame = 0;
    private DispatcherTimer frameTimer;

    public GreetingsControl()
    {
        InitializeComponent();
        this.AttachedToVisualTree += OnLoaded;
    }

    private async void OnLoaded(object? sender, VisualTreeAttachmentEventArgs e)
    {
        var introImage = this.FindControl<Image>("IntroImage");

        frames = Directory.GetFiles("GUI/Assets/greetings", "*.png")
            .OrderBy(x => x)
            .Select(x => new Bitmap(x))
            .ToList();

        introImage.Source = frames[0];

        // Анимация масштабирования вручную через таймер
        if (introImage.RenderTransform is ScaleTransform scale)
        {
            var scaleStartTime = DateTime.Now;
            var scaleDuration = TimeSpan.FromSeconds(2);

            var scaleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };

            scaleTimer.Tick += (_, _) =>
            {
                var elapsed = DateTime.Now - scaleStartTime;
                var progress = Math.Clamp(elapsed.TotalMilliseconds / scaleDuration.TotalMilliseconds, 0, 1);

                // ease-out
                progress = 1 - Math.Pow(1 - progress, 3);

                var value = 0.5 + (2.0 - 0.5) * progress;
                scale.ScaleX = value;
                scale.ScaleY = value;

                if (progress >= 1)
                    scaleTimer.Stop();
            };

            scaleTimer.Start();
            
        }

        // кадры
        frameTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(8)
        };

        frameTimer.Tick += async (_, _) =>
        {
            currentFrame++;
            if (currentFrame >= frames.Count)
            {
                frameTimer.Stop();

                
                this.Opacity = 0;
                if (this.Parent is ContentControl parent)
                    parent.Content = null;
                return;
            }

            introImage.Source = frames[currentFrame];
        };

        frameTimer.Start();
        var fadeOut = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(3500),
                Children =
                    {
                            new KeyFrame
                            {
                                Cue = new Cue(0),
                                Setters = { new Setter(UserControl.OpacityProperty, 1.0) }
                            },
                            new KeyFrame
                            {
                                Cue = new Cue(0.8),
                                Setters = { new Setter(UserControl.OpacityProperty, 1.0) }
                            },
                            new KeyFrame
                            {
                                Cue = new Cue(1),
                                Setters = { new Setter(UserControl.OpacityProperty, 0.0) }
                            }
                    }
            };

            fadeOut.RunAsync(this, default);
    }
}
