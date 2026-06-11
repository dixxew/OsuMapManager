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
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MapManager.GUI.Views;

public partial class GreetingsControl : UserControl
{
    // Кадры декодируются в фоне небольшим окном вперёд, проигранные — диспозятся.
    // Держать все 240 кадров 1000x1000 в памяти нельзя: это ~960 МБ нативной кучи.
    private const int PrefetchWindow = 10;
    private const int DecodeWidth = 800; // Image 400x400 при ScaleX/Y до 2.0 → максимум 800px

    private DispatcherTimer frameTimer;
    private Bitmap previousFrame;

    public GreetingsControl()
    {
        InitializeComponent();
        this.AttachedToVisualTree += OnLoaded;
    }

    private async void OnLoaded(object? sender, VisualTreeAttachmentEventArgs e)
    {
        var introImage = this.FindControl<Image>("IntroImage");

        var files = Directory.GetFiles("GUI/Assets/greetings", "*.png")
            .OrderBy(x => x)
            .ToArray();

        if (files.Length == 0)
            return;

        var channel = Channel.CreateBounded<Bitmap>(PrefetchWindow);
        _ = Task.Run(async () =>
        {
            foreach (var file in files)
            {
                using var stream = File.OpenRead(file);
                var bitmap = Bitmap.DecodeToWidth(stream, DecodeWidth);
                await channel.Writer.WriteAsync(bitmap);
            }
            channel.Writer.Complete();
        });

        introImage.Source = await channel.Reader.ReadAsync();

        // плавное масштабирование первого кадра
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

        frameTimer.Tick += (_, _) =>
        {
            if (!channel.Reader.TryRead(out var nextFrame))
            {
                if (!channel.Reader.Completion.IsCompleted)
                    return; // декодер не успел — ждём следующий тик

                frameTimer.Stop();

                this.Opacity = 0;
                if (this.Parent is ContentControl parent)
                    parent.Content = null;

                previousFrame?.Dispose();
                previousFrame = null;
                var last = introImage.Source as Bitmap;
                introImage.Source = null;
                last?.Dispose();
                return;
            }

            // диспозим кадр, который уже точно не на экране (N-2)
            previousFrame?.Dispose();
            previousFrame = introImage.Source as Bitmap;
            introImage.Source = nextFrame;
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
