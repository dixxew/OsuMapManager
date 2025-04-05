using ReactiveUI;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using MapManager.GUI.Services;
using System.Collections.Generic;
using Avalonia.Media;
using SukiUI.Models;
using SukiUI;
using System.Linq;
using Avalonia.Media.Imaging;
using System.IO;
using static MapManager.GUI.Services.NavigationService;

namespace MapManager.GUI.ViewModels;

public class GreetingsViewModel : ViewModelBase
{
    private readonly NavigationService _navigationService;
    private readonly BeatmapDataService _beatmapDataService;

    public GreetingsViewModel(NavigationService navigationService, BeatmapDataService beatmapDataService)
    {
        _navigationService = navigationService;
        _beatmapDataService = beatmapDataService;
    }

    public void NavigateToMainControl()
    {
        _navigationService.SetContent(NavigationTarget.MainContent, typeof(MainViewModel));
    }
}