using MapManager.GUI.Models;
using MapManager.GUI.Services;
using System.Collections.ObjectModel;

namespace MapManager.GUI.ViewModels;

public class CollectionsViewModel : ViewModelBase
{
    private readonly BeatmapDataService _beatmapDataService;
    private readonly BeatmapService _beatmapService;

    public CollectionsViewModel(BeatmapDataService beatmapDataService, BeatmapService beatmapService)
    {
        _beatmapDataService = beatmapDataService;
        _beatmapService = beatmapService;
    }

    public ObservableCollection<Collection> Collections => _beatmapDataService.Collections;

    public Beatmap SelectedTreeViewCollection
    {
        get => _beatmapDataService.SelectedBeatmap;
        set
        {
            if (value is null)
                return;
            _beatmapDataService.SelectBeatmapAndFindBeatmapSet(value);
        }
    }

    public void RemoveFromCollection(Beatmap beatmap, Collection collection)
    {
        _beatmapService.RemoveFromCollection(collection.Name, beatmap);
    }

}