using System.Collections.Generic;
using System.Globalization;

namespace ODMissionStacker.Utils
{
    public class FdevNameLookup
    {
        public static readonly Dictionary<string, string> ShipIdents = new()
        {
            { "adder", "Adder" },
            { "typex_3", "Alliance Challenger" },
            { "typex", "Alliance Chieftain" },
            { "typex_2", "Alliance Crusader" },
            { "anaconda", "Anaconda" },
            { "asp", "Asp Explorer" },
            { "asp_scout", "Asp Scout" },
            { "belugaliner", "Beluga Liner" },
            { "cobramkiii", "Cobra Mk III" },
            { "cobramkiv", "Cobra Mk IV" },
            { "diamondbackxl", "Diamondback Explorer" },
            { "diamondback", "Diamondback Scout" },
            { "dolphin", "Dolphin" },
            { "eagle", "Eagle" },
            { "federation_dropship_mkii", "Federal Assault Ship" },
            { "federation_corvette", "Federal Corvette" },
            { "federation_dropship", "Federal Dropship" },
            { "federation_gunship", "Federal Gunship" },
            { "ferdelance", "Fer-de-Lance" },
            { "hauler", "Hauler" },
            { "empire_trader", "Imperial Clipper" },
            { "empire_courier", "Imperial Courier" },
            { "cutter", "Imperial Cutter" },
            { "empire_eagle", "Imperial Eagle" },
            { "independant_trader", "Keelback" },
            { "krait_mkii", "Krait Mk II" },
            { "krait_light", "Krait Phantom" },
            { "mamba", "Mamba" },
            { "orca", "Orca" },
            { "python", "Python" },
            { "sidewinder", "Sidewinder" },
            { "type9_military", "Type-10 Defender" },
            { "type6", "Type-6 Transporter" },
            { "type7", "Type-7 Transporter" },
            { "type9", "Type-9 Heavy" },
            { "viper", "Viper Mk III" },
            { "viper_mkiv", "Viper Mk IV" },
            { "vulture", "Vulture" },
        };

        public static string GetShipName(string name)
        {
            if (ShipIdents.ContainsKey(name))
            {
                return ShipIdents[name];
            }

            TextInfo textInfo = new CultureInfo("en-GB", false).TextInfo;

            return textInfo.ToTitleCase(name.Replace('_', ' ').ToLowerInvariant());
        }
    }
}
