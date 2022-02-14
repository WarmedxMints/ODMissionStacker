using System;
using System.Globalization;
using System.Windows.Data;

namespace ODMissionStacker.Utils
{
    public class BoolToRowAlternation : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is bool showLines)
            {
                return showLines ? 2 : 1;
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
