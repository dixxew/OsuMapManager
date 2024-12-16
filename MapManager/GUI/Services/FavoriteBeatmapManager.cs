using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;

public class FavoriteBeatmapManager
{
    private readonly static string _filePath = "favorites.json";

    // Чтение избранных карт из файла
    public static List<int> Load()
    {
        if (!File.Exists(_filePath))
            return new List<int>();

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при чтении файла: {ex.Message}");
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при записи в файл: {ex.Message}");
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
        return true;
    }

    // Проверка, находится ли карта в избранном
    public static bool IsFavorite(int beatmapSetId)
    {
        var favorites = Load();
        return favorites.Contains(beatmapSetId);
    }
}
