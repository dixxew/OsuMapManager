using Avalonia.Controls;
using MapManager.GUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;

public class NavigationService
{
    public enum NavigationTarget
    {
        MainContent,
        DialogContent,
        MainBlockContent
    }

    private readonly ViewLocator _viewLocator;
    private readonly ViewModelLocator _viewModelLocator = new();
    private readonly ILogger<NavigationService> _logger;

    public NavigationService(ViewLocator viewLocator, ILogger<NavigationService> logger)
    {
        _viewLocator = viewLocator;
        _logger = logger;
    }

    private readonly Dictionary<NavigationTarget, Action<UserControl>> _targetChangedEvents = new();

    public void Subscribe(NavigationTarget target, Action<UserControl> handler)
    {
        if (_targetChangedEvents.ContainsKey(target))
            _targetChangedEvents[target] += handler;
        else
            _targetChangedEvents[target] = handler;
    }

    /// <summary>
    /// Создаёт ViewModel из DI по типу и ставит её как контент.
    /// </summary>
    public void SetContent(NavigationTarget target, Type viewModelType)
    {
        if (!typeof(ViewModelBase).IsAssignableFrom(viewModelType))
            throw new ArgumentException("Тип должен быть ViewModelBase", nameof(viewModelType));

        var vm = (ViewModelBase)_viewModelLocator.GetByType(viewModelType);
        SetContent(target, vm);
    }

    /// <summary>
    /// Ставим уже готовую VM (если нужно вручную)
    /// </summary>
    public void SetContent(NavigationTarget target, ViewModelBase viewModel)
    {
        _logger.LogDebug("Navigate {Target} → {ViewModel}", target, viewModel.GetType().Name);
        var view = _viewLocator.Build(viewModel);

        if (view is not UserControl control)
        {
            _logger.LogError("View for {ViewModel} is not a UserControl", viewModel.GetType().Name);
            throw new Exception($"View для {viewModel.GetType().Name} не является UserControl");
        }

        if (_targetChangedEvents.TryGetValue(target, out var handler))
            handler.Invoke(control);
        else
            _logger.LogWarning("Navigate {Target}: no subscriber registered", target);
    }

    public Action SwitchChatContent;
}
