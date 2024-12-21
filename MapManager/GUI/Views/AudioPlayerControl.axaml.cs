using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using MapManager.GUI.ViewModels;
using SukiUI.Controls;
using System;
using System.Threading.Tasks;

namespace MapManager.GUI.Views;

public partial class AudioPlayerControl : UserControl
{
    private double _hoveredPosition;

    public AudioPlayerControl()
    {
        InitializeComponent();
    }

    private void PlayPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Grid grid)
        {
            // Сжимаем элемент до 90% размера
            if (grid.RenderTransform is ScaleTransform scaleTransform)
            {
                scaleTransform.ScaleX = 0.9;
                scaleTransform.ScaleY = 0.9;
            }
            grid.PointerReleased += GridReleased;
        }
    }
    private void StopPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Grid grid)
        {
            // Сжимаем элемент до 90% размера
            if (grid.RenderTransform is ScaleTransform scaleTransform)
            {
                scaleTransform.ScaleX = 0.9;
                scaleTransform.ScaleY = 0.9;
            }
            grid.PointerReleased += GridReleased;
        }
    }
    private void PrevPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Grid grid)
        {
            // Сжимаем элемент до 90% размера
            if (grid.RenderTransform is ScaleTransform scaleTransform)
            {
                scaleTransform.ScaleX = 0.9;
                scaleTransform.ScaleY = 0.9;
            }
            grid.PointerReleased += GridReleased;
        }
    }
    private void NextPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Grid grid)
        {
            // Сжимаем элемент до 90% размера
            if (grid.RenderTransform is ScaleTransform scaleTransform)
            {
                scaleTransform.ScaleX = 0.9;
                scaleTransform.ScaleY = 0.9;
            }
            grid.PointerReleased += GridReleased;
        }
    }
    private void HeartPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Grid grid)
        {
            // Сжимаем элемент до 90% размера
            if (grid.RenderTransform is ScaleTransform scaleTransform)
            {
                scaleTransform.ScaleX = 0.9;
                scaleTransform.ScaleY = 0.9;
            }
            grid.PointerReleased += GridReleased;
        }
    }
    private void RandomPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Grid grid)
        {
            // Сжимаем элемент до 90% размера
            if (grid.RenderTransform is ScaleTransform scaleTransform)
            {
                scaleTransform.ScaleX = 0.9;
                scaleTransform.ScaleY = 0.9;
            }
            grid.PointerReleased += GridReleased;
        }
    }
    private void RepeatPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Grid grid)
        {
            // Сжимаем элемент до 90% размера
            if (grid.RenderTransform is ScaleTransform scaleTransform)
            {
                scaleTransform.ScaleX = 0.9;
                scaleTransform.ScaleY = 0.9;
            }
            grid.PointerReleased += GridReleased;
        }
    }


    private void ProgressBar_PointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (DataContext is AudioPlayerViewModel vm)
        {
            vm.ClosePopup();
        }
    }

    private void ProgressBar_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (DataContext is AudioPlayerViewModel vm && sender is ProgressBar progressBar)
        {
            var position = e.GetPosition(progressBar);
            var relativePosition = position.X / progressBar.Bounds.Width;

            vm.UpdatePopupState(relativePosition, progressBar.Bounds.Width, progressBar.Minimum, progressBar.Maximum);
            vm.IsPopupOpen = true;
        }
    }

    private void ProgressBar_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (DataContext is AudioPlayerViewModel vm)
        {
            vm.SetSongPosition(vm.HoveredPosition);
        }
    }
    private async void GridReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (sender is Grid grid)
        {
            // Плавно возвращаем элемент к исходному размеру
            if (grid.RenderTransform is ScaleTransform scaleTransform)
            {
                for (double i = 0.9; i <= 1.0; i += 0.02)
                {
                    scaleTransform.ScaleX = i;
                    scaleTransform.ScaleY = i;
                    await Task.Delay(10); // Интервал между изменениями
                    grid.PointerReleased -= GridReleased;
                }
            }
        }
    }
}