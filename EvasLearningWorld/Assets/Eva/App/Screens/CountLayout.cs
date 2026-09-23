using System;
using UnityEngine;

namespace EvasLearningWorld.App
{
    // Fixed, hand-placed object-field positions per round quantity, canvas units with the origin at the screen
    // centre (see CountScreen). A generator was not worth it here: five short, known-good rows read and review
    // more easily than a packing algorithm, and the table needs to stay exactly in sync with the plan's own
    // reference layout (Task 7 brief). Object field: x -620..280, y -40..340. Object hit area is 240 wide, so
    // centres are kept at least 220 apart everywhere in the table (CountLayoutTests checks this).
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

        public static Vector2[] Positions(int quantity)
        {
            if (quantity < 1 || quantity > ByQuantity.Length) throw new ArgumentOutOfRangeException(nameof(quantity));
            return ByQuantity[quantity - 1];
        }
    }
}
