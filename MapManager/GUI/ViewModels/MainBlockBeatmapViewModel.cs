using Avalonia.Controls;
using Avalonia.Threading;
using MapManager.GUI.Models;
using MapManager.GUI.Services;
using NAudio.Wave;
using ReactiveUI;
using SoundTouch.Net.NAudioSupport;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Timers;

namespace MapManager.GUI.ViewModels;

public class MainBlockBeatmapViewModel : ViewModelBase
{

    public MainBlockBeatmapViewModel()
    {
    }

    private int _selectedRightTabIndex;
    public int SelectedRightTabIndex
    {
        get => _selectedRightTabIndex;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedRightTabIndex, value);
            UpdateRightTab();
        }
    }

    private async void UpdateRightTab()
    {
    }

}