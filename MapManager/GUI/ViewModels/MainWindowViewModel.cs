using AutoMapper;
using MapManager.GUI.Models;
using MapManager.GUI.Services;
using MapManager.OSU;
using ReactiveUI;
using System.Collections.ObjectModel;

namespace MapManager.GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly AppInitializationService _appInitializationService;
    private readonly AuxiliaryService _auxiliaryService;

    public MainWindowViewModel(AppInitializationService appInitializationService, AuxiliaryService auxiliaryService)
    {
        _appInitializationService = appInitializationService;
        _auxiliaryService = auxiliaryService;
    }

    #region private props
    private int _selectedRightTabIndex;
    private bool _isGlobalRankingsLoadingVisible;
    private bool _isLocalBeatmapsVisible;
    private bool _isGlobalBeatmapsVisible;
    #endregion


    #region public props
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



    public int SelectedRightTabIndex
    {
        get => _selectedRightTabIndex;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedRightTabIndex, value);
            UpdateRightTab();
        }
    }

    #endregion

    private async void UpdateRightTab()
    {
        switch (SelectedRightTabIndex)
        {
            case 0: //LocalRanks
                break;
            case 1: //GlobalRanks

                break;
            case 2: //MapComments

                break;
        }
    }

}
