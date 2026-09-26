# Number Hunt (M3 School activity) — spike notes

Branch: `claude/eva-m3-number-hunt-dt83cq` (based on `origin/eva-m1`). Not merged, no PR opened,
per the task brief. Sudoku/2048 and `ProjectSettings/AndroidResolverDependencies.xml` untouched.

## Environment

- No Unity editor is available in this container, and no `tools/run-editmode-tests.sh` exists in
  this repo. All App-layer (UnityEngine) code below is written carefully but **not compiled or
  run** — say so plainly, don't claim it was tested.
- dotnet SDK is not preinstalled but installs cleanly via `sudo apt-get install -y
  dotnet-sdk-8.0` (Ubuntu 24.04 repo has it); this session installed 8.0.131. The `dot.net`
  install script is blocked by the egress proxy, but `apt-get` works.
- Given no Unity: Rules/ code (no UnityEngine references) was compiled and tested in a throwaway
  dotnet nunit project outside the repo (never committed). App/ code cannot be compiled or run
  here at all.

## Commits on this branch

1. `2d6d02e` — Number hunt round generator with a seeded, six-level ladder (`Rules/NumberHunt.cs`
   + `Tests/NumberHuntTests.cs`, 13 tests).
2. `d0dfd31` — Number Hunt gets its own persisted difficulty ladder (`Rules/Progress.cs`,
   `App/Save/SaveStore.cs`, `Tests/SaveStoreTests.cs`, 2 new tests).
3. `0e59799` — Number Hunt screen, wired into School and the audit (`App/Screens/
   NumberHuntScreen.cs` new, plus `Navigator.cs`, `EvaGame.cs`, `Hud.cs`, `Rules/Activities.cs`,
   `Tests/NoReadingAuditTests.cs`, `Resources/Voice/voice-lines.txt`).

## Tests run vs. written-but-not-executed

**Run** (throwaway dotnet nunit project, pure C# Rules files only — copy `NumberHunt.cs`,
`Counting.cs`, `HelpAndCoins.cs`, `Progress.cs`, `Tutorial.cs`, `House.cs`, `Places.cs`,
`Furniture.cs` plus the test file, `dotnet test`):
- `NumberHuntTests.cs` — 13/13 passing (range/count validity per level, previous-target
  avoidance, seeded determinism, out-of-range throws, tile-count monotonic growth, confusable-
  partner absence below level 5 / guaranteed presence from level 5 for all 6 pairs).
- `ActivitiesTests.cs` — 3/3 passing after appending the `numhunt` activity (verifies `list[0].Id
  == "count"` still holds and every activity has non-empty ScreenKey/IconSprite/VoiceKey).
- `Progress.cs` compiles cleanly standalone with the new `NumberHuntLevel`/`NumberHuntBuffer`
  fields.

**Written but not executed** (UnityEngine dependency — cannot run outside Unity):
- `SaveStoreTests.cs`'s two new tests (`NumberHuntLevelClampsOnLoadAndTheBufferRoundTrips`,
  `ASaveFromBeforeNumberHuntExistedLoadsWithADefaultLevelAndAnEmptyBuffer`) — `SaveStore.cs` uses
  `UnityEngine.JsonUtility`.
- `NoReadingAuditTests.cs`'s `Screens[]` addition — exercises `NumberHuntScreen` through the
  existing digits-only / ≥240-tap-target / no-interactive-without-TapTarget audit, but the whole
  test class needs a Unity test runner.
- `NumberHuntScreen.cs` itself — no bespoke UnityTest was added for its Hint/Demonstrate/
  session-end states (mirroring `CountScreenPassesTheAuditInQuestionHintDemonstrationAndSessionEndStates`
  would be a reasonable nice-to-have, **not done** this session, noted as a gap).

## Decisions made for the user (simplest-default choices, per the brief)

1. **Level table** (`Rules/NumberHunt.cs`): levels 1-6 reuse the shared `DifficultyLadder`
   directly (own independent level/buffer, no new ladder class).
   ```
   level:        1   2   3   4   5   6
   max number:   5  10  10  20  20  20
   tile count:   3   4   5   5   6   6
   ```
2. **Confusable-numeral distractors**: from level 5 up, if the target is one of {6,9,2,5,1,7} its
   flip partner (6↔9, 2↔5, 1↔7) is guaranteed as a choice when in range. Two-digit numbers get no
   guaranteed partner — deliberate MVP simplification.
3. **Tile layout**: its own hand-placed grid (not Count's scatter, not the Building menu grid) —
   see "must be verified on the phone" below, this is the least certain part of the work.
   - Frame 1440×900 centred; Eva at `(627,-140)`, height 430, mirrored to face left (same
     constants as `CountScreen`, for visual consistency).
   - Every tile is a flat 240×240 (`EvaUi.MinTap`) at every level — **no** shrink-below-240
     compromise the way Count's 5-6-tile levels have, because Number Hunt has no exemption from
     the no-reading audit's tap-target rule (the tile *is* the answer, not a counting aid).
   - Grid: 3 tiles → one centred row; 4 → two rows of two; 5 → a row of three over a row of two;
     6 → two rows of three. Worked out to keep every tile at y ≤ 120 (clear of the Hud, which
     starts at y=175) and a ~370-unit gap to Eva's x=627 centre.
   - Background reuses `world/school_bg` (same room as Count); did **not** reuse CountScreen's
     private animated sun/cloud window view (extracting it would mean touching CountScreen) —
     optional future polish, not required.
4. **Help ladder**: 1st mistake Retry (wobble + SFX + voice); 2nd Hint (hand points at, does not
   tap, the correct tile, then all tiles re-enable); 3rd Demonstrate (hand taps the correct tile,
   then only that tile is interactive — the child still taps it themselves, per spec 4.6). No
   object-counting phase exists here, so this is simpler than Count's ladder.
5. **Voice lines** — chose to **reuse** Count's `count_retry` / `count_right_1..3` / `count_done`
   / `count_again` verbatim rather than near-duplicate them (they read as generic enough for any
   game), to minimize new recording work. New keys added to `voice-lines.txt` under `# number
   hunt` / the School section:
   ```
   activity_numhunt   Let's find numbers!
   numhunt_find       Find this number!
   numhunt_hint       Look, it's this one!
   numhunt_demo       Watch me tap it!
   ```
   Round sequence: `SayAndWait("numhunt_find")` then `SayAndWait("num_" + target)`, reusing the
   existing `num_1..num_20` clips. **No clips were generated** (not touched `GOOGLE_TTS_API_KEY`,
   never called the TTS service) — `VoiceCompletenessTests` will fail until the user runs voice
   generation for these 4 new keys. This is expected and deliberate.
6. **Tutorial**: Number Hunt is never part of the first-run tutorial (`TutorialGuide.Plan` needs
   no change — its default `(null, GuideTarget.None)` is already correct for every (step, screen)
   pair involving it). Its `EndSession()` therefore does **not** call
   `Progress.Advance(TutorialEvent.RoundsFinished)` the way Count's does — this wasn't explicit in
   the original brief but follows from "never part of tutorial": advancing the tutorial from
   Number Hunt would let a child who taps it before Count during the `FirstGame` step
   accidentally skip Count's own intro. Home from Number Hunt unconditionally returns to the
   School list (no tutorial branch needed, unlike Count).
7. **Activity icon**: `"activities/numhunt"` — no art exists yet, renders as the automatic
   procedural placeholder (a colored rounded rect). ChatGPT prompt for real art:
   > "Flat vector icon, soft-shaded style with gentle gradients and a soft drop shadow, rounded
   > shapes, warm friendly palette matching a children's counting game. A magnifying glass
   > hovering over a large numeral 7, both fully inside a rounded square tile, no text, no other
   > numbers, clean simple silhouette readable at small size, centered composition, transparent
   > or plain white background."
   > Output file: `activities/numhunt.png` (square; check `activities/count.png`'s pixel size
   > first so the new one matches).

## What must be verified on the phone

- **The tile-grid geometry was worked out by arithmetic, never rendered or visually checked.**
  Confirm on-device that all 3/4/5/6-tile layouts sit fully inside the frame, clear the Hud
  (home button top-left, coins top-right) and leave a comfortable gap to Eva, at every level.
- Whether the confusable-pair MVP scope (single digits only, no two-digit pairs like 16/19) is
  acceptable, or worth extending later.
- Normal on-device play-testing: do the 240-unit tiles feel right to a small child's finger; does
  the Hint (point-not-tap) and Demonstrate (tap-then-wait) choreography read clearly; does voice
  pacing (the waits computed from `Voice.Duration(key) + BreathSeconds`) feel natural once real
  clips exist for the new keys.
- Generate the 4 new voice clips (`activity_numhunt`, `numhunt_find`, `numhunt_hint`,
  `numhunt_demo`) via the project's existing voice generation tooling once ready; until then
  `VoiceCompletenessTests` is expected to fail on this branch.
- Optional, not done this session: a UnityTest exercising NumberHuntScreen's Hint/Demonstrate/
  session-end states against the no-reading audit, mirroring Count's own such test.
