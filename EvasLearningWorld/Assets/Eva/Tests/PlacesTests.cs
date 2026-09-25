using System;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class PlacesTests
    {
        private static readonly WorldBox FirstView = new WorldBox(0f, 0f, Places.ViewWidth, Places.ViewHeight);

        [Test]
        public void CatalogueHasHouseSchoolStoreInThatFixedOrderWithEverythingFilledIn()
        {
            Assert.That(Places.All.Count, Is.EqualTo(3));
            Assert.That(Places.All[0].Id, Is.EqualTo(PlaceId.House));
            Assert.That(Places.All[1].Id, Is.EqualTo(PlaceId.School));
            Assert.That(Places.All[2].Id, Is.EqualTo(PlaceId.Store));
            foreach (var place in Places.All)
            {
                Assert.That(Places.Find(place.Id), Is.SameAs(place));
                Assert.IsNotEmpty(place.ScreenKey, place.Id + " screen");
                Assert.IsNotEmpty(place.BuildingSprite, place.Id + " building");
                Assert.IsNotEmpty(place.VoiceKey, place.Id + " voice");
                Assert.That(place.Road.Count, Is.GreaterThanOrEqualTo(1), place.Id + " road");
                Assert.That(place.Road[0].X, Is.EqualTo(Places.Junction.X), place.Id + " road starts at the junction");
                Assert.That(place.Road[0].Y, Is.EqualTo(Places.Junction.Y), place.Id + " road starts at the junction");
            }
        }

        [Test]
        public void HouseHasNoRoadPictureAndTheOthersHaveOneContainingEveryWaypoint()
        {
            Assert.IsNull(Places.Find(PlaceId.House).RoadSprite);
            Assert.IsNull(Places.Find(PlaceId.House).RoadBox);
            foreach (var id in new[] { PlaceId.School, PlaceId.Store })
            {
                var place = Places.Find(id);
                Assert.IsNotEmpty(place.RoadSprite, id + " road sprite");
                Assert.That(place.Road.Count, Is.GreaterThanOrEqualTo(2), id + " road waypoints");
                Assert.IsTrue(place.RoadBox.HasValue, id + " road box");
                foreach (var point in place.Road) Assert.IsTrue(place.RoadBox.Value.Contains(point), id + " waypoint outside its road box");
                var end = place.Road[place.Road.Count - 1];
                Assert.That(end.X, Is.EqualTo(place.StandingSpot.X), id + " road ends at the standing spot");
                Assert.That(end.Y, Is.EqualTo(place.StandingSpot.Y), id + " road ends at the standing spot");
            }
        }

        // The initial composition (spec "Initial composition"): a design decision, so drift must fail loudly.
        [Test]
        public void InitialCompositionMatchesTheSpecNumbers()
        {
            AssertBox(Places.Find(PlaceId.House).TapBox, 60f, -30f, 320f, 280f);
            AssertBox(Places.Find(PlaceId.School).TapBox, -400f, 40f, 280f, 240f);
            AssertBox(Places.Find(PlaceId.Store).TapBox, 470f, -250f, 280f, 240f);
            AssertPoint(Places.Find(PlaceId.House).StandingSpot, -120f, -300f);
            AssertPoint(Places.Find(PlaceId.School).StandingSpot, -400f, -210f);
            AssertPoint(Places.Find(PlaceId.Store).StandingSpot, 200f, -320f);
            AssertPoint(Places.Junction, 60f, -190f);
            AssertPoint(Places.InitialView, 0f, 0f);
        }

        [Test]
        public void BuildingsAreTappableSizedInsideTheFirstViewWithMarginAndNeverOverlap()
        {
            foreach (var place in Places.All)
            {
                var box = place.TapBox;
                Assert.That(box.Width, Is.GreaterThanOrEqualTo(240f), place.Id + " width");
                Assert.That(box.Height, Is.GreaterThanOrEqualTo(240f), place.Id + " height");
                Assert.That(box.XMin, Is.GreaterThanOrEqualTo(FirstView.XMin + 60f), place.Id + " left margin");
                Assert.That(box.XMax, Is.LessThanOrEqualTo(FirstView.XMax - 60f), place.Id + " right margin");
                Assert.That(box.YMin, Is.GreaterThanOrEqualTo(FirstView.YMin + 60f), place.Id + " bottom margin");
                Assert.That(box.YMax, Is.LessThanOrEqualTo(FirstView.YMax - 60f), place.Id + " top margin");
                Assert.IsFalse(box.Overlaps(Places.SettingsZone), place.Id + " must clear the settings zone");
                Assert.IsFalse(box.Overlaps(Places.CoinZone), place.Id + " must clear the coin zone");
            }
            for (var i = 0; i < Places.All.Count; i++)
            for (var j = i + 1; j < Places.All.Count; j++)
            {
                var a = Places.All[i].TapBox;
                var b = Places.All[j].TapBox;
                Assert.IsFalse(a.Overlaps(b), Places.All[i].Id + " overlaps " + Places.All[j].Id);
                var gapX = Math.Max(b.XMin - a.XMax, a.XMin - b.XMax);
                var gapY = Math.Max(b.YMin - a.YMax, a.YMin - b.YMax);
                Assert.That(Math.Max(gapX, gapY), Is.GreaterThanOrEqualTo(100f), Places.All[i].Id + " and " + Places.All[j].Id + " are too close");
            }
        }

        [Test]
        public void CharacterAreasAreTappableSizedInsideTheFirstViewAndNeverCoverABuildingTapBox()
        {
            foreach (var place in Places.All)
            {
                var area = place.StandingArea;
                Assert.That(area.Width, Is.GreaterThanOrEqualTo(240f));
                Assert.That(area.Height, Is.GreaterThanOrEqualTo(240f));
                Assert.That(area.XMin, Is.GreaterThanOrEqualTo(FirstView.XMin), place.Id + " area left");
                Assert.That(area.XMax, Is.LessThanOrEqualTo(FirstView.XMax), place.Id + " area right");
                Assert.That(area.YMin, Is.GreaterThanOrEqualTo(FirstView.YMin), place.Id + " area bottom");
                Assert.That(area.YMax, Is.LessThanOrEqualTo(FirstView.YMax), place.Id + " area top");
                foreach (var building in Places.All)
                    Assert.IsFalse(area.Overlaps(building.TapBox), place.Id + " characters cover the " + building.Id + " tap box");
                Assert.IsFalse(area.Overlaps(Places.SettingsZone), place.Id + " area clears the settings zone");
                Assert.IsFalse(area.Overlaps(Places.CoinZone), place.Id + " area clears the coin zone");
            }
        }

        [Test]
        public void ParseOrHouseFallsBackToTheHouseForAnythingUnknown()
        {
            Assert.That(Places.ParseOrHouse("Store"), Is.EqualTo(PlaceId.Store));
            Assert.That(Places.ParseOrHouse("School"), Is.EqualTo(PlaceId.School));
            Assert.That(Places.ParseOrHouse(null), Is.EqualTo(PlaceId.House));
            Assert.That(Places.ParseOrHouse(""), Is.EqualTo(PlaceId.House));
            Assert.That(Places.ParseOrHouse("Volcano"), Is.EqualTo(PlaceId.House));
            Assert.That(Places.ParseOrHouse("99"), Is.EqualTo(PlaceId.House));
        }

        private static void AssertBox(WorldBox box, float x, float y, float width, float height)
        {
            Assert.That(new[] { box.X, box.Y, box.Width, box.Height }, Is.EqualTo(new[] { x, y, width, height }));
        }

        private static void AssertPoint(WorldPoint point, float x, float y)
        {
            Assert.That(new[] { point.X, point.Y }, Is.EqualTo(new[] { x, y }));
        }
    }
}
