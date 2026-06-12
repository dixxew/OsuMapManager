using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MapManager.GUI.Views;

public partial class SearchFiltersControl : UserControl
{
    public SearchFiltersControl()
    {
        InitializeComponent();
        this.AddHandler(KeyDownEvent, OnPreviewSliderKey, RoutingStrategies.Tunnel);
    }

    private static void OnPreviewSliderKey(object? sender, KeyEventArgs e)
    {
        if (e.Source is not Slider slider) return;
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift)) return;

        double step = e.KeyModifiers.HasFlag(KeyModifiers.Control) ? 1.0 : 0.5;
        switch (e.Key)
        {
            case Key.Right:
            case Key.Up:
                slider.Value = Math.Min(slider.Maximum, slider.Value + step);
                e.Handled = true;
                break;
            case Key.Left:
            case Key.Down:
                slider.Value = Math.Max(slider.Minimum, slider.Value - step);
                e.Handled = true;
                break;
        }
    }
}
