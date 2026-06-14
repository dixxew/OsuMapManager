using MapManager.GUI.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;

public class OsuPpsService
{
    // Branch "data" is the CDN branch in the osu-pps repo — path starts with "data/" again (intentional double "data")
    private const string DiffsUrl = "https://raw.githubusercontent.com/grumd/osu-pps/data/data/maps/osu/diffs.csv";
    private const string MapsetsUrl = "https://raw.githubusercontent.com/grumd/osu-pps/data/data/maps/osu/mapsets.csv";

    private static readonly string CacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MapManager");

    private static readonly string DiffsCachePath = Path.Combine(CacheDir, "osu-pps-diffs.csv");
    private static readonly string MapsetsCachePath = Path.Combine(CacheDir, "osu-pps-mapsets.csv");

    private const int CacheHours = 24;

    private readonly BeatmapDataService _beatmapDataService;
    private readonly ILogger<OsuPpsService> _logger;

    private List<OsuPpsEntry> _rankedEntries = [];
    public IReadOnlyList<OsuPpsEntry> RankedEntries => _rankedEntries;

    public OsuPpsService(BeatmapDataService beatmapDataService, ILogger<OsuPpsService> logger)
    {
        _beatmapDataService = beatmapDataService;
        _logger = logger;
    }

    // Called after osu.db reload to update IsLocal without re-downloading CSVs
    public void RefreshLocalIndex()
    {
        if (_rankedEntries.Count == 0) return;
        var idx = BuildLocalIndex();
        foreach (var entry in _rankedEntries)
        {
            idx.TryGetValue(entry.BeatmapId, out var localSet);
            entry.IsLocal = localSet is not null;
            entry.LocalBeatmapSet = localSet;
        }
    }

    public async Task LoadAsync()
    {
        _logger.LogInformation("osu!pps: loading diffs/mapsets CSVs");
        try
        {
            var diffsTask = GetCsvAsync(DiffsUrl, DiffsCachePath);
            var mapsetsTask = GetCsvAsync(MapsetsUrl, MapsetsCachePath);
            await Task.WhenAll(diffsTask, mapsetsTask);

            var mapsets = ParseMapsets(await mapsetsTask);
            var localIndex = BuildLocalIndex();
            _rankedEntries = BuildRankedList(await diffsTask, mapsets, localIndex);
            _logger.LogInformation("osu!pps: loaded {Count} ranked entries", _rankedEntries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "osu!pps: LoadAsync failed");
            throw;
        }
    }

    // beatmapId → local BeatmapSet
    private Dictionary<int, BeatmapSet> BuildLocalIndex()
    {
        var idx = new Dictionary<int, BeatmapSet>();
        foreach (var set in _beatmapDataService.BeatmapSets)
            foreach (var b in set.Beatmaps)
                idx.TryAdd(b.BeatmapId, set);
        return idx;
    }

    private static List<OsuPpsEntry> BuildRankedList(
        string diffsText,
        Dictionary<int, (string artist, string title)> mapsets,
        Dictionary<int, BeatmapSet> localIndex)
    {
        var list = new List<OsuPpsEntry>(capacity: 200_000);

        using var reader = new StringReader(diffsText);
        reader.ReadLine(); // header: m,b,x,pp99,adj,v,s,l,d,p,h,appr_h,...

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0) continue;
            var f = SplitLine(line);
            if (f.Length < 11) continue;

            if (!TryInt(f[0], out int mods)) continue;
            if (!TryInt(f[1], out int beatmapId)) continue;
            if (!TryDouble(f[2], out double farmValue)) continue;
            if (!TryDouble(f[3], out double pp99)) continue;
            if (!TryDouble(f[4], out double adj)) continue;
            string version = f[5].Trim('"');
            if (!TryInt(f[6], out int mapsetId)) continue;
            TryInt(f[7], out int lengthSeconds);
            TryDouble(f[8], out double starRating);
            // f[9]=passCount
            if (!TryDouble(f[10], out double hours)) continue;

            // "ByPopulationAndTime" formula from osu-pps farmValue.ts
            double farmScore = 1000.0 * farmValue
                / Math.Pow(Math.Max(1.0, adj), 0.65)
                / Math.Pow(Math.Max(1.0, hours), 0.35);

            if (farmScore <= 0) continue;

            mapsets.TryGetValue(mapsetId, out var meta);
            localIndex.TryGetValue(beatmapId, out var localSet);

            list.Add(new OsuPpsEntry
            {
                BeatmapId = beatmapId,
                BeatmapSetId = mapsetId,
                Mods = mods,
                Artist = meta.artist ?? "",
                Title = meta.title ?? "",
                Version = version,
                FarmScore = farmScore,
                Pp99 = pp99,
                LengthSeconds = lengthSeconds,
                StarRating = starRating,
                IsLocal = localSet is not null,
                LocalBeatmapSet = localSet,
            });
        }

        list.Sort((a, b) => b.FarmScore.CompareTo(a.FarmScore));
        for (int i = 0; i < list.Count; i++)
            list[i].Rank = i + 1;

        return list;
    }

    // mapsets.csv columns: art,t,bpm,g,ln,s
    private static Dictionary<int, (string artist, string title)> ParseMapsets(string text)
    {
        var dict = new Dictionary<int, (string, string)>(capacity: 50_000);
        using var reader = new StringReader(text);
        reader.ReadLine(); // header
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0) continue;
            var f = SplitLine(line);
            if (f.Length < 6) continue;
            if (!TryInt(f[5], out int setId)) continue;
            dict[setId] = (f[0].Trim('"'), f[1].Trim('"'));
        }
        return dict;
    }

    // Handles double-quoted fields (PapaParse output).
    private static string[] SplitLine(string line)
    {
        var fields = new List<string>(14);
        bool inQuotes = false;
        int start = 0;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"') { inQuotes = !inQuotes; continue; }
            if (c == ',' && !inQuotes)
            {
                fields.Add(line[start..i]);
                start = i + 1;
            }
        }
        fields.Add(line[start..]);
        return [.. fields];
    }

    private static bool TryInt(string s, out int v) =>
        int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v);

    private static bool TryDouble(string s, out double v) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);

    private async Task<string> GetCsvAsync(string url, string cachePath)
    {
        if (File.Exists(cachePath))
        {
            if ((DateTime.UtcNow - File.GetLastWriteTimeUtc(cachePath)).TotalHours < CacheHours)
            {
                _logger.LogDebug("osu!pps: using cached CSV {Path}", cachePath);
                return await File.ReadAllTextAsync(cachePath);
            }
        }

        _logger.LogInformation("osu!pps: downloading CSV from {Url}", url);
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        var text = await client.GetStringAsync(url);

        Directory.CreateDirectory(CacheDir);
        await File.WriteAllTextAsync(cachePath, text);
        _logger.LogDebug("osu!pps: cached {Bytes} bytes to {Path}", text.Length, cachePath);
        return text;
    }
}
