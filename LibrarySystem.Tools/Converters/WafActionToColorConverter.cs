using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LibrarySystem.Tools.Converters;

public class WafActionToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush Critical = new(Color.FromRgb(0xF8, 0x51, 0x49));
    private static readonly SolidColorBrush Warning  = new(Color.FromRgb(0xE3, 0xB3, 0x41));
    private static readonly SolidColorBrush Info     = new(Color.FromRgb(0x3F, 0xB9, 0x50));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string action)
        {
            var a = action.ToUpperInvariant();
            if (a is "BLOCKED" or "BLOCKED/ALERT") return Critical;
            if (a is "FLAGGED" or "WARN" or "WARNING") return Warning;
            if (a is "ALLOWED" or "AUDIT") return Info;
        }
        return Warning;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
