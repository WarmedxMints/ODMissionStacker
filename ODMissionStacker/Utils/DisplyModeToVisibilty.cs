using ODMissionStacker.Settings;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ODMissionStacker.Utils
{
    public class DisplyModeToVisibilty : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DisplayMode current = (DisplayMode)value;

            DisplayMode desired = (DisplayMode)Enum.Parse(typeof(DisplayMode), (string)parameter);

            return current == desired ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class DisplyModeToVisibiltyReversed : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DisplayMode current = (DisplayMode)value;

            DisplayMode undesired = (DisplayMode)Enum.Parse(typeof(DisplayMode), (string)parameter);

            return current == undesired ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
