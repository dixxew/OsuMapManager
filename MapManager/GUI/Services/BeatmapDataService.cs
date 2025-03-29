using Avalonia.Threading;
using DynamicData;
using MapManager.GUI.Models;
using MapManager.GUI.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;
public class BeatmapDataService
{
    private readonly OsuDataReader _osuDataReader;


    public BeatmapDataService(OsuDataReader osuDataReader)
    {
        _osuDataReader = osuDataReader;
    }


    private BeatmapSet _selectedBeatmapSet = new();
    private Beatmap _selectedBeatmap = new();


    public Beatmap SelectedBeatmap
    {
        get => _selectedBeatmap;
        set
        {
            _selectedBeatmap = value;
            SelectedBeatmapChanged();
        }
    }
    public BeatmapSet SelectedBeatmapSet
    {
        get => _selectedBeatmapSet;
        set
        {
            if (value == null || (_selectedBeatmapSet != null && value.Id == _selectedBeatmapSet.Id && FilteredBeatmapSets.Count > 1))
                return;
            _selectedBeatmapSet = value;
            SelectBeatmap();
            SelectedBeatmapSetChanged();
        }
    }


    public ObservableCollection<int> FavoriteBeatmapSets { get; set; } = new();
    public ObservableCollection<BeatmapSet> BeatmapSets { get; } = new();
    public ObservableCollection<BeatmapSet> FilteredBeatmapSets { get; set; } = new();
    public ObservableCollection<Models.Collection> SelectedBeatmapCollections { get; } = new();
    public ObservableCollection<Models.Collection> Collections { get; set; } = new();




    public void FilterBeatmaps(string searchText, bool showOnlyFavorites)
    {
        //var filtered = BeatmapSets.AsQueryable();

        //if (showOnlyFavorites)
        //    filtered = filtered.Where(set => set.IsFavorite);

        //if (!string.IsNullOrWhiteSpace(searchText))
        //{
        //    var query = searchText.ToLower();
        //    filtered = filtered.Where(set => set.Beatmaps.Any(b =>
        //        (b.Artist?.ToLower().Contains(query) ?? false) ||
        //        (b.Title?.ToLower().Contains(query) ?? false) ||
        //        (b.Creator?.ToLower().Contains(query) ?? false)));
        //}

        //Dispatcher.UIThread.Invoke(() =>
        //{
        //    FilteredBeatmapSets = new ObservableCollection<BeatmapSet>(filtered.ToList());
        //});
    }

    public void FilterBeatmapSets()
    {
        if (BeatmapSets == null) return;

        var locator = new ViewModelLocator();
        var SearchFilters = locator.SearchFiltersViewModel;

        // Если включён режим отображения только избранного
        IEnumerable<BeatmapSet> beatmapSets = /*IsShowOnlyFavorites
            ?*/ BeatmapSets.Where(set => FavoriteBeatmapSets.Contains(set.Id));
        //: BeatmapSets;

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
    
    public void PerformSearch(string input, bool isOnlyFav)
    {
        var query = input?.ToLower();
        var beatmapSets = isOnlyFav
            ? BeatmapSets.Where(set => FavoriteBeatmapSets.Contains(set.Id))
            : BeatmapSets;

        if (!string.IsNullOrWhiteSpace(query))
        {
            beatmapSets = beatmapSets.Where(set => set.Beatmaps.Any(b =>
                (b.Artist?.ToLower().Contains(query) ?? false) ||
                (b.Creator?.ToLower().Contains(query) ?? false) ||
                (b.TagsList?.Contains(query) ?? false) ||
                (b.Title?.ToLower().Contains(query) ?? false)));
        }

        // Оптимизированное обновление коллекции
        UpdateCollection(FilteredBeatmapSets, beatmapSets);
    }

    

    public void LoadFavoriteBeatmaps()
    {
        var list = FavoriteBeatmapManager.Load();
        FavoriteBeatmapSets.AddRange(list);
    }
    public void UpdateFavoriteBeatmapSets(bool isAdded, int beatmapid)
    {
        if (isAdded)
        {
            FavoriteBeatmapSets.Add(beatmapid);
            FavoriteBeatmapManager.Add(beatmapid);
        }
        else
        {
            FavoriteBeatmapSets.Remove(beatmapid);
            FavoriteBeatmapManager.Remove(beatmapid);
        }
    }



    private void SelectBeatmap()
    {
        SelectedBeatmap = SelectedBeatmapSet.Beatmaps.First();
    }
    private void UpdateFilteredBeatmapSets(List<BeatmapSet> updatedFiltered)
    {
        FilteredBeatmapSets.Clear();
        FilteredBeatmapSets.AddRange(updatedFiltered);
    }
// Метод обновляет существующую коллекцию, не пересоздавая её
    private void UpdateCollection(ObservableCollection<BeatmapSet> target, IEnumerable<BeatmapSet> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }






    public Action OnSelectedBeatmapSetChanged;

    private void SelectedBeatmapSetChanged()
        => OnSelectedBeatmapSetChanged?.Invoke();

    public Action OnSelectedBeatmapChanged;

    private void SelectedBeatmapChanged()
        => OnSelectedBeatmapChanged?.Invoke();


}
