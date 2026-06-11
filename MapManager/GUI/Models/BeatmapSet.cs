using Avalonia.Media.Imaging;
using Avalonia.Threading;
using MapManager.GUI.Services;
using ReactiveUI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MapManager.GUI.Models;
public class BeatmapSet : ReactiveObject
{
    public int Id { get; set; }
    public List<Beatmap> Beatmaps { get; set; }
    public string Title { get; set; }
    public string Artist { get; set; }
    public int BeatmapCount => Beatmaps.Count;
    public string FolderName { get; set; }

    private bool _isFavorite;
    public bool IsFavorite
    {
        get => _isFavorite;
        set => this.RaiseAndSetIfChanged(ref _isFavorite, value);
    }

    private Bitmap? _thumbnail;
    private bool _thumbnailRequested;

    public Bitmap? Thumbnail
    {
        get
        {
            if (!_thumbnailRequested && ThumbnailService.Current != null && Beatmaps?.Count > 0)
            {
                _thumbnailRequested = true;
                var folder = Beatmaps[0].FolderName ?? FolderName;
                var file = Beatmaps[0].FileName;
                _ = Task.Run(async () =>
                {
                    var bmp = await ThumbnailService.Current.GetAsync(folder, file);
                    if (bmp != null)
                        await Dispatcher.UIThread.InvokeAsync(() => Thumbnail = bmp);
                });
            }
            return _thumbnail;
        }
        private set => this.RaiseAndSetIfChanged(ref _thumbnail, value);
    }

    public override string ToString() => $"{Artist} - {Title}, {BeatmapCount}";

    public void SetFavorite(bool value) => IsFavorite = value;
}
