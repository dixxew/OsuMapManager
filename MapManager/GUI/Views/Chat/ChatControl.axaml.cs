using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MapManager.GUI.Models.Chat;
using MapManager.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapManager.GUI.Views;

public partial class ChatControl : UserControl
{
    private ScrollViewer? _scroller;
    private ScrollBar? _vScrollBar;
    private ItemsControl? _messagesList;
    private bool _isUserAtBottom = true;

    // Tab completion state
    private string? _lastCompletion;
    private int _tabCycleIndex;
    private List<string> _tabCandidates = [];

    public ChatControl()
    {
        InitializeComponent();

        this.AttachedToVisualTree += (_, _) =>
        {
            _scroller = this.FindControl<ScrollViewer>("MessagesScroll");
            _messagesList = this.FindControl<ItemsControl>("MessagesList");
            var messageInput = this.FindControl<TextBox>("MessageInput");

            if (messageInput != null)
                messageInput.AddHandler(KeyDownEvent, OnInputKeyDown, RoutingStrategies.Tunnel);

            if (_scroller != null)
            {
                Task.Run(async () =>
                {
                    _vScrollBar = null;
                    while (_vScrollBar == null)
                    {
                        await Task.Delay(200);
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            _vScrollBar = _scroller
                                .GetVisualDescendants()
                                .OfType<ScrollBar>()
                                .FirstOrDefault(sb => sb.Orientation == Orientation.Vertical);
                        });
                    }
                    _vScrollBar.ValueChanged += OnScrollBarValueChanged;
                });
            }

            if (DataContext is ChatViewModel vm)
            {
                vm.CurrentChannelMessageReceived += OnMessageReceived;
                vm.SelectedChannelChanged += OnChannelChanged;
            }
        };
    }

    // ── input key handling ───────────────────────────────────────────────────

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox tb) return;
        var vm = DataContext as ChatViewModel;
        if (vm == null) return;

        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            vm.Send();
            _lastCompletion = null;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Tab)
        {
            e.Handled = true;
            DoTabComplete(tb, vm);
            return;
        }

        // Any other key resets tab completion cycle
        _lastCompletion = null;
    }

    private void DoTabComplete(TextBox tb, ChatViewModel vm)
    {
        var text = tb.Text ?? "";
        var caret = tb.CaretIndex;
        var beforeCaret = text[..Math.Min(caret, text.Length)];

        var atIdx = beforeCaret.LastIndexOf('@');
        if (atIdx < 0) return;

        var currentWord = beforeCaret[(atIdx + 1)..];

        // Detect cycling: current word matches the last completed nick (case-insensitive)
        bool isCycling = _lastCompletion != null
            && currentWord.Equals(_lastCompletion, StringComparison.OrdinalIgnoreCase);

        if (!isCycling)
        {
            _tabCandidates = (vm.SelectedChannel?.Users ?? [])
                .Select(u => u.Name)
                .Where(n => n.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
            _tabCycleIndex = 0;
        }
        else
        {
            _tabCycleIndex = (_tabCycleIndex + 1) % Math.Max(1, _tabCandidates.Count);
        }

        if (_tabCandidates.Count == 0) return;

        var candidate = _tabCandidates[_tabCycleIndex];
        _lastCompletion = candidate;

        var afterCaret = text[Math.Min(caret, text.Length)..].TrimStart();
        var newText = text[..atIdx] + "@" + candidate + " " + afterCaret;
        var newCaret = atIdx + 1 + candidate.Length + 1;

        vm.InputMessage = newText;
        tb.Text = newText;
        tb.CaretIndex = Math.Min(newCaret, newText.Length);
    }

    // ── scroll handling ──────────────────────────────────────────────────────

    private void OnScrollBarValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_vScrollBar == null) return;
        _isUserAtBottom = _vScrollBar.Value >= _vScrollBar.Maximum - 0.5;
    }

    private void OnMessageReceived()
    {
        if (!_isUserAtBottom) return;
        ScrollToBottom();
    }

    private void OnChannelChanged()
    {
        _isUserAtBottom = true;
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        Dispatcher.UIThread.InvokeAsync(() => _scroller?.ScrollToEnd(), DispatcherPriority.Background);
    }
}
