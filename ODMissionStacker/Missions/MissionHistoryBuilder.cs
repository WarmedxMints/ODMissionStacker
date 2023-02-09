using EliteJournalReader;
using EliteJournalReader.Events;
using ODMissionStacker.CustomMessageBox;
using ODMissionStacker.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using static ODMissionStacker.Missions.MissionsContainer;

namespace ODMissionStacker.Missions
{
    public class MissionHistoryBuilder
    {
        private readonly JournalWatcher watcher;
        private readonly string commanderFid;
        private readonly string commanderName;

        //private bool odyssey;
        private GameVersion CurrentGameMode { get; set; }

        private readonly Dictionary<long, MissionData> legacyMissionsData = new();
        private readonly Dictionary<long, MissionData> liveMissionsData = new();
        private bool handleLogs = true;
        private Station currentStation;
        private readonly List<StackInfo> stackInformation = new();
        private readonly List<BountyData> bountiesData = new();

        public MissionHistoryBuilder(JournalWatcher watcher, string fid, string commanderName)
        {
            commanderFid = fid;
            this.commanderName = commanderName;
            this.watcher = watcher;
        }

        private void OnFileHeaderEvent(object sender, FileheaderEvent.FileheaderEventArgs e)
        {
            var gameversion = Version.Parse(e.gameversion);

            CurrentGameMode = gameversion.Major >= 4 ? GameVersion.Live : GameVersion.Legacy;
        }

        private void OnLoadGameEvent(object sender, LoadGameEvent.LoadGameEventArgs e)
        {
            if(CurrentGameMode == GameVersion.Legacy)
            {
                return;
            }
        }

        private void OnCommanderEvent(object sender, CommanderEvent.CommanderEventArgs e)
        {
            if (e.FID is null)
            {
                handleLogs = string.Equals(e.Name, commanderName, StringComparison.OrdinalIgnoreCase);
                return;
            }
            handleLogs = e.FID.Equals(commanderFid);
        }

        private void OnLocationEvent(object sender, LocationEvent.LocationEventArgs e)
        {
            if (handleLogs == false)
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
            if (handleLogs == false)
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

        private void OnUndockedFromStation(object sender, UndockedEvent.UndockedEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            currentStation = null;
        }

        private void OnMissionAccepted(object sender, MissionAcceptedEvent.MissionAcceptedEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            if (currentStation is null || e.KillCount is null || e.KillCount == 0 || string.IsNullOrEmpty(e.TargetFaction) || e.TargetType == null || !e.TargetType.Contains("MissionUtil_FactionTag_Pirate", StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }


            MissionData missionData = new(null, currentStation, e)
            {
                CurrentState = MissionState.Active
            };

            Dictionary<long, MissionData> missionDictionary = CurrentGameMode switch
            {
                GameVersion.Legacy => legacyMissionsData,
                _ => liveMissionsData,
            };

            AddMissionToDictionary(ref missionDictionary, missionData);

            //Stack Information
            StackInfo stackInfo = stackInformation.FirstOrDefault(x => x.IssuingFaction == missionData.IssuingFaction &&
                                                                       x.TargetFaction == missionData.TargetFaction);

            if (stackInfo == default)
            {
                stackInfo = new()
                {
                    IssuingFaction = missionData.IssuingFaction,
                    TargetFaction = missionData.TargetFaction
                };

                stackInformation.Add(stackInfo);
            }
        }

        private void OnMissionRedirected(object sender, MissionRedirectedEvent.MissionRedirectedEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            Dictionary<long, MissionData> dict = CurrentGameMode switch
            {
                GameVersion.Legacy => legacyMissionsData,
                _ => liveMissionsData,
            };

            if (dict is null || !dict.ContainsKey(e.MissionID))
            {
                return;
            }

            MissionData missionData = dict[e.MissionID];
            missionData.CurrentState = MissionState.Redirectied;
            missionData.Kills = missionData.KillCount;
        }

        private void OnMissionCompleted(object sender, MissionCompletedEvent.MissionCompletedEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            Dictionary<long, MissionData> dict = CurrentGameMode switch
            {
                GameVersion.Legacy => legacyMissionsData,
                _ => liveMissionsData,
            };

            if (dict is null || !dict.ContainsKey(e.MissionID))
            {
                return;
            }

            MissionData missionData = dict[e.MissionID];
            missionData.CurrentState = MissionState.Complete;
            missionData.Reward = e.Reward;
        }

        private void OnMissionAbandonded(object sender, MissionAbandonedEvent.MissionAbandonedEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            Dictionary<long, MissionData> dict = CurrentGameMode switch
            {
                GameVersion.Legacy => legacyMissionsData,
                _ => liveMissionsData,
            };

            if (dict is null || !dict.ContainsKey(e.MissionID))
            {
                return;
            }

            MissionData missionData = dict[e.MissionID];
            missionData.CurrentState = MissionState.Abandonded;
            missionData.Reward = 0;
        }

        private void OnMissionFailed(object sender, MissionFailedEvent.MissionFailedEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            Dictionary<long, MissionData> dict = CurrentGameMode switch
            {
                GameVersion.Legacy => legacyMissionsData,
                _ => liveMissionsData,
            };

            if (dict is null || !dict.ContainsKey(e.MissionID))
            {
                return;
            }

            MissionData missionData = dict[e.MissionID];
            missionData.CurrentState = MissionState.Failed;
            missionData.Reward = 0;
        }

        private void OnMissionsEvent(object sender, MissionsEvent.MissionsEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            var missions = new List<Mission>();

            missions.AddRange(e.Active);
            missions.AddRange(e.Complete);
            missions.AddRange(e.Failed);


            var missionsToRemove = new List<long>();

            Dictionary<long, MissionData> dict = CurrentGameMode switch
            {
                GameVersion.Legacy => legacyMissionsData,
                _ => liveMissionsData,
            };

            foreach (var mission in dict.Keys)
            {
                var mis = missions.FirstOrDefault(x => x.MissionID == mission);

                if (mis is null)
                {
                    dict[mission].CurrentState = MissionState.Complete;
                }
            }
        }

        private void OnBoutnyEvent(object sender, BountyEvent.BountyEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            //Ignore skimmers, ground and zero value bounties.
            if (e.VictimFaction.Contains("faction_none") || e.VictimFaction.Contains("faction_Pirate") || e.Target.Contains("suit") || e.TotalReward <= 0)
            {
                return;
            }

            Dictionary<long, MissionData> dict = CurrentGameMode switch
            {
                GameVersion.Legacy => legacyMissionsData,
                _ => liveMissionsData,
            };

            TimeSpan timeSinceLastKill = new();

            if(bountiesData.Any())
            {
                timeSinceLastKill = e.Timestamp - bountiesData[^1].TimeStamp;
            }

            bountiesData.Add(new BountyData(e,timeSinceLastKill));

            if (bountiesData.Count > 20)
            {
                bountiesData.RemoveAt(0);
            }

            for (int i = 0; i < stackInformation.Count; i++)
            {
                StackInfo faction = stackInformation[i];

                if(faction.TargetFaction != e.VictimFaction)
                {
                    continue;
                }

                MissionData mission = dict.FirstOrDefault(x => x.Value.IssuingFaction == faction.IssuingFaction &&
                                                           x.Value.TargetFaction == e.VictimFaction 
                                                            && x.Value.CurrentState == MissionState.Active).Value;

                if (mission == default)
                {
                    continue;
                }

                mission.Kills++;
            }
        }

        private static void AddMissionToDictionary(ref Dictionary<long, MissionData> missionDictionary, MissionData missionData)
        {
            missionDictionary ??= new();

            if (missionDictionary.ContainsKey(missionData.MissionID))
            {
                return;
            }

            missionDictionary.Add(missionData.MissionID, missionData);
        }

        public Tuple<Dictionary<long, MissionData>, Dictionary<long, MissionData>, List<BountyData>> GetHistory(IProgress<string> progress)
        {
            SubscribeToEvents();

            watcher.ParseHistory(progress);

            UnsubscribeFromEvents();

            progress.Report("Processing Log Events");

            return Tuple.Create(legacyMissionsData, liveMissionsData,  bountiesData);
        }

        private void SubscribeToEvents()
        {
            watcher.GetEvent<FileheaderEvent>()?.AddHandler(OnFileHeaderEvent);

            watcher.GetEvent<CommanderEvent>()?.AddHandler(OnCommanderEvent);

            watcher.GetEvent<LocationEvent>()?.AddHandler(OnLocationEvent);

            watcher.GetEvent<DockedEvent>()?.AddHandler(OnDockedAtStation);

            watcher.GetEvent<UndockedEvent>()?.AddHandler(OnUndockedFromStation);

            watcher.GetEvent<MissionAcceptedEvent>()?.AddHandler(OnMissionAccepted);

            watcher.GetEvent<MissionRedirectedEvent>()?.AddHandler(OnMissionRedirected);

            watcher.GetEvent<MissionCompletedEvent>()?.AddHandler(OnMissionCompleted);

            watcher.GetEvent<MissionAbandonedEvent>()?.AddHandler(OnMissionAbandonded);

            watcher.GetEvent<MissionFailedEvent>()?.AddHandler(OnMissionFailed);

            watcher.GetEvent<BountyEvent>()?.AddHandler(OnBoutnyEvent);

            watcher.GetEvent<MissionsEvent>()?.AddHandler(OnMissionsEvent);
        }

        private void UnsubscribeFromEvents()
        {
            watcher.GetEvent<FileheaderEvent>()?.RemoveHandler(OnFileHeaderEvent);

            watcher.GetEvent<CommanderEvent>()?.RemoveHandler(OnCommanderEvent);

            watcher.GetEvent<LocationEvent>()?.RemoveHandler(OnLocationEvent);

            watcher.GetEvent<DockedEvent>()?.RemoveHandler(OnDockedAtStation);

            watcher.GetEvent<UndockedEvent>()?.RemoveHandler(OnUndockedFromStation);

            watcher.GetEvent<MissionAcceptedEvent>()?.RemoveHandler(OnMissionAccepted);

            watcher.GetEvent<MissionRedirectedEvent>()?.RemoveHandler(OnMissionRedirected);

            watcher.GetEvent<MissionCompletedEvent>()?.RemoveHandler(OnMissionCompleted);

            watcher.GetEvent<MissionAbandonedEvent>()?.RemoveHandler(OnMissionAbandonded);

            watcher.GetEvent<MissionFailedEvent>()?.RemoveHandler(OnMissionFailed);

            watcher.GetEvent<BountyEvent>()?.RemoveHandler(OnBoutnyEvent);

            watcher.GetEvent<MissionsEvent>()?.RemoveHandler(OnMissionsEvent);
        }
    }
}
