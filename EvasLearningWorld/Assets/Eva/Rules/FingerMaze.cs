using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public sealed class FingerMazeRound
    {
        // The corridor's centreline, in canvas units: Path[0] is the start (where the character begins),
        // Path[last] is the finish. The screen draws a wide tile along each consecutive pair and lets the
        // character be dragged continuously along the whole line (see FingerMazePath.NearestFraction).
        public WorldPoint[] Path;
    }

    // Finger Maze (Playground, spec 4.1): Logic/spatial, the first of the plan's NAVIGATION games. Drag the
    // character along a path from start to finish, wide corridors, no timer. Progression: path length and how
    // often it turns (this placeholder generator's stand-in for "maze size, obstacle count" until real maze art
    // exists). The grid, CellCenter and Create shape here are reused as-is by every later NAVIGATION game
    // (Follow Numbers/Letters in Order, Shortest Path, Avoid Obstacles, Collect Everything) - only what each of
    // those puts ON the generated path (numbered/lettered checkpoints, hazards, pickups) is game-specific.
    public static class FingerMazeRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // The fixed grid every Playground NAVIGATION game generates its path over: canvas units, left of Eva's
        // anchor (x=627, see FingerMazeScreen) and clear of the Hud (see PatternCompletionScreen for the
        // y<=165 clearance reasoning - row 0's top edge here sits at 150 + 65 = 215... corridor tiles read as
        // background art rather than Hud-competing controls, so the small overlap at the very top row is fine).
        public const int Columns = 5;
        public const int Rows = 4;
        public const float CellSize = 130f;
        public const float OriginX = -590f;
        public const float OriginY = -55f;

        // Index i = level (i + 1): longer corridors that turn more often as level rises.
        private static readonly int[] CellCountByLevel = { 4, 5, 6, 7, 8, 9 };
        private static readonly double[] TurnChanceByLevel = { 0.3, 0.4, 0.5, 0.6, 0.7, 0.8 };
        private const int MaxAttempts = 200;

        private static readonly (int DCol, int DRow)[] Directions = { (1, 0), (-1, 0), (0, 1), (0, -1) };

        public static FingerMazeRound Create(int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var index = level - DifficultyLadder.MinLevel;
            var cells = Walk(CellCountByLevel[index], TurnChanceByLevel[index], rng);

            var path = new WorldPoint[cells.Count];
            for (var i = 0; i < cells.Count; i++) path[i] = CellCenter(cells[i].Col, cells[i].Row);
            return new FingerMazeRound { Path = path };
        }

        public static WorldPoint CellCenter(int col, int row) =>
            new WorldPoint(OriginX + col * CellSize, OriginY + (row - (Rows - 1) / 2f) * CellSize);

        // A self-avoiding walk on the grid: at each step it prefers to keep going in the same direction unless
        // turnChance rolls a turn (or going straight is blocked), so higher levels wind more. Retries from a
        // fresh random start on a dead end; MaxAttempts keeps this finite regardless of the rng sequence. Falls
        // back to the longest partial walk found if every attempt got stuck before reaching the requested
        // length (the grid comfortably fits every level's length, so this is a safety net, not the common case).
        private static List<(int Col, int Row)> Walk(int cellCount, double turnChance, Random rng)
        {
            List<(int Col, int Row)> best = null;
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var path = new List<(int Col, int Row)> { (rng.Next(Columns), rng.Next(Rows)) };
                var visited = new HashSet<(int, int)> { path[0] };
                var lastDirection = -1;

                while (path.Count < cellCount)
                {
                    var current = path[path.Count - 1];
                    var open = new List<int>();
                    for (var i = 0; i < Directions.Length; i++)
                    {
                        var next = (current.Col + Directions[i].DCol, current.Row + Directions[i].DRow);
                        if (next.Item1 < 0 || next.Item1 >= Columns || next.Item2 < 0 || next.Item2 >= Rows) continue;
                        if (visited.Contains(next)) continue;
                        open.Add(i);
                    }
                    if (open.Count == 0) break;

                    int chosen;
                    if (lastDirection >= 0 && open.Contains(lastDirection) && rng.NextDouble() >= turnChance)
                    {
                        chosen = lastDirection;
                    }
                    else
                    {
                        var turnsOnly = open.FindAll(i => i != lastDirection);
                        var pool = turnsOnly.Count > 0 ? turnsOnly : open;
                        chosen = pool[rng.Next(pool.Count)];
                    }

                    var nextCell = (current.Col + Directions[chosen].DCol, current.Row + Directions[chosen].DRow);
                    path.Add(nextCell);
                    visited.Add(nextCell);
                    lastDirection = chosen;
                }

                if (path.Count == cellCount) return path;
                if (best == null || path.Count > best.Count) best = path;
            }
            return best;
        }
    }

    // Drag-projection math shared by every Playground NAVIGATION game: given a raw finger position, finds where
    // along a fixed corridor (as a fraction 0-1) that position is closest to, so the character can snap onto
    // the centreline instead of following the finger 1:1 - the forgiveness "wide corridors, no timer" calls for.
    public static class FingerMazePath
    {
        public static float NearestFraction(IReadOnlyList<WorldPoint> path, WorldPoint point)
        {
            if (path.Count < 2) return 0f;
            var total = MapPath.Length(path);
            if (total <= 0f) return 0f;

            var bestDistanceSq = float.MaxValue;
            var bestAlong = 0f;
            var walked = 0f;
            for (var i = 1; i < path.Count; i++)
            {
                var a = path[i - 1];
                var b = path[i];
                var segLength = Distance(a, b);
                var k = segLength <= 0f ? 0f : Clamp01(Dot(point, a, b) / (segLength * segLength));
                var projectedX = a.X + (b.X - a.X) * k;
                var projectedY = a.Y + (b.Y - a.Y) * k;
                var dx = point.X - projectedX;
                var dy = point.Y - projectedY;
                var distanceSq = dx * dx + dy * dy;
                if (distanceSq < bestDistanceSq)
                {
                    bestDistanceSq = distanceSq;
                    bestAlong = walked + segLength * k;
                }
                walked += segLength;
            }
            return bestAlong / total;
        }

        private static float Dot(WorldPoint p, WorldPoint a, WorldPoint b) =>
            (p.X - a.X) * (b.X - a.X) + (p.Y - a.Y) * (b.Y - a.Y);

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        private static float Distance(WorldPoint a, WorldPoint b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
