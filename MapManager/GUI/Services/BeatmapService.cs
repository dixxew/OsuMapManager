using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using MapManager.GUI.Models;
using MapManager.OSU;

namespace MapManager.GUI.Services;


public class BeatmapService
{
    private readonly OsuDataReader OsuDataReader;
    private readonly BeatmapDataService _beatmapDataService;
    internal (Bitmap bitmap, ObservableCollection<Collection> collections) GetBeatmapPresentationData(Beatmap selectedBeatmap)
    {
        return (new Bitmap(OsuDataReader.GetBeatmapImage(selectedBeatmap.FolderName, selectedBeatmap.FileName)),
            new(_beatmapDataService.Collections.Where(c => c.Beatmaps.Contains(selectedBeatmap))));
    }
}
