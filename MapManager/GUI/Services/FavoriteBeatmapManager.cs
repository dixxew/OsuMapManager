using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MapManager.GUI.Services;

public class FavoriteBeatmapManager
{
    private readonly static string _filePath = "favorites.json";

    // Прокидывается из Program.BuildHost. Может быть null до поднятия хоста — обращаемся через ?.
    public static ILogger? Logger { get; set; }

    // Чтение избранных карт из файла
    public static List<int> Load()
    {
        if (!File.Exists(_filePath))
        {
            Logger?.LogDebug("Favorites file {Path} not found — empty list", _filePath);
            return new List<int>();
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var list = JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
            Logger?.LogDebug("Loaded {Count} favorite beatmap set(s) from {Path}", list.Count, _filePath);
            return list;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to read favorites from {Path}", _filePath);
            return new List<int>();
        }
    }

    // Сохранение списка избранных карт в файл
    public static void Save(List<int> favoriteBeatmaps)
    {
        try
        {
            var json = JsonSerializer.Serialize(favoriteBeatmaps);
            File.WriteAllText(_filePath, json);
            Logger?.LogDebug("Saved {Count} favorite beatmap set(s) to {Path}", favoriteBeatmaps.Count, _filePath);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to write favorites to {Path}", _filePath);
        }
    }

    // Добавление карты в избранное
    public static bool Add(int beatmapSetId)
    {
        var favorites = Load();
        if (favorites.Contains(beatmapSetId))
            return false; // Если уже есть, ничего не делаем

        favorites.Add(beatmapSetId);
        Save(favorites);
        Logger?.LogInformation("Beatmap set {SetId} added to favorites", beatmapSetId);
        return true;
    }

    // Удаление карты из избранного
    public static bool Remove(int beatmapSetId)
    {
        var favorites = Load();
        if (!favorites.Contains(beatmapSetId))
            return false; // Если элемента нет, ничего не делаем

        favorites.Remove(beatmapSetId);
        Save(favorites);
        Logger?.LogInformation("Beatmap set {SetId} removed from favorites", beatmapSetId);
        return true;
    }

    // Проверка, находится ли карта в избранном
    public static bool IsFavorite(int beatmapSetId)
    {
        var favorites = Load();
        return favorites.Contains(beatmapSetId);
    }
}
