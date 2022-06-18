using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Client.Converters
{
    public class IsOwnerToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try { 
                int ownerId = System.Convert.ToInt32(values[0]);
                int clientId = System.Convert.ToInt32(values[1]);

                if (clientId == ownerId)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
            catch(Exception ex)
            {
                return Visibility.Collapsed;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
