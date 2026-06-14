using Avalonia.Threading;
using DynamicData;
using MapManager.GUI.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using osu_database_reader.Components.Beatmaps;
using osu_database_reader.Components.Player;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;

public class AppInitializationService : IHostedService
{
    private readonly OsuDataService _osuDataReader;
    private readonly RankingService _rankingService;
    private readonly BeatmapDataService _beatmapDataService;
    private readonly ChatService _chatService;
    private readonly AppStartupGate _gate;
    private readonly BeatmapDownloadService _beatmapDownloadService;
    private readonly OsuPpsService _osuPpsService;
    private readonly ILogger<AppInitializationService> _logger;

    public AppInitializationService(
        OsuDataService osuDataReader,
        RankingService rankingService,
        BeatmapDataService beatmapDataService,
        ThumbnailService _,
        ChatService chatService,
        NotificationService __,
        AppStartupGate gate,
        BeatmapDownloadService beatmapDownloadService,
        OsuPpsService osuPpsService,
        ILogger<AppInitializationService> logger)
    {
        _osuDataReader = osuDataReader;
        _rankingService = rankingService;
        _beatmapDataService = beatmapDataService;
        _chatService = chatService;
        _gate = gate;
        _beatmapDownloadService = beatmapDownloadService;
        _osuPpsService = osuPpsService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("App initialization: waiting for startup gate");
        await _gate.WhenReady;
        _logger.LogInformation("Startup gate open — beginning initial load");

        var sw = Stopwatch.StartNew();
        try
        {
            _chatService.Start();

            var scores = LoadScores();
            await LoadBeatmaps(scores);
            _beatmapDataService.LoadFavoriteBeatmaps();
            Dispatcher.UIThread.Post(() =>
            {
                _beatmapDataService.Search();
            });

            GC.Collect(2, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced);

            _logger.LogInformation("Initial load complete in {Elapsed}", sw.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initial load failed after {Elapsed}", sw.Elapsed);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AppInitializationService stopping");
        return Task.CompletedTask;
    }

    // Called from SettingsViewModel when user clicks "Синхронизировать с osu!"
    public async Task ReloadAsync()
    {
        _logger.LogInformation("ReloadAsync: re-syncing with osu!");
        var sw = Stopwatch.StartNew();
        try
        {
            // Await Reset so BeatmapSets.Clear() finishes before background load starts
            await Dispatcher.UIThread.InvokeAsync(() => _beatmapDataService.Reset());

            var scores = LoadScores();
            await LoadBeatmaps(scores);
            _beatmapDataService.LoadFavoriteBeatmaps();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _beatmapDataService.Search();
                _beatmapDataService.OnBeatmapsReloaded?.Invoke();
            });

            GC.Collect(2, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced);

            _logger.LogInformation("ReloadAsync complete in {Elapsed}", sw.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReloadAsync failed after {Elapsed}", sw.Elapsed);
        }
    }

    private List<Replay> LoadScores() => _rankingService.GetAllLocalScores();

    private async Task LoadCollections()
    {
        var collectionList = _osuDataReader.GetCollectionsList();

        var hashIndex = _beatmapDataService.BeatmapSets
            .SelectMany(bs => bs.Beatmaps)
            .Where(b => b.MD5Hash != null)
            .ToDictionary(b => b.MD5Hash);

        var collections = new List<Models.Collection>();
        var missingCount = 0;

        foreach (var c in collectionList)
        {
            var found = c.BeatmapHashes
                .Where(h => hashIndex.ContainsKey(h))
                .Select(h => hashIndex[h])
                .ToList();

            var collection = new Models.Collection
            {
                Beatmaps = new(found),
                Name = c.Name,
                Count = c.BeatmapHashes.Count
            };

            foreach (var hash in c.BeatmapHashes.Where(h => !hashIndex.ContainsKey(h)))
            {
                var missing = new MissingBeatmap { MD5Hash = hash };
                collection.MissingBeatmaps.Add(missing);
                _beatmapDownloadService.RegisterForLookup(missing);
                missingCount++;
            }

            collections.Add(collection);
        }

        _logger.LogInformation("Loaded {Collections} collections ({Missing} missing beatmaps queued for lookup)",
            collections.Count, missingCount);

        Dispatcher.UIThread.Post(() =>
        {
            _beatmapDataService.Collections.AddRange(collections);
        });
    }

    private async Task LoadBeatmaps(List<Replay> scores)
    {
        _logger.LogDebug("LoadBeatmaps: {ScoreCount} local scores", scores.Count);
        List<BeatmapEntry> dbBeatmaps = new();
        await Task.Run(() =>
        {
            dbBeatmaps = _osuDataReader.GetBeatmapList();
        });

        var grouped = dbBeatmaps.GroupBy(b => b.BeatmapSetId);
        var list = grouped.Select(g =>
        {
            var firstBeatmap = g.First();
            return new BeatmapSet
            {
                Id = g.Key,
                Title = firstBeatmap.Title,
                Artist = firstBeatmap.Artist,
                FolderName = firstBeatmap.FolderName,
                Beatmaps = g.Select(b =>
                {
                    var beatmap = Beatmap.FromBeatmapEntry(b);
                    Beatmap.AddReplays(beatmap, scores.Where(s => s.BeatmapHash == b.BeatmapChecksum).ToList());
                    return beatmap;
                }).ToList()
            };
        }).ToList();

        _beatmapDataService.BeatmapSets.AddRange(list);
        _logger.LogInformation("Loaded {Sets} beatmap sets ({Maps} beatmaps total)",
            list.Count, list.Sum(s => s.Beatmaps.Count));
        await LoadCollections();
    }
}
