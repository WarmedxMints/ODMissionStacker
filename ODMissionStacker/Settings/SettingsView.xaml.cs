using ODMissionStacker.CustomMessageBox;
using ODMissionStacker.Missions;
using Ookii.Dialogs.Wpf;
using System.Windows;
using System.Windows.Input;

namespace ODMissionStacker.Settings
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : Window
    {
        public AppSettings ApplicationSettings { get; set; }
        public MissionsContainer MissionsContainer { get; set; }
        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        // Close
        private void CommandBinding_Executed_Close(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        public SettingsView(AppSettings settings, MissionsContainer missionsContainer)
        {
            ApplicationSettings = settings;
            MissionsContainer = missionsContainer;
            InitializeComponent();
        }

        private void PurgeCompleted_Click(object sender, RoutedEventArgs e)
        {
            var del = ODMessageBox.Show(this, "Purge All Completed Missions?", MessageBoxButton.YesNo);

            if (del == MessageBoxResult.Yes)
            {
                MissionsContainer.PurgeMissions(MissionState.Complete);
            }
        }

        private void PurgeAbandonded_Click(object sender, RoutedEventArgs e)
        {
            var del = ODMessageBox.Show(this, "Purge All Abandonded Missions?", MessageBoxButton.YesNo);

            if (del == MessageBoxResult.Yes)
            {
                MissionsContainer.PurgeMissions(MissionState.Abandonded);
            }
        }

        private void PurgeFailed_Click(object sender, RoutedEventArgs e)
        {
            var del = ODMessageBox.Show(this, "Purge All Failed Missions?", MessageBoxButton.YesNo);

            if (del == MessageBoxResult.Yes)
            {
                MissionsContainer.PurgeMissions(MissionState.Failed);
            }
        }

        private void ReadHistory_Click(object sender, RoutedEventArgs e)
        {
            MissionHistoryReaderView ret = new(MissionsContainer)
            {
                Owner = this
            };

            if ((bool)ret.ShowDialog())
            {
                MissionsContainer.SaveData();
            }
        }

        private void BrowseJournalFolder_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog folder = new()
            {
                Multiselect = false,
                Description = "Select ED Journal Folder",
                UseDescriptionForTitle = true
            };

            if (folder.ShowDialog().Value)
            {
                ApplicationSettings.CustomJournalPath = folder.SelectedPath;

                MissionsContainer.RestartWatcher();            
            }
        }

        private void ClearJournalFolder_Click(object sender, RoutedEventArgs e)
        {
            ApplicationSettings.CustomJournalPath = null;
            MissionsContainer.RestartWatcher();
        }
    }
}
