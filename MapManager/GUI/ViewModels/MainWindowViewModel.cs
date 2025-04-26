using Avalonia.Controls;
using MapManager.GUI.Services;
using MapManager.GUI.Views;
using ReactiveUI;
using SukiUI.Dialogs;
using static MapManager.GUI.Services.NavigationService;

namespace MapManager.GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly NavigationService _navigationService;

    public MainWindowViewModel(NavigationService navigationService)
    {
        _navigationService = navigationService;

        // подписка на главный контент
        _navigationService.Subscribe(NavigationTarget.MainContent, control =>
        {
            Content = control;
        });

        _navigationService.Subscribe(NavigationTarget.DialogContent, control =>
        {
            DialogContent = control;
        });

        // старт с Greetings
        _navigationService.SetContent(NavigationTarget.MainContent, typeof(MainViewModel));
        _navigationService.SetContent(NavigationTarget.DialogContent, typeof(GreetingsViewModel));
        _navigationService.SetContent(NavigationTarget.MainBlockContent, typeof(MainBlockBeatmapViewModel));
    }

    public static ISukiDialogManager DialogManager { get; } = new SukiDialogManager();

    private UserControl _content;

    public UserControl Content
    {
        get => _content;
        set => this.RaiseAndSetIfChanged(ref _content, value);
    }

    private UserControl _dialogContent;

    public UserControl DialogContent
    {
        get => _dialogContent;
        set => this.RaiseAndSetIfChanged(ref _dialogContent, value);
    }

    public void SwitchChat()
    {
        _navigationService.SwitchChatContent?.Invoke();
    }
}
