using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MapManager.GUI.Converters;

public class RankToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var hex = (value as string)?.ToUpperInvariant() switch
        {
            "XH" or "X"  => "#FFD700", // SS — gold
            "SH"         => "#C8E6FF", // Silver S — light silver-blue
            "S"          => "#66CCFF", // S — sky blue
            "A"          => "#82EE00", // A — lime green
            "B"          => "#5588FF", // B — blue
            "C"          => "#BB55FF", // C — purple
            "D"          => "#FF4455", // D — red
            _            => "#AAAAAA"
        };
        return new SolidColorBrush(Color.Parse(hex));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class RankToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (value as string)?.ToUpperInvariant() switch
        {
            "X"  => "SS",
            "XH" => "SS",
            _    => value as string ?? ""
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
