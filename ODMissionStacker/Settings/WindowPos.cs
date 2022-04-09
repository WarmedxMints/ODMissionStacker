using ODMissionStacker.Utils;
using System.Windows;

namespace ODMissionStacker.Settings
{
    public class WindowPos : PropertyChangeNotify
    {
        private double top;
        private double left;
        private double height = 850;
        private double width = 1500;
        private WindowState state = WindowState.Normal;

        public double Top { get => top; set { top = value; OnPropertyChanged(); } }
        public double Left { get => left; set { left = value; OnPropertyChanged(); } }
        public double Height { get => height; set { height = value; OnPropertyChanged(); } }
        public double Width { get => width; set { width = value; OnPropertyChanged(); } }
        public WindowState State { get => state; set { state = value; OnPropertyChanged(); } }
    }
}
