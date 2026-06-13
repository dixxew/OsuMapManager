using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using MapManager.GUI.Dialogs;
using MapManager.GUI.Models.Chat;
using MapManager.GUI.Services;
using ReactiveUI;
using SukiUI.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Text.RegularExpressions;

namespace MapManager.GUI.ViewModels;

public class ChatViewModel : ViewModelBase
{
    // /beatmapsets/SETID  or  /beatmapsets/SETID#mode/BMID
    private static readonly Regex OsuBeatmapSetUrlRegex = new(
        @"osu\.ppy\.sh/beatmapsets/(\d+)(?:#[^/]*/(\d+))?",
        RegexOptions.Compiled);

    // /b/BMID  or  /beatmaps/BMID  — short URL by beatmap id
    private static readonly Regex OsuBeatmapShortUrlRegex = new(
        @"osu\.ppy\.sh/(?:b|beatmaps)/(\d+)",
        RegexOptions.Compiled);

    // /s/SETID — short URL by beatmapset id
    private static readonly Regex OsuBeatmapSetShortUrlRegex = new(
        @"osu\.ppy\.sh/s/(\d+)",
        RegexOptions.Compiled);

    private readonly ChatService _service;
    private readonly BeatmapDataService _beatmapData;
    private readonly SettingsService _settings;

    public ChatViewModel(ChatService service, BeatmapDataService beatmapData, SettingsService settings)
    {
        _service = service;
        _beatmapData = beatmapData;
        _settings = settings;

        _notificationsEnabled = settings.NotificationsEnabled;
        _soundEnabled = settings.NotificationSoundEnabled;
        _highlightKeywordsText = string.Join(", ", settings.HighlightKeywords);
        _mutedUsersText = string.Join(", ", settings.MutedUsers);

        ToggleNotifications = ReactiveCommand.Create(() =>
        {
            NotificationsEnabled = !NotificationsEnabled;
        });
        ToggleSound = ReactiveCommand.Create(() =>
        {
            SoundEnabled = !SoundEnabled;
        });
        _service.MessageReceived += OnMessageReceived;
        _service.StatusChanged += OnStatusChanged;
        OnStatusChanged(_service.Status);

        OpenPrivateChat = ReactiveCommand.Create<ChatUser>(user =>
            SelectedChannel = _service.OpenPrivateChat(user.Name));

        MentionUser = ReactiveCommand.Create<ChatUser>(user =>
            AppendMention(user.Name));

        OpenProfileInBrowser = ReactiveCommand.Create<ChatUser>(user =>
        {
            try { Process.Start(new ProcessStartInfo { FileName = $"https://osu.ppy.sh/u/{Uri.EscapeDataString(user.Name)}", UseShellExecute = true }); }
            catch { }
        });

        MuteUser = ReactiveCommand.Create<ChatUser>(user =>
            Debug.WriteLine($"Mute requested for {user.Name} (not implemented)"));

        OpenPrivateChatWith = ReactiveCommand.Create<string>(name =>
            SelectedChannel = _service.OpenPrivateChat(name));

        MentionUserNamed = ReactiveCommand.Create<string>(AppendMention);

        OpenProfileInBrowserFor = ReactiveCommand.Create<string>(name =>
        {
            try { Process.Start(new ProcessStartInfo { FileName = $"https://osu.ppy.sh/u/{Uri.EscapeDataString(name)}", UseShellExecute = true }); }
            catch { }
        });

        CopyToClipboard = ReactiveCommand.CreateFromTask<string>(async text =>
        {
            var clipboard = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                ?.MainWindow?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(text);
        });

        HandleMention = ReactiveCommand.Create<string>(nick =>
        {
            UserSearchText = nick;
        });

        HandleChannel = ReactiveCommand.Create<string>(channelName =>
        {
            var existing = _service.Channels.FirstOrDefault(c => c.Name == channelName);
            if (existing != null)
            {
                SelectedChannel = existing;
                return;
            }
            _service.JoinChannel(channelName);
            var joined = _service.Channels.FirstOrDefault(c => c.Name == channelName);
            if (joined != null)
                SelectedChannel = joined;
        });

        HandleLink = ReactiveCommand.Create<string>(url =>
        {
            if (TryNavigateBeatmap(url)) return;
            try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
            catch { }
        });

        LeaveChannel = ReactiveCommand.Create<ChatChannel>(channel =>
        {
            if (SelectedChannel == channel)
            {
                var idx = SortedChannels.IndexOf(channel);
                SelectedChannel = SortedChannels.Count > 1
                    ? SortedChannels[idx > 0 ? idx - 1 : 1]
                    : null;
            }
            _service.LeaveChannel(channel.Name);
        });

        _service.Channels.CollectionChanged += OnServiceChannelsChanged;
        foreach (var ch in _service.Channels.OrderByDescending(c => c.IsChannel).ThenBy(c => c.Name))
            SortedChannels.Add(ch);
    }

    // ── channels / selection ────────────────────────────────────────────────

    public ObservableCollection<ChatChannel> SortedChannels { get; } = new();

    private void OnServiceChannelsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (ChatChannel ch in e.NewItems!)
                    InsertSorted(ch);
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (ChatChannel ch in e.OldItems!)
                    SortedChannels.Remove(ch);
                break;
            case NotifyCollectionChangedAction.Reset:
                SortedChannels.Clear();
                break;
        }
    }

    private void InsertSorted(ChatChannel channel)
    {
        int i = 0;
        while (i < SortedChannels.Count)
        {
            var existing = SortedChannels[i];
            if (existing.IsChannel == channel.IsChannel)
            {
                if (string.Compare(existing.Name, channel.Name, StringComparison.OrdinalIgnoreCase) > 0)
                    break;
            }
            else if (existing.IsPrivate && channel.IsChannel)
            {
                break;
            }
            i++;
        }
        SortedChannels.Insert(i, channel);
    }

    private ChatChannel? _selectedChannel;
    public ChatChannel? SelectedChannel
    {
        get => _selectedChannel;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedChannel, value);
            UserSearchText = "";
            this.RaisePropertyChanged(nameof(DisplayedUsers));
            SelectedChannelChanged?.Invoke();
        }
    }

    public void OpenChannelPicker()
    {
        var available = ChatService.AvailableChannels
            .Where(name => _service.Channels.All(c => c.Name != name))
            .ToList();

        MainWindowViewModel.DialogManager
            .CreateDialog()
            .Dismiss().ByClickingBackground()
            .WithTitle("Join channel")
            .WithContent(new ChannelPickerDialogView
            {
                DataContext = new ChannelPickerDialogViewModel(available, name =>
                {
                    _service.JoinChannel(name);
                    SelectedChannel = _service.Channels.FirstOrDefault(c => c.Name == name);
                })
            })
            .TryShow();
    }

    // ── user actions ────────────────────────────────────────────────────────

    public ReactiveCommand<ChatUser, Unit> OpenPrivateChat { get; }
    public ReactiveCommand<ChatUser, Unit> MentionUser { get; }
    public ReactiveCommand<ChatUser, Unit> OpenProfileInBrowser { get; }
    public ReactiveCommand<ChatUser, Unit> MuteUser { get; }

    // ReactiveCommand variants used by message-avatar flyout (DataContext = ChatMessage, param = string)
    public ReactiveCommand<string, Unit> OpenPrivateChatWith { get; }
    public ReactiveCommand<string, Unit> MentionUserNamed { get; }
    public ReactiveCommand<string, Unit> OpenProfileInBrowserFor { get; }
    public ReactiveCommand<string, Unit> CopyToClipboard { get; }
    public ReactiveCommand<string, Unit> HandleLink { get; }
    public ReactiveCommand<string, Unit> HandleMention { get; }
    public ReactiveCommand<string, Unit> HandleChannel { get; }
    public ReactiveCommand<ChatChannel, Unit> LeaveChannel { get; }
    public ReactiveCommand<Unit, Unit> ToggleNotifications { get; }
    public ReactiveCommand<Unit, Unit> ToggleSound { get; }

    // ── notification settings ────────────────────────────────────────────────

    private bool _notificationsEnabled;
    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref _notificationsEnabled, value);
            this.RaisePropertyChanged(nameof(NotificationsDisabled));
            _settings.NotificationsEnabled = value;
        }
    }
    public bool NotificationsDisabled => !_notificationsEnabled;

    private bool _soundEnabled;
    public bool SoundEnabled
    {
        get => _soundEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref _soundEnabled, value);
            this.RaisePropertyChanged(nameof(SoundDisabled));
            _settings.NotificationSoundEnabled = value;
        }
    }
    public bool SoundDisabled => !_soundEnabled;

    private string _highlightKeywordsText;
    public string HighlightKeywordsText
    {
        get => _highlightKeywordsText;
        set
        {
            this.RaiseAndSetIfChanged(ref _highlightKeywordsText, value);
            _settings.HighlightKeywords = [.. value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        }
    }

    private string _mutedUsersText;
    public string MutedUsersText
    {
        get => _mutedUsersText;
        set
        {
            this.RaiseAndSetIfChanged(ref _mutedUsersText, value);
            _settings.MutedUsers = [.. value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        }
    }

    // ── input ───────────────────────────────────────────────────────────────

    private string _inputMessage = "";
    public string InputMessage
    {
        get => _inputMessage;
        set
        {
            this.RaiseAndSetIfChanged(ref _inputMessage, value);
            this.RaisePropertyChanged(nameof(DisplayedUsers));
        }
    }

    public void Send()
    {
        if (SelectedChannel == null || string.IsNullOrWhiteSpace(InputMessage)) return;
        _service.SendMessage(SelectedChannel.Name, InputMessage);
        InputMessage = "";
    }

    // Prepend @nick before any existing text
    public void AppendMention(string nick)
    {
        var mention = "@" + nick + " ";
        InputMessage = string.IsNullOrEmpty(InputMessage)
            ? mention
            : mention + InputMessage;
    }

    // ── users (search + @mention filtering) ─────────────────────────────────

    private string _userSearchText = "";
    public string UserSearchText
    {
        get => _userSearchText;
        set
        {
            this.RaiseAndSetIfChanged(ref _userSearchText, value);
            this.RaisePropertyChanged(nameof(DisplayedUsers));
        }
    }

    public IEnumerable<ChatUser> DisplayedUsers
    {
        get
        {
            var all = SelectedChannel?.Users ?? [];

            // Явный поиск по нику приоритетнее фильтра по @упоминанию.
            var search = UserSearchText?.Trim();
            if (!string.IsNullOrEmpty(search))
                return all
                    .Where(u => u.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(u => u.Name.ToLower());

            var text = InputMessage ?? "";
            var atIdx = text.LastIndexOf('@');
            if (atIdx < 0) return all;

            var afterAt = text[(atIdx + 1)..];
            // Only filter while still typing the word (no space after @)
            if (afterAt.Length == 0 || afterAt.Contains(' ')) return all;

            var partial = afterAt.ToLower();
            return all
                .Where(u => u.Name.ToLower().StartsWith(partial))
                .OrderBy(u => u.Name.ToLower());
        }
    }

    // ── link navigation ─────────────────────────────────────────────────────

    private bool TryNavigateBeatmap(string url)
    {
        // /beatmapsets/SETID  or  /beatmapsets/SETID#mode/BMID
        var m = OsuBeatmapSetUrlRegex.Match(url);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var setId))
        {
            var set = _beatmapData.BeatmapSets.FirstOrDefault(s => s.Id == setId);
            if (set != null)
            {
                int beatmapId = m.Groups[2].Success && int.TryParse(m.Groups[2].Value, out var bid) ? bid : -1;
                var beatmap = beatmapId > 0
                    ? set.Beatmaps.FirstOrDefault(b => b.BeatmapId == beatmapId) ?? set.Beatmaps.FirstOrDefault()
                    : set.Beatmaps.FirstOrDefault();
                if (beatmap != null)
                {
                    _beatmapData.SelectedBeatmapSet = set;
                    _beatmapData.SelectedBeatmap = beatmap;
                    return true;
                }
            }
        }

        // /b/BMID  or  /beatmaps/BMID
        m = OsuBeatmapShortUrlRegex.Match(url);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var bmId))
        {
            foreach (var set in _beatmapData.BeatmapSets)
            {
                var beatmap = set.Beatmaps.FirstOrDefault(b => b.BeatmapId == bmId);
                if (beatmap != null)
                {
                    _beatmapData.SelectedBeatmapSet = set;
                    _beatmapData.SelectedBeatmap = beatmap;
                    return true;
                }
            }
        }

        // /s/SETID
        m = OsuBeatmapSetShortUrlRegex.Match(url);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var sId))
        {
            var set = _beatmapData.BeatmapSets.FirstOrDefault(s => s.Id == sId);
            if (set?.Beatmaps.FirstOrDefault() is { } first)
            {
                _beatmapData.SelectedBeatmapSet = set;
                _beatmapData.SelectedBeatmap = first;
                return true;
            }
        }

        return false;
    }

    // ── connection status ────────────────────────────────────────────────────

    private ConnectionStatus _status;
    public ConnectionStatus ConnectionStatus
    {
        get => _status;
        private set
        {
            this.RaiseAndSetIfChanged(ref _status, value);
            this.RaisePropertyChanged(nameof(StatusText));
            this.RaisePropertyChanged(nameof(StatusColor));
        }
    }

    public string StatusText => _status switch
    {
        ConnectionStatus.Connected    => "Connected",
        ConnectionStatus.Connecting   => "Connecting…",
        _ => "Disconnected",
    };

    public string StatusColor => _status switch
    {
        ConnectionStatus.Connected    => "#40c060",
        ConnectionStatus.Connecting   => "#f0c040",
        _                             => "#888888",
    };

    // ── events ──────────────────────────────────────────────────────────────

    public event Action? CurrentChannelMessageReceived;
    public event Action? SelectedChannelChanged;

    private void OnMessageReceived(ChatChannel channel, ChatMessage _)
    {
        if (channel.Equals(SelectedChannel))
            CurrentChannelMessageReceived?.Invoke();
    }

    private void OnStatusChanged(ConnectionStatus status)
    {
        Dispatcher.UIThread.Post(() => ConnectionStatus = status);
    }
}
