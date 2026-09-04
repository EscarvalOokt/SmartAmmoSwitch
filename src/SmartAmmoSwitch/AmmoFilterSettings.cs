using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace SmartAmmoSwitch
{
    internal static class AmmoFilterSettings
    {
        private static readonly HashSet<int> _filteredItemIds = new HashSet<int>();

        public static AmmoFilterMode Mode { get; private set; } = AmmoFilterMode.Disabled;
        public static IReadOnlyCollection<int> FilteredItemIds => _filteredItemIds;

        public static void Update(AmmoFilterMode mode, IEnumerable<int> itemIds)
        {
            Mode = mode;
            _filteredItemIds.Clear();

            if (itemIds == null)
                return;

            foreach (int itemId in itemIds)
            {
                if (itemId <= 0)
                    continue;

                if (!IsPotentiallySupportedAmmoItemId(itemId))
                    continue;

                _filteredItemIds.Add(itemId);
            }
        }

        public static bool Contains(int itemId)
        {
            return _filteredItemIds.Contains(itemId);
        }

        public static bool Add(int itemId)
        {
            if (itemId <= 0)
                return false;

            if (!IsPotentiallySupportedAmmoItemId(itemId))
                return false;

            return _filteredItemIds.Add(itemId);
        }

        public static bool Remove(int itemId)
        {
            return _filteredItemIds.Remove(itemId);
        }

        public static bool Toggle(int itemId)
        {
            if (_filteredItemIds.Contains(itemId))
            {
                _filteredItemIds.Remove(itemId);
                return false;
            }

            if (itemId > 0 && IsPotentiallySupportedAmmoItemId(itemId))
            {
                _filteredItemIds.Add(itemId);
                return true;
            }

            return false;
        }

        public static void Clear()
        {
            _filteredItemIds.Clear();
        }

        public static List<int> GetSortedIds()
        {
            return _filteredItemIds.OrderBy(x => x).ToList();
        }

        public static string SerializeToConfigString()
        {
            return string.Join(",", GetSortedIds());
        }

        public static List<int> ParseItemIds(string raw)
        {
            var result = new List<int>();

            if (string.IsNullOrWhiteSpace(raw))
                return result;

            char[] separators = [',', ';', '\n', '\r', '\t', ' '];
            string[] parts = raw.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                if (int.TryParse(part, out int itemId) && itemId > 0)
                    result.Add(itemId);
            }

            return result;
        }

        public static bool IsItemAllowed(int itemId)
        {
            if (Mode == AmmoFilterMode.Disabled)
                return true;

            bool contains = _filteredItemIds.Contains(itemId);

            if (Mode == AmmoFilterMode.Blacklist)
                return !contains;

            return contains;
        }

        private static bool IsPotentiallySupportedAmmoItemId(int itemId)
        {
            try
            {
                var item = new Item();
                item.SetDefaults(itemId);

                return AmmoUtils.IsValidAmmo(item);
            }
            catch
            {
                return false;
            }
        }
    }
}