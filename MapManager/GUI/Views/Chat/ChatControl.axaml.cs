using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using MapManager.GUI.ViewModels;

namespace MapManager.GUI.Views;

public partial class ChatControl : UserControl
{
    private ScrollViewer? _scroller;

    public ChatControl()
    {
        InitializeComponent();

        this.AttachedToVisualTree += (_, _) =>
            _scroller = this.FindControl<ScrollViewer>("MessagesScroll");

        var vm = DataContext as ChatViewModel;
        if (vm != null)
        {
            vm.CurrentChannelMessageReceived += ScrollToEnd;
        }
    }

    private void ScrollToEnd()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_scroller == null)
                return;

            // высота, на которую можно прокрутить
            var scrollableHeight = _scroller.Extent.Height - _scroller.Viewport.Height;
            // текущее смещение
            var offsetY = _scroller.Offset.Y;
            // считаем, что мы "внизу", если отступ до конца <= 5px
            var isAtBottom = scrollableHeight - offsetY <= 5;

            if (isAtBottom)
            {
                _scroller.Offset = new Vector(0, _scroller.Extent.Height);
            }
        });
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