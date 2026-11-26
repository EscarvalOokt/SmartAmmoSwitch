using NUnit.Framework;

namespace SmartAmmoSwitch.Tests.UI
{
    [TestFixture]
    public sealed class FilterEditorClickAwayTests
    {
        [TestCase(true, true, false, false, true, TestName = "AvailableClickSignal_TriggersClickAway")]
        [TestCase(true, false, true, false, true, TestName = "ConsumedClickSignal_PhysicalPressEdgeTriggersClickAway")]
        [TestCase(true, false, true, true, false, TestName = "HeldMouseButton_DoesNotRepeatClickAway")]
        [TestCase(false, true, true, false, false, TestName = "UnfocusedSearch_DoesNotTriggerClickAway")]
        [TestCase(true, false, false, true, false, TestName = "ReleasedMouseWithoutClick_DoesNotTriggerClickAway")]
        public void UnblockedPress_UsesTheClickSignalOrPhysicalEdge(
            bool hasTextFocus,
            bool mouseLeftClick,
            bool mouseLeftDown,
            bool wasMouseLeftDown,
            bool expected)
        {
            bool actual = FilterEditorClickAwayPolicy.IsEligiblePress(
                hasTextFocus,
                inputBlocked: false,
                higherPanelAtPointer: false,
                mouseLeftClick,
                mouseLeftDown,
                wasMouseLeftDown);

            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(true, true, false, true, TestName = "PressOnHigherPanel_ReleasesFocus")]
        [TestCase(true, false, false, false, TestName = "HoverOverHigherPanel_PreservesFocus")]
        [TestCase(true, true, true, false, TestName = "HoldOverHigherPanel_PreservesFocus")]
        [TestCase(false, true, false, false, TestName = "UnrelatedInputBlock_PreservesFocus")]
        public void BlockedPress_OnlyReleasesFocusForANewPressOnAnotherPanel(
            bool higherPanelAtPointer,
            bool mouseLeftDown,
            bool wasMouseLeftDown,
            bool expected)
        {
            bool actual = FilterEditorClickAwayPolicy.IsEligiblePress(
                hasTextFocus: true,
                inputBlocked: true,
                higherPanelAtPointer,
                mouseLeftClick: false,
                mouseLeftDown,
                wasMouseLeftDown);

            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}