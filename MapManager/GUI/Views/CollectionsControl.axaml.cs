using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MapManager.GUI.Models;
using MapManager.GUI.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace MapManager.GUI.Views;

public partial class CollectionsControl : UserControl
{
    public CollectionsControl()
    {
        InitializeComponent();
    }

    private async void OnRemoveClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        var beatmap = button.DataContext as Beatmap;
        if (beatmap == null)
            return;

        // идём вверх по визуальному дереву и ищем Collection
        var collection = button.GetVisualAncestors()
            .OfType<TreeViewItem>()
            .Select(i => i.DataContext)
            .OfType<Collection>()
            .FirstOrDefault();

        if (collection == null)
            return;
        var vm = DataContext as CollectionsViewModel;

        // ждём один тик UI перед удалением
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            vm.RemoveFromCollection(beatmap, collection);
        }, DispatcherPriority.Background);
    }

    private void ExportClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var vm = DataContext as CollectionsViewModel;
        var collection = (sender as Button)!.DataContext as Collection;


        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return;

        vm.ExportCollection(collection, topLevel);
    }
}