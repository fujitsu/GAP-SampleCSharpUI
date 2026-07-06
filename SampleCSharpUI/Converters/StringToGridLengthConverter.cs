using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SampleCSharpUI.Converters
{
    public class StringToGridLengthConverter : IValueConverter
    {
        // ConverterParameter に行の標準高さ（例: 42）を渡せます。
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var id = value as string;
            double height = 42;
            if (parameter != null)
            {
                double.TryParse(parameter.ToString(), out var p);
                height = p;
            }

            if (string.IsNullOrEmpty(id))
            {
                if (height > 0)
                { 
                    height = 0;
                }
                else
                {
                    height = -height;
                }
                return new GridLength(height); // 空文字なら高さ 0（行を消す）
            }
            else
            {
                if (height < 0)
                {
                    height = 0;
                }
                return new GridLength(height); // ID があれば元の高さを返す
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}