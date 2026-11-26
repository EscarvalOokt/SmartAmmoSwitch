using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace SmartAmmoSwitch.Tests.UI
{
    [TestFixture]
    public sealed class FilterEditorCategoryTests
    {
        private readonly SupportedAmmoInfo _arrow = new SupportedAmmoInfo(
            40,
            "Wooden Arrow",
            1,
            SupportedAmmoCategory.Arrows);

        private readonly SupportedAmmoInfo _bullet = new SupportedAmmoInfo(
            97,
            "Musket Ball",
            2,
            SupportedAmmoCategory.Bullets);

        [Test]
        public void ClearingArrowsTwice_PreservesSelectedBullets()
        {
            var categories = new FilterEditorCategories { Active = AmmoCatalogCategoryFilter.Arrows };
            var selected = new List<SupportedAmmoInfo> { _arrow, _bullet };

            categories.RefreshAvailable(selected);
            selected.RemoveAll(categories.Matches);
            categories.RefreshAvailable(selected);
            selected.RemoveAll(categories.Matches);

            Assert.That(categories.Active, Is.EqualTo(AmmoCatalogCategoryFilter.Arrows));
            Assert.That(categories.IsAvailable(AmmoCatalogCategoryFilter.Arrows), Is.True);
            Assert.That(selected, Is.EqualTo(new[] { _bullet }));
        }

        [Test]
        public void SearchMatchingOnlyBullets_StaysEmptyInArrowsUntilTheCategoryChanges()
        {
            var categories = new FilterEditorCategories { Active = AmmoCatalogCategoryFilter.Arrows };
            SupportedAmmoInfo[] searchResults = new[] { _bullet };
            categories.RefreshAvailable(searchResults);

            Assert.That(searchResults.Where(categories.Matches), Is.Empty);
            Assert.That(categories.IsAvailable(AmmoCatalogCategoryFilter.Arrows), Is.True);
            Assert.That(categories.IsAvailable(AmmoCatalogCategoryFilter.All), Is.True);
            Assert.That(categories.IsAvailable(AmmoCatalogCategoryFilter.Bullets), Is.True);
            Assert.That(categories.IsAvailable(AmmoCatalogCategoryFilter.Rockets), Is.False);

            categories.Active = AmmoCatalogCategoryFilter.All;
            Assert.That(searchResults.Where(categories.Matches), Is.EqualTo(searchResults));
        }
    }
}