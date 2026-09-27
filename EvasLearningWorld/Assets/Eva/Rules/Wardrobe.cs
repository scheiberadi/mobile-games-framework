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

    // Empty until Task 2 locks the visual style + v1 asset list and Task 3 generates the real items (see
    // docs/superpowers/plans/2026-09-27-m5-character-system.md). RigFactory does not read this catalogue yet -
    // today it only checks whether a CharacterLook slot is null - so an empty catalogue is not a missing
    // feature, just an honestly-empty one.
    public static class WardrobeCatalog
    {
        public static readonly WardrobeItem[] All = new WardrobeItem[0];
    }
}
