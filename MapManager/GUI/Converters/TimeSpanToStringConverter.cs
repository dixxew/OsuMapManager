using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace MapManager.GUI.Converters;
public class TimeSpanToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeSpan ts)
            return ts.ToString(@"mm\:ss");
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (TimeSpan.TryParseExact(value?.ToString(), @"mm\:ss", CultureInfo.InvariantCulture, out var ts))
            return ts;
        return null;
    }
}
