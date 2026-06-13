using Avalonia.Media.Imaging;
using Avalonia.Threading;
using MapManager.GUI.Services;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;

namespace MapManager.GUI.Models.Chat;

public class ChatChannel : ReactiveObject
{
    private static readonly string[] ColorPalette =
        ["#e57373", "#f06292", "#ba68c8", "#7986cb", "#4fc3f7", "#4db6ac", "#81c784", "#aed581", "#ffb74d", "#ff8a65"];

    private readonly AvatarService? _avatarService;

    public string Name { get; }
    public bool IsChannel => Name.StartsWith('#');
    public bool IsPrivate => !IsChannel;
    public string DisplayName => IsChannel ? Name[1..] : Name;

    public ObservableCollection<ChatMessage> Messages { get; } = new();
    public ObservableCollection<ChatUser> Users { get; } = new();
    public bool IsUsersLoaded { get; set; } = false;

    public Bitmap? Avatar => IsPrivate ? _avatarService?.GetAvatar(Name) : null;
    public bool HasAvatar   => Avatar != null;
    public bool HasNoAvatar => Avatar == null;
    public string AvatarInitial    => string.IsNullOrEmpty(Name) ? "?" : Name[0].ToString().ToUpper();
    public string PlaceholderColor => ColorPalette[Math.Abs(Name.GetHashCode()) % ColorPalette.Length];

    public ChatChannel(string name, AvatarService? avatarService = null)
    {
        Name = name;
        _avatarService = avatarService;

        if (_avatarService != null)
        {
            _avatarService.AvatarLoaded += username =>
            {
                if (IsPrivate && username == Name)
                {
                    this.RaisePropertyChanged(nameof(Avatar));
                    this.RaisePropertyChanged(nameof(HasAvatar));
                    this.RaisePropertyChanged(nameof(HasNoAvatar));
                }
            };
        }
    }

    public void AddMessage(ChatMessage message)
    {
        Dispatcher.UIThread.Invoke(() => Messages.Add(message));
    }
}
