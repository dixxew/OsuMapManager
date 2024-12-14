using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MapManager.GUI.Converters
{
    internal class BoolConverter : IValueConverter
    {
        public object Convert(object? inputValue, Type targetType, object? parameter, CultureInfo culture)
        {
            if (inputValue is bool value)
                switch (parameter as string)
                {
                    case "PlayerPlay":
                        if (value)
                            return "PauseSolid";
                        else
                            return "PlaySolid";
                    case "PlayerHeart":
                        if (value)
                            return "HeartSolid";
                        else
                            return "HeartRegular";
                    case "PlayerRepeat":
                        if (value)
                            return "White";
                        else
                            return "Gray";
                    case "PlayerRandom":
                        if (value)
                            return "White";
                        else
                            return "Gray";
                    default:
                        return null;
                }
            return null;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return new Avalonia.Data.BindingNotification(value);
        }
    }
}
