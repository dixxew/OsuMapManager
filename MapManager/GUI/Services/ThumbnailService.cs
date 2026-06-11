using Avalonia;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace MapManager.GUI.Services;

public class ThumbnailService
{
    public static ThumbnailService? Current { get; private set; }

    private readonly OsuDataService _osuDataService;
    private readonly string _thumbsDir;
    private readonly ConcurrentDictionary<string, Bitmap?> _memoryCache = new();
    private readonly SemaphoreSlim _semaphore = new(4, 4);

    private const int TargetWidth = 300;

    public ThumbnailService(OsuDataService osuDataService)
    {
        _osuDataService = osuDataService;
        _thumbsDir = Path.Combine("cache", "thumbnails");
        Directory.CreateDirectory(_thumbsDir);
        Current = this;
    }

    public async Task<Bitmap?> GetAsync(string folderName, string beatmapFileName)
    {
        if (_memoryCache.TryGetValue(folderName, out var cached))
            return cached;

        await _semaphore.WaitAsync();
        try
        {
            if (_memoryCache.TryGetValue(folderName, out cached))
                return cached;

            var diskPath = Path.Combine(_thumbsDir, SanitizeKey(folderName) + ".png");

            if (File.Exists(diskPath))
            {
                try
                {
                    var fromDisk = new Bitmap(diskPath);
                    _memoryCache[folderName] = fromDisk;
                    return fromDisk;
                }
                catch { }
            }

            return await Task.Run(() =>
            {
                try
                {
                    var imgPath = _osuDataService.GetBeatmapImage(folderName, beatmapFileName);
                    if (!File.Exists(imgPath)) return null;

                    using var original = new Bitmap(imgPath);
                    var h = (int)(original.PixelSize.Height * (TargetWidth / (double)original.PixelSize.Width));
                    h = Math.Clamp(h, 40, 200);

                    var scaled = original.CreateScaledBitmap(
                        new PixelSize(TargetWidth, h),
                        BitmapInterpolationMode.MediumQuality);

                    try { scaled.Save(diskPath); } catch { }

                    _memoryCache[folderName] = scaled;
                    return scaled;
                }
                catch
                {
                    return null;
                }
            });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static string SanitizeKey(string key)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = key.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
            if (Array.IndexOf(invalid, chars[i]) >= 0)
                chars[i] = '_';
        return new string(chars);
    }
}
