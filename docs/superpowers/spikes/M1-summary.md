# M1 Summary — Eva's Learning World, vertical slice

Plan: `docs/superpowers/plans/2026-09-21-eva-m1-vertical-slice.md`. Executed via `superpowers:subagent-driven-development`; full task-by-task history (implementer reports, review verdicts, fix rounds, rulings) lives in `.superpowers/sdd/2026-09-21-eva-m1-vertical-slice/progress.md` (git-ignored workspace, not part of this summary's permanence — this file is the durable record).

Nothing was committed during M1 execution (standing user rule). The working tree currently holds all 13 tasks' work product, uncommitted.

## Task verdicts

1. **App shell** (canvas, safe area, navigator, Hud, Voice, Sfx, no-reading audit) — complete, review clean.
2. **Counting/help/coin rules** (engine-free) — complete, review clean, 95/95.
3. **Progress/House/Furniture/Tutorial rules + JSON save** — complete, review clean, 139/139.
4. **Voice-line file + generator** — complete; real generation run done, all 35 clips produced, user confirmed "Sounds are awesome."
5. **Art** (icons/objects/world backgrounds + importer) — complete after 1 fix round (icon rasterized at wrong size), 142/142. Device-confirmed OK.
6. **Character rig on RectTransforms** — complete after 1 fix round (ArtTests coverage), 151/151. Biggest technical risk of the plan; held on the first attempt, no SpriteRenderer fallback needed.
7. **Count the Objects, core round** — complete, review clean, 155/155.
8. **Help ladder** (Hint/Demonstrate/first-session intro) — complete, review clean, 163/163. Proved by integration test that a 3rd mistake is always reachable and pays 1 coin.
9. **Character creator + first-run flow** — complete after 1 fix round (Confirm button and shirt row cropped below the real 900-unit-tall canvas floor), 164/164.
10. **House** (drag furniture into slots) — complete after 1 fix round (Critical: a placed item's rect occluded a tray item's tap target, fixed by reordering render/raycast priority; plus tray x-bound and overflow fixes), 165/165.
11. **Store** (buy furniture) — complete after 1 fix round (Critical: two shelf items overlapped the fixed Hud Home/Bubble buttons, stealing part of their tap targets — caused by the implementation wrongly assuming a 1600-unit design floor instead of the project's real 1440-unit frame), 167/167.
12. **Tutorial guidance** (Eva shows the way) — complete after 1 fix round (Critical: line-repeat dedup bug; plus a load-bearing pre-existing gap fixed by ruling: `TutorialEvent.EnteredSchool` was never wired anywhere, silently stranding the whole tutorial state machine at `GoToSchool` forever), 198/198.
13. **Acceptance, fixes, this summary** — Steps 1-2 (cold verification) all green, 199/199 (added a voice-completeness test). Step 3 (on-device acceptance) done informally by the user; Step 4 had nothing to fix.

**Recurring lesson across Tasks 9-11:** the production canvas is height-matched at exactly 900 units tall (y ∈ [-450, 450], no vertical slack) and the real on-device design frame is 1440 units wide, not the 1600 the canvas nominally reports (which only exists as horizontal slack on wider screens). Three separate tasks shipped a Critical geometry defect from getting one of these two numbers wrong before review caught it. Worth calling out explicitly for M2: any new screen must check its layout against 1440×900 with the real vertical bound, not against assumed nominal dimensions.

## Acceptance table (A1-A7)

| # | Criterion | Verdict | Notes |
|---|---|---|---|
| A1 | No reading | **Pass (automated)** | `NoReadingAuditTests` (part of the 199 green tests) verifies no letters/undersized targets on every screen. On-device: user completed the full run without reporting any reliance on text; speech-bubble-never-opened not explicitly confirmed. |
| A2 | Independent first run | **Pass (informal)** | User ran the full loop (character → sofa → school → rounds → store → purchase → placement) and reported "seems ok," no hesitation moments called out. Not a rigorous 0-intervention count. |
| A3 | Mistakes are safe and teach | **Pass (informal)** | Automated: Task 8's integration test proves a 3rd mistake always reaches Demonstrate and pays 1 coin, never zero. On-device 3-wrong-tap walkthrough was offered as part of the acceptance script; user's "seems ok" covers it without a detailed per-step report. |
| A4 | Reward is clear and fair | **Pass (automated)** | `CoinPayout.MinSessionPayout >= FurnitureCatalog.CheapestPrice` is an automated test (part of the 199 green tests) — the worst case (every round demonstrated) still pays enough for the cheapest item. The optional on-device "demonstrate every round" stress test was offered but not explicitly run/reported. |
| A5 | Feel (immediate response, no stall, 110+ fps) | **Pass** | User observed the fps digit holding steady around 120 ("doesn't seem to move out of 120"), well above the 110 target — likely the device's own refresh-rate cap rather than a performance ceiling. |
| A6 | Persistence (force-stop/relaunch) | **Pass** | Dedicated 3-checkpoint on-device run (clear save → create character → force-stop/relaunch; earn coins, buy bed, force-stop/relaunch; place bed, force-stop/relaunch). Every checkpoint relaunched cleanly to the correct screen (never fell back to Creator), coins persisted correctly across the purchase, and the user visually confirmed the bed stayed placed in the House. `SaveStoreTests` (automated) separately cover corrupt-save handling and round-trip serialization at the rules layer. |
| A7 | Fun (user's judgement, the actual gate) | **Pass** | User: "M1 is technically approved." M2 (Plan 3) is proceeding on that basis. |

## Measured

- **fps:** steady ~120 (user-observed, "doesn't seem to move out of 120") — comfortably above the 110 target.
- **APK size:** 25,868,853 bytes (~24.7 MB), debug build, compliance-checked (no AD_ID, launchable activity, only `INTERNET` + Unity's own self-scoped receiver permission).
- **Test count:** 199/199 Eva-specific tests green (cold run); 187/187 repo-root (framework/Sudoku/2048) tests green.
- **Hesitation moments (user or child):** none reported.

## M2 inputs (issues found during M1, not blocking, worth M2 attention)

- The 1440×900-vs-1600 layout-frame confusion (see "Recurring lesson" above) — consider a shared constant or a lint-style test that catches this class of bug earlier than task review.
- House screen has no automated 20-unit tap-target overlap test (Creator and Store now have one each; House's Critical z-order finding was caught and fixed but has no regression test of its own — deferred as a Task 10 minor).
- Store: `GrowIn` coroutine not tracked/stopped the way the buy-confirm coroutine is (fast tap→cancel→re-tap could leave a stale one running; cosmetic, self-flagged).
- Store: the guide's pointing hand stays visible/pulsing at the now-hidden shelf position while the buy-confirm dialog is open (self-flagged, no visual clash verified).
- `TutorialGuide.cs`'s class doc comment still says "once per entry to that (step, screen) pairing," stale after the per-step dedup fix — comment-only.
- `EvaGame.cs`'s unused fallback canvas constructor still says 1600×900 (dead code path, not on the real device flow).
- Full A6 persistence checks still need a dedicated on-device pass; A7 (the fun verdict) is still open and is the actual gate for whether M2 starts.

## Deferred out of M1 (unchanged from the plan)

- Difficulty ladder and rolling window (spec 4.7) — M2.
- Tutorial skip control, "time for a break," Settings, parent gate, volume controls, parent progress — M2+.
- Paying in the Store by tapping coins (spec 4.3) — belongs with the Store shopping game.
- Furniture set id/category, more rooms, clothing, pets — when a second collection exists.
- Generic activity engine, content schema/validator, offline AI content generation — when the second mode exists.
- Other-language text, TMP font work, CJK fonts, voice packs, Play asset delivery — M4.
- Real art, final Eva design, music, real sound effects — M2 onward.
- Battery optimisation, tablet layout check, low-end device check, Auto Backup on a Play install — M6 (was M5; the character system took M5 on 2026-09-27).
- Release keystore, package id, store listing — M7 (was M6).

## Status: M1 closed

User approved M1 ("technically approved") and A6 passed its dedicated on-device verification. M1 is committed in four parts (rules+save; art+characters; screens; voice pipeline+lines — see git log). Plan 3 (M2) is being drafted next, focused on making the existing slice noticeably more fun and polished for a 4-5-year-old rather than adding new systems.
