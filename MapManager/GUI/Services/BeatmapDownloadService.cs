using Avalonia.Threading;
using MapManager.GUI.Models;
using MapManager.GUI.Models.Enums;
using MapManager.GUI.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;

public class BeatmapDownloadService : IDisposable
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MapManager");
    private static readonly string QueuePath = Path.Combine(DataDir, "downloads.json");
    private static readonly string LookupCachePath = Path.Combine(DataDir, "md5-lookup-cache.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(10) };

    private readonly OsuApiService _osuApiService;
    private readonly SettingsService _settingsService;
    private readonly ILogger<BeatmapDownloadService> _logger;
    private readonly CancellationTokenSource _cts = new();

    // Serializes file writes so concurrent fire-and-forget saves don't collide on the same path.
    private readonly SemaphoreSlim _queueSaveLock = new(1, 1);
    private readonly SemaphoreSlim _cacheSaveLock = new(1, 1);

    // Wakes the download scheduler; coalesced so idle costs nothing (no 500ms polling).
    private readonly SemaphoreSlim _kick = new(0, int.MaxValue);

    // Guards _lookupCache and _md5PendingDownloads (both touched from UI + background threads).
    private readonly object _lookupSync = new();

    // MD5 → resolved info cache, persisted to file
    private Dictionary<string, LookupCacheEntry> _lookupCache = new();

    // Missing beatmaps waiting for MD5 → BeatmapSetId resolution (+ per-item backoff timestamps)
    private readonly List<MissingBeatmap> _pendingLookups = new();
    private readonly Dictionary<MissingBeatmap, DateTime> _nextAttempt = new();

    // MD5 → download entry Id; populated by EnqueueByMd5 so lookup loop can promote entries
    private readonly Dictionary<string, Guid> _md5PendingDownloads = new();

    private int _activeDownloads;

    public ObservableCollection<DownloadEntryViewModel> Downloads { get; } = new();

    public event Action? ActiveCountChanged;

    public BeatmapDownloadService(OsuApiService osuApiService, SettingsService settingsService,
        ILogger<BeatmapDownloadService> logger)
    {
        _osuApiService = osuApiService;
        _settingsService = settingsService;
        _logger = logger;

        Directory.CreateDirectory(DataDir);
        LoadLookupCache();
        LoadDownloadQueue();

        _ = ProcessDownloadQueueAsync(_cts.Token);
        _ = ProcessLookupQueueAsync(_cts.Token);

        Kick(); // pick up any Queued entries restored from disk
        _logger.LogInformation("BeatmapDownloadService initialized ({Queued} entries restored, {Cached} cached lookups)",
            Downloads.Count, _lookupCache.Count);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void EnqueueDownload(int beatmapSetId, string title, string artist)
    {
        if (beatmapSetId <= 0) return;

        var vm = new DownloadEntryViewModel(Guid.NewGuid(), beatmapSetId, title, artist, this);
        // Dedup check + Add run together on the UI thread, so serialized posts can't race
        // and create duplicates of the same set id.
        Dispatcher.UIThread.Post(() =>
        {
            if (IsAlreadyQueued(beatmapSetId))
            {
                _logger.LogDebug("EnqueueDownload: set {SetId} already queued, skipping", beatmapSetId);
                return;
            }
            Downloads.Add(vm);
            _ = SaveQueueAsync();
            Kick();
            _logger.LogInformation("Enqueued download: set {SetId} '{Artist} - {Title}'", beatmapSetId, artist, title);
        });
    }

    // Enqueue by BeatmapSetId (from osu!pps). Download starts immediately; if title/artist
    // are empty (not in mapsets.csv), metadata is fetched in background via osu! API.
    public void EnqueueByBeatmapSetId(int beatmapSetId, int beatmapId, string title, string artist)
    {
        if (beatmapSetId <= 0) return;

        var displayTitle = !string.IsNullOrEmpty(title) ? title : $"Beatmapset #{beatmapSetId}";
        var vm = new DownloadEntryViewModel(Guid.NewGuid(), beatmapSetId, displayTitle, artist, this);
        Dispatcher.UIThread.Post(() =>
        {
            if (IsAlreadyQueued(beatmapSetId))
            {
                _logger.LogDebug("EnqueueByBeatmapSetId: set {SetId} already queued, skipping", beatmapSetId);
                return;
            }
            Downloads.Add(vm);
            _ = SaveQueueAsync();
            Kick();
            _logger.LogInformation("Enqueued download by setId {SetId} (beatmapId {BeatmapId})", beatmapSetId, beatmapId);

            if (string.IsNullOrEmpty(title))
                _ = FetchAndUpdateMetaAsync(vm, beatmapId);
        });
    }

    // Must be called on the UI thread.
    private bool IsAlreadyQueued(int beatmapSetId) =>
        Downloads.Any(d => d.BeatmapSetId == beatmapSetId &&
            d.Status != DownloadStatus.Cancelled && d.Status != DownloadStatus.Failed);

    private async Task FetchAndUpdateMetaAsync(DownloadEntryViewModel vm, int beatmapId)
    {
        try
        {
            var result = await _osuApiService.LookupBeatmapByIdAsync(beatmapId);
            if (result.HasValue)
            {
                // InvokeAsync (not Post) so SaveQueueAsync runs after properties are updated
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    vm.Title = result.Value.title;
                    vm.Artist = result.Value.artist;
                });
                _ = SaveQueueAsync();
                _logger.LogDebug("Fetched metadata for beatmapId {BeatmapId}: '{Artist} - {Title}'",
                    beatmapId, result.Value.artist, result.Value.title);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FetchAndUpdateMetaAsync failed for beatmapId {BeatmapId}", beatmapId);
        }
    }

    // Enqueue by MD5 hash. Creates an AwaitingLookup entry immediately; when the lookup
    // loop resolves the MD5 → BeatmapSetId, the entry is promoted to Queued automatically.
    public void EnqueueByMd5(MissingBeatmap missing)
    {
        _logger.LogDebug("EnqueueByMd5: {Md5} (resolved={Resolved})", missing.MD5Hash, missing.IsResolved);
        // Fast path 1: model already resolved
        if (missing.IsResolved && missing.BeatmapSetId.HasValue)
        {
            EnqueueDownload(missing.BeatmapSetId.Value, missing.Title ?? "Unknown", missing.Artist ?? "Unknown");
            return;
        }

        // Fast path 2: cache has the answer (handles race where RegisterForLookup's
        // UIThread.Post hasn't fired yet but lookup already completed)
        LookupCacheEntry? cached;
        lock (_lookupSync)
            cached = _lookupCache.TryGetValue(missing.MD5Hash, out var c) ? c : null;
        if (cached != null)
        {
            EnqueueDownload(cached.BeatmapSetId, cached.Title, cached.Artist);
            return;
        }

        var vm = new DownloadEntryViewModel(Guid.NewGuid(), 0, "", "", this)
        {
            MD5Hash = missing.MD5Hash,
            Status = DownloadStatus.AwaitingLookup
        };
        var shortHash = missing.MD5Hash.Length >= 8 ? missing.MD5Hash[..8] : missing.MD5Hash;
        vm.Title = $"MD5: {shortHash}…";

        lock (_lookupSync)
        {
            // Skip if there's already an AwaitingLookup entry for this MD5
            if (_md5PendingDownloads.ContainsKey(missing.MD5Hash)) return;
            _md5PendingDownloads[missing.MD5Hash] = vm.Id;
        }

        Dispatcher.UIThread.Post(() =>
        {
            Downloads.Add(vm);
            _ = SaveQueueAsync();
        });

        // Ensure this MD5 is still in the pending lookup queue. It may have been removed
        // already (if lookup ran and resolved before this call), in which case re-add it so
        // the loop picks it up again and can promote our AwaitingLookup entry.
        lock (_pendingLookups)
        {
            if (!_pendingLookups.Contains(missing))
            {
                _pendingLookups.Add(missing);
                _nextAttempt.Remove(missing);
            }
        }
    }

    public void CancelDownload(Guid id)
    {
        var vm = Downloads.FirstOrDefault(d => d.Id == id);
        if (vm == null) return;

        if (vm.MD5Hash != null)
            lock (_lookupSync) _md5PendingDownloads.Remove(vm.MD5Hash);

        vm.CancellationTokenSource?.Cancel();
        Dispatcher.UIThread.Post(() =>
        {
            vm.Status = DownloadStatus.Cancelled;
            NotifyActiveCount();
        });
        _ = SaveQueueAsync();
        _logger.LogInformation("Download cancelled: {Id} (set {SetId})", id, vm.BeatmapSetId);
    }

    // Removes a finished/errored entry from the list (used by per-item ✕ and "clear" actions).
    public void RemoveDownload(Guid id)
    {
        var vm = Downloads.FirstOrDefault(d => d.Id == id);
        if (vm == null) return;

        if (vm.MD5Hash != null)
            lock (_lookupSync) _md5PendingDownloads.Remove(vm.MD5Hash);

        Dispatcher.UIThread.Post(() => Downloads.Remove(vm));
        _ = SaveQueueAsync();
        _logger.LogInformation("Download removed: {Id} (set {SetId})", id, vm.BeatmapSetId);
    }

    public void RetryDownload(Guid id)
    {
        var vm = Downloads.FirstOrDefault(d => d.Id == id);
        if (vm == null || (vm.Status != DownloadStatus.Failed && vm.Status != DownloadStatus.Cancelled))
            return;
        Dispatcher.UIThread.Post(() =>
        {
            vm.Status = DownloadStatus.Queued;
            vm.Progress = 0;
            vm.Error = null;
            Kick();
        });
        _ = SaveQueueAsync();
        _logger.LogInformation("Download retry queued: {Id} (set {SetId})", id, vm.BeatmapSetId);
    }

    // Called by AppInitializationService/CollectionService to register unresolved MD5s for model updates
    public void RegisterForLookup(MissingBeatmap missing)
    {
        LookupCacheEntry? cached;
        lock (_lookupSync)
            cached = _lookupCache.TryGetValue(missing.MD5Hash, out var c) ? c : null;

        if (cached != null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                missing.BeatmapSetId = cached.BeatmapSetId;
                missing.BeatmapId = cached.BeatmapId;
                missing.Title = cached.Title;
                missing.Artist = cached.Artist;
                missing.IsResolved = true;
            });
            return;
        }

        lock (_pendingLookups)
            if (!_pendingLookups.Contains(missing))
                _pendingLookups.Add(missing);
    }

    public int GetActiveCount() => Volatile.Read(ref _activeDownloads);

    public async Task SaveQueueAsync()
    {
        try
        {
            // Snapshot on the UI thread — Downloads is an ObservableCollection mutated there;
            // enumerating it off-thread would throw "Collection was modified".
            // Don't persist AwaitingLookup entries — re-created when user clicks "Download all".
            var dtos = await Dispatcher.UIThread.InvokeAsync(() => Downloads
                .Where(d => d.Status != DownloadStatus.AwaitingLookup)
                .Select(d => new DownloadDto(
                    d.Id, d.BeatmapSetId, d.Title, d.Artist,
                    d.Status, d.AddedAt, d.CompletedAt, d.Error))
                .ToList());

            var json = JsonSerializer.Serialize(dtos, JsonOpts);

            await _queueSaveLock.WaitAsync();
            try { await File.WriteAllTextAsync(QueuePath, json); }
            finally { _queueSaveLock.Release(); }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SaveQueueAsync failed");
        }
    }

    // ── Private: download loop ────────────────────────────────────────────────

    private void Kick()
    {
        // Coalesce: at most one pending permit so we don't spin needlessly.
        if (_kick.CurrentCount == 0)
        {
            try { _kick.Release(); } catch (SemaphoreFullException) { }
        }
    }

    private async Task ProcessDownloadQueueAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Event-driven, with a slow fallback tick so a missed kick still self-heals.
                await _kick.WaitAsync(5000, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; } // semaphore disposed on shutdown

            await Dispatcher.UIThread.InvokeAsync(TryStartNextDownloads);
        }
    }

    // Must be called on UI thread (reads Downloads which is ObservableCollection on UI thread)
    private void TryStartNextDownloads()
    {
        var max = _settingsService.MaxConcurrentDownloads;
        var slots = max - Volatile.Read(ref _activeDownloads);
        if (slots <= 0) return;

        var toStart = Downloads
            .Where(d => d.Status == DownloadStatus.Queued)
            .Take(slots)
            .ToList();

        if (toStart.Count > 0)
            _logger.LogDebug("Starting {Count} download(s) (max concurrent={Max})", toStart.Count, max);

        foreach (var next in toStart)
        {
            Interlocked.Increment(ref _activeDownloads);
            NotifyActiveCount();
            next.Status = DownloadStatus.Downloading;
            _ = DownloadAsync(next);
        }
    }

    private async Task DownloadAsync(DownloadEntryViewModel vm)
    {
        var cts = new CancellationTokenSource();
        vm.CancellationTokenSource = cts;

        var dir = Path.Combine(_settingsService.OsuDirPath?.Replace('/', '\\') ?? @"D:\osu!", "Songs");
        Directory.CreateDirectory(dir);
        var outPath = Path.Combine(dir, $"{vm.BeatmapSetId}.osz");
        var tmpPath = outPath + ".part";

        var mirrors = GetMirrorUrls(vm.BeatmapSetId);
        bool success = false;
        string? lastError = null;

        _logger.LogInformation("Download started: set {SetId} → {OutPath}", vm.BeatmapSetId, outPath);

        // Throttle progress posts: at most when it advances ≥1% (avoids flooding the UI thread).
        double lastReported = -1;
        void ReportProgress(double p)
        {
            if (p < 1.0 && p - lastReported < 0.01) return;
            lastReported = p;
            Dispatcher.UIThread.Post(() => vm.Progress = p);
        }

        foreach (var url in mirrors)
        {
            if (cts.Token.IsCancellationRequested) break;
            try
            {
                lastReported = -1;
                _logger.LogDebug("Download attempt: set {SetId} via {Url}", vm.BeatmapSetId, url);
                await DownloadFileAsync(url, tmpPath, ReportProgress, cts.Token);
                File.Move(tmpPath, outPath, overwrite: true);
                success = true;
                break;
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                lastError = ex.Message;
                _logger.LogWarning(ex, "Mirror {Url} failed for set {SetId}", url, vm.BeatmapSetId);
            }
        }

        if (!success) TryDelete(tmpPath);
        if (cts.Token.IsCancellationRequested) { TryDelete(tmpPath); TryDelete(outPath); }

        Interlocked.Decrement(ref _activeDownloads);
        NotifyActiveCount();
        Kick(); // free slot → let the scheduler start the next queued item

        Dispatcher.UIThread.Post(() =>
        {
            if (cts.Token.IsCancellationRequested)
            {
                vm.Status = DownloadStatus.Cancelled;
                _logger.LogInformation("Download cancelled mid-flight: set {SetId}", vm.BeatmapSetId);
            }
            else if (success)
            {
                vm.Status = DownloadStatus.Completed;
                vm.CompletedAt = DateTime.UtcNow;
                vm.Progress = 1.0;
                _logger.LogInformation("Download completed: set {SetId}", vm.BeatmapSetId);
            }
            else
            {
                vm.Status = DownloadStatus.Failed;
                vm.Error = lastError ?? "Все зеркала недоступны";
                _logger.LogError("Download failed: set {SetId} — {Error}", vm.BeatmapSetId, vm.Error);
            }
        });

        _ = SaveQueueAsync();
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    private static async Task DownloadFileAsync(string url, string path, Action<double> onProgress, CancellationToken ct)
    {
        using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? -1L;
        await using var src = await response.Content.ReadAsStreamAsync(ct);
        await using var dst = File.Create(path);

        var buf = new byte[81920];
        long downloaded = 0;
        int read;
        while ((read = await src.ReadAsync(buf, ct)) > 0)
        {
            await dst.WriteAsync(buf.AsMemory(0, read), ct);
            downloaded += read;
            if (total > 0) onProgress((double)downloaded / total);
        }
    }

    private string[] GetMirrorUrls(int setId)
    {
        var preferred = _settingsService.PreferredMirror;
        var all = new[]
        {
            ("catboy.best",    $"https://catboy.best/d/{setId}"),
            ("beatconnect.io", $"https://beatconnect.io/b/{setId}"),
            ("osu.direct",     $"https://osu.direct/api/d/{setId}"),
        };
        return all.OrderByDescending(x => x.Item1 == preferred).Select(x => x.Item2).ToArray();
    }

    // ── Private: lookup loop ──────────────────────────────────────────────────

    private async Task ProcessLookupQueueAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            MissingBeatmap? item = null;
            lock (_pendingLookups)
            {
                var now = DateTime.UtcNow;
                // Pick the first item whose backoff has elapsed; unresolvable items in backoff
                // are skipped so they don't stall resolvable ones.
                item = _pendingLookups.FirstOrDefault(m =>
                    !_nextAttempt.TryGetValue(m, out var t) || t <= now);
            }

            if (item == null)
            {
                await Task.Delay(2_000, ct).ConfigureAwait(false);
                continue;
            }

            try
            {
                var result = await _osuApiService.LookupBeatmapByMd5Async(item.MD5Hash);
                if (result.HasValue)
                {
                    var r = result.Value;

                    // Update the MissingBeatmap model
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        item.BeatmapSetId = r.beatmapSetId;
                        item.BeatmapId = r.beatmapId;
                        item.Title = r.title;
                        item.Artist = r.artist;
                        item.IsResolved = true;
                    });

                    // If there's a pending AwaitingLookup download for this MD5, promote it
                    Guid pendingId;
                    bool hasPending;
                    lock (_lookupSync)
                        hasPending = _md5PendingDownloads.Remove(item.MD5Hash, out pendingId);

                    if (hasPending)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            var pendingVm = Downloads.FirstOrDefault(d => d.Id == pendingId);
                            if (pendingVm == null || pendingVm.Status != DownloadStatus.AwaitingLookup) return;

                            // Deduplicate: another entry for this BeatmapSetId might already be queued
                            // (e.g., multiple diffs from the same set all enqueued via EnqueueByMd5)
                            bool duplicate = Downloads.Any(d => d != pendingVm &&
                                d.BeatmapSetId == r.beatmapSetId &&
                                d.Status != DownloadStatus.Cancelled &&
                                d.Status != DownloadStatus.Failed);

                            if (duplicate)
                                Downloads.Remove(pendingVm);
                            else
                            {
                                pendingVm.BeatmapSetId = r.beatmapSetId;
                                pendingVm.Title = r.title;
                                pendingVm.Artist = r.artist;
                                pendingVm.Status = DownloadStatus.Queued;
                                Kick();
                            }
                        });
                    }

                    lock (_lookupSync)
                        _lookupCache[item.MD5Hash] = new LookupCacheEntry(r.beatmapSetId, r.beatmapId, r.title, r.artist);
                    await SaveLookupCacheAsync();

                    lock (_pendingLookups)
                    {
                        _pendingLookups.Remove(item);
                        _nextAttempt.Remove(item);
                    }

                    _logger.LogInformation("MD5 lookup resolved: {Md5} → set {SetId} '{Artist} - {Title}'",
                        item.MD5Hash, r.beatmapSetId, r.artist, r.title);
                }
                else
                {
                    // Not found right now — back off this item only, keep processing others.
                    lock (_pendingLookups)
                        _nextAttempt[item] = DateTime.UtcNow.AddSeconds(60);
                    _logger.LogDebug("MD5 lookup not found: {Md5} — backing off 60s", item.MD5Hash);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                lock (_pendingLookups)
                    _nextAttempt[item] = DateTime.UtcNow.AddSeconds(30);
                _logger.LogWarning(ex, "MD5 lookup threw for {Md5} — backing off 30s", item.MD5Hash);
            }

            // No local delay needed: LookupBeatmapByMd5Async is rate-limited centrally in OsuApiService.
        }
    }

    // ── Persistence ───────────────────────────────────────────────────────────

    private void LoadDownloadQueue()
    {
        try
        {
            if (!File.Exists(QueuePath)) return;
            var json = File.ReadAllText(QueuePath);
            var dtos = JsonSerializer.Deserialize<List<DownloadDto>>(json, JsonOpts) ?? [];

            foreach (var dto in dtos)
            {
                var status = dto.Status == DownloadStatus.Downloading ? DownloadStatus.Queued : dto.Status;
                var vm = new DownloadEntryViewModel(dto.Id, dto.BeatmapSetId, dto.Title, dto.Artist, this)
                {
                    Status = status,
                    AddedAt = dto.AddedAt,
                    CompletedAt = dto.CompletedAt,
                    Error = dto.Error,
                };
                Downloads.Add(vm);
            }
            _logger.LogDebug("Loaded download queue: {Count} entries from {Path}", Downloads.Count, QueuePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LoadDownloadQueue failed");
        }
    }

    private void LoadLookupCache()
    {
        try
        {
            if (!File.Exists(LookupCachePath)) return;
            var json = File.ReadAllText(LookupCachePath);
            lock (_lookupSync)
                _lookupCache = JsonSerializer.Deserialize<Dictionary<string, LookupCacheEntry>>(json, JsonOpts) ?? new();
            _logger.LogDebug("Loaded MD5 lookup cache: {Count} entries", _lookupCache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LoadLookupCache failed");
        }
    }

    private async Task SaveLookupCacheAsync()
    {
        try
        {
            string json;
            lock (_lookupSync)
                json = JsonSerializer.Serialize(_lookupCache, JsonOpts);

            await _cacheSaveLock.WaitAsync();
            try { await File.WriteAllTextAsync(LookupCachePath, json); }
            finally { _cacheSaveLock.Release(); }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SaveLookupCacheAsync failed");
        }
    }

    private void NotifyActiveCount() => ActiveCountChanged?.Invoke();

    public void Dispose()
    {
        _logger.LogInformation("BeatmapDownloadService disposing");
        try { _cts.Cancel(); } catch { }
        _cts.Dispose();
        _kick.Dispose();
        _queueSaveLock.Dispose();
        _cacheSaveLock.Dispose();
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────

    private record DownloadDto(
        Guid Id, int BeatmapSetId, string Title, string Artist,
        DownloadStatus Status, DateTime AddedAt, DateTime? CompletedAt, string? Error);

    private record LookupCacheEntry(int BeatmapSetId, int BeatmapId, string Title, string Artist);
}
