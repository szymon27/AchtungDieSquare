using System;
using System.Globalization;
using System.Windows.Data;

namespace Client.Converters
{
    public class MyColorToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Models.Color color = value as Models.Color;
            if (color == null)
                return System.Windows.Media.Brushes.Black;
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
