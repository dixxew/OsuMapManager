using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;

namespace MapManager.GUI.Services;

public class CacheService
{
    public static CacheService? Current { get; private set; }

    private readonly string _avatarsDir;
    private readonly string _scoresDir;
    private readonly AppSettings _appSettings;
    private readonly ILogger<CacheService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public CacheService(AppSettings appSettings, ILogger<CacheService> logger)
    {
        _appSettings = appSettings;
        _logger = logger;
        _avatarsDir = Path.Combine("cache", "avatars");
        _scoresDir = Path.Combine("cache", "scores");
        Directory.CreateDirectory(_avatarsDir);
        Directory.CreateDirectory(_scoresDir);
        Current = this;
        _logger.LogInformation("CacheService initialized (avatars: {Avatars}, scores: {Scores})", _avatarsDir, _scoresDir);
    }

    private bool IsCacheValid(string path)
    {
        if (!File.Exists(path)) return false;
        var written = File.GetLastWriteTimeUtc(path);
        if ((DateTime.UtcNow - written).TotalHours >= 24) return false;
        if (_appSettings.CacheInvalidatedAt.HasValue && written < _appSettings.CacheInvalidatedAt.Value)
            return false;
        return true;
    }

    public bool IsImageCacheValid(string key)
    {
        var path = Path.Combine(_avatarsDir, SanitizeKey(key) + ".png");
        return IsCacheValid(path);
    }

    // Reads disk file if it exists, regardless of TTL. Returns null if file is missing.
    public Task<Bitmap?> TryReadCachedImageAsync(string key) => Task.Run<Bitmap?>(() =>
    {
        var path = Path.Combine(_avatarsDir, SanitizeKey(key) + ".png");
        if (!File.Exists(path)) return null;
        try { return new Bitmap(path); }
        catch { return null; }
    });

    public async Task<Bitmap?> GetImageAsync(string key, Func<Task<Bitmap?>> fetchFunc)
    {
        var path = Path.Combine(_avatarsDir, SanitizeKey(key) + ".png");
        if (IsCacheValid(path))
        {
            try { _logger.LogTrace("Image cache hit: {Key}", key); return new Bitmap(path); }
            catch (Exception ex) { _logger.LogWarning(ex, "Image cache read failed: {Key}", key); }
        }

        _logger.LogTrace("Image cache miss: {Key} — fetching", key);
        var bitmap = await fetchFunc();
        if (bitmap != null)
        {
            try { bitmap.Save(path); }
            catch (Exception ex) { _logger.LogWarning(ex, "Image cache save failed: {Key}", key); }
        }
        return bitmap;
    }

    public async Task<T?> GetJsonAsync<T>(string key, Func<Task<T?>> fetchFunc)
    {
        var path = Path.Combine(_scoresDir, SanitizeKey(key) + ".json");
        if (IsCacheValid(path))
        {
            try
            {
                var json = await File.ReadAllTextAsync(path);
                var result = JsonSerializer.Deserialize<T>(json, JsonOptions);
                if (result != null) { _logger.LogTrace("JSON cache hit: {Key}", key); return result; }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "JSON cache read failed: {Key}", key); }
        }

        _logger.LogTrace("JSON cache miss: {Key} — fetching", key);
        var data = await fetchFunc();
        if (data != null)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, JsonOptions);
                await File.WriteAllTextAsync(path, json);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "JSON cache save failed: {Key}", key); }
        }
        return data;
    }

    public async Task InvalidateAllAsync()
    {
        _logger.LogInformation("Invalidating all caches");
        _appSettings.CacheInvalidatedAt = DateTime.UtcNow;
        await AppSettingsManager.SaveSettingsAsync(_appSettings);
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
