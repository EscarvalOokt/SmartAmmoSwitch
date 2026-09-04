using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;

namespace SmartAmmoSwitch
{
    internal static class SupportedAmmoCatalog
    {
        private static readonly Dictionary<int, SupportedAmmoInfo> _itemsById =
            new Dictionary<int, SupportedAmmoInfo>();

        private static bool _isBuilt;

        public static IReadOnlyCollection<SupportedAmmoInfo> Items
        {
            get
            {
                EnsureBuilt();
                return _itemsById.Values;
            }
        }

        public static void Build()
        {
            if (_isBuilt)
                return;

            _itemsById.Clear();

            for (var itemId = 1; itemId < ItemID.Count; itemId++)
            {
                try
                {
                    var item = new Item();
                    item.SetDefaults(itemId);

                    if (!AmmoUtils.IsValidAmmo(item))
                        continue;

                    string itemName = GetItemDisplayName(itemId, item);
                    SupportedAmmoCategory category = SupportedAmmoClassifier.Classify(item);

                    _itemsById[itemId] = new SupportedAmmoInfo(itemId, itemName, item.ammo, category);
                }
                catch (Exception ex)
                {
                    SmartAmmoSwitchMod.Context?.Logger?.Warn(
                        $"[Smart Ammo Switch] Failed to inspect ammo item {itemId}: {ex.Message}");
                }
            }

            _isBuilt = true;
        }

        public static void Invalidate()
        {
            _itemsById.Clear();
            _isBuilt = false;
        }

        public static bool TryGetItem(int itemId, out SupportedAmmoInfo info)
        {
            EnsureBuilt();
            return _itemsById.TryGetValue(itemId, out info);
        }

        public static List<SupportedAmmoInfo> GetSortedItems()
        {
            EnsureBuilt();

            return _itemsById.Values.OrderBy(x => x.Category).ThenBy(x => x.ItemName).ThenBy(x => x.ItemId).ToList();
        }

        private static void EnsureBuilt()
        {
            if (!_isBuilt)
                Build();
        }

        private static string GetItemDisplayName(int itemId, Item item)
        {
            string itemName = null;

            try
            {
                itemName = Lang.GetItemNameValue(itemId);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
                // Localization lookup is optional; Item.Name is used as a fallback below.
            }

            if (string.IsNullOrWhiteSpace(itemName) && item != null)
                itemName = item.Name;

            if (string.IsNullOrWhiteSpace(itemName))
                itemName = $"Item_{itemId}";

            return itemName;
        }
    }
}