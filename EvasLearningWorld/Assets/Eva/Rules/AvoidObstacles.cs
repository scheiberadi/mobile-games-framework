using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public sealed class AvoidObstaclesRound
    {
        // Finger Maze's own corridor, unchanged - the one true path from start to finish always stays hazard-free.
        public WorldPoint[] Path;

        // Grid cells adjacent to some interior waypoint of Path but never on it - decoys close enough to tempt a
        // wandering finger. AvoidObstaclesScreen decides what touching one means (see its own class comment).
        public WorldPoint[] Hazards;
    }

    // Avoid Obstacles (Playground, spec 4.1): reuses Finger Maze's own grid walk and corridor
    // (FingerMazeRoundGenerator) exactly as generated, then scatters a few hazard tiles on grid cells that
    // neighbour the path without ever sitting on it. Progression: hazard count. The plan's note that this
    // "reuses Finger Maze's drag mechanic with hazard tiles... instead of just walls" is why this generator only
    // places hazards near the path rather than altering the walk itself - the corridor a child can actually drag
    // along is always exactly as forgiving as Finger Maze's.
    public static class AvoidObstaclesRoundGenerator
    {
        public const int RoundsPerSession = 5;

        // Index i = level (i + 1): more hazards to watch for as level rises.
        private static readonly int[] HazardCountByLevel = { 1, 1, 2, 2, 3, 3 };
        private static readonly (int DCol, int DRow)[] Directions = { (1, 0), (-1, 0), (0, 1), (0, -1) };

        public static AvoidObstaclesRound Create(int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var maze = FingerMazeRoundGenerator.Create(level, rng);
            var hazards = PlaceHazards(maze.Path, HazardCountByLevel[level - DifficultyLadder.MinLevel], rng);
            return new AvoidObstaclesRound { Path = maze.Path, Hazards = hazards };
        }

        // Candidates are every in-grid neighbour of an interior waypoint (never the start or finish cell, so the
        // very ends of the corridor are always clear) that isn't itself one of the path's own cells. Picks up to
        // `count` of them at random; if fewer candidates exist than requested (a short, mostly-straight path),
        // returns however many there are rather than throwing - a quieter round, not an error.
        private static WorldPoint[] PlaceHazards(WorldPoint[] path, int count, Random rng)
        {
            var pathCells = new HashSet<(int Col, int Row)>();
            var cells = new (int Col, int Row)[path.Length];
            for (var i = 0; i < path.Length; i++)
            {
                cells[i] = CellOf(path[i]);
                pathCells.Add(cells[i]);
            }

            var candidates = new List<(int Col, int Row)>();
            var seen = new HashSet<(int Col, int Row)>();
            for (var i = 1; i < cells.Length - 1; i++)
            for (var d = 0; d < Directions.Length; d++)
            {
                var next = (Col: cells[i].Col + Directions[d].DCol, Row: cells[i].Row + Directions[d].DRow);
                if (next.Col < 0 || next.Col >= FingerMazeRoundGenerator.Columns) continue;
                if (next.Row < 0 || next.Row >= FingerMazeRoundGenerator.Rows) continue;
                if (pathCells.Contains(next) || seen.Contains(next)) continue;
                seen.Add(next);
                candidates.Add(next);
            }

            Shuffle(candidates, rng);
            var picked = candidates.Count < count ? candidates.Count : count;
            var hazards = new WorldPoint[picked];
            for (var i = 0; i < picked; i++) hazards[i] = FingerMazeRoundGenerator.CellCenter(candidates[i].Col, candidates[i].Row);
            return hazards;
        }

        private static (int Col, int Row) CellOf(WorldPoint point)
        {
            var col = (int)Math.Round((point.X - FingerMazeRoundGenerator.OriginX) / FingerMazeRoundGenerator.CellSize);
            var row = (int)Math.Round((point.Y - FingerMazeRoundGenerator.OriginY) / FingerMazeRoundGenerator.CellSize + (FingerMazeRoundGenerator.Rows - 1) / 2f);
            return (col, row);
        }

        private static void Shuffle<T>(List<T> values, Random rng)
        {
            for (var i = values.Count - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }
    }
}
