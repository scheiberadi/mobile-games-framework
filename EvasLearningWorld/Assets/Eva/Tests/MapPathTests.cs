using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class MapPathTests
    {
        private static readonly PlaceId[] Ids = { PlaceId.House, PlaceId.School, PlaceId.Store };

        [Test]
        public void EveryRouteBetweenDifferentPlacesRunsThroughTheJunctionStartsAndEndsAtTheStandingSpots()
        {
            foreach (var from in Ids)
            foreach (var to in Ids)
            {
                if (from == to) continue;
                var route = MapPath.Route(from, to);
                var start = Places.Find(from).StandingSpot;
                var end = Places.Find(to).StandingSpot;
                Assert.That(route[0].X, Is.EqualTo(start.X), from + ">" + to);
                Assert.That(route[0].Y, Is.EqualTo(start.Y), from + ">" + to);
                Assert.That(route[route.Length - 1].X, Is.EqualTo(end.X), from + ">" + to);
                Assert.That(route[route.Length - 1].Y, Is.EqualTo(end.Y), from + ">" + to);
                var passesJunction = false;
                foreach (var point in route)
                    if (point.X == Places.Junction.X && point.Y == Places.Junction.Y) passesJunction = true;
                Assert.IsTrue(passesJunction, from + ">" + to + " must pass the house junction");
            }
        }

        [Test]
        public void ARouteToTheSamePlaceIsASinglePointWithZeroDuration()
        {
            foreach (var id in Ids)
            {
                var route = MapPath.Route(id, id);
                Assert.That(route.Length, Is.EqualTo(1));
                Assert.That(MapPath.Duration(route), Is.EqualTo(0f));
            }
        }

        [Test]
        public void NoRouteHasConsecutiveDuplicatePoints()
        {
            foreach (var from in Ids)
            foreach (var to in Ids)
            {
                var route = MapPath.Route(from, to);
                for (var i = 1; i < route.Length; i++)
                    Assert.That(MapPath.Length(new[] { route[i - 1], route[i] }), Is.GreaterThanOrEqualTo(1f), from + ">" + to + " point " + i);
            }
        }

        [Test]
        public void DurationIsCappedAtTwoAndAHalfSecondsAndNeverShorterThanTheMinimumForARealTrip()
        {
            foreach (var from in Ids)
            foreach (var to in Ids)
            {
                if (from == to) continue;
                var seconds = MapPath.Duration(MapPath.Route(from, to));
                Assert.That(seconds, Is.LessThanOrEqualTo(MapPath.MaxSeconds), from + ">" + to);
                Assert.That(seconds, Is.GreaterThanOrEqualTo(MapPath.MinSeconds), from + ">" + to);
            }
            var far = new[] { new WorldPoint(0f, 0f), new WorldPoint(5000f, 0f) };
            Assert.That(MapPath.Duration(far), Is.EqualTo(MapPath.MaxSeconds));
            var near = new[] { new WorldPoint(0f, 0f), new WorldPoint(10f, 0f) };
            Assert.That(MapPath.Duration(near), Is.EqualTo(MapPath.MinSeconds));
        }

        [Test]
        public void PositionAtWalksTheRouteFromItsFirstToItsLastPoint()
        {
            var route = new[] { new WorldPoint(0f, 0f), new WorldPoint(100f, 0f), new WorldPoint(100f, 100f) };
            Assert.That(MapPath.Length(route), Is.EqualTo(200f).Within(0.001f));
            AssertAt(route, 0f, 0f, 0f);
            AssertAt(route, 0.25f, 50f, 0f);
            AssertAt(route, 0.5f, 100f, 0f);
            AssertAt(route, 0.75f, 100f, 50f);
            AssertAt(route, 1f, 100f, 100f);
            AssertAt(route, -3f, 0f, 0f);
            AssertAt(route, 7f, 100f, 100f);
        }

        [Test]
        public void PositionAtOfASinglePointRouteIsThatPoint()
        {
            var route = new[] { new WorldPoint(7f, -3f) };
            AssertAt(route, 0.5f, 7f, -3f);
        }

        [Test]
        public void StonePointsAreEvenlySpacedAlongAStraightRoad()
        {
            var road = new[] { new WorldPoint(0f, 0f), new WorldPoint(500f, 0f) };
            var points = MapPath.StonePoints(road, 100f);
            Assert.That(points.Length, Is.EqualTo(4)); // 100, 200, 300, 400 - never on the doorstep at 0 or 500
            for (var i = 0; i < points.Length; i++)
            {
                Assert.That(points[i].X, Is.EqualTo(100f * (i + 1)).Within(0.001f));
                Assert.That(points[i].Y, Is.EqualTo(0f).Within(0.001f));
            }
        }

        [Test]
        public void StonePointsCarryLeftoverSpacingAcrossABend()
        {
            // First leg 150 long: one stone at 100, 50 left over. Second leg starts 50 in, so the next stone is 50
            // further along it (at x=100+50=150), then every 100 after.
            var road = new[] { new WorldPoint(0f, 0f), new WorldPoint(150f, 0f), new WorldPoint(150f, 250f) };
            var points = MapPath.StonePoints(road, 100f);
            Assert.That(points.Length, Is.EqualTo(3));
            Assert.That(points[0].X, Is.EqualTo(100f).Within(0.001f));
            Assert.That(points[0].Y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(points[1].X, Is.EqualTo(150f).Within(0.001f));
            Assert.That(points[1].Y, Is.EqualTo(50f).Within(0.001f));
            Assert.That(points[2].X, Is.EqualTo(150f).Within(0.001f));
            Assert.That(points[2].Y, Is.EqualTo(150f).Within(0.001f));
        }

        [Test]
        public void StonePointsOfARoadShorterThanTheSpacingIsEmpty()
        {
            var road = new[] { new WorldPoint(0f, 0f), new WorldPoint(50f, 0f) };
            Assert.That(MapPath.StonePoints(road, 100f), Is.Empty);
        }

        [Test]
        public void StonePointsOfASinglePointRoadIsEmpty()
        {
            var road = new[] { new WorldPoint(0f, 0f) };
            Assert.That(MapPath.StonePoints(road, 100f), Is.Empty);
        }

        [Test]
        public void EveryStonePathPlaceHasAtLeastOneStone()
        {
            foreach (var place in Places.All)
            {
                if (place.RoadSprite != null) continue; // has its own bespoke road art instead
                if (place.Id == PlaceId.House) continue; // the junction itself: a single point, no road to walk
                Assert.That(MapPath.StonePoints(place.Road, 130f), Is.Not.Empty, place.Id + " has no stones on its road");
            }
        }

        // Several places' roads share an identical prefix out of the junction (same literal waypoints) - stoning
        // each place's road independently would draw the shared stretch's stones once per place that walks it.
        [Test]
        public void SharedStonePointsDedupesTheJunctionsSharedPrefix()
        {
            var naive = 0;
            foreach (var place in Places.All)
                if (place.RoadSprite == null) naive += MapPath.StonePoints(place.Road, 130f).Length;
            var deduped = MapPath.SharedStonePoints(Places.All, 130f).Length;
            Assert.That(deduped, Is.LessThan(naive), "shared trunk stones should be drawn once, not once per place");
        }

        [Test]
        public void CameraClampsToTheWorldEdgesAndLeavesInsideCentresAlone()
        {
            var halfX = (Places.WorldWidth - Places.ViewWidth) / 2f;
            var halfY = (Places.WorldHeight - Places.ViewHeight) / 2f;
            Assert.That(halfX, Is.EqualTo(1080f));
            Assert.That(halfY, Is.EqualTo(225f));
            AssertClamped(0f, 0f, 0f, 0f);
            AssertClamped(300f, -100f, 300f, -100f);
            AssertClamped(5000f, 0f, halfX, 0f);
            AssertClamped(-5000f, 0f, -halfX, 0f);
            AssertClamped(0f, 5000f, 0f, halfY);
            AssertClamped(0f, -5000f, 0f, -halfY);
            AssertClamped(-400f, -420f, -400f, -halfY); // centring near a bottom edge: the view stays inside the world
        }

        [Test]
        public void VisibleRectangleStaysInsideTheWorldAtEveryExtremePanForSeveralAspectRatios()
        {
            var sizes = new[] { new[] { 1440f, 900f }, new[] { 900f * 2340f / 1080f, 900f }, new[] { 1200f, 900f }, new[] { 4000f, 900f }, new[] { 1440f, 2000f } };
            var extremes = new[] { -9999f, -300f, 0f, 300f, 9999f };
            foreach (var size in sizes)
            foreach (var x in extremes)
            foreach (var y in extremes)
            {
                var c = MapCamera.Clamp(new WorldPoint(x, y), size[0], size[1]);
                AssertAxis(c.X, size[0], Places.WorldWidth);
                AssertAxis(c.Y, size[1], Places.WorldHeight);
            }
        }

        // Inside the world when the view is smaller; centred when it is larger.
        private static void AssertAxis(float centre, float visible, float world)
        {
            if (visible >= world) Assert.That(centre, Is.EqualTo(0f));
            else
            {
                Assert.That(centre - visible / 2f, Is.GreaterThanOrEqualTo(-world / 2f - 0.001f));
                Assert.That(centre + visible / 2f, Is.LessThanOrEqualTo(world / 2f + 0.001f));
            }
        }

        private static void AssertAt(WorldPoint[] route, float t, float x, float y)
        {
            var p = MapPath.PositionAt(route, t);
            Assert.That(p.X, Is.EqualTo(x).Within(0.001f), "x at t=" + t);
            Assert.That(p.Y, Is.EqualTo(y).Within(0.001f), "y at t=" + t);
        }

        private static void AssertClamped(float x, float y, float expectedX, float expectedY)
        {
            var c = MapCamera.Clamp(new WorldPoint(x, y));
            Assert.That(c.X, Is.EqualTo(expectedX).Within(0.001f));
            Assert.That(c.Y, Is.EqualTo(expectedY).Within(0.001f));
        }
    }
}
