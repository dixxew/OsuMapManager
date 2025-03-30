using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace MapManager.GUI.Converters;
public class TreeViewWidthByParentConveter : IValueConverter
{
    public double Subtract { get; set; } = 32;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width)
            return width - Subtract;
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
