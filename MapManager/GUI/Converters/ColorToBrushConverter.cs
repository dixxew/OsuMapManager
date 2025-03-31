using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MapManager.GUI.Converters;

public class ColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Color c ? new SolidColorBrush(c) : Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is SolidColorBrush b ? b.Color : default;
    }
}
