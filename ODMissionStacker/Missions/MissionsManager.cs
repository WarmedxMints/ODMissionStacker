using ODMissionStacker.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ODMissionStacker.Missions
{
    public class MissionsManager : PropertyChangeNotify
    {
        private ObservableCollection<MissionData> missions = new();
        public ObservableCollection<MissionData> Missions { get => missions; set { missions = value; OnPropertyChanged(); } }

        private ObservableCollection<StackInfo> stackInformation = new();
        public ObservableCollection<StackInfo> StackInformation { get => stackInformation; set { stackInformation = value; OnPropertyChanged(); } }

        private ObservableCollection<TargetFactionInfo> targetFactionInfo = new();
        public ObservableCollection<TargetFactionInfo> TargetFactionInfo { get => targetFactionInfo; set { targetFactionInfo = value; OnPropertyChanged(); } }

        public void AddMission(MissionData mission)
        {
            if (Missions.Contains(mission))
            {
                return;
            }

            Missions.AddToCollection(mission);

            //Add To Stack
            AddToStack(mission);

            AddToTargetFactionInfo(mission);
        }

        public void MissionRedirected(MissionData mission)
        {
            if (Missions.Contains(mission) == false)
            {
                return;
            }

            RemoveFromStack(mission);

            UpdateFactionInfo(mission.TargetFaction);
        }

        public void RemoveMission(MissionData mission)
        {
            if (Missions.Contains(mission) == false)
            {
                return;
            }

            Missions.RemoveFromCollection(mission);

            RemoveFromStack(mission);

            RemoveFromTargetFactionInfo(mission);
        }

        public void UpdateKillsFromUI(MissionData mission)
        {
            if (Missions.Contains(mission) == false)
            {
                return;
            }

            MissionState currentState = mission.CurrentState;

            mission.CurrentState = mission.Kills >= mission.KillCount ? MissionState.Redirectied : MissionState.Active;

            UpdateFactionInfo(mission.TargetFaction);

            if (currentState == mission.CurrentState)
            {
                return;
            }

            switch (mission.CurrentState)
            {
                case MissionState.Active:
                    AddToStack(mission);
                    break;
                case MissionState.Redirectied:
                    RemoveFromStack(mission);
                    break;
                default:
                    break;
            }
        }

        public void AddMissions(List<MissionData> missions, bool keepCurrentMissions)
        {
            if (keepCurrentMissions)
            {
                missions.AddRange(Missions);
            }

            List<StackInfo> stackInformation = new();
            List<TargetFactionInfo> targetFactionInfos = new();

            foreach (var mission in missions)
            {
                if (mission.CurrentState is MissionState.Abandonded or MissionState.Failed)
                {
                    continue;
                }

                if (mission.CurrentState != MissionState.Redirectied)
                {
                    StackInfo stackInfo = stackInformation.FirstOrDefault(x => string.Equals(x.IssuingFaction, mission.IssuingFaction, StringComparison.OrdinalIgnoreCase) &&
                                                                        string.Equals(x.TargetFaction, mission.TargetFaction, StringComparison.OrdinalIgnoreCase));

                    if (stackInfo == default)
                    {
                        stackInfo = new()
                        {
                            IssuingFaction = mission.IssuingFaction,
                            TargetFaction = mission.TargetFaction,
                            Missions = new()
                        };

                        stackInformation.Add(stackInfo);
                    }

                    if (stackInfo.Missions.Contains(mission) == false)
                    {
                        stackInfo.Missions.Add(mission);
                        stackInfo.Reward += mission.Reward;
                        stackInfo.KillCount += mission.KillCount;
                        stackInfo.MissionCount++;
                    }
                }

                TargetFactionInfo targetFactionInfo = targetFactionInfos.FirstOrDefault(x => string.Equals(x.TargetFaction, mission.TargetFaction, StringComparison.OrdinalIgnoreCase));

                if (targetFactionInfo == default)
                {
                    targetFactionInfo = new()
                    {
                        TargetFaction = mission.TargetFaction,
                        KillCount = 1,
                        Missions = new()
                    };

                    targetFactionInfos.Add(targetFactionInfo);
                }

                if (targetFactionInfo.Missions.Contains(mission) == false)
                {
                    targetFactionInfo.Missions.Add(mission);
                    targetFactionInfo.MissionCount++;
                    targetFactionInfo.Reward += mission.Reward;

                    if (mission.Wing)
                    {
                        targetFactionInfo.ShareableValue += mission.Reward;
                    }
                }
            }

            foreach (StackInfo stack in stackInformation)
            {
                stack.Missions.Sort((x, y) => DateTime.Compare(x.CollectionTime, y.CollectionTime));

                IEnumerable<StackInfo> stacksToCheck = stackInformation.Where(x => x.TargetFaction == stack.TargetFaction);

                if (stacksToCheck.Any())
                {
                    int max = stacksToCheck.Max(x => x.KillCount);
                    stack.Difference = max - stack.KillCount;
                    UpdateStackLeftKills(stack);
                }
            }

            foreach (var targetFaction in targetFactionInfos)
            {
                targetFaction.UpdateValues();
            }

            Missions = new(missions);
            StackInformation = new(stackInformation);
            TargetFactionInfo = new(targetFactionInfos);
        }

        public void RemoveMissions(MissionState stateToRemove)
        {
            if (Missions is null || Missions.Any() == false)
            {
                return;
            }

            List<MissionData> missions = new(Missions);
            List<StackInfo> stackInformation = new(Helpers.CloneList(StackInformation));
            List<TargetFactionInfo> targetFactionInfos = new(Helpers.CloneList(TargetFactionInfo));

            foreach (var mission in Missions)
            {
                if (mission.CurrentState != stateToRemove || Missions.Contains(mission) == false)
                {
                    continue;
                }

                missions.Remove(mission);

                StackInfo stackInfo = stackInformation.FirstOrDefault(x => string.Equals(x.IssuingFaction, mission.IssuingFaction, StringComparison.OrdinalIgnoreCase) &&
                                                            string.Equals(x.TargetFaction, mission.TargetFaction, StringComparison.OrdinalIgnoreCase));

                if (stackInfo != default && stackInfo.Missions.Contains(mission))
                {
                    stackInfo.Missions.Remove(mission);

                    if (stackInfo.Missions.Any() == false)
                    {
                        stackInformation.Remove(stackInfo);
                    }
                    else
                    {
                        stackInfo.Reward -= mission.Reward;
                        stackInfo.KillCount -= mission.KillCount;
                        stackInfo.MissionCount--;
                    }
                }

                TargetFactionInfo targetFactionInfo = targetFactionInfos.FirstOrDefault(x => string.Equals(x.TargetFaction, mission.TargetFaction, StringComparison.OrdinalIgnoreCase));

                if (targetFactionInfo != default && targetFactionInfo.Missions.Contains(mission))
                {
                    targetFactionInfo.Missions.Remove(mission);

                    if (targetFactionInfo.Missions.Any() == false)
                    {
                        targetFactionInfos.Remove(targetFactionInfo);
                    }
                    else
                    {
                        targetFactionInfo.MissionCount--;
                        targetFactionInfo.Reward -= mission.Reward;

                        if (mission.Wing)
                        {
                            targetFactionInfo.ShareableValue -= mission.Reward;
                        }
                    }
                }
            }

            foreach (StackInfo stack in stackInformation)
            {
                stack.Missions.Sort((x, y) => DateTime.Compare(x.CollectionTime, y.CollectionTime));

                IEnumerable<StackInfo> stacksToCheck = stackInformation.Where(x => x.TargetFaction == stack.TargetFaction);

                if (stacksToCheck.Any())
                {
                    int max = stacksToCheck.Max(x => x.KillCount);
                    stack.Difference = max - stack.KillCount;
                    UpdateStackLeftKills(stack);
                }
            }

            foreach (var targetFaction in targetFactionInfos)
            {
                int maxCount = 0;
                IEnumerable<StackInfo> stacksToCheck = stackInformation.Where(x => x.TargetFaction == targetFaction.TargetFaction);

                if (stacksToCheck.Any())
                {
                    maxCount = stacksToCheck.Max(x => x.KillCount);
                }

                targetFaction.KillCount = maxCount;
                targetFaction.UpdateValues();
            }

            Missions = new(missions);
            StackInformation = new(stackInformation);
            TargetFactionInfo = new(targetFactionInfos);
        }

        #region TargetFactionInfo
        private void AddToTargetFactionInfo(MissionData mission)
        {
            TargetFactionInfo targetFactionInfo = TargetFactionInfo.FirstOrDefault(x => string.Equals(x.TargetFaction, mission.TargetFaction, StringComparison.OrdinalIgnoreCase));

            if (targetFactionInfo == default)
            {
                targetFactionInfo = new()
                {
                    TargetFaction = mission.TargetFaction,
                    KillCount = 1,
                    Missions = new()
                };

                TargetFactionInfo.AddToCollection(targetFactionInfo);
            }

            if (targetFactionInfo.Missions.Contains(mission) == false)
            {
                targetFactionInfo.Missions.Add(mission);
                targetFactionInfo.MissionCount++;
                targetFactionInfo.Reward += mission.Reward;

                if (mission.Wing)
                {
                    targetFactionInfo.ShareableValue += mission.Reward;
                }
            }

            targetFactionInfo.UpdateValues();
        }

        private void RemoveFromTargetFactionInfo(MissionData mission)
        {
            TargetFactionInfo targetFactionInfo = TargetFactionInfo.FirstOrDefault(x => string.Equals(x.TargetFaction, mission.TargetFaction, StringComparison.OrdinalIgnoreCase));

            if (targetFactionInfo == default)
            {
                return;
            }

            if (targetFactionInfo.Missions.Contains(mission))
            {
                targetFactionInfo.Missions.Remove(mission);

                if (targetFactionInfo.Missions.Any() == false)
                {
                    TargetFactionInfo.RemoveFromCollection(targetFactionInfo);
                    return;
                }

                targetFactionInfo.MissionCount--;
                targetFactionInfo.Reward -= mission.Reward;

                if (mission.Wing)
                {
                    targetFactionInfo.ShareableValue -= mission.Reward;
                }
            }

            targetFactionInfo.UpdateValues();
        }

        private void UpdateFactionInfo(string targetFaction)
        {
            if (targetFactionInfo == null || targetFactionInfo.Any() == false)
            {
                return;
            }

            IEnumerable<TargetFactionInfo> factionInfos = TargetFactionInfo.Where(x => string.Equals(x.TargetFaction, targetFaction, StringComparison.OrdinalIgnoreCase));

            if (factionInfos.Any() == false)
            {
                return;
            }

            foreach (TargetFactionInfo info in factionInfos)
            {
                info.UpdateValues();
            }
        }
        #endregion

        #region StackInfo Management
        private void AddToStack(MissionData mission)
        {
            //Find Stack
            StackInfo stackInfo = StackInformation.FirstOrDefault(x => string.Equals(x.IssuingFaction, mission.IssuingFaction, StringComparison.OrdinalIgnoreCase) &&
                                                                        string.Equals(x.TargetFaction, mission.TargetFaction, StringComparison.OrdinalIgnoreCase));

            if (stackInfo == default)
            {
                stackInfo = new()
                {
                    IssuingFaction = mission.IssuingFaction,
                    TargetFaction = mission.TargetFaction,
                    Missions = new()
                };

                StackInformation.AddToCollection(stackInfo);
            }

            if (stackInfo.Missions.Contains(mission) == false)
            {
                stackInfo.Missions.Add(mission);
                stackInfo.Missions.Sort((x, y) => DateTime.Compare(x.CollectionTime, y.CollectionTime));
                stackInfo.Reward += mission.Reward;
                stackInfo.KillCount += mission.KillCount;
                stackInfo.MissionCount++;
            }

            UpdateStackDiffence(mission);
        }

        public void RemoveFromStack(MissionData mission)
        {
            //Find Stack
            StackInfo stackInfo = StackInformation.FirstOrDefault(x => string.Equals(x.IssuingFaction, mission.IssuingFaction, StringComparison.OrdinalIgnoreCase) &&
                                                                        string.Equals(x.TargetFaction, mission.TargetFaction, StringComparison.OrdinalIgnoreCase));

            if (stackInfo == default)
            {
                return;
            }

            if (stackInfo.Missions.Contains(mission))
            {
                stackInfo.Missions.Remove(mission);

                if (stackInfo.Missions.Any() == false)
                {
                    StackInformation.RemoveFromCollection(stackInfo);
                    UpdateStackDiffence(mission);
                    return;
                }

                stackInfo.Reward -= mission.Reward;
                stackInfo.KillCount -= mission.KillCount;
                stackInfo.MissionCount--;
            }
            UpdateStackDiffence(mission);
            return;
        }

        private void UpdateStackDiffence(MissionData mission)
        {
            if (mission == null || stackInformation is null || stackInformation.Any() == false)
            {
                return;
            }

            IEnumerable<StackInfo> stacksToCheck = stackInformation.Where(x => x.TargetFaction == mission.TargetFaction);

            if (!stacksToCheck.Any())
            {
                return;
            }

            int max = stacksToCheck.Max(x => x.KillCount);

            foreach (StackInfo stack in stacksToCheck)
            {
                stack.Difference = max - stack.KillCount;
                UpdateStackLeftKills(stack);
            }
        }
        #endregion

        public void OnBounty(BountyData data)
        {
            foreach (StackInfo stack in stackInformation)
            {
                if (string.Equals(stack.TargetFaction, data.VictimFaction, StringComparison.OrdinalIgnoreCase) == false)
                {
                    continue;
                }

                MissionData mission = stack.Missions.FirstOrDefault(x => x.CurrentState == MissionState.Active);

                if (mission == default)
                {
                    continue;
                }

                mission.Kills++;
                UpdateStackLeftKills(stack);
            }

            UpdateFactionInfo(data.VictimFaction);
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

                    mission.ReadyToTurnIn = false;
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
                    mission.ReadyToTurnIn = true;
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

            List<string> factions = new(), targets = new();

            foreach (MissionData mission in missions)
            {
                if(v > 0 ? mission.Kills >= mission.KillCount : mission.Kills <= 0)
                {
                    continue;
                }

                if(targets.Contains($"{mission.IssuingFaction} {mission.TargetFaction}"))
                {
                    continue;
                }

                factions.Add(mission.IssuingFaction);
                targets.Add($"{mission.IssuingFaction} {mission.TargetFaction}");

                mission.Kills += v;

                UpdateKillsFromUI(mission);
            }

            foreach (var faction in factions)
            {
                UpdateFactionInfo(faction);
            }
        }

        private void UpdateStackLeftKills(StackInfo stack)
        {
            stack.Left = stack.KillCount - stack.Missions.Sum(x => x.Kills);
            stack.Kills = stack.KillCount - stack.Left;
        }
    }
}
