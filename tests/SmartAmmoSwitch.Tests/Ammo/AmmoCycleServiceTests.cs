using System.Collections.Generic;
using NUnit.Framework;

namespace SmartAmmoSwitch.Tests.Ammo
{
    [TestFixture]
    public sealed class AmmoCycleServiceTests
    {
        [Test]
        public void AdditionalStackOfExistingType_PreservesCycleAfterSwitch()
        {
            var cycle = new AmmoCycleService();
            var types = new List<int> { 101, 102, 103 };
            cycle.Synchronize(1, 1, types, 101, 123, Layout((54, 101), (55, 102), (56, 103)));

            Assert.That(cycle.SelectNext(CycleDirection.Forward), Is.EqualTo(102));
            cycle.RememberLayout(Layout((54, 102), (55, 101), (56, 103)));

            cycle.Synchronize(
                1,
                1,
                new List<int> { 102, 101, 103 },
                102,
                123,
                Layout((54, 102), (55, 101), (56, 103), (57, 103)));

            Assert.That(cycle.OrderedTypes, Is.EqualTo(new[] { 101, 102, 103 }));
            Assert.That(cycle.SelectNext(CycleDirection.Forward), Is.EqualTo(103));
        }

        [Test]
        public void RemovingOneOfSeveralStacks_DoesNotResetCycle()
        {
            var cycle = new AmmoCycleService();
            cycle.Synchronize(
                1,
                1,
                new List<int> { 101, 102, 103 },
                101,
                123,
                Layout((54, 101), (55, 102), (56, 103), (57, 103)));
            Assert.That(cycle.SelectNext(CycleDirection.Forward), Is.EqualTo(102));
            cycle.RememberLayout(Layout((54, 102), (55, 101), (56, 103), (57, 103)));

            cycle.Synchronize(1, 1, new List<int> { 102, 101, 103 }, 102, 123, Layout((54, 102), (55, 101), (56, 103)));

            Assert.That(cycle.OrderedTypes, Is.EqualTo(new[] { 101, 102, 103 }));
            Assert.That(cycle.SelectNext(CycleDirection.Forward), Is.EqualTo(103));
        }

        [Test]
        public void ConsumedActiveStackWithReserve_ReanchorsIndexWithoutReset()
        {
            var cycle = new AmmoCycleService();
            cycle.Synchronize(
                1,
                1,
                new List<int> { 101, 102, 103 },
                101,
                123,
                Layout((54, 101), (55, 102), (56, 103), (57, 102)));

            Assert.That(cycle.SelectNext(CycleDirection.Forward), Is.EqualTo(102));
            cycle.RememberLayout(Layout((54, 102), (55, 101), (56, 103), (57, 102)));

            List<AmmoLayoutEntry> afterConsumption = Layout((55, 101), (56, 103), (57, 102));
            Assert.That(cycle.HasManualLayoutChange(afterConsumption), Is.False);

            cycle.Synchronize(1, 1, new List<int> { 101, 103, 102 }, 101, 123, afterConsumption);

            Assert.That(cycle.OrderedTypes, Is.EqualTo(new[] { 101, 102, 103 }));
            Assert.That(cycle.CurrentIndex, Is.EqualTo(0));
            Assert.That(cycle.SelectNext(CycleDirection.Forward), Is.EqualTo(102));
        }

        [Test]
        public void NewlyAvailableType_IsAppendedWithoutReorderingExistingCycle()
        {
            var cycle = new AmmoCycleService();
            cycle.Reset(1, 1, new List<int> { 101, 103, 102 }, 101, 123, Layout((54, 101), (55, 102), (56, 103)));

            cycle.Synchronize(
                1,
                1,
                new List<int> { 101, 102, 103, 104 },
                101,
                456,
                Layout((54, 101), (55, 102), (56, 103), (57, 104)));

            Assert.That(cycle.OrderedTypes, Is.EqualTo(new[] { 101, 103, 102, 104 }));
            Assert.That(cycle.SelectNext(CycleDirection.Forward), Is.EqualTo(103));
        }

        [TestCase(true, 47)]
        [TestCase(false, 3003)]
        public void DifferentAmmoTypesWithSameSignature_ReplaceUnavailableTypes(bool forward, int expectedNextType)
        {
            var cycle = new AmmoCycleService();
            // These two sets of arrow item IDs both produce this inventory signature.
            const int signature = 16947882;
            cycle.Synchronize(
                1,
                1,
                new List<int> { 40, 41, 516, 988 },
                40,
                signature,
                Layout((54, 40), (55, 41), (56, 516), (57, 988)));

            cycle.Synchronize(
                1,
                1,
                new List<int> { 40, 47, 265, 3003 },
                40,
                signature,
                Layout((54, 40), (55, 47), (56, 265), (57, 3003)));

            Assert.That(cycle.OrderedTypes, Is.EqualTo(new[] { 40, 47, 265, 3003 }));
            Assert.That(cycle.CurrentIndex, Is.EqualTo(0));
            CycleDirection direction = forward ? CycleDirection.Forward : CycleDirection.Backward;
            Assert.That(cycle.SelectNext(direction), Is.EqualTo(expectedNextType));
        }

        [Test]
        public void TypeReplacementWithSameSignature_PreservesExistingOrderAndAppendsNewTypes()
        {
            var cycle = new AmmoCycleService();
            cycle.Reset(
                1,
                1,
                new List<int> { 101, 103, 102, 105 },
                101,
                123,
                Layout((54, 101), (55, 102), (56, 103), (57, 105)));

            cycle.Synchronize(
                1,
                1,
                new List<int> { 101, 102, 103, 104 },
                101,
                123,
                Layout((54, 101), (55, 102), (56, 103), (57, 104)));

            Assert.That(cycle.OrderedTypes, Is.EqualTo(new[] { 101, 103, 102, 104 }));
            Assert.That(cycle.CurrentIndex, Is.EqualTo(0));
            Assert.That(cycle.SelectNext(CycleDirection.Forward), Is.EqualTo(103));
        }

        [Test]
        public void DisappearingType_IsRemovedWithoutResettingTheCycle()
        {
            var cycle = new AmmoCycleService();
            cycle.Reset(
                1,
                1,
                new List<int> { 101, 103, 102, 104 },
                101,
                123,
                Layout((54, 101), (55, 102), (56, 103), (57, 104)));

            cycle.Synchronize(1, 1, new List<int> { 101, 102, 104 }, 101, 456, Layout((54, 101), (55, 102), (57, 104)));

            Assert.That(cycle.OrderedTypes, Is.EqualTo(new[] { 101, 102, 104 }));
            Assert.That(cycle.SelectNext(CycleDirection.Forward), Is.EqualTo(102));
        }

        [Test]
        public void ManualSwap_ResetsCycleToPhysicallyActiveType()
        {
            var cycle = new AmmoCycleService();
            cycle.Reset(1, 1, new List<int> { 101, 102, 103 }, 101, 123, Layout((54, 101), (55, 102), (56, 103)));
            Assert.That(cycle.SelectNext(CycleDirection.Forward), Is.EqualTo(102));
            cycle.RememberLayout(Layout((54, 102), (55, 101), (56, 103)));

            cycle.Synchronize(1, 1, new List<int> { 101, 102, 103 }, 101, 123, Layout((54, 101), (55, 102), (56, 103)));

            Assert.That(cycle.CurrentIndex, Is.EqualTo(0));
            Assert.That(cycle.SelectNext(CycleDirection.Forward), Is.EqualTo(102));
        }

        [Test]
        public void MovingAnExistingStackToAnEmptySlot_IsManualLayoutChange()
        {
            var cycle = new AmmoCycleService();
            cycle.Reset(1, 1, new List<int> { 101, 102, 103 }, 101, 123, Layout((54, 101), (55, 102), (56, 103)));

            Assert.That(cycle.HasManualLayoutChange(Layout((55, 102), (56, 103), (57, 101))), Is.True);
        }

        [Test]
        public void NewTypeAndManualSwap_TakeTheManualResetPath()
        {
            var cycle = new AmmoCycleService();
            cycle.Reset(1, 1, new List<int> { 101, 103, 102 }, 101, 123, Layout((54, 101), (55, 102), (56, 103)));

            cycle.Synchronize(
                1,
                1,
                new List<int> { 102, 101, 103, 104 },
                102,
                456,
                Layout((54, 102), (55, 101), (56, 103), (57, 104)));

            Assert.That(cycle.OrderedTypes, Is.EqualTo(new[] { 102, 101, 103, 104 }));
            Assert.That(cycle.CurrentIndex, Is.EqualTo(0));
            Assert.That(cycle.SelectNext(CycleDirection.Forward), Is.EqualTo(101));
        }

        private static List<AmmoLayoutEntry> Layout(params (int Slot, int Type)[] entries)
        {
            var layout = new List<AmmoLayoutEntry>(entries.Length);
            foreach ((int Slot, int Type) entry in entries)
                layout.Add(new AmmoLayoutEntry(entry.Slot, entry.Type));

            return layout;
        }
    }
}