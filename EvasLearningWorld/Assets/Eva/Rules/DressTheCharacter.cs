using System;

namespace EvasLearningWorld.Rules
{
    // Kept for Dress for the Occasion / Pack a Suitcase (Rules/DressForOccasion.cs, Rules/PackASuitcase.cs),
    // which still use their own pre-M5 placeholder catalogue and this same slot shape - only Dress the
    // Character itself was rebuilt onto the new Rules/WardrobeSlot for M5 (see ClothingPiece/
    // DressTheCharacterRoundGenerator below). Not touched by M5 Task 4.
    public enum ClothingSlot { Head, Top, Bottom, Feet }

    public sealed class ClothingPiece
    {
        // Sprite key: "character/<ItemId>" (Rules/Wardrobe.cs's own convention - ItemId already embeds its
        // category, e.g. "top_boy_0", so the sprite path is just "character/" + ItemId, no extra slot
        // prefix). Matches art/character/STYLE.md's v1 naming exactly, even though the art itself is still
        // placeholder - see the class comment below.
        public string ItemId;
        public WardrobeSlot Slot;
        public WorldPoint HomePosition;
        public WorldPoint TrayPosition;
    }

    public sealed class DressTheCharacterRound
    {
        // 4 pieces (Top, Bottom, Shoes, Glasses) normally, or 3 (Dress, Shoes, Glasses) on a Dress round -
        // see the class comment below for why there is no fixed piece count any more.
        public ClothingPiece[] Pieces;
        public float SnapRadius;
        // The gender this round was generated for (always the child's own Progress.Look.Gender - the screen
        // passes it straight through so a boy's session never rolls a Dress round).
        public Gender Gender;
    }

    // Dress the Character (Store, spec 4.3 dressing cluster). Rebuilt for M5 Task 4
    // (docs/superpowers/plans/2026-09-27-m5-character-system.md) to dress the child's own boy/girl character
    // (App/Screens/DressTheCharacterScreen.cs shows a live CharacterRig preview alongside the existing
    // slot/tray drag mechanic) instead of an abstract, gender-less slot row. Still DRAG & DROP, still reusing
    // DragItem/Jigsaw's own "snap when close to its own correct region" mechanic unchanged, still "no single
    // correct answer at low levels - reward is for completing a full outfit" per the original brief:
    // difficulty is purely growing item variety (PoolSizeByLevel), never a harder placement puzzle.
    //
    // What changed from the pre-M5 version: slots are now the real wardrobe slots (Rules/WardrobeSlot, the
    // same enum Task 1's rig/wardrobe contract uses), not a bespoke Head/Top/Bottom/Feet enum - "Head"
    // (hats) had no home in the M5 character model (hats are explicitly deferred - see the design spec's
    // "Deferred out of this milestone") and is dropped. Dress is not a fifth independent slot: per
    // CharacterLook's own exclusivity rule (CharacterLook.SetDress), a round either dresses Top+Bottom+
    // Shoes+Glasses (4 pieces) or Dress+Shoes+Glasses (3 pieces) - never both shapes' slots at once. Boys
    // never roll a Dress round (Dress is girls-only, same rule CharacterLook.Normalize enforces); girls roll
    // between the two shapes each round, for outfit variety across a session's 3 rounds.
    //
    // Item ids are still placeholder - WardrobeCatalog.All (Rules/Wardrobe.cs) is empty until Task 3
    // generates the real v1 wardrobe art. The pools below use the exact id convention that art is expected
    // to land under (art/character/STYLE.md's approved v1 counts: 4 t-shirts/gender, 3 bottoms/gender, 3
    // dresses, 3 shoes/gender, 3 shared glasses), so swapping in real art later is a sprite-file change only,
    // never a code change here.
    public static class DressTheCharacterRoundGenerator
    {
        public const int RoundsPerSession = 3;

        private static readonly string[] TopBoy = { "top_boy_0", "top_boy_1", "top_boy_2", "top_boy_3" };
        private static readonly string[] TopGirl = { "top_girl_0", "top_girl_1", "top_girl_2", "top_girl_3" };
        private static readonly string[] BottomBoy = { "bottom_boy_0", "bottom_boy_1", "bottom_boy_2" };
        private static readonly string[] BottomGirl = { "bottom_girl_0", "bottom_girl_1", "bottom_girl_2" };
        private static readonly string[] DressGirl = { "dress_girl_0", "dress_girl_1", "dress_girl_2" };
        private static readonly string[] ShoesBoy = { "shoes_boy_0", "shoes_boy_1", "shoes_boy_2" };
        private static readonly string[] ShoesGirl = { "shoes_girl_0", "shoes_girl_1", "shoes_girl_2" };
        private static readonly string[] GlassesShared = { "glasses_0", "glasses_1", "glasses_2" };

        // Index i = level (i+1): how many of each slot's own pool is in the draw - clamped to that pool's own
        // length in Draw() below, since pools here are 3-4 items, not a flat 4 like the pre-M5 catalogue.
        private static readonly int[] PoolSizeByLevel = { 1, 2, 2, 3, 3, 4 };

        // The character's own fixed slot row: 4 columns, reused across every round shape. A Dress round uses
        // columns 0 (Dress - the same column Top would otherwise sit in, since the two are mutually
        // exclusive by construction), 2 (Shoes) and 3 (Glasses); column 1 is simply silent that round, the
        // same "some columns unused this round" shape Shopping's own level-doubles-as-mode rounds already use.
        public const float SlotSize = 240f; // EvaUi.MinTap
        public const float SnapRadius = 110f; // see the pre-M5 comment: columns are 260 apart, comfortably over double this
        private static readonly float[] ColumnX = { -390f, -130f, 130f, 390f };
        private const float SlotY = 40f;
        private const float TrayY = -260f;

        public static DressTheCharacterRound Create(int level, Random rng, Gender gender)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var poolIndex = level - DifficultyLadder.MinLevel;
            var wearsDress = gender == Gender.Girl && rng.Next(0, 2) == 0;

            var slots = wearsDress
                ? new[] { WardrobeSlot.Dress, WardrobeSlot.Shoes, WardrobeSlot.Glasses }
                : new[] { WardrobeSlot.Top, WardrobeSlot.Bottom, WardrobeSlot.Shoes, WardrobeSlot.Glasses };
            var columns = wearsDress ? new[] { 0, 2, 3 } : new[] { 0, 1, 2, 3 };

            var trayOrder = new int[slots.Length];
            for (var i = 0; i < slots.Length; i++) trayOrder[i] = i;
            Shuffle(trayOrder, rng);

            var pieces = new ClothingPiece[slots.Length];
            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                var pool = PoolFor(slot, gender);
                var poolSize = Math.Min(PoolSizeByLevel[poolIndex], pool.Length);
                var itemId = pool[rng.Next(0, poolSize)];

                pieces[i] = new ClothingPiece
                {
                    ItemId = itemId,
                    Slot = slot,
                    HomePosition = new WorldPoint(ColumnX[columns[i]], SlotY),
                    TrayPosition = new WorldPoint(ColumnX[columns[trayOrder[i]]], TrayY),
                };
            }

            return new DressTheCharacterRound { Pieces = pieces, SnapRadius = SnapRadius, Gender = gender };
        }

        private static string[] PoolFor(WardrobeSlot slot, Gender gender)
        {
            switch (slot)
            {
                case WardrobeSlot.Top: return gender == Gender.Boy ? TopBoy : TopGirl;
                case WardrobeSlot.Bottom: return gender == Gender.Boy ? BottomBoy : BottomGirl;
                case WardrobeSlot.Dress: return DressGirl;
                case WardrobeSlot.Shoes: return gender == Gender.Boy ? ShoesBoy : ShoesGirl;
                case WardrobeSlot.Glasses: return GlassesShared;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
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
