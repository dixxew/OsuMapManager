using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MapManager.GUI.ViewModels;
using System.Linq;

namespace MapManager.GUI.Views;

public partial class OsuPpsControl : UserControl
{
    public OsuPpsControl()
    {
        InitializeComponent();

        // TemplateApplied fires after InitializeComponent sets up the ListBox,
        // so by then GetVisualDescendants() can find the inner ScrollViewer.
        PpsList.TemplateApplied += OnListTemplateApplied;

        Loaded += OnLoaded;
    }

    private void OnListTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        PpsList.TemplateApplied -= OnListTemplateApplied;
        var sv = PpsList.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (sv is not null)
            sv.ScrollChanged += OnScrollChanged;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is OsuPpsViewModel vm)
            await vm.InitializeAsync();
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer sv) return;
        if (DataContext is not OsuPpsViewModel vm) return;
        if (!vm.HasMore || vm.IsLoading) return;

        // Load next page when within 300px of the bottom
        if (sv.Offset.Y + sv.Viewport.Height >= sv.Extent.Height - 300)
            vm.LoadNextPage();
    }

    private void OnListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not OsuPpsViewModel vm) return;
        if (PpsList.SelectedItem is OsuPpsEntryViewModel entry)
            vm.SelectLocal(entry);
    }
}
