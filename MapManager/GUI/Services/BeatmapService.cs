using Avalonia.Media.Imaging;
using MapManager.GUI.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace MapManager.GUI.Services;


public class BeatmapService
{
    private readonly OsuDataReader OsuDataReader;
    private readonly BeatmapDataService _beatmapDataService;

    public BeatmapService(OsuDataReader osuDataReader, BeatmapDataService beatmapDataService)
    {
        OsuDataReader = osuDataReader;
        _beatmapDataService = beatmapDataService;
    }

    internal (Bitmap bitmap, ObservableCollection<Collection> collections) GetBeatmapPresentationData(Beatmap selectedBeatmap)
    {
        return (new Bitmap(OsuDataReader.GetBeatmapImage(selectedBeatmap.FolderName, selectedBeatmap.FileName)),
            new(_beatmapDataService.Collections.Where(c => c.Beatmaps.Contains(selectedBeatmap))));
    }
}
