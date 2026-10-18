using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public const string ModVersion = "1.2.1";

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
        private MethodInfo _pickAmmoItemMethod;
        private bool _showMessages = true;

        internal static ModContext Context { get; private set; }

        public string Id => ModId;
        public string Name => ModName;
        public string Version => ModVersion;

        public void Initialize(ModContext context)
        {
            Context = context;

            _log = context.Logger;
            InitializeAmmoSelector();
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
                _pickAmmoItemMethod = null;
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

        private void InitializeAmmoSelector()
        {
            try
            {
                _pickAmmoItemMethod = typeof(Player).GetMethod(
                    "PickAmmo_PickAmmoItem",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    binder: null,
                    types: [typeof(Item)],
                    modifiers: null);

                if (_pickAmmoItemMethod == null || _pickAmmoItemMethod.ReturnType != typeof(Item))
                {
                    _pickAmmoItemMethod = null;
                    _log.Error("Terraria ammo selector is unavailable; ammo switching is disabled.");
                }
            }
            catch (Exception ex)
            {
                _pickAmmoItemMethod = null;
                _log.Error("Failed to resolve Terraria ammo selector: " + ex);
            }
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
            int currentType = GetActiveAmmoType(player, weapon);
            if (currentType == -1)
            {
                ShowMessage("Unable to determine active ammo", 255, 80, 80);
                return false;
            }

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

            int activeSlot = GetPreferredAmmoSlot(player, weapon);
            int targetSlot = PickSlotForType(candidates, nextType, useAmmo);

            if (activeSlot == -1 || targetSlot == -1)
            {
                ShowMessage("No compatible ammo", 255, 80, 80);
                return false;
            }

            if (activeSlot != targetSlot)
                Swap(player.inventory, activeSlot, targetSlot);

            int newActiveSlot = GetPreferredAmmoSlot(player, weapon);
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

        private int GetPreferredAmmoSlot(Player player, Item weapon)
        {
            if (_pickAmmoItemMethod == null)
                return -1;

            try
            {
                // This selector reads the current cycling offset without consuming ammo or advancing it.
                var activeItem = (Item)_pickAmmoItemMethod.Invoke(player, [weapon]);
                if (!AmmoUtils.IsValidAmmo(activeItem) || activeItem.ammo != weapon.useAmmo)
                    return -1;

                for (var slot = 0; slot < player.inventory.Length; slot++)
                {
                    if (ReferenceEquals(player.inventory[slot], activeItem))
                        return slot;
                }

                _log?.Warn("Terraria ammo selector returned an item outside the player inventory.");
            }
            catch (Exception ex)
            {
                // Return failure so the caller can roll back a swap if this is the verification lookup.
                _log?.Error("Failed to determine active ammo: " + ex);
            }

            return -1;
        }

        private int GetActiveAmmoType(Player player, Item weapon)
        {
            int slot = GetPreferredAmmoSlot(player, weapon);
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