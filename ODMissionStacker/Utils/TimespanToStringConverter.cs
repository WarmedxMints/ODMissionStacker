using System;
using System.Globalization;
using System.Windows.Data;

namespace ODMissionStacker.Utils
{
    public class TimespanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value is not TimeSpan span)
            {
                return "?";
            }

            string formatted = string.Format("{0}{1}{2}",
                    span.Duration().Days > 0 ? string.Format("{0:0} d, ", span.Days) : string.Empty,
                    span.Duration().Hours > 0 ? string.Format("{0:0} h, ", span.Hours) : string.Empty,
                    span.Duration().Minutes > 0 ? string.Format("{0:0} m, ", span.Minutes) : string.Empty);

            if (formatted.EndsWith(", "))
            {
                formatted = formatted[..^2];
            }

            if (string.IsNullOrEmpty(formatted))
            {
                formatted = span.Duration().Seconds > 0 ? string.Format("{0:0} s", span.Seconds) : string.Format("{0:0} ms", span.Milliseconds);
            }

            return formatted;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
