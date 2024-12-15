using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MapManager.GUI.Views;

public partial class SettingsControl : UserControl
{
    public SettingsControl()
    {
        InitializeComponent();
        DataContext = AppStore.SettingsVM;
    }
}