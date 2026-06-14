using MapManager.GUI.Models.Enums;
using MapManager.GUI.Services;
using ReactiveUI;
using System;
using System.Reactive;
using System.Threading;

namespace MapManager.GUI.ViewModels;

public class DownloadEntryViewModel : ViewModelBase
{
    private readonly BeatmapDownloadService _service;

    public Guid Id { get; }
    public DateTime AddedAt { get; internal init; }
    public DateTime? CompletedAt { get; set; }

    // MD5 hash for AwaitingLookup entries; null for directly enqueued downloads
    public string? MD5Hash { get; internal set; }

    internal CancellationTokenSource? CancellationTokenSource { get; set; }

    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> RetryCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveCommand { get; }

    public DownloadEntryViewModel(Guid id, int beatmapSetId, string title, string artist, BeatmapDownloadService service)
    {
        Id = id;
        _beatmapSetId = beatmapSetId;
        _title = title;
        _artist = artist;
        _status = DownloadStatus.Queued; // explicit: AwaitingLookup=0 is the C# default
        AddedAt = DateTime.UtcNow;
        _service = service;

        CancelCommand = ReactiveCommand.Create(() => _service.CancelDownload(Id));
        RetryCommand = ReactiveCommand.Create(() => _service.RetryDownload(Id));
        RemoveCommand = ReactiveCommand.Create(() => _service.RemoveDownload(Id));
    }

    private int _beatmapSetId;
    public int BeatmapSetId
    {
        get => _beatmapSetId;
        internal set => this.RaiseAndSetIfChanged(ref _beatmapSetId, value);
    }

    private string _title;
    public string Title
    {
        get => _title;
        internal set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    private string _artist;
    public string Artist
    {
        get => _artist;
        internal set => this.RaiseAndSetIfChanged(ref _artist, value);
    }

    private DownloadStatus _status;
    public DownloadStatus Status
    {
        get => _status;
        set
        {
            this.RaiseAndSetIfChanged(ref _status, value);
            this.RaisePropertyChanged(nameof(IsActive));
            this.RaisePropertyChanged(nameof(IsAwaitingLookup));
            this.RaisePropertyChanged(nameof(ShowProgress));
            this.RaisePropertyChanged(nameof(CanCancel));
            this.RaisePropertyChanged(nameof(CanRetry));
            this.RaisePropertyChanged(nameof(CanRemove));
            this.RaisePropertyChanged(nameof(StatusLabel));
        }
    }

    private double _progress;
    public double Progress
    {
        get => _progress;
        set
        {
            this.RaiseAndSetIfChanged(ref _progress, value);
            this.RaisePropertyChanged(nameof(StatusLabel));
        }
    }

    private string? _error;
    public string? Error
    {
        get => _error;
        set => this.RaiseAndSetIfChanged(ref _error, value);
    }

    public bool IsActive => _status == DownloadStatus.Downloading;
    public bool IsAwaitingLookup => _status == DownloadStatus.AwaitingLookup;
    // Show the progress bar while downloading (determinate) or resolving an MD5 (indeterminate).
    public bool ShowProgress => _status is DownloadStatus.Downloading or DownloadStatus.AwaitingLookup;
    public bool CanCancel => _status is DownloadStatus.AwaitingLookup or DownloadStatus.Queued or DownloadStatus.Downloading;
    public bool CanRetry => _status is DownloadStatus.Failed or DownloadStatus.Cancelled;
    // Terminal entries can be dismissed from the list (incl. errored ones).
    public bool CanRemove => _status is DownloadStatus.Completed or DownloadStatus.Failed or DownloadStatus.Cancelled;

    public string StatusLabel => _status switch
    {
        DownloadStatus.AwaitingLookup => "Определяется...",
        DownloadStatus.Queued => "В очереди",
        DownloadStatus.Downloading => $"{_progress * 100:0}%",
        DownloadStatus.Completed => "Готово",
        DownloadStatus.Failed => "Ошибка",
        DownloadStatus.Cancelled => "Отменено",
        _ => ""
    };
}
