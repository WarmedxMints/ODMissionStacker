using EliteJournalReader;
using EliteJournalReader.Events;
using ODMissionStacker.CustomMessageBox;
using ODMissionStacker.Settings;
using ODMissionStacker.Utils;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ODMissionStacker.Missions
{
    public class MissionsContainer : PropertyChangeNotify
    {
        #region Private Values
        private string HorizonDataSaveFile => Path.Combine(Directory.GetCurrentDirectory(), "Data", $"[{CommanderFID}] HorizonsMissions.json");
        private string OdysseyDataSaveFile => Path.Combine(Directory.GetCurrentDirectory(), "Data", $"[{CommanderFID}] OdysseyMissions.json");

        private static readonly string clipboarDataSaveFile = Path.Combine(Directory.GetCurrentDirectory(), "Data", "MissionSourceClipboard.json");
        private string BountyDataSaveFile => Path.Combine(Directory.GetCurrentDirectory(), "Data", $"[{CommanderFID}] BountyData.json");

        private bool odyssey;

        private Station currentStation;

        private Dictionary<long, MissionData> horizonMissionsData, odysseyMissionsData;

        private readonly AppSettings appSettings;
        #endregion

        #region Public Properties
        public JournalWatcher JournalWatcher { get; private set; }

        public MissionsManager CurrentManager => appSettings.ViewDisplayMode switch
        {
            DisplayMode.Horizons => HorizionMissions,
            DisplayMode.Odyssey => OdysseyMissions,
            DisplayMode.Completed => CompletedMissions,
            _ => HorizionMissions,
        };

        private MissionsManager horizonMissions = new();
        private MissionsManager odysseyMissions = new();
        private MissionsManager completedMissions = new();
        private ObservableCollection<MissionSourceClipboardData> missionSourceClipboards = new();
        private ObservableCollection<BountyData> bounties = new();

        public MissionsManager HorizionMissions { get => horizonMissions; set { horizonMissions = value; OnPropertyChanged(); } }
        public MissionsManager OdysseyMissions { get => odysseyMissions; set { odysseyMissions = value; OnPropertyChanged(); } }
        public MissionsManager CompletedMissions { get => completedMissions; set { completedMissions = value; OnPropertyChanged(); } }
        public ObservableCollection<MissionSourceClipboardData> MissionSourceClipboards { get => missionSourceClipboards; set { missionSourceClipboards = value; OnPropertyChanged(); } }
        public ObservableCollection<BountyData> Bounties { get => bounties; set { bounties = value; OnPropertyChanged(); } }
        #endregion

        public MissionsContainer(AppSettings settings)
        {
            appSettings = settings;
            appSettings.CommanderChanged += AppSettings_CommanderChanged;
        }

        #region Init and Event Methods
        public void Init()
        {
            SetCurrentManager();

            RestartWatcher();
        }

        private void SubscribeToEvents()
        {
            JournalWatcher.GetEvent<FileheaderEvent>()?.AddHandler(OnFileHeaderEvent);

            JournalWatcher.GetEvent<LocationEvent>()?.AddHandler(OnLocationEvent);

            JournalWatcher.GetEvent<DockedEvent>()?.AddHandler(OnDockedAtStation);

            JournalWatcher.GetEvent<UndockedEvent>()?.AddHandler(OnUndockedFromStation);

            JournalWatcher.GetEvent<MissionAcceptedEvent>()?.AddHandler(OnMissionAccepted);

            JournalWatcher.GetEvent<MissionRedirectedEvent>()?.AddHandler(OnMissionRedirected);

            JournalWatcher.GetEvent<MissionCompletedEvent>()?.AddHandler(OnMissionCompleted);

            JournalWatcher.GetEvent<MissionAbandonedEvent>()?.AddHandler(OnMissionAbandonded);

            JournalWatcher.GetEvent<MissionFailedEvent>()?.AddHandler(OnMissionFailed);

            JournalWatcher.GetEvent<BountyEvent>()?.AddHandler(OnBoutnyEvent);

            JournalWatcher.GetEvent<CommanderEvent>()?.AddHandler(OnCommanderEvent);
        }

        private void UnSubscribeFromEvents()
        {
            JournalWatcher.GetEvent<FileheaderEvent>()?.RemoveHandler(OnFileHeaderEvent);

            JournalWatcher.GetEvent<LocationEvent>()?.RemoveHandler(OnLocationEvent);

            JournalWatcher.GetEvent<DockedEvent>()?.RemoveHandler(OnDockedAtStation);

            JournalWatcher.GetEvent<UndockedEvent>()?.RemoveHandler(OnUndockedFromStation);

            JournalWatcher.GetEvent<MissionAcceptedEvent>()?.RemoveHandler(OnMissionAccepted);

            JournalWatcher.GetEvent<MissionRedirectedEvent>()?.RemoveHandler(OnMissionRedirected);

            JournalWatcher.GetEvent<MissionCompletedEvent>()?.RemoveHandler(OnMissionCompleted);

            JournalWatcher.GetEvent<MissionAbandonedEvent>()?.RemoveHandler(OnMissionAbandonded);

            JournalWatcher.GetEvent<MissionFailedEvent>()?.RemoveHandler(OnMissionFailed);

            JournalWatcher.GetEvent<BountyEvent>()?.RemoveHandler(OnBoutnyEvent);

            JournalWatcher.GetEvent<CommanderEvent>()?.RemoveHandler(OnCommanderEvent);
        }

        public void RestartWatcher()
        {
            if (JournalWatcher is not null)
            {
                UnSubscribeFromEvents();
                JournalWatcher.StopWatching();
            }

            if (Directory.Exists(appSettings.JournalPath) == false)
            {
                _ = ODMessageBox.Show(Application.Current.Windows.OfType<MainWindow>().First(),
                                                         $"Journal Directory Not Found\nPlease Specify Journal Log Folder",
                                                         MessageBoxButton.OK);

                if (FindJournalDir() == false)
                {
                    return;
                }
            }

            JournalWatcher = new(appSettings.JournalPath);

            SubscribeToEvents();

            JournalWatcher.StartWatching().ConfigureAwait(false);
        }

        private bool FindJournalDir()
        {
            VistaFolderBrowserDialog folder = new()
            {
                Multiselect = false,
                Description = "Select ED Journal Folder",
                UseDescriptionForTitle = true
            };

            if (folder.ShowDialog().Value)
            {
                appSettings.CustomJournalPath = folder.SelectedPath;
                return true;
            }

            _ = ODMessageBox.Show(Application.Current.Windows.OfType<MainWindow>().First(),
                                            "Journal Directory Not Set\n\nA Valid Directory Is Required",
                                             MessageBoxButton.OK);
            return false;
        }

        private void OnFileHeaderEvent(object sender, FileheaderEvent.FileheaderEventArgs e)
        {
            if (JournalWatcher.ReadingHistory)
            {
                return;
            }

            odyssey = e.Odyssey;
        }

        private void OnLocationEvent(object sender, LocationEvent.LocationEventArgs e)
        {
            if (JournalWatcher.ReadingHistory)
            {
                return;
            }

            if (e.Docked == false)
            {
                return;
            }

            currentStation = new()
            {
                StarSystem = e.StarSystem,
                SystemAddress = e.SystemAddress,
                StationName = e.StationName
            };
        }

        private void OnDockedAtStation(object sender, DockedEvent.DockedEventArgs e)
        {
            if (JournalWatcher.ReadingHistory)
            {
                return;
            }

            currentStation = new()
            {
                StarSystem = e.StarSystem,
                SystemAddress = e.SystemAddress,
                StationName = e.StationName
            };

            if (odyssey)
            {
                OdysseyMissions.UpdateMissionsStates(currentStation);
                return;
            }

            HorizionMissions.UpdateMissionsStates(currentStation);
        }

        private void OnUndockedFromStation(object sender, UndockedEvent.UndockedEventArgs e)
        {
            if (JournalWatcher.ReadingHistory)
            {
                return;
            }

            currentStation = null;

            if (odyssey)
            {
                OdysseyMissions.UpdateMissionsStates(currentStation);
                return;
            }

            HorizionMissions.UpdateMissionsStates(currentStation);
        }

        private void OnMissionAccepted(object sender, MissionAcceptedEvent.MissionAcceptedEventArgs e)
        {
            //Ignore missions with no kills
            if (JournalWatcher.IsLive == false || currentStation == null || e.KillCount is null || e.KillCount == 0 || string.IsNullOrEmpty(e.TargetFaction) || e.TargetType == null || !e.TargetType.Contains("MissionUtil_FactionTag_Pirate", StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            MissionData missionData = new(this, currentStation, e);
            missionData.CurrentState = MissionState.Active;

            AddMissionToDictionary(ref odyssey ? ref odysseyMissionsData : ref horizonMissionsData, missionData);
            AddMissionToGui(odyssey ? OdysseyMissions : HorizionMissions, missionData);

            SaveData();
        }

        private void OnMissionRedirected(object sender, MissionRedirectedEvent.MissionRedirectedEventArgs e)
        {
            if (JournalWatcher.IsLive == false)
            {
                return;
            }

            if (odyssey ? odysseyMissionsData is null || !odysseyMissionsData.ContainsKey(e.MissionID) : horizonMissionsData is null || !horizonMissionsData.ContainsKey(e.MissionID))
            {
                return;
            }

            if (odyssey)
            {
                odysseyMissionsData[e.MissionID].CurrentState = MissionState.Redirectied;
                odysseyMissionsData[e.MissionID].Kills = odysseyMissionsData[e.MissionID].KillCount;
                OdysseyMissions.MissionRedirected(odysseyMissionsData[e.MissionID]);
                SaveData();
                return;
            }

            horizonMissionsData[e.MissionID].CurrentState = MissionState.Redirectied;
            horizonMissionsData[e.MissionID].Kills = horizonMissionsData[e.MissionID].KillCount;
            HorizionMissions.MissionRedirected(horizonMissionsData[e.MissionID]);
            SaveData();
        }

        private void OnMissionCompleted(object sender, MissionCompletedEvent.MissionCompletedEventArgs e)
        {
            if (JournalWatcher.IsLive == false)
            {
                return;
            }

            if (odyssey ? odysseyMissionsData is null || !odysseyMissionsData.ContainsKey(e.MissionID) : horizonMissionsData is null || !horizonMissionsData.ContainsKey(e.MissionID))
            {
                return;
            }

            MissionData missionData;

            if (odyssey)
            {
                missionData = odysseyMissionsData[e.MissionID];

                OdysseyMissions.RemoveMission(missionData);
            }
            else
            {
                missionData = horizonMissionsData[e.MissionID];

                HorizionMissions.RemoveMission(missionData);
            }

            missionData.CurrentState = MissionState.Complete;
            missionData.Reward = e.Reward;

            CompletedMissions.AddMission(missionData);
            SaveData();
        }

        private void OnMissionFailed(object sender, MissionFailedEvent.MissionFailedEventArgs e)
        {
            if (JournalWatcher.IsLive == false)
            {
                return;

            }
            if (odyssey ? odysseyMissionsData is null || !odysseyMissionsData.ContainsKey(e.MissionID) : horizonMissionsData is null || !horizonMissionsData.ContainsKey(e.MissionID))
            {
                return;
            }

            MissionData missionData;

            if (odyssey)
            {
                missionData = odysseyMissionsData[e.MissionID];

                OdysseyMissions.RemoveMission(missionData);
            }
            else
            {
                missionData = horizonMissionsData[e.MissionID];

                HorizionMissions.RemoveMission(missionData);
            }

            missionData.CurrentState = MissionState.Failed;
            missionData.Reward = 0;

            CompletedMissions.AddMission(missionData);
            SaveData();
        }

        private void OnMissionAbandonded(object sender, MissionAbandonedEvent.MissionAbandonedEventArgs e)
        {
            if (JournalWatcher.IsLive == false)
            {
                return;
            }

            if (odyssey ? odysseyMissionsData is null || !odysseyMissionsData.ContainsKey(e.MissionID) : horizonMissionsData is null || !horizonMissionsData.ContainsKey(e.MissionID))
            {
                return;
            }

            MissionData missionData;

            if (odyssey)
            {
                missionData = odysseyMissionsData[e.MissionID];

                OdysseyMissions.RemoveMission(missionData);
            }
            else
            {
                missionData = horizonMissionsData[e.MissionID];

                HorizionMissions.RemoveMission(missionData);
            }

            missionData.CurrentState = MissionState.Abandonded;
            missionData.Reward = 0;

            CompletedMissions.AddMission(missionData);
            SaveData();
        }

        private void OnBoutnyEvent(object sender, BountyEvent.BountyEventArgs e)
        {
            //Ignore skimmers, ground and zero value bounties.
            if (JournalWatcher.IsLive == false || e.VictimFaction.Contains("faction_none") || e.VictimFaction.Contains("faction_Pirate") || e.Target.Contains("suit") || e.TotalReward <= 0)
            {
                return;
            }

            TimeSpan timeSinceLastKill = new();

            if (Bounties.Any())
            {
                timeSinceLastKill = e.Timestamp - Bounties[^1].TimeStamp;
            }

            BountyData data = new(e, timeSinceLastKill);

            AddBounty(data);

            if (odyssey)
            {
                OdysseyMissions.OnBounty(data);
                SaveData();
                return;
            }

            HorizionMissions.OnBounty(data);
            SaveData();
        }

        private void OnCommanderEvent(object sender, CommanderEvent.CommanderEventArgs e)
        {
            if (JournalWatcher.ReadingHistory)
            {
                return;
            }

            Commander cmdr = appSettings.Commanders.FirstOrDefault(x => x.FID == e.FID);

            if (cmdr == default)
            {
                cmdr = new()
                {
                    FID = e.FID,
                    Name = e.Name
                };

                appSettings.Commanders.AddToCollection(cmdr);
                appSettings.CurrentCommander = cmdr;

                CompletedMissions.Missions.ClearCollection();
                HorizionMissions.Missions.ClearCollection();
                OdysseyMissions.Missions.ClearCollection();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBoxResult result = ODMessageBox.Show(Application.Current.Windows.OfType<MainWindow>().First(),
                                                         $"New Commander Detected - {cmdr.Name}\nWould you like to read mission history?",
                                                         MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes)
                    {
                        MissionHistoryReaderView reader = new(this, true);
                        reader.Owner = Application.Current.Windows.OfType<MainWindow>().First();
                        _ = reader.ShowDialog();
                    }
                });

                return;
            }

            appSettings.CurrentCommander = cmdr;
        }
        #endregion

        private void AppSettings_CommanderChanged(object sender, EventArgs e)
        {
            LoadData();
        }

        public void ProcessHistory(Dictionary<long, MissionData> horizonDictionary, Dictionary<long, MissionData> odysseyDisctionary, List<BountyData> bountiesData, IProgress<string> progress)
        {
            horizonMissionsData = new(horizonDictionary);
            odysseyMissionsData = new(odysseyDisctionary);

            HorizionMissions.Missions.ClearCollection();
            OdysseyMissions.Missions.ClearCollection();
            CompletedMissions.Missions.ClearCollection();
            Bounties.ClearCollection();

            //Horizons Data
            if (!(horizonDictionary is null && horizonDictionary.Count <= 0))
            {
                progress.Report("Horizon Missions");

                ProcessDictionary(horizonMissionsData, HorizionMissions);
            }
            if (!(odysseyDisctionary is null && odysseyDisctionary.Count <= 0))
            {
                progress.Report("Odyssey Missions");

                ProcessDictionary(odysseyMissionsData, OdysseyMissions);
            }

            if (bountiesData.Count > 0)
            {
                foreach (BountyData data in bountiesData)
                {
                    AddBounty(data);
                }
            }

            SaveData();
        }

        private void AddBounty(BountyData data)
        {
            Bounties.AddToCollection(data);

            if (Bounties.Count > 20)
            {
                Bounties.RemoveAtIndex(0);
            }
        }

        public string CommanderFID => appSettings.CurrentCommander?.FID;

        public void SetCurrentManager()
        {
            OnPropertyChanged("CurrentManager");
        }

        internal void MoveMission(MissionData missionData, bool moveToActive)
        {
            bool horizonsMission = false;

            if (horizonMissionsData is not null && horizonMissionsData.ContainsKey(missionData.MissionID))
            {
                horizonsMission = true;
            }
            else if (odysseyMissionsData is not null && odysseyMissionsData.ContainsKey(missionData.MissionID) == false)
            {
                return;
            }

            if (moveToActive)
            {
                if (CompletedMissions.Missions.Contains(missionData))
                {
                    CompletedMissions.RemoveMission(missionData);
                }

                if (horizonsMission && !HorizionMissions.Missions.Contains(missionData))
                {
                    HorizionMissions.AddMission(missionData);
                    return;
                }

                if (horizonsMission || OdysseyMissions.Missions.Contains(missionData))
                {
                    return;
                }

                OdysseyMissions.AddMission(missionData);
                return;
            }

            if (!CompletedMissions.Missions.Contains(missionData))
            {
                CompletedMissions.AddMission(missionData);
            }

            if (horizonsMission && HorizionMissions.Missions.Contains(missionData))
            {
                HorizionMissions.RemoveMission(missionData);
                return;
            }

            if (horizonsMission || !OdysseyMissions.Missions.Contains(missionData))
            {
                return;
            }

            OdysseyMissions.RemoveMission(missionData);
        }

        public void DeleteMission(MissionData missionData)
        {
            MissionsManager manager = GetMissionsManager(missionData);

            if (manager is null)
            {
                return;
            }

            manager.RemoveMission(missionData);

            Dictionary<long, MissionData> dictionary = GetMissionDictionary(missionData);

            if (dictionary is not null)
            {
                dictionary.Remove(missionData.MissionID);
            }

            SaveData();
        }

        private Dictionary<long, MissionData> GetMissionDictionary(MissionData missionData)
        {
            if (horizonMissionsData is not null && horizonMissionsData.ContainsKey(missionData.MissionID))
            {
                return horizonMissionsData;
            }
            else if (odysseyMissionsData is not null && odysseyMissionsData.ContainsKey(missionData.MissionID))
            {
                return odysseyMissionsData;
            }

            return null;
        }

        private MissionsManager GetMissionsManager(MissionData missionData)
        {
            if (horizonMissions.Missions.Contains(missionData))
            {
                return horizonMissions;
            }

            if (odysseyMissions.Missions.Contains(missionData))
            {
                return odysseyMissions;
            }

            if (completedMissions.Missions.Contains(missionData))
            {
                return completedMissions;
            }

            return null;
        }

        private static void AddMissionToGui(MissionsManager manager, MissionData missionData)
        {
            if (manager == null)
            {
                manager = new();
            }

            manager.AddMission(missionData);
        }

        private static void AddMissionToDictionary(ref Dictionary<long, MissionData> missionDictionary, MissionData missionData)
        {
            if (missionDictionary == null)
            {
                missionDictionary = new();
            }

            if (missionDictionary.ContainsKey(missionData.MissionID))
            {
                return;
            }

            missionDictionary.Add(missionData.MissionID, missionData);
        }

        private void ProcessDictionary(Dictionary<long, MissionData> missionData, MissionsManager missionManager)
        {
            List<MissionData> completedMissions = new(), missions = new();

            foreach (KeyValuePair<long, MissionData> mission in missionData)
            {
                mission.Value.SetContainer(this);

                if (mission.Value.CurrentState is MissionState.Complete or MissionState.Abandonded or MissionState.Failed)
                {

                    completedMissions.Add(mission.Value);
                    continue;
                }

                missions.Add(mission.Value);
            }

            CompletedMissions.AddMissions(completedMissions, true);
            missionManager.AddMissions(missions, false);
        }

        public void AddToMissionSourceClipBoard(MissionData data)
        {
            MissionSourceClipboardData clipboard = MissionSourceClipboards.FirstOrDefault(x => x.DestinationName == data.DestinationSystem &&
                                                                                            x.SystemName == data.SourceSystem &&
                                                                                            x.StationName == data.SourceStation);
            if (clipboard is null)
            {
                clipboard = new()
                {
                    DestinationName = data.DestinationSystem,
                    SystemName = data.SourceSystem,
                    StationName = data.SourceStation
                };

                clipboard.SetContainer(this);

                MissionSourceClipboards.AddToCollection(clipboard);

                _ = LoadSaveJson.SaveJson(MissionSourceClipboards, clipboarDataSaveFile);
            }
        }

        public void RemoveClipboardEntry(MissionSourceClipboardData data)
        {
            if (MissionSourceClipboards.Contains(data))
            {
                MissionSourceClipboards.Remove(data);

                _ = LoadSaveJson.SaveJson(MissionSourceClipboards, clipboarDataSaveFile);
            }
        }

        public void PurgeMissions(MissionState stateToPurge)
        {
            if (CompletedMissions is null || CompletedMissions.Missions.Count == 0)
            {
                return;
            }
            _ = Task.Run(() =>
              {
                  CompletedMissions.RemoveMissions(stateToPurge);

                  SaveData();
              });
        }

        public void LoadData()
        {
            if (appSettings == null || appSettings.CurrentCommander == null)
            {
                return;
            }

            CompletedMissions.Missions.ClearCollection();
            HorizionMissions.Missions.ClearCollection();
            OdysseyMissions.Missions.ClearCollection();
            Bounties.ClearCollection();

            horizonMissionsData = LoadSaveJson.LoadJson<Dictionary<long, MissionData>>(HorizonDataSaveFile);

            if (horizonMissionsData is not null)
            {
                ProcessDictionary(horizonMissionsData, HorizionMissions);
            }

            odysseyMissionsData = LoadSaveJson.LoadJson<Dictionary<long, MissionData>>(OdysseyDataSaveFile);

            if (odysseyMissionsData is not null)
            {
                ProcessDictionary(odysseyMissionsData, OdysseyMissions);
            }

            ObservableCollection<MissionSourceClipboardData> missionSourceClipboards = LoadSaveJson.LoadJson<ObservableCollection<MissionSourceClipboardData>>(clipboarDataSaveFile);

            if (missionSourceClipboards is not null)
            {
                foreach (MissionSourceClipboardData clipboard in missionSourceClipboards)
                {
                    if (MissionSourceClipboards.Contains(clipboard) == false)
                    {
                        MissionSourceClipboards.AddToCollection(clipboard);
                        clipboard.SetContainer(this);
                    }
                }
            }

            ObservableCollection<BountyData> bountiesData = LoadSaveJson.LoadJson<ObservableCollection<BountyData>>(BountyDataSaveFile);

            if (bountiesData is not null)
            {
                foreach (BountyData data in bountiesData)
                {
                    AddBounty(data);
                }
            }
        }

        public void SaveData()
        {
            _ = LoadSaveJson.SaveJson(horizonMissionsData, HorizonDataSaveFile);
            _ = LoadSaveJson.SaveJson(odysseyMissionsData, OdysseyDataSaveFile);
            _ = LoadSaveJson.SaveJson(bounties, BountyDataSaveFile);
        }
    }
}