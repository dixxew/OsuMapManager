using Avalonia.Controls;
using Avalonia.Controls.Templates;
using MapManager.GUI.ViewModels;
using System;
using System.Collections.Generic;

namespace MapManager.GUI
{
    public class ViewLocator : IDataTemplate
    {
        private readonly Dictionary<Type, Type> _viewMap = new();

        public void Register<TViewModel, TView>()
            where TViewModel : ViewModelBase
            where TView : Control, new()
        {
            _viewMap[typeof(TViewModel)] = typeof(TView);
        }

        public Control? Build(object? param)
        {
            if (param is null)
                return null;

            var vmType = param.GetType();

            if (_viewMap.TryGetValue(vmType, out var viewType))
            {
                var view = (Control)Activator.CreateInstance(viewType)!;
                view.DataContext = param;
                return view;
            }

            return new TextBlock { Text = $"Not Found View for: {vmType.Name}" };
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}
