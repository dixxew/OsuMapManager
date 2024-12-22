using AutoMapper;
using Avalonia.Controls.Primitives;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using DynamicData;
using MapManager.GUI.Models;
using MapManager.GUI.Services;
using MapManager.OSU;
using OsuParsers.Database.Objects;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MapManager.GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{

    private readonly OsuDataReader OsuDataReader;
    private readonly OsuService _osuService;
    private readonly AudioPlayerViewModel _audioPlayerVM;
    private readonly SettingsViewModel _settingsVM;
    private readonly Mapper _mapper;

    public MainWindowViewModel(OsuService osuService, AudioPlayerViewModel audioPlayerVM, SettingsViewModel settingsVM, OsuDataReader osuDataReader, Mapper mapper)
    {
        _osuService = osuService;
        _audioPlayerVM = audioPlayerVM;
        _audioPlayerVM.ToggleFavorite += ToggleFavorite;
        _settingsVM = settingsVM;
        OsuDataReader = osuDataReader;
        _mapper = mapper;

        _audioPlayerVM.NextMapSetRequested += SelectNextBeatmapSet;
        _audioPlayerVM.PrevMapSetRequested += SelectPrevBeatmapSet;
        _audioPlayerVM.RandomMapSetRequested += SelectRandomBeatmapSet;
    }

    #region private props
    private string _searchBeatmapSetText;
    private ObservableCollection<BeatmapSet> _filteredBeatmapSets;
    private int _selectedRightTabIndex;
    private BeatmapSet _selectedBeatmapSet;
    private Bitmap _mapBackground;
    private Beatmap _selectedBeatmap;
    private bool _isGlobalRankingsLoadingVisible;
    private string _searchComboboxSelectedDestination = "Local";
    private bool _isLocalBeatmapsVisible;
    private bool _isGlobalBeatmapsVisible;
    private string _searchCollectionText;
    private long _collectionsCount;
    private ObservableCollection<int> _favoriteBeatmaps;
    private bool _isShowOnlyFavorites;
    private int _selectedMainTab;
    private ObservableCollection<Models.Collection> _selectedBeatmapCollections;
    private int _selectedBeatmapCollectionsCount;
    #endregion




    #region public props
    public string SelectedBeatmapCollectionsCount
        => $"Collections {SelectedBeatmapCollections?.Count ?? 0}";

    public ObservableCollection<Models.Collection> SelectedBeatmapCollections
    {
        get => _selectedBeatmapCollections;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedBeatmapCollections, value);
            _selectedBeatmapCollections.CollectionChanged += OnBeatmapSelectedCollectionsChanges;
            if (value != null)
                this.RaisePropertyChanged(nameof(SelectedBeatmapCollectionsCount));
        }
    }

    public int SelectedMainTab
    {
        get => _selectedMainTab;
        set => this.RaiseAndSetIfChanged(ref _selectedMainTab, value);
    }

    public bool IsShowOnlyFavorites
    {
        get => _isShowOnlyFavorites;
        set
        {
            this.RaiseAndSetIfChanged(ref _isShowOnlyFavorites, value);
            PerformSearch();
        }
    }

    public ObservableCollection<int> FavoriteBeatmapSets
    {
        get => _favoriteBeatmaps;
        set => this.RaiseAndSetIfChanged(ref _favoriteBeatmaps, value);
    }

    public long CollectionsCount
    {
        get => _collectionsCount;
        set => this.RaiseAndSetIfChanged(ref _collectionsCount, value);
    }

    public string SearchCollectionText
    {
        get => _searchCollectionText;
        set => this.RaiseAndSetIfChanged(ref _searchCollectionText, value);
    }

    public bool IsGlobalBeatmapsVisible
    {
        get => _isGlobalBeatmapsVisible;
        set => this.RaiseAndSetIfChanged(ref _isGlobalBeatmapsVisible, value);
    }

    public bool IsLocalBeatmapsVisible
    {
        get => _isLocalBeatmapsVisible;
        set => this.RaiseAndSetIfChanged(ref _isLocalBeatmapsVisible, value);
    }

    public string SearchComboboxSelectedDestination
    {
        get => _searchComboboxSelectedDestination;
        set
        {
            this.RaiseAndSetIfChanged(ref _searchComboboxSelectedDestination, value);
            SearchComboboxDestinationChanged();
        }
    }

    public bool IsGlobalRankingsLoadingVisible
    {
        get => _isGlobalRankingsLoadingVisible;
        set => this.RaiseAndSetIfChanged(ref _isGlobalRankingsLoadingVisible, value);
    }

    public long BeatmapsCount { get; set; } = 0;

    public string SearchBeatmapSetText
    {
        get => _searchBeatmapSetText;
        set
        {
            this.RaiseAndSetIfChanged(ref _searchBeatmapSetText, value);
            PerformSearch();
        }
    }

    public int SelectedRightTabIndex
    {
        get => _selectedRightTabIndex;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedRightTabIndex, value);
            UpdateRightTab();
        }
    }

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
            if (value == null || (_selectedBeatmapSet != null && value.Id == _selectedBeatmapSet.Id && FilteredBeatmapSets.Count > 1))
                return;

            this.RaiseAndSetIfChanged(ref _selectedBeatmapSet, value);
            SelectedBeatmapSetChanged();
        }
    }

    public ObservableCollection<BeatmapSet> BeatmapSets { get; set; } = new();

    public ObservableCollection<BeatmapSet> FilteredBeatmapSets
    {
        get => _filteredBeatmapSets;
        set
        {
            this.RaiseAndSetIfChanged(ref _filteredBeatmapSets, value);
            BeatmapsCount = FilteredBeatmapSets.Sum(set => set.Beatmaps?.Count ?? 0);
            this.RaisePropertyChanged(nameof(BeatmapsCount));
        }
    }

    public ObservableCollection<GlobalScore> GlobalScores { get; set; } = new();

    public ObservableCollection<Models.Collection> Collections { get; set; } = new();
    #endregion


    private void SearchComboboxDestinationChanged()
    {
        switch (SearchComboboxSelectedDestination)
        {
            case "Local":
                {
                    IsLocalBeatmapsVisible = true;
                    IsGlobalBeatmapsVisible = false;
                    break;
                }
            case "Global":
                {
                    IsLocalBeatmapsVisible = false;
                    IsGlobalBeatmapsVisible = true;

                    break;
                }

        }
    }

    private void UpdateRightTab()
    {
        switch (SelectedRightTabIndex)
        {
            case 0: //LocalRanks
                {
                    break;
                }
            case 1: //GlobalRanks
                {
                    GlobalScores.Clear();
                    IsGlobalRankingsLoadingVisible = true;
                    Task.Run(async () =>
                    {
                        if (SelectedBeatmap != null)
                        {
                            var scores = await _osuService.GetBeatmapScoresByIdAsync(SelectedBeatmap.BeatmapId);
                            GlobalScores.AddRange(scores.Scores.Select(s => _mapper.Map<GlobalScore>(s)).ToList());
                            IsGlobalRankingsLoadingVisible = false;
                        }
                    });
                    break;
                }
            case 2: //MapComments
                {
                    GlobalScores.Clear();
                    Task.Run(async () =>
                    {
                        if (SelectedBeatmap != null)
                        {
                            var scores = await _osuService.GetBeatmapScoresByIdAsync(SelectedBeatmap.BeatmapId);
                            GlobalScores.AddRange(scores.Scores.Select(s => _mapper.Map<GlobalScore>(s)).ToList());
                        }
                    });
                    break;
                }
        }
    }

    private async void LoadBeatmaps(List<Tuple<string, List<OsuParsers.Database.Objects.Score>>> scores)
    {
        // Преобразуем список скорингов в словарь для быстрого поиска
        var scoresDictionary = scores.ToDictionary(s => s.Item1, s => s.Item2);

        List<DbBeatmap> dbBeatmaps = new();
        await Task.Run(() =>
        {
            dbBeatmaps = OsuDataReader.GetBeatmapList();
        });

        var grouped = dbBeatmaps.GroupBy(b => b.BeatmapSetId);

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
            }));

            // Устанавливаем FilteredBeatmapSets и случайно выбираем BeatmapSet
            FilteredBeatmapSets = BeatmapSets;
            SelectedBeatmapSet = FilteredBeatmapSets.ElementAt(Random.Shared.Next(0, FilteredBeatmapSets.Count));
            LoadCollections();
        });
    }

    private void LoadCollections()
    {
        var collections = OsuDataReader.GeCollectionsList();
        Collections.AddRange(collections.Select(c => new Models.Collection
        {
            Beatmaps = new(BeatmapSets
                .SelectMany(bs => bs.Beatmaps)
                .Where(b => c.MD5Hashes.Contains(b.MD5Hash))
                .ToList()),
            Name = c.Name,
            Count = c.Count
        }).ToList());
    }

    private void LoadFavoriteBeatmaps()
    {
        FavoriteBeatmapSets = new ObservableCollection<int>();
        var list = FavoriteBeatmapManager.Load();
        FavoriteBeatmapSets.AddRange(list);
    }

    private void UpdateFavoriteBeatmapSets(bool isAdded)
    {
        if (isAdded)
        {
            FavoriteBeatmapSets.Add(SelectedBeatmapSet.Id);
            FavoriteBeatmapManager.Add(SelectedBeatmapSet.Id);
        }
        else
        {
            FavoriteBeatmapSets.Remove(SelectedBeatmapSet.Id);
            FavoriteBeatmapManager.Remove(SelectedBeatmapSet.Id);
        }
    }

    private List<Tuple<string, List<OsuParsers.Database.Objects.Score>>> LoadScores()
    {
        return OsuDataReader.GetScoresList();

    }

    private void SelectedBeatmapSetChanged()
    {
        SelectedBeatmap = SelectedBeatmapSet.Beatmaps.FirstOrDefault();


        if (SelectedBeatmap != null)
        {
            _audioPlayerVM.SetSongAndPlay(Path.Combine(_settingsVM.OsuDirPath, "Songs", SelectedBeatmap.FolderName, SelectedBeatmap.AudioFileName));
            _audioPlayerVM.SetSelectedBeatmapData(SelectedBeatmapSet);
        }
    }

    private void OnBeatmapSelectedCollectionsChanges(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                Collections.First(c => c == e.NewItems[0] as Models.Collection).Beatmaps.Add(SelectedBeatmap);
                this.RaisePropertyChanged(nameof(SelectedBeatmapCollectionsCount));
                break;
            case NotifyCollectionChangedAction.Remove:
                Collections.First(c => c == e.OldItems[0] as Models.Collection).Beatmaps.Remove(SelectedBeatmap);
                this.RaisePropertyChanged(nameof(SelectedBeatmapCollectionsCount));
                break;
        }
    }

    private void SelectedBeatmapChanged()
    {
        MapBackground = new Bitmap(OsuDataReader.GetBeatmapImage(SelectedBeatmap.FolderName, SelectedBeatmap.FileName));
        SelectedBeatmapCollections = new(Collections.Where(c => c.Beatmaps.Contains(SelectedBeatmap)));
    }

    private void PerformSearch()
    {
        IEnumerable<BeatmapSet> beatmapSets = BeatmapSets;

        // Если включен режим отображения только избранного
        if (IsShowOnlyFavorites)
        {
            beatmapSets = beatmapSets.Where(set => FavoriteBeatmapSets.Contains(set.Id));
        }

        if (string.IsNullOrWhiteSpace(SearchBeatmapSetText))
        {
            // Если строка поиска пустая, показываем отфильтрованные BeatmapSets
            FilteredBeatmapSets = new ObservableCollection<BeatmapSet>(beatmapSets);
        }
        else
        {
            var query = SearchBeatmapSetText.ToLower();

            // Фильтруем BeatmapSets по строке поиска
            var results = beatmapSets
                .Where(set => set.Beatmaps.Any(b =>
                    (b.Artist != null && b.Artist.ToLower().Contains(query)) ||
                    (b.Creator != null && b.Creator.ToLower().Contains(query)) ||
                    (b.TagsList != null && b.TagsList.Contains(query)) ||
                    (b.Title != null && b.Title.ToLower().Contains(query))))
                .ToList();

            // Создаём новый ObservableCollection только с подходящими BeatmapSets
            FilteredBeatmapSets = new ObservableCollection<BeatmapSet>(results);
        }
    }

    private void ToggleFavorite(bool value)
    {
        SelectedBeatmapSet.IsFavorite = value;
        UpdateFavoriteBeatmapSets(value);
    }

    private void SubscribeSearchFiltersChangings()
    {
        var locator = new ViewModelLocator();
        locator.SearchFiltersViewModel.FiltersChanged += FilterBeatmapSets;
    }

    private void FilterBeatmapSets()
    {
        if (BeatmapSets == null) return;

        var locator = new ViewModelLocator();
        var SearchFilters = locator.SearchFiltersViewModel;

        // Если включён режим отображения только избранного
        IEnumerable<BeatmapSet> beatmapSets = IsShowOnlyFavorites
            ? BeatmapSets.Where(set => FavoriteBeatmapSets.Contains(set.Id))
            : BeatmapSets;

        // Создаём список отфильтрованных битмапсетов
        var filtered = new List<BeatmapSet>();
        foreach (var beatmapSet in beatmapSets)
        {
            // Проверяем, есть ли битмапы, соответствующие текущим фильтрам
            var filteredBeatmaps = beatmapSet.Beatmaps.Where(b =>
                b.StandardStarRating >= SearchFilters.MinStarRating &&
                b.StandardStarRating <= SearchFilters.MaxStarRating &&
                b.ApproachRate >= SearchFilters.MinAR &&
                b.ApproachRate <= SearchFilters.MaxAR &&
                b.CircleSize >= SearchFilters.MinCS &&
                b.CircleSize <= SearchFilters.MaxCS &&
                b.OverallDifficulty >= SearchFilters.MinOD &&
                b.OverallDifficulty <= SearchFilters.MaxOD &&
                b.HPDrain >= SearchFilters.MinHP &&
                b.HPDrain <= SearchFilters.MaxHP &&
                (SearchFilters.MinDuration != null ? b.TotalTime >= SearchFilters.MinDuration : true) &&
                (SearchFilters.MaxDuration != null ? b.TotalTime <= SearchFilters.MaxDuration : true) &&
                (!string.IsNullOrEmpty(SearchFilters.Artist) ? b.Artist.Contains(SearchFilters.Artist) : true) &&
                (!string.IsNullOrEmpty(SearchFilters.Title) ? b.Title.Contains(SearchFilters.Title) : true) &&
                (!string.IsNullOrEmpty(SearchFilters.Mapper) ? b.Creator.Contains(SearchFilters.Mapper) : true) &&
                ((SearchFilters.TagList != null && SearchFilters.TagList.Any()) ? SearchFilters.TagList.All(filterTag =>
                    b.TagsList.Any(tag => tag.Contains(filterTag, StringComparison.OrdinalIgnoreCase))) : true)
            ).ToList();

            // Если битмапы прошли фильтры, добавляем сет в результирующий список
            if (filteredBeatmaps.Any())
            {
                filtered.Add(new BeatmapSet
                {
                    Id = beatmapSet.Id,
                    Title = beatmapSet.Title,
                    Artist = beatmapSet.Artist,
                    FolderName = beatmapSet.FolderName,
                    Beatmaps = filteredBeatmaps,
                    IsFavorite = beatmapSet.IsFavorite,
                });
            }
        }

        // Обновляем только изменённые элементы
        UpdateFilteredBeatmapSets(filtered);
    }

    private void UpdateFilteredBeatmapSets(List<BeatmapSet> updatedFiltered)
    {
        FilteredBeatmapSets = new(updatedFiltered);
    }




    public void OnMainWindowLoaded()
    {
        var scores = LoadScores();
        LoadBeatmaps(scores);
        LoadFavoriteBeatmaps();
        SubscribeSearchFiltersChangings();
    }
    public void OpenBeatmapInOsu()
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = $"{_settingsVM.OsuDirPath}\\osu!.exe",
            Arguments = $"\"osu://b/{SelectedBeatmap.BeatmapId}\"",
            UseShellExecute = false 
        };
        Process.Start(processStartInfo);
    }
    public void SelectNextBeatmapSet()
    {
        if (FilteredBeatmapSets == null || FilteredBeatmapSets.Count == 0)
            return;

        var currIndex = FilteredBeatmapSets.IndexOf(SelectedBeatmapSet);

        // Если текущий элемент последний, выбираем первый
        if (currIndex == FilteredBeatmapSets.Count - 1)
        {
            SelectedBeatmapSet = FilteredBeatmapSets.First();
        }
        else
        {
            SelectedBeatmapSet = FilteredBeatmapSets.ElementAt(currIndex + 1);
        }
    }

    public void SelectPrevBeatmapSet()
    {
        if (FilteredBeatmapSets == null || FilteredBeatmapSets.Count == 0)
            return;

        var currIndex = FilteredBeatmapSets.IndexOf(SelectedBeatmapSet);

        // Если текущий элемент первый, выбираем последний
        if (currIndex == 0 || currIndex == -1)
        {
            SelectedBeatmapSet = FilteredBeatmapSets.Last();
        }
        else
        {
            SelectedBeatmapSet = FilteredBeatmapSets.ElementAt(currIndex - 1);
        }
    }

    public void SelectRandomBeatmapSet()
    {
        if (FilteredBeatmapSets != null || FilteredBeatmapSets.Count != 0)
            SelectedBeatmapSet = FilteredBeatmapSets.ElementAt(Random.Shared.Next(0, FilteredBeatmapSets.Count));
    }

    public void ToggleSelectionCommand(Models.Collection collection)
    {
        if (SelectedBeatmapCollections.Contains(collection))
        {
            SelectedBeatmapCollections.Remove(collection);
        }
        else
        {
            SelectedBeatmapCollections.Add(collection);
        }
    }
}
