using Avalonia.Controls;
using MapManager.GUI.Services;
using MapManager.GUI.Views;
using ReactiveUI;
using SukiUI.Dialogs;
using static MapManager.GUI.Services.NavigationService;
using MapManager;

namespace MapManager.GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly NavigationService _navigationService;

    public MainWindowViewModel(NavigationService navigationService, AppSettings appSettings)
    {
        _navigationService = navigationService;

        _navigationService.Subscribe(NavigationTarget.MainContent, control => Content = control);
        _navigationService.Subscribe(NavigationTarget.DialogContent, control => DialogContent = control);

        _navigationService.SetContent(NavigationTarget.DialogContent, typeof(GreetingsViewModel));

        if (appSettings.SetupCompleted)
        {
            // MainViewModel constructor subscribes to MainBlockContent, so set it first
            _navigationService.SetContent(NavigationTarget.MainContent, typeof(MainViewModel));
            _navigationService.SetContent(NavigationTarget.MainBlockContent, typeof(MainBlockBeatmapViewModel));
        }
        else
        {
            // Setup wizard is the first screen; MainBlockContent will be wired after it navigates to Main
            _navigationService.SetContent(NavigationTarget.MainContent, typeof(SetupViewModel));
        }
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
