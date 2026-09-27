using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    public sealed class SuitcaseItem
    {
        // Sprite key: "dressup/<ItemId>" (Dress the Character/Dress for the Occasion's own sprite convention).
        public string ItemId;
        public bool Correct; // true for one of this trip's own 3 packable items; false for a distractor from another trip
        public WorldPoint TrayPosition;
    }

    public sealed class PackASuitcaseRound
    {
        // Reuses Occasion (school/beach/winter/birthday/sports/camping) as the trip type - the same 6 named
        // contexts as Dress for the Occasion, just packed for rather than worn, so no separate enum is defined.
        public Occasion Trip;

        // The suitcase's own fixed packed-item positions - always 3, independent of any round.
        public WorldPoint[] SlotPositions;

        // The shelf: 3 correct items (this trip's own packable items) plus 2 distractors from other trips,
        // shuffled together - same shape as Dress for the Occasion's shelf.
        public SuitcaseItem[] ShelfItems;
    }

    // Pack a Suitcase (Store, dressing cluster's 3rd and last game): reuses Dress for the Occasion's shelf
    // shape (3 correct items + 2 wrong-trip distractors, read that generator first) and DragItem's snap
    // mechanic, but the target is one suitcase rather than 3 distinct body-part slots - any correct item may
    // land in any of the suitcase's 3 fixed positions, first-come-first-served, since packing (unlike dressing)
    // has no "which slot" identity to match. A distractor dragged into the suitcase, same as a wrong-slot drag
    // in Dress for the Occasion, is this game's mistake; the piece floats back to the shelf rather than
    // sitting wherever it was dropped.
    //
    // Level doubles as which trip is being packed for, in the same brief order as Dress for the Occasion (so a
    // child who has just learned "beach" there meets the same word again here, reinforcing rather than
    // introducing a fresh vocabulary) - the same "level doubles as content" shape Shopping/Dress for the
    // Occasion already established.
    public static class PackASuitcaseRoundGenerator
    {
        public const int RoundsPerSession = 3; // fewer, longer rounds - same reasoning as the rest of the cluster.
        private const int DistractorCount = 2;

        // Placeholder packing-list catalogue, one 3-item list per trip - flagged for a real content/art pass
        // same as every other catalogue this session. Deliberately distinct items from Dress for the
        // Occasion's own worn-clothing catalogue (packed, not worn).
        private static readonly Dictionary<Occasion, string[]> PackingLists = new Dictionary<Occasion, string[]>
        {
            { Occasion.School, new[] { "backpack", "lunchbox", "notebook" } },
            { Occasion.Beach, new[] { "towel", "sunscreen", "flip_flops" } },
            { Occasion.Winter, new[] { "scarf", "mittens", "thermos" } },
            { Occasion.Birthday, new[] { "gift", "party_hat", "balloon" } },
            { Occasion.Sports, new[] { "water_bottle", "whistle", "ball" } },
            { Occasion.Camping, new[] { "flashlight", "tent", "sleeping_bag" } },
        };

        // The suitcase's own fixed packed-item positions: a row of 3, full 240-unit tap targets, same
        // reasoning as every other slot row this cluster (see DressTheCharacterRoundGenerator's class comment).
        public const float SlotSize = 240f; // EvaUi.MinTap
        public const float SnapRadius = 110f;
        private static readonly float[] SlotColumnX = { -260f, 0f, 260f };
        private const float SlotY = 40f;

        // Tray row (shelf): same 5-item, 250-unit-pitch row as Dress for the Occasion.
        private const float TrayPitch = 250f;
        private const float TrayY = -260f;

        public static WorldPoint[] SlotPositions()
        {
            var positions = new WorldPoint[SlotColumnX.Length];
            for (var i = 0; i < SlotColumnX.Length; i++) positions[i] = new WorldPoint(SlotColumnX[i], SlotY);
            return positions;
        }

        public static PackASuitcaseRound Create(int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var trip = (Occasion)(level - DifficultyLadder.MinLevel);
            var packingList = PackingLists[trip];

            var items = new List<SuitcaseItem>();
            foreach (var itemId in packingList) items.Add(new SuitcaseItem { ItemId = itemId, Correct = true });

            var distractorPool = new List<string>();
            foreach (var pair in PackingLists)
            {
                if (pair.Key == trip) continue;
                distractorPool.AddRange(pair.Value);
            }
            Shuffle(distractorPool, rng);
            for (var i = 0; i < DistractorCount; i++)
                items.Add(new SuitcaseItem { ItemId = distractorPool[i], Correct = false });

            Shuffle(items, rng);
            var pitchStart = -(items.Count - 1) / 2f * TrayPitch;
            for (var i = 0; i < items.Count; i++)
                items[i].TrayPosition = new WorldPoint(pitchStart + i * TrayPitch, TrayY);

            return new PackASuitcaseRound
            {
                Trip = trip,
                SlotPositions = SlotPositions(),
                ShelfItems = items.ToArray(),
            };
        }

        private static void Shuffle<T>(List<T> list, Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
