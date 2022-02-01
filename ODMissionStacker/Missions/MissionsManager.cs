using ODMissionStacker.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ODMissionStacker.Missions
{
    public class MissionsManager : PropertyChangeNotify
    {
        public MissionsManager()
        {
            Missions.CollectionChanged += Missions_CollectionChanged;
        }

        private void Missions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateValues();
        }

        private ObservableCollection<MissionData> missions = new();
        public ObservableCollection<MissionData> Missions { get => missions; set { missions = value; OnPropertyChanged(); } }

        private ObservableCollection<StackInfo> stackInformation = new();
        public ObservableCollection<StackInfo> StackInformation { get => stackInformation; set { stackInformation = value; OnPropertyChanged(); } }

        private ObservableCollection<TargetFactionInfo> targetFactionInfo = new();
        public ObservableCollection<TargetFactionInfo> TargetFactionInfo { get => targetFactionInfo; set { targetFactionInfo = value; OnPropertyChanged(); } }

        private long stackValue;
        public long StackValue { get => stackValue; set { stackValue = value; OnPropertyChanged(); } }

        public void UpdateValues()
        {
            StackValue = 0;
            StackInformation.ClearCollection();
            TargetFactionInfo.ClearCollection();

            if (Missions == null || Missions.Count == 0)
            {
                return;
            }

            foreach (MissionData data in Missions)
            {
                if (data.CurrentState == MissionState.Abandonded)
                {
                    continue;
                }
                //Stack Value
                StackValue += data.Reward;

                //Stack Information
                StackInfo stackInfo = StackInformation.FirstOrDefault(x => string.Equals(x.IssuingFaction, data.IssuingFaction, StringComparison.OrdinalIgnoreCase) &&
                                                                            string.Equals(x.TargetFaction, data.TargetFaction, StringComparison.OrdinalIgnoreCase));

                if (stackInfo == default)
                {
                    stackInfo = new()
                    {
                        IssuingFaction = data.IssuingFaction,
                        TargetFaction = data.TargetFaction
                    };

                    StackInformation.AddToCollection(stackInfo);
                }

                stackInfo.Reward += data.Reward;
                stackInfo.KillCount += data.KillCount;
                stackInfo.MissionCount++;

                TargetFactionInfo targetFactionInfo = TargetFactionInfo.FirstOrDefault(x => string.Equals(x.TargetFaction, data.TargetFaction, StringComparison.OrdinalIgnoreCase));

                if (targetFactionInfo == default)
                {
                    targetFactionInfo = new()
                    {
                        TargetFaction = data.TargetFaction,
                        KillCount = 1
                    };

                    TargetFactionInfo.AddToCollection(targetFactionInfo);
                }

                if (stackInfo.KillCount > targetFactionInfo.KillCount)
                {
                    targetFactionInfo.KillCount = stackInfo.KillCount;
                }

                targetFactionInfo.MissionCount++;
                targetFactionInfo.Reward += data.Reward;

                if (data.Wing)
                {
                    targetFactionInfo.ShareableValue += data.Reward;
                }
            }

            foreach (StackInfo stack in stackInformation)
            {
                int max = stackInformation.Where(x => x.TargetFaction == stack.TargetFaction).Max(x => x.KillCount);

                stack.Difference = max - stack.KillCount;
            }

            UpdateFactionInfo();
        }

        public void UpdateFactionInfo()
        {
            if (targetFactionInfo == null || targetFactionInfo.Count == 0)
            {
                return;
            }
            foreach (TargetFactionInfo info in TargetFactionInfo)
            {
                List<MissionData> data = Missions.Where(x => string.Equals(x.TargetFaction, info.TargetFaction, StringComparison.OrdinalIgnoreCase)).ToList();

                info.UpdateValues(data);
            }
        }

        public void OnBounty(BountyData data)
        {
            IOrderedEnumerable<MissionData> missions = new List<MissionData>(Missions).OrderBy(x => x.CollectionTime);

            foreach (StackInfo faction in stackInformation)
            {
                MissionData mission = missions.FirstOrDefault(x => string.Equals(x.IssuingFaction, faction.IssuingFaction, StringComparison.OrdinalIgnoreCase) &&
                                                            string.Equals(x.TargetFaction, data.VictimFaction, StringComparison.OrdinalIgnoreCase) &&
                                                            x.CurrentState == MissionState.Active);

                if (mission == default)
                {
                    continue;
                }

                mission.KillsWithoutStateChange++;

                UpdateFactionInfo();
            }
        }


        public void UpdateMissionsStates(Station currentStation)
        {
            if (currentStation == null)
            {
                foreach (MissionData mission in missions)
                {
                    if (mission.CurrentState is MissionState.Complete or MissionState.Abandonded)
                    {
                        continue;
                    }

                    //update the current states
                    mission.CurrentState = mission.Kills >= mission.KillCount ? MissionState.Redirectied : MissionState.Active;
                }
                return;
            }

            foreach (MissionData mission in missions)
            {
                if (mission.CurrentState is MissionState.Complete or MissionState.Abandonded)
                {
                    continue;
                }

                if (string.Equals(mission.SourceStation, currentStation.StationName, StringComparison.OrdinalIgnoreCase) && mission.CurrentState == MissionState.Redirectied)
                {
                    mission.CurrentState = MissionState.ReadyToTurnIn;
                }
            }
        }

        internal void UpdateKills(int v)
        {
            if (Missions is null || Missions.Count == 0)
            {
                return;
            }

            IOrderedEnumerable<MissionData> missions = v > 0
                ? new List<MissionData>(Missions).OrderBy(x => x.CollectionTime)
                : new List<MissionData>(Missions).OrderByDescending(x => x.CollectionTime);

            List<string> factions = new();


            foreach (StackInfo faction in stackInformation)
            {
                MissionData mission = v > 0
                    ? missions.FirstOrDefault(x => string.Equals(x.IssuingFaction, faction.IssuingFaction, StringComparison.OrdinalIgnoreCase) &&
                                                            factions.Contains(x.IssuingFaction) == false &&
                                                            x.Kills < x.KillCount)
                    : missions.FirstOrDefault(x => string.Equals(x.IssuingFaction, faction.IssuingFaction, StringComparison.OrdinalIgnoreCase) &&
                                                     factions.Contains(x.IssuingFaction) == false &&
                                                     x.Kills > 0);
                if (mission == default)
                {
                    continue;
                }

                factions.Add(faction.IssuingFaction);

                mission.Kills += v;

                UpdateFactionInfo();
            }
        }
    }
}
