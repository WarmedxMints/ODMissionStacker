using ODMissionStacker.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ODMissionStacker.Missions
{
    public class TargetFactionInfo : PropertyChangeNotify
    {
        private string targetFaction;
        private long reward;
        private long shareableValue;
        private int killCount;
        private int remainingCount;
        private int killsToNextMissonCompletion;
        private double killRatio;
        private int totalKills;
        private int missionCount;
        private int activeMissionsCount;
        private long avergePerKill;
        private long avergePerMission;
        private long turnInValue;
        private long shareableTurnInValue;

        public string TargetFaction { get => targetFaction; set { targetFaction = value; OnPropertyChanged(); } }
        public long Reward { get => reward; set { reward = value; OnPropertyChanged(); } }
        public long ShareableValue { get => shareableValue; set { shareableValue = value; OnPropertyChanged(); } }
        public int KillCount { get => killCount; set { killCount = value; OnPropertyChanged(); } }
        public int RemainingCount { get => remainingCount; set { remainingCount = value; OnPropertyChanged(); } }
        public int KillsToNextMissonCompletion { get => killsToNextMissonCompletion; set { killsToNextMissonCompletion = value; OnPropertyChanged(); } }
        public double KillRatio { get => killRatio; set { killRatio = value; OnPropertyChanged(); } }
        public int TotalKills { get => totalKills; set { totalKills = value; OnPropertyChanged(); } }
        public int MissionCount { get => missionCount; set { missionCount = value; OnPropertyChanged(); } }
        public int ActiveMissionsCount { get => activeMissionsCount; set { activeMissionsCount = value; OnPropertyChanged(); } }
        public long AveragePerKill { get => avergePerKill; set { avergePerKill = value; OnPropertyChanged(); } }
        public long AveragePerMission { get => avergePerMission; set { avergePerMission = value; OnPropertyChanged(); } }
        public long TurnInValue { get => turnInValue; set { turnInValue = value; OnPropertyChanged(); } }
        public long ShareableTurnInValue { get => shareableTurnInValue; set { shareableTurnInValue = value; OnPropertyChanged(); } }

        public void UpdateValues(List<MissionData> missions)
        {
            if (missions is null || missions.Count == 0)
            {
                return;
            }

            Dictionary<string, int[]> remainingKills = new();
            List<int> missionRemainingKills = new();
            List<string> factions = new();
            remainingCount = KillCount;

            int activeMisCount = 0;
            long turnInValue = 0;
            long wingTurnInValue = 0;
            int totalKills = 0;

            foreach (MissionData data in missions)
            {
                if (string.Equals(data.TargetFaction, TargetFaction, StringComparison.OrdinalIgnoreCase) == false || data.CurrentState == MissionState.Abandonded)
                {
                    continue;
                }

                if (factions.Contains(data.IssuingFaction) == false)
                {
                    factions.Add(data.IssuingFaction);
                }

                if (remainingKills.ContainsKey(data.IssuingFaction) == false)
                {
                    remainingKills.Add(data.IssuingFaction, new int[] { 0, 0 });
                }

                remainingKills[data.IssuingFaction][0] += data.Kills;
                remainingKills[data.IssuingFaction][1] += data.KillCount;

                totalKills += data.KillCount;

                int killsToGo = data.KillCount - data.Kills;

                if (killsToGo > 0)
                {
                    missionRemainingKills.Add(killsToGo);
                }

                if (data.CurrentState == MissionState.Active)
                {
                    activeMisCount++;
                }

                if (data.CurrentState == MissionState.Redirectied)
                {
                    turnInValue += data.Reward;

                    if (data.Wing)
                    {
                        wingTurnInValue += data.Reward;
                    }
                }
            }

            ActiveMissionsCount = activeMisCount;
            RemainingCount = remainingKills.Max(x => x.Value[1] - x.Value[0]);
            TotalKills = totalKills;
            KillRatio = totalKills / (double)KillCount;

            List<int> killsRemaining = new();

            foreach (string faction in factions)
            {
                MissionData mission = missions.FirstOrDefault(x => string.Equals(x.IssuingFaction, faction, StringComparison.OrdinalIgnoreCase) &&
                                                                x.CurrentState == MissionState.Active);

                if (mission == default)
                {
                    continue;
                }
                killsRemaining.Add(mission.KillCount - mission.Kills);
            }

            KillsToNextMissonCompletion = killsRemaining.Count > 0 ? killsRemaining.Min() : 0;
            AveragePerKill = Reward / KillCount;
            AveragePerMission = Reward / MissionCount;
            TurnInValue = turnInValue;
            ShareableTurnInValue = wingTurnInValue;
        }
    }
}