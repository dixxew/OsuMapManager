using Avalonia.Controls;
using MapManager.GUI.Services;
using ReactiveUI;
using static MapManager.GUI.Services.NavigationService;

namespace MapManager.GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
private readonly NavigationService _navigationService;

    public MainWindowViewModel(NavigationService navigationService)
    {
        _navigationService = navigationService;

        // подписка на главный контент
        _navigationService.Subscribe(NavigationTarget.MainContent, control =>
        {
            Content = control;
        });

        // старт с Greetings
        _navigationService.SetContent(NavigationTarget.MainContent, typeof(GreetingsViewModel));
    }

    private UserControl _content;

    public UserControl Content
    {
        get => _content;
        set => this.RaiseAndSetIfChanged(ref _content, value);
    }


}
