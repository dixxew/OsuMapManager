using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MapManager.GUI.Converters;

public class StarRatingToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double stars = value switch
        {
            double d => d,
            float f => f,
            _ => 0
        };

        // osu!-подобный спектр сложности
        var hex = stars switch
        {
            < 2.0 => "#4FC0FF", // easy — голубой
            < 2.7 => "#7CFF4F", // normal — зелёный
            < 4.0 => "#F6F05C", // hard — жёлтый
            < 5.3 => "#FF4E6F", // insane — красно-розовый
            < 6.5 => "#C645B8", // expert — пурпурный
            _ => "#8C68F5"      // expert+ — фиолетовый (вместо чёрного, чтобы читался на тёмной теме)
        };

        return new SolidColorBrush(Color.Parse(hex));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
