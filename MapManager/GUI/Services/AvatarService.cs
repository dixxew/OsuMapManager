using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;

public class AvatarService
{
    private const int MaxCacheSize = 200;

    private readonly OsuApiService _osuApiService;
    private readonly CacheService _cacheService;

    // All accessed only on UI thread
    private readonly Dictionary<string, Bitmap?> _avatarCache = new();
    private readonly Queue<string> _cacheOrder = new();
    private readonly HashSet<string> _loading = new();

    public AvatarService(OsuApiService osuApiService, CacheService cacheService)
    {
        _osuApiService = osuApiService;
        _cacheService = cacheService;
    }

    // Must be called from UI thread
    public Bitmap? GetAvatar(string username)
    {
        if (_avatarCache.TryGetValue(username, out var bitmap))
            return bitmap;

        if (_loading.Add(username))
            _ = LoadAvatarAsync(username);

        return null;
    }

    private async Task LoadAvatarAsync(string username)
    {
        var cacheKey = $"chat_{username}";

        // Phase 1: show stale disk cache immediately while network fetch is in flight
        if (!_cacheService.IsImageCacheValid(cacheKey))
        {
            Bitmap? stale = null;
            try { stale = await _cacheService.TryReadCachedImageAsync(cacheKey); }
            catch { }

            if (stale != null)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    PutInMemoryCache(username, stale);
                    AvatarLoaded?.Invoke(username);
                });
            }
        }

        // Phase 2: fetch fresh (valid disk cache → skip network; stale/missing → network + save)
        Bitmap? fresh = null;
        try
        {
            fresh = await _cacheService.GetImageAsync(
                cacheKey,
                () => _osuApiService.GetAvatarAsync(username));
        }
        catch { }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _loading.Remove(username);

            if (fresh == null) return;

            // Dispose the stale bitmap we may have placed in phase 1
            if (_avatarCache.TryGetValue(username, out var existing) && !ReferenceEquals(existing, fresh))
                existing?.Dispose();

            PutInMemoryCache(username, fresh);
            AvatarLoaded?.Invoke(username);
        });
    }

    // Must be called from UI thread
    private void PutInMemoryCache(string username, Bitmap bitmap)
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
        _avatarCache[username] = bitmap;
    }

    public event Action<string>? AvatarLoaded;
}
