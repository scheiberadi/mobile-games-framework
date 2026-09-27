using System;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class PlacesTests
    {
        private static readonly WorldBox FirstView = new WorldBox(0f, 0f, Places.ViewWidth, Places.ViewHeight);

        [Test]
        public void CatalogueHasHouseSchoolStorePlaygroundInThatFixedOrderWithEverythingFilledIn()
        {
            Assert.That(Places.All.Count, Is.EqualTo(11));
            Assert.That(Places.All[0].Id, Is.EqualTo(PlaceId.House));
            Assert.That(Places.All[1].Id, Is.EqualTo(PlaceId.School));
            Assert.That(Places.All[2].Id, Is.EqualTo(PlaceId.Store));
            Assert.That(Places.All[3].Id, Is.EqualTo(PlaceId.Playground));
            Assert.That(Places.All[4].Id, Is.EqualTo(PlaceId.ZooFarm));
            Assert.That(Places.All[5].Id, Is.EqualTo(PlaceId.ScienceLab));
            Assert.That(Places.All[6].Id, Is.EqualTo(PlaceId.Workshop));
            Assert.That(Places.All[7].Id, Is.EqualTo(PlaceId.ArtStudio));
            Assert.That(Places.All[8].Id, Is.EqualTo(PlaceId.BrainGym));
            Assert.That(Places.All[9].Id, Is.EqualTo(PlaceId.FriendsPark));
            Assert.That(Places.All[10].Id, Is.EqualTo(PlaceId.Arcade));
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

        // House has no road at all (it is the junction). School and Store have their own bespoke road picture,
        // fitted to a RoadBox that contains every waypoint. Every other place has neither: MapScreen scatters
        // stepping stones along its Road instead (see MapPathTests.StonePoints* and MapScreen.AddStonePath).
        [Test]
        public void HouseHasNoRoadPictureSchoolAndStoreHaveOneAndEveryOtherPlaceUsesStonesInstead()
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
            foreach (var id in new[] { PlaceId.Playground, PlaceId.ZooFarm, PlaceId.ScienceLab, PlaceId.Workshop, PlaceId.ArtStudio, PlaceId.BrainGym, PlaceId.FriendsPark, PlaceId.Arcade })
            {
                var place = Places.Find(id);
                Assert.IsNull(place.RoadSprite, id + " has no bespoke road sprite");
                Assert.IsNull(place.RoadBox, id + " has no road box");
                Assert.That(place.Road.Count, Is.GreaterThanOrEqualTo(2), id + " road waypoints");
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
            AssertBox(Places.Find(PlaceId.School).TapBox, -470f, -10f, 280f, 240f);
            AssertBox(Places.Find(PlaceId.Store).TapBox, 490f, -225f, 280f, 240f);
            AssertPoint(Places.Find(PlaceId.House).StandingSpot, -120f, -300f);
            AssertPoint(Places.Find(PlaceId.School).StandingSpot, -470f, -170f);
            AssertPoint(Places.Find(PlaceId.Store).StandingSpot, 535f, -420f);
            AssertPoint(Places.Junction, 60f, -190f);
            AssertPoint(Places.InitialView, 0f, 0f);
        }

        // House/School/Store are the fixed M1-M3 "Initial composition"; every place added since (M4's new POIs,
        // Playground first) is real map-composition work, placed anywhere in the larger pannable world instead -
        // see the M4 plan's own flag on this. Only the original three are held to fitting the first view.
        [Test]
        public void TheInitialThreeBuildingsFitInsideTheFirstViewWithMargin()
        {
            foreach (var id in new[] { PlaceId.House, PlaceId.School, PlaceId.Store })
            {
                var box = Places.Find(id).TapBox;
                Assert.That(box.XMin, Is.GreaterThanOrEqualTo(FirstView.XMin + 60f), id + " left margin");
                Assert.That(box.XMax, Is.LessThanOrEqualTo(FirstView.XMax - 60f), id + " right margin");
                Assert.That(box.YMin, Is.GreaterThanOrEqualTo(FirstView.YMin + 60f), id + " bottom margin");
                Assert.That(box.YMax, Is.LessThanOrEqualTo(FirstView.YMax - 60f), id + " top margin");
            }
        }

        [Test]
        public void BuildingsAreTappableSizedAndNeverOverlap()
        {
            foreach (var place in Places.All)
            {
                var box = place.TapBox;
                Assert.That(box.Width, Is.GreaterThanOrEqualTo(240f), place.Id + " width");
                Assert.That(box.Height, Is.GreaterThanOrEqualTo(240f), place.Id + " height");
                Assert.That(box.XMin, Is.GreaterThanOrEqualTo(-Places.WorldWidth / 2f), place.Id + " left world edge");
                Assert.That(box.XMax, Is.LessThanOrEqualTo(Places.WorldWidth / 2f), place.Id + " right world edge");
                Assert.That(box.YMin, Is.GreaterThanOrEqualTo(-Places.WorldHeight / 2f), place.Id + " bottom world edge");
                Assert.That(box.YMax, Is.LessThanOrEqualTo(Places.WorldHeight / 2f), place.Id + " top world edge");
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
        public void CharacterAreasAreTappableSizedInsideTheWorldAndOnlyCoverTheirOwnBuildingsTapBox()
        {
            foreach (var place in Places.All)
            {
                var area = place.StandingArea;
                Assert.That(area.Width, Is.GreaterThanOrEqualTo(240f));
                Assert.That(area.Height, Is.GreaterThanOrEqualTo(240f));
                // The characters stand at their building's door, so the Store spot is below the first view (the camera follows).
                Assert.That(area.XMin, Is.GreaterThanOrEqualTo(-Places.WorldWidth / 2f), place.Id + " area left");
                Assert.That(area.XMax, Is.LessThanOrEqualTo(Places.WorldWidth / 2f), place.Id + " area right");
                Assert.That(area.YMin, Is.GreaterThanOrEqualTo(-Places.WorldHeight / 2f), place.Id + " area bottom");
                Assert.That(area.YMax, Is.LessThanOrEqualTo(Places.WorldHeight / 2f), place.Id + " area top");
                // Standing at the door means overlapping the own building's tap box (relaxed on purpose); never another one.
                foreach (var building in Places.All)
                    if (building.Id != place.Id)
                        Assert.IsFalse(area.Overlaps(building.TapBox), place.Id + " characters cover the " + building.Id + " tap box");
                Assert.IsFalse(area.Overlaps(Places.SettingsZone), place.Id + " area clears the settings zone");
                Assert.IsFalse(area.Overlaps(Places.CoinZone), place.Id + " area clears the coin zone");
            }
        }

        [Test]
        public void RoadsLeaveOppositeSidesOfTheJunctionAndNeverCross()
        {
            var school = Places.Find(PlaceId.School).Road;
            var store = Places.Find(PlaceId.Store).Road;
            for (var i = 1; i < school.Count; i++) Assert.That(school[i].X, Is.LessThan(Places.Junction.X), "school road stays left of the junction");
            for (var i = 1; i < store.Count; i++) Assert.That(store[i].X, Is.GreaterThan(Places.Junction.X), "store road stays right of the junction");
            for (var i = 1; i < school.Count; i++)
            for (var j = 1; j < store.Count; j++)
                Assert.IsFalse(Crosses(school[i - 1], school[i], store[j - 1], store[j]) && !(i == 1 && j == 1), "roads cross");
        }

        // A road never runs through any building's tap box (the ends stop at the door, just outside it).
        [Test]
        public void RoadsNeverEnterAnyTapBox()
        {
            foreach (var place in Places.All)
            foreach (var building in Places.All)
            {
                var box = building.TapBox;
                for (var i = 0; i < place.Road.Count; i++)
                {
                    Assert.IsFalse(box.Contains(place.Road[i]), place.Id + " road point " + i + " is inside the " + building.Id + " tap box");
                    if (i == 0) continue;
                    var a = place.Road[i - 1];
                    var b = place.Road[i];
                    for (var s = 1; s < 200; s++)
                    {
                        var t = s / 200f;
                        Assert.IsFalse(box.Contains(new WorldPoint(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t)),
                            place.Id + " road segment " + i + " runs through the " + building.Id + " tap box");
                    }
                }
            }
        }

        // True when two segments properly intersect (a shared start point does not count; callers skip the first pair).
        private static bool Crosses(WorldPoint a, WorldPoint b, WorldPoint c, WorldPoint d)
        {
            float Side(WorldPoint p, WorldPoint q, WorldPoint r) => (q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X);
            return Side(a, b, c) * Side(a, b, d) < 0f && Side(c, d, a) * Side(c, d, b) < 0f;
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
