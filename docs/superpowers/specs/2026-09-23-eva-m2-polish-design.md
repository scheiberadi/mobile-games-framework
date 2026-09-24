# Eva's Learning World, M2: Polish design

## 1. Summary

M1 delivered a playable, technically-accepted vertical slice (creator, map, School's Count the Objects, House, Store) and the user approved it ("M1 is technically approved"). M2 does not add new modes, rooms, or systems. Its job is to take the existing loop from "works" to "feels like a charming, coherent children's game" before any content is added on top of it. Concretely: fix the small defects M1 review turned up, establish a visual direction the current art doesn't yet have, tune the difficulty ladder and pacing together (since both shape how a round feels), add feedback/celebration juice where it earns its keep, and only then consider adding more furniture or objects to the existing catalog.

## 2. Guiding principle

**Make the existing vertical slice feel like a charming, coherent children's game before increasing its content.** More furniture, more objects, more anything is not inherently valuable in M2 — it is explicitly secondary to the quality of the five-minute experience the child already has. Every task in this milestone should be judged against "does this make the current loop feel better," not "does this add something new."

## 3. Non-goals

- No new game modes, rooms, furniture categories, or clothing/pet systems (deferred to when a second collection or second mode exists, per the M1 spec).
- No final art/music/sound asset production. Task 2 (visual direction) reviews the current look and makes the *minimum* changes needed to establish a direction — it is not a full art pass.
- No tutorial-skip control, Settings, parent gate, volume controls, or parent progress view (still M2+/deferred; explicitly excluded from this milestone by user decision).
- No difficulty ladder for modes that don't exist yet (Count the Objects is the only implemented mode; the ladder applies there only).
- No new voice lines beyond what a juice/pacing change strictly requires (e.g. a new celebration line is in scope if it serves task 4; a new mode's voice lines are not).

## 4. Scope

### 4.1 M2 bug/layout fixes

The concrete, already-identified issues from M1 review (`docs/superpowers/spikes/M1-summary.md`, "M2 inputs"), and nothing broader:

- **1440-vs-1600 layout-frame confusion.** Three M1 tasks (9, 10, 11) shipped a Critical geometry defect from assuming the canvas's nominal 1600-unit width instead of the real 1440×900 device frame. Introduce a single shared constant (e.g. `EvaLayout.DesignWidth = 1440`, `EvaLayout.DesignHeight = 900`) for new and shared code to reference going forward. **This is not a mandate to build a generic layout-validation framework or mechanically replace every existing `1600`/`1440`/`900` literal in the codebase.** Each existing hardcoded usage should be looked at individually and only changed where it's actually wrong or where centralizing it removes real duplication — a literal that's correct and unrelated to this bug class can stay a literal. The test to add is the smallest one that would have caught the actual failure mode from Tasks 9-11 (Store's shelf items assuming a 1600-unit frame and overlapping the fixed Hud buttons; Creator's Confirm button and shirt row falling below the real 900-unit-tall floor) — a small, targeted regression test or two against those specific screens' known-bad geometry, not a whole-canvas generic auditing abstraction.
- **House overlap-regression test.** Creator and Store each have an automated 20-unit tap-target overlap test; House's Critical z-order finding (a placed item occluding a tray item's tap target) was fixed but has no regression test of its own. Add one, following the existing Creator/Store test pattern.
- **Store `GrowIn` coroutine cleanup.** The buy-confirm coroutine is tracked and stopped correctly; `GrowIn` is not, so a fast tap→cancel→re-tap could leave a stale coroutine running. Track and stop it the same way.
- **Stale doc comments.** `TutorialGuide.cs`'s class doc still describes the pre-fix "once per (step, screen) pairing" dedup behavior; `EvaGame.cs`'s unused fallback canvas constructor still references 1600×900. Both are comment-only fixes, but the second should be re-checked against the new `EvaLayout` constant from this task in case it's worth deleting the dead fallback entirely rather than just fixing its comment.

### 4.2 Visual direction and polish

M1's art (hand-written SVG icons/objects/world backgrounds, procedural character rig) was built to prove the technical pipeline, not to establish how the game should look and feel. Before tuning anything else, this task reviews the whole game on a real device as a *visual and experiential* whole and makes the minimum changes needed to fix what's wrong — it does not replace the art pipeline or commission final assets.

Review dimensions (on-device, screenshots as the observation channel per the project's established phone rules):
- **Character appeal** — does Eva and the player-character rig read as warm and inviting to a 4-5-year-old, or flat/generic?
- **Visual consistency** — do icons, objects, and backgrounds share a coherent style, palette, and line weight, or do they look like separately-produced placeholders?
- **Readability and composition** — is it obvious at a glance what's tappable, what's decoration, and where to look next on each screen?
- **Map/House/Store clarity** — do the three destinations read clearly as distinct, understandable places?
- **Object/icon recognition** — can a non-reading 4-5-year-old identify each counted object and store item from its icon alone?
- **Age-appropriateness** — does the overall look feel like it's for a young child, without being condescending?

Output of the review is a short findings list (what's actually wrong, not a wishlist), followed by the smallest set of concrete changes — palette adjustments, icon redraws, rig proportion or color tweaks, background simplification — that establishes a visual direction the rest of the project can build on later. Findings and changes both get recorded in this milestone's summary document, the same way M1 recorded its M2-inputs.

**This task establishes a visual language, not a set of prettier isolated assets.** Alongside the findings list, the review must record a short, concrete design note — a handful of bullet points, not a document — covering:
- **Palette direction** (the specific colors/families the art should draw from going forward).
- **Shape/illustration language** (rounded vs. geometric, simple flat shapes vs. more detail, general silhouette style).
- **Outline/line-weight treatment** (whether shapes get an outline, how thick, how consistent it is across icons/objects/characters).
- **Shading/detail level** (flat color, simple two-tone shading, or something else — and how much detail is too much for a glance-legible icon).
- **Character/object proportion treatment** (how stylized vs. realistic proportions should be, kept consistent between Eva, the player character, and objects).
- **Interactive-vs-decoration visual distinction** (the concrete visual cue — e.g. a consistent highlight, shadow, or border treatment — that marks something as tappable, applied consistently rather than ad hoc per screen).

This note is what makes the "minimum changes" of this task additive rather than a one-off: it is a short design record living in the M2 summary, not a new art pipeline, tool, or framework.

**Keep the changes small and representative, not broad.** Prefer a small set of corrections — enough to make the visual language above legible across Creator, Map, School, House, and Store — over redrawing large amounts of existing content. If a screen already reads fine against the visual language once established, leave it alone; this task is about establishing and demonstrating direction, not repainting the slice.

**Non-negotiable baseline.** Every change made under this task must preserve, not just avoid actively breaking:
- No-reading gameplay — nothing spoken-only becomes text-dependent.
- Icons as the primary interaction language — a redraw must stay at least as recognizable as the icon it replaces, not merely prettier.
- Large, forgiving touch targets — no visual change shrinks a tap/drag target below M1's sizes.
- Immediate visual/audio feedback on every interaction.
- A clear visual distinction between interactive elements and decoration — a prettier screen that makes it less obvious what's tappable is a regression, not an improvement, and must be reverted or reworked rather than shipped.

### 4.3 Difficulty and pacing

Implemented together because both directly shape how a round feels to play. This section is the complete, final specification of Count the Objects' difficulty behavior for M2 — a concrete design decision, not a range for the implementation to tune. Playtesting may change these numbers in a later milestone; M2 ships exactly this.

**The four levels** (matching spec 4.7's own example of "1 to 3, then 1 to 5, then 1 to 10, then 1 to 20") are the entire difficulty system for Count the Objects. This is not a generic difficulty framework — it is one small, hardcoded table for this one mode, the same shape as today's `MaxQuantityByRound` array:

| Level | Quantity range | Answer choices |
|---|---|---|
| 1 | 1–3 | 3 |
| 2 | 1–5 | 3 |
| 3 | 1–10 | 4 |
| 4 (top) | 1–20 | 4 |

**Exact per-round gameplay at a given level:**
- **Quantity selection is intentionally not uniform at levels 3-4.** Uniform selection across a wide range (e.g. 1-20 at level 4) makes a quantity of 1 exactly as likely as 20, which risks the highest level not feeling harder in practice even though its range is technically bigger — this is a deliberate, accepted design decision for M2, resolved as follows rather than left uniform:
  - **Levels 1-2** (ranges 1-3, 1-5 — small enough that skew doesn't matter): quantity is drawn uniformly at random from the level's range, independently every round, exactly as Count the Objects already does today.
  - **Levels 3-4** (ranges 1-10, 1-20 — wide enough that skew matters): quantity is the **maximum of two independent uniform draws** from the level's range (`quantity = Math.Max(rng.Next(1, levelMax+1), rng.Next(1, levelMax+1))`). This is a single, simple, well-known trick — no weight table, no new sampling framework — that skews the distribution toward the upper part of the range (its CDF is quadratic, so low values become proportionally rarer) while still allowing an occasional easy round, rather than every value in range being equally likely.
  - This is the entirety of the weighting mechanism — no per-child tuning, no adaptive/dynamic difficulty, no additional levels.
- **Answer choices, guaranteed to hit the exact count deterministically** (fixes a gap in an earlier draft of this spec, where "distinct random candidates, deduplicated" could under-fill if a candidate collided): 1) start with the correct quantity as the first choice. 2) Build a pool of every other integer in `1..levelMax` (level's range max), excluding the correct quantity — this pool is always large enough (levels 1-2 need exactly 2 more values from a pool of exactly `levelMax - 1 >= 2`; levels 3-4 need exactly 3 more from a much larger pool), so this can never under-fill. 3) Randomly select without replacement from that pool until the choice list holds the level's exact choice count (3 for levels 1-2, 4 for levels 3-4). 4) Shuffle the resulting list for presentation. **This replaces today's ascending-sort presentation with a shuffled one — an intentional M2 change**, not an oversight: a fixed sorted order lets a child learn "the answer is usually in the same slot" rather than actually reading the quantity, and shuffling closes that shortcut.
- **Object selection**: unchanged from M1 — a random `CountObject`, never repeating the immediately previous round's object.
- **How the next round's difficulty is chosen within a level**: it isn't chosen per round beyond the above — every round at a given level draws independently using that level's fixed range, distribution shape, and choice count. There is no further per-round tuning (no streak-based adjustment within a level); all progression happens at the level boundary via the rule below.
- This quantity/choice generation logic stays local to `Counting.cs`/`CountRoundGenerator` — no generic sampling utility or framework is introduced for it.

**Level-change trigger and reset, stated exactly — this is a genuine rolling window, not fixed 5-round blocks:**
- The engine keeps a small ordered buffer (at most 5 entries) of the most recent completed rounds' outcomes at the current level, each recorded as clean (`HelpStep != Demonstrate`) or demonstrated (`HelpStep == Demonstrate`). This is a local queue/ring-buffer for Count the Objects, not a generic rolling-window abstraction shared with anything else.
- **No evaluation happens until the buffer holds 5 rounds** (i.e. rounds 1-4 at a level, or the first 4 rounds after any level change, never trigger an evaluation).
- **From the 5th round at a level onward, evaluation happens after every completed round**, against whichever 5 most-recent rounds are currently in the buffer. Round 5 evaluates rounds 1-5; if unchanged, round 6 evaluates rounds 2-6 (oldest dropped, newest added); round 7 evaluates rounds 3-7; and so on — this is what makes it a rolling window rather than a fixed block.
- **Each evaluation checks, in order**: level-up first (`cleanCount >= 4` among the current 5) — if true, level increases by exactly 1, clamped so it never exceeds 4; otherwise level-down (`demonstratedCount >= 3` among the current 5) — if true, level decreases by exactly 1, clamped so it never drops below 1; otherwise the level is unchanged.
- **Boundary clamping, stated exactly**: at level 4, a level-up condition firing does not change the level (already at the top) — this is treated identically to "no change," not as a level change. At level 1, a level-down condition firing likewise leaves the level at 1 and is treated as "no change." **Only an actual numeric level change clears the buffer.** A clamped-at-boundary evaluation (condition fired but the level number didn't move) leaves the buffer rolling exactly as the "level unchanged" case does — the oldest entry still drops and the newest is still added, and the very next round is evaluated again against its own latest 5, the same as any other non-changing evaluation. This means a child who is already at level 4 and keeps playing cleanly, or already at level 1 and keeps needing demonstration, simply keeps rolling at that level rather than the ladder getting stuck waiting for a fresh 5-round block that never comes.
- **When a level actually changes (a real 1-step move, not a clamped no-op), the buffer is cleared entirely.** The new level then needs 5 fresh rounds before its first evaluation, exactly like a brand-new level — evaluation does not resume mid-window with old data from the previous level.
- **A level only ever moves by exactly one step per evaluation** — even an extreme window (5/5 clean, or 5/5 demonstrated) moves the level by 1, never more. This keeps every change a single, age-appropriate step rather than a jarring jump.
- **Immediately after a level change**, the very next round is generated at the new level's quantity range and choice count — there is no transition round, grace period, or gradual ramp between levels. Because the ranges and choice counts in the table above are visibly different from each other (a 1-10 round looks and plays differently from a 1-3 round), the child experiences a clearly different difficulty right away; because levels only ever move one step and the table's steps are all still within the spec's "no reading, no timer, forgiving" baseline, the new difficulty stays age-appropriate at every level. Task 3 (implementation plan) additionally requires an on-device check that this change doesn't feel arbitrary to the child — see spec §5's product gate and the plan's Task 3 UX check.
- **Round outcome mutual exclusivity is provable, not assumed**: since every round is binary (clean or demonstrated) and every evaluation is always against exactly 5 rounds (rolling or not), `cleanCount + demonstratedCount == 5` always holds at evaluation time, which makes `cleanCount >= 4` and `demonstratedCount >= 3` mutually exclusive by construction — they can never both be true at the same evaluation. The implementation checks level-up before level-down regardless (defensive ordering only, since the ambiguity is mathematically unreachable), and a test asserts this invariant directly (e.g. iterate all 6 possible 5-round clean/demonstrated splits and confirm at most one of level-up/level-down fires for each), so the impossibility is proven rather than assumed.

**Pacing tuning**, evaluated against the ladder so the two don't fight each other:
- Screen transition speed and feel (Map → School → Count round → back).
- Time between a round ending and the next one starting (currently whatever falls out of existing code — measure it, then decide if it needs tightening).
- Friction points noticed during M1's on-device acceptance pass or found during this task's own device pass (e.g. any step that makes a session feel slower than it should).

**Level-change feel is an explicit on-device check, not just a rules-layer proof.** No level screen, text label, or blocking transition — the child never needs to understand the word "level." But a level change should feel intentional rather than like the game arbitrarily changed. Where practical, reuse an existing lightweight feedback mechanism (Eva's voice/reaction, a brief visual cue) rather than building any new progression-UI system to signal it.

**Level 3 vs. level 4 distinctness is also an explicit on-device check.** Because level 4 keeps the upper-skewed (not uniform) distribution from the quantity-selection rule above, this is expected to already help, but it must still be confirmed by playing rather than assumed from the math: play enough level-4 rounds to judge whether they read as meaningfully harder than level 3, not just technically-a-bigger-range. If they don't, that's an M2 finding for the summary (spec §5), not a reason to add more levels or adaptive tuning.

### 4.4 Feedback and celebration juice

Added only where it improves clarity or delight — not decoration for its own sake:
- **Correct-answer feedback**: a squash/bounce animation on the tapped/correct object, replacing or augmenting the current static feedback.
- **Coin reward feedback**: coins visibly fly from the reward moment to the HUD coin counter, rather than the counter just incrementing.
- **Eva's reactions**: bigger, warmer visual reaction on a clean round (building on the existing `Cheer` animation) so success feels rewarded, not just acknowledged.
- Each addition is evaluated against the guiding principle: if it doesn't make an existing moment feel better, it doesn't ship.
- **Non-blocking, always.** No juice animation gates the next interaction. Coin-fly and celebration effects must be short or run concurrently with input already being accepted — the child can start the next action immediately, while the animation is still finishing on screen. An addition that makes the child wait is a regression against M1's "no stall" feel target (A5), not an improvement.
- **Coin logic and coin presentation are strictly separate.** The child's logical coin balance (save data, HUD's authoritative displayed value, what the Store checks against a price) updates immediately when a round's reward is earned — exactly as it does today, with no dependency on any animation. The coin-fly-to-HUD animation is a purely cosmetic overlay layered on top of that already-correct state: it never delays input, purchasing, navigation, or saving, and if the child leaves the screen before the animation finishes, the balance already on screen (and already saved) is correct regardless. If the animation can't be implemented that way without touching the authoritative balance's timing, it doesn't ship.
- **Juice adds no new gameplay-affecting machinery.** Correct-answer feedback and Eva's celebration are presentation-only, kept very short and readable at a glance, and must not introduce new animation/state tracking beyond what each effect strictly needs to play once and stop. Do not add architecture or test hooks solely to make the presentation layer itself testable — `CountTally.TryCount` already returns `false` on a repeated tap of an already-counted object, and that existing gameplay invariant (tally doesn't change, no coin awarded, no reward logic re-run) is what gets proven by an automated test; whether the squash/bounce *visual* plays or not on that no-op tap is a device/manual verification concern, not something to build test scaffolding for.

### 4.5 Catalog expansion

Only after tasks 4.1 through 4.4 land and feel good — this is explicitly last and explicitly optional if the milestone's time is better spent polishing further. Add more entries to the *existing* `FurnitureCatalog`/Store data (more objects a session can buy and place) using the current schema. No new furniture categories, no new rooms, no new systems — purely more rows of the same kind of data M1 already has. **If Tasks 4.1-4.4 already make the game feel good, ending M2 without this task is a fully acceptable outcome.** The existence of unused catalog slots is never a reason to keep this task open or to pad it further — more furniture is not inherently valuable (spec §2), and this task exists only to be picked up when the rest of the milestone leaves genuine time for it. **This task is exactly "add a few more catalog rows" — no additional catalog features, pricing logic, or content system gets scoped around it.** Tasks 4.1-4.4 and the quality of the existing five-minute loop are the actual success criterion for M2; this task is a bonus, never a requirement.

## 5. Acceptance

M2 does not have M1's kind of measurable acceptance table (no reading, independent first run, etc. — those were slice-defining and already passed). Its gate has two explicitly separate parts: technical acceptance proves the implementation works; the product gate is the only thing that decides whether M2 succeeded.

**Technical acceptance** (proves correctness, does not by itself mean M2 is done):
- All existing M1 tests still pass (199/199 baseline), plus new tests added for 4.1's fixes and 4.3's ladder.
- The difficulty ladder is verified with rules-level tests proving, deterministically and with no reliance on random play: no evaluation before 5 rounds at the current level; from the 5th round onward, evaluation after every subsequent completed round against the latest 5 (the window actually slides — e.g. round 6 evaluates rounds 2-6, not a fresh block); the buffer clears only after an actual numeric level change, never after a clamped no-op; a clamped level-up at level 4 or level-down at level 1 leaves the level unchanged and keeps the window rolling rather than resetting it; a level only ever moves by exactly one step per evaluation; the level-up/level-down mutual-exclusivity invariant; and the answer-choice generator's exact-count guarantee. The same way M1's Task 8 proved the help ladder with an integration test.
- No regression in any M1 acceptance criterion covered by automated tests (A1-A4) — this milestone changes feel and content, not the underlying rules M1 already proved.

**Product gate** (the actual measure of M2's success, same status as M1's A7): the user plays the existing five-minute loop after all completed tasks land and judges whether it is now noticeably more **charming, coherent, understandable, and fun** for the intended 4-5-year-old non-reader — not whether the tests pass. Passing every technical check above is necessary but not sufficient; a milestone that's fully green in tests but doesn't feel better to play has not met the product gate. This includes confirming a difficulty-level change doesn't feel arbitrary (spec §4.3) — the child doesn't need to know what "level" means, but the transition should read as intentional. Any A5 (feel) or A6 (persistence) concern gets a spot-check on device only if a task in this milestone plausibly touched that behavior.

## 6. Risks

- **Visual direction (4.2) is subjective and only the user can judge it.** The task produces a findings list, a short visual-language note, and proposed changes; final go/no-go on "does this look right" needs the user's own eyes on the device, the same way every UI task in M1 ended with a device step.
- **Difficulty ladder and pacing (4.3) can fight each other if tuned separately** — handled by scoping them as one task instead of two, per the design above, and by fully specifying the ladder's numbers and timing here rather than leaving them to implementation discretion.
- **Juice (4.4) can bloat scope, or quietly couple to gameplay state, if not bounded.** Handled by the "only where it improves clarity or delight" test, the explicit coin-logic/coin-presentation separation, the no-new-gameplay-machinery rule, and by keeping the list to three concrete additions rather than an open-ended wishlist.
- **Catalog expansion (4.5) is the easiest task to over-invest in** because it's the most content-shaped work in the milestone. The design explicitly sequences it last, marks it optional, and rules out scoping any further catalog work around it.
- **Task 1's layout fix could balloon into a generic validation framework if not bounded** — handled by scoping the test to the specific known failure mode from Tasks 9-11 and reviewing existing literals individually instead of a blanket replacement pass.
