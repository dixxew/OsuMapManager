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
        DataContext = AppStore.AudioPlayerVM;
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
        AppStore.AudioPlayerVM.PlayPauseCommand();
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
        AppStore.AudioPlayerVM.StopCommand();
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
        AppStore.AudioPlayerVM.PrevCommand();
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
        AppStore.AudioPlayerVM.NextCommand();
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
        AppStore.AudioPlayerVM.ToggleFavorite();
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
        AppStore.AudioPlayerVM.IsRandomEnabled = !AppStore.AudioPlayerVM.IsRandomEnabled;
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
        AppStore.AudioPlayerVM.IsLoopEnabled = !AppStore.AudioPlayerVM.IsLoopEnabled;
    }


    private void ProgressBar_PointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        // Закрываем Popup при выходе курсора
        AppStore.AudioPlayerVM.IsPopupOpen = false;
    }

    private void ProgressBar_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (sender is ProgressBar progressBar)
        {
            // Позиция курсора относительно ProgressBar
            var position = e.GetPosition(progressBar);

            // Вычисляем временную метку
            var relativePosition = position.X / progressBar.Bounds.Width;
            var hoveredTime = progressBar.Minimum + relativePosition * (progressBar.Maximum - progressBar.Minimum);
            _hoveredPosition = hoveredTime; // Сохраняем позицию для перемотки
            AppStore.AudioPlayerVM.PopupTime = TimeSpan.FromSeconds(hoveredTime).ToString(@"m\:ss");

            // Устанавливаем Popup в фиксированной высоте, но по горизонтали над курсором
            TimePopup.PlacementMode = PlacementMode.AnchorAndGravity;
            TimePopup.PlacementAnchor = PopupAnchor.Top; // Привязка к верхней части ProgressBar
            TimePopup.PlacementGravity = PopupGravity.Bottom; // Размещаем Popup снизу вверх
            TimePopup.HorizontalOffset = position.X; // Горизонтальное смещение по ширине ProgressBar
            TimePopup.VerticalOffset = -40; // Фиксированное положение над ProgressBar

            AppStore.AudioPlayerVM.IsPopupOpen = true;
        }
    }


    private void ProgressBar_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is ProgressBar progressBar)
        {
            AppStore.AudioPlayerVM.SongProgress = _hoveredPosition;
            AppStore.AudioPlayerVM.SetSongPosition(_hoveredPosition);
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