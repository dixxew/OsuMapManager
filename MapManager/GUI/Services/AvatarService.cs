using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;

public class AvatarService
{
    private const int MaxCacheSize = 200;
    private const int ConcurrencyLimit = 3;

    private readonly OsuApiService _osuApiService;
    private readonly CacheService _cacheService;

    // ── All fields below: UI-thread-only ─────────────────────────────────────

    private readonly Dictionary<string, Bitmap?> _avatarCache = new();
    private readonly Queue<string> _cacheOrder = new();

    // Items in _pending queue OR currently being fetched
    private readonly HashSet<string> _scheduled = new();

    // Lower priority value = processed first (0 = UI-visible, 1 = background preload)
    private readonly PriorityQueue<string, int> _pending = new();

    // ── Cross-thread: limits concurrent network/disk fetches ──────────────────

    private readonly SemaphoreSlim _slots = new(ConcurrencyLimit, ConcurrencyLimit);

    // Sequential gate for osu! API network calls + cooldown between them.
    // Disk-cache hits bypass this entirely.
    private readonly SemaphoreSlim _networkGate = new(1, 1);
    private DateTime _lastNetworkFetch = DateTime.MinValue;
    private static readonly TimeSpan NetworkInterval = TimeSpan.FromSeconds(1.2); // ~50 req/min

    public event Action<string>? AvatarLoaded;

    public AvatarService(OsuApiService osuApiService, CacheService cacheService)
    {
        _osuApiService = osuApiService;
        _cacheService = cacheService;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// Returns cached bitmap immediately, or null while loading.
    /// Call from UI thread (e.g. inside a property getter bound to UI).
    public Bitmap? GetAvatar(string username)
    {
        if (_avatarCache.TryGetValue(username, out var bmp)) return bmp;
        Schedule(username, priority: 0);
        return null;
    }

    /// Kick off a low-priority background load for a user not yet visible on screen.
    /// When the user scrolls into view and GetAvatar is called, that call takes priority.
    public void Preload(string username)
    {
        if (_avatarCache.ContainsKey(username)) return;
        Schedule(username, priority: 1);
    }

    // ── Scheduling ────────────────────────────────────────────────────────────

    private void Schedule(string username, int priority)
    {
        // Low-priority: skip entirely if already in queue or loading
        if (priority > 0 && _scheduled.Contains(username)) return;

        // High-priority (UI-visible): always enqueue, even if a low-priority entry is already
        // in the queue. The drain loop skips duplicate entries once the avatar is cached.
        _scheduled.Add(username);
        _pending.Enqueue(username, priority);
        _ = DrainOneAsync();
    }

    /// Acquires one concurrency slot, dequeues the highest-priority pending username, loads it.
    private async Task DrainOneAsync()
    {
        await _slots.WaitAsync();

        // Dequeue on UI thread (all queue/cache state is UI-thread-only)
        string? username = await Dispatcher.UIThread.InvokeAsync(PickNext);

        if (username == null)
        {
            _slots.Release();
            return;
        }

        try
        {
            await LoadAvatarAsync(username);
        }
        finally
        {
            _slots.Release();
        }
    }

    /// Dequeues the next item that still needs loading. Must run on UI thread.
    private string? PickNext()
    {
        while (_pending.TryDequeue(out var u, out _))
        {
            if (!_avatarCache.ContainsKey(u))
                return u;

            // Already cached (promoted + loaded via an earlier high-priority entry)
            _scheduled.Remove(u);
        }
        return null;
    }

    // ── Loading ───────────────────────────────────────────────────────────────

    private async Task LoadAvatarAsync(string username)
    {
        var cacheKey = $"chat_{username}";

        // Phase 1: show stale disk cache instantly while network fetch runs
        if (!_cacheService.IsImageCacheValid(cacheKey))
        {
            Bitmap? stale = null;
            try { stale = await _cacheService.TryReadCachedImageAsync(cacheKey); }
            catch (Exception ex) { Debug.WriteLine($"[Avatar] stale-read failed for '{username}': {ex.Message}"); }

            if (stale != null)
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    PutCache(username, stale);
                    AvatarLoaded?.Invoke(username);
                });
        }

        // Phase 2: fetch fresh.
        // If disk cache is valid we never touch the osu! API — skip the rate-limit gate.
        // If we need the network, go through the sequential gate with a cooldown.
        Bitmap? fresh = null;
        bool needsNetwork = !_cacheService.IsImageCacheValid(cacheKey);

        if (needsNetwork)
        {
            await _networkGate.WaitAsync();
            try
            {
                var wait = NetworkInterval - (DateTime.UtcNow - _lastNetworkFetch);
                if (wait > TimeSpan.Zero)
                {
                    Debug.WriteLine($"[Avatar] rate-limit wait {wait.TotalMilliseconds:0}ms before '{username}'");
                    await Task.Delay(wait);
                }
                _lastNetworkFetch = DateTime.UtcNow;

                fresh = await _cacheService.GetImageAsync(
                    cacheKey,
                    () => _osuApiService.GetAvatarAsync(username));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Avatar] network fetch failed for '{username}': {ex.Message}");
            }
            finally
            {
                _networkGate.Release();
            }
        }
        else
        {
            try
            {
                fresh = await _cacheService.GetImageAsync(
                    cacheKey,
                    () => _osuApiService.GetAvatarAsync(username));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Avatar] disk-cache read failed for '{username}': {ex.Message}");
            }
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _scheduled.Remove(username);

            if (fresh == null) return;

            if (_avatarCache.TryGetValue(username, out var old) && !ReferenceEquals(old, fresh))
                old?.Dispose();

            PutCache(username, fresh);
            AvatarLoaded?.Invoke(username);
        });
    }

    // ── Cache helpers ─────────────────────────────────────────────────────────

    private void PutCache(string username, Bitmap bmp)
    {
        if (!_avatarCache.ContainsKey(username))
        {
            if (_avatarCache.Count >= MaxCacheSize && _cacheOrder.TryDequeue(out var oldest))
            {
                _avatarCache[oldest]?.Dispose();
                _avatarCache.Remove(oldest);
            }
            _cacheOrder.Enqueue(username);
        }
        _avatarCache[username] = bmp;
    }
}
