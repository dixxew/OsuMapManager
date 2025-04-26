using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MapManager.GUI.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MapManager.GUI.Views;

public partial class ChatControl : UserControl
{
    private ScrollViewer? _scroller;
    private ScrollBar? _vScrollBar;
    private ItemsControl? _messagesList;
    private bool _isUserAtBottom = true;

    public ChatControl()
    {
        InitializeComponent();

        this.AttachedToVisualTree += (_, _) =>
        {
            _scroller = this.FindControl<ScrollViewer>("MessagesScroll");
            _messagesList = this.FindControl<ItemsControl>("MessagesList");

            if (_scroller != null)
            {
                Task.Run(async () =>
                {
                    // найдём вертикальный ScrollBar из шаблона
                    _vScrollBar = _scroller
                        .GetVisualDescendants()
                        .OfType<ScrollBar>()
                        .FirstOrDefault(sb => sb.Orientation == Orientation.Vertical);
                    while (_vScrollBar == null)
                    {
                        await Task.Delay(1000);
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            _vScrollBar = _scroller
                                .GetVisualDescendants()
                                .OfType<ScrollBar>()
                                .FirstOrDefault(sb => sb.Orientation == Orientation.Vertical);
                        });
                    }
                    
                    if (_vScrollBar != null)
                        _vScrollBar.ValueChanged += OnScrollBarValueChanged;
                });
            }

            // подписка на новые сообщения
            if (DataContext is ChatViewModel vm)
                vm.CurrentChannelMessageReceived += OnMessageReceived;
        };

        var vm = DataContext as ChatViewModel;
        if (vm != null)
        {
            vm.CurrentChannelMessageReceived += ScrollToEnd;
        }
    }

    private void OnScrollBarValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_vScrollBar == null) return;
        // считаем, что внизу, когда ползунок максимально близко к концу
        _isUserAtBottom = _vScrollBar.Value >= _vScrollBar.Maximum - 0.5;
    }

// при новом сообщении
    private void OnMessageReceived()
    {
        if (!_isUserAtBottom || _messagesList == null)
            return;

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            // получаем контейнер последнего элемента
            var idx = _messagesList.Items.Count - 1;
            var container = _messagesList
                .ItemContainerGenerator
                .ContainerFromIndex(idx);
            container?.BringIntoView(); // скроллим к нему
        }, DispatcherPriority.Background);
    }

    private void ScrollToEnd()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_scroller == null)
                return;

            if (_isUserAtBottom)
            {
                _scroller.Offset = new Vector(0, _scroller.Extent.Height - _scroller.Viewport.Height);
            }
        }, DispatcherPriority.Background);
    }


    private void ChatLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var vm = DataContext as ChatViewModel;
        if (vm != null)
        {
            vm.ConnectChat();
        }
    }
}