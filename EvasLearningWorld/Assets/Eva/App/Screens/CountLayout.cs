using System;
using UnityEngine;

namespace EvasLearningWorld.App
{
    // Object-field positions per round quantity, canvas units with the origin at the screen centre (see
    // CountScreen). Quantities 1-5 are fixed, hand-placed rows (object hit area 240, centres at least 220
    // apart). Quantities 6-20 are a tidy grid, rows centred and read left-to-right, top-to-bottom, with the
    // hit area (and icon) scaled down as the quantity grows so 20 fit in x -450..365, y -140..440: clear of
    // the home button, the coins, Eva and the answer tiles. The child answers via the tiles, so those small
    // slots are only a counting aid and are exempt from the 240 MinTap rule (CountLayoutTests checks no overlap).
    public static class CountLayout
    {
        private static readonly Vector2[][] ByQuantity =
        {
            new[] { new Vector2(-170f, 150f) },
            new[] { new Vector2(-320f, 150f), new Vector2(-20f, 150f) },
            new[] { new Vector2(-470f, 150f), new Vector2(-170f, 150f), new Vector2(130f, 150f) },
            new[]
            {
                new Vector2(-470f, 270f), new Vector2(-170f, 270f),
                new Vector2(-320f, 60f), new Vector2(-20f, 60f)
            },
            new[]
            {
                new Vector2(-470f, 270f), new Vector2(-170f, 270f), new Vector2(130f, 270f),
                new Vector2(-320f, 60f), new Vector2(-20f, 60f)
            }
        };

        public const int MaxQuantity = 20;
        private const float GridCenterX = -45f, GridCenterY = 150f;

        // Grid columns and cell size (= hit size) per quantity band: 6-8, 9-12, 13-15, 16-20.
        private static void GridFor(int quantity, out int columns, out float cell)
        {
            if (quantity <= 8) { columns = 4; cell = 200f; }
            else if (quantity <= 12) { columns = 4; cell = 190f; }
            else if (quantity <= 15) { columns = 5; cell = 160f; }
            else { columns = 5; cell = 145f; }
        }

        // Side of one object's tap area: 240 (EvaUi.MinTap) up to 5 objects, smaller for the grids.
        public static float HitSize(int quantity)
        {
            CheckRange(quantity);
            if (quantity <= ByQuantity.Length) return 240f;
            GridFor(quantity, out _, out var cell);
            return cell;
        }

        public static Vector2[] Positions(int quantity)
        {
            CheckRange(quantity);
            if (quantity <= ByQuantity.Length) return ByQuantity[quantity - 1];

            GridFor(quantity, out var columns, out var cell);
            var rows = (quantity + columns - 1) / columns;
            var positions = new Vector2[quantity];
            for (var i = 0; i < quantity; i++)
            {
                var row = i / columns;
                var inRow = Math.Min(columns, quantity - row * columns);
                var x = GridCenterX + ((i % columns) - (inRow - 1) / 2f) * cell;
                var y = GridCenterY + ((rows - 1) / 2f - row) * cell;
                positions[i] = new Vector2(x, y);
            }
            return positions;
        }

        private static void CheckRange(int quantity)
        {
            if (quantity < 1 || quantity > MaxQuantity) throw new ArgumentOutOfRangeException(nameof(quantity));
        }
    }
}
