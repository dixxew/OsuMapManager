using Avalonia.Threading;
using DynamicData;
using MapManager.GUI.Models;
using Microsoft.Extensions.Hosting;
using OsuParsers.Database.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;
public class AppInitializationService : IHostedService
{
    private readonly OsuDataReader OsuDataReader;
    private readonly RankingService _rankingService;
    private readonly BeatmapDataService _beatmapDataService;

    public AppInitializationService(OsuDataReader osuDataReader, RankingService rankingService, BeatmapDataService beatmapDataService)
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
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _beatmapDataService.PerformSearch("", false);
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }


    private List<Tuple<string, List<OsuParsers.Database.Objects.Score>>> LoadScores()
    {
        return _rankingService.GetAllLocalScores();
    }

    private void LoadCollections()
    {
        var collections = OsuDataReader.GeCollectionsList();
        _beatmapDataService.Collections.AddRange(collections.Select(c => new Models.Collection
        {
            Beatmaps = new(_beatmapDataService.BeatmapSets
                .SelectMany(bs => bs.Beatmaps)
                .Where(b => c.MD5Hashes.Contains(b.MD5Hash))
                .ToList()),
            Name = c.Name,
            Count = c.Count
        }).ToList());
    }

    private async Task LoadBeatmaps(List<Tuple<string, List<OsuParsers.Database.Objects.Score>>> scores)
    {
        // Преобразуем список скорингов в словарь для быстрого поиска
        var scoresDictionary = scores.ToDictionary(s => s.Item1, s => s.Item2);

        List<DbBeatmap> dbBeatmaps = new();
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
                    var beatmap = Beatmap.FromDbBeatmap(b);

                    // Добавляем скоринги, если они есть
                    if (scoresDictionary.TryGetValue(b.MD5Hash, out var beatmapScores))
                    {
                        Beatmap.AddScores(beatmap, beatmapScores);
                    }

                    return beatmap;
                }).ToList()
            };
        }).ToList();
        _beatmapDataService.BeatmapSets.AddRange(list);
        LoadCollections();

    }

}
