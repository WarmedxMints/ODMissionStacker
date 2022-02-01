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
            DestinationSystem = e.DestinationSystem;
            TargetFaction = e.TargetFaction;
            LocalisedName = e.LocalisedName;
            Reward = e.Reward;
            KillCount = e.KillCount ?? 0;
            Wing = e.Wing;
            CollectionTime = e.Timestamp;
            ExpireTime = e.Expiry ?? DateTime.Now;
        }

        private int kills;
        private MissionState currentState;
        private DateTime expireTime;
        private bool highlight;

        public string SourceSystem { get; set; }
        public long SystemAddress { get; set; }
        public string SourceStation { get; set; }
        public long MissionID { get; set; }
        public string IssuingFaction { get; set; }
        public string DestinationSystem { get; set; }
        public string TargetFaction { get; set; }
        public string LocalisedName { get; set; }
        public int Reward { get; set; }
        public int KillCount { get; set; }
        public int Kills
        {
            get => kills;
            set
            {
                kills = Math.Clamp(value, 0, KillCount);

                if (currentState is MissionState.Active or MissionState.Redirectied)
                {
                    CurrentState = kills >= KillCount ? MissionState.Redirectied : MissionState.Active;
                }

                OnPropertyChanged();
            }
        }

        public int KillsWithoutStateChange
        {
            get => kills;
            set { kills = Math.Clamp(value, 0, KillCount); OnPropertyChanged("Kills"); }
        }

        public bool Wing { get; set; }
        public MissionState CurrentState { get => currentState; set { currentState = value; OnPropertyChanged(); } }
        public DateTime CollectionTime { get; set; }
        public DateTime ExpireTime { get => expireTime; set { expireTime = value; } }
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
                                                     $"Delete Mission from {IssuingFaction} at {SourceStation} for {KillCount} kills?",
                                                     MessageBoxButton.YesNo);

            if (ret == MessageBoxResult.Yes)
            {
                container?.DeleteMission(this);
            }
        }

        private void MarkAsActive(object sender, RoutedEventArgs e)
        {
            MessageBoxResult ret = ODMessageBox.Show(Application.Current.Windows.OfType<MainWindow>().First(),
                                                     $"Mark Mission from {IssuingFaction} at {SourceStation} for {KillCount} kills as Active?",
                                                     MessageBoxButton.YesNo);

            if (ret == MessageBoxResult.Yes)
            {
                CurrentState = MissionState.Active;
                container?.MoveMission(this, true);
                OnPropertyChanged("ContextMenu");
            }
        }

        private void MarkAsAbandonded(object sender, RoutedEventArgs e)
        {
            MessageBoxResult ret = ODMessageBox.Show(Application.Current.Windows.OfType<MainWindow>().First(),
                                                     $"Mark Mission from {IssuingFaction} at {SourceStation} for {KillCount} kills as Abandonded?",
                                                     MessageBoxButton.YesNo);

            if (ret == MessageBoxResult.Yes)
            {
                CurrentState = MissionState.Abandonded;
                container?.MoveMission(this, false);
                OnPropertyChanged("ContextMenu");
            }
        }

        private void MarkAsCompleted(object sender, RoutedEventArgs e)
        {
            MessageBoxResult ret = ODMessageBox.Show(Application.Current.Windows.OfType<MainWindow>().First(),
                                                     $"Mark Mission {IssuingFaction} at {SourceStation} for {KillCount} kills as Completed?",
                                                     MessageBoxButton.YesNo);

            if (ret == MessageBoxResult.Yes)
            {
                CurrentState = MissionState.Complete;
                Kills = KillCount;
                container?.MoveMission(this, false);
                OnPropertyChanged("ContextMenu");
            }
        }

        private void AddToMissionSource(object sender, RoutedEventArgs e)
        {
            MessageBoxResult ret = ODMessageBox.Show(Application.Current.Windows.OfType<MainWindow>().First(),
                                                     $"Add {SourceSystem} - {SourceStation} to Mission Source Clipboard?",
                                                     MessageBoxButton.YesNo);

            if (ret == MessageBoxResult.Yes)
            {
                container?.AddToMissionSourceClipBoard(this);
            }
        }

        public void SetContainer(MissionsContainer container) => this.container = container;
    }
}
