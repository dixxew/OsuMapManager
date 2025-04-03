using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MapManager.GUI.ViewModels;

namespace MapManager.GUI.Views;

public partial class BeatmapsControl : UserControl
{
    public BeatmapsControl()
    {
        InitializeComponent();

        this.AttachedToVisualTree += (_, _) =>
        {
            var listBox = this.FindControl<ListBox>("BeatmapsListBox");
            var vm = DataContext as BeatmapsViewModel;

            vm.SelectedBeatmapSetChanged += () =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (listBox != null)
                        listBox!.Scroll.Offset = new Vector(0, 0);
                });
            };
        };
    }
}