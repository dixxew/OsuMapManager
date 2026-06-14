using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MapManager.GUI.Models;
using MapManager.GUI.Services;
using ReactiveUI;
using SukiUI.Dialogs;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;

namespace MapManager.GUI.ViewModels;

public class CollectionsViewModel : ViewModelBase
{
    private readonly BeatmapDataService _beatmapDataService;
    private readonly CollectionService _collectionService;
    private readonly BeatmapDownloadService _beatmapDownloadService;

    public CollectionsViewModel(
        CollectionService collectionService,
        BeatmapDataService beatmapDataService,
        BeatmapDownloadService beatmapDownloadService)
    {
        _beatmapDataService = beatmapDataService;
        _collectionService = collectionService;
        _beatmapDownloadService = beatmapDownloadService;

        _beatmapDataService.OnLoadingChanged += () => this.RaisePropertyChanged(nameof(IsLoading));

        DownloadMissingCommand = ReactiveCommand.Create<MissingBeatmap>(DownloadMissing);
        DownloadAllMissingCommand = ReactiveCommand.Create<Collection>(DownloadAllMissing);
    }

    public bool IsLoading => _beatmapDataService.IsLoading;

    public ObservableCollection<Collection> Collections => _beatmapDataService.Collections;

    public ReactiveCommand<MissingBeatmap, Unit> DownloadMissingCommand { get; }
    public ReactiveCommand<Collection, Unit> DownloadAllMissingCommand { get; }

    public Beatmap SelectedTreeViewCollection
    {
        get => _beatmapDataService.SelectedBeatmap;
        set
        {
            if (value is null) return;
            _beatmapDataService.SelectBeatmapAndFindBeatmapSet(value);
        }
    }

    public void RemoveFromCollection(Beatmap beatmap, Collection collection)
    {
        _collectionService.RemoveFromCollection(collection, beatmap);
    }

    public void RemoveCollection(object collection)
    {
        if (collection is not Collection col) return;

        MainWindowViewModel.DialogManager.CreateDialog()
            .WithTitle($"Remove {col.Name}")
            .WithContent($"Are you sure want to delete collection with {col.Beatmaps.Count} beatmaps?")
            .Dismiss().ByClickingBackground()
            .WithActionButton("Yes", _ => _collectionService.RemoveCollection(col), true)
            .WithActionButton("No", _ => { }, true)
            .OfType(Avalonia.Controls.Notifications.NotificationType.Warning)
            .TryShow();
    }

    public async Task ExportCollection(Collection collection, TopLevel topLevel)
    {
        var filePath = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"Export {collection.Name}.db",
            DefaultExtension = "db",
            SuggestedFileName = $"collection-{collection.Name}"
        });
        _collectionService.ExportCollection(collection, filePath);
    }

    private void DownloadMissing(MissingBeatmap missing) =>
        _beatmapDownloadService.EnqueueByMd5(missing);

    private void DownloadAllMissing(Collection collection)
    {
        foreach (var missing in collection.MissingBeatmaps)
            _beatmapDownloadService.EnqueueByMd5(missing);
    }
}
