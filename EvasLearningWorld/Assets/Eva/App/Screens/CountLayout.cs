using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvasLearningWorld.App
{
    // Scattered object-field positions for a round's total item count (targets + distractors), canvas units
    // with the origin at the screen centre (see CountScreen). Slots are dropped at random inside the
    // whiteboard in school_bg.svg (canvas x -430..295, y -120..430), so they never sit in a straight row or
    // tidy grid. The hit area (and icon) shrinks as the count grows (200 up to 5 items, then 160/150/135/130).
    // Placement is deterministic per seed: rejection sampling (square hit areas never overlap), with a
    // guaranteed jittered-grid fallback, so it never fails or throws for 1..20 items. The child answers via
    // the tiles, so small slots are only a counting aid and are exempt from the 240 MinTap rule.
    // Positions come back sorted left-to-right (then top-to-bottom), so slot order is reading order.
    public static class CountLayout
    {
        public const int MaxQuantity = 20;

        // Region the hit rects must stay inside: clear of the home button, coins, Eva and the answer tiles.
        public const float FieldMinX = -410f, FieldMaxX = 275f, FieldMinY = -105f, FieldMaxY = 420f;

        private const int SamplesPerItem = 60;
        private const int Restarts = 20;

        // Side of one object's tap area for a total item count.
        public static float HitSize(int total)
        {
            CheckRange(total);
            if (total <= 5) return 200f;
            if (total <= 8) return 160f;
            if (total <= 12) return 150f;
            if (total <= 15) return 135f;
            return 130f;
        }

        public static Vector2[] Scatter(int total, int seed)
        {
            CheckRange(total);
            var hit = HitSize(total);
            var rng = new System.Random(seed);
            var result = TryRejectionSample(total, hit, rng) ?? JitteredGrid(total, hit, rng);
            Array.Sort(result, (a, b) => a.x != b.x ? a.x.CompareTo(b.x) : b.y.CompareTo(a.y));
            return result;
        }

        private static Vector2[] TryRejectionSample(int total, float hit, System.Random rng)
        {
            var half = hit / 2f;
            var minX = FieldMinX + half; var maxX = FieldMaxX - half;
            var minY = FieldMinY + half; var maxY = FieldMaxY - half;
            for (var restart = 0; restart < Restarts; restart++)
            {
                var placed = new List<Vector2>(total);
                while (placed.Count < total)
                {
                    var found = false;
                    for (var attempt = 0; attempt < SamplesPerItem && !found; attempt++)
                    {
                        var p = new Vector2(Lerp(minX, maxX, rng), Lerp(minY, maxY, rng));
                        found = true;
                        foreach (var q in placed)
                            if (Mathf.Max(Mathf.Abs(p.x - q.x), Mathf.Abs(p.y - q.y)) < hit) { found = false; break; }
                        if (found) placed.Add(p);
                    }
                    if (!found) break;
                }
                if (placed.Count == total) return placed.ToArray();
            }
            return null;
        }

        // Cells at least `hit` wide and tall, `total` of them picked at random, each spot jittered within
        // the slack its cell leaves around the hit square (so squares can never overlap or leave the field).
        private static Vector2[] JitteredGrid(int total, float hit, System.Random rng)
        {
            var width = FieldMaxX - FieldMinX; var height = FieldMaxY - FieldMinY;
            var maxCols = Mathf.Max(1, Mathf.FloorToInt(width / hit));
            var maxRows = Mathf.Max(1, Mathf.FloorToInt(height / hit));
            var columns = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(total * width / height)), 1, maxCols);
            var rows = (total + columns - 1) / columns;
            while (rows > maxRows && columns < maxCols) { columns++; rows = (total + columns - 1) / columns; }

            var cells = new List<int>(columns * rows);
            for (var i = 0; i < columns * rows; i++) cells.Add(i);
            for (var i = cells.Count - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (cells[i], cells[j]) = (cells[j], cells[i]);
            }

            var cellW = width / columns; var cellH = height / rows;
            var slackX = (cellW - hit) / 2f; var slackY = (cellH - hit) / 2f;
            var result = new Vector2[total];
            for (var i = 0; i < total; i++)
            {
                var col = cells[i] % columns; var row = cells[i] / columns;
                var cx = FieldMinX + (col + 0.5f) * cellW; var cy = FieldMinY + (row + 0.5f) * cellH;
                result[i] = new Vector2(cx + Lerp(-slackX, slackX, rng), cy + Lerp(-slackY, slackY, rng));
            }
            return result;
        }

        private static float Lerp(float a, float b, System.Random rng) => a + (b - a) * (float)rng.NextDouble();

        private static void CheckRange(int total)
        {
            if (total < 1 || total > MaxQuantity) throw new ArgumentOutOfRangeException(nameof(total));
        }
    }
}
