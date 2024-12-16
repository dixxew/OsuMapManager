using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MapManager;

public static class AppSettingsManager
{
    private static readonly string SettingsFilePath = "appsettings.json";

    public static async Task<AppSettings> LoadSettingsAsync()
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
        await File.WriteAllTextAsync(SettingsFilePath, json);
    }
}
