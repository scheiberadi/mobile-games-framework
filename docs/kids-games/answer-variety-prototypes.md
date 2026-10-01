# Answer-variety: the two prototypes (Item to Shadow, Sorting)

Status: **implementation plan only, written in claude.ai from a static reading of the code. Nothing is built,
compiled or run.** Parent document: `docs/kids-games/answer-variety-plan.md` (direction and constraints
approved by the user on 2026-10-01). Scope of this document: exactly two prototypes, then stop. No rollout to
other games until both have been built and judged on the PC/device.

The point of the prototypes is not to prove that drag works in code. It is to find out whether the new
interaction is **more playful** for a 4-5 year old than "tap the one right answer", and not just a quiz with a
different gesture. Section 4 defines how that is judged.

## 0. Shared building blocks (what both prototypes need)

Existing and reused as they are:
- `DragItem` (`App/Ui/DragItem.cs`): follows the finger 1:1, raises BeginDrag/EndDrag events, adds a `TapTarget`.
- The snap-radius / snap-back / pop-on-place pattern from `DressForOccasionScreen` (`OnPieceEndDrag`,
  `NearestSlotWithinRadius`, `SnapBackToTray`, `PopPulse`).
- `HelpLadder`, `CoinPayout`, `DifficultyLadder` (`Rules/HelpAndCoins.cs`, `Rules/Counting.cs`), `PointerHand`,
  `Voice`, `Sfx`, the end panel and `Hud` as every existing game uses them.

New, small, and shared by both:
1. **`Rules/DropGeometry.cs` (pure):** `NearestWithinRadius(point, centres[], radius)` returning the index or -1. Unit tested.
   Drag logic is easy to get subtly wrong and cannot be debugged in the cloud, so it lives in Rules.
2. **Hover feedback:** while a dragged item is within snap range of a target, that target highlights (grows
   slightly, glows). This is the "game feel" that tap games lack: the child sees the target "want" the item
   before letting go. Implemented in the presenter's per-frame check using `DropGeometry`.
3. **`PointerHand` drag demo:** a coroutine that moves the hand to an item, "grabs" it, carries it to the target
   while moving the item with it, and releases. Needed for the hint and demonstrate steps, since today the hand
   only points and taps (`MoveTo`, `Tap`, `Pulse`).
4. **Layout audit for drag handles:** the existing `NoReadingAuditTests` pattern (every `TapTarget` at least 240,
   inside the frame, overlap at most 20 units), applied to each new screen's initial layout.

Each prototype is a **new shared presenter configured per game** (as `MatchScreen` is), so that once validated it
can be reused, but it is built and wired for one game only at this stage.

## 1. Prototype A: Item to Shadow (drag-to-target, DT)

### 1.1 Current implementation (read from the code)
- `App/Screens/ItemToShadowScreen.cs` (458 lines, its own screen, not `MatchScreen`) and
  `Rules/ItemToShadow.cs`.
- Flow today: Eva says "Find its shadow!". One object picture (150 units) sits above; 3-4 silhouette tiles (240) sit
  in a row below; the child taps one. Wrong tap greys the tile, Eva reacts angry, help ladder: retry, hint (hand
  points at the right silhouette), demonstrate (hand taps it, only that tile stays live). Five rounds, then coins.
- Rules: `ItemToShadowRoundGenerator.Create(level, rng)` returns `TargetKey`, `Choices[]` (shuffled keys),
  `CorrectIndex`. 16 items in 4 outline groups (Round, Narrow, Animal, Vehicle). Levels 1-2 use 3 choices with
  distractors from other groups (obvious); levels 3-6 use 3-4 choices from the same group (hard). Own level and
  buffer in `PlayerProgress`.
- Art in `Resources/Art/itemtoshadow/`: 16 `<key>.png` objects and 16 `<key>_silhouette.png`.
- **Existing mismatch found:** the screen loads the target as `itemtoshadow/<key>_object`, but the files are
  named `<key>.png` and `art/eva/playground/PROMPTS.md` also says `itemtoshadow/<key>`. As the code reads, the
  target picture currently resolves to a placeholder. Verify on the PC; the prototype uses `itemtoshadow/<key>`.

### 1.2 New interaction flow
Reframed from "which shadow matches?" to "put each thing on its shadow", a small tidy-up game:
1. Scene: several silhouettes lie on the ground as shadows (non-interactive, about 200 units). Below, the same
   number of coloured objects sit in a row (draggable, 240), in a **different order** from the shadows.
2. Eva says one line ("Put each one on its shadow!"); no text.
3. The child drags an object. As it nears the right shadow, the shadow glows (hover feedback); near a wrong one,
   nothing happens. Releasing close to the **right** shadow: the object slides in, pops, the shadow fills with the
   object's colour, a sound plays. Releasing near a **wrong** shadow or in empty space: the object springs back
   to its place (a wrong shadow also does a small "no" shake). It never blocks the child or ends the round.
4. When every object is placed, Eva cheers and the scene lights up, then the next round.
5. Help ladder per round (same `HelpLadder`): 1st wrong drop: retry sound and line; 2nd: hint, the hand grabs the
   object that still needs placing and glides it onto its shadow slowly; 3rd: demonstrate, the hand carries it and
   drops it, that item is then placed for the child.
6. Session: 3 rounds (a presenter setting; the Rules constant `RoundsPerSession = 5` is left alone), because one
   round now means 3-4 drags.

Why this should read as a game, not a quiz (to be confirmed, not assumed): the child physically moves the
object; the result is a visible, satisfying consequence (the shadow fills); a wrong try costs nothing and shows
why (it does not fit); and all pairs get done, so the child is "finishing a scene", not answering a question.
A fallback configuration `ObjectsPerRound = 1` reproduces "one object, several shadows" if the multi-pair version
turns out too long or too hard.

### 1.3 Presenter to build
- New `App/Screens/DragToTargetScreen.cs`: a shared DT presenter, configured with a round factory, level/buffer
  accessors, voice keys, sprite prefixes and backdrop, in the same shape as `MatchScreen`'s constructor. Draws
  targets (no `TapTarget`), draggable items via `DragItem`, hover highlight, snap/snap-back, ladder, hand demo, end
  panel, `Hud`.
- Registration: `ScreenId.ItemToShadow` is switched to the new presenter in `EvaGame`. The old
  `ItemToShadowScreen` stays in the repo, unused, until the evaluation is finished, so the two can be compared
  side by side on the device (a single constant/flag selects which is registered).

### 1.4 Rules logic retained, and what is added
- **Kept unchanged:** `ItemToShadowRoundGenerator` (catalogue, groups, `ChoiceCountByLevel`, the "obvious to
  similar" progression, level/buffer persistence). Its `Choices[]` become the set of pairs for the round: each
  choice key is both an object and a shadow. `TargetKey`/`CorrectIndex` are used only to pick which object the
  hint helps first.
- **Added (pure Rules):** `Rules/DragToTarget.cs` with `DragToTargetRound` (item keys, target keys, mapping),
  an adapter `FromItemToShadow(round, rng)` that guarantees the object order differs from the shadow order, and
  `DropGeometry` (section 0).

### 1.5 Art
- **Reuse existing:** the 16 objects and 16 silhouettes cover the prototype. No generation is needed.
- **Only a rename/code fix:** the `_object` vs `<key>` mismatch above.
- Nice-to-have, **not** part of the prototype and not to be generated yet: a dedicated "ground with shadow
  marks" backdrop. The prototype uses the existing Playground background.
- Note for later art specs (DT in general): every draggable must be a separate transparent cut-out, every target a
  clearly bounded shape that reads as "something goes here".

### 1.6 Tests required
Unit (Edit Mode, Rules, need Unity to run):
- `DragToTarget` adapter: every item maps to exactly one target; the set of item keys equals the set of target
  keys; object order is never identical to shadow order when there are 2+ pairs; deterministic for a seed; works
  for all levels 1-6 (3 and 4 pairs).
- `DropGeometry.NearestWithinRadius`: inside, outside, equidistant, empty list.
- `ItemToShadowTests` (existing) must still pass unchanged.
Screen/audit:
- Initial layout of the new screen passes the existing tap-target audit (size, frame, overlap, Home clearance) with
  4 draggables and Eva at her usual position.
- Smoke: the screen builds, shows a round and completes one by invoking its drop handler for each pair.
- `ArtTests`: add the `itemtoshadow/` keys the screen loads so the rename mismatch cannot return.
Voice: new lines (below) need clips, or `VoiceCompletenessTests` fails.

New voice lines (to be recorded with the existing TTS pipeline): `itemtoshadow_drag` ("Put each one on its
shadow!"), `itemtoshadow_drag_hint` ("Drag it here!"), `itemtoshadow_drag_demo` ("Watch me drag it!").

### 1.7 PC/device acceptance criteria
A. Mechanics (the user, on the device):
1. Drag follows the finger without lag or offset; the object is easy to grab (240 handle).
2. Hover highlight appears at a sensible distance; release near the right shadow always snaps; release near a
   wrong shadow always springs back; no state where an object gets stuck or lost.
3. Hint and demonstrate hands visibly drag the right object to the right shadow; the child's own drags still
   work after a hint.
4. A session of 3 rounds completes, pays coins by help step, returns to Playground; leaving mid-drag and
   re-entering leaves no ghost objects.
5. Layout: nothing overlaps Home, Eva or each other beyond the audit allowance; works on the tested aspect
   ratios.
B. Child test (the real prototype question). With 3 or more children aged 4-5 (or as many as available), same
   session, old tap version then new, or alternating order:
1. Understands what to do from Eva's line and the first demo, without an adult explaining.
2. Completes levels 1-2 rounds mostly without the demonstrate step.
3. First-attempt drop accuracy is acceptable and misses are not frustrating (no repeated failed grabs).
4. Watch for engagement: smiles, comments, reaching for the next object, asking to play again, versus drifting off.
5. When offered both versions, which do they choose, and which do they ask for again?
C. Verdict wording to record: **"more game-like"**, **"about the same, only a different gesture"** or **"worse"**.
   Proposed bar for continuing with DT: at least 2 of 3 children prefer or ask again for the new version, and
   the mechanics criteria A pass. The user sets the final bar.

## 2. Prototype B: Sorting (drop-sort, DS)

### 2.1 Current implementation (read from the code)
- Registered in `EvaGame` as `new MatchScreen(ScreenId.Sorting, ...)` with
  `BrainGymMatchRoundGenerator.Create(BrainGymMatchGameKind.Sorting, ...)` and `Progress.SortingLevel/Buffer`.
- `MatchScreen` shows ONE item picture (`braingym/sortitem_<id>`) and 2-4 category tiles
  (`braingym/category_<value>`); the child taps the category. Five rounds, retry/hint/demonstrate ladder (hint
  "Look, it's this one!", demo "Watch me tap it!"), prompt "Where does it go?".
- Rules: `MatchRoundBuilder.Build` over `SortingItems`: apple/fruit, carrot/vegetable, shirt/clothes,
  truck/vehicle, banana/fruit, pants/clothes. Level sets how many of the 6 items are in play (4,5,6,6,6,6) and
  how many category tiles show (2,3,3,4,4,4).
- Art: **none present.** `Resources/Art` has no `braingym/` folder; the Brain Gym gameplay-art prompts have not
  been written. Today the game runs on generated placeholder boxes.
- **Content limitation found:** with only 6 items, "vegetable" and "vehicle" have a single member each, so a
  round that sorts several items into several bins cannot have 2+ items per bin. A real sorting game needs more
  items.

### 2.2 New interaction flow
Reframed from "which category is this?" to "put the things away":
1. Scene: 2-3 bins along the top, each a recognisable container (fruit basket, clothes hamper, vehicle garage...)
   shown with its own picture, not a label. A belt/tray at the bottom brings the round's items one at a time (the
   current item large and draggable, the next ones waiting greyed out).
2. Eva: "Where does it go?" (existing line, reused).
3. The child drags the item toward a bin. The bin under the finger opens/glows (hover feedback). Release over the
   **right** bin: the item drops in, the bin bounces and plays its sound, and visibly fills (a small stack of
   what it holds, plus a digit count). Release over a **wrong** bin: the bin shakes "no thanks", the item springs
   back, no penalty beyond the ladder.
4. After all items are sorted, Eva cheers and the bins are shown full; next round or end.
5. Help ladder per round: retry; hint (hand grabs the current item and glides it to the right bin); demonstrate
   (hand sorts it, then the child continues).
6. Session: 3 rounds of 4-6 items each (a presenter setting), bins capped at 3 so they fit left of Eva.

Why this should read as a game (to be confirmed): the child physically puts things in containers that react and
fill up; the same objects visibly "live" somewhere afterwards; the round is a short chore with a clear finish, not
a series of unrelated questions.

### 2.3 Presenter to build
- New `App/Screens/DropSortScreen.cs`: shared DS presenter (bins, belt of draggable items, hover open/glow, drop
  reaction, fill display, ladder, hand drag demo, end panel). Same constructor shape as `MatchScreen`. It shares
  `DropGeometry`, the hover feedback and the hand drag demo with the DT presenter; if the two end up with large
  duplicated code after the prototypes, merge the common part then, not before.
- Registration: `ScreenId.Sorting` switches from `MatchScreen` to `DropSortScreen`. `Recycling`,
  `MatchItemToCategory`, `SortLaundryChores` stay on `MatchScreen` for now (no rollout).

### 2.4 Rules logic retained, and what is added
- **Kept:** the learning content model (an item belongs to a category value), the level/buffer fields, the
  prompt and per-category voice keys, `DifficultyLadder`, `HelpLadder`, `CoinPayout`.
- **Not kept as is:** `MatchRoundBuilder` yields one target per round, which cannot drive a multi-item sort. A
  new pure `SortRoundBuilder` (in `Rules/`) builds a round of N items and B bins from the same `(id, category)`
  list and the same level idea (level raises the item and bin counts). This is a new builder next to the old one,
  not a change to the old one, so the other `MatchScreen` games are untouched.
- **Content addition (data, not logic):** extend the Sorting item list to about 12 items (3 per category across 4
  categories: fruit, vegetable, clothes, vehicle) so every bin shown can receive 2+ items.
- **Tests keep their meaning:** the existing Brain Gym generator tests for the match games continue to apply.

### 2.5 Art required (specification only; do not generate yet)
Because no Brain Gym art exists, the prototype defines the reusable DS pattern for later games (Recycling,
Laundry, animal classes...):
- **Bins:** 4 container pictures, each with a clear open top and a unique silhouette/colour, readable without
  text: fruit basket, vegetable crate, clothes hamper, vehicle garage. Plus a "full/closed" variant is optional.
- **Items:** 12 transparent cut-outs, one per sort item (e.g. apple, banana, orange; carrot, tomato, corn; shirt,
  pants, sock; truck, bus, bike), each about 240 units, clearly recognisable alone.
- **Belt/tray strip** and an optional sparkle; the backdrop reuses `world/braingym_bg`.
- Sprite keys follow the existing scheme (`braingym/category_<value>`, `braingym/sortitem_<id>`) so nothing
  else in the codebase changes. Until the art arrives, the prototype can be exercised with placeholders but
  **cannot be judged** (a coloured box in a coloured box does not tell us whether it is fun), so the judged
  evaluation needs at least the 4 bins and 12 items.
- This is the only art the prototype needs: 16 images, all written as one small ChatGPT sheet prompt set, to be
  made only after the user approves this plan.

### 2.6 Tests required
Unit (Rules, need Unity to run):
- `SortRoundBuilder`: every item's category is one of the round's bins; every shown bin has at least one item
  (and at least two when the pool allows); no duplicate items; item and bin counts follow the level table and
  never exceed 6 items / 3 bins; deterministic for a seed; levels 1-6.
- Content invariant: every category in the Sorting list has at least 2 items.
- `DropGeometry` tests (shared with prototype A).
- Existing Brain Gym tests still pass.
Screen/audit:
- Layout audit of the new screen (draggable items and any tappable controls at least 240, inside frame, overlap,
  clear of Home and Eva), with 3 bins.
- Smoke: builds, runs a round and completes it by invoking the drop handler per item.
Voice: new lines `sorting_drag_hint` ("Drag it into the right one!") and `sorting_drag_demo` ("Watch me put it
away!") need clips; the existing `braingym_prompt_sorting` is reused.

### 2.7 PC/device acceptance criteria
A. Mechanics (the user, on the device):
1. Items come one at a time from the belt, grab easily, follow the finger exactly.
2. Hover opens/glows the right bin at a sensible distance; right drop accepts and reacts, wrong drop rejects and
   springs back; bins visibly fill and the count is correct.
3. Hint and demonstrate hands drag the item to the correct bin; the child's drags still work afterwards.
4. 3 rounds complete, coins are paid by help step, return to Brain Gym works; no stuck items.
5. Layout holds with 3 bins and Eva visible, on the tested aspect ratios.
B. Child test (same protocol as prototype A), with the old tap version of Sorting as comparison:
1. Understands the goal from the single prompt and the first demo.
2. Finishes level 1-2 rounds largely unassisted; misses are not frustrating.
3. Engagement: do they want to keep putting items away, watch the bins fill, ask for more?
4. Preference when offered both versions.
C. Same verdict wording and proposed bar as prototype A. **Additional DS-specific check:** the child should
   understand *why* each item belongs in its bin (the bin reaction and the picture, not a label), otherwise the
   learning goal is lost even if it is fun.

## 3. Order of work and stop point

1. Build prototype A (DT) and its tests. The art is already in the repo.
2. Write the DS art prompt set (16 images) for the user to approve; build prototype B (DS) and its tests.
3. The user evaluates both on the PC/device against sections 1.7 and 2.7.
4. Only then adjust the shared presenters and decide about rollout.

**Nothing beyond these two prototypes is started.** No other game is converted, no other presenter (hotspot,
real-time, paint, fill) is touched, and no broad art is generated.

## 4. How "more game-like than a quiz" is judged

A prototype passes the product test only if the observations show all of these, not just that the code works:
- the child acts on objects and sees the consequence in the scene (not a right/wrong verdict screen);
- a wrong attempt is cheap, informative and non-punishing;
- the round ends because a scene is finished, not because a question was answered;
- the child's own behaviour (replay requests, preference, sustained attention) beats the tap version;
- the learning target is still clearly the thing being practised (shape matching; categorising).
If a prototype is mechanically different but is judged "about the same", that is a negative result and the
rollout plan must be revised, not pushed through.

## 5. Constraints to keep in view
- 4-5 year old non-reader baseline; digits are the only visible text; handles at least 240.
- Do not change unrelated games; keep both old screens in place until the evaluation is done.
- No claim that anything compiles or works until it has been run in Unity and played on the device.
- The M4 branch is also worked on from the PC: agree a time to touch `EvaGame.cs`, `ItemToShadowScreen` and Brain
  Gym content before implementing.
