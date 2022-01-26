using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ODMissionStacker.Missions
{
    /// <summary>
    /// Interaction logic for MissionHistoryReaderView.xaml
    /// </summary>
    public partial class MissionHistoryReaderView : Window
    {
        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !readingHistory;
        }

        // Close
        private void CommandBinding_Executed_Close(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private readonly MissionsContainer container;
        private bool readingHistory;
        public MissionHistoryReaderView(MissionsContainer container, bool autoRun = false)
        {
            this.container = container;
            InitializeComponent();
            if(autoRun)
            {
                ReadHistoryBtn_Click(null, null);
            }
        }

        private async void ReadHistoryBtn_Click(object sender, RoutedEventArgs e)
        {
            readingHistory = true;
            Header.Text = "Reading History, Please Wait....";
            ReadHistoryBtn.Content = "Reading History...";
            ReadHistoryBtn.IsEnabled = false;
            ReadHistoryBtn.Visibility = Visibility.Hidden;
            ProgressPanel.Visibility = Visibility.Visible;
            AutoClose.Visibility = Visibility.Visible;
            Progress<string> progress = new();
            progress.ProgressChanged += (_, newText) => ProgressText.Text = newText;
            TitleText.Text = "Processing Journal File : ";
            MissionHistoryBuilder builder = new(container.JournalWatcher, container.CommanderFID);

            Tuple<Dictionary<long, MissionData>, Dictionary<long, MissionData>> ret = await Task.Run(() => builder.GetHistory(progress));
            TitleText.Text = "Processing Mission ID : ";
            await Task.Run(() => container.ProcessHistory(ret.Item1, ret.Item2, progress));

            DialogResult = true;
        }
    }
}
