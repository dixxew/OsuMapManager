using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MapManager.GUI.Converters;

public class RankedStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // сравнение по имени, чтобы не зависеть от номеров значений enum'а osu.Shared.SubmissionStatus
        var hex = value?.ToString() switch
        {
            "Ranked" => "#6BE585",    // зелёный
            "Approved" => "#45D9A8",  // бирюзовый
            "Qualified" => "#4FC0FF", // голубой
            "Loved" => "#FF66AB",     // розовый
            "Pending" => "#F6CD45",   // жёлтый
            _ => "#666B7A"            // graveyard / not submitted / unknown — серый
        };

        return new SolidColorBrush(Color.Parse(hex));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
