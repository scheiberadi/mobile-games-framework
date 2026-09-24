using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class FurnitureTests
    {
        [Test]
        public void ItemIdsAreUnique()
        {
            var ids = FurnitureCatalog.All.Select(i => i.Id).ToList();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
        }

        [Test]
        public void EveryItemKindHasAtLeastOneSlot()
        {
            foreach (var item in FurnitureCatalog.All)
                Assert.That(HouseSlots.All.Any(s => s.Kind == item.Kind), Is.True, item.Id + " has no slot of kind " + item.Kind);
        }

        [Test]
        public void TheStarterSofaIsFree()
        {
            Assert.That(FurnitureCatalog.StarterId, Is.EqualTo("sofa"));
            Assert.That(FurnitureCatalog.Find("sofa").Price, Is.EqualTo(0));
        }

        [Test]
        public void CheapestPriceIgnoresTheFreeStarter()
        {
            Assert.That(FurnitureCatalog.CheapestPrice, Is.EqualTo(5));
        }

        [Test]
        public void OneSessionAlwaysPaysForTheCheapestItem()
        {
            Assert.That(CoinPayout.MinSessionPayout, Is.GreaterThanOrEqualTo(FurnitureCatalog.CheapestPrice));
        }

        [Test]
        public void FindReturnsNullForUnknownIds()
        {
            Assert.That(FurnitureCatalog.Find("nope"), Is.Null);
        }

        [Test]
        public void CatalogMatchesTheDesignTable()
        {
            Assert.That(FurnitureCatalog.Find("rug").Price, Is.EqualTo(5));
            Assert.That(FurnitureCatalog.Find("table").Kind, Is.EqualTo(SlotKind.Table));
            Assert.That(FurnitureCatalog.Find("bed").Price, Is.EqualTo(10));
            Assert.That(FurnitureCatalog.Find("bookshelf").Kind, Is.EqualTo(SlotKind.Wall));
            Assert.That(HouseSlots.All.Count, Is.EqualTo(31));
            Assert.That(HouseSlots.Find("kids_wall").Room, Is.EqualTo("kids"));
            Assert.That(HouseSlots.Find("nope"), Is.Null);
        }
    }
}
