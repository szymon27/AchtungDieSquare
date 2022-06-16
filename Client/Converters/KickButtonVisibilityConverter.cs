using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Client.Converters
{
    public class KickButtonVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            int ownerId = System.Convert.ToInt32(values[0]);
            int clientId = System.Convert.ToInt32(values[1]);
            int elementId = System.Convert.ToInt32(values[2]);

            if(clientId == ownerId)
            {
                if(clientId == elementId)
                    return Visibility.Collapsed;
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
