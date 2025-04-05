using Avalonia.Threading;
using DynamicData;
using MapManager.GUI.Models;
using Microsoft.Extensions.Hosting;
using osu_database_reader.Components.Beatmaps;
using osu_database_reader.Components.Player;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;
public class AppInitializationService : IHostedService
{
    private readonly OsuDataService OsuDataReader;
    private readonly RankingService _rankingService;
    private readonly BeatmapDataService _beatmapDataService;

    public AppInitializationService(OsuDataService osuDataReader, RankingService rankingService, BeatmapDataService beatmapDataService)
    {
        OsuDataReader = osuDataReader;
        _rankingService = rankingService;
        _beatmapDataService = beatmapDataService;
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var scores = LoadScores();
        await LoadBeatmaps(scores);
        _beatmapDataService.LoadFavoriteBeatmaps();
        Dispatcher.UIThread.Post(() =>
        {
            _beatmapDataService.Search();
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }


    private List<Replay> LoadScores()
    {
        return _rankingService.GetAllLocalScores();
    }

    private async Task LoadCollections()
    {
        var collectionList = OsuDataReader.GetCollectionsList();
        var collections = collectionList.Select(c => new Models.Collection
        {
            Beatmaps = new(_beatmapDataService.BeatmapSets
                    .SelectMany(bs => bs.Beatmaps)
                    .Where(b => c.BeatmapHashes.Contains(b.MD5Hash))
                    .ToList()),
            Name = c.Name,
            Count = c.BeatmapHashes.Count
        }).ToList();
        Dispatcher.UIThread.Post(() =>
        {
            _beatmapDataService.Collections.AddRange(collections);
        });
    }

    private async Task LoadBeatmaps(List<Replay> scores)
    {

        List<BeatmapEntry> dbBeatmaps = new();
        await Task.Run(() =>
        {
            dbBeatmaps = OsuDataReader.GetBeatmapList();
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
        await LoadCollections();

    }

}
