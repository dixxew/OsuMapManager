using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MapManager;

public static class AppSettingsManager
{
    private static readonly string SettingsFilePath = "appsettings.json";

    // Serialises writes so concurrent fire-and-forget saves (from many SettingsService setters)
    // don't collide on the same file.
    private static readonly SemaphoreSlim SaveLock = new(1, 1);

    public static AppSettings LoadSettingsAsync()
    {
        if (File.Exists(SettingsFilePath))
        {
            var json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }

        return new AppSettings(); // Если файл отсутствует, возвращаем дефолтные настройки
    }

    public static async Task SaveSettingsAsync(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });

        await SaveLock.WaitAsync();
        try { await File.WriteAllTextAsync(SettingsFilePath, json); }
        finally { SaveLock.Release(); }
    }
}
