using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LibrarySystem.Tools.Converters;

public class NodeTypeToRailColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is string t && t == "Author"
            ? new SolidColorBrush(Color.FromRgb(0xE3, 0xB3, 0x41))  // amber
            : new SolidColorBrush(Color.FromRgb(0x7C, 0x16, 0xFF)); // violet

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
