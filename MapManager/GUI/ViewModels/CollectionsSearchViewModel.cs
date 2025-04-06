using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MapManager.GUI.Dialogs;
using MapManager.GUI.Services;
using SukiUI.Dialogs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MapManager.GUI.ViewModels;

public class CollectionsSearchViewModel : ViewModelBase
{
    private readonly CollectionService _collectionService;
    private readonly BeatmapDataService _beatmapDataService;
    public CollectionsSearchViewModel(CollectionService collectionService, BeatmapDataService beatmapDataService)
    {
        _collectionService = collectionService;
        _beatmapDataService = beatmapDataService;
    }

    public void AddCollection()
    {
        //var method = new AccpetMethod....
        MainWindowViewModel.DialogManager
            .CreateDialog()
            .Dismiss().ByClickingBackground()
            .WithTitle("Create collection")
            .WithContent(new TextBoxDialogView()
            {
                DataContext = new TextBoxDialogViewModel(name =>
                {
                    if (string.IsNullOrWhiteSpace(name))
                        return false;

                    return _collectionService.AddCollection(name, new() { _beatmapDataService.SelectedBeatmap.MD5Hash });
                })
            })
            .TryShow();

           
        //_collectionService.AddCollection(Random.Shared.Next(0, 99999999).ToString(), new() { _beatmapDataService.SelectedBeatmap.MD5Hash });
    }

    public async Task ImportCollections(TopLevel topLevel)
    {
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open collection files",
            AllowMultiple = true,
            FileTypeFilter = new List<FilePickerFileType>() { new FilePickerFileType("osuDb")
            {
                Patterns = new List<string>() { "*.db" }
            }}            
        });

        if (files is null || files.Count == 0)
            _collectionService.ImportCollections(files);
    }
}