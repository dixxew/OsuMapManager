using Avalonia.Controls;
using SukiUI.Dialogs;

namespace MapManager.GUI.Dialogs;

public partial class TextBoxDialogView : UserControl
{
    public TextBoxDialogView()
    {
        InitializeComponent();
    }

    public ISukiDialog Dialog;
}