using MapManager;
using osu.Shared.Serialization;
using osu_database_reader.BinaryFiles;
using osu_database_reader.Components.Beatmaps;
using osu_database_reader.Components.Player;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class OsuDataService
{
    private readonly AppSettings _appSettings;

    private string osuDirectory;
    private string osuDbPath;
    private string scoresDbPath;
    private string collectionsDbPath;

    private OsuDb _osuDb;
    private CollectionDb _collectionDb;
    private ScoresDb _scoresDb;

    public OsuDataService(AppSettings appSettings)
    {
        _appSettings = appSettings;
        osuDirectory = appSettings.OsuDirectory;
        osuDbPath = Path.Combine(osuDirectory, "osu!.db");
        scoresDbPath = Path.Combine(osuDirectory, "scores.db");
        collectionsDbPath = Path.Combine(osuDirectory, "collection.db");
    }
    public void UpdateSettings(AppSettings updatedSettings)
    {
        // Set new settings
        osuDirectory = updatedSettings.OsuDirectory;
    }

    public List<BeatmapEntry> GetBeatmapList()
    {
        _osuDb = OsuDb.Read(osuDbPath);

        return _osuDb.Beatmaps;
    }

    public List<Collection> GetCollectionsList()
    {
        _collectionDb = CollectionDb.Read(collectionsDbPath);

        return _collectionDb.Collections;
    }
    public List<Replay> GetScoresList()
    {
        _scoresDb = ScoresDb.Read(scoresDbPath);

        return _scoresDb.Scores.ToList();
    }



    public void AddCollection(string name, List<string> md5hashes)
    {
        Task.Run(() =>
        {
            var collection = new Collection()
            {
                Name = name,
                BeatmapHashes = md5hashes,
            };
            _collectionDb.Collections.Add(collection);

            using (var stream = new FileStream(collectionsDbPath, FileMode.Create, FileAccess.Write))
            {
                var writer = new SerializationWriter(stream);
                _collectionDb.WriteToStream(writer);
            }
        });
    }


    public void AddToCollection(string collectionName, string md5)
    {
        Task.Run(() =>
        {
            if (_collectionDb.Collections.First(c => c.Name == collectionName).BeatmapHashes.Contains(md5))
                return;

            _collectionDb.Collections.First(c => c.Name == collectionName).BeatmapHashes.Add(md5);

            using (var stream = new FileStream(collectionsDbPath, FileMode.Create, FileAccess.Write))
            {
                var writer = new SerializationWriter(stream);
                _collectionDb.WriteToStream(writer);
            }
        });
    }
    public void RemoveFromCollection(string collectionName, string md5)
    {
        Task.Run(() =>
        {
            if (!_collectionDb.Collections.First(c => c.Name == collectionName).BeatmapHashes.Contains(md5))
                return;

            _collectionDb.Collections.First(c => c.Name == collectionName).BeatmapHashes.Remove(md5);
            using (var stream = new FileStream(collectionsDbPath, FileMode.Create, FileAccess.Write))
            {
                var writer = new SerializationWriter(stream);
                _collectionDb.WriteToStream(writer);
            }
        });
    }
    public string GetBeatmapImage(string beatmapFolder, string beatmapFileName)
    {
        // Путь к файлу карты
        var beatmapPath = Path.Combine(osuDirectory, "Songs", beatmapFolder, beatmapFileName);

        // Список допустимых расширений изображений
        var imageExtensions = new[] { ".jpg", ".png", ".jpeg" };

        try
        {
            if (File.Exists(beatmapPath))
            {
                // Читаем строки из файла .osu
                var lines = File.ReadAllLines(beatmapPath);

                // Ищем блок [Events] и путь к картинке
                var eventIndex = Array.FindIndex(lines, line => line.Trim() == "[Events]");
                if (eventIndex >= 0)
                {
                    for (int i = eventIndex + 1; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();

                        // Ищем строку с 0,0,"path/to/image.jpg"
                        if (line.StartsWith("0,0,\""))
                        {
                            // Извлекаем путь к картинке
                            var startIndex = line.IndexOf("\"") + 1;
                            var endIndex = line.LastIndexOf("\"");
                            if (startIndex > 0 && endIndex > startIndex)
                            {
                                var relativeImagePath = line.Substring(startIndex, endIndex - startIndex);

                                // Полный путь к картинке
                                var fullImagePath = Path.Combine(osuDirectory, "Songs", beatmapFolder, relativeImagePath);

                                if (File.Exists(fullImagePath))
                                {
                                    return fullImagePath;
                                }
                            }
                        }
                    }
                }
            }

            // Если путь из файла .osu не найден, ищем картинку по названию в папке
            var imageFiles = Directory.GetFiles(Path.Combine(osuDirectory, "Songs", beatmapFolder), "*.*", SearchOption.AllDirectories)
                                      .Where(file => imageExtensions.Contains(Path.GetExtension(file).ToLower()));

            // Находим файл с наибольшим размером
            var largestImage = imageFiles
                .Select(file => new FileInfo(file))
                .OrderByDescending(fileInfo => fileInfo.Length)
                .FirstOrDefault();

            return largestImage?.FullName ?? "GUI/Assets/defaultBg.jpg";
        }
        catch (Exception ex)
        {
            // Логируем ошибки
            Console.WriteLine($"Ошибка при поиске изображения для карты {beatmapFileName}: {ex.Message}");

            // Возвращаем путь к изображению по умолчанию
            return "GUI/Assets/defaultBg.jpg";
        }
    }

}
