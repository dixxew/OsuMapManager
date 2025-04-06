using Avalonia.Platform.Storage;
using DynamicData;
using MapManager.GUI.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MapManager.GUI.Services;

public class CollectionService
{
    private readonly OsuDataService OsuDataReader;
    private readonly BeatmapDataService _beatmapDataService;
    private readonly OsuDataService _osuDataService;


    public CollectionService(OsuDataService osuDataReader, BeatmapDataService beatmapDataService, OsuDataService osuDataService)
    {
        OsuDataReader = osuDataReader;
        _beatmapDataService = beatmapDataService;
        _osuDataService = osuDataService;
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
        collection.Beatmaps = new (beatmaps);
        _beatmapDataService.Collections.Add(collection);
        _osuDataService.AddCollection(collection.Name, beatmaps.Select(b => b.MD5Hash).ToList());

        return true;
    }

    public bool AddCollections(Dictionary<string, List<string>> collectionsData)
    {
        var existingNames = _beatmapDataService.Collections
            .Select(c => c.Name)
            .ToHashSet();

        var newCollections = new List<Collection>();
        var osuCollections = new Dictionary<string, List<string>>();

        foreach (var (name, hashes) in collectionsData)
        {
            if (existingNames.Contains(name))
                continue;

            var beatmaps = _beatmapDataService.BeatmapSets
                .SelectMany(s => s.Beatmaps)
                .Where(b => hashes.Contains(b.MD5Hash))
                .ToList();

            var collection = new Collection
            {
                Name = name,
                Beatmaps = new ObservableCollection<Models.Beatmap>(beatmaps),
                Count = beatmaps.Count
            };

            newCollections.Add(collection);
            osuCollections[name] = hashes;
        }

        if (newCollections.Count == 0)
            return false;

        _beatmapDataService.Collections.AddRange(newCollections);
        _osuDataService.AddCollections(osuCollections);

        return true;
    }



    public bool RemoveCollection(Collection collection)
    {
        if (_beatmapDataService.Collections.Contains(collection))
        {
            _beatmapDataService.Collections.Remove(collection);
            _osuDataService.RemoveCollection(collection.Name);
            return true;
        }

        return false;
    }


    public bool ExportCollection(Collection collection, IStorageFile? filePath)
    {
        if (filePath == null)
            return false;

        _osuDataService.ExportCollection(new()
        {
            Name = collection.Name,
            BeatmapHashes = collection.Beatmaps.Select(b => b.MD5Hash).ToList()
        }, filePath.Path.LocalPath);

        return true;
    }


    public bool ImportCollections(IReadOnlyList<IStorageFile> files)
    {
        if (files == null || files.Count == 0)
            return false;

        var collectionList = _osuDataService.ImportCollections(files.Select(f => f.Path.LocalPath));
        return AddCollections(collectionList.ToDictionary(c => c.Name, c => c.BeatmapHashes));
    }

}