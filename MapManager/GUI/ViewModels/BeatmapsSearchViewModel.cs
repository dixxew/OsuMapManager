using MapManager.GUI.Models.Enums;
using MapManager.GUI.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapManager.GUI.ViewModels;
public class BeatmapsSearchViewModel : ViewModelBase
{
    private readonly BeatmapDataService _beatmapDataService;

    public BeatmapsSearchViewModel(BeatmapDataService beatmapDataService)
    {
        _beatmapDataService = beatmapDataService;
    }

    private BeatmapsSearchModeEnum _searchMode;
    public BeatmapsSearchModeEnum SearchMode
    {
        get => _beatmapDataService.SearchMode;
        set
        {
            _beatmapDataService.SearchMode = value;
            this.RaisePropertyChanged();
        }
    }


    public string SearchBeatmapSetText
    {
        get => _beatmapDataService.QueryText;
        set
        {
            _beatmapDataService.QueryText = value;
            this.RaisePropertyChanged();
        }
    }

    public bool IsShowOnlyFavorites
    {
        get => _beatmapDataService.IsOnlyFavorite;
        set
        {
            _beatmapDataService.IsOnlyFavorite = value;
            this.RaisePropertyChanged();
        }
    }

    public long BeatmapsCount { get; set; } = 0;
}
