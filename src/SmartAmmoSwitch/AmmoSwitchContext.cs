using System.Collections.Generic;

namespace SmartAmmoSwitch
{
    internal sealed class AmmoSwitchContext(
        int useAmmo,
        List<AmmoCandidate> candidates,
        List<int> orderedTypes,
        int signature,
        int currentType,
        List<AmmoLayoutEntry> currentLayout)
    {
        public int UseAmmo { get; } = useAmmo;
        public List<AmmoCandidate> Candidates { get; } = candidates;
        public List<int> OrderedTypes { get; } = orderedTypes;
        public int Signature { get; } = signature;
        public int CurrentType { get; } = currentType;
        public List<AmmoLayoutEntry> CurrentLayout { get; } = currentLayout;
    }
}