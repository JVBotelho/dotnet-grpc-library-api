using System;
using System.Globalization;
using System.Windows.Data;

namespace LibrarySystem.Tools.Converters
{
    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMonitoring && isMonitoring)
            {
                return "Stop"; // Or "Stop Monitoring"
            }
            return "Start"; // Or "Start Monitoring"
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("One-way conversion only.");
        }
    }
}