using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MapManager.GUI.Views; 

public partial class BeatmapsSearchControl : UserControl
{
    public BeatmapsSearchControl()
    {
        InitializeComponent();

        this.AttachedToVisualTree += (_, _) =>
        {
            var tagScrollViewer = this.FindControl<ScrollViewer>("TagScrollViewer");

            if (tagScrollViewer != null)
            {
                tagScrollViewer.AddHandler(
                    InputElement.PointerWheelChangedEvent,
                    (s, ev) =>
                    {
                        var delta = ev.Delta;
                        tagScrollViewer.Offset = new Vector(
                            tagScrollViewer.Offset.X - delta.Y * 20,
                            tagScrollViewer.Offset.Y
                        );
                        ev.Handled = true;
                    },
                    RoutingStrategies.Tunnel | RoutingStrategies.Bubble
                );
            }
        };
    }
}