using EliteJournalReader.Events;
using ODMissionStacker.CustomMessageBox;
using ODMissionStacker.Utils;
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;

namespace ODMissionStacker.Missions
{
    public enum MissionState
    {
        Active,
        Redirectied,
        Complete,
        Abandonded,
        ReadyToTurnIn
    }

    public class MissionData : PropertyChangeNotify
    {
        private MissionsContainer container;

        public MissionData() { }
        public MissionData(MissionsContainer container, Station currentStation, MissionAcceptedEvent.MissionAcceptedEventArgs e)
        {
            this.container = container;

            SourceSystem = currentStation.StarSystem;
            SystemAddress = currentStation.SystemAddress;
            SourceStation = currentStation.StationName;
            MissionID = e.MissionID;
            IssuingFaction = e.Faction;
            destinationSystem = e.DestinationSystem;
            TargetFaction = e.TargetFaction;
            LocalisedName = e.LocalisedName;
            Reward = e.Reward;
            KillCount = e.KillCount ?? 0;
            Wing = e.Wing;
            CollectionTime = e.Timestamp;
            ExpireTime = e.Expiry ?? DateTime.Now;
        }

        private string sourceSystem;
        private long systemAddress;
        private string sourceStation;
        private long missionID;
        private string issuingFaction;
        private string destinationSystem;
        private string targetFaction;
        private string localisedName;
        private int reward;
        private int killCount;
        private int kills;
        private bool wing;
        private MissionState currentState;
        private DateTime collectionTime;
        private DateTime expireTime;
        private bool highlight;

        public string SourceSystem { get => sourceSystem; set { sourceSystem = value; OnPropertyChanged(); } }
        public long SystemAddress { get => systemAddress; set { systemAddress = value; OnPropertyChanged(); } }
        public string SourceStation { get => sourceStation; set { sourceStation = value; OnPropertyChanged(); } }
        public long MissionID { get => missionID; set { missionID = value; OnPropertyChanged(); } }
        public string IssuingFaction { get => issuingFaction; set { issuingFaction = value; OnPropertyChanged(); } }
        public string DestinationSystem { get => destinationSystem; set { destinationSystem = value; OnPropertyChanged(); } }
        public string TargetFaction { get => targetFaction; set { targetFaction = value; OnPropertyChanged(); } }
        public string LocalisedName { get => localisedName; set { localisedName = value; OnPropertyChanged(); } }
        public int Reward { get => reward; set { reward = value; OnPropertyChanged(); } }
        public int KillCount { get => killCount; set { killCount = value; OnPropertyChanged(); } }
        public int Kills
        {
            get => kills;
            set
            {
                kills = value;

                if (currentState is MissionState.Active or MissionState.Redirectied)
                {
                    CurrentState = kills >= killCount ? MissionState.Redirectied : MissionState.Active;
                }

                OnPropertyChanged();
            }
        }
        public bool Wing { get => wing; set { wing = value; OnPropertyChanged(); } }
        public MissionState CurrentState { get => currentState; set { currentState = value; OnPropertyChanged(); } }
        public DateTime CollectionTime { get => collectionTime; set { collectionTime = value; OnPropertyChanged(); } }
        public DateTime ExpireTime { get => expireTime; set { expireTime = value; OnPropertyChanged(); } }
        public bool Highlight { get => highlight; set { highlight = value; OnPropertyChanged(); } }

        [IgnoreDataMember]
        public ContextMenu ContextMenu
        {
            get
            {
                ContextMenu menu = new();

                MenuItem item = new()
                {
                    Header = $"Add {SourceSystem} - {SourceStation} To Mission Source Clipboard",
                };
                item.Click += AddToMissionSource;
                _ = menu.Items.Add(item);

                if (currentState != MissionState.Active)
                {
                    item = new()
                    {
                        Header = "Mark as Active"
                    };

                    item.Click += MarkAsActive;
                    _ = menu.Items.Add(item);
                }

                if (currentState != MissionState.Complete)
                {
                    item = new()
                    {
                        Header = "Mark as Completed"
                    };
                    item.Click += MarkAsCompleted;
                    _ = menu.Items.Add(item);
                }

                if (currentState != MissionState.Abandonded)
                {
                    item = new()
                    {
                        Header = "Mark as Abandonded"
                    };
                    item.Click += MarkAsAbandonded;
                    _ = menu.Items.Add(item);
                }

                item = new()
                {
                    Header = "Delete Mission"
                };

                item.Click += DeleteMission;
                _ = menu.Items.Add(item);

                return menu;
            }
        }

        private void DeleteMission(object sender, RoutedEventArgs e)
        {
            MessageBoxResult ret = ODMessageBox.Show(Application.Current.Windows.OfType<MainWindow>().First(),
                                                     $"Delete Mission from {issuingFaction} at {sourceStation} for {killCount} kills?",
                                                     MessageBoxButton.YesNo);

            if (ret == MessageBoxResult.Yes)
            {
                container.DeleteMission(this);
            }
        }

        private void MarkAsActive(object sender, RoutedEventArgs e)
        {
            MessageBoxResult ret = ODMessageBox.Show(Application.Current.Windows.OfType<MainWindow>().First(),
                                                     $"Mark Mission from {issuingFaction} at {sourceStation} for {killCount} kills as Active?",
                                                     MessageBoxButton.YesNo);

            if (ret == MessageBoxResult.Yes)
            {
                CurrentState = MissionState.Active;
                container.MoveMission(this, true);
                OnPropertyChanged("ContextMenu");
            }
        }

        private void MarkAsAbandonded(object sender, RoutedEventArgs e)
        {
            MessageBoxResult ret = ODMessageBox.Show(Application.Current.Windows.OfType<MainWindow>().First(),
                                                     $"Mark Mission from {issuingFaction} at {sourceStation} for {killCount} kills as Abandonded?",
                                                     MessageBoxButton.YesNo);

            if (ret == MessageBoxResult.Yes)
            {
                CurrentState = MissionState.Abandonded;
                container.MoveMission(this, false);
                OnPropertyChanged("ContextMenu");
            }
        }

        private void MarkAsCompleted(object sender, RoutedEventArgs e)
        {
            MessageBoxResult ret = ODMessageBox.Show(Application.Current.Windows.OfType<MainWindow>().First(),
                                                     $"Mark Mission {issuingFaction} at {sourceStation} for {killCount} kills as Completed?",
                                                     MessageBoxButton.YesNo);

            if (ret == MessageBoxResult.Yes)
            {
                CurrentState = MissionState.Complete;
                Kills = killCount;
                container.MoveMission(this, false);
                OnPropertyChanged("ContextMenu");
            }
        }

        private void AddToMissionSource(object sender, RoutedEventArgs e)
        {
            MessageBoxResult ret = ODMessageBox.Show(Application.Current.Windows.OfType<MainWindow>().First(),
                                                     $"Add {sourceSystem} - {sourceStation} to Mission Source Clipboard?",
                                                     MessageBoxButton.YesNo);

            if (ret == MessageBoxResult.Yes)
            {
                container.AddToMissionSourceClipBoard(this);
            }
        }

        public void SetContainer(MissionsContainer container) => this.container = container;
    }
}
