using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;

public class AvatarService
{
    private const int MaxCacheSize = 200;
    private const int ConcurrencyLimit = 3;

    private readonly OsuApiService _osuApiService;
    private readonly CacheService _cacheService;

    // ── UI-thread-only ────────────────────────────────────────────────────────

    private readonly Dictionary<string, Bitmap?> _avatarCache = new();
    private readonly Queue<string> _cacheOrder = new();
    private readonly HashSet<string> _scheduled = new();
    private readonly PriorityQueue<string, int> _pending = new();

    // ── Cross-thread ──────────────────────────────────────────────────────────

    // Limits concurrent network slots; disk reads release this immediately.
    private readonly SemaphoreSlim _slots = new(ConcurrencyLimit, ConcurrencyLimit);

    // Serialises osu! API calls (GetUserAsync) with a cooldown.
    // Image downloads bypass this gate entirely.
    private readonly SemaphoreSlim _networkGate = new(1, 1);
    private DateTime _lastNetworkFetch = DateTime.MinValue;
    private static readonly TimeSpan NetworkInterval = TimeSpan.FromSeconds(1.2);

    // username → avatar URL; persisted to disk so GetUserAsync is called only once per user.
    private readonly ConcurrentDictionary<string, string> _urlCache =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly string _urlCachePath = Path.Combine("cache", "avatars", "url_map.json");

    public event Action<string>? AvatarLoaded;

    public AvatarService(OsuApiService osuApiService, CacheService cacheService)
    {
        _osuApiService = osuApiService;
        _cacheService = cacheService;
        LoadUrlCache();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public Bitmap? GetAvatar(string username)
    {
        if (_avatarCache.TryGetValue(username, out var bmp)) return bmp;
        Schedule(username, priority: 0);
        return null;
    }

    public void Preload(string username)
    {
        if (_avatarCache.ContainsKey(username)) return;
        Schedule(username, priority: 1);
    }

    // ── Scheduling ────────────────────────────────────────────────────────────

    private void Schedule(string username, int priority)
    {
        if (priority > 0 && _scheduled.Contains(username)) return;
        _scheduled.Add(username);
        _pending.Enqueue(username, priority);
        _ = DrainOneAsync();
    }

    private async Task DrainOneAsync()
    {
        await _slots.WaitAsync();

        string? username = await Dispatcher.UIThread.InvokeAsync(PickNext);
        if (username == null)
        {
            _slots.Release();
            return;
        }

        var cacheKey = $"chat_{username}";

        // Valid disk cache: release the slot immediately so network fetches aren't blocked.
        if (_cacheService.IsImageCacheValid(cacheKey))
        {
            _slots.Release();
            await LoadFromDiskAsync(username, cacheKey);
            return;
        }

        try   { await LoadFromNetworkAsync(username, cacheKey); }
        finally { _slots.Release(); }
    }

    private string? PickNext()
    {
        while (_pending.TryDequeue(out var u, out _))
        {
            if (!_avatarCache.ContainsKey(u))
                return u;
            _scheduled.Remove(u);
        }
        return null;
    }

    // ── Loading ───────────────────────────────────────────────────────────────

    private async Task LoadFromDiskAsync(string username, string cacheKey)
    {
        Bitmap? bitmap = null;
        try { bitmap = await _cacheService.TryReadCachedImageAsync(cacheKey); }
        catch (Exception ex) { Debug.WriteLine($"[Avatar] disk read failed for '{username}': {ex.Message}"); }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _scheduled.Remove(username);
            if (bitmap == null) return;
            if (_avatarCache.TryGetValue(username, out var old) && !ReferenceEquals(old, bitmap))
                old?.Dispose();
            PutCache(username, bitmap);
            AvatarLoaded?.Invoke(username);
        });
    }

    private async Task LoadFromNetworkAsync(string username, string cacheKey)
    {
        // Phase 1: show stale disk image instantly while fresh one is fetched.
        Bitmap? stale = null;
        try { stale = await _cacheService.TryReadCachedImageAsync(cacheKey); }
        catch { }

        if (stale != null)
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PutCache(username, stale);
                AvatarLoaded?.Invoke(username);
            });

        // Phase 2a: resolve avatar URL.
        // If the URL is already cached we skip the osu! API call entirely.
        if (!_urlCache.TryGetValue(username, out var avatarUrl))
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

                avatarUrl = await _osuApiService.GetUserAvatarUrlAsync(username);
                if (avatarUrl != null)
                {
                    _urlCache[username] = avatarUrl;
                    _ = SaveUrlCacheAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Avatar] URL lookup failed for '{username}': {ex.Message}");
            }
            finally
            {
                _networkGate.Release();
            }
        }

        if (avatarUrl == null)
        {
            await Dispatcher.UIThread.InvokeAsync(() => _scheduled.Remove(username));
            return;
        }

        // Phase 2b: download image.
        // Not gated by _networkGate — only the osu! API call above is rate-limited.
        Bitmap? fresh = null;
        try
        {
            var url = avatarUrl; // capture for lambda
            fresh = await _cacheService.GetImageAsync(
                cacheKey,
                () => _osuApiService.DownloadAvatarFromUrlAsync(url));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Avatar] image download failed for '{username}': {ex.Message}");
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

    // ── URL cache ─────────────────────────────────────────────────────────────

    private void LoadUrlCache()
    {
        try
        {
            if (!File.Exists(_urlCachePath)) return;
            var json = File.ReadAllText(_urlCachePath);
            var map = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (map == null) return;
            foreach (var (k, v) in map)
                _urlCache[k] = v;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Avatar] URL cache load failed: {ex.Message}");
        }
    }

    private async Task SaveUrlCacheAsync()
    {
        try
        {
            var snapshot = new Dictionary<string, string>(_urlCache);
            var json = JsonSerializer.Serialize(snapshot);
            await File.WriteAllTextAsync(_urlCachePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Avatar] URL cache save failed: {ex.Message}");
        }
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
