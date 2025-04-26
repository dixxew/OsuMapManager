using Avalonia.Media.Imaging;
using MapManager.GUI.ViewModels;
using ReactiveUI;
using System;

namespace MapManager.GUI.Models.Chat;

public class ChatMessage : ViewModelBase
{
    public ChatMessageType Type { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string NickColor { get; set; } = "#FFFFFF";
    public string? Channel { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    private Bitmap? _avatar;
    public Bitmap? Avatar
    {
        get => _avatar;
        set
        {
            if (_avatar != value)
            {
                _avatar = value;
                this.RaisePropertyChanged(nameof(Avatar));
            }
        }
    }
}
