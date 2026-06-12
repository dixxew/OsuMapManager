using Avalonia.Threading;
using DynamicData;
using MapManager.GUI.Models;
using MapManager.GUI.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace MapManager.GUI.ViewModels;

public class BeatmapCommentsViewModel : ViewModelBase
{
    private readonly BeatmapDataService _beatmapDataService;
    private readonly OsuApiService _osuApiService;
    private readonly CacheService _cacheService;
    private CancellationTokenSource _cts = new();

    public BeatmapCommentsViewModel(
        BeatmapDataService beatmapDataService,
        OsuApiService osuApiService,
        CacheService cacheService)
    {
        _beatmapDataService = beatmapDataService;
        _osuApiService = osuApiService;
        _cacheService = cacheService;
        _beatmapDataService.OnSelectedBeatmapSetChanged += OnSelectedBeatmapSetChanged;
    }

    public ObservableCollection<BeatmapComment> Comments { get; } = new();

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            this.RaiseAndSetIfChanged(ref _isLoading, value);
            this.RaisePropertyChanged(nameof(IsEmpty));
        }
    }

    private bool _hasMore;
    public bool HasMore
    {
        get => _hasMore;
        set => this.RaiseAndSetIfChanged(ref _hasMore, value);
    }

    public bool IsEmpty => !IsLoading && Comments.Count == 0;

    private void OnSelectedBeatmapSetChanged() =>
        Task.Run(LoadCommentsAsync);

    private async Task LoadCommentsAsync()
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        var beatmapSet = _beatmapDataService.SelectedBeatmapSet;
        if (beatmapSet == null) return;
        var setId = beatmapSet.Id;

        Dispatcher.UIThread.Post(() =>
        {
            IsLoading = true;
            Comments.Clear();
            this.RaisePropertyChanged(nameof(IsEmpty));
        });

        try
        {
            var entries = await _cacheService.GetJsonAsync<List<BeatmapCommentCacheEntry>>(
                $"comments_{setId}",
                () => _osuApiService.GetBeatmapsetCommentsAsync(setId));

            if (token.IsCancellationRequested) return;

            var comments = entries?.ConvertAll(e => new BeatmapComment
            {
                Id = e.Id,
                ParentId = e.ParentId,
                Message = e.Message,
                CreatedAt = e.CreatedAt,
                VotesCount = e.VotesCount,
                RepliesCount = e.RepliesCount,
                Pinned = e.Pinned,
                User = string.IsNullOrEmpty(e.Username) ? null : new GlobalUserCompact
                {
                    Id = e.UserId ?? 0,
                    Username = e.Username,
                    AvatarUrl = string.IsNullOrEmpty(e.AvatarUrl)
                        ? null!
                        : new Uri(e.AvatarUrl),
                },
            }) ?? [];

            Dispatcher.UIThread.Post(() =>
            {
                Comments.Clear();
                Comments.AddRange(comments);
                IsLoading = false;
                this.RaisePropertyChanged(nameof(IsEmpty));
            });
        }
        catch (OperationCanceledException) { }
        catch
        {
            Dispatcher.UIThread.Post(() =>
            {
                IsLoading = false;
                this.RaisePropertyChanged(nameof(IsEmpty));
            });
        }
    }
}
