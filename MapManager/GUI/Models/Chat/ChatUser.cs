using Avalonia.Media.Imaging;
using MapManager.GUI.Services;
using ReactiveUI;

namespace MapManager.GUI.Models.Chat;

public class ChatUser : ReactiveObject
{
    private readonly AvatarService AvatarService;
    public string Name { get; set; }
    public Bitmap? Avatar => AvatarService.GetAvatar(Name);

    public ChatUser(AvatarService avatarService)
    {
        AvatarService = avatarService;
    }
}
