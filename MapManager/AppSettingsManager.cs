using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MapManager;

public static class AppSettingsManager
{
    private static readonly string SettingsFilePath = "appsettings.json";

    // Прокидывается из Program.BuildHost после поднятия хоста; до этого момента null
    // (LoadSettingsAsync вызывается раньше), поэтому все обращения через ?.
    public static ILogger? Logger { get; set; }

    // Serialises writes so concurrent fire-and-forget saves (from many SettingsService setters)
    // don't collide on the same file.
    private static readonly SemaphoreSlim SaveLock = new(1, 1);

    public static AppSettings LoadSettingsAsync()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                Logger?.LogInformation("Settings loaded from {Path} ({Bytes} bytes)", SettingsFilePath, json.Length);
                return settings;
            }

            Logger?.LogWarning("Settings file {Path} not found — using defaults", SettingsFilePath);
            return new AppSettings(); // Если файл отсутствует, возвращаем дефолтные настройки
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to load settings from {Path} — using defaults", SettingsFilePath);
            return new AppSettings();
        }
    }

    public static async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });

            await SaveLock.WaitAsync();
            try { await File.WriteAllTextAsync(SettingsFilePath, json); }
            finally { SaveLock.Release(); }

            Logger?.LogDebug("Settings saved to {Path} ({Bytes} bytes)", SettingsFilePath, json.Length);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to save settings to {Path}", SettingsFilePath);
        }
    }
}
