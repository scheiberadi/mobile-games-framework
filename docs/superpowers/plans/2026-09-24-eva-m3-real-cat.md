# Eva's Learning World, M3 Plan: a real cat Eva, and child-experience fixes

**For review only. Nothing in this plan is executed until the user approves it.** M2 is closed and merged into `eva-m1` at `3f97596`; it is not reopened unless a regression appears during M3. Inputs: `docs/superpowers/spikes/M2-summary.md` (M3 inputs section) and the user-confirmed input list.

## Goal

Establish Eva's final visual identity as a **real cat**, then fix the counting-screen issues a 4-5-year-old actually meets (pointer hand not pointing at the target, too few answer choices at higher levels), then expand the catalog. The real-cat work is the clear priority and is done first; everything else follows it.

## Eva requirement (fixed, not to be softened, reinterpreted or stylized)

Eva must be:

- a real-looking, four-legged cat;
- **black**;
- **fluffy, approximately midway between a shorthair and a Persian**, with visible fluff around the cheeks and chest;
- **bright-eyed** (amber or green) so the face reads against the black coat;
- **dwarf-cat proportions** with the same **tiny bushy bunny-like bobtail** as the user's own cat (only two tail vertebrae, from birth);
- no humanoid anatomy, no humanoid gestures, **no tail sway**.

The goal is a real cat, not a cartoon cat made suitable for children. The children's-game requirement applies to presentation and readability (size, contrast, clarity of motion), never to reducing Eva's realism. No cartoon exaggeration is added to make her more child-friendly.

**Realism is practical, not photographic.** The target is a convincing illustrated representation of a real cat, with believable feline anatomy, proportions, fur volume and movement, inside the existing SVG workflow (`art/eva/**/*.svg` to `tools/build-eva-art.sh` to `Resources/Art/**`) and the simplest viable animation approach. Not allowed: skeletal or skin-deformation systems, a generic character-animation framework, a new rendering pipeline, fur simulation, unnecessary animation states.

Eva is drawn by Claude as SVG layers (user decision). No photos are needed; the user may optionally supply a reference image if the drawing does not read as real.

## Global constraints (unchanged from M1/M2)

- No reading anywhere; voice-first. No ads, IAP, accounts, analytics, server, runtime AI. Deterministic rewards only (no chests, loot boxes, random drops).
- Landscape, 1440x900 design frame (`EvaLayout`); large forgiving tap targets, >= `EvaUi.MinTap` (240) except counting slots (floor 100).
- Only the on-device user taps through gameplay; never scripted taps, never `KEYCODE_BACK` (gesture-nav phone).
- No generic frameworks, no future-proofing; a spike is small, disposable, and answers one question.
- Design assets: individual clean layers plus native UI elements, never a processed flattened composite.
- Tests after each task: `bash tools/run-editmode-tests.sh ".../EvasLearningWorld"` (baseline **Eva 227**, root 187). Never `-quit`, never edit sources while Unity runs, bounded waits.
- Do not touch or commit the unrelated Sudoku change in `ProjectSettings/AndroidResolverDependencies.xml`.

## Decisions already made by the user

- Creator and the child's own character stay humanoid in M3. Only Eva changes (`RigFactory.CreatePlayer`, `CreatorScreen`, `char_*` art untouched; `ApplyLook` keeps working for the player).
- Eva's greeting and cheer are bounces. The Map tap `Wave` becomes a bounce.
- Answer choices fixed per level: **5 at levels 3-4, 6 at levels 5-6**.
- Catalog and House-slot change is in M3 (simple, see Task 7).
- Product gate is the user's on-device play of the whole loop.

## Execution order

1. Task 1: real-cat spike
2. **User review and approval of the spike (hard gate)**
3. Task 2: real-cat Eva in the game
4. Task 3: hand-pointer alignment
5. Task 4: more answer choices
6. Task 5: test gaps
7. Task 6: placeholder art replacement, only screens the user selects
8. Task 7: catalog and House-slot model change
9. Task 8: acceptance

Tasks 3-5 are technically independent of the cat, but they are deliberately not prioritized over establishing Eva's identity. Nothing else starts before the spike is approved.

## Tasks

### Task 1: Real-cat spike (small, disposable, decision only)

**Question:** can we make this specific cat (black, fluffy, dwarf, bobtail) look convincingly real and appealing as an animated game character at the actual gameplay sizes?

Approach to try first (**pose set plus small moving layers**): a few clean SVG-drawn poses (sit, sit with mouth open, stand, happy) with ears, mouth/jaw and the bobtail as separate layers. Body motion (breathing, squash-and-hop, head tilt) comes from the code-driven transforms and Animator we already have; no limb skinning. If the first approach looks poor, try the alternative or a mix (a fuller cutout quadruped with separate legs, head, body) rather than polishing a fundamentally wrong rig.

Build only enough to judge: the rendered cat and enough animation to see it move. Do not make the spike production-clean.

Judge **appearance**:
- Reads as a real, four-legged cat with believable anatomy and dwarf proportions.
- Black coat with visible fluff (cheeks, chest, midway shorthair/Persian); bright eyes; bobtail correct.

Judge **feline movement**, not just appearance (a realistic cat that moves like a person fails):
- idle breathing and body movement;
- ear movement;
- subtle head movement;
- talking without humanoid mouth or face behavior (a cat-appropriate cue such as a small jaw, head or body movement in time with voice);
- cheer as a cat-appropriate bounce or hop;
- greeting as a bounce;
- bobtail essentially static or barely moving;
- no vestigial arms, legs or waving from the old rig.

Judge **black-cat readability**, in this order, without turning her into a glowing or heavily outlined character:
1. background and value separation (adjust the background behind her where needed);
2. silhouette readability;
3. subtle fur highlights or rim light only where needed.

Check her at the **actual gameplay sizes and backgrounds**: Map (`EvaHeight`), School/Count (mirrored, beside the hand and speech bubble), House and Store; not only in a close-up. Also check existing interplays: `BigCheer` scale punch on the rig root still works, and the pointer hand is a separate UI element and is unaffected.

**Output:** the rendered cat on the phone; `docs/superpowers/spikes/cat-rig.md` with the decision (approach 1, 2 or a mix), the **minimum** list of layers and poses Task 2 needs, and anything that read poorly. Spike code is deleted or labelled throwaway.

**Gate:** the user approves the look and the movement on the phone before Task 2. If it fails, we change approach or ask for a reference image; no dependent work starts.

### Task 2: Real-cat Eva in the game

Only after Task 1 is approved. Build the accepted approach as the real Eva.

- Replace the Eva branch of `RigFactory`/`CharacterRig` with the cat, keeping the `CharacterRig` public API (`Root`, `Animator`, `Wave`, `Cheer`, `SetTalking`) so `MapScreen`, `CountScreen`, `TutorialGuide` and `Hud` callers do not change (`Wave` now plays the bounce). `ApplyLook` stays a no-op for Eva.
- Eva everywhere she appears (Map, Count/School, House, Store): correct size, position, mirroring, sorting, and black-cat readability confirmed at each real size. The stray white rectangle on the old left leg disappears with the humanoid rig.
- Remove dead Eva art (`eva_arm/leg/torso/head/tail.svg` and derived PNGs) only if nothing else uses it; keep `char_*` player art.
- Update `RigTests` for the new Eva structure; keep player rig tests.
- **Check:** editmode suite green; `NoReadingAuditTests` green; on-device Map and a School round: cat reads as real and moves like a cat, idle/talk/cheer/greeting play, Map greeting cannot be mashed, clean-round cheer still works.

### Task 3: Hand-pointer alignment (extremely simple)

- One fixed offset derived from the hand art, so the fingertip, not the hand's centre, lands on the target centre, applied where `PointerHand` is moved in `CountScreen.RunHandCount`, `MoveHandToNextDemoObject` and `RunDemoAnswer` (objects and answer tiles). No pointer-targeting system, no generalized anchor framework.
- One editmode test that hand target minus the fingertip offset equals the target centre.
- **Device check (visual):** on a scattered level 3-4 board, the fingertip lands on the actual object being indicated and on the actual answer tile, with no ambiguity from the hand's centre being used as the target.

### Task 4: More answer choices (5 at levels 3-4, 6 at levels 5-6)

- Update the level table in `Counting.cs` to 5 and 6 fixed choices. The exact-count, no-duplicates, shuffled generator guarantee from M2 is kept.
- **Layout must not compromise preschool UX.** Tiles stay at or above `EvaUi.MinTap` (240); no shrinking below the child-friendly minimum. Two rows is not accepted just because it fits geometrically: the arrangement must stay easy for a 4-5-year-old to scan and understand without reading. Solve in this order: (1) composition and layout changes; (2) reclaiming unused space; (3) repositioning non-essential visual elements (Eva, HUD, speech bubble placement); (4) only then a different arrangement such as two rows. Final tiles remain large, distinct and immediately understandable.
- Extend `MaxTiles`/tile-position code in `CountScreen`/`CountLayout`, the tile-fit audit test to 5 and 6 tiles (non-overlapping, clear of Eva, HUD and bubble), and the choice-generator tests for the new counts.
- **Check:** suite green; on-device levels 3-6, tiles comfortable to tap and to scan. The user judges the arrangement.

### Task 5: The two test gaps (nothing more)

- One level 5/6 screen-level Count test (fix `CountScreenTestSupport` so `CorrectTileIndex`/`WrongTileIndices` count only asked objects, not distractors): distractor tap is never a mistake, correct answer settles the round.
- One `SaveStore` test: `DifficultyLevel` clamps to 1..6 on load and `DifficultyBuffer` round-trips.
- No broader test hardening. The existing level 5-6 on-device verification stays valuable, since tests do not prove the child-facing experience.

### Task 6: Placeholder art replacement (subordinate to Eva; user selects)

Not "finish the art". After the user sees the real Eva, identify only placeholder assets that materially harm the child-facing experience (candidates: House divider, ghost drop-target rectangles, important counting objects, backgrounds). **The user chooses what actually gets replaced**, one screen at a time, with the same clean-layer rule and the same real-not-cartoon direction. No general visual cleanup.

### Task 7: Catalog and House-slot model change (simple) - SUPERSEDED by docs/superpowers/plans/2026-09-24-eva-house.md

Required in M3, but no "proper shopping system". Target only:
- more House slots;
- a Store layout that comfortably accommodates more furniture (simple re-grid, or pagination only if actually needed);
- the existing deterministic purchase flow intact.

Not introduced: categories, filters, inventory screens, nested navigation or other shop machinery, unless the existing model literally cannot support the required experience. The child must still understand what can be bought and where it goes with minimal explanation. Invariant: `CoinPayout.MinSessionPayout >= FurnitureCatalog.CheapestPrice`. Extend `FurnitureTests`; art follows the clean-layer rule.

### Task 8: Acceptance and M3 summary

**Technical (necessary, not sufficient):** cold editmode runs, Eva (227 plus new tests) and repo root (187); no M2 regression.

**Product gate (the user's device play, whole loop):** Map, Eva, School, Count, reward, Store, House. The user judges:
- Eva convincingly reads as a real cat, and her movement feels feline;
- the child can understand the counting screen without reading;
- the pointer actually points at the target;
- 5 and 6 answer choices remain comfortable and clear;
- distractors behave correctly;
- Store and House expansion remains understandable;
- the whole loop still feels cohesive and fun.

Write `docs/superpowers/spikes/M3-summary.md` in the M2 summary format.

## Risks

- Drawn layers look flat or stiff when moved: mitigated by the spike and gate; fallback is the mixed or cutout approach, or asking the user for a reference image.
- Black fur vanishes on dark backgrounds: solve by value separation and silhouette first, subtle highlights last; verify at every real size.
- Dwarf proportions must be preserved, not normalised toward a generic cat.
- Six tiles at >= 240 in 1440 wide leave little slack; may need layout changes beyond the tile row (composition, reclaimed space).
- M2 tests hard-code 1440x900 in a few places; new layout tests use `EvaLayout`.

## Deferred out of M3

New game modes, rooms, clothing, pets, the player character's redesign, final music/sound, general visual cleanup, and the cosmetic findings (wobble stacking, small tick badge, unused `icons/dot` art) except any the cat work touches.
