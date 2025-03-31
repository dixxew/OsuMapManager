using AutoMapper;
using Avalonia.Controls.Primitives;
using DynamicData;
using MapManager.GUI.Models;
using MapManager.GUI.Services;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MapManager.GUI.ViewModels;

public class GlobalScoresViewModel : ViewModelBase
{
    private readonly RankingService _rankingService;
    private readonly BeatmapDataService _beatmapDataService;
    private readonly IMapper _mapper;

    public GlobalScoresViewModel(BeatmapDataService beatmapDataService, RankingService rankingService, IMapper mapper)
    {
        _beatmapDataService = beatmapDataService;

        _beatmapDataService.OnSelectedBeatmapChanged += OnSelectedBeatmapChanged;
        _rankingService = rankingService;
        _mapper = mapper;
    }

    private bool _isGlobalRankingsLoadingVisible = true;

    public bool IsGlobalRankingsLoadingVisible
    {
        get => _isGlobalRankingsLoadingVisible;
        set => this.RaiseAndSetIfChanged(ref _isGlobalRankingsLoadingVisible, value);
    }

    public ObservableCollection<GlobalScore> GlobalScores { get; set; } = new();

    private void OnSelectedBeatmapChanged()
    {
        this.RaisePropertyChanged(nameof(OnSelectedBeatmapChanged));
        Task.Run(() => LoadScores());
    }

    public async Task LoadScores()
    {
        if (SelectedBeatmap != null)
        {
            var scores = await _rankingService.GetGlobalRanksByBeatmapIdAsync(SelectedBeatmap.BeatmapId);
            GlobalScores.AddRange(scores.Select(s => _mapper.Map<GlobalScore>(s)).ToList());
            IsGlobalRankingsLoadingVisible = false;
        }
    }

    public Beatmap SelectedBeatmap => _beatmapDataService.SelectedBeatmap;
}