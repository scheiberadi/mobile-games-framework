using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvasLearningWorld.App
{
    // Scattered object-field positions for a round's total item count (targets + distractors), canvas units
    // with the origin at the screen centre (see CountScreen). Slots are dropped at random inside the
    // whiteboard in school_bg.svg (canvas x -430..295, y -120..430), so they never sit in a straight row or
    // tidy grid. The slot (hit area and icon) size comes from the count and the board area:
    // clamp(sqrt(area * 0.42 / total), 100, band) where band is 200 up to 5 items, then 160/150/135/130, so
    // small counts keep big slots and 13-20 items get 100-108. The child answers via the tiles, so slots
    // are only a counting aid and are exempt from the 240 MinTap rule.
    // Placement is deterministic per seed: rejection sampling (square hit areas never overlap) with many
    // restarts, then an overlap-relaxation pass from random starts, and only as a last resort a staggered,
    // jittered grid. Positions come back sorted left-to-right (then top-to-bottom), i.e. reading order.
    public static class CountLayout
    {
        public const int MaxQuantity = 20;

        // Region the hit rects must stay inside: clear of the home button, coins, Eva and the answer tiles.
        public const float FieldMinX = -410f, FieldMaxX = 275f, FieldMinY = -105f, FieldMaxY = 420f;

        public const float MinSlotSize = 100f;
        private const float BoardFill = 0.42f;
        private const int SamplesPerItem = 100;
        private const int Restarts = 30;
        private const int RelaxRestarts = 20;
        private const int RelaxPasses = 300;

        // Side of one object's tap area (and its icon) for a total item count.
        public static float HitSize(int total)
        {
            CheckRange(total);
            var area = (FieldMaxX - FieldMinX) * (FieldMaxY - FieldMinY);
            return Mathf.Clamp(Mathf.Sqrt(area * BoardFill / total), MinSlotSize, BandSize(total));
        }

        private static float BandSize(int total)
        {
            if (total <= 5) return 200f;
            if (total <= 8) return 160f;
            if (total <= 12) return 150f;
            if (total <= 15) return 135f;
            return 130f;
        }

        public static Vector2[] Scatter(int total, int seed) => Scatter(total, seed, out _);

        // usedGridFallback reports whether the last-resort staggered grid had to be used (tests assert it never is).
        public static Vector2[] Scatter(int total, int seed, out bool usedGridFallback)
        {
            CheckRange(total);
            var hit = HitSize(total);
            var rng = new System.Random(seed);
            var result = TryRejectionSample(total, hit, rng) ?? TryRelax(total, hit, rng);
            usedGridFallback = result == null;
            if (result == null) result = JitteredGrid(total, hit, rng);
            SortReadingOrder(result);
            return result;
        }

        // The last-resort layout on its own, so it can be tested without needing the primary path to fail.
        public static Vector2[] GridFallback(int total, int seed)
        {
            CheckRange(total);
            var result = JitteredGrid(total, HitSize(total), new System.Random(seed));
            SortReadingOrder(result);
            return result;
        }

        private static void SortReadingOrder(Vector2[] positions) =>
            Array.Sort(positions, (a, b) => a.x != b.x ? a.x.CompareTo(b.x) : b.y.CompareTo(a.y));

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

        // Dense counts (18-20 at the 100 floor) rarely survive pure rejection sampling: start from random
        // points and push overlapping pairs apart along their shallower axis until none overlap.
        private static Vector2[] TryRelax(int total, float hit, System.Random rng)
        {
            var half = hit / 2f;
            var minX = FieldMinX + half; var maxX = FieldMaxX - half;
            var minY = FieldMinY + half; var maxY = FieldMaxY - half;
            var p = new Vector2[total];
            for (var restart = 0; restart < RelaxRestarts; restart++)
            {
                for (var i = 0; i < total; i++) p[i] = new Vector2(Lerp(minX, maxX, rng), Lerp(minY, maxY, rng));
                for (var pass = 0; pass < RelaxPasses; pass++)
                {
                    var moved = false;
                    for (var i = 0; i < total; i++)
                    for (var j = i + 1; j < total; j++)
                    {
                        var dx = p[i].x - p[j].x; var dy = p[i].y - p[j].y;
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) >= hit - 0.01f) continue;
                        moved = true;
                        var px = hit - Mathf.Abs(dx); var py = hit - Mathf.Abs(dy);
                        if (px < py)
                        {
                            var s = (dx >= 0f ? 1f : -1f) * (px / 2f + 0.05f);
                            p[i].x = Mathf.Clamp(p[i].x + s, minX, maxX); p[j].x = Mathf.Clamp(p[j].x - s, minX, maxX);
                        }
                        else
                        {
                            var s = (dy >= 0f ? 1f : -1f) * (py / 2f + 0.05f);
                            p[i].y = Mathf.Clamp(p[i].y + s, minY, maxY); p[j].y = Mathf.Clamp(p[j].y - s, minY, maxY);
                        }
                    }
                    if (!moved) return (Vector2[])p.Clone();
                }
            }
            return null;
        }

        // Last resort: a grid of cells at least `hit` apart, every other row shifted half a cell, `total` cells
        // picked at random and each jittered by up to 25% of the cell (limited so squares still never overlap).
        private static Vector2[] JitteredGrid(int total, float hit, System.Random rng)
        {
            var half = hit / 2f;
            var spanX = FieldMaxX - FieldMinX - hit; var spanY = FieldMaxY - FieldMinY - hit;
            var maxCols = Mathf.Max(1, Mathf.FloorToInt(spanX / hit + 0.5f));
            var maxRows = Mathf.Max(1, Mathf.FloorToInt(spanY / hit) + 1);
            var columns = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(total * (spanX + hit) / (spanY + hit))), 1, maxCols);
            var rows = (total + columns - 1) / columns;
            while (rows > maxRows && columns < maxCols) { columns++; rows = (total + columns - 1) / columns; }

            var cells = new List<int>(columns * rows);
            for (var i = 0; i < columns * rows; i++) cells.Add(i);
            for (var i = cells.Count - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (cells[i], cells[j]) = (cells[j], cells[i]);
            }

            var stepX = columns > 1 || rows > 1 ? spanX / (columns - 0.5f) : 0f;
            var stepY = rows > 1 ? spanY / (rows - 1) : 0f;
            var jitterX = Mathf.Max(0f, Mathf.Min(0.25f * stepX, (stepX - hit) / 2f));
            var jitterY = Mathf.Max(0f, Mathf.Min(0.25f * stepY, (stepY - hit) / 2f));
            var minX = FieldMinX + half; var maxX = FieldMaxX - half;
            var minY = FieldMinY + half; var maxY = FieldMaxY - half;
            var result = new Vector2[total];
            for (var i = 0; i < total; i++)
            {
                var col = cells[i] % columns; var row = cells[i] / columns;
                var cx = minX + (col + (row % 2 == 1 ? 0.5f : 0f)) * stepX;
                var cy = rows > 1 ? minY + row * stepY : (minY + maxY) / 2f;
                result[i] = new Vector2(
                    Mathf.Clamp(cx + Lerp(-jitterX, jitterX, rng), minX, maxX),
                    Mathf.Clamp(cy + Lerp(-jitterY, jitterY, rng), minY, maxY));
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
