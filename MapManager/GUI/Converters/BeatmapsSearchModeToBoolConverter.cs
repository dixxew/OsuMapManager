using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace MapManager.GUI.Converters;
public class BeatmapsSearchModeToBoolConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return Enum.Parse(targetType, parameter.ToString());
        return BindingOperations.DoNothing;
    }
}
