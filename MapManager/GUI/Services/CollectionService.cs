using Avalonia.Platform.Storage;
using DynamicData;
using MapManager.GUI.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MapManager.GUI.Services;

public class CollectionService
{
    private readonly OsuDataService _osuDataService;
    private readonly BeatmapDataService _beatmapDataService;
    private readonly BeatmapDownloadService _beatmapDownloadService;

    public CollectionService(
        OsuDataService osuDataService,
        BeatmapDataService beatmapDataService,
        BeatmapDownloadService beatmapDownloadService)
    {
        _osuDataService = osuDataService;
        _beatmapDataService = beatmapDataService;
        _beatmapDownloadService = beatmapDownloadService;
    }

    public bool AddToCollection(Collection collection, Beatmap beatmap)
    {
        if (collection.Beatmaps.Contains(beatmap))
            return false;

        collection.Beatmaps.Add(beatmap);
        _osuDataService.AddToCollection(collection.Name, beatmap.MD5Hash);
        return true;
    }

    public bool RemoveFromCollection(Collection collection, Beatmap beatmap)
    {
        if (!collection.Beatmaps.Contains(beatmap))
            return false;

        collection.Beatmaps.Remove(beatmap);
        _osuDataService.RemoveFromCollection(collection.Name, beatmap.MD5Hash);
        return true;
    }

    public bool AddCollection(Collection collection, List<Beatmap> beatmaps)
    {
        if (_beatmapDataService.Collections.Any(c => c.Name == collection.Name))
            return false;

        collection.Beatmaps = new(beatmaps);
        _beatmapDataService.Collections.Add(collection);
        _osuDataService.AddCollection(collection.Name, beatmaps.Select(b => b.MD5Hash).ToList());
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
            return (false, []);

        _beatmapDataService.Collections.AddRange(newCollections);
        _osuDataService.AddCollections(osuCollections);
        return (true, allMissing);
    }

    public bool RemoveCollection(Collection collection)
    {
        if (!_beatmapDataService.Collections.Contains(collection))
            return false;

        _beatmapDataService.Collections.Remove(collection);
        _osuDataService.RemoveCollection(collection.Name);
        return true;
    }

    public bool ExportCollection(Collection collection, IStorageFile? filePath)
    {
        if (filePath == null) return false;

        _osuDataService.ExportCollection(new()
        {
            Name = collection.Name,
            BeatmapHashes = collection.Beatmaps.Select(b => b.MD5Hash).ToList()
        }, filePath.Path.LocalPath);

        return true;
    }

    public (bool success, List<MissingBeatmap> missing) ImportCollections(IReadOnlyList<IStorageFile> files)
    {
        if (files == null || files.Count == 0) return (false, []);

        var collectionList = _osuDataService.ImportCollections(files.Select(f => f.Path.LocalPath));
        return AddCollections(collectionList.ToDictionary(c => c.Name, c => c.BeatmapHashes));
    }
}
