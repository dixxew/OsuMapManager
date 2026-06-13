using MapManager.GUI.ViewModels;
using ReactiveUI;
using SukiUI.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;

namespace MapManager.GUI.Dialogs;

public class ChannelPickerDialogViewModel : ViewModelBase
{
    private ISukiDialogManager dialog => MainWindowViewModel.DialogManager;

    public ObservableCollection<string> Channels { get; }

    public ReactiveCommand<string, Unit> Join { get; }
    public ReactiveCommand<Unit, Unit> Dismiss { get; }

    public ChannelPickerDialogViewModel(IEnumerable<string> channels, Action<string> onJoin)
    {
        Channels = new ObservableCollection<string>(channels);

        Join = ReactiveCommand.Create<string>(channel =>
        {
            onJoin(channel);
            dialog.DismissDialog();
        });

        Dismiss = ReactiveCommand.Create(() => dialog.DismissDialog());
    }
}
