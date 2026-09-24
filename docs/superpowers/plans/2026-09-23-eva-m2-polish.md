# Eva's Learning World, M2 Polish Implementation Plan

Spec: `docs/superpowers/specs/2026-09-23-eva-m2-polish-design.md`. M1 (the first playable vertical slice) is committed and user-accepted; this plan is **for review only — nothing in it is to be executed until the user approves it.**

## Global Constraints

Unchanged from M1 unless a task says otherwise:

- No reading required anywhere; voice-first, icons carry meaning.
- No ads, IAP, accounts, analytics, server, or runtime AI.
- Deterministic rewards only — earn coins, buy furniture you choose. Never chests, loot boxes, or random drops.
- Landscape only, real design frame is **1440×900** with no vertical slack (y ∈ [-450, 450]) — Task 1 makes this a named constant instead of tribal knowledge.
- Touch targets ≥ 80 dp, main buttons/draggables ≈ 96-120 dp, forgiving near-misses.
- Only the on-device human user taps through gameplay; no guessed coordinates, no `KEYCODE_BACK` (gesture-nav test device).
- Every task that touches gameplay feel ends with a device step, same as M1.
- `bash tools/run-editmode-tests.sh "C:/Users/schei/mobile-games-framework/EvasLearningWorld"` after every task; current baseline is **199/199**, rising as tasks add tests. Never `-quit`, never edit sources while Unity batchmode runs, bounded waits only.

## Guiding principle (repeat of spec §2)

Make the existing vertical slice feel like a charming, coherent children's game before increasing its content. Tasks are ordered so quality work lands before content work, and Task 5 (catalog expansion) is explicitly optional.

## Tasks

### Task 1: M2 bug and layout fixes

Fixes the concrete defects M1 review already found (spec §4.1). No new behavior, no device step required beyond a quick sanity check.

- Add `EvaLayout` (or extend an existing shared constants file) with `DesignWidth = 1440`, `DesignHeight = 900`, for new and shared code to reference. **Not** a mandate to mechanically replace every existing `1600`/`1440`/`900` literal — review each existing usage individually and only change it where it's actually wrong or removes real duplication.
- Add the smallest regression test(s) that would have caught the actual Tasks 9-11 failure mode: Store's shelf items overlapping the fixed Hud Home/Bubble buttons (wrongly assumed 1600-unit frame) and Creator's Confirm button/shirt row falling below the real 900-unit floor. Target these two known-bad geometries specifically (extend `StoreTests`/`CreatorScreen`-adjacent tests or add a small `LayoutFrameTests.cs` scoped to just these cases) — do not build a generic whole-canvas layout-auditing abstraction.
- Add a House tap-target overlap regression test, following `CreatorScreen`/`StoreScreen`'s existing 20-unit overlap test pattern (`HouseTests.cs` or a new test file).
- Track and stop `StoreScreen`'s `GrowIn` coroutine the same way the buy-confirm coroutine already is, so fast tap→cancel→re-tap can't leave a stale one running.
- Fix `TutorialGuide.cs`'s stale class doc comment (dedup is per-step now, not per (step, screen) pairing).
- Re-check `EvaGame.cs`'s unused fallback canvas constructor against the new `EvaLayout` constant; fix its stale 1600×900 comment, or delete the dead fallback path entirely if it's confirmed unreachable on the real device flow.

**Check:** editmode suite green with new tests included; no behavior change on device (this task is bug fixes and constants, not feel changes).

### Task 2: Visual direction review and minimum polish

Spec §4.2. This task is a **review first, changes second** — do not skip straight to redraws.

1. On-device pass (screenshots as the only observation channel, per phone rules) across Creator, Map, School/Count, House, Store. Judge against the six review dimensions in spec §4.2: character appeal, visual consistency, readability/composition, map/House/Store clarity, object/icon recognition, age-appropriateness.
2. Write a short, concrete findings list — what's actually wrong, not a wishlist. This becomes part of the M2 summary document (Task 6).
3. Make the minimum set of changes to establish a visual direction: palette adjustments, targeted icon/object redraws via the existing `art/eva/` SVG → `EvaArtImporter` pipeline, rig proportion/color tweaks in `RigFactory`/`Palette`, background simplification. Reuse the existing art tooling (`tools/build-eva-art.sh`); do not stand up a new pipeline.
4. This is explicitly not final asset production — if a finding needs more than the existing SVG/rig pipeline can reasonably fix, note it for a later milestone instead of scope-creeping this task.

**Visual-language note (spec §4.2, part of the deliverable, not just prettier assets).** Alongside the findings list, record a short bullet-point design note covering: palette direction; shape/illustration language; outline/line-weight treatment; shading/detail level; character/object proportion treatment; and the concrete visual cue that distinguishes interactive elements from decoration. This is a lightweight note for the M2 summary (Task 6), not a new art framework or tool.

**Keep changes small and representative.** Prefer a small set of corrections that makes the visual-language note legible across Creator, Map, School, House, and Store over redrawing large amounts of existing content. A screen that already reads fine once the direction is set stays untouched.

**Baseline every change must preserve** (spec §4.2, non-negotiable — verify explicitly, not just "didn't obviously break it"): no-reading gameplay; icons as the primary interaction language (a redraw must stay at least as recognizable, not just prettier); large/forgiving touch targets (no visual change shrinks a tap/drag target below M1's sizes); immediate visual/audio feedback on every interaction; a clear distinction between interactive elements and decoration. A prettier screen that becomes less obvious to a non-reading child is a regression — revert or rework it rather than ship it.

**Check:** `ArtTests`/`RigTests` still green after any asset changes; `NoReadingAuditTests` still green (proves the no-reading/target-size baseline held); on-device screenshot comparison before/after; user's own eyes are the actual gate for this task, same as A1/A7 were for M1.

### Task 3: Difficulty ladder and pacing

Spec §4.3. Implemented as one task because pacing and difficulty interact. Spec §4.3 is the complete, final numbers-and-timing specification — implement exactly what's below, not a tunable range.

**The ladder (one small hardcoded table, same shape as today's `MaxQuantityByRound`, not a generic difficulty framework):**

| Level | Quantity range | Answer choices |
|---|---|---|
| 1 | 1–3 | 3 |
| 2 | 1–5 | 3 |
| 3 | 1–10 | 4 |
| 4 (top) | 1–20 | 4 |

- Replace `Counting.cs`'s fixed `MaxQuantityByRound` in-session ramp with this table.
- **Quantity selection is not uniform at levels 3-4, by design.** Levels 1-2 (ranges 1-3, 1-5): draw uniformly at random from the level's range, independently every round, as today. Levels 3-4 (ranges 1-10, 1-20): `quantity = Math.Max(rng.Next(1, levelMax+1), rng.Next(1, levelMax+1))` — max of two independent uniform draws, a one-line, well-known trick that skews toward the upper part of the range instead of every value being equally likely (uniform 1-20 would make a 1 as likely as a 20 at the top level, which risks level 4 not feeling harder than level 3 despite the bigger nominal range). No weight table, no sampling framework, no per-child tuning — this is the entire mechanism, local to `Counting.cs`.
- **Answer choices, generated to deterministically guarantee the exact count** (do not rely on "random candidates, deduplicated," which can under-fill on a collision): (1) start the choice list with the correct quantity; (2) build a pool of every other integer in `1..levelMax` excluding the correct quantity — always large enough (levels 1-2 need 2 more from a pool of exactly `levelMax-1 >= 2`; levels 3-4 need 3 more from a much larger pool), so this never under-fills; (3) randomly select without replacement from the pool until the list holds the level's exact choice count (3 for levels 1-2, 4 for levels 3-4); (4) shuffle the resulting list for presentation. **This replaces today's ascending-sort with a shuffle — intentional**: a fixed sort order lets a child learn "the answer's usually in this slot" instead of actually reading the quantity.
- **Object selection**: unchanged — random `CountObject`, never repeating the previous round's. There is no further per-round tuning within a level beyond the quantity distribution and choice generation above (no streak-based easing) — all progression happens only at the level-change boundary below.
- **This is a genuine rolling window, not fixed 5-round blocks.** Keep a small ordered buffer (max 5 entries) of the current level's most recent round outcomes (clean = `HelpStep != Demonstrate`, demonstrated = `HelpStep == Demonstrate`) — a local queue/ring-buffer scoped to `Counting.cs`/`CountRoundGenerator`, not a generic rolling-window abstraction.
- **Evaluation timing, stated exactly**: no evaluation until the buffer holds 5 rounds (rounds 1-4 at a level, or the first 4 after any level change, never evaluate). From the 5th round at a level onward, evaluate after *every* completed round against whichever 5 most-recent rounds are currently buffered: round 5 evaluates rounds 1-5; if unchanged, round 6 evaluates rounds 2-6 (drop oldest, push newest); round 7 evaluates rounds 3-7; and so on. Each evaluation checks level-up first (`cleanCount >= 4` of the current 5) → level +1, clamped so it never exceeds 4; else level-down (`demonstratedCount >= 3` of the current 5) → level −1, clamped so it never drops below 1; else level unchanged.
- **Boundary clamping, stated exactly**: at level 4, a level-up condition firing leaves the level at 4 — treated identically to "no change," not as a level change. At level 1, a level-down condition firing likewise leaves the level at 1 and is treated as "no change." **Only an actual numeric level change clears the buffer** — a clamped-at-boundary evaluation keeps the buffer rolling exactly like any other "no change" evaluation (oldest drops, newest is added, next round evaluates its own latest 5). This keeps a child who's plateaued at level 4 (playing cleanly) or level 1 (still needing demonstration) rolling smoothly instead of the ladder stalling on a fresh block that never arrives.
- **Reset, stated exactly**: when a level *actually changes* (a real 1-step move, not a clamped no-op), clear the buffer entirely — the new level needs 5 fresh rounds before its first evaluation, exactly like a brand-new level; evaluation never resumes mid-window with data from the previous level. A level only ever moves by exactly one step per evaluation (even a 5/5 extreme window), and the very next round after a real change is immediately generated at the new level's range/choices/distribution with no transition round.
- **Mutual exclusivity is provable, not assumed**: since every round is binary and every evaluation is always against exactly 5 rounds (rolling or not), `cleanCount + demonstratedCount == 5` always holds at evaluation time, so `cleanCount >= 4` and `demonstratedCount >= 3` can never both be true at the same evaluation. Check level-up before level-down regardless (defensive ordering only). Add a test that asserts the invariant directly (iterate all 6 possible 5-round clean/demonstrated splits, confirm at most one of level-up/level-down fires for each).
- Add rules-level integration tests proving, deterministically (no random play): a synthetic sequence where rounds 1-5 contain ≥4 clean triggers level-up at round 5; a sequence where rounds 1-5 contain ≥3 demonstrated triggers level-down at round 5; when neither fires at round 5, round 6 evaluates rounds 2-6 (prove the window actually slid, not just stayed put — e.g. a round 6 outcome that only becomes decisive once round 1 drops out of the window); the buffer clears on a real level change and needs 5 fresh rounds before the next evaluation; level is clamped at 4 and 1; a level only ever moves by exactly 1 per evaluation; **two boundary-clamp tests**: at level 1, a window meeting the level-down condition leaves the level at 1 and the buffer keeps rolling (does not reset — the very next round evaluates rounds 2-6, not a fresh 1-5); at level 4, the mirror case with the level-up condition. Add a test for the answer-choices generator proving it always returns exactly the level's choice count with no duplicates, even run many times. Mirrors how Task 8 of M1 proved the help ladder deterministically (`CountingTests.cs`, extended).
- Pacing: measure and tune screen-transition speed (Map → School → Count round → back) and the gap between a round ending and the next starting. Fix any friction points noted during M1's on-device acceptance pass or found during this task's own device pass.
- **UX check for level changes (spec §4.3)**: no level screen, text label, or blocking transition — the child never needs to know the word "level." On-device, confirm a level change doesn't feel arbitrary: where practical, reuse an existing lightweight cue (Eva's voice/reaction from Task 4, or a brief visual cue) rather than building any new progression-UI system.
- **UX check for level 3 vs. 4 distinctness**: play enough level-4 rounds to judge whether they read as meaningfully harder than level 3 despite sharing the same choice count, not just technically-a-bigger-range. If the upper-skewed distribution above doesn't make that difference land, record it as an M2 finding (Task 6) — don't respond by adding levels or adaptive tuning.

**Check:** new ladder tests green and deterministically cover level-up, level-down, the rolling (not block) evaluation/reset behavior, the one-step clamp, the two boundary-clamp cases, the mutual-exclusivity invariant, and the answer-choices generator's exact-count guarantee (no on-device play required to prove any of these — the rules tests are the proof). On-device pass is scoped narrowly: confirm the visible round content (object count range/choice count) changes when the level changes — e.g. force one session's state to level 1 and another to level 3-4 and compare — rather than grinding a natural session hoping for a demotion; confirm the level change reads as intentional rather than arbitrary; and confirm level 4 feels meaningfully harder than level 3. User judges pacing feel.

### Task 4: Feedback and celebration juice

Spec §4.4. Each addition below is justified against "does this make an existing moment feel better" — if a candidate doesn't clear that bar during implementation, drop it rather than ship it anyway.

- Squash/bounce animation on the tapped/correct object in Count the Objects.
- Coin-fly-to-HUD animation for reward payout, replacing the instant counter increment.
- Bigger/warmer Eva reaction on a clean round, built on the existing `Cheer` animation rather than a new rig state.

**Non-blocking, always** (spec §4.4): none of the three may gate the next interaction. Coin-fly and celebration effects run short or concurrent with input already being accepted — the child can act again immediately while the animation finishes on screen. If an addition can't be made non-blocking without losing its point, cut it rather than let it stall the game.

**Coin logic and coin presentation are strictly separate.** The authoritative coin balance (save data, HUD's displayed value, what Store checks against a price) updates immediately when a round's reward is earned, exactly as today, with zero dependency on animation timing. The coin-fly-to-HUD animation is a cosmetic overlay on top of that already-correct state — it must never delay input, purchasing, navigation, or saving, and if the child navigates away mid-animation, the balance already shown (and already saved) must be correct. If this separation can't be achieved cleanly, don't ship the animation rather than compromise the balance's immediacy.

**No new gameplay-affecting machinery, and don't over-test presentation.** Keep each effect to presentation-only, very short, readable at a glance — no new animation/state tracking beyond playing once and stopping, and no architecture or test hooks added solely to make the presentation layer itself testable. The gameplay invariant that matters is already provable at the rules layer today: `CountTally.TryCount` returns `false` on a repeated tap of an already-counted object, so its tally doesn't change, no coin gets awarded, and no reward logic re-runs. That's what an automated test proves (extend `CountingTests.cs`/`HelpAndCoinsTests.cs` if not already covered) — whether the squash/bounce *visual* happens to play on that no-op tap is a device/manual check, not something to build test scaffolding around.

**Check:** editmode suite green (no rules-layer behavior changed, only presentation); the existing already-counted-tap invariant (tally unchanged, no coin, no reward re-run) is confirmed covered by a rules-layer test; on-device pass confirming the additions read clearly at a glance, don't introduce new friction, and specifically that input is accepted again — and the coin balance is already correct — before each animation finishes (not just "feels fast").

### Task 5: Catalog expansion (optional, do last)

Spec §4.5. Only start this task if Tasks 1-4 are done and the user has confirmed the slice already feels better. Add more entries to the existing `FurnitureCatalog`/Store data using the current schema — no new categories, no new rooms, no new systems. If time or momentum runs out before this task starts, that's an acceptable outcome for this milestone; note it as deferred rather than cutting corners elsewhere to fit it in. **This task stays optional exactly as written**: if Tasks 1-4 already make the game feel good, ending M2 without it is a complete, successful milestone. The existence of unused catalog slots is never a justification to keep this task open, extend it, or treat M2 as incomplete without it. No additional catalog features, pricing logic, or content system gets scoped around this task — it is exactly "add a few more rows," nothing more.

**Check:** `FurnitureTests.cs` extended for new entries; `CoinPayout.MinSessionPayout >= FurnitureCatalog.CheapestPrice` invariant re-verified still holds; on-device Store/House pass with the new items.

### Task 6: Acceptance and M2 summary

Spec §5 splits this into two explicitly separate parts — do not conflate them in the summary.

**Technical acceptance** (proves the implementation works; necessary but not sufficient for M2 to be "done"):
- Cold editmode run of the full suite (repo-root and Eva) as a sanity check, same as M1 Task 13 Step 1.
- Confirm Task 3's ladder tests deterministically cover: no evaluation before 5 rounds at the current level; evaluation after every subsequent completed round against the latest 5 once the window is full (proving the window actually slides, not just resets in fixed blocks); the buffer clearing only after an actual numeric level change, never after a clamped no-op; clamped level-up at level 4 and level-down at level 1 each leaving the level unchanged and the window still rolling (not reset); one-step-only level changes; the mutual-exclusivity invariant; and the answer-choice generator's exact-count guarantee.
- Confirm no regression in any M1 acceptance criterion covered by automated tests (A1-A4).

**Product gate** (the only thing that decides M2's success, same status as M1's A7): on-device, the user plays the existing five-minute loop after all completed tasks land and judges whether it is now noticeably more **charming, coherent, understandable, and fun** for the intended 4-5-year-old non-reader — not whether the tests pass. A milestone that's fully green in tests but doesn't feel better to play has not met this gate. Any A5 (feel) or A6 (persistence) concern gets a device spot-check only if a task in this milestone plausibly touched that behavior.

- Write `docs/superpowers/spikes/M2-summary.md`, following `M1-summary.md`'s format: task verdicts, Task 2's findings list and visual-language note, measured results, the technical-acceptance results and the product-gate verdict reported separately, and an M3-inputs section for anything found but deferred.

## Deferred out of M2 (unchanged from spec §3, and from M1's own deferred list)

- New game modes, rooms, furniture categories, clothing, pets.
- Final art/music/sound asset production (Task 2 establishes direction only).
- Tutorial skip control, Settings, parent gate, volume controls, parent progress view.
- Difficulty ladders for modes that don't exist yet.
- Everything else already in M1's deferred list unaffected by this milestone (other-language support, battery/tablet/low-end checks, release packaging, generic activity engine).

## Risks and how the plan handles them

- **Task 1 could balloon into a generic layout-validation framework.** Handled by scoping the new test(s) to the specific known Tasks 9-11 failure mode and reviewing existing literals individually rather than a blanket replacement pass.
- **Task 2 is subjective.** Handled by making the review-then-minimum-change structure explicit, requiring a recorded visual-language note (not just prettier isolated assets), keeping changes small and representative, and treating the user's on-device judgment as the actual gate rather than an internal checklist.
- **Task 3 conflates two concerns (difficulty, pacing).** Handled deliberately — spec §6 argues they need to be tuned together, not separately — and de-risked further by fully specifying the ladder's numbers, its genuine rolling-window evaluation/reset behavior, its boundary-clamp handling, and its choice-generation algorithm here instead of leaving them to implementation discretion. Kept simple by scoping the window and the distribution/choice logic to small local code rather than generic abstractions.
- **Task 3's level changes could feel arbitrary to the child.** Handled by the explicit on-device UX check in the task, reusing existing feedback (Eva's reaction/voice) rather than building new progression UI.
- **Task 3's uniform-distribution risk (level 4 not feeling harder than level 3).** Handled explicitly rather than left to chance: levels 3-4 use a max-of-two-draws skew instead of uniform selection, plus a dedicated on-device check that level 4 actually reads as harder.
- **Task 3's answer-choice generation could under-fill on a random collision.** Handled by the deterministic pool-then-sample algorithm (spec §4.3), which is provably always large enough at every level, plus a test proving the exact count every time.
- **Task 4 scope creep, quiet coupling to gameplay state, or over-testing presentation.** Bounded to exactly three additions, an explicit coin-logic/coin-presentation separation, a no-new-gameplay-machinery rule, and an explicit instruction not to add test scaffolding just to make visuals testable — the already-counted-tap invariant is proven at the rules layer, which already exists to prove it; anything else noticed goes to the M2 summary's M3-inputs section, not into this task.
- **Task 5 eating the milestone's time.** Sequenced last, marked optional, and explicitly scoped to "add a few more rows" only; Task 6's summary must not claim M2 success on the basis of Task 5 alone if Tasks 1-4 are weak.

## Self-review

- **Spec coverage:** every spec §4 subsection (4.1-4.5) maps to exactly one task (1-5); §5's acceptance gate maps to Task 6; §3's non-goals are restated as this plan's deferred list.
- **Placeholder scan:** every task names the files/patterns it extends and the check that closes it out; no task is a bare restatement of the spec without an implementation path.
- **Type/name consistency:** `EvaLayout`, `Counting.cs`, `MaxQuantityByRound`, `FurnitureCatalog`, `CoinPayout.MinSessionPayout`, `CheapestPrice`, `Cheer` animation, and all file names match M1's actual shipped names (verified against the committed M1 code, not the M1 plan's pre-implementation names).
