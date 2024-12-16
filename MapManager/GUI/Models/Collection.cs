using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapManager.GUI.Models;
public class Collection : ReactiveObject
{
    public string Name { get; set; }

    public int Count { get; set; }

    private ObservableCollection<Beatmap> _beatmaps;

    public ObservableCollection<Beatmap> Beatmaps
    {
        get => _beatmaps;
        set => this.RaiseAndSetIfChanged(ref _beatmaps, value);
    }
}
