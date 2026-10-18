namespace SmartAmmoSwitch
{
    internal static class AmmoSlotPriority
    {
        internal static int GetRank(int slotIndex, bool isCoinAmmo)
        {
            if (isCoinAmmo)
            {
                if (slotIndex is >= 50 and <= 53)
                    return slotIndex - 50;
            }
            else if (slotIndex is >= 54 and <= 57)
            {
                return slotIndex - 54;
            }

            return slotIndex is >= 0 and <= 49 ? slotIndex + 4 : int.MaxValue;
        }
    }
}