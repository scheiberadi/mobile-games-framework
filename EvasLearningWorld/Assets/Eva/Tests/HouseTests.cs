using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class HouseTests
    {
        private static readonly string[] Everything = { "sofa", "rug", "table", "lamp", "plant", "bed", "bookshelf" };

        [Test]
        public void AnOwnedItemGoesIntoASlotOfItsKind()
        {
            var house = new HouseLayout();
            Assert.That(house.TryPlace("sofa", "living_seat", Everything), Is.True);
            Assert.That(house.SlotOf("sofa"), Is.EqualTo("living_seat"));
            Assert.That(house.ItemIn("living_seat"), Is.EqualTo("sofa"));
        }

        [Test]
        public void TheWrongKindFails()
        {
            var house = new HouseLayout();
            Assert.That(house.TryPlace("sofa", "living_floor", Everything), Is.False);
            Assert.That(house.SlotOf("sofa"), Is.Null);
        }

        [Test]
        public void AnUnownedItemFails()
        {
            var house = new HouseLayout();
            Assert.That(house.TryPlace("rug", "living_floor", new[] { "sofa" }), Is.False);
            Assert.That(house.ItemIn("living_floor"), Is.Null);
        }

        [Test]
        public void UnknownItemsAndSlotsFail()
        {
            var house = new HouseLayout();
            Assert.That(house.TryPlace("nope", "living_seat", new[] { "nope" }), Is.False);
            Assert.That(house.TryPlace("sofa", "nope", Everything), Is.False);
        }

        [Test]
        public void AnOccupiedSlotRejectsADifferentItem()
        {
            var house = new HouseLayout();
            Assert.That(house.TryPlace("lamp", "living_corner", Everything), Is.True);
            Assert.That(house.CanPlace("plant", "living_corner", Everything), Is.False);
            Assert.That(house.TryPlace("plant", "living_corner", Everything), Is.False);
            Assert.That(house.ItemIn("living_corner"), Is.EqualTo("lamp"));
        }

        [Test]
        public void LampAndPlantCanEachTakeOneOfTheTwoCornerSlots()
        {
            var house = new HouseLayout();
            Assert.That(house.TryPlace("lamp", "living_corner", Everything), Is.True);
            Assert.That(house.TryPlace("plant", "bedroom_corner", Everything), Is.True);
            Assert.That(house.ItemIn("living_corner"), Is.EqualTo("lamp"));
            Assert.That(house.ItemIn("bedroom_corner"), Is.EqualTo("plant"));
        }

        [Test]
        public void PlacingAnAlreadyPlacedItemMovesIt()
        {
            var house = new HouseLayout();
            house.TryPlace("lamp", "living_corner", Everything);
            Assert.That(house.TryPlace("lamp", "bedroom_corner", Everything), Is.True);
            Assert.That(house.SlotOf("lamp"), Is.EqualTo("bedroom_corner"));
            Assert.That(house.ItemIn("living_corner"), Is.Null);
            Assert.That(house.Placements.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveFreesTheSlotAndReportsWhetherItWasPlaced()
        {
            var house = new HouseLayout();
            house.TryPlace("rug", "living_floor", Everything);
            Assert.That(house.Remove("rug"), Is.True);
            Assert.That(house.ItemIn("living_floor"), Is.Null);
            Assert.That(house.Remove("rug"), Is.False);
        }

        [Test]
        public void PlacingTheSameItemInTheSameSlotAgainSucceeds()
        {
            var house = new HouseLayout();
            house.TryPlace("bed", "bedroom_bed", Everything);
            Assert.That(house.TryPlace("bed", "bedroom_bed", Everything), Is.True);
            Assert.That(house.Placements.Count, Is.EqualTo(1));
        }
    }
}
