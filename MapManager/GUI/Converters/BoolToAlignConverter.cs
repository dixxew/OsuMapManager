using Avalonia.Data.Converters;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapManager.GUI.Converters;

public class BoolToAlignConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (bool)value ? HorizontalAlignment.Right : HorizontalAlignment.Left;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}