using System.Linq;
using NUnit.Framework;

namespace SmartAmmoSwitch.Tests.Ammo
{
    [TestFixture]
    public sealed class AmmoSlotPriorityTests
    {
        [Test]
        public void RegularAmmo_PrioritizesAmmoSlotsAndExcludesCoinSlots()
        {
            int[] slots = { 1, 55, 49, 0, 54, 57, 50, 53, 56 };

            int[] ordered = slots.Where(slot => AmmoSlotPriority.GetRank(slot, false) != int.MaxValue)
                .OrderBy(slot => AmmoSlotPriority.GetRank(slot, false))
                .ToArray();

            Assert.That(ordered, Is.EqualTo(new[] { 54, 55, 56, 57, 0, 1, 49 }));
        }

        [Test]
        public void CoinAmmo_PrioritizesCoinSlotsAndExcludesAmmoSlots()
        {
            int[] slots = { 1, 51, 49, 0, 50, 53, 54, 57, 52 };

            int[] ordered = slots.Where(slot => AmmoSlotPriority.GetRank(slot, true) != int.MaxValue)
                .OrderBy(slot => AmmoSlotPriority.GetRank(slot, true))
                .ToArray();

            Assert.That(ordered, Is.EqualTo(new[] { 50, 51, 52, 53, 0, 1, 49 }));
        }

        [TestCase(-1, false)]
        [TestCase(50, false)]
        [TestCase(53, false)]
        [TestCase(54, true)]
        [TestCase(57, true)]
        [TestCase(58, true)]
        [TestCase(58, false)]
        public void UnsupportedSlot_HasNoPriority(int slot, bool isCoinAmmo)
        {
            Assert.That(AmmoSlotPriority.GetRank(slot, isCoinAmmo), Is.EqualTo(int.MaxValue));
        }
    }
}