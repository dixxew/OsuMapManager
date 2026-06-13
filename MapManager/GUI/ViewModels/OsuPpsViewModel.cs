using MapManager.GUI.Models;
using MapManager.GUI.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MapManager.GUI.ViewModels;

public class OsuPpsViewModel : ViewModelBase
{
    private readonly OsuPpsService _osuPpsService;
    private readonly ThumbnailService _thumbnailService;
    private readonly BeatmapDataService _beatmapDataService;
    private readonly OsuPpsFiltersViewModel _filters;

    private const int PageSize = 50;
    private int _loadedCount;
    private bool _isLoading;
    private bool _initialized;

    // Current filtered+sorted slice of all entries
    private IReadOnlyList<OsuPpsEntry> _filteredEntries = [];

    public OsuPpsViewModel(
        OsuPpsService osuPpsService,
        ThumbnailService thumbnailService,
        BeatmapDataService beatmapDataService,
        OsuPpsFiltersViewModel filters)
    {
        _osuPpsService = osuPpsService;
        _thumbnailService = thumbnailService;
        _beatmapDataService = beatmapDataService;
        _filters = filters;

        _filters.FiltersChanged += ApplyFilters;
    }

    public ObservableCollection<OsuPpsEntryViewModel> Items { get; } = [];

    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        private set => this.RaiseAndSetIfChanged(ref _totalCount, value);
    }

    public bool HasMore => _loadedCount < _filteredEntries.Count;

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        IsLoading = true;
        try
        {
            await _osuPpsService.LoadAsync();
            ApplyFilters();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        _filteredEntries = _osuPpsService.RankedEntries
            .Where(_filters.Passes)
            .ToList();

        TotalCount = _filteredEntries.Count;
        Items.Clear();
        _loadedCount = 0;
        LoadNextPage();
    }

    public void LoadNextPage()
    {
        int end = Math.Min(_loadedCount + PageSize, _filteredEntries.Count);
        for (int i = _loadedCount; i < end; i++)
            Items.Add(new OsuPpsEntryViewModel(_filteredEntries[i], _thumbnailService));
        _loadedCount = end;
    }

    public void SelectLocal(OsuPpsEntryViewModel entry)
    {
        if (!entry.IsLocal || entry.LocalBeatmapSet is null) return;
        // Find the specific diff by BeatmapId, not just the first one in the set
        var beatmap = entry.LocalBeatmapSet.Beatmaps
            .FirstOrDefault(b => b.BeatmapId == entry.BeatmapId);
        _beatmapDataService.SelectBeatmapSetAndBeatmap(entry.LocalBeatmapSet, beatmap);
    }
}
