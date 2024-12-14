using Avalonia.Controls;
using Avalonia;
using Avalonia.Media.Imaging;
using OsuParsers.Database.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.VisualTree;
using Avalonia.Threading;
using System.Threading;

namespace MapManager.GUI.Models;
public class BeatmapSet
{
    public int Id { get; set; } // Уникальный идентификатор сета
    public List<Beatmap> Beatmaps { get; set; } // Список идентификаторов битмапов
    public string Title { get; set; } // Название сета
    public string Artist { get; set; } // Исполнитель
    public int BeatmapCount => Beatmaps.Count; // Количество битмапов в сете
    public string FolderName { get; set; } // Имя папки с сетом

    public override string ToString()
    {
        return $"{Artist} - {Title}, {BeatmapCount}";
    }
}

