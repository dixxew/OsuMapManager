using MapManager.GUI.ViewModels;
using ReactiveUI;
using SukiUI.Dialogs;
using System;

namespace MapManager.GUI.Dialogs;
public class TextBoxDialogViewModel : ViewModelBase
{
    private ISukiDialogManager dialog => MainWindowViewModel.DialogManager;

    public TextBoxDialogViewModel(Func<string, bool> onAccept)
    {
        _onAccept = onAccept;
    }

    private readonly Func<string, bool> _onAccept;

    private string _textBoxText;

    public string TextBoxText
    {
        get => _textBoxText;
        set => this.RaiseAndSetIfChanged(ref _textBoxText, value);
    }


    public void Accept()
    {
        var result = _onAccept.Invoke(TextBoxText);

        if (result)
            dialog.DismissDialog();
    }

    public void Dismiss()
    {
        dialog.DismissDialog();
    }
}