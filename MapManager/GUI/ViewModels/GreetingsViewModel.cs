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

    public GreetingsViewModel(NavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public void NavigateToMainControl()
    {
        _navigationService.SetContent(NavigationTarget.MainContent, typeof(MainViewModel));
    }
}