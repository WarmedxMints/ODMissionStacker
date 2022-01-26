using EliteJournalReader;
using EliteJournalReader.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ODMissionStacker.Missions
{
    public class MissionHistoryBuilder
    {
        private readonly JournalWatcher watcher;
        private readonly string commanderFid;

        private bool odyssey;
        private Dictionary<long, MissionData> horizonMissionsData = new(), odysseyMissionsData = new();
        private bool handleLogs;
        private Station currentStation;
        private List<StackInfo> stackInformation = new();

        public MissionHistoryBuilder(JournalWatcher watcher, string fid)
        {
            commanderFid = fid;

            this.watcher = watcher;
        }

        private void OnFileHeaderEvent(object sender, FileheaderEvent.FileheaderEventArgs e)
        {
            odyssey = e.Odyssey;
        }

        private void OnCommanderEvent(object sender, CommanderEvent.CommanderEventArgs e)
        {
            handleLogs = e.FID == commanderFid;
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

            if (currentStation == null || e.KillCount is null || e.KillCount == 0 || string.IsNullOrEmpty(e.TargetFaction))
            {
                return;
            }

            MissionData missionData = new(null, currentStation, e);
            missionData.CurrentState = MissionState.Active;

            AddMissionToDictionary(ref odyssey ? ref odysseyMissionsData : ref horizonMissionsData, missionData);

            //Stack Information
            StackInfo stackInfo = stackInformation.FirstOrDefault(x => string.Equals(x.IssuingFaction, missionData.IssuingFaction, StringComparison.OrdinalIgnoreCase) &&
                                                                        string.Equals(x.TargetFaction, missionData.TargetFaction, StringComparison.OrdinalIgnoreCase));

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

            if (odyssey ? odysseyMissionsData is null || !odysseyMissionsData.ContainsKey(e.MissionID) : horizonMissionsData is null || !horizonMissionsData.ContainsKey(e.MissionID))
            {
                return;
            }

            if (odyssey)
            {
                odysseyMissionsData[e.MissionID].CurrentState = MissionState.Redirectied;
                odysseyMissionsData[e.MissionID].Kills = odysseyMissionsData[e.MissionID].KillCount;
                return;
            }

            horizonMissionsData[e.MissionID].CurrentState = MissionState.Redirectied;
            horizonMissionsData[e.MissionID].Kills = horizonMissionsData[e.MissionID].KillCount;
        }

        private void OnMissionCompleted(object sender, MissionCompletedEvent.MissionCompletedEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            if (odyssey ? odysseyMissionsData is null || !odysseyMissionsData.ContainsKey(e.MissionID) : horizonMissionsData is null || !horizonMissionsData.ContainsKey(e.MissionID))
            {
                return;
            }

            MissionData missionData = odyssey ? odysseyMissionsData[e.MissionID] : horizonMissionsData[e.MissionID];
            missionData.CurrentState = MissionState.Complete;
            missionData.Reward = e.Reward;

        }

        private void OnMissionAbandonded(object sender, MissionAbandonedEvent.MissionAbandonedEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            if (odyssey ? odysseyMissionsData is null || !odysseyMissionsData.ContainsKey(e.MissionID) : horizonMissionsData is null || !horizonMissionsData.ContainsKey(e.MissionID))
            {
                return;
            }

            MissionData missionData = odyssey ? odysseyMissionsData[e.MissionID] : horizonMissionsData[e.MissionID];
            missionData.CurrentState = MissionState.Abandonded;
            missionData.Reward = 0;
        }

        private void OnBoutnyEvent(object sender, BountyEvent.BountyEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            Dictionary<long, MissionData> dict = odyssey ? odysseyMissionsData : horizonMissionsData;

            foreach (StackInfo faction in stackInformation)
            {
                MissionData mission = dict.FirstOrDefault(x => string.Equals(x.Value.IssuingFaction, faction.IssuingFaction, StringComparison.OrdinalIgnoreCase) &&
                                                            string.Equals(x.Value.TargetFaction, e.VictimFaction, StringComparison.OrdinalIgnoreCase) &&
                                                            x.Value.Kills < x.Value.KillCount &&
                                                            x.Value.CurrentState == MissionState.Active).Value;
                
                if (mission == default)
                {
                    continue;
                }

                mission.Kills++;
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

        public Tuple<Dictionary<long, MissionData>, Dictionary<long, MissionData>> GetHistory(IProgress<string> progress)
        {
            SubscribeToEvents();

            watcher.ParseHistory(progress);

            UnsubscribeFromEvents();

            return Tuple.Create(horizonMissionsData, odysseyMissionsData);
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

            watcher.GetEvent<BountyEvent>()?.AddHandler(OnBoutnyEvent);
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

            watcher.GetEvent<BountyEvent>()?.RemoveHandler(OnBoutnyEvent);
        }
    }
}
