using Avalonia.Controls;
using Avalonia.Threading;
using MapManager.GUI.Models;
using MapManager.GUI.Models.Chat;
using MapManager.GUI.Services;
using NAudio.Wave;
using ReactiveUI;
using SoundTouch.Net.NAudioSupport;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Timers;

namespace MapManager.GUI.ViewModels;

public class ChatViewModel : ViewModelBase
{


    private readonly ChatService _service;
    public ChatViewModel(ChatService service)
    {
        _service = service;
        _service.MessageReceived += MessageReceived;
    }


    public ObservableCollection<ChatChannel> Channels => _service.Channels;

    private ChatChannel _selectedChannel;

    public ChatChannel SelectedChannel
    {
        get => _selectedChannel;
        set => this.RaiseAndSetIfChanged(ref _selectedChannel, value);
    }

    private string _inputMessage;

    public string InputMessage
    {
        get => _inputMessage;
        set => this.RaiseAndSetIfChanged(ref _inputMessage, value);
    }



    public void ConnectChat()
    {
        _service.Connect();
    }

    public void Send()
    {
        _service.SendMessage(SelectedChannel.Name, InputMessage);
    }

    public event Action CurrentChannelMessageReceived;

    private void MessageReceived(ChatChannel channel, ChatMessage message)
    {
        if (channel.Equals(SelectedChannel))
            CurrentChannelMessageReceived?.Invoke();
    }
}