using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using MapManager.GUI.Models.Chat;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace MapManager.GUI.Services;

public class NotificationService
{
    private const string AppId = "MapManager.App";

    private readonly ChatService _chat;
    private readonly SettingsService _settings;

    public bool OsNotificationsBlocked { get; private set; }

    public NotificationService(ChatService chat, SettingsService settings)
    {
        _chat = chat;
        _settings = settings;
        RegisterAumid();
        CheckOsSetting();
        _chat.MessageReceived += OnMessageReceived;
    }

    private static void RegisterAumid()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(
                @"SOFTWARE\Classes\AppUserModelId\" + AppId);
            key.SetValue("DisplayName", "MapManager");
            var icon = Path.Combine(AppContext.BaseDirectory, "GUI", "Assets", "mm_logo.ico");
            if (File.Exists(icon))
                key.SetValue("IconUri", icon);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Notif] RegisterAumid failed: {ex}");
        }
    }

    private void CheckOsSetting()
    {
        try
        {
            var setting = ToastNotificationManager.CreateToastNotifier(AppId).Setting;
            OsNotificationsBlocked = setting != NotificationSetting.Enabled;
            if (OsNotificationsBlocked)
                Debug.WriteLine($"[Notif] OS notifications blocked: {setting}");
        }
        catch { OsNotificationsBlocked = true; }
    }

    private void OnMessageReceived(ChatChannel channel, ChatMessage message)
    {
        if (!_settings.NotificationsEnabled) return;

        var ownNick = _settings.IrcNickname;
        if (!string.IsNullOrEmpty(ownNick) &&
            message.Sender.Equals(ownNick, StringComparison.OrdinalIgnoreCase)) return;

        bool isActive = Dispatcher.UIThread.Invoke(() =>
        {
            var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            return lifetime?.MainWindow?.IsActive ?? false;
        });
        if (isActive) return;

        bool shouldNotify = channel.IsPrivate
            ? !_settings.MutedUsers.Any(u => u.Equals(message.Sender, StringComparison.OrdinalIgnoreCase))
            : IsHighlight(message.Message);

        if (!shouldNotify) return;

        var ch = channel.Name;
        var sender = message.Sender;
        var body = message.Message;
        Dispatcher.UIThread.Post(() => ShowToast(ch, sender, body));
    }

    private bool IsHighlight(string text)
    {
        var ownNick = _settings.IrcNickname;

        if (!string.IsNullOrEmpty(ownNick) &&
            text.Contains(ownNick, StringComparison.OrdinalIgnoreCase)) return true;

        return _settings.HighlightKeywords.Any(kw =>
            !string.IsNullOrWhiteSpace(kw) &&
            text.Contains(kw, StringComparison.OrdinalIgnoreCase));
    }

    private void ShowToast(string channel, string sender, string body)
    {
        try
        {
            var avatarUri = GetAvatarFileUri(sender);
            var logoTag = avatarUri != null
                ? $"<image placement=\"appLogoOverride\" hint-crop=\"circle\" src=\"{avatarUri}\"/>"
                : "";

            var audioTag = _settings.NotificationSoundEnabled ? "" : "<audio silent=\"true\"/>";

            var label = channel.StartsWith('#') ? $"#{Esc(channel[1..])}" : "PM";

            var xml = new XmlDocument();
            xml.LoadXml(
                $"""
                 <toast>
                   <visual>
                     <binding template="ToastGeneric">
                       {logoTag}
                       <text hint-style="title">{Esc(sender)}</text>
                       <text hint-style="captionSubtle">{label}</text>
                       <text hint-maxLines="3">{Esc(body)}</text>
                     </binding>
                   </visual>
                   {audioTag}
                 </toast>
                 """);

            ToastNotificationManager.CreateToastNotifier(AppId)
                .Show(new ToastNotification(xml));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Notif] ShowToast failed: {ex}");
        }
    }

    private static string? GetAvatarFileUri(string username)
    {
        var path = Path.GetFullPath(
            Path.Combine("cache", "avatars", SanitizeKey($"chat_{username}") + ".png"));
        return File.Exists(path) ? new Uri(path).AbsoluteUri : null;
    }

    private static string SanitizeKey(string key)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = key.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
            if (Array.IndexOf(invalid, chars[i]) >= 0)
                chars[i] = '_';
        return new string(chars);
    }

    private static string Esc(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
