using Avalonia.Media.Imaging;
using MapManager.GUI.Models;
using MapManager.GUI.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapManager.GUI.ViewModels;
public class BeatmapInfoViewModel : ViewModelBase
{

    private readonly BeatmapDataService _beatmapDataService;
    private readonly BeatmapService _beatmapService;
    private readonly AuxiliaryService _auxiliaryService;

    public BeatmapInfoViewModel(BeatmapDataService beatmapDataService, BeatmapService beatmapService, AuxiliaryService auxiliaryService)
    {
        _beatmapDataService = beatmapDataService;
        _beatmapService = beatmapService;


        _beatmapDataService.OnSelectedBeatmapSetChanged += OnSelectedBeatmapSetChanged;
        _beatmapDataService.OnSelectedBeatmapChanged += OnSelectedBeatmapChanged;
        _auxiliaryService = auxiliaryService;
    }



    public BeatmapSet SelectedBeatmapSet => _beatmapDataService.SelectedBeatmapSet;
    public Beatmap SelectedBeatmap
    {
        get => _beatmapDataService.SelectedBeatmap;
        set
        {
            if (value is null)
                return;
            _beatmapDataService.SelectedBeatmap = value;
        }
    }

    private Bitmap _mapBackground;
    public Bitmap MapBackground
    {
        get => _mapBackground;
        set => this.RaiseAndSetIfChanged(ref _mapBackground, value);
    }

    public string SelectedBeatmapCollectionsCount
        => $"Collections {SelectedBeatmapCollections?.Count ?? 0}";

    private ObservableCollection<Models.Collection> _selectedBeatmapCollections;
    public ObservableCollection<Models.Collection> SelectedBeatmapCollections
    {
        get => _selectedBeatmapCollections;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedBeatmapCollections, value);
            _selectedBeatmapCollections.CollectionChanged += OnBeatmapSelectedCollectionsChanges;
            if (value != null)
                this.RaisePropertyChanged(nameof(SelectedBeatmapCollectionsCount));
        }
    }


    public void OpenBeatmapInOsu()
    {
        _auxiliaryService.OpenBeatmapInOsu(SelectedBeatmap.BeatmapId);
    }
    public void LookSimilar()
    {
        _beatmapDataService.SearchMode = Models.Enums.BeatmapsSearchModeEnum.SAME;
        _beatmapDataService.Search();
    }



    private void SelectedBeatmapSetChanged()
    {
        this.RaisePropertyChanged(nameof(SelectedBeatmapSet));

    }
    private void SelectedBeatmapChanged()
    {
        this.RaisePropertyChanged(nameof(SelectedBeatmap));

        var beatmapData = _beatmapService.GetBeatmapPresentationData(SelectedBeatmap);
        MapBackground = beatmapData.bitmap;
        SelectedBeatmapCollections = beatmapData.collections;

    }


    private void OnBeatmapSelectedCollectionsChanges(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                //Collections.First(c => c == e.NewItems[0] as Models.Collection).Beatmaps.Add(SelectedBeatmap);
                this.RaisePropertyChanged(nameof(SelectedBeatmapCollectionsCount));
                break;
            case NotifyCollectionChangedAction.Remove:
                //Collections.First(c => c == e.OldItems[0] as Models.Collection).Beatmaps.Remove(SelectedBeatmap);
                this.RaisePropertyChanged(nameof(SelectedBeatmapCollectionsCount));
                break;
        }
    }
    private void OnSelectedBeatmapSetChanged()
        => SelectedBeatmapSetChanged();

    private void OnSelectedBeatmapChanged()
        => SelectedBeatmapChanged();

}
