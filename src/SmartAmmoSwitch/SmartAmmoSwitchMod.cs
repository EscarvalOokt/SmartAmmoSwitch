using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using TerrariaModder.Core;
using TerrariaModder.Core.Logging;

namespace SmartAmmoSwitch
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class SmartAmmoSwitchMod : IMod, IModLifecycle
    {
        public const string ModId = "smart-ammo-switch";
        public const string ModName = "Smart Ammo Switch";
        public const string ModVersion = "1.2.0";

        private const string NextAmmoKey = "X";
        private const string PrevAmmoKey = "Z";
        private const string OpenFilterUiKey = "ALT + C";

        private readonly AmmoCycleService _cycle = new AmmoCycleService();
        private readonly AmmoFilterPipeline _filterPipeline = new AmmoFilterPipeline();
        private readonly AmmoScanner _scanner = new AmmoScanner();

        private SmartAmmoSwitchConfig _config;
        private FilterUiController _filterUi;
        private ILogger _log;

        private AmmoSearchMode _mode = AmmoSearchMode.AmmoSlotsOnly;
        private bool _showMessages = true;

        internal static ModContext Context { get; private set; }

        public string Id => ModId;
        public string Name => ModName;
        public string Version => ModVersion;

        public void Initialize(ModContext context)
        {
            Context = context;

            _log = context.Logger;
            _config = context.GetConfig<SmartAmmoSwitchConfig>();

            if (_config == null)
            {
                throw new InvalidOperationException("SmartAmmoSwitchConfig was not registered by TerrariaModder.");
            }

            _filterPipeline.Add(new ItemTypeAmmoFilter());

            LoadConfig();

            _filterUi = new FilterUiController();
            _filterUi.Initialize(context);

            context.RegisterKeybind(
                "ammo-next",
                "Next Ammo",
                "Next ammo type",
                NextAmmoKey,
                () => Switch(CycleDirection.Forward));

            context.RegisterKeybind(
                "ammo-prev",
                "Prev Ammo",
                "Previous ammo type",
                PrevAmmoKey,
                () => Switch(CycleDirection.Backward));

            context.RegisterKeybind(
                "open-filter-ui",
                "Open Filter UI",
                "Opens ammo filter editor",
                OpenFilterUiKey,
                OnOpenFilterUiKeyPressed);
        }

        public void Unload()
        {
            try
            {
                _filterUi?.Unload();
            }
            finally
            {
                _filterUi = null;

                SupportedAmmoCatalog.Invalidate();
                AmmoFilterSettings.Update(AmmoFilterMode.Disabled, null);

                _config = null;
                Context = null;
            }
        }

        public void OnContentReady(ModContext context)
        {
        }

        public void OnWorldLoad()
        {
            SupportedAmmoCatalog.Invalidate();
            _filterUi?.RefreshFromConfig();
        }

        public void OnWorldUnload()
        {
        }

        public void OnConfigChanged()
        {
            LoadConfig();
            _filterUi?.RefreshFromConfig();
        }

        private void LoadConfig()
        {
            _showMessages = _config.ShowMessages;
            _mode = GetSearchModeConfig();

            string filterModeRaw = _config.FilterMode ?? "Disabled";
            string filteredItemIdsRaw = _config.FilteredAmmoItemIds ?? string.Empty;

            AmmoFilterMode filterMode = ParseAmmoFilterMode(filterModeRaw);
            List<int> parsedIds = AmmoFilterSettings.ParseItemIds(filteredItemIdsRaw);

            AmmoFilterSettings.Update(filterMode, parsedIds);
        }

        private static AmmoFilterMode ParseAmmoFilterMode(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return AmmoFilterMode.Disabled;

            if (Enum.TryParse(raw, ignoreCase: true, out AmmoFilterMode result))
                return result;

            return AmmoFilterMode.Disabled;
        }

        private void OnOpenFilterUiKeyPressed()
        {
            _filterUi?.Toggle();
        }

        private void Switch(CycleDirection direction)
        {
            try
            {
                Player player = Main.player[Main.myPlayer];
                Item weapon = player.HeldItem;

                if (!TryGetSwitchContext(player, weapon, out AmmoSwitchContext switchContext))
                    return;

                SyncCycleState(weapon, switchContext);

                int nextType = _cycle.SelectNext(direction);
                if (nextType == -1)
                {
                    ShowMessage("No compatible ammo", 255, 80, 80);
                    return;
                }

                if (!TryPerformSwap(player, weapon, switchContext.UseAmmo, nextType, out Item activeItem))
                    return;

                ShowMessage($"[{_cycle.CurrentIndex + 1}/{_cycle.Total}] {activeItem.Name}", 150, 200, 255);
            }
            catch (Exception ex)
            {
                _log.Error("Ammo switch error: " + ex);
            }
        }

        private bool TryGetSwitchContext(Player player, Item weapon, out AmmoSwitchContext switchContext)
        {
            switchContext = null;

            if (weapon == null || weapon.IsAir || weapon.useAmmo <= 0)
            {
                ShowMessage("Weapon has no ammo", 255, 80, 80);
                return false;
            }

            int useAmmo = weapon.useAmmo;

            List<AmmoCandidate> allCandidates = GetAmmoCandidates(player, useAmmo);
            if (allCandidates.Count == 0)
            {
                ShowMessage("No compatible ammo", 255, 80, 80);
                return false;
            }

            List<AmmoCandidate> candidates = GetFilteredAmmoCandidates(player, weapon, useAmmo);
            if (candidates.Count == 0)
            {
                ShowMessage("All ammo blocked by filter", 255, 180, 80);
                return false;
            }

            List<int> orderedTypes = BuildOrderedTypes(candidates, useAmmo);

            if (orderedTypes.Count == 1)
            {
                ShowMessage("Only one ammo type", 200, 200, 200);
                return false;
            }

            int signature = AmmoUtils.BuildInventorySignature(candidates);
            int currentType = GetActiveAmmoType(player, useAmmo);
            List<AmmoLayoutEntry> currentLayout = AmmoUtils.BuildCandidateLayout(candidates);

            switchContext = new AmmoSwitchContext(
                useAmmo,
                candidates,
                orderedTypes,
                signature,
                currentType,
                currentLayout);

            return true;
        }

        private void SyncCycleState(Item weapon, AmmoSwitchContext context)
        {
            _cycle.Synchronize(
                weapon.type,
                context.UseAmmo,
                context.OrderedTypes,
                context.CurrentType,
                context.Signature,
                context.CurrentLayout);
        }

        private bool TryPerformSwap(Player player, Item weapon, int useAmmo, int nextType, out Item activeItem)
        {
            activeItem = null;

            List<AmmoCandidate> candidates = GetFilteredAmmoCandidates(player, weapon, useAmmo);

            int activeSlot = GetPreferredAmmoSlot(player, useAmmo);
            int targetSlot = PickSlotForType(candidates, nextType, useAmmo);

            if (activeSlot == -1 || targetSlot == -1)
            {
                ShowMessage("No compatible ammo", 255, 80, 80);
                return false;
            }

            if (activeSlot != targetSlot)
                Swap(player.inventory, activeSlot, targetSlot);

            int newActiveSlot = GetPreferredAmmoSlot(player, useAmmo);
            if (newActiveSlot == -1 || player.inventory[newActiveSlot].type != nextType)
            {
                if (activeSlot != targetSlot)
                    Swap(player.inventory, activeSlot, targetSlot);

                ShowMessage("Unable to switch ammo", 255, 80, 80);
                return false;
            }

            List<AmmoCandidate> updatedCandidates = GetFilteredAmmoCandidates(player, weapon, useAmmo);
            _cycle.RememberLayout(AmmoUtils.BuildCandidateLayout(updatedCandidates));

            activeItem = player.inventory[newActiveSlot];
            return true;
        }

        private List<AmmoCandidate> GetAmmoCandidates(Player player, int useAmmo)
        {
            bool isCoinAmmo = useAmmo == AmmoID.Coin;
            return _scanner.Scan(player, _mode)
                .Where(c => c.AmmoType == useAmmo && AmmoSlotPriority.GetRank(c.SlotIndex, isCoinAmmo) != int.MaxValue)
                .ToList();
        }

        private List<AmmoCandidate> GetFilteredAmmoCandidates(Player player, Item weapon, int useAmmo)
        {
            bool isCoinAmmo = useAmmo == AmmoID.Coin;
            return _scanner.Scan(player, _mode)
                .Where(c => c.AmmoType == useAmmo && AmmoSlotPriority.GetRank(c.SlotIndex, isCoinAmmo) != int.MaxValue)
                .Where(c => _filterPipeline.Allows(c.Item, weapon))
                .ToList();
        }

        private static List<int> BuildOrderedTypes(List<AmmoCandidate> candidates, int useAmmo)
        {
            bool isCoinAmmo = useAmmo == AmmoID.Coin;
            return candidates.OrderBy(c => AmmoSlotPriority.GetRank(c.SlotIndex, isCoinAmmo))
                .GroupBy(c => c.ItemType)
                .Select(g => g.Key)
                .ToList();
        }

        private static int GetPreferredAmmoSlot(Player player, int useAmmo)
        {
            bool isCoinAmmo = useAmmo == AmmoID.Coin;
            int bestRank = int.MaxValue;
            int bestSlot = -1;

            for (var slot = 0; slot < player.inventory.Length; slot++)
            {
                int rank = AmmoSlotPriority.GetRank(slot, isCoinAmmo);
                if (rank >= bestRank)
                    continue;

                Item item = player.inventory[slot];
                if (!AmmoUtils.IsValidAmmo(item) || item.ammo != useAmmo)
                    continue;

                bestRank = rank;
                bestSlot = slot;
            }

            return bestSlot;
        }

        private int GetActiveAmmoType(Player player, int useAmmo)
        {
            int slot = GetPreferredAmmoSlot(player, useAmmo);
            if (slot == -1)
                return -1;

            return player.inventory[slot].type;
        }

        private AmmoSearchMode GetSearchModeConfig()
        {
            string raw = _config.SearchMode ?? "1";

            return raw == "1" ? AmmoSearchMode.WholeInventory : AmmoSearchMode.AmmoSlotsOnly;
        }

        private static int PickSlotForType(List<AmmoCandidate> candidates, int type, int useAmmo)
        {
            bool isCoinAmmo = useAmmo == AmmoID.Coin;
            return candidates.Where(c => c.ItemType == type)
                       .OrderBy(c => AmmoSlotPriority.GetRank(c.SlotIndex, isCoinAmmo))
                       .FirstOrDefault()
                       ?.SlotIndex ??
                   -1;
        }

        private static void Swap(Item[] inv, int a, int b)
        {
            (inv[a], inv[b]) = (inv[b], inv[a]);
        }

        private void ShowMessage(string text, byte r, byte g, byte b)
        {
            if (_showMessages)
                Main.NewText(text, r, g, b);
        }
    }
}