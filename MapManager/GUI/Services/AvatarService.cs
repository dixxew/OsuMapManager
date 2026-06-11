using System;
using Avalonia.Media.Imaging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;

public class AvatarService
{
    private const int MaxCacheSize = 200;

    private readonly OsuApiService _osuApiService;
    private readonly Dictionary<string, Bitmap?> _avatarCache = new();
    private readonly Queue<string> _cacheOrder = new();

    public AvatarService(OsuApiService osuApiService)
    {
        _osuApiService = osuApiService;
    }

    public Bitmap? GetAvatar(string username)
    {
        if (_avatarCache.TryGetValue(username, out var bitmap))
            return bitmap;

        _ = LoadAvatarAsync(username);
        return null;
    }

    private async Task LoadAvatarAsync(string username)
    {
        var avatar = await _osuApiService.GetAvatarAsync(username);
        if (avatar == null) return;

        if (!_avatarCache.ContainsKey(username))
        {
            if (_avatarCache.Count >= MaxCacheSize && _cacheOrder.TryDequeue(out var oldest))
            {
                _avatarCache[oldest]?.Dispose();
                _avatarCache.Remove(oldest);
            }
            _cacheOrder.Enqueue(username);
        }

        _avatarCache[username] = avatar;
        AvatarLoaded?.Invoke(username);
    }

    public event Action<string>? AvatarLoaded;
}