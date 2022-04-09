using System;
using System.Globalization;
using System.Windows.Data;

namespace ODMissionStacker.Utils
{
    public class UtcToLocalDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return DateTime.MinValue;
            }

            if (value is DateTime time)
            {
                return time.ToLocalTime();
            }

            if (DateTime.TryParse(value?.ToString(), out DateTime parsedResult))
            {
                return parsedResult.ToLocalTime();
            }

            return DateTime.MinValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
