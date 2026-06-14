using Avalonia.Controls;
using Avalonia.Threading;
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
    private readonly BeatmapDownloadService _beatmapDownloadService;

    // DownloadManagerViewModel injected here only to ensure eager DI creation at startup,
    // so the first flyout open doesn't pay the initialization cost.
    public MainWindowViewModel(NavigationService navigationService, AppSettings appSettings,
        BeatmapDownloadService beatmapDownloadService, DownloadManagerViewModel _)
    {
        _navigationService = navigationService;
        _beatmapDownloadService = beatmapDownloadService;

        _navigationService.Subscribe(NavigationTarget.MainContent, control => Content = control);
        _navigationService.Subscribe(NavigationTarget.DialogContent, control => DialogContent = control);

        _navigationService.SetContent(NavigationTarget.DialogContent, typeof(GreetingsViewModel));

        if (appSettings.SetupCompleted)
        {
            _navigationService.SetContent(NavigationTarget.MainContent, typeof(MainViewModel));
            _navigationService.SetContent(NavigationTarget.MainBlockContent, typeof(MainBlockBeatmapViewModel));
        }
        else
        {
            _navigationService.SetContent(NavigationTarget.MainContent, typeof(SetupViewModel));
        }

        _beatmapDownloadService.ActiveCountChanged += () =>
            Dispatcher.UIThread.Post(() =>
            {
                this.RaisePropertyChanged(nameof(ActiveDownloadCount));
                this.RaisePropertyChanged(nameof(HasActiveDownloads));
            });
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

    public int ActiveDownloadCount => _beatmapDownloadService.GetActiveCount();
    public bool HasActiveDownloads => ActiveDownloadCount > 0;

    public void SwitchChat()
    {
        _navigationService.SwitchChatContent?.Invoke();
    }
}
