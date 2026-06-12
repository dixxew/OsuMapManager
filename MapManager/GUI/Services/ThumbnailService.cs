using Avalonia.Media.Imaging;
using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;

public class ThumbnailService
{
    public static ThumbnailService? Current { get; private set; }

    private readonly OsuDataService _osuDataService;
    private readonly string _thumbsDir;
    private readonly ConcurrentDictionary<string, Bitmap?> _memoryCache = new();
    private readonly SemaphoreSlim _semaphore = new(4, 4);

    private const int TargetWidth = 160;
    private const int JpegQuality = 55;

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

            var diskPath = Path.Combine(_thumbsDir, SanitizeKey(folderName) + ".jpg");

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

                    using var original = SKBitmap.Decode(imgPath);
                    if (original == null) return null;

                    var targetH = (int)(original.Height * (TargetWidth / (double)original.Width));
                    targetH = Math.Clamp(targetH, 20, 100);

                    using var scaled = original.Resize(
                        new SKImageInfo(TargetWidth, targetH),
                        SKFilterQuality.Medium);
                    if (scaled == null) return null;

                    using var image = SKImage.FromBitmap(scaled);
                    using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
                    using (var fs = File.OpenWrite(diskPath))
                        encoded.SaveTo(fs);

                    var bitmap = new Bitmap(diskPath);
                    _memoryCache[folderName] = bitmap;
                    return bitmap;
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
