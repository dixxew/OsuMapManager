using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace MapManager.GUI.Converters;

public class MessageWidthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width)
            return width * 2 / 3 - 32;

        return 200;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}