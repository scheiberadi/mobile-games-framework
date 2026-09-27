# Building activity list (School game list, reusable for other buildings)

Date: 2026-09-25. Status: design approved by the user with six adjustments (folded in below); implementation plan to follow, no code yet. Part of Eva's Learning World (`EvasLearningWorld/`).

## Goal

Tapping School on the Map currently opens the Count game directly. Replace that with a simple, reusable "building activity list": a screen that shows one big picture tile per activity the building offers, so more games can be added later and other buildings (the Store, later) can use the same pattern. It must feel like stepping into a place, not like a generic app launcher.

## Decisions (from the design conversation)

- Presentation: a grid of big picture tiles on the building's own backdrop. Not objects-in-the-room, not a carousel (no swiping in child-facing screens).
- A tap on a tile starts the activity at once. No select-then-confirm.
- The list shows only activities that exist. No locks, no greyed-out or "coming soon" tiles (the parent spec has no progression locks).
- Deliberately simple: no activity progress, no locks, no inventory-like machinery, no categories, no larger framework. One catalogue, one screen class.

## Data (Rules layer, pure C#)

`Activities` (new file `Rules/Activities.cs`):

- `enum BuildingId { School }` (only School now; the Store joins when its second activity exists).
- `sealed class Activity { string Id; BuildingId Building; string ScreenKey; string IconSprite; string VoiceKey; }`. `ScreenKey` is the name of the app-layer `ScreenId` (kept as a string so the Rules assembly stays engine-free and does not reference the App layer).
- `static IReadOnlyList<Activity> Activities.For(BuildingId building)`: the activities of that building in their declared, fixed order. Order is part of the contract (it is the tile order on screen).
- Content today: one entry, `count` (School, screen `Count`, icon `activities/count`, voice `activity_count`).

`TileLayout` (new, `Rules/TileLayout.cs`): `static Rect[] TileLayout.Compute(int count)` returns tile rectangles in canvas units, centred in the content area (below), so layout is testable without Unity UI:

| count | columns x rows | tile side |
|---|---|---|
| 1 | 1 x 1 | 480 (large and prominent, fills the content area's height) |
| 2 | 2 x 1 | 440 |
| 3 | 3 x 1 | 380 |
| 4 | 4 x 1 | 270 |
| 5-8 | up to 4 columns x 2 rows, last row centred | 250 |

Gap between tiles 30. More than 8 activities is out of scope (the function throws; scrolling or paging is a later decision). All sides are at least `EvaUi.MinTap` (240).

Content area (canvas units, centre origin, the 1440 x 900 design frame): x from -600 to 600, y from -380 to 170. It is chosen so it never touches the Hud's Home button (top-left, roughly x -690..-450, y 175..415) or the coin counter (top-right), and stays inside the safe area.

## Screen (App layer)

`BuildingScreen : ScreenBase` (new, `App/Screens/BuildingScreen.cs`), constructed with a `BuildingId`:

- Backdrop per building (a small switch: School uses `world/school_list_bg`, used only by this list; the Count game keeps `world/school_bg`). A new building adds a backdrop and nothing else.
- Builds one tile per `Activities.For(building)` entry at the `TileLayout` positions. Each tile is a rounded panel with the activity's picture (a `TapTarget` button, at least 240).
- Tap: start the activity's Eva voice line and open the activity screen in the same frame. The voice line is never awaited; navigation is immediate (the new screen may cut the line off, which is acceptable because the tile picture already says what the game is).
- `OnShow` for School: `Progress.Advance(TutorialEvent.EnteredSchool)` (moved here from `CountScreen.OnShow`), then `Commit`, then `TutorialGuide.Refresh(ScreenId.School)`.
- The global Hud Home button (top-left, goes to the Map) is unchanged.

## Wiring

- `ScreenId` gains `Count`. `School` now maps to `BuildingScreen(BuildingId.School)`; `Count` maps to the existing `CountScreen`.
- `CountScreen`: its own "session home" button after a session returns to the School list (`ScreenId.School`), not the Map. `OnShow` no longer advances the tutorial; it only starts a new session. The Hud Home still goes to the Map.
- `NoReadingAuditTests.Screens` gains `ScreenId.Count`.
- Store: unchanged now. It becomes a `BuildingScreen` only when it has two activities (shop plus the shopping game).

## Tutorial

- `GoToSchool` advances when the School list opens (`EnteredSchool`), as above.
- `TutorialGuide.Plan`: `FirstGame` on `ScreenId.School` returns the voice line `map_count` (or reuses `activity_count`; decided in the plan after checking the voice script) and a new target `GuideTarget.CountTile`, which pulses the hand on the first tile of the School list. `FirstGame` on `ScreenId.Count` stays "no line, no pointing" (Count owns its own in-play help ladder).
- The session flow is unchanged: `RoundsFinished` still advances `FirstGame` to `GoToStore`.

## Art and voice

- The Count tile must say "counting" without reading. The user supplied the art: `counting_tile.png` (classroom scene with three apples, three stars and stacked blocks), imported as `activities/count` (centre-cropped square, 1024, rounded corners, thin brown outline), and `School_background.png` (empty classroom), imported as `world/school_list_bg` for the list backdrop only. `tools/art-import/import-activity-art.js` does the import.
- One new voice line, `activity_count` (for example "Let's count!"), added to `Resources/Voice/voice-lines.txt`. The audio file is produced by the existing voice pipeline (`tools/voice`, needs the TTS key held by the user's environment); until then `Voice.Say` for a missing clip must not crash (verify in the plan).

## Testing

Rules (edit mode, engine-free):

- `Activities.For(School)`: non-empty, ids unique, every entry has building, screen key, icon and voice key.
- Fixed order: `Activities.For(building)` returns entries in exactly their declared order, and repeated calls return the same order (a test compares against the expected id sequence, not only uniqueness). Once a second School activity exists the same test covers it.
- `TileLayout.Compute(n)` for n = 1 to 8: every side at least 240; no two tiles overlap; every tile inside the content area; tiles are centred; n = 1 is the largest tile (side 480); n = 9 throws.

App (edit mode, via the existing test canvas):

- Tap audit for the School list: every tile is at least 240; tiles do not overlap; every tile lies inside the content area and the safe area; no tile intersects the Hud Home button rectangle or the coin counter rectangle (a stricter rule than the 20-unit tolerance used elsewhere, because tiles have room to avoid them entirely). `ScreenId.School` and `ScreenId.Count` are added to the all-screens audits (digits only, no text).
- Navigation: tapping the Count tile shows `ScreenId.Count` in the same call, and starts `activity_count` (Voice last key), without waiting for it to finish.
- Tutorial: opening the School list at `GoToSchool` advances to `FirstGame`; `Plan(FirstGame, School)` returns the Count tile target; `Plan(FirstGame, Count)` returns nothing; Count's session-home returns to `ScreenId.School`.
- The existing suite (251 tests) keeps passing.

Phone review remains the real gate: is the single large Count tile prominent and obviously "counting", and does the list feel like the school rather than a menu?

## Out of scope

Locks, progress or stars on tiles, categories, "coming soon" tiles, swipe or paging, more than eight activities, changing the Store, the Map rework, new games.
