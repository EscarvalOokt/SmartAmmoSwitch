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
            if (_state.LastCandidateLayout == null || _state.LastCandidateLayout.Count != currentLayout.Count)
                return true;

            for (var i = 0; i < currentLayout.Count; i++)
            {
                AmmoLayoutEntry prev = _state.LastCandidateLayout[i];
                AmmoLayoutEntry curr = currentLayout[i];

                if (prev.SlotIndex != curr.SlotIndex || prev.ItemType != curr.ItemType)
                    return true;
            }

            return false;
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