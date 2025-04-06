using MapManager.GUI.Dialogs;
using MapManager.GUI.Models;
using MapManager.GUI.Services;
using osu.Shared;
using ReactiveUI;
using SukiUI.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MapManager.GUI.ViewModels;
public class SearchOptionsViewModel : ViewModelBase
{
    private readonly CollectionService _collectionService;
    private readonly BeatmapDataService _beatmapDataService;

    public SearchOptionsViewModel(CollectionService collectionService, BeatmapDataService beatmapDataService)
    {
        _collectionService = collectionService;
        _beatmapDataService = beatmapDataService;
    }

    public void CreateFilteredCollection()
    {
        //var method = new AccpetMethod....
        MainWindowViewModel.DialogManager
            .CreateDialog()
            .Dismiss().ByClickingBackground()
            .WithTitle("Create collection with filtered beatmaps")
            .WithContent(new TextBoxDialogView()
            {
                DataContext = new TextBoxDialogViewModel(name =>
                {
                    if (string.IsNullOrWhiteSpace(name))
                        return false;
                    var collection = new Collection
                    {
                        Name = name
                    };
                    return _collectionService.AddCollection(collection, 
                        _beatmapDataService.FilteredBeatmapSets
                            .SelectMany(bs => bs.Beatmaps)
                            .ToList() );
                })
            })
            .TryShow();
    }

}
