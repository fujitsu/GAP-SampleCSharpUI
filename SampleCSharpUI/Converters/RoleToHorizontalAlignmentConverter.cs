using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SampleCSharpUI.Converters
{
    public class RoleToHorizontalAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string && !string.IsNullOrEmpty((string)value))
            {
                var s = value.ToString().Trim().ToLowerInvariant();
                return s == "user" ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
            else
            {
                return HorizontalAlignment.Left;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}