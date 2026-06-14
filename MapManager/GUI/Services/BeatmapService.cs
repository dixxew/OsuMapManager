using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DynamicData;
using MapManager.GUI.Models;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<BeatmapService> _logger;

    public BeatmapService(OsuDataService osuDataReader, BeatmapDataService beatmapDataService, OsuDataService osuDataService,
        ILogger<BeatmapService> logger)
    {
        OsuDataReader = osuDataReader;
        _beatmapDataService = beatmapDataService;
        _osuDataService = osuDataService;
        _logger = logger;
    }

    public (Bitmap bitmap, ObservableCollection<Collection> collections) GetBeatmapPresentationData(Models.Beatmap selectedBeatmap)
    {
        _logger.LogDebug("GetBeatmapPresentationData(beatmapId={Id}, folder='{Folder}')",
            selectedBeatmap.BeatmapId, selectedBeatmap.FolderName);
        var bitmap = new Bitmap(OsuDataReader.GetBeatmapImage(selectedBeatmap.FolderName, selectedBeatmap.FileName));
        var collections = new ObservableCollection<Collection>(
            _beatmapDataService.Collections.Where(c => c.Beatmaps.Contains(selectedBeatmap)));
        return (bitmap, collections);
    }
}
