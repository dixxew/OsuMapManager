using Avalonia.Threading;
using System.Collections.ObjectModel;

namespace MapManager.GUI.Models.Chat;

// Новый класс для хранения сообщений конкретного чата.
public class ChatChannel
{
    public string Name { get; }
    public ObservableCollection<ChatMessage> Messages { get; } = new();
    public ObservableCollection<ChatUser> Users { get; } = new();
    public bool IsUsersLoaded { get; set; } = false;

    public ChatChannel(string name)
    {
        Name = name;
    }

    public void AddMessage(ChatMessage message)
    {
        // Добавление сообщений в UI-потоке.
        Dispatcher.UIThread.Invoke(() =>
        {
            Messages.Add(message);
        });
    }
}
