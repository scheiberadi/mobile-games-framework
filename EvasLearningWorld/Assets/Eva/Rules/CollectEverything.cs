using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public sealed class CollectEverythingRound
    {
        // Finger Maze's own corridor, unchanged.
        public WorldPoint[] Path;

        // A subset of Path's own interior waypoints (never Path[0] or Path[last]) that must all be dragged over
        // before reaching the finish counts. Ordered the same as they occur along Path.
        public WorldPoint[] Pickups;

        // Each pickup's own cumulative-length fraction (0-1) along Path - lets the screen tell, from the
        // dragger's own Fraction, which pickups have already been passed without recomputing the projection.
        public float[] PickupFractions;
    }

    // Collect Everything (Playground, spec 4.1): reuses Finger Maze's own grid walk, corridor and drag mechanic
    // unchanged (CollectEverythingRoundGenerator.Create just wraps FingerMazeRoundGenerator.Create), then marks a
    // few of the path's own interior waypoints as pickups the child must drag over before the finish counts.
    // Unlike Avoid Obstacles' hazards (placed beside the path, never on it), pickups sit directly on the
    // corridor - collecting one is just a matter of continuing to drag through, not straying off course, so this
    // game needs no equivalent of PathDragger.RawMoved. Progression: pickup count.
    public static class CollectEverythingRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // Index i = level (i + 1): more pickups to gather as level rises.
        private static readonly int[] PickupCountByLevel = { 1, 2, 2, 3, 3, 4 };

        public static CollectEverythingRound Create(int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var maze = FingerMazeRoundGenerator.Create(level, rng);
            var path = maze.Path;

            var interiorIndices = new List<int>();
            for (var i = 1; i < path.Length - 1; i++) interiorIndices.Add(i);
            Shuffle(interiorIndices, rng);

            var requested = PickupCountByLevel[level - DifficultyLadder.MinLevel];
            var count = Math.Min(requested, interiorIndices.Count);
            var chosen = interiorIndices.GetRange(0, count);
            chosen.Sort();

            var fractions = CumulativeFractions(path);
            var pickups = new WorldPoint[count];
            var pickupFractions = new float[count];
            for (var i = 0; i < count; i++)
            {
                pickups[i] = path[chosen[i]];
                pickupFractions[i] = fractions[chosen[i]];
            }

            return new CollectEverythingRound { Path = path, Pickups = pickups, PickupFractions = pickupFractions };
        }

        // Cumulative length fraction at each waypoint (0 at the start, 1 at the finish) - the same math
        // MazeCorridorRenderer.WaypointFractions uses (App/Ui), recomputed here since Rules stays engine- and
        // App-free.
        private static float[] CumulativeFractions(WorldPoint[] path)
        {
            var total = MapPath.Length(path);
            var fractions = new float[path.Length];
            var walked = 0f;
            for (var i = 0; i < path.Length; i++)
            {
                if (i > 0) walked += Distance(path[i - 1], path[i]);
                fractions[i] = total <= 0f ? 0f : walked / total;
            }
            return fractions;
        }

        private static float Distance(WorldPoint a, WorldPoint b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private static void Shuffle(List<int> values, Random rng)
        {
            for (var i = values.Count - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }
    }
}
