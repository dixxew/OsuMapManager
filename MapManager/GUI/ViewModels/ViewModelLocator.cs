using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapManager.GUI.ViewModels;
public class ViewModelLocator
{
    public MainWindowViewModel MainWindowViewModel =>
        App.AppHost.Services.GetRequiredService<MainWindowViewModel>();

    public AudioPlayerViewModel AudioPlayerViewModel =>
        App.AppHost.Services.GetRequiredService<AudioPlayerViewModel>();

    public SettingsViewModel SettingsViewModel =>
        App.AppHost.Services.GetRequiredService<SettingsViewModel>();

    public SearchFiltersViewModel SearchFiltersViewModel =>
        App.AppHost.Services.GetRequiredService<SearchFiltersViewModel>();

    public BeatmapsSearchViewModel BeatmapsSearchViewModel =>
        App.AppHost.Services.GetRequiredService<BeatmapsSearchViewModel>();

    public BeatmapsViewModel BeatmapsViewModel =>
        App.AppHost.Services.GetRequiredService<BeatmapsViewModel>();

    public CollectionsSearchViewModel CollectionsSearchViewModel =>
        App.AppHost.Services.GetRequiredService<CollectionsSearchViewModel>();

    public CollectionsViewModel CollectionsViewModel =>
        App.AppHost.Services.GetRequiredService<CollectionsViewModel>();

    public BeatmapInfoViewModel BeatmapInfoViewModel =>
        App.AppHost.Services.GetRequiredService<BeatmapInfoViewModel>();

    public LocalScoresViewModel LocalScoresViewModel =>
        App.AppHost.Services.GetRequiredService<LocalScoresViewModel>();

    public GlobalScoresViewModel GlobalScoresViewModel =>
        App.AppHost.Services.GetRequiredService<GlobalScoresViewModel>();
    public GreetingsViewModel GreetingsViewModel =>
        App.AppHost.Services.GetRequiredService<GreetingsViewModel>();

}
