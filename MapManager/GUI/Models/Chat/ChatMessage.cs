using Avalonia.Media.Imaging;
using MapManager.GUI.Services;
using MapManager.GUI.ViewModels;
using ReactiveUI;
using System;

namespace MapManager.GUI.Models.Chat;

public class ChatMessage : ViewModelBase
{
    public readonly AvatarService AvatarService; 
    
    public ChatMessage(AvatarService avatarService)
    {
        AvatarService = avatarService;
        AvatarService.AvatarLoaded += (Sender) =>
        {
            this.RaisePropertyChanged(nameof(Avatar));
        };
    }
    
    public ChatMessageType Type { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string NickColor { get; set; } = "#FFFFFF";
    public string? Channel { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public Bitmap? Avatar => AvatarService.GetAvatar(Sender);
}
