using NUnit.Framework;

namespace SmartAmmoSwitch.Tests.Ammo
{
    [TestFixture]
    internal sealed class AmmoFilterSettingsTests
    {
        [TestCase("3", AmmoFilterMode.Disabled)]
        [TestCase("-1", AmmoFilterMode.Disabled)]
        [TestCase("unknown", AmmoFilterMode.Disabled)]
        [TestCase(null, AmmoFilterMode.Disabled)]
        [TestCase("Whitelist", AmmoFilterMode.Whitelist)]
        [TestCase("blacklist", AmmoFilterMode.Blacklist)]
        [TestCase("1", AmmoFilterMode.Blacklist)]
        public void ParseMode_AcceptsOnlyDefinedValues(string raw, AmmoFilterMode expected)
        {
            Assert.That(AmmoFilterSettings.ParseMode(raw), Is.EqualTo(expected));
        }
    }
}