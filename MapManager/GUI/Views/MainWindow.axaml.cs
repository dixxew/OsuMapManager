using MapManager.GUI.ViewModels;
using SukiUI.Controls;

namespace MapManager.GUI.Views;

public partial class MainWindow : SukiWindow
{
    private MainWindowViewModel viewModel;
    public MainWindow()
    {
        InitializeComponent();
    }

    private void MainWindowLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var viewModel = DataContext as MainWindowViewModel;
        viewModel.OnMainWindowLoaded(); 
    }
}