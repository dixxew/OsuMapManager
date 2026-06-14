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

    private string OsuDirectory       => _appSettings.OsuDirectory ?? "";
    private string OsuDbPath          => Path.Combine(OsuDirectory, "osu!.db");
    private string ScoresDbPath       => Path.Combine(OsuDirectory, "scores.db");
    private string CollectionsDbPath  => Path.Combine(OsuDirectory, "collection.db");

    private CollectionDb _collectionDb;

    public OsuDataService(AppSettings appSettings)
    {
        _appSettings = appSettings;
    }

    public List<BeatmapEntry> GetBeatmapList()
    {
        var osuDb = OsuDb.Read(OsuDbPath);
        return osuDb.Beatmaps;
    }

    public List<Collection> GetCollectionsList()
    {
        _collectionDb = CollectionDb.Read(CollectionsDbPath);
        return _collectionDb.Collections;
    }

    public List<Replay> GetScoresList()
    {
        var scoresDb = ScoresDb.Read(ScoresDbPath);
        return scoresDb.Scores.ToList();
    }



    private void BackupCollectionDb()
    {
        if (File.Exists(CollectionsDbPath))
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            File.Copy(CollectionsDbPath, CollectionsDbPath + $".{timestamp}.bak");
        }
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

            BackupCollectionDb();
            using (var stream = new FileStream(CollectionsDbPath, FileMode.Create, FileAccess.Write))
            {
                var writer = new SerializationWriter(stream);
                _collectionDb.WriteToStream(writer);
            }
        });
    }
    public void AddCollections(Dictionary<string, List<string>> collectionsData)
    {
        Task.Run(() =>
        {
            var newCollections = collectionsData
                .Select(entry => new Collection
                {
                    Name = entry.Key,
                    BeatmapHashes = entry.Value
                })
                .ToList();

            _collectionDb.Collections.AddRange(newCollections);

            BackupCollectionDb();
            using var stream = new FileStream(CollectionsDbPath, FileMode.Create, FileAccess.Write);
            var writer = new SerializationWriter(stream);
            _collectionDb.WriteToStream(writer);
        });
    }


    public void AddToCollection(string collectionName, string md5)
    {
        Task.Run(() =>
        {
            if (_collectionDb.Collections.First(c => c.Name == collectionName).BeatmapHashes.Contains(md5))
                return;

            _collectionDb.Collections.First(c => c.Name == collectionName).BeatmapHashes.Add(md5);

            BackupCollectionDb();
            using (var stream = new FileStream(CollectionsDbPath, FileMode.Create, FileAccess.Write))
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
            BackupCollectionDb();
            using (var stream = new FileStream(CollectionsDbPath, FileMode.Create, FileAccess.Write))
            {
                var writer = new SerializationWriter(stream);
                _collectionDb.WriteToStream(writer);
            }
        });
    }
    public void RemoveCollection(string collectionName)
    {
        Task.Run(() =>
        {
            if (!_collectionDb.Collections.Any(c => c.Name == collectionName))
                return;

            _collectionDb.Collections.Remove(_collectionDb.Collections.First(c => c.Name == collectionName));
            BackupCollectionDb();
            using (var stream = new FileStream(CollectionsDbPath, FileMode.Create, FileAccess.Write))
            {
                var writer = new SerializationWriter(stream);
                _collectionDb.WriteToStream(writer);
            }
        });
    }

    public void ExportCollection(Collection collection, string path)
    {
        Task.Run(() =>
        {
            var db = new CollectionDb()
            {
                OsuVersion = _collectionDb.OsuVersion
            };
            db.Collections.Add(collection);
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                var writer = new SerializationWriter(stream);
                db.WriteToStream(writer);
            }
        });
    }

    internal List<Collection> ImportCollections(IEnumerable<string> paths)
    {
        List<Collection> res = new();
        foreach (var path in paths)
            res.AddRange(CollectionDb.Read(path).Collections);

        return res;

    }

    public string GetBeatmapImage(string beatmapFolder, string beatmapFileName)
    {
        // Путь к файлу карты
        var beatmapPath = Path.Combine(OsuDirectory, "Songs", beatmapFolder, beatmapFileName);

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
                                var fullImagePath = Path.Combine(OsuDirectory, "Songs", beatmapFolder, relativeImagePath);

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
            var imageFiles = Directory.GetFiles(Path.Combine(OsuDirectory, "Songs", beatmapFolder), "*.*", SearchOption.AllDirectories)
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
