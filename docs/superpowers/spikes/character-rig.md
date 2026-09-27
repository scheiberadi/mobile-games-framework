# Spike: M5 character data model + rig mechanics (Task 1)

**Gate 1 review document.** Per `docs/superpowers/plans/2026-09-27-m5-character-system.md`, nothing past
this point (Task 2's style lock, any real wardrobe art) starts until the user approves what's below. No
Unity build or on-device pass has happened in this container - same caveat every M4 building carried
(`docs/kids-games/m4-handover.md`) - so "proven" below means proven structurally (hierarchy, sibling order,
active/inactive state) via a rewritten `RigTests.cs`, not seen on a screen.

## Question

Does the expanded `CharacterLook`/rig shape (Gender, Face, Skin, HairStyle, HairColor, EyeColor, Top/
Bottom-or-Dress, Shoes, Glasses) hold together mechanically - specifically the draw-order/occlusion cases
that break a naive layering model - and can an existing save load into it safely, before any real wardrobe
art is generated?

## Verdict

PASS, structurally. Every occlusion case the plan called out resolves with a plain child-order/nesting
rule (no new rig topology needed); the four data-model questions below are all resolved; save migration is
implemented and has its required fixture test. Genuinely unverified: anything about how this actually
*looks* (proportions, whether the placeholder rectangles are legible as "clothing" versus "a rectangle"),
since that needs a screenshot or a real device, neither available here.

## Resolved data-model questions

- **Hair/eye colour counts**: 6 each (`CharacterLook.HairColorCount` / `EyeColorCount`), matching
  `Palette.HairColor` / `Palette.EyeColor`'s actual array lengths. Landed in the middle of the spec's
  proposed 6-8 range - enough variety for a creator, not so many rows that Task 3's real picker UI (still
  unbuilt) gets harder to lay out.
- **Eyes: baked into Face vs. a separate movable layer**: a separate `EyeIris` layer, but nested *inside*
  the `Head` GameObject (not a `Torso`-level sibling) so it inherits the head's own bob/tilt for free and
  never needs its own position math relative to a moving head. "Baked into Face art" was the spec's other
  option; a separate tintable layer was chosen because Face art doesn't exist yet either way (Task 2/3),
  and a separate layer is what actually lets `EyeColor` tint independently of `Face`.
- **Bottom (Pants/Skirt) vs. Dress exclusivity**: enforced at the data level, in both directions -
  `CharacterLook.SetDress`/`SetTop`/`SetBottom` clear whichever of the other two would conflict, and
  `Normalize()` re-asserts the same rule defensively (Dress wins over Top/Bottom; Boy clears Dress) for any
  `CharacterLook` assembled without going through the setters - JsonUtility's field-by-field deserialization
  is exactly this case, which is why `SaveStore.Load()` calls `Normalize()` on every load.
- **Dress is a distinct category, never a third Bottom option**: modelled as its own nullable field
  (`CharacterLook.Dress`), its own `WardrobeSlot.Dress` value, and its own rig layer/GameObject
  (`Torso/Dress`) - never folded into `Bottom`'s own slot or catalogue. Confirmed in the rig too: `Dress`
  suppresses rendering of *both* `Top` and `Bottom` (see `CharacterRig.ApplyLook`), it never coexists with
  either being visible.

## Final slot list

`CharacterLook` (`Rules/Character.cs`): `Gender`, `Face` (int, 0-9 per gender - `FaceCount`), `Skin` (int,
`Palette.Skin`), `HairStyle` (int, placeholder shape table today), `HairColor` (int, `Palette.HairColor`),
`EyeColor` (int, `Palette.EyeColor`), `Top`/`Bottom`/`Dress`/`Shoes`/`Glasses` (nullable wardrobe item id
strings - null is a valid, default "nothing worn" choice everywhere, including Glasses).

`WardrobeItem` (`Rules/Wardrobe.cs`): the metadata contract per spec point 12 - `Id` and `Gender`
(`WardrobeGender`: BoyOnly/GirlOnly/Both) are the only two fields ever set per item; draw order and rig
attachment are a fixed default per `WardrobeSlot` (Top/Bottom/Dress/Shoes/Glasses), resolved once below, not
per-item data. `AttachOffset` (nullable `WorldPoint`) exists for the rare item that genuinely needs its own
override - none do yet, since `WardrobeCatalog.All` is still empty (Task 2/3 populate it).

## Draw-order/layering convention, proven against the occlusion cases

Everything lives under the player rig's existing `Torso` (for clothing/hair/eyes/glasses) or each `Leg`
(for shoes) - no new rig topology, just more siblings/children of parts that already existed. Unity uGUI
draws children in sibling order, later = in front (the same convention `RigFactory`'s class comment already
documented for Eva's own ear/eye layering), so ordering is entirely a matter of *when* each part is created
in `RigFactory.Build()` (or which existing part it's nested inside). Per the plan's own required cases:

1. **Hair is never a single "always behind" or "always in front" layer.** `HairBack` is created *before*
   `ArmL`/`ArmR`/`Head` (so it draws behind the whole head/face) and is sized 1.25x the head, so it peeks
   out past the face silhouette. `HairFront` is a horizontal band *nested inside* `Head` itself, added after
   `EyeIris`, so it draws in front of the face and the eyes and its own coverage (how far down over the
   forehead it reaches) is resized per `HairStyle` (`RigFactory.HairShapeFor`) - three placeholder shapes
   (12% / 35% / 60% front coverage) prove the front piece is a real, variable-sized layer, not a fixed strip.
2. **A Top/Dress placeholder shaped to overlap where the arms attach.** `Top` and `Dress` are both created
   *before* `ArmL`/`ArmR` (clothing sits behind the arms - they swing in front of a shirt, not through it),
   and both are sized past the torso's own width (`TorsoWidth + ShoulderX * 1.2`), which - since `ShoulderX`
   is where the arms actually attach - guarantees the overlap the spike needs to prove, rather than just
   sitting flush behind the torso's own bounds.
3. **Shoes shaped to overlap the bottom of the leg art, not just sit below it.** `Shoe` is a band nested
   *inside* each `Leg`'s own Image GameObject (not a `Torso` sibling), covering the bottom 35% of the leg's
   own rect - a child always draws in front of its own parent's Image, so this holds regardless of where the
   band's vertical split actually falls.
4. **Glasses over the face without fighting hair drawn in front of it.** `Glasses` is a `Torso`-level
   sibling of `Head`, added *last* among the head-region parts - since sibling order determines draw order
   for an entire subtree, `Glasses` draws in front of everything nested inside `Head` (`Face`'s own image,
   `EyeIris`, *and* `HairFront`), regardless of which hairstyle or coverage is active.

`RigTests.PlayerHierarchyMatchesTheSpecAndHasNoTail` asserts all four via `GetSiblingIndex()` comparisons
(structural proof, not a screenshot) - see that test for the exact assertions. `ApplyLookTintsSkinPartsAnd-
HairAndTogglesWornWardrobeSlots` and `DressReplacesTopAndBottomInsteadOfLayeringWithThem` cover the tinting
and Dress-suppresses-Top/Bottom behaviour respectively.

**Load-bearing constraint carried over unchanged**: `EvaRigAssets`-generated animation clips
(`Resources/Anim/*.anim`) bind curves to the exact paths `Torso`, `Torso/Head`, `Torso/ArmL`, `Torso/ArmR`.
`Transform.Find` resolves by name, not sibling index, so inserting new siblings around these never breaks
the bindings - confirmed by `EveryGeneratedClipBindsOnlyToPathsThatExistUnderAFreshPlayerRig`, unchanged and
still passing structurally. `Head`, `ArmL`, `ArmR` therefore keep those exact names permanently, even though
`Head`'s own Image now shows Face art and it has grown `EyeIris`/`HairFront` children.

## Category-default metadata contract

Decided in `Rules/Wardrobe.cs` per spec point 12: every `WardrobeItem` sets `Id` and `Gender` itself; draw
order and rig attachment are **not** per-item - every item in a `WardrobeSlot` inherits that slot's one
fixed default from `RigFactory` (resolved above), and `AttachOffset` is the sole per-item override field,
used only when an item's own shape genuinely doesn't fit its slot's default (an unusually tall boot, an
asymmetric hairstyle - none exist yet). Whether an item participates in the Dress/Bottom exclusivity rule is
implied by its `Slot`, never a value set per item. `WardrobeCatalog.All` is intentionally empty until Task
2/3 lock the v1 style and asset list - this spike proves the *shape* the contract and rig need to support,
not real content.

## Save migration: Invalidate, not Migrate

`PlayerProgress.Version` bumped to 2 (the class's own default, so a brand-new save is never mistaken for an
old one) and `SaveStore.Load()` now actually branches on it - previously the field existed but nothing read
it. On `Version < 2`: `Look` is reset to a fresh `CharacterLook` and `HasCharacter` is forced back to
`false`, so the child re-runs Creator instead of continuing with a silently-wrong character; nothing else in
the save (coins, house, every difficulty ladder) is touched.

**Why Invalidate over Migrate**: the old model's `Shirt` was a colour *tint* over a generic torso shape;
`Top` is now a specific *illustrated item id* - there is no faithful mapping from "which of 5 tints was
picked" to "which of Task 3's real wardrobe items to assign," and guessing (e.g. `Shirt % NewTopCount`)
would produce a migrated-but-arbitrary outfit that looks like a real choice when it isn't. `Skin` is the one
field that *could* carry across losslessly (same 5-swatch dimension both before and after), but resetting it
alongside everything else was judged worth the consistency of "one clear rule, no partial migration" over
preserving one field out of six. The spec's own text names this exact tradeoff and calls Invalidate
"acceptable and likely simpler given these are development saves, not live user data" - true here: no
players exist yet, so there is no real save to protect.

**Required test**: `SaveStoreTests.AnOldHeadSkinShirtSaveIsInvalidatedNotSilentlyMigrated` feeds `Load()` a
hard-coded JSON fixture shaped like a genuine M1-era save (`"Version":1`, the old
`{"Head":2,"Skin":3,"Shirt":1}` `Look` shape, `"HasCharacter":true`) and asserts the specific invalidated
state (`Version` bumped to 2, `HasCharacter` false, `Look` at its fresh defaults) rather than merely "it
doesn't throw" - and that `Coins` (present in the same fixture) survives untouched, proving the migration is
scoped to `Look`/`HasCharacter` alone.

## What didn't hold together / had to change

Nothing needed a fundamentally different rig topology - every occlusion case resolved with ordering/nesting
alone. Two adjustments worth flagging as real (if small) deviations from a literal reading of the plan:

- **`CreatorScreen` was touched, not left alone**, because `CharacterLook`'s shape changed out from under
  it (`Head`/`Shirt` no longer exist) - leaving it untouched would mean the project doesn't compile at all,
  not just that Task 3's rebuild is deferred. Per the plan's own "enough of a harness... is sufficient," the
  change is deliberately minimal: identical layout/positions/button count as before, Gender fixed to Boy (no
  picker), Face reuses the 4 legacy placeholder sprites, and the third row (was a Shirt tint) now picks one
  of 5 placeholder Top item ids that all render as the same flat box today. See the class comment at the top
  of `CreatorScreen.cs` for the full reasoning. Task 3 is still where this screen actually gets rebuilt.
- **`Palette.Shirt` is retired**, not kept alongside the new model, since Top is no longer tint-based;
  `CreatorScreen` keeps a small local `TopSwatchPreview` array (the same 5 colours) purely for its own
  button-icon variety, unrelated to what the rig actually renders for a placeholder Top.

## Open items for Gate 1

- Confirm Invalidate (not Migrate) is acceptable - the spec flagged this as the more likely choice but left
  it as this task's decision to make and document, not a foregone conclusion.
- Confirm 6 hair colours / 6 eye colours as the working count (Task 2 can still change this before real art
  is generated against it).
- Confirm the CreatorScreen interim state (Gender fixed to Boy, Face capped at 4 legacy sprites, Top as
  unlabelled placeholder swatches) is acceptable to ship as-is until Task 3, rather than needing its own
  smaller fix first.
- Everything visual (proportions, whether placeholder rectangles read as "hair"/"clothing"/"shoes" at all)
  is unverified - first real signal comes from Adrian's own machine (Unity edit-mode run, then a build).
