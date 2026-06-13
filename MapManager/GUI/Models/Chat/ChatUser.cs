using Avalonia.Media.Imaging;
using MapManager.GUI.Services;
using ReactiveUI;
using System;

namespace MapManager.GUI.Models.Chat;

public class ChatUser : ReactiveObject
{
    private readonly AvatarService AvatarService;
    private static readonly string[] ColorPalette =
        ["#e57373", "#f06292", "#ba68c8", "#7986cb", "#4fc3f7", "#4db6ac", "#81c784", "#aed581", "#ffb74d", "#ff8a65"];

    public string Name { get; set; }
    public Bitmap? Avatar => AvatarService.GetAvatar(Name);
    public bool HasAvatar   => Avatar != null;
    public bool HasNoAvatar => Avatar == null;
    public string AvatarInitial    => string.IsNullOrEmpty(Name) ? "?" : Name[0].ToString().ToUpper();
    public string PlaceholderColor => ColorPalette[Math.Abs(Name.GetHashCode()) % ColorPalette.Length];

    public ChatUser(AvatarService avatarService)
    {
        AvatarService = avatarService;
        AvatarService.AvatarLoaded += username =>
        {
            if (username == Name)
            {
                this.RaisePropertyChanged(nameof(Avatar));
                this.RaisePropertyChanged(nameof(HasAvatar));
                this.RaisePropertyChanged(nameof(HasNoAvatar));
            }
        };
    }
}
