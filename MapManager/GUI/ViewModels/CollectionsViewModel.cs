using MapManager.GUI.Models;
using MapManager.GUI.Services;
using System.Collections.ObjectModel;

namespace MapManager.GUI.ViewModels;

public class CollectionsViewModel : ViewModelBase
{
    private readonly BeatmapDataService _beatmapDataService;

    public CollectionsViewModel(BeatmapDataService beatmapDataService)
    {
        _beatmapDataService = beatmapDataService;
    }

    public ObservableCollection<Collection> Collections => _beatmapDataService.Collections;

    public Beatmap SelectedTreeViewCollection 
    {
    get => _beatmapDataService.SelectedBeatmap;
    set {
            _beatmapDataService.SelectBeatmapAndFindBeatmapSet(value);

        }
    }
    }