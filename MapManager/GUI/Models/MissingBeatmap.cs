using ReactiveUI;

namespace MapManager.GUI.Models;

public class MissingBeatmap : ReactiveObject
{
    public string MD5Hash { get; init; } = "";

    private int? _beatmapSetId;
    public int? BeatmapSetId
    {
        get => _beatmapSetId;
        set => this.RaiseAndSetIfChanged(ref _beatmapSetId, value);
    }

    private int? _beatmapId;
    public int? BeatmapId
    {
        get => _beatmapId;
        set => this.RaiseAndSetIfChanged(ref _beatmapId, value);
    }

    private string? _title;
    public string? Title
    {
        get => _title;
        set
        {
            this.RaiseAndSetIfChanged(ref _title, value);
            this.RaisePropertyChanged(nameof(DisplayName));
        }
    }

    private string? _artist;
    public string? Artist
    {
        get => _artist;
        set
        {
            this.RaiseAndSetIfChanged(ref _artist, value);
            this.RaisePropertyChanged(nameof(DisplayName));
        }
    }

    private bool _isResolved;
    public bool IsResolved
    {
        get => _isResolved;
        set => this.RaiseAndSetIfChanged(ref _isResolved, value);
    }

    public string DisplayName => (Title != null && Artist != null)
        ? $"{Artist} - {Title}"
        : $"Неизвестная карта ({(MD5Hash.Length >= 8 ? MD5Hash[..8] : MD5Hash)}...)";
}
