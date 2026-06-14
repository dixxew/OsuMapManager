using Avalonia.Platform.Storage;
using DynamicData;
using MapManager.GUI.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MapManager.GUI.Services;

public class CollectionService
{
    private readonly OsuDataService _osuDataService;
    private readonly BeatmapDataService _beatmapDataService;
    private readonly BeatmapDownloadService _beatmapDownloadService;
    private readonly ILogger<CollectionService> _logger;

    public CollectionService(
        OsuDataService osuDataService,
        BeatmapDataService beatmapDataService,
        BeatmapDownloadService beatmapDownloadService,
        ILogger<CollectionService> logger)
    {
        _osuDataService = osuDataService;
        _beatmapDataService = beatmapDataService;
        _beatmapDownloadService = beatmapDownloadService;
        _logger = logger;
    }

    public bool AddToCollection(Collection collection, Beatmap beatmap)
    {
        if (collection.Beatmaps.Contains(beatmap))
        {
            _logger.LogDebug("AddToCollection '{Name}': beatmap {Id} already present", collection.Name, beatmap.BeatmapId);
            return false;
        }

        collection.Beatmaps.Add(beatmap);
        _osuDataService.AddToCollection(collection.Name, beatmap.MD5Hash);
        _logger.LogInformation("Added beatmap {Id} to collection '{Name}'", beatmap.BeatmapId, collection.Name);
        return true;
    }

    public bool RemoveFromCollection(Collection collection, Beatmap beatmap)
    {
        if (!collection.Beatmaps.Contains(beatmap))
        {
            _logger.LogDebug("RemoveFromCollection '{Name}': beatmap {Id} not present", collection.Name, beatmap.BeatmapId);
            return false;
        }

        collection.Beatmaps.Remove(beatmap);
        _osuDataService.RemoveFromCollection(collection.Name, beatmap.MD5Hash);
        _logger.LogInformation("Removed beatmap {Id} from collection '{Name}'", beatmap.BeatmapId, collection.Name);
        return true;
    }

    public bool AddCollection(Collection collection, List<Beatmap> beatmaps)
    {
        if (_beatmapDataService.Collections.Any(c => c.Name == collection.Name))
        {
            _logger.LogDebug("AddCollection '{Name}': name already exists", collection.Name);
            return false;
        }

        collection.Beatmaps = new(beatmaps);
        _beatmapDataService.Collections.Add(collection);
        _osuDataService.AddCollection(collection.Name, beatmaps.Select(b => b.MD5Hash).ToList());
        _logger.LogInformation("Created collection '{Name}' with {Count} beatmaps", collection.Name, beatmaps.Count);
        return true;
    }

    public (bool success, List<MissingBeatmap> missing) AddCollections(Dictionary<string, List<string>> collectionsData)
    {
        var existingNames = _beatmapDataService.Collections
            .Select(c => c.Name)
            .ToHashSet();

        var hashIndex = _beatmapDataService.BeatmapSets
            .SelectMany(s => s.Beatmaps)
            .Where(b => b.MD5Hash != null)
            .ToDictionary(b => b.MD5Hash);

        var newCollections = new List<Collection>();
        var osuCollections = new Dictionary<string, List<string>>();
        var allMissing = new List<MissingBeatmap>();

        foreach (var (name, hashes) in collectionsData)
        {
            if (existingNames.Contains(name)) continue;

            var found = hashes
                .Where(h => hashIndex.ContainsKey(h))
                .Select(h => hashIndex[h])
                .ToList();

            var collection = new Collection
            {
                Name = name,
                Count = hashes.Count,
                Beatmaps = new ObservableCollection<Beatmap>(found),
            };

            foreach (var hash in hashes.Where(h => !hashIndex.ContainsKey(h)))
            {
                var missing = new MissingBeatmap { MD5Hash = hash };
                collection.MissingBeatmaps.Add(missing);
                _beatmapDownloadService.RegisterForLookup(missing);
                allMissing.Add(missing);
            }

            newCollections.Add(collection);
            osuCollections[name] = hashes;
        }

        if (newCollections.Count == 0)
        {
            _logger.LogDebug("AddCollections: nothing new to add");
            return (false, []);
        }

        _beatmapDataService.Collections.AddRange(newCollections);
        _osuDataService.AddCollections(osuCollections);
        _logger.LogInformation("Added {Count} collections ({Missing} missing beatmaps)", newCollections.Count, allMissing.Count);
        return (true, allMissing);
    }

    public bool RemoveCollection(Collection collection)
    {
        if (!_beatmapDataService.Collections.Contains(collection))
        {
            _logger.LogDebug("RemoveCollection '{Name}': not found", collection.Name);
            return false;
        }

        _beatmapDataService.Collections.Remove(collection);
        _osuDataService.RemoveCollection(collection.Name);
        _logger.LogInformation("Removed collection '{Name}'", collection.Name);
        return true;
    }

    public bool ExportCollection(Collection collection, IStorageFile? filePath)
    {
        if (filePath == null)
        {
            _logger.LogDebug("ExportCollection '{Name}': no file path", collection.Name);
            return false;
        }

        _osuDataService.ExportCollection(new()
        {
            Name = collection.Name,
            BeatmapHashes = collection.Beatmaps.Select(b => b.MD5Hash).ToList()
        }, filePath.Path.LocalPath);

        _logger.LogInformation("Exporting collection '{Name}' to {Path}", collection.Name, filePath.Path.LocalPath);
        return true;
    }

    public (bool success, List<MissingBeatmap> missing) ImportCollections(IReadOnlyList<IStorageFile> files)
    {
        if (files == null || files.Count == 0)
        {
            _logger.LogDebug("ImportCollections: no files provided");
            return (false, []);
        }

        _logger.LogInformation("Importing collections from {Count} file(s)", files.Count);
        var collectionList = _osuDataService.ImportCollections(files.Select(f => f.Path.LocalPath));
        return AddCollections(collectionList.ToDictionary(c => c.Name, c => c.BeatmapHashes));
    }
}
