using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // Pure helpers for drag-and-drop presenters (DragToTargetScreen). Kept in Rules so the placement maths is
    // unit-tested directly: drag logic is easy to get subtly wrong and cannot be debugged without a device.
    public static class DropGeometry
    {
        // Index of the centre nearest to (x, y) that lies within `radius` of it, or -1 if none does. Ties go to the
        // lower index. The radius is inclusive.
        public static int NearestWithinRadius(float x, float y, IReadOnlyList<WorldPoint> centres, float radius)
        {
            var best = -1;
            var bestDist2 = float.MaxValue;
            var radius2 = radius * radius;
            for (var i = 0; i < centres.Count; i++)
            {
                var dx = x - centres[i].X;
                var dy = y - centres[i].Y;
                var dist2 = dx * dx + dy * dy;
                if (dist2 <= radius2 && dist2 < bestDist2) { best = i; bestDist2 = dist2; }
            }
            return best;
        }
    }

    // One round of a "drag each item onto its matching target" game. ItemKeys and TargetKeys hold the same set of
    // keys (every item has exactly one target, the one with the same key); they are shown in these orders, and the
    // item order is deliberately different from the target order so the layout never gives the pairing away.
    // HintItemIndex is the item a hint or demonstration helps with first.
    public sealed class DragToTargetRound
    {
        public string[] ItemKeys;
        public string[] TargetKeys;
        public int HintItemIndex;

        // The index in TargetKeys of the target that item `itemIndex` belongs on.
        public int TargetIndexOf(int itemIndex) => Array.IndexOf(TargetKeys, ItemKeys[itemIndex]);
    }

    public static class DragToTargetRoundBuilder
    {
        // Turns an Item to Shadow round (target plus shuffled choices, see ItemToShadowRoundGenerator) into a
        // multi-pair drag round without changing how that generator picks content: every choice becomes both an
        // object and a shadow, in the generator's own (shuffled) order for the shadows and a different order for
        // the objects. The round's target is the item the hint helps first.
        public static DragToTargetRound FromItemToShadow(ItemToShadowRound round, Random rng)
        {
            var targetKeys = (string[])round.Choices.Clone();
            var itemKeys = (string[])round.Choices.Clone();
            ShuffleDifferentFrom(itemKeys, targetKeys, rng);
            return new DragToTargetRound
            {
                ItemKeys = itemKeys,
                TargetKeys = targetKeys,
                HintItemIndex = Array.IndexOf(itemKeys, round.TargetKey),
            };
        }

        // Shuffles `items` until no position equals `reference` at the same index (a derangement), so no object
        // sits directly under/over its own shadow. Needs 2+ items; one item is left as is.
        private static void ShuffleDifferentFrom(string[] items, string[] reference, Random rng)
        {
            if (items.Length < 2) return;
            do
            {
                for (var i = items.Length - 1; i > 0; i--)
                {
                    var j = rng.Next(0, i + 1);
                    (items[i], items[j]) = (items[j], items[i]);
                }
            } while (HasFixedPoint(items, reference));
        }

        private static bool HasFixedPoint(string[] items, string[] reference)
        {
            for (var i = 0; i < items.Length; i++) if (items[i] == reference[i]) return true;
            return false;
        }
    }
}
