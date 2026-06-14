using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MapManager.GUI.Converters;

public class DownloadStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // сравнение по имени, чтобы не зависеть от номеров значений enum'а DownloadStatus
        var hex = value?.ToString() switch
        {
            "Completed" => "#6BE585",     // зелёный
            "Failed" => "#FF5C5C",        // красный
            "Cancelled" => "#A0A4AD",     // серый
            "Downloading" => "#4FC0FF",   // голубой
            "AwaitingLookup" => "#F6CD45",// жёлтый
            _ => "#C8CCD4"                // в очереди — светло-серый
        };

        return new SolidColorBrush(Color.Parse(hex));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
