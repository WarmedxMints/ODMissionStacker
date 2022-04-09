using ODMissionStacker.Utils;
using System.Collections.Generic;

namespace ODMissionStacker.Missions
{
    public class StackInfo : PropertyChangeNotify
    {
        private string issuingFaction;
        private string targetFaction;
        private long reward;
        private int killCount;
        private int kills;
        private int left;
        private int difference;
        private int missionCount;

        public string IssuingFaction { get => issuingFaction; set { issuingFaction = value; OnPropertyChanged(); } }
        public string TargetFaction { get => targetFaction; set { targetFaction = value; OnPropertyChanged(); } }
        public long Reward { get => reward; set { reward = value; OnPropertyChanged(); } }
        public int KillCount { get => killCount; set { killCount = value; OnPropertyChanged(); } }
        public int Kills { get => kills; set { kills = value; OnPropertyChanged(); } }
        public int Left { get => left; set { left = value; OnPropertyChanged(); } }
        public int Difference { get => difference; set { difference = value; OnPropertyChanged(); } }
        public int MissionCount { get => missionCount; set { missionCount = value; OnPropertyChanged(); } }
        public List<MissionData> Missions { get; set; }
    }
}
