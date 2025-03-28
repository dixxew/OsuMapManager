﻿using AutoMapper;
using MapManager.GUI.Models;
using MapManager.GUI.Services;
using MapManager.OSU;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapManager.GUI.ViewModels;
public class BeatmapsViewModel : ViewModelBase
{
    private readonly BeatmapDataService _beatmapDataService;

    public BeatmapsViewModel(BeatmapDataService beatmapDataService)
    {
        _beatmapDataService = beatmapDataService;
        _beatmapDataService.OnSelectedBeatmapSetChanged += OnSelectedBeatmapSetChanged;
    }


    public BeatmapSet SelectedBeatmapSet => _beatmapDataService.SelectedBeatmapSet;

    public ObservableCollection<BeatmapSet> FilteredBeatmapSets => _beatmapDataService.FilteredBeatmapSets;

    public void update()
    {
        this.RaisePropertyChanged(nameof(FilteredBeatmapSets));
    }

    private void OnSelectedBeatmapSetChanged()
        => this.RaisePropertyChanged(nameof(SelectedBeatmapSet));
}
