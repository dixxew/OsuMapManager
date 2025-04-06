using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DynamicData;
using MapManager.GUI.Models;
using OsuSharp.Domain;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MapManager.GUI.Services;


public class BeatmapService
{
    private readonly OsuDataService OsuDataReader;
    private readonly BeatmapDataService _beatmapDataService;
    private readonly OsuDataService _osuDataService;

    public BeatmapService(OsuDataService osuDataReader, BeatmapDataService beatmapDataService, OsuDataService osuDataService)
    {
        OsuDataReader = osuDataReader;
        _beatmapDataService = beatmapDataService;
        _osuDataService = osuDataService;
    }

    public (Bitmap bitmap, ObservableCollection<Collection> collections) GetBeatmapPresentationData(Models.Beatmap selectedBeatmap)
    {
        return (new Bitmap(OsuDataReader.GetBeatmapImage(selectedBeatmap.FolderName, selectedBeatmap.FileName)),
            new(_beatmapDataService.Collections.Where(c => c.Beatmaps.Contains(selectedBeatmap))));
    }    
}
