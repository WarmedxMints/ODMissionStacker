using System;
using System.Windows;
using System.Windows.Controls;

namespace ODMissionStacker.CustomControls
{
    /// <summary>
    /// Interaction logic for KillCountBox.xaml
    /// </summary>
    public partial class KillCountBox : UserControl
    {
        public KillCountBox()
        {
            InitializeComponent();
        }
        public int ButtonStep
        {
            get => (int)GetValue(ButtonStepProperty);
            set => SetValue(ButtonStepProperty, value);
        }

        public static readonly DependencyProperty ButtonStepProperty =
            DependencyProperty.Register("ButtonStep", typeof(int), typeof(KillCountBox), new UIPropertyMetadata(1));

        public int Minimum
        {
            get => (int)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(int), typeof(KillCountBox), new UIPropertyMetadata(0));

        public int Maximum
        {
            get => (int)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(int), typeof(KillCountBox), new UIPropertyMetadata(1));

        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(int), typeof(KillCountBox), new FrameworkPropertyMetadata(default(int), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));


        public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(
        "Click", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(KillCountBox));

        public event RoutedEventHandler Click
        {
            add { AddHandler(ClickEvent, value); }
            remove { RemoveHandler(ClickEvent, value); }
        }

        private void RaiseClickEvent()
        {
            RoutedEventArgs newEventArgs = new(ClickEvent);
            RaiseEvent(newEventArgs);
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            Value = Math.Clamp(Value - ButtonStep, Minimum, Maximum);
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            Value = Math.Clamp(Value + ButtonStep, Minimum, Maximum);
        }

        private void Button_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RaiseClickEvent();
        }
    }
}
