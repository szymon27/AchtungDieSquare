using System;
using System.Globalization;
using System.Windows.Data;

namespace Client.Converters
{
    public class PlayersInRoomConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                int players = System.Convert.ToInt32(values[0]);
                int maxPlayers = System.Convert.ToInt32(values[1]);

                return $"{players}/{maxPlayers}";
            }
            catch (Exception ex)
            {
                return "-/-";
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
