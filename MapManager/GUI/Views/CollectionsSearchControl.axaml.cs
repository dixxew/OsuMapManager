using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MapManager.GUI.ViewModels;

namespace MapManager.GUI.Views;

public partial class CollectionsSearchControl : UserControl
{
    public CollectionsSearchControl()
    {
        InitializeComponent();
    }

    private void ImportClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var vm = DataContext as CollectionsSearchViewModel;


        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return;

        vm.ImportCollections(topLevel);
    }
}