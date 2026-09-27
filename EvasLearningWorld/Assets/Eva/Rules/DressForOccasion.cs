using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    // The brief's own 6 occasions - one per DifficultyLadder level, the same "level doubles as content" shape
    // Shopping already established (see ShoppingRoundGenerator's class comment), in the brief's own listed order.
    public enum Occasion { School, Beach, Winter, Birthday, Sports, Camping }

    public sealed class DressForOccasionItem
    {
        // Sprite key: "dressup/<ItemId>" (Dress the Character's own sprite convention).
        public string ItemId;
        public ClothingSlot Slot;
        public bool Correct; // true for one of this occasion's own 3 items; false for a distractor from another occasion
        public WorldPoint TrayPosition;
    }

    public sealed class DressForOccasionRound
    {
        public Occasion Occasion;

        // Always Top/Bottom/Feet, in that order - see the class comment below for why Head is left out here.
        public Dictionary<ClothingSlot, WorldPoint> SlotHomePosition;

        // The shelf: 3 correct items (one per slot) plus 2 distractors from other occasions, shuffled together.
        public DressForOccasionItem[] ShelfItems;
    }

    // Dress for the Occasion (Store, dressing cluster's 2nd game): MATCH/DRAG & DROP - Eva names an occasion,
    // the child drags the matching items onto the character from a shelf that also holds a couple of
    // wrong-occasion distractors (per the brief: "child drags the matching items"), unlike Dress the
    // Character's shelf, which never has a wrong choice at all. Reuses the same DragItem "snap near its own
    // correct region" mechanic (read DressTheCharacterRoundGenerator/DressTheCharacterScreen first) - a
    // distractor simply has no correct region to snap to; dragging one onto the character is this game's
    // "mistake" instead, same two-step Retry/Hint/Demonstrate ladder as every other game.
    //
    // Only Top/Bottom/Feet are used, not all 4 ClothingSlot values - the brief's occasion outfits (a swimsuit,
    // a snowsuit, a sports jersey) are naturally about the body and feet, and skipping Head keeps this game's
    // shelf a manageable 5 items (3 correct + 2 distractors) at the usual 240-unit tap size; a themed hat could
    // be added later without changing this shape.
    //
    // Level doubles as which Occasion is being dressed for (see the Occasion enum's own comment) - so unlike
    // Dress the Character, difficulty here progresses through the brief's own occasion list as the child
    // succeeds, not through a growing item-variety pool.
    public static class DressForOccasionRoundGenerator
    {
        public const int RoundsPerSession = 3; // fewer, longer rounds - same reasoning as Jigsaw/Dress the Character.
        private const int DistractorCount = 2;

        private static readonly ClothingSlot[] Slots = { ClothingSlot.Top, ClothingSlot.Bottom, ClothingSlot.Feet };

        // Placeholder outfit catalogue, one 3-item outfit per occasion - flagged for a real content/art pass
        // same as every other catalogue this session.
        private static readonly Dictionary<Occasion, Dictionary<ClothingSlot, string>> Outfits = new Dictionary<Occasion, Dictionary<ClothingSlot, string>>
        {
            { Occasion.School, new Dictionary<ClothingSlot, string> { { ClothingSlot.Top, "school_shirt" }, { ClothingSlot.Bottom, "school_pants" }, { ClothingSlot.Feet, "school_shoes" } } },
            { Occasion.Beach, new Dictionary<ClothingSlot, string> { { ClothingSlot.Top, "swimsuit_top" }, { ClothingSlot.Bottom, "swim_shorts" }, { ClothingSlot.Feet, "sandals" } } },
            { Occasion.Winter, new Dictionary<ClothingSlot, string> { { ClothingSlot.Top, "sweater" }, { ClothingSlot.Bottom, "snow_pants" }, { ClothingSlot.Feet, "boots" } } },
            { Occasion.Birthday, new Dictionary<ClothingSlot, string> { { ClothingSlot.Top, "party_shirt" }, { ClothingSlot.Bottom, "party_pants" }, { ClothingSlot.Feet, "party_shoes" } } },
            { Occasion.Sports, new Dictionary<ClothingSlot, string> { { ClothingSlot.Top, "jersey" }, { ClothingSlot.Bottom, "shorts" }, { ClothingSlot.Feet, "cleats" } } },
            { Occasion.Camping, new Dictionary<ClothingSlot, string> { { ClothingSlot.Top, "flannel_shirt" }, { ClothingSlot.Bottom, "cargo_pants" }, { ClothingSlot.Feet, "hiking_boots" } } },
        };

        // Slot row (character): full 240-unit tap targets, same reasoning as Dress the Character.
        public const float SlotSize = 240f; // EvaUi.MinTap
        public const float SnapRadius = 110f;
        private static readonly float[] SlotColumnX = { -260f, 0f, 260f };
        private const float SlotY = 40f;

        // Tray row (shelf): 5 items, a wider row than Dress the Character's 4 - 250-unit pitch (240 + 10
        // clearance, tighter than Dress the Character's 20 purely to keep the row from running past Eva's own
        // position on the right of the screen; still comfortably clear of every item's neighbours).
        private const float TrayPitch = 250f;
        private const float TrayY = -260f;

        // A slot's own fixed body position, independent of any round - same purpose as
        // DressTheCharacterRoundGenerator.HomePositionFor, used by the screen to draw the 3 slot outlines once.
        public static WorldPoint HomePositionFor(ClothingSlot slot)
        {
            var index = Array.IndexOf(Slots, slot);
            return new WorldPoint(SlotColumnX[index], SlotY);
        }

        public static DressForOccasionRound Create(int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var occasion = (Occasion)(level - DifficultyLadder.MinLevel);
            var outfit = Outfits[occasion];

            var items = new List<DressForOccasionItem>();
            foreach (var slot in Slots) items.Add(new DressForOccasionItem { ItemId = outfit[slot], Slot = slot, Correct = true });

            var distractorPool = new List<(string ItemId, ClothingSlot Slot)>();
            foreach (var pair in Outfits)
            {
                if (pair.Key == occasion) continue;
                foreach (var slot in Slots) distractorPool.Add((pair.Value[slot], slot));
            }
            Shuffle(distractorPool, rng);
            for (var i = 0; i < DistractorCount; i++)
                items.Add(new DressForOccasionItem { ItemId = distractorPool[i].ItemId, Slot = distractorPool[i].Slot, Correct = false });

            Shuffle(items, rng);
            var pitchStart = -(items.Count - 1) / 2f * TrayPitch;
            for (var i = 0; i < items.Count; i++)
                items[i].TrayPosition = new WorldPoint(pitchStart + i * TrayPitch, TrayY);

            var slotHomePosition = new Dictionary<ClothingSlot, WorldPoint>();
            for (var i = 0; i < Slots.Length; i++) slotHomePosition[Slots[i]] = new WorldPoint(SlotColumnX[i], SlotY);

            return new DressForOccasionRound
            {
                Occasion = occasion,
                SlotHomePosition = slotHomePosition,
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
