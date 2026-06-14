using Avalonia.Media.Imaging;
using Avalonia.Threading;
using MapManager.GUI.Models;
using MapManager.GUI.Services;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace MapManager.GUI.ViewModels;

public class OsuPpsEntryViewModel : ViewModelBase
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

    // Limit concurrent thumbnail loads to avoid lag when many rows appear at once
    private static readonly SemaphoreSlim _thumbSlot = new(4, 4);

    private static readonly string CdnCacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MapManager", "cdn-thumbs");

    private readonly OsuPpsEntry _entry;
    private readonly ThumbnailService _thumbnailService;
    private readonly BeatmapDownloadService _downloadService;

    public OsuPpsEntryViewModel(OsuPpsEntry entry, ThumbnailService thumbnailService, BeatmapDownloadService downloadService)
    {
        _entry = entry;
        _thumbnailService = thumbnailService;
        _downloadService = downloadService;

        OpenDirectCommand = ReactiveCommand.Create(OpenDirect);
        OpenWebCommand = ReactiveCommand.Create(OpenWeb);
        DownloadCommand = ReactiveCommand.Create(EnqueueDownload);
    }

    public int Rank => _entry.Rank;
    public int BeatmapId => _entry.BeatmapId;
    public int BeatmapSetId => _entry.BeatmapSetId;
    public string Artist => _entry.Artist;
    public string Title => _entry.Title;
    public string Version => _entry.Version;
    public double Pp99 => _entry.Pp99;
    public double StarRating => _entry.StarRating;
    public string StarRatingLabel => $"{_entry.StarRating:F2}★";
    public bool IsLocal => _entry.IsLocal;
    public BeatmapSet? LocalBeatmapSet => _entry.LocalBeatmapSet;
    public double RowOpacity => _entry.IsLocal ? 1.0 : 0.5;

    public string ModsString
    {
        get
        {
            int m = _entry.Mods;
            var s = string.Concat(
                (m & 2) != 0 ? "EZ" : "",
                (m & 8) != 0 ? "HD" : "",
                (m & 16) != 0 ? "HR" : "",
                (m & 64) != 0 ? "DT" : "",
                (m & 256) != 0 ? "HT" : "",
                (m & 1024) != 0 ? "FL" : "");
            return s.Length > 0 ? s : "";
        }
    }

    public bool HasMods => _entry.Mods != 0;

    public string RankLabel => $"#{_entry.Rank}";

    public string PpLabel => $"{_entry.Pp99:0}pp";

    public ReactiveCommand<Unit, Unit> OpenDirectCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenWebCommand { get; }
    public ReactiveCommand<Unit, Unit> DownloadCommand { get; }

    private Bitmap? _thumbnail;
    private bool _thumbnailRequested;

    public Bitmap? Thumbnail
    {
        get
        {
            if (!_thumbnailRequested)
            {
                _thumbnailRequested = true;
                _ = LoadThumbnailAsync();
            }
            return _thumbnail;
        }
        private set => this.RaiseAndSetIfChanged(ref _thumbnail, value);
    }

    private async Task LoadThumbnailAsync()
    {
        await _thumbSlot.WaitAsync().ConfigureAwait(false);
        try
        {
            Bitmap? bmp;

            if (_entry.IsLocal && _entry.LocalBeatmapSet is { Beatmaps: { Count: > 0 } } localSet)
            {
                var b = localSet.Beatmaps[0];
                bmp = await _thumbnailService.GetAsync(b.FolderName ?? localSet.FolderName, b.FileName);
            }
            else
            {
                bmp = await GetCdnThumbnailAsync(_entry.BeatmapSetId);
            }

            if (bmp is not null)
                await Dispatcher.UIThread.InvokeAsync(() => Thumbnail = bmp);
        }
        finally
        {
            _thumbSlot.Release();
        }
    }

    private static async Task<Bitmap?> GetCdnThumbnailAsync(int beatmapSetId)
    {
        try
        {
            Directory.CreateDirectory(CdnCacheDir);
            var cachePath = Path.Combine(CdnCacheDir, $"{beatmapSetId}.jpg");

            byte[] bytes;
            if (File.Exists(cachePath))
            {
                bytes = await File.ReadAllBytesAsync(cachePath);
            }
            else
            {
                bytes = await _http.GetByteArrayAsync($"https://b.ppy.sh/thumb/{beatmapSetId}l.jpg");
                await File.WriteAllBytesAsync(cachePath, bytes);
            }

            return new Bitmap(new MemoryStream(bytes));
        }
        catch { return null; }
    }

    private void OpenDirect() =>
        Process.Start(new ProcessStartInfo($"osu://s/{_entry.BeatmapSetId}") { UseShellExecute = true });

    private void OpenWeb() =>
        Process.Start(new ProcessStartInfo($"https://osu.ppy.sh/beatmapsets/{_entry.BeatmapSetId}") { UseShellExecute = true });

    private void EnqueueDownload() =>
        _downloadService.EnqueueByBeatmapSetId(_entry.BeatmapSetId, _entry.BeatmapId, _entry.Title, _entry.Artist);
}
