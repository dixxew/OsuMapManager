using Avalonia.Media.Imaging;
using MapManager.GUI.Models;
using MapManager.GUI.Models.Enums;
using MapManager.GUI.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace MapManager.GUI.ViewModels;

public class BeatmapInfoViewModel : ViewModelBase
{
    private readonly BeatmapDataService _beatmapDataService;
    private readonly BeatmapService _beatmapService;
    private readonly AuxiliaryService _auxiliaryService;
    private readonly SearchFiltersViewModel _searchFiltersViewModel;

    public BeatmapInfoViewModel(
        BeatmapDataService beatmapDataService,
        BeatmapService beatmapService,
        AuxiliaryService auxiliaryService,
        SearchFiltersViewModel searchFiltersViewModel)
    {
        _beatmapDataService = beatmapDataService;
        _beatmapService = beatmapService;
        _auxiliaryService = auxiliaryService;
        _searchFiltersViewModel = searchFiltersViewModel;

        _beatmapDataService.OnSelectedBeatmapSetChanged += OnSelectedBeatmapSetChanged;
        _beatmapDataService.OnSelectedBeatmapChanged += OnSelectedBeatmapChanged;

        _searchFiltersViewModel.WhenAnyValue(x => x.TagList)
            .Skip(1)
            .Subscribe(_ => RefreshTagActiveStates());
    }

    public BeatmapSet SelectedBeatmapSet => _beatmapDataService.SelectedBeatmapSet;
    public Beatmap SelectedBeatmap
    {
        get => _beatmapDataService.SelectedBeatmap;
        set
        {
            if (value is null) return;
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
            _selectedBeatmapCollections.CollectionChanged += OnBeatmapSelectedCollectionsChanged;
            if (value != null)
                this.RaisePropertyChanged(nameof(SelectedBeatmapCollectionsCount));
        }
    }

    public ObservableCollection<BeatmapTagViewModel> Tags { get; } = new();

    public void OpenBeatmapInOsu() =>
        _auxiliaryService.OpenBeatmapInOsu(SelectedBeatmap.BeatmapId);

    public void LookSimilar()
    {
        _beatmapDataService.SearchMode = BeatmapsSearchModeEnum.SAME;
        _beatmapDataService.Search();
    }

    private void RefreshTags()
    {
        Tags.Clear();
        var tagsList = SelectedBeatmap?.TagsList;
        if (tagsList == null) return;
        foreach (var tag in tagsList)
            Tags.Add(new BeatmapTagViewModel(tag, _searchFiltersViewModel.HasTag(tag), ToggleTag));
    }

    private void RefreshTagActiveStates()
    {
        foreach (var tagVm in Tags)
            tagVm.IsActive = _searchFiltersViewModel.HasTag(tagVm.Tag);
    }

    private void ToggleTag(BeatmapTagViewModel tagVm)
    {
        if (tagVm.IsActive)
        {
            _searchFiltersViewModel.RemoveTag(tagVm.Tag);
            tagVm.IsActive = false;
        }
        else
        {
            _beatmapDataService.SearchMode = BeatmapsSearchModeEnum.FILTERS;
            _searchFiltersViewModel.AddTag(tagVm.Tag);
            tagVm.IsActive = true;
        }
    }

    private void SelectedBeatmapSetChanged()
    {
        this.RaisePropertyChanged(nameof(SelectedBeatmapSet));
        this.RaisePropertyChanged(nameof(SelectedBeatmapCollectionsCount));
    }

    private void SelectedBeatmapChanged()
    {
        this.RaisePropertyChanged(nameof(SelectedBeatmap));

        var oldBackground = _mapBackground;
        var beatmapData = _beatmapService.GetBeatmapPresentationData(SelectedBeatmap);
        MapBackground = beatmapData.bitmap;
        oldBackground?.Dispose();

        SelectedBeatmapCollections = beatmapData.collections;
        this.RaisePropertyChanged(nameof(SelectedBeatmapCollectionsCount));

        RefreshTags();
    }

    private void OnBeatmapSelectedCollectionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
            case NotifyCollectionChangedAction.Remove:
                this.RaisePropertyChanged(nameof(SelectedBeatmapCollectionsCount));
                break;
        }
    }

    private void OnSelectedBeatmapSetChanged() => SelectedBeatmapSetChanged();
    private void OnSelectedBeatmapChanged() => SelectedBeatmapChanged();
}

public class BeatmapTagViewModel : ReactiveObject
{
    private readonly Action<BeatmapTagViewModel> _toggle;

    public string Tag { get; }

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set => this.RaiseAndSetIfChanged(ref _isActive, value);
    }

    public ReactiveCommand<Unit, Unit> ToggleCommand { get; }

    public BeatmapTagViewModel(string tag, bool isActive, Action<BeatmapTagViewModel> toggle)
    {
        Tag = tag;
        _isActive = isActive;
        _toggle = toggle;
        ToggleCommand = ReactiveCommand.Create(() => _toggle(this));
    }
}
