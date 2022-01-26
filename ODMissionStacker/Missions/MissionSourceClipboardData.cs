using ODMissionStacker.CustomMessageBox;
using ODMissionStacker.Utils;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;

namespace ODMissionStacker.Missions
{
    public class MissionSourceClipboardData : PropertyChangeNotify
    {
        private MissionsContainer container;

        public void SetContainer(MissionsContainer container) => this.container = container;

        private string detinationName;
        public string DestinationName { get => detinationName; set { detinationName = value; OnPropertyChanged(); } }

        private string systemName;
        public string SystemName { get => systemName; set { systemName = value; OnPropertyChanged(); } }

        private string stationName;
        public string StationName { get => stationName; set { stationName = value; OnPropertyChanged(); } }

        private ContextMenu contextMenu;
        [IgnoreDataMember]
        public ContextMenu ContextMenu
        {
            get
            {
                if(contextMenu is not null)
                {
                    return contextMenu;
                }

                contextMenu = new();

                MenuItem item = new()
                {
                    Header = $"Copy {SystemName} To Clipboard",
                    Tag = SystemName
                };
                item.Click += CopyToClipboard;
                _ = contextMenu.Items.Add(item);

                item = new()
                {
                    Header = $"Copy {StationName} To Clipboard",
                    Tag = StationName
                };
                item.Click += CopyToClipboard;
                _ = contextMenu.Items.Add(item);

                item = new()
                {
                    Header = $"Copy {DestinationName} To Clipboard",
                    Tag = DestinationName
                };
                item.Click += CopyToClipboard;
                _ = contextMenu.Items.Add(item);

                item = new()
                {
                    Header = $"Delete Entry",
                };
                item.Click += DeleteEntry;
                _ = contextMenu.Items.Add(item);

                return contextMenu;
            }
        }

        private void DeleteEntry(object sender, RoutedEventArgs e)
        {
            MessageBoxResult ret = ODMessageBox.Show(Application.Current.Windows.OfType<MainWindow>().First(), $"Delete Enrty {SystemName} - {SystemName} - {StationName}?", MessageBoxButton.YesNo);

            if (ret == MessageBoxResult.Yes)
            {
                container.RemoveClipboardEntry(this);
            }
        }

        private void CopyToClipboard(object sender, RoutedEventArgs e)
        {
            Helpers.SetClipboard(((MenuItem)sender).Tag);
        }
    }
}