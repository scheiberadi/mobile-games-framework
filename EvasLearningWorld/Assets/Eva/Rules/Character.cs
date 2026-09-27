using System;

namespace EvasLearningWorld.Rules
{
    // Boy/Girl replaces the old animal-head idea (docs/superpowers/specs/2026-09-27-character-system-design.md,
    // decision 1) - no third value, no animal heads.
    public enum Gender { Boy, Girl }

    // The player's whole visual appearance, saved as PlayerProgress.Look. Grew from three tint-index fields
    // (Head/Skin/Shirt) to this fuller model for M5 (docs/superpowers/plans/2026-09-27-m5-character-system.md,
    // Task 1) - see docs/superpowers/spikes/character-rig.md for how the rig layers these.
    //
    // Two kinds of field: Face/Skin/HairStyle/HairColor/EyeColor are small numbered choices, same shape Skin
    // already had (an index into a fixed palette or a small placeholder-shape table); Top/Bottom/Dress/Shoes/
    // Glasses are nullable wardrobe item ids (WardrobeItem.Id, see Rules/Wardrobe.cs) - null means "nothing worn
    // in that slot", a valid default everywhere including Glasses ("none" is a real choice, not a missing one).
    [Serializable]
    public sealed class CharacterLook
    {
        public const int FaceCount = 10;      // 10 face types per gender (spec decision 2)
        public const int ColorCount = 5;       // Palette.Skin's own swatch count, kept from the original model
        public const int HairColorCount = 6;
        public const int EyeColorCount = 6;

        public Gender Gender;
        public int Face;
        public int Skin;
        public int HairStyle;
        public int HairColor;
        public int EyeColor;
        public string Top;
        public string Bottom;
        public string Dress;
        public string Shoes;
        public string Glasses;

        // Dress is never a third Bottom option - it is a distinct one-piece garment that REPLACES both Top and
        // Bottom's rendering at once (spec's data-model section, said everywhere on purpose so it never gets
        // flattened into "one more bottom choice"). These setters are how UI code (CreatorScreen, the Task 4
        // Dress the Character rebuild) is meant to assign the three fields, so the exclusivity holds by
        // construction; see Normalize() below for the belt-and-braces case JsonUtility's field-by-field
        // deserialization bypasses these entirely.
        public void SetDress(string itemId)
        {
            Dress = itemId;
            if (itemId != null) { Top = null; Bottom = null; }
        }

        public void SetTop(string itemId) { Top = itemId; if (itemId != null) Dress = null; }
        public void SetBottom(string itemId) { Bottom = itemId; if (itemId != null) Dress = null; }

        // Defensive normalisation for combinations that arrive already-assembled (a loaded save, a test
        // fixture) rather than built up through the setters above: Dress wins over Top/Bottom, and Dress is
        // girls-only (spec's "Dress... girls only", an assumption confirmed at Task 1). SaveStore.Load() calls
        // this on every load so an invalid combination can never be observed by the rest of the app, no matter
        // how it was produced.
        public void Normalize()
        {
            if (Dress != null) { Top = null; Bottom = null; }
            if (Gender == Gender.Boy) Dress = null;
        }
    }
}
