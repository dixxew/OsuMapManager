using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using MapManager.GUI.Models;
using MapManager.GUI.ViewModels;
using SukiUI.Controls;
using System;
using System.Linq;

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