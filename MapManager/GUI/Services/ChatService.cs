using Avalonia.Threading;
using DynamicData;
using MapManager.GUI.Models.Chat;
using Meebey.SmartIrc4net;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;

public enum ConnectionStatus { Disconnected, Connecting, Connected }

// Простой клиент Bancho IRC поверх инжектируемого IrcClient.
// Пайплайн как в рабочей версии: Connect → Login → Listen в фоне.
// Из нового кода добавлены: статус-модель, JOIN после регистрации (001),
// системные сообщения, детект кривого пароля, UTF-8 без BOM, логи.
// Реконнект намеренно примитивный — один отложенный ретрай.
public class ChatService
{
    private readonly IrcClient _irc;
    private readonly SettingsService _settings;
    private readonly AvatarService _avatarService;
    private readonly ILogger<ChatService> _logger;

    private string _server;
    private int _port;
    private string _nickname;
    private string _password;

    private readonly Dictionary<string, string> _nickColors = new();
    private readonly Random _random = new();

    private volatile bool _isRegistered;          // 001 принят — можно слать JOIN
    private volatile bool _stopRequested = true;  // ручной стоп / фатал → не реконнектимся
    private int _reconnectPending;                // защита от двойного реконнекта
    private readonly object _sync = new();        // сериализует Connect/ForceClose на общем _irc

    private const int ReconnectDelayMs = 3_000;

    // Фиксированный список каналов Bancho для подключения.
    public static readonly string[] AvailableChannels =
    {
        "#osu", "#russian", "#english", "#help", "#announce", "#lobby", "#taiko", "#ctb", "#osumania", "#mapping",
        "#modreqs", "#spanish", "#french", "#german", "#chinese", "#japanese", "#korean", "#polish", "#ukrainian",
        "#videogames", "#lazer",
    };

    public ObservableCollection<ChatChannel> Channels { get; } = new();
    public bool IsConnected => _irc.IsConnected;

    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;
    public event Action<ConnectionStatus>? StatusChanged;
    public event Action<ChatChannel, ChatMessage>? MessageReceived;

    public ChatService(SettingsService settings, IrcClient irc, OsuApiService _, AvatarService avatarService,
        ILogger<ChatService> logger)
    {
        _settings = settings;
        _avatarService = avatarService;
        _logger = logger;
        _irc = irc;

        _server = _settings.IrcServer ?? "irc.ppy.sh";
        _port = _settings.IrcPort ?? 6667;
        _nickname = _settings.IrcNickname ?? "";
        _password = _settings.IrcPassword ?? "";

        // КРИТИЧНО: UTF-8 без BOM. Encoding.UTF8 эмитит преамбулу (EF BB BF),
        // и либа шлёт её первой строкой до PASS — Bancho рвёт коннект.
        // EnableUTF8Recode уводит чтение/запись в UTF-8 без BOM (ветка без преамбулы).
        _irc.Encoding = new UTF8Encoding(false);
        _irc.EnableUTF8Recode = true;

        // Убираем встроенный обработчик обрыва (Thread.Abort → краш на .NET 8),
        // чтобы обрывом рулил наш HandleConnectionError, а не Disconnect() либы.
        StripBuiltInConnectionErrorHandler(_irc);

        _irc.OnConnected += HandleConnected;
        _irc.OnRegistered += HandleRegistered;
        _irc.OnDisconnected += HandleDisconnected;
        _irc.OnConnectionError += HandleConnectionError;
        _irc.OnError += (_, e) => _logger.LogWarning("IRC ERROR: {Error}", e.ErrorMessage);
        _irc.OnRawMessage += HandleRawMessage;
        _irc.OnReadLine += (_, e) => _logger.LogTrace("<< {Line}", e.Line);
        _irc.OnWriteLine += (_, e) => _logger.LogTrace(">> {Line}", e.Line);
        _irc.OnChannelMessage += HandleChannelMessage;
        _irc.OnQueryMessage += HandlePrivateMessage;
        _irc.OnChannelAction += HandleChannelAction;
        _irc.OnQueryAction += HandleQueryAction;
        _irc.OnNames += HandleNames;

        _settings.OnIrcSettingsChanged += OnIrcSettingsChanged;
    }

    // ── public API ──────────────────────────────────────────────────────────

    public void Start()
    {
        var saved = _settings.OpenedChannels ?? new List<string> { "#russian" };
        foreach (var name in saved)
            AddChannelIfMissing(name, isPrivate: !name.StartsWith('#'));

        if (string.IsNullOrWhiteSpace(_server) || string.IsNullOrWhiteSpace(_nickname) ||
            string.IsNullOrWhiteSpace(_password))
        {
            _logger.LogWarning("IRC not configured — not connecting");
            BroadcastSystemMessage("IRC is not configured: set your nickname and IRC password " +
                                   "(osu.ppy.sh/home/account/edit, Legacy API → IRC) in settings.");
            return;
        }

        _stopRequested = false;
        Task.Run(Connect);
    }

    public void Stop()
    {
        _logger.LogInformation("Stopping IRC chat");
        _stopRequested = true;
        _isRegistered = false;
        ForceClose();
        SetStatus(ConnectionStatus.Disconnected);
    }

    public void JoinChannel(string channel)
    {
        // До регистрации JOIN бессмыслен — Bancho его молча выкидывает.
        // Канал встаёт в список, реальный JOIN уйдёт из HandleRegistered.
        if (_isRegistered && channel.StartsWith('#') && _irc.IsConnected)
            try { _irc.RfcJoin(channel); _logger.LogDebug("JOIN {Channel}", channel); }
            catch (Exception ex) { _logger.LogWarning(ex, "JOIN {Channel} failed", channel); }

        AddChannelIfMissing(channel);
    }

    // Личный чат — «канал» с именем ника, без JOIN на сервере.
    public ChatChannel OpenPrivateChat(string nick)
    {
        var channel = Channels.FirstOrDefault(c => c.Name == nick);
        if (channel == null)
        {
            channel = new ChatChannel(nick, _avatarService);
            Channels.Add(channel);
            SaveChannels();
        }
        return channel;
    }

    public void LeaveChannel(string name)
    {
        if (name.StartsWith('#') && _isRegistered && _irc.IsConnected)
            try { _irc.RfcPart(name); _logger.LogDebug("PART {Channel}", name); }
            catch (Exception ex) { _logger.LogWarning(ex, "PART {Channel} failed", name); }

        Dispatcher.UIThread.Post(() =>
        {
            var channel = Channels.FirstOrDefault(c => c.Name == name);
            if (channel != null) Channels.Remove(channel);
            SaveChannels();
        });
    }

    public void SendMessage(string target, string message)
    {
        if (!_irc.IsConnected)
        {
            _logger.LogWarning("SendMessage to {Target} dropped: not connected", target);
            return;
        }

        try { _irc.SendMessage(SendType.Message, target, message); }
        catch (Exception ex) { _logger.LogWarning(ex, "SendMessage to {Target} failed", target); return; }

        var type = target.StartsWith('#') ? ChatMessageType.Channel : ChatMessageType.Private;
        AddMessageToChannel(target, new ChatMessage(_avatarService)
        {
            Type = type,
            Sender = _nickname,
            Channel = target,
            Message = message,
            Timestamp = DateTime.Now,
        });
    }

    // ── connection ──────────────────────────────────────────────────────────

    private void Connect()
    {
        // Лок: два параллельных Connect на одном _irc дают "Stream was not
        // readable" / SocketException — лезут в один сокет одновременно.
        lock (_sync)
        {
            if (_stopRequested || _irc.IsConnected) return;

            SetStatus(ConnectionStatus.Connecting);
            _logger.LogInformation("Connecting to {Server}:{Port} as {Nick}", _server, _port, _nickname);

            try
            {
                _irc.Connect(_server, _port);
                var nick = _nickname.Replace(' ', '_');   // Bancho не любит пробелы в нике
                _irc.Login(nick, nick, 0, nick, _password);

                // Один Listen на соединение; диспатчит все IRC-события, пока жив сокет.
                Task.Run(() =>
                {
                    try { _irc.Listen(); }
                    catch (Exception ex) { _logger.LogDebug(ex, "Listen loop threw"); }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Connect failed");
                ForceClose();             // подчистить полуоткрытое состояние
                ScheduleReconnect();
            }
        }
    }

    private void ScheduleReconnect()
    {
        if (_stopRequested) return;
        if (Interlocked.Exchange(ref _reconnectPending, 1) == 1) return;   // OnDisconnected + OnConnectionError не должны удвоить

        SetStatus(ConnectionStatus.Disconnected);
        _logger.LogInformation("Reconnecting in {Delay}ms", ReconnectDelayMs);
        Task.Delay(ReconnectDelayMs).ContinueWith(_ =>
        {
            Interlocked.Exchange(ref _reconnectPending, 0);
            if (!_stopRequested) Connect();
        });
    }

    private void SetStatus(ConnectionStatus s)
    {
        if (Status == s) return;
        Status = s;
        StatusChanged?.Invoke(s);
    }

    // ── SmartIrc4net teardown ────────────────────────────────────────────────
    // Единственный нужный хак: либин Disconnect() зовёт Thread.Abort(), которого
    // в .NET 8 нет — он кидает исключение ДО закрытия сокета и сброса _IsConnected,
    // оставляя клиента «вечно подключённым», из-за чего ломается реконнект.
    // Закрываем сокет сами через приватные поля.
    private static readonly FieldInfo IsConnectedField = ConnField("_IsConnected");
    private static readonly FieldInfo IsRegisteredField = ConnField("_IsRegistered");
    private static readonly FieldInfo IsDisconnectingField = ConnField("_IsDisconnecting");
    private static readonly FieldInfo IsConnectionErrorField = ConnField("_IsConnectionError");
    private static readonly FieldInfo TcpClientField = ConnField("_TcpClient");
    private static readonly FieldInfo OnConnectionErrorField = ConnField("OnConnectionError");

    private static FieldInfo ConnField(string name) =>
        typeof(IrcConnection).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new MissingFieldException(nameof(IrcConnection), name);

    private void ForceClose()
    {
        lock (_sync)
        {
            try
            {
                IsDisconnectingField.SetValue(_irc, true);   // глушим OnConnectionError на нашем же обрыве
                IsConnectedField.SetValue(_irc, false);
                IsRegisteredField.SetValue(_irc, false);
                IsConnectionErrorField.SetValue(_irc, false); // стейл-флаг не должен тащиться на переиспользуемый клиент
                (TcpClientField.GetValue(_irc) as TcpClient)?.Close();
                IsDisconnectingField.SetValue(_irc, false);
            }
            catch (Exception ex) { _logger.LogDebug(ex, "ForceClose threw (ignored)"); }
        }
    }

    // Встроенный _OnConnectionError либы зовёт Disconnect() → Thread.Abort(),
    // которого нет в .NET 8: он крашит Listen на любом обрыве и не даёт
    // выполниться нашему HandleConnectionError. Вырезаем его из эвента.
    private static void StripBuiltInConnectionErrorHandler(IrcClient irc)
    {
        if (OnConnectionErrorField.GetValue(irc) is not Delegate handlers) return;

        var cleaned = handlers.GetInvocationList()
            .Where(d => !ReferenceEquals(d.Target, irc))
            .Aggregate((Delegate?)null, Delegate.Combine);
        OnConnectionErrorField.SetValue(irc, cleaned);
    }

    // ── IRC event handlers ──────────────────────────────────────────────────

    private void HandleConnected(object? sender, EventArgs e)
    {
        // Сокет открыт, но Connected ставим только после 001 (HandleRegistered):
        // логин может не пройти. При реконнекте чистим протухшие списки юзеров.
        _logger.LogDebug("Socket connected");
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var ch in Channels)
            {
                ch.IsUsersLoaded = false;
                ch.Users.Clear();
            }
        });
    }

    private void HandleRegistered(object? sender, EventArgs e)
    {
        _logger.LogInformation("Registered (001) — connection live");
        _isRegistered = true;
        SetStatus(ConnectionStatus.Connected);

        foreach (var name in Channels.Select(c => c.Name).Where(n => n.StartsWith('#')).ToList())
            try { _irc.RfcJoin(name); _logger.LogDebug("JOIN {Channel}", name); }
            catch (Exception ex) { _logger.LogWarning(ex, "JOIN {Channel} failed", name); }
    }

    private void HandleDisconnected(object? sender, EventArgs e)
    {
        _logger.LogDebug("OnDisconnected");
        _isRegistered = false;
        if (_stopRequested) SetStatus(ConnectionStatus.Disconnected);
        else ScheduleReconnect();
    }

    private void HandleConnectionError(object? sender, EventArgs e)
    {
        _logger.LogDebug("OnConnectionError");
        _isRegistered = false;
        ForceClose();                 // закрыть сокет, чтобы Listen/ReadLine вышли
        if (!_stopRequested) ScheduleReconnect();
        else SetStatus(ConnectionStatus.Disconnected);
    }

    private void HandleRawMessage(object? sender, IrcEventArgs e)
    {
        _logger.LogDebug("RAW [{Code}/{Type}] {Raw}", (int)e.Data.ReplyCode, e.Data.Type, e.Data.RawMessage);

        // Кривой/протухший пароль: Bancho шлёт нотис до регистрации и рвёт коннект.
        // Только до 001 — после в raw летят обычные сообщения со словом "password".
        if (!_isRegistered &&
            (e.Data.Message?.Contains("authenticate", StringComparison.OrdinalIgnoreCase) == true ||
             e.Data.ReplyCode == ReplyCode.ErrorPasswordMismatch))   // 464
        {
            HandleAuthFailure();
            return;
        }

        if ((int)e.Data.ReplyCode >= 400 && (int)e.Data.ReplyCode < 600)
        {
            _logger.LogWarning("IRC error reply {Code}: {Raw}", (int)e.Data.ReplyCode, e.Data.RawMessage);

            var target = e.Data.RawMessageArray?.FirstOrDefault(p => p.StartsWith('#'));
            if (target != null && Channels.Any(c => c.Name == target))
                AddMessageToChannel(target, SystemMessage(target,
                    $"Server error {(int)e.Data.ReplyCode}: {e.Data.Message ?? e.Data.RawMessage}"));
        }

        if (e.Data.ReplyCode == ReplyCode.EndOfNames)
        {
            var channel = Channels.FirstOrDefault(c => c.Name == e.Data.Channel);
            if (channel != null) channel.IsUsersLoaded = true;
        }
    }

    private void HandleAuthFailure()
    {
        _logger.LogError("Bancho rejected login — bad IRC password");
        _stopRequested = true;        // тем же паролем долбиться смысла нет
        _isRegistered = false;
        ForceClose();
        SetStatus(ConnectionStatus.Disconnected);
        BroadcastSystemMessage("Bancho rejected the login: bad IRC password. " +
                               "Get a fresh one at osu.ppy.sh/home/account/edit (Legacy API → IRC).");
    }

    private async void HandleNames(object? sender, NamesEventArgs e)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var channel = Channels.FirstOrDefault(c => c.Name == e.Channel);
            if (channel == null) return;

            var existing = new HashSet<string>(channel.Users.Select(u => u.Name));
            var newUsers = e.UserList
                .Select(u => u.TrimStart('@', '+', '~', '%', '&'))
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct()
                .Where(u => !existing.Contains(u))
                .Select(u => new ChatUser(_avatarService, this) { Name = u })
                .ToList();

            if (newUsers.Count > 0)
                channel.Users.AddRange(newUsers);
        });
    }

    private void HandleChannelMessage(object? sender, IrcEventArgs e)
    {
        var nick = e.Data.Nick;
        if (!_nickColors.TryGetValue(nick, out var color))
        {
            color = $"#{_random.Next(0x1000000):X6}";
            _nickColors[nick] = color;
        }

        AddMessageToChannel(e.Data.Channel, new ChatMessage(_avatarService)
        {
            Type = ChatMessageType.Channel,
            Sender = nick,
            Channel = e.Data.Channel,
            Message = e.Data.Message,
            Timestamp = DateTime.Now,
            NickColor = color,
        });
    }

    private void HandlePrivateMessage(object? sender, IrcEventArgs e)
    {
        AddMessageToChannel(e.Data.Nick, new ChatMessage(_avatarService)
        {
            Type = ChatMessageType.Private,
            Sender = e.Data.Nick,
            Channel = e.Data.Nick,
            Message = e.Data.Message,
            Timestamp = DateTime.Now,
        });
    }

    private void HandleChannelAction(object? sender, ActionEventArgs e)
    {
        var nick = e.Data.Nick;
        if (!_nickColors.TryGetValue(nick, out var color))
        {
            color = $"#{_random.Next(0x1000000):X6}";
            _nickColors[nick] = color;
        }

        AddMessageToChannel(e.Data.Channel, new ChatMessage(_avatarService)
        {
            Type = ChatMessageType.Action,
            Sender = nick,
            Channel = e.Data.Channel,
            Message = e.ActionMessage,
            Timestamp = DateTime.Now,
            NickColor = color,
        });
    }

    private void HandleQueryAction(object? sender, ActionEventArgs e)
    {
        AddMessageToChannel(e.Data.Nick, new ChatMessage(_avatarService)
        {
            Type = ChatMessageType.Action,
            Sender = e.Data.Nick,
            Channel = e.Data.Nick,
            Message = e.ActionMessage,
            Timestamp = DateTime.Now,
        });
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private ChatMessage SystemMessage(string channel, string text) => new(_avatarService)
    {
        Type = ChatMessageType.Channel,
        Sender = "system",
        Channel = channel,
        Message = text,
        Timestamp = DateTime.Now,
        NickColor = "#f08080",
    };

    private void BroadcastSystemMessage(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var ch in Channels.ToList())
                ch.AddMessage(SystemMessage(ch.Name, text));
        });
    }

    private void AddChannelIfMissing(string name, bool isPrivate = false)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (Channels.All(c => c.Name != name))
            {
                Channels.Add(isPrivate ? new ChatChannel(name, _avatarService) : new ChatChannel(name));
                SaveChannels();
            }
        });
    }

    private void SaveChannels()
    {
        _settings.OpenedChannels = Channels.Select(c => c.Name).ToList();
    }

    private void AddMessageToChannel(string key, ChatMessage message)
    {
        // Channels привязана к UI — трогаем её только из UI-потока.
        Dispatcher.UIThread.Post(() =>
        {
            var channel = Channels.FirstOrDefault(c => c.Name == key);
            if (channel == null)
            {
                channel = new ChatChannel(key);
                Channels.Add(channel);
            }

            channel.AddMessage(message);
            MessageReceived?.Invoke(channel, message);
        });
    }

    private void OnIrcSettingsChanged()
    {
        var server = _settings.IrcServer ?? "irc.ppy.sh";
        var port = _settings.IrcPort ?? 6667;
        var nick = _settings.IrcNickname ?? "";
        var pass = _settings.IrcPassword ?? "";

        // Эвент стреляет несколько раз при загрузке настроек на старте.
        // Реконнектиться, когда ничего не поменялось, нельзя — это и плодило гонку.
        if (server == _server && port == _port && nick == _nickname && pass == _password)
        {
            _logger.LogDebug("IRC settings event but nothing changed — ignoring");
            return;
        }

        _logger.LogInformation("IRC settings changed — reconnecting");
        _server = server;
        _port = port;
        _nickname = nick;
        _password = pass;

        _stopRequested = true;
        _isRegistered = false;
        ForceClose();
        Start();
    }
}