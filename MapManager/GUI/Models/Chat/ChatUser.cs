using Avalonia.Media.Imaging;
using MapManager.GUI.Services;
using ReactiveUI;
using System;
using System.Diagnostics;

namespace MapManager.GUI.Models.Chat;

public class ChatUser : ReactiveObject
{
    private readonly AvatarService AvatarService;
    private readonly ChatService ChatService;
    private static readonly string[] ColorPalette =
        ["#e57373", "#f06292", "#ba68c8", "#7986cb", "#4fc3f7", "#4db6ac", "#81c784", "#aed581", "#ffb74d", "#ff8a65"];

    public string Name { get; set; }
    public Bitmap? Avatar => AvatarService.GetAvatar(Name);
    public bool HasAvatar   => Avatar != null;
    public bool HasNoAvatar => Avatar == null;
    public string AvatarInitial    => string.IsNullOrEmpty(Name) ? "?" : Name[0].ToString().ToUpper();
    public string PlaceholderColor => ColorPalette[Math.Abs(Name.GetHashCode()) % ColorPalette.Length];

    public ChatUser(AvatarService avatarService, ChatService chatService)
    {
        AvatarService = avatarService;
        ChatService = chatService;
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
    
    
    public void OpenPrivateChat()
    {
        ChatService.OpenPrivateChat(Name);
    }

    public void MentionUser()
    {
    }

    public void OpenProfileInBrowser()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = $"https://osu.ppy.sh/u/{Uri.EscapeDataString(Name)}",
                UseShellExecute = true,
            });
        }
        catch { }
    }

    // Заглушка: реального мьюта пока нет.
    public void MuteUser()
    {
        Debug.WriteLine($"Mute requested for {Name} (not implemented)");
    }
}
