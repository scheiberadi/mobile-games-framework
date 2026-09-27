using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // The four body-part slots a clothing item can go in. Fixed, unlike everything else about a round.
    public enum ClothingSlot { Head, Top, Bottom, Feet }

    public sealed class ClothingPiece
    {
        // Sprite key: "clothes/<ItemId>". Placeholder art, same as every other catalogue this session.
        public string ItemId;
        public ClothingSlot Slot;
        public WorldPoint HomePosition;
        public WorldPoint TrayPosition;
    }

    public sealed class DressTheCharacterRound
    {
        // Always exactly 4 pieces, one per ClothingSlot value - see the class comment below for why there is
        // no smaller/larger count to progress through.
        public ClothingPiece[] Pieces;
        public float SnapRadius;
    }

    // Dress the Character (Store, spec 4.3 dressing cluster): DRAG & DROP, reusing DragItem/Jigsaw's own "snap
    // when close to its own correct region" mechanic unchanged (read JigsawRoundGenerator/JigsawScreen first).
    // The one real difference from Jigsaw: "no single correct answer at low levels (any combination is fine,
    // reward is for completing a full outfit)" per the brief - so unlike Jigsaw, a piece's HomePosition is
    // simply its ClothingSlot's fixed body position, and *which* item variant fills that slot is drawn at
    // random from the level's own pool rather than one specific piece having one specific home; the child can
    // never be "wrong" about which item goes where beyond matching its own slot type, so difficulty here comes
    // only from the growing pool of item variety (matching every other catalogue-progression game this
    // session), not from a harder placement puzzle. A themed "does this outfit fit the occasion" goal is a
    // separate, harder skill and is Dress for the Occasion's job, not this game's - see that generator instead.
    // Every round is a complete 4-item outfit (Head, Top, Bottom, Feet), so there is no piece-count ladder to
    // progress through the way Jigsaw's board size does.
    public static class DressTheCharacterRoundGenerator
    {
        public const int RoundsPerSession = 3; // fewer, longer rounds than a tap game - same reasoning as Jigsaw.

        private static readonly Dictionary<ClothingSlot, string[]> Catalogue = new Dictionary<ClothingSlot, string[]>
        {
            { ClothingSlot.Head, new[] { "cap", "hat", "beanie", "sunhat" } },
            { ClothingSlot.Top, new[] { "shirt", "jacket", "sweater", "tshirt" } },
            { ClothingSlot.Bottom, new[] { "pants", "shorts", "skirt", "jeans" } },
            { ClothingSlot.Feet, new[] { "shoes", "sandals", "boots", "sneakers" } },
        };

        // Index i = level (i + 1): how many of each slot's 4 catalogue items are in the draw pool. Growing
        // variety is the only progression here (see class comment) - never fewer than 1 so a round always has
        // something to draw, never more than the catalogue itself (4).
        private static readonly int[] PoolSizeByLevel = { 1, 2, 2, 3, 4, 4 };

        // The character's own fixed slot row - a simple row of 4 labelled silhouette slots (Head, Top, Bottom,
        // Feet, left to right) rather than a single stacked dress-up-doll body: at a full 240-unit tap size
        // each, 4 slots stacked vertically would either crowd the Hud's Home button or run off the real
        // on-device frame (see StoreScreen's own layout comments for that same 1440-wide/900-tall real-frame
        // math), while a horizontal row fits cleanly in the safe band below Home (y <= 180) with room to spare -
        // and unlike Jigsaw's up-to-25 pieces, 4 items need no exemption from the usual 240-unit tap floor, so
        // none is used here. Not Eva's own animated CharacterRig, which has no clothing-layer concept and isn't
        // meant to grow one for a single game.
        public const float SlotSize = 240f; // EvaUi.MinTap
        public const float SnapRadius = 110f; // columns are 260 apart (240 + 20 clearance); comfortably under half that, so no two adjacent snap zones can ever touch
        private static readonly float[] ColumnX = { -390f, -130f, 130f, 390f };
        private const float SlotY = 40f;
        private const float TrayY = -260f;
        private static readonly ClothingSlot[] SlotOrder = { ClothingSlot.Head, ClothingSlot.Top, ClothingSlot.Bottom, ClothingSlot.Feet };
        private static readonly Dictionary<ClothingSlot, WorldPoint> SlotHomePosition = BuildSlotHomePositions();

        private static Dictionary<ClothingSlot, WorldPoint> BuildSlotHomePositions()
        {
            var result = new Dictionary<ClothingSlot, WorldPoint>();
            for (var i = 0; i < SlotOrder.Length; i++) result[SlotOrder[i]] = new WorldPoint(ColumnX[i], SlotY);
            return result;
        }

        // A slot's own fixed body position, independent of any round - used by the screen to draw the 4 slot
        // outlines once, up front, rather than only through a generated round.
        public static WorldPoint HomePositionFor(ClothingSlot slot) => SlotHomePosition[slot];

        public static DressTheCharacterRound Create(int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var poolSize = PoolSizeByLevel[level - DifficultyLadder.MinLevel];

            var trayOrder = new int[4];
            for (var i = 0; i < 4; i++) trayOrder[i] = i;
            Shuffle(trayOrder, rng);

            var pieces = new ClothingPiece[4];
            for (var i = 0; i < 4; i++)
            {
                var slot = SlotOrder[i];
                var pool = Catalogue[slot];
                var itemId = pool[rng.Next(0, Math.Min(poolSize, pool.Length))];
                var trayIndex = trayOrder[i];

                pieces[i] = new ClothingPiece
                {
                    ItemId = itemId,
                    Slot = slot,
                    HomePosition = SlotHomePosition[slot],
                    TrayPosition = new WorldPoint(ColumnX[trayIndex], TrayY),
                };
            }

            return new DressTheCharacterRound { Pieces = pieces, SnapRadius = SnapRadius };
        }

        private static void Shuffle(int[] values, Random rng)
        {
            for (var i = values.Length - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }
    }
}
