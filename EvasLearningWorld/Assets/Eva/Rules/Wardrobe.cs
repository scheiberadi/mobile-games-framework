namespace EvasLearningWorld.Rules
{
    public enum WardrobeSlot { Top, Bottom, Dress, Shoes, Glasses }

    // Which gender(s) an item is valid for. "Both" is expected mainly for Glasses (spec: "glasses shapes
    // aren't inherently gendered the way clothing is") but is not restricted to that slot.
    public enum WardrobeGender { BoyOnly, GirlOnly, Both }

    // One wearable item's catalogue entry (spec's "wardrobe item metadata contract", decided once here and
    // applied uniformly - not a generic wardrobe engine, just enough that item #47 is "one catalogue entry
    // plus its art," never "one entry and then debug why it renders in the wrong place").
    //
    // Only Id and Gender are ever set per item - both genuinely vary item to item. Draw order and rig
    // attachment are NOT per-item fields: every item in a Slot shares that slot's one fixed default (see
    // RigFactory, resolved once against Task 1's occlusion spike: Top/Bottom sit behind the arms, Dress
    // replaces both, Shoes overlap the foot of the leg art, Glasses sit in front of Face and its hair-front
    // fringe). AttachOffset exists only for the rare item that genuinely needs its own override (an unusually
    // tall boot, an asymmetric hairstyle) - null means "use the slot's default", the expected shape for almost
    // every entry. Whether an item participates in the Dress/Bottom exclusivity rule is implied by its Slot
    // (true for every Bottom and every Dress item, not applicable to anything else), never a value set per item.
    public sealed class WardrobeItem
    {
        public string Id;
        public WardrobeSlot Slot;
        public WardrobeGender Gender;
        public WorldPoint? AttachOffset;
    }

    // The v1 catalogue (art/character/STYLE.md's approved list: 4 t-shirts/gender, 3 bottoms/gender, 3 dresses,
    // 3 shoes/gender, 3 shared glasses). Ids follow the sprite-key convention "character/<Id>", so real art
    // landing under those names needs no code change. This is the single source of truth for what each slot
    // offers: CharacterCreator (CreatorScreen) and DressTheCharacterRoundGenerator both read it.
    // RigFactory still only null-checks the look's slots and does not read it.
    public static class WardrobeCatalog
    {
        public static readonly WardrobeItem[] All = Build();

        private static WardrobeItem[] Build()
        {
            var items = new System.Collections.Generic.List<WardrobeItem>();
            Add(items, "top_boy_", 4, WardrobeSlot.Top, WardrobeGender.BoyOnly);
            Add(items, "top_girl_", 4, WardrobeSlot.Top, WardrobeGender.GirlOnly);
            Add(items, "bottom_boy_", 3, WardrobeSlot.Bottom, WardrobeGender.BoyOnly);
            Add(items, "bottom_girl_", 3, WardrobeSlot.Bottom, WardrobeGender.GirlOnly);
            Add(items, "dress_girl_", 3, WardrobeSlot.Dress, WardrobeGender.GirlOnly);
            Add(items, "shoes_boy_", 3, WardrobeSlot.Shoes, WardrobeGender.BoyOnly);
            Add(items, "shoes_girl_", 3, WardrobeSlot.Shoes, WardrobeGender.GirlOnly);
            Add(items, "glasses_", 3, WardrobeSlot.Glasses, WardrobeGender.Both);
            return items.ToArray();
        }

        private static void Add(System.Collections.Generic.List<WardrobeItem> items, string prefix, int count, WardrobeSlot slot, WardrobeGender gender)
        {
            for (var i = 0; i < count; i++)
                items.Add(new WardrobeItem { Id = prefix + i, Slot = slot, Gender = gender });
        }

        // The items a child of this gender can pick in a slot, in catalogue order (stable: index 0 is the
        // slot's default choice). Dress is empty for boys.
        public static string[] IdsFor(WardrobeSlot slot, Gender gender)
        {
            var ids = new System.Collections.Generic.List<string>();
            foreach (var item in All)
            {
                if (item.Slot != slot) continue;
                var ok = item.Gender == WardrobeGender.Both
                    || (item.Gender == WardrobeGender.BoyOnly) == (gender == Gender.Boy);
                if (ok) ids.Add(item.Id);
            }
            return ids.ToArray();
        }
    }
}
