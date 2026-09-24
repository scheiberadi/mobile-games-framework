# M2 Summary — Eva's Learning World, polish

Plan: `docs/superpowers/plans/2026-09-23-eva-m2-polish.md`, spec: `docs/superpowers/specs/2026-09-23-eva-m2-polish-design.md`. Executed with `superpowers:subagent-driven-development` in the worktree branch `worktree-agent-a6f3beb2fd133ebe5` (base `eb22621`). Full history lives in the git-ignored `.superpowers/sdd/2026-09-23-eva-m2-polish/` workspace; this file is the durable record.

## Task verdicts

1. **Bug and layout fixes** — complete after 1 fix round. `EvaLayout` (1440x900) constants, Store/Creator/House tap-target tests, Store `GrowIn` coroutine tracking, stale doc comment, fallback canvas.
2. **Visual direction** — **deferred by the user**, not done. See findings below. The visual-language note was not written.
3. **Difficulty ladder** — complete. Rolling 5-round window, up at >=4 clean, down at >=3 demonstrated, one step at a time, persisted in `PlayerProgress`. Ladder later extended to 6 levels (see Task 7).
   - Task 3b (found on device): the level table asked for quantities up to 20 with 4 answer tiles, but the whole Count mode only supported 1-5 objects and 3 tiles, so level 3 crashed (`ArgumentOutOfRangeException` in `CountLayout.Positions`). Fixed by building the content: object layouts up to 20, four-tile answer row, numeral-only tiles above 5, and 15 new voice clips (`num_6`..`num_20`), plus a regression test over levels 1-4 that checks layout, choice count and voice-file existence.
4. **Feedback and celebration juice** — complete, review clean. Correct-tile pop, coin fly to the HUD (balance committed before the animation), bigger Eva reaction on clean rounds. Device-verified: pop and coin fly seen, cheer is a scale punch only.
5. **Catalog expansion** — **not done, by finding.** The Store is a fixed 3x2 grid already full with the six buyable items (a 7th overlaps row 2, a 4th column crowds the Home/Bubble buttons) and every House slot kind is already filled, so new items would need new House slots and a re-gridded shelf. That is a model change, outside "add a few rows". Deferred to M3.
6. **Acceptance and summary** — this document. See acceptance below.

### Work added during on-device sessions (user-requested)

- Whiteboard in the School background (replaces two leftover placeholder rectangles); counting objects sit inside it, smaller.
- Scattered object placement on every level (best-candidate sampling across the whole board, deterministic per seed, no overlaps, sizes shrink with item count from 200 down to 100).
- Levels 5 and 6: mixed-in distractor objects (level 5: target up to 10 plus 2-6 of one other type; level 6: target up to 15 plus 3-8 of one or two other types; total <= 20). Distractors are never counted, tapping one only wobbles it and is never a mistake; Hint/Demonstrate hand walks only the asked objects.
- Pip dots removed from answer tiles (numerals only).
- Voice pacing: every spoken line now waits out its full clip plus a 0.35s breath; hand-count waits per number and for the lead-in line; the number is spoken before the cheer after a correct answer; `num_5`, `num_7`, `num_11` regenerated because the first generations had long silence baked in (TTS output varies run to run).

## Acceptance

**Technical (necessary, not sufficient):**
- Eva editmode suite: 227/227 passing on the final build; repo-root suite 187/187 (run after Task 7b's predecessor commit; the later commits touch only Eva code).
- Ladder coverage (window not evaluated before 5 rounds, sliding window, buffer cleared only on real level change, clamped boundaries keep the window rolling, one-step changes, exact-count choice generator) is covered in `CountingTests`, extended for the level 6 clamp.
- A1-A4 automated criteria unchanged and green.

**Product gate (the actual M2 gate, user's judgement):** *pending an explicit verdict from the user.* Feedback so far during device sessions: juice reads well, levels 3-6 work, scatter and whiteboard "look good, great job". No overall "charming/coherent/understandable/fun" verdict has been given yet.

## Findings from the on-device review (Task 2 material)

- The user's own verdict on the art: it looks like placeholder quality ("looks like crap"), and they chose to defer the visual pass.
- House: two rooms with a hard vertical divider and mismatched tones, plus several overlapping translucent "ghost" rectangles that look like debug artifacts, not clear drop-target hints.
- Store: consistent two-tone motif, legible price tags, same rounded/thick-outline language.
- Eva has a stray white rectangle on her left leg in School.
- Debug-only `FpsCounter` (top-right number, `.dev` package id) is not a UI element.
- Speech-locked input is intended (tiles are disabled while Eva speaks the question and the post-correct lines); the user is fine with it for now.

## M3 inputs

- **Art pass**: Eva must be an **actual, realistic cat (four-legged animal), not a cat-person**; the current upright humanoid rig with a cat head is placeholder only. The rig and animations (talk, cheer, hop) will likely need rebuilding for a real cat's anatomy. Replace placeholder art everywhere (House divider and ghost rectangles, character, objects, backgrounds).
- **Catalog expansion** needs a small model change first: more House slots and a re-gridded/paginated Store shelf.
- Known cosmetic items: rapid distractor taps can stack wobbles; the biggest rounds (about 19-20 items) can still line up by chance at 100-unit slots; tick badge is small on the smallest slots; `NoReadingAuditTests.cs:38` hard-codes `new Vector2(1440f, 900f)` instead of `EvaLayout`.
- Test gaps: no screen-level test runs a level 5-6 round (the shared test helper counts distractor slots as objects), and no SaveStore test covers the DifficultyLevel 1..6 clamp or buffer round-trip.
- Object-tap counting voice: each new number cuts off the previous clip by design (child-driven taps); only the automated hand-count waits.

## Rulings made during M2

1. All tasks continue in Task 1's worktree/branch; a single merge into `eva-m1` at the end.
2. The difficulty level and rolling buffer persist across sessions in `PlayerProgress`.
3. "Clean round" for the bigger cheer is `_ladder.Step != HelpStep.Demonstrate`, the same definition the ladder uses.
4. The `NoReadingAuditTests.cs:38` literal was deliberately not fixed in Task 1.
5. Task 5 was stopped by the implementer per its brief (needs a model change) and skipped per the plan's own "optional" clause; user was told.
6. Counting slots (object icons) are exempt from the 240-unit minimum tap size; answers go through the tiles. Floor for slot size is 100 units.
7. Level 5 and 6 definitions (ranges above) and "distractor taps are never a mistake".
8. Voice breath pause of 0.35s after every spoken line.
9. Final-review fixes: a correct tap now locks all tiles (including wrong ones a Hint re-enabled) and objects for the rest of the round, and the ladder result and coins are settled at the tap, before the voice lines, with the coin-fly running concurrently.
