using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LibrarySystem.Tools.Converters;

public class StatusTextToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush Live  = new(Color.FromRgb(0x3F, 0xB9, 0x50)); // green
    private static readonly SolidColorBrush Busy  = new(Color.FromRgb(0xE3, 0xB3, 0x41)); // amber
    private static readonly SolidColorBrush Error = new(Color.FromRgb(0xF8, 0x51, 0x49)); // red
    private static readonly SolidColorBrush Muted = new(Color.FromRgb(0x6E, 0x6E, 0x76)); // gray

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            if (s.Contains("LIVE") || s.Contains("Loaded") || s == "Ready") return Live;
            if (s.Contains("Connecting") || s.Contains("Fetching")) return Busy;
            if (s.Contains("Error") || s.Contains("Lost") || s.Contains("Failed")) return Error;
        }
        return Muted;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
