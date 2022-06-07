using System;
using EliteJournalReader.Events;
using ODMissionStacker.Utils;

namespace ODMissionStacker.Missions
{
    public class BountyData
    {
        public BountyData() { }
        public BountyData(BountyEvent.BountyEventArgs args, TimeSpan timeSinceLastKill)
        {
            TimeStamp = args.Timestamp;
            Target = FdevNameLookup.GetShipName(args.Target);
            VictimFaction = args.VictimFaction;
            TotalReward = args.TotalReward;
            TimeSinceLastKill = timeSinceLastKill;
        }
        public DateTime TimeStamp { get; set; }
        public TimeSpan TimeSinceLastKill { get; set; }
        public string Target { get; set; }
        public string VictimFaction { get; set; }
        public int TotalReward { get; set; }
    }
}
