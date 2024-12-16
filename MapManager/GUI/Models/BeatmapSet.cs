using ReactiveUI;
using System.Collections.Generic;

namespace MapManager.GUI.Models;
public class BeatmapSet : ReactiveObject
{
    public int Id { get; set; } // Уникальный идентификатор сета
    public List<Beatmap> Beatmaps { get; set; } // Список идентификаторов битмапов
    public string Title { get; set; } // Название сета
    public string Artist { get; set; } // Исполнитель
    public int BeatmapCount => Beatmaps.Count; // Количество битмапов в сете
    public string FolderName { get; set; } // Имя папки с сетом

    private bool _isFavorite;

    public bool IsFavorite
    {
        get => _isFavorite;
        set => this.RaiseAndSetIfChanged(ref _isFavorite, value);
    }
    public override string ToString()
    {
        return $"{Artist} - {Title}, {BeatmapCount}";
    }
    public void SetFavorite(bool value)
    {
        IsFavorite = value;
    }
}

