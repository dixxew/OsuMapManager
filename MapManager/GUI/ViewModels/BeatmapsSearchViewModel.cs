using MapManager.GUI.Models;
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
        _beatmapDataService.OnFiltesChanged += OnFiltesChanged;
        _beatmapDataService.OnReferenceBeatmapChanged += OnReferenceBeatmapChanged;
    }

    private BeatmapsSearchModeEnum _searchMode;



    public Beatmap? ReferenceBeatmap => _beatmapDataService.ReferenceBeatmap;
    public bool IsReferenceSearhcing => _beatmapDataService.ReferenceBeatmap is not null;


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

    public long BeatmapsCount => _beatmapDataService.FilteredBeatmapSets.SelectMany(bs => bs.Beatmaps).Count();




    private void OnFiltesChanged()
    {
        this.RaisePropertyChanged(nameof(BeatmapsCount));
    }
    private void OnReferenceBeatmapChanged()
    {
        this.RaisePropertyChanged(nameof(ReferenceBeatmap));
        this.RaisePropertyChanged(nameof(IsReferenceSearhcing));
        this.RaisePropertyChanged(nameof(SearchMode));
    }
    
}
