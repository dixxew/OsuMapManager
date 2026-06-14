using Avalonia.Threading;
using DynamicData;
using DynamicData.Binding;
using MapManager.GUI.Models.Enums;
using MapManager.GUI.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace MapManager.GUI.ViewModels;

public class DownloadManagerViewModel : ViewModelBase
{
    private readonly BeatmapDownloadService _service;
    private readonly SettingsService _settingsService;
    private readonly ReadOnlyObservableCollection<DownloadEntryViewModel> _sorted;
    private readonly IDisposable _subscription;

    // Sorted view: active downloads first, then waiting, then finished — stable by add time.
    public ReadOnlyObservableCollection<DownloadEntryViewModel> Downloads => _sorted;

    public List<string> AvailableMirrors { get; } = ["catboy.best", "beatconnect.io", "osu.direct"];

    public ReactiveCommand<Unit, Unit> ClearFinishedCommand { get; }

    public DownloadManagerViewModel(BeatmapDownloadService service, SettingsService settingsService)
    {
        _service = service;
        _settingsService = settingsService;

        var comparer = SortExpressionComparer<DownloadEntryViewModel>
            .Ascending(d => StatusRank(d.Status))
            .ThenByAscending(d => d.AddedAt);

        // Re-sort when an item's Status changes; recompute the summary on any change.
        _subscription = _service.Downloads
            .ToObservableChangeSet()
            .AutoRefresh(d => d.Status)
            .Sort(comparer)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _sorted)
            .Subscribe(_ => RecomputeSummary());

        ClearFinishedCommand = ReactiveCommand.Create(ClearFinished);

        _service.ActiveCountChanged += () =>
            Dispatcher.UIThread.Post(() =>
            {
                this.RaisePropertyChanged(nameof(ActiveCount));
                this.RaisePropertyChanged(nameof(HasActiveDownloads));
            });

        RecomputeSummary();
    }

    private static int StatusRank(DownloadStatus s) => s switch
    {
        DownloadStatus.Downloading => 0,
        DownloadStatus.AwaitingLookup => 1,
        DownloadStatus.Queued => 2,
        DownloadStatus.Completed => 3,
        DownloadStatus.Failed => 4,
        DownloadStatus.Cancelled => 5,
        _ => 6
    };

    // ── Badge (used by MainWindow) ──────────────────────────────────────────────
    public int ActiveCount => _service.GetActiveCount();
    public bool HasActiveDownloads => ActiveCount > 0;

    // ── Summary ─────────────────────────────────────────────────────────────────
    private int _totalCount, _completedCount, _failedCount, _downloadingCount, _pendingCount;

    public int TotalCount => _totalCount;
    public int CompletedCount => _completedCount;
    public int FailedCount => _failedCount;
    public int DownloadingCount => _downloadingCount;
    public int PendingCount => _pendingCount;

    public bool HasDownloads => _totalCount > 0;
    public bool HasErrors => _failedCount > 0;
    public bool HasFinished => _service.Downloads.Any(d =>
        d.Status is DownloadStatus.Completed or DownloadStatus.Failed or DownloadStatus.Cancelled);

    public double OverallProgress => _totalCount == 0 ? 0 : (double)_completedCount / _totalCount;

    public string SummaryText
    {
        get
        {
            if (_totalCount == 0) return "";
            var parts = new List<string> { $"Готово {_completedCount} из {_totalCount}" };
            if (_downloadingCount > 0) parts.Add($"качается {_downloadingCount}");
            if (_pendingCount > 0) parts.Add($"в очереди {_pendingCount}");
            return string.Join(" · ", parts);
        }
    }

    public string ErrorsText => _failedCount > 0 ? $"ошибок: {_failedCount}" : "";

    private void RecomputeSummary()
    {
        var all = _service.Downloads;
        _totalCount = all.Count;
        _completedCount = all.Count(d => d.Status == DownloadStatus.Completed);
        _failedCount = all.Count(d => d.Status == DownloadStatus.Failed);
        _downloadingCount = all.Count(d => d.Status == DownloadStatus.Downloading);
        _pendingCount = all.Count(d => d.Status is DownloadStatus.Queued or DownloadStatus.AwaitingLookup);

        this.RaisePropertyChanged(nameof(TotalCount));
        this.RaisePropertyChanged(nameof(CompletedCount));
        this.RaisePropertyChanged(nameof(FailedCount));
        this.RaisePropertyChanged(nameof(DownloadingCount));
        this.RaisePropertyChanged(nameof(PendingCount));
        this.RaisePropertyChanged(nameof(HasDownloads));
        this.RaisePropertyChanged(nameof(HasErrors));
        this.RaisePropertyChanged(nameof(HasFinished));
        this.RaisePropertyChanged(nameof(OverallProgress));
        this.RaisePropertyChanged(nameof(SummaryText));
        this.RaisePropertyChanged(nameof(ErrorsText));
    }

    public string PreferredMirror
    {
        get => _settingsService.PreferredMirror;
        set
        {
            _settingsService.PreferredMirror = value;
            this.RaisePropertyChanged();
        }
    }

    public int MaxConcurrentDownloads
    {
        get => _settingsService.MaxConcurrentDownloads;
        set
        {
            if (value < 1) value = 1;
            if (value > 5) value = 5;
            _settingsService.MaxConcurrentDownloads = value;
            this.RaisePropertyChanged();
        }
    }

    // Removes everything that's done — completed, failed and cancelled.
    private void ClearFinished()
    {
        var finished = _service.Downloads
            .Where(d => d.Status is DownloadStatus.Completed or DownloadStatus.Failed or DownloadStatus.Cancelled)
            .ToList();
        foreach (var d in finished)
            _service.RemoveDownload(d.Id);
    }
}
