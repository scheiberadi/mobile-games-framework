using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // Walking routes between places. Every trip goes out along one road to the junction beside the House and along the
    // other road to the destination; the trip time is capped so a child never waits long.
    public static class MapPath
    {
        public const float Speed = 400f, MinSeconds = 0.4f, MaxSeconds = 2.5f;

        public static WorldPoint[] Route(PlaceId from, PlaceId to)
        {
            var start = Places.Find(from);
            var end = Places.Find(to);
            var points = new List<WorldPoint> { start.StandingSpot };
            if (from == to) return points.ToArray();
            for (var i = start.Road.Count - 1; i >= 0; i--) Append(points, start.Road[i]);
            foreach (var point in end.Road) Append(points, point);
            Append(points, end.StandingSpot);
            return points.ToArray();
        }

        public static float Length(IReadOnlyList<WorldPoint> route)
        {
            var length = 0f;
            for (var i = 1; i < route.Count; i++) length += Distance(route[i - 1], route[i]);
            return length;
        }

        // Constant speed, at least MinSeconds and at most MaxSeconds; 0 for a route with no length.
        public static float Duration(IReadOnlyList<WorldPoint> route)
        {
            var length = Length(route);
            if (length <= 0f) return 0f;
            return Math.Min(MaxSeconds, Math.Max(MinSeconds, length / Speed));
        }

        // The point a fraction t (0 to 1, clamped) of the way along the route, by distance.
        public static WorldPoint PositionAt(IReadOnlyList<WorldPoint> route, float t)
        {
            if (route.Count == 1) return route[0];
            var remaining = Math.Max(0f, Math.Min(1f, t)) * Length(route);
            for (var i = 1; i < route.Count; i++)
            {
                var segment = Distance(route[i - 1], route[i]);
                if (remaining <= segment || i == route.Count - 1)
                {
                    var k = segment <= 0f ? 1f : Math.Min(1f, remaining / segment);
                    return new WorldPoint(route[i - 1].X + (route[i].X - route[i - 1].X) * k, route[i - 1].Y + (route[i].Y - route[i - 1].Y) * k);
                }
                remaining -= segment;
            }
            return route[route.Count - 1];
        }

        // Evenly spaced points along a polyline road, `spacing` apart by arc length, one `spacing` in from the first
        // point (so a stone never sits on the doorstep). Used to scatter stepping stones along a road that has no
        // bespoke road art of its own.
        public static WorldPoint[] StonePoints(IReadOnlyList<WorldPoint> road, float spacing)
        {
            var points = new List<WorldPoint>();
            if (road.Count < 2 || spacing <= 0f) return points.ToArray();
            var carry = spacing;
            for (var i = 1; i < road.Count; i++)
            {
                var a = road[i - 1];
                var b = road[i];
                var segment = Distance(a, b);
                if (segment <= 0f) continue;
                var d = carry;
                while (d < segment)
                {
                    var k = d / segment;
                    points.Add(new WorldPoint(a.X + (b.X - a.X) * k, a.Y + (b.Y - a.Y) * k));
                    d += spacing;
                }
                carry = d - segment;
            }
            return points.ToArray();
        }

        private static void Append(List<WorldPoint> points, WorldPoint point)
        {
            if (points.Count > 0 && Distance(points[points.Count - 1], point) < 1f) return;
            points.Add(point);
        }

        private static float Distance(WorldPoint a, WorldPoint b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }

    // Keeps the visible rectangle (its size in world units) inside the world; on an axis where the view is larger than
    // the world the camera is centred.
    public static class MapCamera
    {
        public static WorldPoint Clamp(WorldPoint centre) => Clamp(centre, Places.ViewWidth, Places.ViewHeight);

        public static WorldPoint Clamp(WorldPoint centre, float visibleWidth, float visibleHeight) =>
            new WorldPoint(Axis(centre.X, Places.WorldWidth, visibleWidth), Axis(centre.Y, Places.WorldHeight, visibleHeight));

        private static float Axis(float value, float world, float visible)
        {
            var half = (world - visible) / 2f;
            if (half <= 0f) return 0f;
            return Math.Max(-half, Math.Min(half, value));
        }
    }
}
