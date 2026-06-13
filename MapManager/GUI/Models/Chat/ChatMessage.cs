using Avalonia.Media.Imaging;
using MapManager.GUI.Services;
using MapManager.GUI.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;

namespace MapManager.GUI.Models.Chat;

public enum ActionKind { None, ListeningTo, Playing, Watching, Editing }

public class ChatMessage : ViewModelBase
{
    public readonly AvatarService AvatarService;

    public ChatMessage(AvatarService avatarService)
    {
        AvatarService = avatarService;
        AvatarService.AvatarLoaded += username =>
        {
            if (username == Sender)
            {
                this.RaisePropertyChanged(nameof(Avatar));
                this.RaisePropertyChanged(nameof(HasAvatar));
                this.RaisePropertyChanged(nameof(HasNoAvatar));
            }
        };
    }

    public ChatMessageType Type { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string NickColor { get; set; } = "#FFFFFF";
    public string? Channel { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public bool IsAction => Type == ChatMessageType.Action;
    public bool IsRegular => !IsAction;

    private ActionKind? _actionKind;
    public ActionKind ActionKind => _actionKind ??= IsAction ? DetectActionKind(Message) : ActionKind.None;

    public bool IsListeningAction => ActionKind == ActionKind.ListeningTo;
    public bool IsPlayingAction   => ActionKind == ActionKind.Playing;
    public bool IsWatchingAction  => ActionKind == ActionKind.Watching;
    public bool IsEditingAction   => ActionKind == ActionKind.Editing;
    public bool HasActionIcon     => ActionKind != ActionKind.None;

    private IReadOnlyList<MessageSegment>? _segments;
    public IReadOnlyList<MessageSegment> Segments =>
        _segments ??= MessageParser.Parse(Message, isAction: IsAction);

    public Bitmap? Avatar => AvatarService.GetAvatar(Sender);
    public bool HasAvatar  => Avatar != null;
    public bool HasNoAvatar => Avatar == null;
    public string AvatarInitial => string.IsNullOrEmpty(Sender) ? "?" : Sender[0].ToString().ToUpper();

    private static ActionKind DetectActionKind(string msg)
    {
        if (msg.StartsWith("is listening to ")) return ActionKind.ListeningTo;
        if (msg.StartsWith("is playing "))      return ActionKind.Playing;
        if (msg.StartsWith("is watching "))     return ActionKind.Watching;
        if (msg.StartsWith("is editing "))      return ActionKind.Editing;
        return ActionKind.None;
    }
}
