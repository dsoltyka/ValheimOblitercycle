using BepInEx.Configuration;
using Jotunn.Utils;

namespace Oblitercycle
{
    internal enum RecycleMode
    {
        /// <summary>Everything becomes coal, exactly as in vanilla.</summary>
        Vanilla,
        /// <summary>Crafted items give back the trophies in their recipe; the rest of the item becomes coal.</summary>
        Trophies,
        /// <summary>Crafted items give back every material in their recipe.</summary>
        Materials,
    }

    internal enum BaseItemFate
    {
        /// <summary>Items with no recipe (raw materials, cooked food, drops) burn to coal as in vanilla.</summary>
        Coal,
        /// <summary>Items with no recipe are left in the Obliterator untouched.</summary>
        Keep,
    }

    /// <summary>
    /// Server-authoritative configuration. Every entry is admin-only so Jötunn syncs the server's values to clients;
    /// the peer that owns an Obliterator does the recycling, so all peers must agree.
    /// </summary>
    internal sealed class Settings
    {
        public ConfigEntry<RecycleMode> Mode { get; }
        public ConfigEntry<BaseItemFate> BaseItems { get; }
        public ConfigEntry<bool> KeepLooseTrophies { get; }
        public ConfigEntry<bool> RecoverUpgrades { get; }
        public ConfigEntry<int> MaterialYieldPercent { get; }
        public ConfigEntry<bool> VanillaConversions { get; }

        public Settings(ConfigFile config)
        {
            const string section = "1 - Recycling";

            Mode = config.Bind(section, "Mode", RecycleMode.Trophies, Admin(
                "What pulling the lever does to crafted items (anything with a crafting recipe).\n" +
                "Vanilla: everything burns to coal.\n" +
                "Trophies: the trophies in the item's recipe are returned; the rest of the item burns to coal.\n" +
                "Materials: every material in the item's recipe is returned (see MaterialYieldPercent)."));

            BaseItems = config.Bind(section, "BaseItems", BaseItemFate.Coal, Admin(
                "What happens to items that have no crafting recipe (raw materials, cooked food, creature drops) in Trophies or Materials mode.\n" +
                "Coal: they burn to coal as in vanilla. Keep: they are left in the Obliterator."));

            KeepLooseTrophies = config.Bind(section, "KeepLooseTrophies", true, Admin(
                "Never burn trophies that were placed in the Obliterator directly. Applies in Trophies and Materials mode."));

            RecoverUpgrades = config.Bind(section, "RecoverUpgrades", true, Admin(
                "Also return the materials spent on upgrading an item above quality 1 (trophies included)."));

            MaterialYieldPercent = config.Bind(section, "MaterialYieldPercent", 100,
                new ConfigDescription("Materials mode only: the share of non-trophy materials returned, rounded down per material. Trophies are always returned in full.",
                    new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { IsAdminOnly = true }));

            VanillaConversions = config.Bind(section, "VanillaConversions", true, Admin(
                "Run the Obliterator's built-in conversions first (the game and other mods can define special item-to-item conversions)."));
        }

        private static ConfigDescription Admin(string description)
        {
            return new ConfigDescription(description, null, new ConfigurationManagerAttributes { IsAdminOnly = true });
        }
    }
}
