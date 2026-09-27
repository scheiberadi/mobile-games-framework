using System;

namespace EvasLearningWorld.Rules
{
    public sealed class ShortestPathRound
    {
        // One simple bulge-shaped route per lane, each with its own start (Routes[i][0]) and finish
        // (Routes[i][last]); drawn via MazeCorridorRenderer. Lengths[i] == MapPath.Length(Routes[i]).
        // ShortestIndex is the lane whose route is shortest - the correct tap.
        public WorldPoint[][] Routes;
        public float[] Lengths;
        public int ShortestIndex;
    }

    // Shortest Path (Playground, spec 4.1): Logic/spatial, path planning. Reuses Finger Maze's corridor
    // renderer (MazeCorridorRenderer, App/Ui) to draw 2-3 separate routes side by side rather than a single
    // grid-walk maze - that mechanic is one path to drag or tap-in-order along; here the child compares several
    // at once, so each route gets its own start and finish instead of sharing Finger Maze's single grid walk.
    // Each route bulges away from a straight line between its own start and finish by a different amount; the
    // amount controls its length monotonically (longer bulge = longer route), so ranking routes by bulge
    // amount ranks them by length. Progression: route count (2 then 3) and how close the routes' lengths are -
    // the real difficulty lever, since the shortest route still usually has a nonzero bulge of its own, never
    // simply "the one flat line".
    public static class ShortestPathRoundGenerator
    {
        public const int RoundsPerSession = 5;

        public const float StartX = -600f;
        public const float FinishX = -60f;
        private static readonly float[] LaneYByCount2 = { 90f, -90f };
        private static readonly float[] LaneYByCount3 = { 150f, 0f, -150f };

        // Index i = level (i + 1).
        private static readonly int[] RouteCountByLevel = { 2, 2, 2, 3, 3, 3 };
        private static readonly float[] AmplitudeGapByLevel = { 160f, 160f, 110f, 110f, 60f, 60f };
        private const float BaseAmplitude = 30f;

        public static ShortestPathRound Create(int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var count = RouteCountByLevel[index];
            var gap = AmplitudeGapByLevel[index];
            var lanes = count == 2 ? LaneYByCount2 : LaneYByCount3;
            var midX = (StartX + FinishX) / 2f;

            // Rank 0 is the shortest; shuffling which lane gets which rank keeps the shortest route from
            // always landing in the same lane.
            var ranks = new int[count];
            for (var i = 0; i < count; i++) ranks[i] = i;
            Shuffle(ranks, rng);

            var routes = new WorldPoint[count][];
            var lengths = new float[count];
            var shortestIndex = 0;
            for (var i = 0; i < count; i++)
            {
                var amplitude = BaseAmplitude + ranks[i] * gap;
                var laneY = lanes[i];
                routes[i] = new[]
                {
                    new WorldPoint(StartX, laneY),
                    new WorldPoint(midX, laneY + amplitude),
                    new WorldPoint(FinishX, laneY),
                };
                lengths[i] = MapPath.Length(routes[i]);
                if (ranks[i] == 0) shortestIndex = i;
            }

            return new ShortestPathRound { Routes = routes, Lengths = lengths, ShortestIndex = shortestIndex };
        }

        private static void Shuffle(int[] values, Random rng)
        {
            for (var i = values.Length - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }
    }
}
