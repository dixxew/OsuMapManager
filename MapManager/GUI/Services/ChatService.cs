using Avalonia.Threading;
using DynamicData;
using MapManager.GUI.Models.Chat;
using MapManager.GUI.ViewModels;
using Meebey.SmartIrc4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace MapManager.GUI.Services;

public class ChatService
{
    private readonly IrcClient _irc;
    private readonly SettingsService _settings;
    private readonly OsuApiService _osuApiService;
    private readonly AvatarService _avatarService;



    public ChatService(SettingsService settings, IrcClient irc, OsuApiService osuApiService, AvatarService avatarService)
    {
        _settings = settings;
        _settings.OnIrcSettingsChanged += OnIrcSettingsChanged;
        _server = _settings.IrcServer;
        _port = _settings.IrcPort ?? 6667;
        _nickname = _settings.IrcNickname;
        _password = _settings.IrcPassword;
        _irc = irc;

        _irc.OnConnected += HandleConnected;
        _irc.OnDisconnected += HandleDisconnected;
        _irc.OnConnectionError += HandleConnectionError;
        _irc.OnError += HandleError;
        _irc.OnChannelMessage += HandleChannelMessage;
        _irc.OnQueryMessage += HandlePrivateMessage;
        _irc.OnNames += HandleNames;
        _irc.OnRawMessage += HandleRawMessage;

        _osuApiService = osuApiService;
        _avatarService = avatarService;
    }

    private string _server;
    private int _port;
    private string _nickname;
    private string _password;
    private readonly Dictionary<string, string> _nickColors = new();
    private readonly Random _random = new Random();


    public ObservableCollection<ChatChannel> Channels = new();
    public bool IsConnected => _irc.IsConnected;



    public void Connect()
    {
        try
        {
                _irc.Connect(_server, _port);
                JoinChannel("#russian");
                JoinChannel("#osu");
                _irc.Login(_nickname, _nickname, 0, _nickname, _password);
                Task.Run(() => _irc.Listen());
        }
        catch
        {
            // Прямой вызов эвента без добавления системного сообщения.
            ConnectionFailed?.Invoke();
        }
    }
    public void Disconnect()
    {
        if (_irc.IsConnected)
            _irc.Disconnect();
    }
    public void Reconnect()
    {
        Disconnect();
        Connect();
    }
    public void JoinChannel(string channel)
    {
        if (_irc.IsConnected)
            _irc.RfcJoin(channel);

        // Если чат с таким названием не создан, создать новый.
        if (!Channels.Any(c => c.Name == channel))
            Channels.Add(new ChatChannel(channel));
    }
    public void SendMessage(string target, string message)
    {
        if (_irc.IsConnected)
        {
            _irc.SendMessage(SendType.Message, target, message);
            var msgType = string.IsNullOrEmpty(target) || target.StartsWith("#")
                ? ChatMessageType.Channel : ChatMessageType.Private;
            var chatMessage = new ChatMessage(_avatarService)
            {
                Type = msgType,
                Sender = _nickname,
                Channel = msgType == ChatMessageType.Channel ? target : target,
                Message = message,
                Timestamp = DateTime.Now
            };

            AddMessageToChannel(target, chatMessage);
        }
    }


    private void HandleRawMessage(object? sender, IrcEventArgs e)
    {
        if (e.Data.ReplyCode == ReplyCode.EndOfNames)
        {
            var channelName = e.Data.Channel;
            var channel = Channels.FirstOrDefault(c => c.Name == channelName);
            if (channel != null)
                channel.IsUsersLoaded = true; // Всё, теперь можно считать что юзеры загружены
        }
    }
    private async void HandleNames(object? sender, NamesEventArgs e)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var channel = Channels.FirstOrDefault(c => c.Name == e.Channel);
            if (channel == null)
                return;

            var existingNames = new HashSet<string>(channel.Users.Select(u => u.Name));

            var cleanedNames = e.UserList
                .Select(u => u.TrimStart('@', '+', '~', '%', '&'))
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct(); // убираем повторы из пришедшего списка

            var newUsers = cleanedNames
                .Where(u => !existingNames.Contains(u))
                .Select(u => new ChatUser(_avatarService) { Name = u })
                .ToList();

            if (newUsers.Count > 0)
                channel.Users.AddRange(newUsers);
        });
    }


    private void HandleChannelMessage(object? sender, IrcEventArgs e)
    {
        var nick = e.Data.Nick;
        if (!_nickColors.TryGetValue(nick, out var nickColor))
        {
            nickColor = GenerateRandomColor();
            _nickColors[nick] = nickColor;
        }

        var chatMessage = new ChatMessage(_avatarService)
        {
            Type = ChatMessageType.Channel,
            Sender = nick,
            Channel = e.Data.Channel,
            Message = e.Data.Message,
            Timestamp = DateTime.Now,
            NickColor = nickColor
        };

        AddMessageToChannel(e.Data.Channel, chatMessage);
    }
    private void HandlePrivateMessage(object? sender, IrcEventArgs e)
    {
        // Для приватных сообщений используем ник собеседника как ключ чата.
        var chatMessage = new ChatMessage(_avatarService)
        {
            Type = ChatMessageType.Private,
            Sender = e.Data.Nick,
            Channel = e.Data.Nick,
            Message = e.Data.Message,
            Timestamp = DateTime.Now
        };

        AddMessageToChannel(e.Data.Nick, chatMessage);
    }
    private void AddMessageToChannel(string channelKey, ChatMessage message)
    {
        if (!Channels.Any(c => c.Name == channelKey))
            Channels.Add(new ChatChannel(channelKey));


        Channels
            .First(c => c.Name == channelKey)
            .AddMessage(message);

        MessageReceived?.Invoke(Channels.First(c => c.Name == channelKey), message);
    }
    private void OnIrcSettingsChanged()
    {
        UpdateClientSettings();
    }
    private void UpdateClientSettings()
    {
        _server = _settings.IrcServer;
        _port = _settings.IrcPort ?? 6667;
        _nickname = _settings.IrcNickname;
        _password = _settings.IrcPassword;
        Reconnect();
    }
    private string GenerateRandomColor()
    {
        // Генерируем цвет в формате "#RRGGBB"
        return $"#{_random.Next(0x1000000):X6}";
    }



    public event Action<ChatChannel, ChatMessage>? MessageReceived;
    public event Action? Connected;
    public event Action? Disconnected;
    public event Action? ConnectionFailed;
    public event Action<string>? ErrorReceived;
    private void HandleConnected(object? sender, EventArgs e)
    {
        // Просто вызов эвента, сообщения в чаты не добавляются.
        Connected?.Invoke();
    }
    private void HandleDisconnected(object? sender, EventArgs e)
    {
        Disconnected?.Invoke();
    }
    private void HandleConnectionError(object? sender, EventArgs e)
    {
        ConnectionFailed?.Invoke();
        Task.Delay(1000).ContinueWith(_ => Reconnect());
    }
    private void HandleError(object? sender, ErrorEventArgs e)
    {
        ErrorReceived?.Invoke(e.ErrorMessage);
    }

}
