using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using System.ComponentModel;
using System.Linq;

namespace MapManager.GUI.Views;

public partial class BeatmapBlockCollectionsControl : UserControl
{
    public BeatmapBlockCollectionsControl()
    {
        InitializeComponent();
    }

    private void OnListBoxItemPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is not Grid listBoxItemGrid)
            return;
        var listBoxItem = listBoxItemGrid.GetLogicalParent() as ListBoxItem;
        var listBox = listBoxItem.GetLogicalParent() as ListBox;

        var dataContext = listBox.DataContext;

        // Переключаем выделение вручную
        if (listBox.SelectedItems.Contains(listBoxItem.DataContext))
        {
            listBox.SelectedItems.Remove(listBoxItem.DataContext);
        }
        else
        {
            listBox.SelectedItems.Add(listBoxItem.DataContext);
        }

        e.Handled = true; // Предотвращаем стандартное поведение
    }
}