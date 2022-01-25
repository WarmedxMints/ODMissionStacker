using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ODMissionStacker.Missions
{
    /// <summary>
    /// Interaction logic for AddClipboardSourceView.xaml
    /// </summary>
    public partial class AddClipboardSourceView : Window
    {
        public AddClipboardSourceView()
        {
            InitializeComponent();
        }

        public string TargetSystem => TargetSystemName.Text;
        public string SourceSystem => SystemName.Text;
        public string SourceStation => StationName.Text;

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        // Close
        private void CommandBinding_Executed_Close(object sender, ExecutedRoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
