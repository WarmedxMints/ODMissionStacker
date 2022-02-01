using System;
using System.Globalization;
using EliteJournalReader.Events;

namespace ODMissionStacker.Missions
{
    public class BountyData
    {
        public BountyData() { }
        public BountyData(BountyEvent.BountyEventArgs args)
        {
            TimeStamp = args.Timestamp;

            args.Target = args.Target.Replace('_', ' ').ToLowerInvariant();

            TextInfo textInfo = new CultureInfo("en-GB", false).TextInfo;

            Target = textInfo.ToTitleCase(args.Target);
            VictimFaction = args.VictimFaction;
            TotalReward = args.TotalReward;
        }
        public DateTime TimeStamp { get; set; }
        public string Target { get; set; }
        public string VictimFaction { get; set; }
        public int TotalReward { get; set; }
    }
}
