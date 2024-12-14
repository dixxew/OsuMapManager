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
            // ������� ������� �� 90% �������
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
            // ������� ������� �� 90% �������
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
            // ������� ������� �� 90% �������
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
            // ������� ������� �� 90% �������
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
            // ������� ������� �� 90% �������
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
            // ������� ������� �� 90% �������
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
            // ������� ������� �� 90% �������
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
        // ��������� Popup ��� ������ �������
        AppStore.AudioPlayerVM.IsPopupOpen = false;
    }

    private void ProgressBar_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (sender is ProgressBar progressBar)
        {
            // ������� ������� ������������ ProgressBar
            var position = e.GetPosition(progressBar);

            // ��������� ��������� �����
            var relativePosition = position.X / progressBar.Bounds.Width;
            var hoveredTime = progressBar.Minimum + relativePosition * (progressBar.Maximum - progressBar.Minimum);
            _hoveredPosition = hoveredTime; // ��������� ������� ��� ���������
            AppStore.AudioPlayerVM.PopupTime = TimeSpan.FromSeconds(hoveredTime).ToString(@"m\:ss");

            // ������������� Popup � ������������� ������, �� �� ����������� ��� ��������
            TimePopup.PlacementMode = PlacementMode.AnchorAndGravity;
            TimePopup.PlacementAnchor = PopupAnchor.Top; // �������� � ������� ����� ProgressBar
            TimePopup.PlacementGravity = PopupGravity.Bottom; // ��������� Popup ����� �����
            TimePopup.HorizontalOffset = position.X; // �������������� �������� �� ������ ProgressBar
            TimePopup.VerticalOffset = -40; // ������������� ��������� ��� ProgressBar

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
            // ������ ���������� ������� � ��������� �������
            if (grid.RenderTransform is ScaleTransform scaleTransform)
            {
                for (double i = 0.9; i <= 1.0; i += 0.02)
                {
                    scaleTransform.ScaleX = i;
                    scaleTransform.ScaleY = i;
                    await Task.Delay(10); // �������� ����� �����������
                    grid.PointerReleased -= GridReleased;
                }
            }
        }
    }
}