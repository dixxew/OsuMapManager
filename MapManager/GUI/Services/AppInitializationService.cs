using Avalonia.Threading;
using OsuParsers.Database.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;
public class AppInitializationService
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

    public void InitializeApplicationData()
    {
        var scores = LoadScores();
        LoadBeatmaps(scores);
        _beatmapDataService.LoadFavoriteBeatmaps();
    }

    private List<Tuple<string, List<OsuParsers.Database.Objects.Score>>> LoadScores()
    {
        return _rankingService.GetAllLocalScores();
    }

    private void LoadCollections()
    {
        //var collections = OsuDataReader.GeCollectionsList();
        //_beatmapDataService.Collections.AddRange(collections.Select(c => new Models.Collection
        //{
        //    Beatmaps = new(_beatmapDataService.BeatmapSets
        //        .SelectMany(bs => bs.Beatmaps)
        //        .Where(b => c.MD5Hashes.Contains(b.MD5Hash))
        //        .ToList()),
        //    Name = c.Name,
        //    Count = c.Count
        //}).ToList());
    }

    private async void LoadBeatmaps(List<Tuple<string, List<OsuParsers.Database.Objects.Score>>> scores)
    {
        //// Преобразуем список скорингов в словарь для быстрого поиска
        //var scoresDictionary = scores.ToDictionary(s => s.Item1, s => s.Item2);

        //List<DbBeatmap> dbBeatmaps = new();
        //await Task.Run(() =>
        //{
        //    dbBeatmaps = OsuDataReader.GetBeatmapList();
        //});

        //var grouped = dbBeatmaps.GroupBy(b => b.BeatmapSetId);

        //Dispatcher.UIThread.Invoke(() =>
        //{
        //    BeatmapSets.AddRange(grouped.Select(g =>
        //    {
        //        var firstBeatmap = g.First();

        //        return new BeatmapSet
        //        {
        //            Id = g.Key,
        //            Title = firstBeatmap.Title,
        //            Artist = firstBeatmap.Artist,
        //            FolderName = firstBeatmap.FolderName,
        //            Beatmaps = g.Select(b =>
        //            {
        //                var beatmap = Beatmap.FromDbBeatmap(b);

        //                // Добавляем скоринги, если они есть
        //                if (scoresDictionary.TryGetValue(b.MD5Hash, out var beatmapScores))
        //                {
        //                    Beatmap.AddScores(beatmap, beatmapScores);
        //                }

        //                return beatmap;
        //            }).ToList()
        //        };
        //    }));

        //    // Устанавливаем FilteredBeatmapSets и случайно выбираем BeatmapSet
        //    FilteredBeatmapSets = BeatmapSets;
        //    SelectedBeatmapSet = FilteredBeatmapSets.ElementAt(Random.Shared.Next(0, FilteredBeatmapSets.Count));
        //    LoadCollections();
        //});
    }
}
