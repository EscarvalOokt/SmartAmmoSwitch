using System.Collections.Generic;

namespace SmartAmmoSwitch
{
    internal sealed class AmmoCycleService
    {
        private readonly AmmoCycleState _state = new AmmoCycleState();

        public int CurrentIndex => _state.CurrentIndex;
        public int Total => _state.OrderedTypes.Count;
        public int InventorySignature => _state.InventorySignature;
        public IReadOnlyList<int> OrderedTypes => _state.OrderedTypes;

        public void Reset(
            int weaponType,
            int useAmmo,
            List<int> orderedTypes,
            int currentType,
            int signature,
            List<AmmoLayoutEntry> currentLayout)
        {
            _state.WeaponType = weaponType;
            _state.UseAmmoType = useAmmo;
            _state.OrderedTypes = new List<int>(orderedTypes);
            _state.InventorySignature = signature;
            _state.LastCandidateLayout = new List<AmmoLayoutEntry>(currentLayout);

            _state.CurrentIndex = _state.OrderedTypes.IndexOf(currentType);
            if (_state.CurrentIndex < 0)
                _state.CurrentIndex = 0;
        }

        public bool IsStateValid(int weaponType, int useAmmo)
        {
            return _state.WeaponType == weaponType && _state.UseAmmoType == useAmmo;
        }

        public bool HasManualLayoutChange(List<AmmoLayoutEntry> currentLayout)
        {
            if (_state.LastCandidateLayout == null)
                return true;

            Dictionary<int, HashSet<int>> previousSlots = GroupSlotsByType(_state.LastCandidateLayout);
            Dictionary<int, HashSet<int>> currentSlots = GroupSlotsByType(currentLayout);

            foreach (KeyValuePair<int, HashSet<int>> previous in previousSlots)
            {
                if (!currentSlots.TryGetValue(previous.Key, out HashSet<int> current))
                    continue;

                var lostSlot = false;
                foreach (int slot in previous.Value)
                {
                    if (!current.Contains(slot))
                    {
                        lostSlot = true;
                        break;
                    }
                }

                if (!lostSlot)
                    continue;

                foreach (int slot in current)
                {
                    if (!previous.Value.Contains(slot))
                        return true;
                }
            }

            return false;
        }

        private static Dictionary<int, HashSet<int>> GroupSlotsByType(List<AmmoLayoutEntry> layout)
        {
            var slotsByType = new Dictionary<int, HashSet<int>>();

            foreach (AmmoLayoutEntry entry in layout)
            {
                if (!slotsByType.TryGetValue(entry.ItemType, out HashSet<int> slots))
                {
                    slots = new HashSet<int>();
                    slotsByType.Add(entry.ItemType, slots);
                }

                slots.Add(entry.SlotIndex);
            }

            return slotsByType;
        }

        public void ApplyTypeChanges(
            List<int> currentTypesInOrder,
            int currentType,
            int signature,
            List<AmmoLayoutEntry> currentLayout)
        {
            var existing = new HashSet<int>(_state.OrderedTypes);
            var current = new HashSet<int>(currentTypesInOrder);

            _state.OrderedTypes.RemoveAll(type => !current.Contains(type));

            foreach (int type in currentTypesInOrder)
            {
                if (!existing.Contains(type))
                    _state.OrderedTypes.Add(type);
            }

            if (_state.OrderedTypes.Count == 0)
            {
                _state.CurrentIndex = -1;
            }
            else
            {
                _state.CurrentIndex = _state.OrderedTypes.IndexOf(currentType);
                if (_state.CurrentIndex < 0)
                    _state.CurrentIndex = 0;
            }

            _state.InventorySignature = signature;
            _state.LastCandidateLayout = new List<AmmoLayoutEntry>(currentLayout);
        }

        public void Synchronize(
            int weaponType,
            int useAmmo,
            List<int> orderedTypes,
            int currentType,
            int signature,
            List<AmmoLayoutEntry> currentLayout)
        {
            bool stateInvalid = !IsStateValid(weaponType, useAmmo);
            bool currentTypeMissing = currentType == -1 || !orderedTypes.Contains(currentType);
            bool inventoryChanged = signature != _state.InventorySignature;
            bool manualLayoutChanged = !stateInvalid && HasManualLayoutChange(currentLayout);

            if (stateInvalid || currentTypeMissing || manualLayoutChanged)
                Reset(weaponType, useAmmo, orderedTypes, currentType, signature, currentLayout);
            else if (inventoryChanged)
                ApplyTypeChanges(orderedTypes, currentType, signature, currentLayout);
            else
            {
                _state.CurrentIndex = _state.OrderedTypes.IndexOf(currentType);
                RememberLayout(currentLayout);
            }
        }

        public void RememberLayout(List<AmmoLayoutEntry> currentLayout)
        {
            _state.LastCandidateLayout = new List<AmmoLayoutEntry>(currentLayout);
        }

        public int SelectNext(CycleDirection direction)
        {
            int count = _state.OrderedTypes.Count;
            if (count == 0)
                return -1;

            if (_state.CurrentIndex < 0)
                _state.CurrentIndex = 0;

            _state.CurrentIndex = direction == CycleDirection.Forward
                ? (_state.CurrentIndex + 1) % count
                : (_state.CurrentIndex - 1 + count) % count;

            return _state.OrderedTypes[_state.CurrentIndex];
        }
    }
}