using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MapManager.GUI.ViewModels;

namespace MapManager.GUI.Views;

public partial class SetupControl : UserControl
{
    public SetupControl()
    {
        InitializeComponent();
    }

    private async void OnBrowseOsuPath(object? sender, RoutedEventArgs e)
    {
        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storage is null) return;

        var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select osu! installation folder",
            AllowMultiple = false,
        });

        if (folders.Count > 0 && DataContext is SetupViewModel vm)
            vm.ApplyBrowsedPath(folders[0].Path.LocalPath);
    }
}
