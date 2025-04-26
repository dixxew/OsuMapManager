using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace MapManager.GUI.Converters;

public class BoolToChatItemConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (bool)value ? "Primary" : "Accent";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}