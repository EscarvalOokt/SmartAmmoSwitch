using NUnit.Framework;

namespace SmartAmmoSwitch.Tests.UI
{
    [TestFixture]
    public sealed class FilterEditorClickAwayTests
    {
        [TestCase(true, false, true, false, false, true, TestName = "AvailableClickSignal_TriggersClickAway")]
        [TestCase(
            true,
            false,
            false,
            true,
            false,
            true,
            TestName = "ConsumedClickSignal_PhysicalPressEdgeTriggersClickAway")]
        [TestCase(true, false, false, true, true, false, TestName = "HeldMouseButton_DoesNotRepeatClickAway")]
        [TestCase(false, false, true, true, false, false, TestName = "UnfocusedSearch_DoesNotTriggerClickAway")]
        [TestCase(true, true, true, true, false, false, TestName = "HigherPriorityPanel_BlocksClickAway")]
        [TestCase(
            true,
            false,
            false,
            false,
            true,
            false,
            TestName = "ReleasedMouseWithoutClick_DoesNotTriggerClickAway")]
        public void IsEligiblePress_RespectsFocusAndMouseOwnership(
            bool hasTextFocus,
            bool inputBlocked,
            bool mouseLeftClick,
            bool mouseLeftDown,
            bool wasMouseLeftDown,
            bool expected)
        {
            bool actual = FilterEditorClickAwayPolicy.IsEligiblePress(
                hasTextFocus,
                inputBlocked,
                mouseLeftClick,
                mouseLeftDown,
                wasMouseLeftDown);

            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}