using MapManager;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<OsuDataService> _logger;

    private string OsuDirectory       => _appSettings.OsuDirectory ?? "";
    private string OsuDbPath          => Path.Combine(OsuDirectory, "osu!.db");
    private string ScoresDbPath       => Path.Combine(OsuDirectory, "scores.db");
    private string CollectionsDbPath  => Path.Combine(OsuDirectory, "collection.db");

    private CollectionDb _collectionDb;

    public OsuDataService(AppSettings appSettings, ILogger<OsuDataService> logger)
    {
        _appSettings = appSettings;
        _logger = logger;
        _logger.LogInformation("OsuDataService initialized (osu! dir: {Dir})", OsuDirectory);
    }

    public List<BeatmapEntry> GetBeatmapList()
    {
        _logger.LogInformation("Reading osu!.db from {Path}", OsuDbPath);
        try
        {
            var osuDb = OsuDb.Read(OsuDbPath);
            _logger.LogInformation("osu!.db read: {Count} beatmaps", osuDb.Beatmaps.Count);
            return osuDb.Beatmaps;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read osu!.db from {Path}", OsuDbPath);
            throw;
        }
    }

    public List<Collection> GetCollectionsList()
    {
        _logger.LogInformation("Reading collection.db from {Path}", CollectionsDbPath);
        try
        {
            _collectionDb = CollectionDb.Read(CollectionsDbPath);
            _logger.LogInformation("collection.db read: {Count} collections", _collectionDb.Collections.Count);
            return _collectionDb.Collections;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read collection.db from {Path}", CollectionsDbPath);
            throw;
        }
    }

    public List<Replay> GetScoresList()
    {
        _logger.LogInformation("Reading scores.db from {Path}", ScoresDbPath);
        try
        {
            var scoresDb = ScoresDb.Read(ScoresDbPath);
            var list = scoresDb.Scores.ToList();
            _logger.LogInformation("scores.db read: {Count} score entries", list.Count);
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read scores.db from {Path}", ScoresDbPath);
            throw;
        }
    }



    private void BackupCollectionDb()
    {
        if (File.Exists(CollectionsDbPath))
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var backupPath = CollectionsDbPath + $".{timestamp}.bak";
            File.Copy(CollectionsDbPath, backupPath);
            _logger.LogDebug("collection.db backed up to {Path}", backupPath);
        }
    }

    private void WriteCollectionDb()
    {
        BackupCollectionDb();
        using var stream = new FileStream(CollectionsDbPath, FileMode.Create, FileAccess.Write);
        var writer = new SerializationWriter(stream);
        _collectionDb.WriteToStream(writer);
        _logger.LogInformation("collection.db written: {Count} collections", _collectionDb.Collections.Count);
    }

    public void AddCollection(string name, List<string> md5hashes)
    {
        _logger.LogInformation("AddCollection '{Name}' ({Count} hashes)", name, md5hashes.Count);
        Task.Run(() =>
        {
            try
            {
                var collection = new Collection()
                {
                    Name = name,
                    BeatmapHashes = md5hashes,
                };
                _collectionDb.Collections.Add(collection);
                WriteCollectionDb();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddCollection '{Name}' failed", name);
            }
        });
    }
    public void AddCollections(Dictionary<string, List<string>> collectionsData)
    {
        _logger.LogInformation("AddCollections ({Count} collections)", collectionsData.Count);
        Task.Run(() =>
        {
            try
            {
                var newCollections = collectionsData
                    .Select(entry => new Collection
                    {
                        Name = entry.Key,
                        BeatmapHashes = entry.Value
                    })
                    .ToList();

                _collectionDb.Collections.AddRange(newCollections);
                WriteCollectionDb();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddCollections failed");
            }
        });
    }


    public void AddToCollection(string collectionName, string md5)
    {
        Task.Run(() =>
        {
            try
            {
                if (_collectionDb.Collections.First(c => c.Name == collectionName).BeatmapHashes.Contains(md5))
                {
                    _logger.LogDebug("AddToCollection '{Name}': {Md5} already present, skipping", collectionName, md5);
                    return;
                }

                _collectionDb.Collections.First(c => c.Name == collectionName).BeatmapHashes.Add(md5);
                _logger.LogInformation("AddToCollection '{Name}': added {Md5}", collectionName, md5);
                WriteCollectionDb();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddToCollection '{Name}' failed for {Md5}", collectionName, md5);
            }
        });
    }
    public void RemoveFromCollection(string collectionName, string md5)
    {
        Task.Run(() =>
        {
            try
            {
                if (!_collectionDb.Collections.First(c => c.Name == collectionName).BeatmapHashes.Contains(md5))
                {
                    _logger.LogDebug("RemoveFromCollection '{Name}': {Md5} not present, skipping", collectionName, md5);
                    return;
                }

                _collectionDb.Collections.First(c => c.Name == collectionName).BeatmapHashes.Remove(md5);
                _logger.LogInformation("RemoveFromCollection '{Name}': removed {Md5}", collectionName, md5);
                WriteCollectionDb();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RemoveFromCollection '{Name}' failed for {Md5}", collectionName, md5);
            }
        });
    }
    public void RemoveCollection(string collectionName)
    {
        Task.Run(() =>
        {
            try
            {
                if (!_collectionDb.Collections.Any(c => c.Name == collectionName))
                {
                    _logger.LogDebug("RemoveCollection '{Name}': not found, skipping", collectionName);
                    return;
                }

                _collectionDb.Collections.Remove(_collectionDb.Collections.First(c => c.Name == collectionName));
                _logger.LogInformation("RemoveCollection '{Name}'", collectionName);
                WriteCollectionDb();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RemoveCollection '{Name}' failed", collectionName);
            }
        });
    }

    public void ExportCollection(Collection collection, string path)
    {
        _logger.LogInformation("ExportCollection '{Name}' → {Path}", collection.Name, path);
        Task.Run(() =>
        {
            try
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
                _logger.LogInformation("ExportCollection '{Name}' written to {Path}", collection.Name, path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExportCollection '{Name}' failed", collection.Name);
            }
        });
    }

    internal List<Collection> ImportCollections(IEnumerable<string> paths)
    {
        List<Collection> res = new();
        foreach (var path in paths)
        {
            try
            {
                var collections = CollectionDb.Read(path).Collections;
                _logger.LogInformation("Imported {Count} collection(s) from {Path}", collections.Count, path);
                res.AddRange(collections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import collections from {Path}", path);
            }
        }

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
            _logger.LogWarning(ex, "Failed to resolve image for beatmap {File} (folder {Folder})", beatmapFileName, beatmapFolder);

            // Возвращаем путь к изображению по умолчанию
            return "GUI/Assets/defaultBg.jpg";
        }
    }

}
