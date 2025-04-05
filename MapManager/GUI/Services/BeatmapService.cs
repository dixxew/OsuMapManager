using Avalonia.Media.Imaging;
using MapManager.GUI.Models;
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

    public (Bitmap bitmap, ObservableCollection<Collection> collections) GetBeatmapPresentationData(Beatmap selectedBeatmap)
    {
        return (new Bitmap(OsuDataReader.GetBeatmapImage(selectedBeatmap.FolderName, selectedBeatmap.FileName)),
            new(_beatmapDataService.Collections.Where(c => c.Beatmaps.Contains(selectedBeatmap))));
    }

    public void AddToCollection(string collectionName, Beatmap beatmap)
    {
        if (!_beatmapDataService.Collections.First(c => c.Name == collectionName).Beatmaps.Contains(beatmap))
        {
            _beatmapDataService.Collections.First(c => c.Name == collectionName).Beatmaps.Add(beatmap);
            _osuDataService.AddToCollection(collectionName, beatmap.MD5Hash);
        }
    }

    public void RemoveFromCollection(string collectionName, Beatmap beatmap)
    {
        if (_beatmapDataService.Collections.First(c => c.Name == collectionName).Beatmaps.Contains(beatmap))
        {
            _beatmapDataService.Collections.First(c => c.Name == collectionName).Beatmaps.Remove(beatmap);
            _osuDataService.RemoveFromCollection(collectionName, beatmap.MD5Hash);
        }
    }

    public void AddCollection(string name, List<string> md5hashes)
    {
        _osuDataService.AddCollection(name, md5hashes);
    }
}
