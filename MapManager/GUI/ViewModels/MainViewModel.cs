using Avalonia.Controls;
using MapManager.GUI.Services;
using ReactiveUI;
using static MapManager.GUI.Services.NavigationService;

namespace MapManager.GUI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly AppInitializationService _appInitializationService;
    private readonly AuxiliaryService _auxiliaryService;
    private readonly NavigationService _navigationService;

    public MainViewModel(NavigationService navigationService, AppInitializationService appInitializationService, AuxiliaryService auxiliaryService)
    {
        _navigationService = navigationService;
        _appInitializationService = appInitializationService;
        _auxiliaryService = auxiliaryService;

        _navigationService.Subscribe(NavigationTarget.MainBlockContent, control =>
        {
            MainBlockContent = control;
        });

        _navigationService.SwitchChatContent += () =>
        {
            if (MainBlockContent.DataContext is ChatViewModel)
                _navigationService.SetContent(NavigationTarget.MainBlockContent, typeof(MainBlockBeatmapViewModel));
            else
                _navigationService.SetContent(NavigationTarget.MainBlockContent, typeof(ChatViewModel));
        };
    }

    #region private props
    private bool _isGlobalRankingsLoadingVisible;
    private bool _isLocalBeatmapsVisible;
    private bool _isGlobalBeatmapsVisible;
    private UserControl _mainBlockContent;
    #endregion



    #region public props
    public UserControl MainBlockContent
    {
        get => _mainBlockContent;
        set => this.RaiseAndSetIfChanged(ref _mainBlockContent, value);
    }
    public bool IsGlobalBeatmapsVisible
    {
        get => _isGlobalBeatmapsVisible;
        set => this.RaiseAndSetIfChanged(ref _isGlobalBeatmapsVisible, value);
    }

    public bool IsLocalBeatmapsVisible
    {
        get => _isLocalBeatmapsVisible;
        set => this.RaiseAndSetIfChanged(ref _isLocalBeatmapsVisible, value);
    }


    public bool IsGlobalRankingsLoadingVisible
    {
        get => _isGlobalRankingsLoadingVisible;
        set => this.RaiseAndSetIfChanged(ref _isGlobalRankingsLoadingVisible, value);
    }




    #endregion

}
