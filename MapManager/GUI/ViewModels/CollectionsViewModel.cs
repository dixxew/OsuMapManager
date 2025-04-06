using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MapManager.GUI.Models;
using MapManager.GUI.Services;
using SukiUI.Dialogs;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MapManager.GUI.ViewModels;

public class CollectionsViewModel : ViewModelBase
{
    private readonly BeatmapDataService _beatmapDataService;
    private readonly CollectionService _collectionService;

    public CollectionsViewModel(CollectionService collectionService, BeatmapDataService beatmapDataService)
    {
        _beatmapDataService = beatmapDataService;
        _collectionService = collectionService;
    }

    public ObservableCollection<Collection> Collections => _beatmapDataService.Collections;

    public Beatmap SelectedTreeViewCollection
    {
        get => _beatmapDataService.SelectedBeatmap;
        set
        {
            if (value is null)
                return;
            _beatmapDataService.SelectBeatmapAndFindBeatmapSet(value);
        }
    }

    public void RemoveFromCollection(Beatmap beatmap, Collection collection)
    {
        _collectionService.RemoveFromCollection(collection, beatmap);
    }

    public void RemoveCollection(object collection)
    {
        if (collection is not Collection)
            return;

        var col = collection as Collection;

        MainWindowViewModel.DialogManager.CreateDialog()
            .WithTitle($"Remove {col.Name}")
            .WithContent($"Are you sure want to delete collection with {col.Beatmaps.Count} betmaps?")
            .Dismiss().ByClickingBackground()
            .WithActionButton("Yes", _ =>
            {
                _collectionService.RemoveCollection(col);
            }, true)
            .WithActionButton("No ", _ => { }, true)  // last parameter optional
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

}