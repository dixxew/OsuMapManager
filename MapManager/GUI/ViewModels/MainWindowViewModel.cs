using Avalonia.Media.Imaging;
using Avalonia.Threading;
using DynamicData;
using MapManager.GUI.Models;
using OsuParsers.Database.Objects;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MapManager.GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        OsuDataReader = new();
    }

    private OsuDataReader OsuDataReader;

    #region private props
    private BeatmapSet _selectedBeatmapSet;
    private Bitmap _mapBackground;
    private Beatmap _selectedBeatmap;
    #endregion


    private string _searchText;
    private ObservableCollection<BeatmapSet> filteredBeatmaps;

    public long BeatmapsCount { get; set; } = 0;

    public ObservableCollection<BeatmapSet> FilteredBeatmaps
    {
        get => filteredBeatmaps;
        set
        {
            this.RaiseAndSetIfChanged(ref filteredBeatmaps, value);
            BeatmapsCount = FilteredBeatmaps.Sum(set => set.Beatmaps?.Count ?? 0);
            this.RaisePropertyChanged(nameof(BeatmapsCount));
        }
    }
    public string SearchText
    {
        get => _searchText;
        set
        {
            this.RaiseAndSetIfChanged(ref _searchText, value);
            PerformSearch();
        }
    }


    #region public props
    public Beatmap SelectedBeatmap
    {
        get => _selectedBeatmap;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedBeatmap, value); 
            if (value != null)
                SelectedBeatmapChanged();
        }
    }

    public Bitmap MapBackground
    {
        get => _mapBackground;
        set => this.RaiseAndSetIfChanged(ref _mapBackground, value);
    }

    public BeatmapSet SelectedBeatmapSet
    {
        get => _selectedBeatmapSet;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedBeatmapSet, value);
            if (value != null)
                SelectedBeatmapSetChanged();
        }
    }
    public ObservableCollection<BeatmapSet> BeatmapSets { get; set; } = new();
    #endregion



    public void OnMainWindowLoaded()
    {
        LoadBeatmaps();
    }

    private async void LoadBeatmaps()
    {
        List<DbBeatmap> dbBeatmaps = new();
        await Task.Run(() =>
        {
            dbBeatmaps = OsuDataReader.GetBeatmapList();
        });
        var grouped = dbBeatmaps.GroupBy(b => b.BeatmapSetId);
        var beatmapSets = new List<BeatmapSet>();

        Dispatcher.UIThread.Invoke(() =>
        {
            BeatmapSets.AddRange(grouped.Select(g =>
            {
                var firstBeatmap = g.First();

                return new BeatmapSet
                {
                    Id = g.Key,
                    Title = firstBeatmap.Title,
                    Artist = firstBeatmap.Artist,
                    FolderName = firstBeatmap.FolderName,
                    Beatmaps = g.Select(b => Beatmap.FromDbBeatmap(b)).ToList()
                };
            }));
            FilteredBeatmaps = BeatmapSets;
            SelectedBeatmapSet = FilteredBeatmaps.ElementAt(Random.Shared.Next(0, FilteredBeatmaps.Count));
        });
    }

    private void SelectedBeatmapSetChanged()
    {
        MapBackground = new Bitmap(OsuDataReader.GetBeatmapImage(SelectedBeatmapSet.Beatmaps.First().FolderName));
        SelectedBeatmap = SelectedBeatmapSet.Beatmaps.First();
        AppStore.AudioPlayerVM.SetSongAndPlay(Path.Combine(AppStore.OsuDirectory, "Songs", SelectedBeatmap.FolderName, SelectedBeatmap.AudioFileName));
    }
    private void SelectedBeatmapChanged()
    {
    }

    private void PerformSearch()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            // Если строка поиска пустая, показываем все BeatmapSets
            FilteredBeatmaps = new ObservableCollection<BeatmapSet>(BeatmapSets);
        }
        else
        {
            var query = SearchText.ToLower();

            // Фильтруем BeatmapSets по условию, чтобы хотя бы один Beatmap соответствовал поиску
            var results = BeatmapSets
                .Where(set => set.Beatmaps.Any(b =>
                    (b.Artist != null && b.Artist.ToLower().Contains(query)) ||
                    (b.Creator != null && b.Creator.ToLower().Contains(query)) ||
                    (b.Tags != null && b.Tags.ToLower().Contains(query)) ||
                    (b.Title != null && b.Title.ToLower().Contains(query))))
                .ToList();

            // Создаём новый ObservableCollection только с подходящими BeatmapSets
            FilteredBeatmaps = new ObservableCollection<BeatmapSet>(results);
        }
    }

    public void SelectNextBeatmapSet()
    {
        if (FilteredBeatmaps == null || FilteredBeatmaps.Count == 0)
            return;

        var currIndex = FilteredBeatmaps.IndexOf(SelectedBeatmapSet);

        // Если текущий элемент последний, выбираем первый
        if (currIndex == BeatmapSets.Count - 1)
        {
            SelectedBeatmapSet = FilteredBeatmaps.First();
        }
        else
        {
            SelectedBeatmapSet = FilteredBeatmaps.ElementAt(currIndex + 1);
        }
    }

    public void SelectPrevBeatmapSet()
    {
        if (FilteredBeatmaps == null || FilteredBeatmaps.Count == 0)
            return;

        var currIndex = FilteredBeatmaps.IndexOf(SelectedBeatmapSet);

        // Если текущий элемент первый, выбираем последний
        if (currIndex == 0 || currIndex == -1)
        {
            SelectedBeatmapSet = FilteredBeatmaps.Last();
        }
        else
        {
            SelectedBeatmapSet = FilteredBeatmaps.ElementAt(currIndex - 1);
        }
    }

    public void SelectRandomBeatmapSet()
    {
        if (FilteredBeatmaps != null || FilteredBeatmaps.Count != 0)
            SelectedBeatmapSet = FilteredBeatmaps.ElementAt(Random.Shared.Next(0, FilteredBeatmaps.Count));
    }

}
