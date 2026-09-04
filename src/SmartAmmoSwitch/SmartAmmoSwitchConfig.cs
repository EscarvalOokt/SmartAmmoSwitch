using TerrariaModder.Core.Config;

namespace SmartAmmoSwitch
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class SmartAmmoSwitchConfig : ModConfig
    {
        public override int Version => 1;

        [Client]
        [Label("Show Messages")]
        [Description("Display chat messages when ammo is switched.")]
        [FormerlySerializedAs("show_messages")]
        public bool ShowMessages { get; set; } = true;

        [Client]
        [Label("Search Mode")]
        [Description("1 = search whole inventory with priority on ammo slots. 2 = search only ammo slots.")]
        [Options("1", "2")]
        [FormerlySerializedAs("search_mode")]
        public string SearchMode { get; set; } = "1";

        [Client]
        [Label("Ammo Filter Mode")]
        [Description(
            "Disabled = all ammo is allowed. Blacklist = selected ammo is blocked. Whitelist = only selected ammo is allowed.")]
        [Options("Disabled", "Blacklist", "Whitelist")]
        [FormerlySerializedAs("ammoFilterMode")]
        public string FilterMode { get; set; } = "Disabled";

        [Client]
        [Label("Filtered Ammo Item IDs")]
        [Description("Comma-separated list of ammo item IDs used by blacklist or whitelist mode.")]
        [FormerlySerializedAs("filteredAmmoItemIds")]
        public string FilteredAmmoItemIds { get; set; } = string.Empty;
    }
}