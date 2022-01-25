using EliteJournalReader;
using EliteJournalReader.Events;
using ODMissionStacker.Settings;
using ODMissionStacker.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace ODMissionStacker.Missions
{
    public class MissionsContainer : PropertyChangeNotify
    {
        #region Private Values
        private readonly string horizonDataSaveFile = Path.Combine(Directory.GetCurrentDirectory(), "Data", "HorizonsMissions.json");
        private readonly string odysseyDataSaveFile = Path.Combine(Directory.GetCurrentDirectory(), "Data", "OdysseyMissions.json");
        private readonly string clipboarDataSaveFile = Path.Combine(Directory.GetCurrentDirectory(), "Data", "MissionSourceClipboard.json");

        private readonly JournalWatcher journalWatcher;

        private bool odyssey;

        private readonly string journalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Saved Games",
            "Frontier Developments",
            "Elite Dangerous");

        private Station currentStation;

        private Dictionary<long, MissionData> horizonMissionsData, odysseyMissionsData;

        private readonly AppSettings appSettings;
        public MissionsManager CurrentManager => appSettings.ViewDisplayMode switch
        {
            DisplayMode.Horizons => HorizionMissions,
            DisplayMode.Odyssey => OdysseyMissions,
            DisplayMode.Completed => CompletedMissions,
            _ => HorizionMissions,
        };
        #endregion

        #region Public Properties
        private MissionsManager horizonMissions = new();
        private MissionsManager odysseyMissions = new();
        private MissionsManager completedMissions = new();
        private ObservableCollection<MissionSourceClipboardData> missionSourceClipboards = new();

        public MissionsManager HorizionMissions { get => horizonMissions; set { horizonMissions = value; OnPropertyChanged(); } }
        public MissionsManager OdysseyMissions { get => odysseyMissions; set { odysseyMissions = value; OnPropertyChanged(); } }
        public MissionsManager CompletedMissions { get => completedMissions; set { completedMissions = value; OnPropertyChanged(); } }
        public ObservableCollection<MissionSourceClipboardData> MissionSourceClipboards { get => missionSourceClipboards; set { missionSourceClipboards = value; OnPropertyChanged(); } }
        #endregion

        public MissionsContainer(AppSettings settings)
        {
            journalWatcher = new(journalPath);
            appSettings = settings;
        }

        public void Init()
        {
            journalWatcher.GetEvent<FileheaderEvent>()?.AddHandler(OnFileHeaderEvent);

            journalWatcher.GetEvent<LocationEvent>()?.AddHandler(OnLocationEvent);

            journalWatcher.GetEvent<DockedEvent>()?.AddHandler(OnDockedAtStation);

            journalWatcher.GetEvent<UndockedEvent>()?.AddHandler(OnUndockedFromStation);

            journalWatcher.GetEvent<MissionAcceptedEvent>()?.AddHandler(OnMissionAccepted);

            journalWatcher.GetEvent<MissionRedirectedEvent>()?.AddHandler(OnMissionRedirected);

            journalWatcher.GetEvent<MissionCompletedEvent>()?.AddHandler(OnMissionCompleted);

            journalWatcher.GetEvent<MissionAbandonedEvent>()?.AddHandler(OnMissionAbandonded);

            journalWatcher.GetEvent<BountyEvent>()?.AddHandler(OnBoutnyEvent);

            _ = journalWatcher.StartWatching();

            LoadData();

            SetCurrentManager();
        }

        public void SetCurrentManager()
        {
            OnPropertyChanged("CurrentManager");
        }

        private void OnFileHeaderEvent(object sender, FileheaderEvent.FileheaderEventArgs e)
        {
            odyssey = e.Odyssey;
        }

        private void OnLocationEvent(object sender, LocationEvent.LocationEventArgs e)
        {
            if(e.Docked == false)
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
            if (journalWatcher.IsLive == false || currentStation == null || e.KillCount is null || e.KillCount == 0)
            {
                return;
            }

            MissionData missionData = new(this, currentStation, e);
            missionData.CurrentState = MissionState.Active;

            AddMissionToDictionary(ref odyssey ? ref odysseyMissionsData : ref horizonMissionsData, missionData);
            AddMissionToGui(odyssey ? OdysseyMissions.Missions : HorizionMissions.Missions, missionData);

            SaveData();
        }

        private void OnMissionRedirected(object sender, MissionRedirectedEvent.MissionRedirectedEventArgs e)
        {
            if (journalWatcher.IsLive == false)
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
                OdysseyMissions.UpdateValues();
                SaveData();
                return;
            }

            horizonMissionsData[e.MissionID].CurrentState = MissionState.Redirectied;
            horizonMissionsData[e.MissionID].Kills = horizonMissionsData[e.MissionID].KillCount;
            HorizionMissions.UpdateValues();
            SaveData();
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
                    CompletedMissions.Missions.RemoveFromCollection(missionData);                
                }
                CompletedMissions.UpdateValues();
                if (horizonsMission && !HorizionMissions.Missions.Contains(missionData))
                {
                    HorizionMissions.Missions.AddToCollection(missionData);
                    HorizionMissions.UpdateValues();
                    return;
                }
                if(OdysseyMissions.Missions.Contains(missionData))
                {
                    return;
                }
                OdysseyMissions.Missions.AddToCollection(missionData);
                OdysseyMissions.UpdateValues();
            }

            if (!CompletedMissions.Missions.Contains(missionData))
            {
                CompletedMissions.Missions.AddToCollection(missionData);         
            }
            CompletedMissions.UpdateValues();
            if (horizonsMission && HorizionMissions.Missions.Contains(missionData))
            {
                HorizionMissions.Missions.RemoveFromCollection(missionData);
                HorizionMissions.UpdateValues();
                return;
            }

            if (!OdysseyMissions.Missions.Contains(missionData))
            {
                return;
            }

            OdysseyMissions.Missions.RemoveFromCollection(missionData);
            OdysseyMissions.UpdateValues();
        }

        public void DeleteMission(MissionData missionData)
        {
            MissionsManager manager = GetMissionsManager(missionData);

            if (manager is null)
            {
                return;
            }

            manager.Missions.RemoveFromCollection(missionData);
            manager.UpdateValues();

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

        private void OnMissionCompleted(object sender, MissionCompletedEvent.MissionCompletedEventArgs e)
        {
            if (journalWatcher.IsLive == false)
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

                OdysseyMissions.Missions.RemoveFromCollection(missionData);
            }
            else
            {
                missionData = horizonMissionsData[e.MissionID];

                HorizionMissions.Missions.RemoveFromCollection(missionData);
            }

            missionData.CurrentState = MissionState.Complete;
            missionData.Reward = e.Reward;

            CompletedMissions.Missions.AddToCollection(missionData);
            SaveData();
        }

        private void OnMissionAbandonded(object sender, MissionAbandonedEvent.MissionAbandonedEventArgs e)
        {
            if (journalWatcher.IsLive == false)
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

                OdysseyMissions.Missions.RemoveFromCollection(missionData);
            }
            else
            {
                missionData = horizonMissionsData[e.MissionID];

                HorizionMissions.Missions.RemoveFromCollection(missionData);
            }

            missionData.CurrentState = MissionState.Abandonded;
            missionData.Reward = 0;

            CompletedMissions.Missions.AddToCollection(missionData);
            SaveData();
        }

        private void OnBoutnyEvent(object sender, BountyEvent.BountyEventArgs e)
        {
            if (journalWatcher.IsLive == false)
            {
                return;
            }

            if (odyssey)
            {
                OdysseyMissions.OnBounty(e);
                return;
            }

            HorizionMissions.OnBounty(e);
        }

        private static void AddMissionToGui(ObservableCollection<MissionData> missions, MissionData missionData)
        {
            if (missions == null)
            {
                missions = new();
            }

            MissionData mission = missions.FirstOrDefault(x => x.MissionID == missionData.MissionID);

            if (mission == default)
            {
                //Add the mission
                missions.AddToCollection(missionData);
                //Ensure our list is sorted correctly
                missions.Sort((x, y) => DateTime.Compare(x.CollectionTime, y.CollectionTime));
            }
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

        public void LoadData()
        {
            horizonMissionsData = LoadSaveJson.LoadJson<Dictionary<long, MissionData>>(horizonDataSaveFile);

            if (horizonMissionsData is not null)
            {
                foreach (KeyValuePair<long, MissionData> mission in horizonMissionsData)
                {
                    mission.Value.SetContainer(this);

                    if (mission.Value.CurrentState is MissionState.Complete or MissionState.Abandonded)
                    {

                        CompletedMissions.Missions.AddToCollection(mission.Value);
                        continue;
                    }

                    AddMissionToGui(HorizionMissions.Missions, mission.Value);
                }
            }

            odysseyMissionsData = LoadSaveJson.LoadJson<Dictionary<long, MissionData>>(odysseyDataSaveFile);

            if (odysseyMissionsData is not null)
            {
                foreach (KeyValuePair<long, MissionData> mission in odysseyMissionsData)
                {
                    mission.Value.SetContainer(this);

                    if (mission.Value.CurrentState is MissionState.Complete or MissionState.Abandonded)
                    {
                        CompletedMissions.Missions.AddToCollection(mission.Value);
                        continue;
                    }

                    AddMissionToGui(OdysseyMissions.Missions, mission.Value);
                }
            }

            ObservableCollection<MissionSourceClipboardData> missionSourceClipboards = LoadSaveJson.LoadJson<ObservableCollection<MissionSourceClipboardData>>(clipboarDataSaveFile);

            if (missionSourceClipboards is not null)
            {
                foreach (MissionSourceClipboardData clipboard in missionSourceClipboards)
                {
                    MissionSourceClipboards.AddToCollection(clipboard);
                    clipboard.SetContainer(this);
                }
            }
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

        public void UpdateValues()
        {
            CurrentManager?.UpdateValues();
        }

        public void SaveData()
        {
            _ = LoadSaveJson.SaveJson(horizonMissionsData, horizonDataSaveFile);
            _ = LoadSaveJson.SaveJson(odysseyMissionsData, odysseyDataSaveFile);
        }
    }
}