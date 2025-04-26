using System;
using Avalonia.Media.Imaging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;

public class AvatarService
{
    private readonly OsuApiService _osuApiService;
    
    private readonly Dictionary<string, Bitmap?> _avatarCache = new();
    

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
        if (avatar != null)
        {
            _avatarCache[username] = avatar;
            AvatarLoaded?.Invoke(username);
        }
    }
    
    
    public event Action<string>? AvatarLoaded;
}