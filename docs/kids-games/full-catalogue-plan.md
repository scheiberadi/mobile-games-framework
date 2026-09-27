# Eva's Learning World — full building/game catalogue & implementation tracker

Source: user prompt of 2026-09-26 ("PRODUCT WORLD" / full catalogue), which supersedes and
completes the game-list section of `game-modes-backlog.md` (that file's per-category brainstorm
lists are now folded into the per-building lists below; its DECIDED log at the bottom stays the
source of truth for cross-cutting product decisions).

**How to use this doc:** check it before starting new game work, and update the Status column as
things land, so we never lose track of what's built vs. still owed. Do not delete rows; a
game that's cut gets marked `[cut]` with a one-line reason instead of removed, so the decision is
recorded. Milestones: **M4 = build all of this content** (redefined 2026-09-26; localization,
previously M4, moves to ship alongside M6 release instead).

Status legend: `[x]` implemented and playable end to end · `[~]` in progress · `[ ]` not started.

Branch: `claude/eva-m4-full-content` (based on `claude/eva-m3-number-hunt-dt83cq`, which already
carries Count + Number Hunt — not off bare `eva-m1`, so that work isn't redone).

## Shared reuse mechanics (implementation concern only — never surfaces to the child)

Per the brief: the child always sees BUILDING → GAME, never a mechanic name. These are the
internal interaction patterns multiple games are expected to share:

| Mechanic | Used by (examples) | Status |
|---|---|---|
| **TAP-THE-TARGET** (existing: `HelpLadder`, `DifficultyLadder`, tap-tile pattern from Count/Number Hunt) | Count, Number Hunt, Letter Hunt, most Zoo/Science/Brain Gym single-choice games | `[x]` pattern exists, not yet generalized into a shared presenter base |
| **MATCH** (pick B for shown A) | Animal→Habitat/Mother/Food/Footprint/Covering, Word→Image, Emotion Matching | `[ ]` |
| **SORT** (bucket items by rule) | Domestic vs Wild, Land/Sea/Air, Healthy vs Unhealthy, recycling-style sorts | `[ ]` |
| **SEQUENCE** (arrange in order) | Plant Growth, Number Ordering, Sequence Ordering | `[x]` generalized as `SequenceRoundBuilder`/`SequenceScreen` (M4.5 Plant Growth); Number Ordering keeps its own numeral-specific code |
| **DRAG & DROP** (existing: `DragItem`, House placement) | Furniture (done), Jigsaw, Dressing, Shopping, Building games, Cleaning | `[x]` `DragItem`/House slot-snap exists; not yet generalized beyond furniture |
| **NAVIGATION** (path from A to B) | Finger Maze, Avoid Obstacles, Treasure Hunt, Shortest Path | `[ ]` |
| **TRACE** (finger follows a shape/letter/number) | Trace Shapes/Letters/Numbers | `[ ]` |
| **CHOOSE** (listen, pick the matching option) | Listen and Choose, science/social scenario games | `[ ]` overlaps heavily with TAP-THE-TARGET; may not need its own mechanic |
| **BUILD → TEST → OBSERVE** | Workshop's Build-a-X games, Bridge Building, Balance | `[ ]` |

Extraction happens opportunistically: per the existing house rule, don't build a shared mechanic
until the 2nd or 3rd game that needs it shows what actually repeats.

## 1. House

House is furniture-only — the meta-game/reward destination, not a place with its own games
(user direction, 2026-09-26, overrides the original brief's House game list below).

- [ ] More furniture (catalog expansion — tracked separately, not a "game")

Already implemented: character creator, persistent house with room slots, furniture drag-drop
placement, coins/progression meta-layer.

`[cut]` Morning Routine, Clean Your Room, Cook a Meal, Clock, Calendar — explicitly out of scope
(user decision, 2026-09-26): House carries no games and these five don't move elsewhere either.

## 2. School — `BuildingId.School`

| id | Game | Domain | Skills | Mechanic | Status |
|---|---|---|---|---|---|
| `COUNT_OBJECTS` | Count the Objects | Mathematics | counting, number recognition | TAP-THE-TARGET (bespoke, has its own object-counting phase) | `[x]` |
| `NUMBER_HUNT` | Number Hunt | Mathematics | number recognition | TAP-THE-TARGET | `[x]` |
| `LETTER_HUNT` | Letter Hunt | Literacy | letter recognition | TAP-THE-TARGET (reuses Number Hunt's shape almost exactly, target letter spoken not shown) | `[ ]` built, awaiting Unity pass |
| `ADDITION` | Addition | Mathematics | addition | TAP-THE-TARGET, visual objects → symbolic later | `[ ]` built, awaiting Unity pass |
| `SUBTRACTION` | Subtraction | Mathematics | subtraction | TAP-THE-TARGET, visible removal → symbolic later | `[ ]` built, awaiting Unity pass |
| `MULTIPLICATION` | Multiplication | Mathematics | multiplication | TAP-THE-TARGET, visual groups (rows×cols) → notation later | `[ ]` built, awaiting Unity pass |
| `NUMBER_ORDERING` | Number Ordering | Mathematics | ordering | SEQUENCE, ascending → descending | `[ ]` built, awaiting Unity pass |
| `MISSING_NUMBER` | Missing Number | Mathematics | arithmetic, algebraic thinking | TAP-THE-TARGET (2 + ? = 5) | `[ ]` built, awaiting Unity pass |
| `ONE_MORE_ONE_LESS` | One More / One Less | Mathematics | counting, +/-1 | DRAG & DROP → numeric answer later | `[ ]` built, awaiting Unity pass |
| `WHICH_HAS_MORE` | Which Has More? | Mathematics | comparison | TAP-THE-TARGET, later "how many more" | `[ ]` built, awaiting Unity pass |
| `NUMBER_LINE` | Number Line | Mathematics | number line, +/- | own mechanic (character hops N spaces) | `[ ]` built, awaiting Unity pass |
| `UPPER_LOWER_CASE` | Uppercase to Lowercase | Literacy | letter recognition | MATCH | `[ ]` built, awaiting Unity pass |
| `LETTER_TO_SOUND` | Letter to Sound | Literacy | phonics | MATCH (audio-led; later blends into C+A+T=CAT) | `[ ]` built, awaiting Unity pass |
| `BUILD_A_WORD` | Build a Word | Literacy | phonics, spelling | TAP-THE-TARGET (CA_ with letter choices) | `[ ]` built, awaiting Unity pass |
| `SCRAMBLED_WORD` | Scrambled Word | Literacy | spelling | DRAG & DROP (reorder letters) | `[ ]` built, awaiting Unity pass |
| `MISSING_LETTER` | Missing Letter | Literacy | spelling | TAP-THE-TARGET (C_T) | `[ ]` built, awaiting Unity pass |
| `BEGINNING_SOUND` | Beginning Sound | Literacy | phonics | MATCH (audio-led) | `[ ]` built, awaiting Unity pass |
| `RHYMING` | Rhyming | Literacy | phonological awareness | MATCH (audio-led) | `[ ]` built, awaiting Unity pass |
| `WORD_TO_IMAGE` | Word to Image | Literacy | reading readiness | MATCH | `[ ]` built, awaiting Unity pass |
| `IMAGE_TO_WORD` | Image to Word | Literacy | reading readiness | MATCH | `[ ]` built, awaiting Unity pass |
| `SENTENCE_BUILDER` | Simple Sentence Builder | Literacy | sentence construction | DRAG & DROP (pictograms → words) | `[ ]` built, awaiting Unity pass |

Note: brief's item L ("Counting/Number Hunt variants — reuse the existing system") is a build
instruction, not a distinct game — folded into how the above are implemented, not a catalogue row.

The 10 Literacy rows above were missing from the original big-catalogue prompt entirely (2026-09-26
scope audit finding, `docs/kids-games/m4-scope-audit.md`) — only Letter Hunt had made it in. Added
here under School since Literacy is already one of School's declared domains (design doc 4.8).

## 3. Playground — new `PlaceId`/`BuildingId`, doesn't exist yet

Standing up Playground itself is a prerequisite for every row below: add `PlaceId.Playground` to
`Rules/Places.cs` (map road/standing-spot/tap-box/sprite), a `ScreenId.Playground` +
`BuildingScreen`-equivalent list screen, and its own `activities/*` icons.

| id | Game | Domain | Skills | Mechanic | Status |
|---|---|---|---|---|---|
| `FINGER_MAZE` | Finger Maze | Logic/spatial | path planning | NAVIGATION | `[ ]` |
| `JIGSAW` | Jigsaw | Logic/spatial | spatial reasoning | DRAG & DROP + snapping, 4→25+ pieces | `[ ]` |
| `PATTERN_COMPLETION` | Pattern Completion | Logic/spatial | patterns | TAP-THE-TARGET, AB→ABB→ABC | `[ ]` |
| `ODD_ONE_OUT` | Odd One Out | Logic/spatial | classification | TAP-THE-TARGET | `[ ]` |
| `WHATS_MISSING` | What's Missing? | Logic/spatial, memory | sequence/set recall | TAP-THE-TARGET | `[ ]` |
| `WHICH_DOESNT_MAKE_SENSE` | Which Doesn't Make Sense? | Logic/spatial | classification | TAP-THE-TARGET | `[ ]` |
| `SHORTEST_PATH` | Shortest Path | Logic/spatial | path planning | NAVIGATION (route comparison) | `[ ]` |
| `AVOID_OBSTACLES` | Avoid Obstacles | Logic/spatial | path planning | NAVIGATION | `[ ]` |
| `COLLECT_EVERYTHING` | Collect Everything | Logic/spatial | path planning | NAVIGATION | `[ ]` |
| `ITEM_TO_SHADOW` | Item to Shadow | Logic/spatial | classification, shape recognition | MATCH (object → its silhouette) | `[ ]` added by the audit |
| `TANGRAM_CONSTRUCTION` | Tangram / Puzzle Blocks | Logic/spatial | spatial reasoning | DRAG & DROP (free-form shapes against a ghost silhouette, distinct from Jigsaw's cut-photo reassembly) | `[ ]` added by the audit |
| `ROTATE_THE_PIECE` | Rotate the Piece | Logic/spatial | spatial reasoning, mental rotation | own mechanic (interactive rotate, not just a judgment call like Brain Gym's Match Rotation) | `[ ]` added by the audit |
| `FOLLOW_NUMBERS_IN_ORDER` | Follow Numbers in Order | Mathematics, Logic/spatial | path planning, number ordering | NAVIGATION (maze with numbered checkpoints, ordered) | `[ ]` added by the audit |
| `FOLLOW_LETTERS_IN_ORDER` | Follow Letters in Order | Literacy, Logic/spatial | path planning, letter ordering | NAVIGATION (maze with lettered checkpoints, ordered) | `[ ]` added by the audit |

Item to Shadow/Tangram/Rotate the Piece/Follow Numbers-Letters in Order were missing from the
original big-catalogue prompt (2026-09-26 audit finding) but have an unambiguous fit here
(spatial reasoning / pathfinding, same domain as the rest of Playground).

Build order note: the original M3 plan already picked Finger Maze/Jigsaw/Pattern Completion as
Playground's first 2-3 — the brief now asks for all 9, so Playground alone is roughly as big as
School.

## 4. Store — not split into `BuildingId`/Activities (see the Shopping narrative below for why); its
## non-shelf games are reached via a new, separate `StoreActivitiesScreen` tile menu instead

| id | Game | Domain | Skills | Mechanic | Status |
|---|---|---|---|---|---|
| — | Furniture Store | (meta) | — | DRAG & DROP-adjacent (buy → placeable in House) | `[x]` |
| `SHOPPING` | Shopping Game | Mathematics | coins, notes, addition, subtraction, prices, change, budgeting | own mechanic (shop interaction, not an equation screen); levels 1-7 per the brief (coins → notes → exact payment → +→ −/change → compare prices → budget), compressed onto the existing 6-level `DifficultyLadder` (see narrative below) | `[ ]` built, awaiting Unity pass |
| `DRESS_THE_CHARACTER` | Dress the Character | Executive function | fine motor, categorization | DRAG & DROP (clothes onto the character) | `[ ]` built, awaiting Unity pass |
| `DRESS_FOR_OCCASION` | Dress for the Occasion | Executive function | categorization | MATCH/DRAG & DROP (school, beach, winter, birthday, sports, camping → matching outfit) | `[ ]` built, awaiting Unity pass |
| `PACK_A_SUITCASE` | Pack a Suitcase | Executive function | planning, categorization | DRAG & DROP (pick items appropriate to a trip) | `[ ]` built, awaiting Unity pass |

The dressing-game cluster reuses the Furniture Store's own buy/browse presentation shape (a shelf
of choices) with `DragItem` for the dressing interaction itself — assigned to Store since it's the
closest existing "browse and apply cosmetic items" building, per user decision 2026-09-26.

## 5. Arcade — new building, doesn't exist yet

| id | Game | Mechanic | Status |
|---|---|---|---|
| `BALLOON_POPPING` | Balloon Popping | TAP-THE-TARGET on moving targets; content rule decides valid pops (even numbers, >5, target letter, etc.) | `[ ]` |
| `FRUIT_CATCHER` | Fruit Catcher | own mechanic (moving basket, drag/swipe) | `[ ]` |
| `SPACE_SHOOTER` | Space Shooter | own mechanic (aim + shoot the correct visual answer) | `[ ]` |
| `FISHING` | Fishing | own mechanic (catch by tap/drag, content rule on valid catch) | `[ ]` |
| `WHACK_A_MOLE` | Whack-a-Mole | TAP-THE-TARGET, timed pop-up | `[ ]` |
| `PLATFORMER` | Platformer | NAVIGATION-adjacent (sequenced jumps through numbered/lettered platforms) | `[ ]` |
| `TREASURE_HUNT` | Treasure Hunt | meta-mechanic: chains several other mechanics' small challenges into one run | `[ ]` |

Arcade's rule per the brief: fun-first, the educational rule layers on top of a reusable arcade
mechanic — so this building is where TAP-THE-TARGET/NAVIGATION get reskinned as arcade games
rather than needing all-new mechanics, except Fruit Catcher/Space Shooter/Fishing which are
themselves new movement mechanics reused across their few games.

## 6. Zoo & Farm — new building, doesn't exist yet

| id | Game | Mechanic | Status |
|---|---|---|---|
| `ANIMAL_HABITAT` | Animal → Habitat | MATCH | `[ ]` built, awaiting Unity pass |
| `ANIMAL_MOTHER` | Animal → Mother | MATCH | `[ ]` built, awaiting Unity pass |
| `ANIMAL_FOOD` | Animal → Food | MATCH | `[ ]` built, awaiting Unity pass |
| `ANIMAL_SOUND` | Animal → Sound | MATCH (audio-led) | `[ ]` built, awaiting Unity pass |
| `ANIMAL_FOOTPRINT` | Animal → Footprint | MATCH | `[ ]` built, awaiting Unity pass |
| `ANIMAL_COVERING` | Animal → Body Covering | MATCH | `[ ]` built, awaiting Unity pass |
| `DOMESTIC_VS_WILD` | Domestic vs Wild | SORT | `[ ]` built (as one-animal-at-a-time tap-the-bucket, see note below), awaiting Unity pass |
| `LAND_SEA_AIR` | Land / Sea / Air | SORT | `[ ]` built (tap-the-bucket, see note below), awaiting Unity pass |
| `ANIMAL_BABIES` | Animal Babies | MATCH/CHOOSE (identify baby vs adult) | `[ ]` built, awaiting Unity pass |
| `ANIMAL_CLASSIFICATION` | Animal Classification | SORT (multi-attribute, higher levels) | `[ ]` built (tap-the-bucket, 2→3→4 buckets by level), awaiting Unity pass |
| `GEOGRAPHY` | Geography | MATCH/CHOOSE (which is Romania, continents, flags, landmarks, animals by continent, foods by country, globe) | `[ ]` built (Flag→Continent→Landmark by level), awaiting Unity pass |

All ten animal rows share one MATCH/SORT presenter over a common animal-content dataset (id,
habitat, mother, food, sound key, footprint sprite, covering, domestic/wild, land/sea/air) — built
once as `Rules/MatchRoundBuilder.cs` (the round shape) + `App/Screens/MatchScreen.cs` (the
presenter), configured per game in `EvaGame.cs`. Geography (`Rules/Geography.cs`) has its own
11-country/6-continent dataset but reuses the same builder and presenter.

**Implementation decision (2026-09-27, flagged for Adrian's review same as the StoreActivitiesScreen
call):** the three SORT games (Domestic vs Wild, Land/Sea/Air, Animal Classification) are implemented
as one-animal-at-a-time "tap the correct bucket" rounds through the same MatchScreen presenter,
rather than a batch drag-multiple-animals-into-buckets screen. A bucket is just another MATCH choice
value that happens to be shared by several animals (no different from "farm" being shared by Cow and
Sheep for Habitat), so this reuses 100% of the MATCH presenter instead of building a second,
bespoke SORT UI - lower risk, and still exercises the same classification skill. Content (15
animals, 11 countries) is placeholder, same caveat as every dataset built this milestone - pending a
real art/content pass, and nothing in M4 has had a Unity build or phone test yet.

## 7. Science Lab — `BuildingId.ScienceLab`

| id | Game | Mechanic | Status |
|---|---|---|---|
| `SINK_OR_FLOAT` | Sink or Float | SORT (built as tap-the-bucket, see note below) | `[ ]` built, awaiting Unity pass |
| `MAGNET` | Magnet Game | SORT (built as tap-the-bucket, see note below) | `[ ]` built, awaiting Unity pass |
| `LIVING_VS_NONLIVING` | Living vs Non-Living | SORT | `[ ]` built, awaiting Unity pass |
| `PLANT_GROWTH` | Plant Growth | SEQUENCE (first game to need it - new shared `SequenceScreen` presenter, see note below) | `[ ]` built, awaiting Unity pass |
| `HUMAN_SENSES` | Human Body / Senses | MATCH (sense organ → sense) | `[ ]` built, awaiting Unity pass |
| `HEALTHY_VS_UNHEALTHY` | Healthy vs Unhealthy | SORT | `[ ]` built, awaiting Unity pass |
| `WEATHER` | Weather | MATCH (audio-led, same shape as Zoo & Farm's Animal → Sound) | `[ ]` built, awaiting Unity pass |
| `DRESS_FOR_WEATHER` | Dress for the Weather | MATCH/CHOOSE | `[ ]` built, awaiting Unity pass |
| `CAUSE_AND_EFFECT` | Cause and Effect | MATCH (built as "pick the matching effect picture", see note below) | `[ ]` built, awaiting Unity pass |
| `COOKING_MEASURES` | Cooking Measures | SORT (built as tap-the-bucket: full/half/empty, see note below) | `[ ]` built, awaiting Unity pass |
| `SEASONS` | Seasons | MATCH (scene/activity → season) | `[ ]` built, awaiting Unity pass |
| `DAY_NIGHT` | Day/Night Activities | MATCH (activity → time of day) | `[ ]` built, awaiting Unity pass |
| `SPACE` | Space | MATCH (audio-led; planet order/gravity levels deferred, see note below) | `[ ]` built, awaiting Unity pass |

Cooking Measures/Seasons/Day-Night/Space were unassigned after the first audit pass (no building
fit "world knowledge" beyond animals); assigned to Science Lab 2026-09-26. Seasons/Day-Night/Space
reuse the MATCH presenter Weather/Human Senses already establish; Cooking Measures is closer to
Sink or Float/Magnet's predict-and-observe shape.

**Implementation decisions (2026-09-27, flagged for Adrian's review, same pattern as the
StoreActivitiesScreen and Zoo & Farm SORT-via-MATCH calls):**
- Twelve of the thirteen games reuse Zoo & Farm's MatchScreen presenter, each with its own small
  (id, value) dataset (`Rules/ScienceLab.cs`) rather than one shared entity table (unlike Zoo &
  Farm's animals, a sink/float object or a weather condition doesn't have many attributes to key
  games off of). Sink or Float and Magnet are tap-the-bucket predictions rather than their own
  drop-and-observe physics simulation; Cause and Effect is "pick the matching effect picture"
  rather than Workshop's own BUILD→TEST→OBSERVE shape (which doesn't exist yet either); Cooking
  Measures is a full/half/empty bucket sort over nine cup sprites rather than interactive
  measuring cups; Space is audio-led planet/object identification only - the plan's own
  "planet size and order, gravity" later-level ideas are deferred, not built. All reuse the
  presenter fully at the cost of the interactive-simulation flavor the plan's mechanic column
  describes - lower risk, ships now, still teaches the same prediction/matching skill.
- Plant Growth is the first SEQUENCE game the catalogue needed (the reuse-mechanics table above
  lists it, nothing had built it yet). Built as `Rules/SequenceRoundBuilder.cs` +
  `App/Screens/SequenceScreen.cs`, generalizing Number Ordering's own tile-grid-tap-in-order shape
  (numeral tiles → sprite tiles) the same way MatchScreen generalized WordToImageScreen. Five
  ordered stages (seed/sprout/seedling/flower/fruit); low levels play a 3-stage prefix, higher
  levels the full 5.
- Content across every dataset is placeholder (objects, foods, activities, cup fill levels, space
  objects), pending a real art/content pass - same caveat as every dataset built this milestone.
  Nothing in M4 has had a Unity build or phone test yet.
- Science Lab's map placement (`Rules/Places.cs`): mirrors Zoo & Farm's road shape on the opposite
  (east) side of the map, dipping north first to clear the Store's tap box/road corridor before
  running east then north to the building. Had to update `PlacesTests.cs` and
  `tools/art-import/places-layout.json` (both hardcode the place count/layout), same as every
  building added this milestone.

## 8. Workshop — new building, doesn't exist yet

Shared pattern: **BUILD → TEST → OBSERVE RESULT**, the test outcome feeding the reward loop.

| id | Game | Mechanic | Status |
|---|---|---|---|
| `BUILD_A_CAR` | Build a Car | DRAG & DROP assembly + BUILD→TEST | `[ ]` |
| `BUILD_A_ROCKET` | Build a Rocket | DRAG & DROP assembly + BUILD→TEST | `[ ]` |
| `BUILD_A_HOUSE` | Build a House | DRAG & DROP assembly + BUILD→TEST | `[ ]` |
| `BUILD_A_BOAT` | Build a Boat | DRAG & DROP assembly + BUILD→TEST | `[ ]` |
| `BUILD_A_ROBOT` | Build a Robot | DRAG & DROP assembly + BUILD→TEST | `[ ]` |
| `TOOL_SELECTION` | Tool Selection | TAP-THE-TARGET (choose the right tool for a shown problem) | `[ ]` |
| `SIMPLE_PHYSICS` | Simple Physics | own mechanic (place ramps/blocks, ball rolls to target) | `[ ]` |
| `BRIDGE_BUILDING` | Bridge Building | DRAG & DROP assembly + BUILD→TEST | `[ ]` |
| `BALANCE` | Balance | own mechanic (scale, add/remove to equalize) | `[ ]` |
| `HELP_THE_CHARACTER` | Help the Character | CHOOSE/TAP-THE-TARGET (a small scenario is shown — dog, bone, fence — child picks the action that solves it; distinct from Tool Selection's "pick the right tool") | `[ ]` added by the audit |

The five Build-a-X games share one assembly presenter (slots for parts, then a test animation) —
build it once against whichever of the five ships first. Help the Character was missing from the
original big-catalogue prompt (2026-09-26 audit finding) but fits Workshop's problem-solving scope.

**Built 2026-09-27 (M4.6).** All ten games are live. `Rules/Workshop.cs` + `App/Screens/AssemblyScreen.cs`
generalize the shared assembly presenter mentioned above to all seven BUILD→TEST games (the five
Build-a-X games, Bridge Building and Simple Physics), reusing Dress for the Occasion's own
slot/shelf/distractor + DragItem snap shape (Store, M4.3) and adding the TEST→OBSERVE tail those
games don't need: once every slot is filled, the assembled build's own test sprite pops and a test
voice line plays (the car drives, the rocket launches, the ball rolls) before the usual reward
flow. The remaining three (Tool Selection, Help the Character, Balance) reduce directly to the
existing MatchScreen presenter (Zoo & Farm, M4.4) — no new UI at all.

Design calls flagged for Adrian's review (same pattern as every earlier milestone's simplification
calls):
- **Simple Physics** is built on the same assembly presenter as the five Build-a-X games (place 3
  ramp/block pieces onto their fixed slots, then watch the ball roll to the target) rather than the
  plan's own free continuous placement + rolling simulation.
- **Balance** is built as a single-round MATCH pick-the-correct-counterweight (a scale tipped one
  way, tap the weight among choices that levels it) rather than a continuous add/remove-weights
  simulation.
- Both **Tool Selection** and **Help the Character** reduce directly to MatchRoundBuilder's
  existing scenario → pick-the-right-choice shape.

Map placement (`Rules/Places.cs`): Workshop sits well west of Zoo & Farm rather than due north of
it (which would put Zoo & Farm's own building between the Junction and Workshop on the same
column) — the road continues Zoo & Farm's own westbound leg further out, staying south of every
tap box before its final northbound run in. Had to update `Tests/PlacesTests.cs` and
`tools/art-import/places-layout.json` (both hardcode the place count/layout), same as every
building added this milestone.

Content (3 parts per build × 7 assembly games, 6 items each for Tool Selection/Help the
Character/Balance) is placeholder, pending a real art/content pass — same caveat as every
catalogue built this session. No Unity build or phone test has happened for Workshop yet either.

## 9. Art Studio — new building, doesn't exist yet

| id | Game | Mechanic | Status |
|---|---|---|---|
| `GUIDED_DRAWING` | Guided Drawing | SEQUENCE + TRACE-adjacent (step-by-step build, e.g. house) | `[ ]` |
| `TRACE_SHAPES` | Trace Shapes | TRACE | `[ ]` |
| `TRACE_LETTERS` | Trace Letters | TRACE | `[ ]` |
| `TRACE_NUMBERS` | Trace Numbers | TRACE | `[ ]` |
| `FINISH_THE_DRAWING` | Finish the Drawing | DRAG & DROP or TAP (complete missing half) | `[ ]` |
| `COLOR_BY_NUMBER` | Color by Number | TAP-THE-TARGET (numbered region → color) | `[ ]` |
| `COLOR_BY_INSTRUCTION` | Color by Instruction | CHOOSE (voice instruction → region) | `[ ]` |
| `DRAW_WHAT_YOU_HEAR` | Draw What You Hear | CHOOSE/DRAG & DROP combo (listening + placement) | `[ ]` |
| `DRAWING_CHALLENGES` | Drawing Challenges | reuses Guided Drawing's presenter over offline-authored content (no runtime AI) | `[ ]` |
| `FREE_DRAWING` | Free Drawing | own mechanic (open canvas, no goal/round/help-ladder — reward is a flat per-session coin, not per-round) | `[ ]` added by the audit |

TRACE is Art Studio's one new must-have mechanic — Trace Shapes/Letters/Numbers all sit directly
on it. Free Drawing was missing from the original big-catalogue prompt (2026-09-26 audit finding)
but Art Studio is its only possible home.

**Built 2026-09-27 (M4.7).** All ten games are live. `Rules/ArtStudio.cs` + `App/Screens/
TraceScreen.cs` build TRACE, the building's one new mechanic: a pencil-tip drag along a fixed path
(reusing `PathDragger`/`FingerMazePath.NearestFraction` from Finger Maze, Playground M4.1, exactly
as they are), with each segment lighting up as "drawn" once passed. One `TraceScreen` instance
serves all three trace games (Shapes/Letters/Numbers), configured with `TraceGameKind` and its own
path catalogue (`TraceCatalog`: regular polygons/stars for shapes, small single-stroke placeholder
outlines for letters/numbers). The four MATCH games (Color by Number, Color by Instruction, Finish
the Drawing, Draw What You Hear) reduce to the existing `MatchScreen` presenter (Zoo & Farm, M4.4);
the two SEQUENCE games (Guided Drawing, Drawing Challenges) reduce to `SequenceScreen` (Science
Lab's Plant Growth, M4.5), with Drawing Challenges literally the same presenter over a second
content pack, per the plan's own note. Free Drawing gets its own small `FreeDrawingScreen`: a color
palette + stamp palette, tap-to-place on an open canvas, no round/target/help-ladder shape at all,
a flat per-session coin on exit, and a gentle Eva idle line instead of any Hint/Demonstrate ladder.

Design calls flagged for Adrian's review (same pattern as every earlier milestone's simplification
calls):
- **Color by Number** and **Color by Instruction** are both built as single-round MATCH (tap the
  correct color swatch for a shown/spoken scene part) rather than a multi-region full coloring page
  per round.
- **Finish the Drawing** is built as MATCH (tap the correct missing-half tile among choices) rather
  than DRAG & DROP.
- **Draw What You Hear** is built as MATCH (Eva narrates a scene, tap the one completed picture
  that matches) rather than assembling the scene from individual shape pieces — reuses the
  audio-led shape Weather/Space (Science Lab, M4.5) already established.
- **Trace Shapes/Letters/Numbers'** path data (circle/triangle/square/star, and the letter/number
  outlines) is procedurally generated placeholder geometry, not real glyph/shape art.

Map placement (`Rules/Places.cs`): Art Studio sits south-east, well clear of Store's tap box/road
box (x ≤ 630) and Science Lab's (y ≥ 260) — the road dips to y=-400 (below Store's own tap box
range) right after the junction, then runs east at that depth before its final northbound run up
into Art Studio's own column, clear of every other building's tap box. Had to update
`Tests/PlacesTests.cs` and `tools/art-import/places-layout.json` (both hardcode the place
count/layout), same as every building added this milestone.

Content (6-color/6-stamp Free Drawing palette; 4-6 item pools for the other nine games) is
placeholder, pending a real art/content pass — same caveat as every catalogue built this session.
No Unity build or phone test has happened for Art Studio yet either.

## 10. Brain Gym — `BuildingId.BrainGym`

| id | Game | Mechanic | Status |
|---|---|---|---|
| `CLASSIC_MEMORY` | Classic Memory | own mechanic (flip-and-match pairs, new `MemoryBoardScreen`) | `[ ]` built, awaiting Unity pass |
| `REMEMBER_THE_SEQUENCE` | Remember the Sequence | own mechanic (flash then reproduce order, new `SequenceRecallScreen`, see note below) | `[ ]` built, awaiting Unity pass |
| `WHATS_DISAPPEARED` | What Disappeared? | MATCH (built as tap-the-missing-item, see note below) | `[ ]` built, awaiting Unity pass |
| `SIMON_SAYS` | Simon Says | shares Remember the Sequence's `SequenceRecallScreen`, own colour-pad content pack | `[ ]` built, awaiting Unity pass |
| `REMEMBER_THE_LOCATION` | Remember the Location | MATCH (built as tap-the-remembered-spot, see note below) | `[ ]` built, awaiting Unity pass |
| `SAME_OR_DIFFERENT` | Same or Different? | MATCH | `[ ]` built, awaiting Unity pass |
| `MATCH_ROTATION` | Match Rotation | MATCH (built as tap-the-rotation-match, see note below) | `[ ]` built, awaiting Unity pass |
| `WHICH_IS_BIGGER` | Which Is Bigger? | MATCH (binary bigger/smaller judgement, see note below) | `[ ]` built, awaiting Unity pass |
| `COMPLETE_THE_PICTURE` | Complete the Picture | MATCH (missing-section identification) | `[ ]` built, awaiting Unity pass |
| `FIND_THE_DIFFERENCES` | Find the Differences | MATCH (built as tap-the-different-spot, see note below) | `[ ]` built, awaiting Unity pass |
| `SPOT_THE_OBJECT` | Spot the Object | MATCH (hidden object in scene) | `[ ]` built, awaiting Unity pass |
| `FOLLOW_THE_PATH` | Follow the Path | MATCH (built as tap-the-path's-destination, see note below) | `[ ]` built, awaiting Unity pass |
| `WHATS_BEHIND` | What's Behind the Object? | MATCH | `[ ]` built, awaiting Unity pass |
| `PERSPECTIVE` | Perspective | MATCH | `[ ]` built, awaiting Unity pass |
| `COPY_THE_CONSTRUCTION` | Copy the Construction | MATCH (built as tap-the-matching-construction, see note below) | `[ ]` built, awaiting Unity pass |
| `FIND_THE_MISSING_PIECE` | Find the Missing Piece | MATCH | `[ ]` built, awaiting Unity pass |
| `SORTING` | Sorting | MATCH (built as tap-the-bucket, same call as Zoo & Farm/Science Lab's SORT rows) | `[ ]` built, awaiting Unity pass |
| `SEQUENCE_ORDERING` | Sequence Ordering | SEQUENCE (reuses `SequenceScreen` directly, same as Plant Growth) | `[ ]` built, awaiting Unity pass |
| `RECYCLING` | Recycling | MATCH (tap-the-bin reskin of Sorting) | `[ ]` built, awaiting Unity pass, added by the audit |
| `MATCH_ITEM_TO_CATEGORY` | Match Item to Category | MATCH (generic "which category" tap, distinct from Zoo & Farm's animal-specific MATCH rows) | `[ ]` built, awaiting Unity pass, added by the audit |
| `SORT_LAUNDRY_CHORES` | Sort Laundry / Chores | MATCH (tap-the-bin/room reskin of Sorting) | `[ ]` built, awaiting Unity pass, added by the audit, assigned to Brain Gym 2026-09-26 |

Biggest single building by game count (18 original + Recycling, Match Item to Category, Sort
Laundry/Chores = 21). Recycling, Match Item to Category and Sort Laundry/Chores were all missing
from the original big-catalogue prompt (2026-09-26 audit finding) but reuse mechanics Brain Gym
already has.

**Implementation decisions (2026-09-27, flagged for Adrian's review, same pattern as every earlier
building's simplifications):**
- 17 of the 21 games reduce to the existing `MatchScreen` presenter (`Rules/BrainGym.cs`'s
  `BrainGymMatchRoundGenerator`), each with its own small (id, value) dataset - the same "reduce a
  judged-comparison/categorisation game to tap-the-correct-tile" call every earlier building has
  made (Workshop's Balance, Art Studio's Finish the Drawing, Zoo & Farm/Science Lab's whole SORT
  cluster). What's Disappeared, Same or Different, Match Rotation, Complete the Picture, Find the
  Differences, Spot the Object, What's Behind, Perspective, Copy the Construction and Find the
  Missing Piece all use a self-referential (id == value) dataset, the same shape as Art Studio's
  Finish the Drawing/Draw What You Hear; Copy the Construction in particular drops DRAG & DROP for
  tap-the-matching-photo, the same call Science Lab made for Sink or Float. Which Is Bigger is a
  binary MATCH (tap the bigger/smaller size icon for a shown animal) rather than a side-by-side
  pairwise comparison tap.
- Classic Memory and Remember the Sequence/Simon Says needed two new presenters, both built once
  and reused: `MemoryBoardScreen` (flip-and-match-pairs board, 2-6 pairs by level, its own small
  peek-a-pair/auto-match-a-pair help ladder) and `SequenceRecallScreen` (`SequenceScreen`'s own
  tap-in-order shape with a flash/study phase added in front of it, since - unlike Plant Growth's
  inferable growth order - the child has to actually recall this order). Remember the Sequence and
  Simon Says both drop true Simon-style repeat-allowed growing sequences for a fixed-pool, no-repeat
  order (`SequenceRoundBuilder`'s existing prefix-of-a-list shape); Simon Says is the same
  presenter over a second, colour-pad content pack, exactly as Art Studio's Drawing Challenges
  reused Guided Drawing's presenter.
- Content across every dataset above is placeholder, pending a real art/content pass, same caveat
  as every catalogue built this session.

## 11. Friends' Park — new building, doesn't exist yet

| id | Game | Mechanic | Status |
|---|---|---|---|
| `EMOTION_MATCHING` | Emotion Matching | MATCH | `[ ]` |
| `WHAT_WOULD_YOU_DO` | What Would You Do? | CHOOSE (scenario → response) | `[ ]` |
| `FACIAL_EXPRESSION` | Facial Expression Game | MATCH/own mechanic (match or create an expression) | `[ ]` |
| `EMPATHY` | Empathy | CHOOSE (scenario → helpful response) | `[ ]` |
| `SOCIAL_SITUATIONS` | Social Situations | CHOOSE | `[ ]` |
| `LISTEN_AND_CHOOSE` | Listen and Choose | CHOOSE (sentence → picture) | `[ ]` |
| `LISTEN_FOR_DETAILS` | Listen for Details | CHOOSE (detail-level listening) | `[ ]` |
| `FOLLOW_1_INSTRUCTION` | Follow 1 Instruction | DRAG & DROP-adjacent (act on a spoken instruction) | `[ ]` |
| `FOLLOW_2_INSTRUCTIONS` | Follow 2 Instructions | same mechanic, longer instruction | `[ ]` |
| `FOLLOW_3_INSTRUCTIONS` | Follow 3 Instructions | same mechanic, longer instruction | `[ ]` |
| `ROAD_SAFETY` | Road Safety | TAP-THE-TARGET (cross on green, wait on red) | `[ ]` |
| `SAFETY_SCENARIOS` | Safety Scenarios | CHOOSE (hot stove / stranger / lost, handled gently) | `[ ]` |

`FOLLOW_1/2/3_INSTRUCTION` share one presenter parameterized by instruction count.

Built 2026-09-27 (Rules/FriendsPark.cs, PlaceId.FriendsPark/BuildingId.FriendsPark, map entry east of the
junction at y=0 - see Rules/Places.cs's own comment on why this POI sits at House's height instead of north/
south like every other one, Science Lab's tap box leaving no room to clear it with the usual 100-unit gap
this far east inside the world bounds). Design calls (flagged for Adrian's review, same pattern as every
earlier building's simplifications):
- Road Safety is a single-round binary MATCH (traffic-light scene shown, tap "cross" or "wait") rather than a
  timed reaction game where the light actually cycles - reuses MatchScreen fully, same shape as Brain Gym's
  Which Is Bigger.
- Follow 1/2/3 Instructions reuse SequenceScreen/SequenceRoundBuilder directly (Brain Gym's Sequence Ordering
  precedent, no new presenter) with a *fixed* instruction count per game (1/2/3, not level-scaled) over one
  shared six-action park pool; each round speaks one placeholder instruction line rather than assembling one
  dynamically from named actions.
- Facial Expression Game reuses Emotion Matching's own six-emotion item pool with target/choice swapped
  (plan's own explicit call: "MATCH, not an open-ended expression-builder").
- Emotion Matching and the two Listen games are audio-led (no target picture, only spoken prompt/target lines),
  same shape as Zoo & Farm's Animal → Sound/Geography.
Content (six-item pools for the MATCH games, a six-action pool for Follow Instructions) is placeholder,
pending a real art/content pass, same caveat as every catalogue built this session. No Unity build or phone
test has happened for Friends' Park either.

## Unassigned — needs a decision

All game-level unassigned items from the 2026-09-26 scope audit were resolved the same day (House-
orphaned games cut outright; dressing cluster → Store; Cooking Measures/Seasons/Day-night/Space →
Science Lab; Sort Laundry/Chores → Brain Gym; Geography → Zoo & Farm — all folded into their POI
sections above). Two non-game items remain open, tracked here only so they aren't lost between
docs:

- `[unassigned]` Daily Adventure — intentionally deferred per the original backlog's own note
  ("meta feature, discuss after first modes exist"), not dropped by this plan.
- `[unassigned]` Parent progress view — a screen, not a building/game row; still owed per the
  design doc's M3 scope, tracked here only so it isn't lost between docs.

## Cross-cutting rules that apply to every game above (not repeated per row)

- **Metadata**: every game gets a stable id (`UPPER_SNAKE_CASE`, as tagged above), display name,
  building id, domain, skills, age range, difficulty progression, interaction type, content/audio
  requirements, reward profile — per `Rules/Activities.cs`'s existing `Activity` record, extended
  as needed once a second building exists.
- **Difficulty**: reuse `DifficultyLadder`/rolling-5-round-window exactly as Count/Number Hunt do
  — own level + buffer per game, no shared state between games, no Easy/Medium/Hard menu.
  Per-game parameter that the level changes is noted in the reuse-mechanic column above where it
  isn't obvious.
- **Help ladder**: reuse `HelpLadder`/`CoinPayout` (Retry → Hint → Demonstrate) exactly as-is;
  each presenter defines its own concrete hint/demonstration, never a generic "show answer".
  Difficulty is capped at each game's own hint/demo hint content existing — a game with no
  meaningful non-verbal hint animation is a design problem to flag, not to skip.
  reward:
- **Reward**: every round reports its result to the shared `CoinPayout`/`Progress` flow; no
  per-game hardcoded economy.
- **Voice**: every new game reuses `Voice`/`VoiceLines`/`voice-lines.txt` — no per-game audio
  system. New keys follow existing naming (`<game>_<purpose>`).
- **No "Coming Soon" placeholders**: per the brief, a catalogue entry is only checked off once it
  is real content, playable, in its correct building, with retry/help and a difficulty ladder
  wired to the reward flow — not before.
- **No MVP shrink**: this full list stays the target; nothing here gets quietly dropped to a
  smaller v1 without an explicit ask. Build order is still one game at a time, each accepted
  before the next (existing working method), tracked by flipping the Status column above.

## Immediate next step

**M4.1 Playground** (per `docs/superpowers/plans/2026-09-26-m4-full-content-plan.md`, the user's
fixed starting point) — stand up the place, then its 14 games. `LETTER_HUNT` is still the next
School game whenever School work resumes, but Playground goes first per the M4 order.

Progress this session: Playground is stood up (`PlaceId.Playground`/`BuildingId.Playground`,
`ScreenId.Playground`, map entry with a placeholder road/tap box - real coordinates and art are
still a design pass, see the M4 plan's own flag on this) and the first eight games in the build
order are each written end to end - own Rules generator, difficulty ladder, help ladder, screen
and tests: `PATTERN_COMPLETION`, `ODD_ONE_OUT`, `WHATS_MISSING`, `WHICH_DOESNT_MAKE_SENSE`,
`ITEM_TO_SHADOW`, `FINGER_MAZE`, `FOLLOW_NUMBERS_IN_ORDER`, `FOLLOW_LETTERS_IN_ORDER`. Finger Maze
is the first NAVIGATION game: it adds a shared grid/path generator, a corridor renderer
(`MazeCorridorRenderer`, App/Ui) and a drag-along-a-path component (`PathDragger`, App/Ui) that the
plan's other NAVIGATION games are meant to reuse rather than reinvent. Follow Numbers/Letters in
Order both reuse the grid/path generator and corridor renderer but not the drag component - they
are tap-in-order games, not drags - which is why their screens are a plain TAP-THE-TARGET-style
tile pool positioned along the same corridor. Follow Letters draws its checkpoints as sprites
(`letters/a`..`letters/h`), never as TMP_Text, and has Eva speak the target sequence aloud before
each round (new single-letter voice lines `letter_a`..`letter_h`) since - unlike numbers - letters
carry no order a preliterate child can infer by sight; matching what was heard to what is shown is
the point, not reading, so this stays inside the no-reading rule. None of these eight are flipped
to `[x]` above: no Unity is available in this session to compile or run the test suite, so each
still needs a cold build + on-device pass (same gate every earlier game went through, e.g. Number
Hunt's spike notes) before it is confirmed real - Finger Maze especially, since its drag mechanic
could not be touch-tested at all here.

`SHORTEST_PATH` (9th game) is also now written end to end - own Rules generator
(`ShortestPathRoundGenerator`, each route its own 3-point start/bulge/finish polyline whose length
is a monotonic function of the bulge amplitude, ranked and shuffled per round), screen and tests.
It reuses the shared `MazeCorridorRenderer` to draw 2-3 side-by-side routes but not `PathDragger` or
the Finger Maze grid walk - it's a tap-to-choose game (2-3 separate routes to compare), not a single
shared maze to drag or step through, so its screen instead mirrors `OddOneOutScreen`'s
tap-and-eliminate shape, with a start tile per route standing in for OddOneOut's item tiles. Per the
open flag below, `ShortestPathScreen` is registered in `Navigator`/`EvaGame` (so it can be shown and
audited directly) but deliberately **not** added to `Rules/Activities.cs` - it does not yet appear in
Playground's building menu. Not flipped to `[x]` for the same reason as the first eight: no Unity
build/on-device pass has happened yet.

`AVOID_OBSTACLES` (10th game) is also written end to end. It reuses Finger Maze's own generated
corridor exactly unchanged (`AvoidObstaclesRoundGenerator.Create` just wraps
`FingerMazeRoundGenerator.Create`) and scatters a few hazard tiles on grid cells that neighbour the
path without ever sitting on it, so the one true corridor a child can drag along is always exactly
as forgiving as Finger Maze's. Straying the finger onto a hazard is this game's "mistake" (soft
Retry -> Hint -> Demonstrate, same ladder as everything else, never a hard fail) - detecting that
needed the raw, unsnapped drag point rather than `PathDragger`'s existing `Fraction`/`Progressed`
(which always sits on the corridor centreline by design), so `PathDragger` gained a new `RawMoved`
event for this one game; every earlier NAVIGATION screen is unaffected since none of them subscribe
to it. Same as Shortest Path: registered in `Navigator`/`EvaGame` for audit/preview but deliberately
**not** added to `Rules/Activities.cs` pending the ceiling decision, and not flipped to `[x]` (no
Unity pass yet - this one especially needs an on-device check, since the raw-drag hazard-radius
constant is an untested guess at what "straying too close" should feel like on a real touchscreen).

`COLLECT_EVERYTHING` (11th game) is also written end to end. It reuses Finger Maze's own generated
corridor exactly unchanged, marking a few of the path's own interior waypoints as pickups instead of
placing anything beside the path - unlike Avoid Obstacles' hazards, a pickup sits directly on the
corridor, so collecting one is just a matter of continuing to drag through it, and this game needed
no equivalent of `PathDragger.RawMoved`. Reaching the finish before every pickup is collected is this
game's own "mistake" (soft Retry -> Hint -> Demonstrate, same ladder as everything else): Hint pulses
the nearest uncollected pickup, and Demo has the hand collect just that one pickup to show the
motion, then hands control back for the child to finish the rest - it deliberately never retraces
the whole route the way Finger Maze's and Avoid Obstacles' Demo do, since collecting is cumulative
and every already-collected pickup has to stay collected. Same as the two games before it: registered
in `Navigator`/`EvaGame` for audit/preview but deliberately **not** added to `Rules/Activities.cs`
pending the ceiling decision, and not flipped to `[x]` (no Unity pass yet).

`ROTATE_THE_PIECE` (12th game) is also written end to end - the plan's one new mechanic between the
NAVIGATION cluster and the DRAG&DROP cluster. It adds `RotateDragger` (App/Ui): a handle that orbits
a piece, dragged around to rotate the piece to match a target orientation, snapped to 90/45-degree
steps at low levels and free at high levels (progression: rotation step, per the plan). Its Hint
(the plan calls for "an arrow shows which way to rotate") is stood in for with a small wobble toward
the correct direction rather than a real directional-arrow sprite - no placeholder art can actually
convey a rotation direction yet, so this is flagged the same way every other placeholder-art gap has
been this session. Its Demo is unlike every earlier game's: the piece animates through the correct
rotation itself, then resets - there is no hand miming a tap or drag here, exactly per the plan's own
note for this game, so `PointerHand` isn't used at all in this screen. Same as the three games
before it: registered in `Navigator`/`EvaGame` for audit/preview but deliberately **not** added to
`Rules/Activities.cs` pending the ceiling decision, and not flipped to `[x]` (no Unity pass yet -
this one especially needs an on-device check, since the rotate-handle gesture and its snap-to-step
feel are both untested guesses).

`JIGSAW` (13th game) is also written end to end - the first of the plan's DRAG&DROP cluster. It
reuses `DragItem` unchanged, adding the "snap when close to its own correct region" check the plan
calls for. The board and tray are two identical-shaped grids (piece count 4/6/9/16/25 per level, the
plan's own ladder); a piece starts in a shuffled tray slot and belongs in one board slot, so "where it
starts" and "where it goes" never need scatter/overlap math, just two grids. Hint glows the correct
slot for the currently-held (or most recently touched) piece; Demo drags exactly one piece home then
hands control back, the same "demonstrate one, not the whole thing" shape Collect Everything's Demo
already established, since placement here is cumulative too. Real piece art (a source photo actually
sliced into N tiles) is a separate content-pipeline prerequisite the plan itself calls out, so this
uses placeholder tiles like every other game before real art exists.

New open item: at 25 pieces, each piece is necessarily smaller than the usual 240-unit tap floor (a
board sized for a phone screen can't give 25 tap targets 240 units each) - the same rationale as
Count's own small object slots, so `NoReadingAuditTests` gained a second, identically-shaped
exemption (`IsPuzzlePieceSlot`, keyed on a container literally named `PuzzlePieceField`) rather than
weakening the 240-unit rule generally. This is a defensible technical call, not a UX one, but Adrian
should know it exists - flagged here for review, and Tangram/Puzzle Blocks next will need the same
exemption for the same reason. Same as every DRAG&DROP/NAVIGATION game since Follow Letters: this one
is registered in `Navigator`/`EvaGame` for audit/preview but deliberately **not** added to
`Rules/Activities.cs` pending the `TileLayout.MaxTiles` ceiling decision, and not flipped to `[x]`
(no Unity pass yet - drag-and-snap touch feel is untested here same as every other drag mechanic this
session).

`TANGRAM_CONSTRUCTION` (14th and last Playground game) is also written end to end, closing out the
whole 14-game Playground build order from the M4 plan. It is Jigsaw's closest sibling - same
board/tray grid-permutation trick, same DragItem-plus-snap-to-region screen shape, same Hint/Demo
behaviour (glow the correct spot; place one shape then hand control back) - swapped to a "ghost
silhouette" theme with a classic 7-piece tangram piece count (3 -> 7, capped there) instead of
Jigsaw's photo-grid counts, and pieces of varied size (`TangramPiece.SizeScale`) standing in for the
classic set's own 2-large/1-medium/2-small-triangle-plus-square-plus-parallelogram composition. Real
tangram geometry (actual shaped pieces needing rotation to fit) is a separate content/design pass,
same as Jigsaw's own real-photo-slicing gap - this uses placeholder square tiles of varied size until
that exists. Also reuses the `PuzzlePieceField` container name (and so `IsPuzzlePieceSlot`'s
exemption) for the same too-small-for-240-units reason Jigsaw hit. Registered in `Navigator`/`EvaGame`
for audit/preview but deliberately **not** added to `Rules/Activities.cs`, and not flipped to `[x]`
(no Unity pass yet), same as every game since Shortest Path.

All 14 Playground games from the M4 plan are now written end to end (Rules generator, screen, tests)
and committed. Games 1-8 are live in Playground's menu; games 9-14 (`SHORTEST_PATH` through
`TANGRAM_CONSTRUCTION`) are built, audited and reachable directly by `ScreenId` but held out of
`Rules/Activities.cs`, waiting on Adrian's call on the `TileLayout.MaxTiles=8` ceiling (see the open
flag above) before they can join the visible menu. None of the 14 is flipped to `[x]` yet - that gate
is reserved for an actual Unity build/test/on-device pass, which this session cannot do. Until Adrian
responds on the ceiling and runs that pass, the next step for this session is to keep watching for his
reply rather than starting new Playground content past this point (M4.1 Playground's own scope, per
the M4 plan, ends at these 14 games).

Open flag, now actually biting: `TileLayout.MaxTiles = 8` throws if a building's activity list
exceeds 8 entries; Playground is at 8 of 8 after this session (`ActivitiesTests` now asserts this
upper bound too, so a 9th entry fails fast in a unit test instead of crashing a running building
screen). `SHORTEST_PATH` onward - 6 more games - cannot be added to `Activities.cs` without
raising this ceiling or making the building list screen scrollable/paged, and Brain Gym later
needs 21 per building regardless. This is a UX/architecture call, not a bug fix, and Adrian has
been asked for a decision on it (open as of this note) - until then, any further Playground game
built past the 8th should have its Rules generator, screen and tests
written and committed as usual, but must NOT be added to `Activities.cs`, or the Playground
building screen crashes for every child who opens it (`TileLayout.Compute` throws unconditionally
past 8). Note this explicitly in each such game's own commit message.

With Playground's own 14-game scope closed out and the ceiling decision still Adrian's to make,
this session picked up School's own next unblocked game rather than idling: `LETTER_HUNT`, per
Adrian's own standing instruction to continue with any part of M4 that isn't blocked. School has
only `count`/`numhunt` registered (nowhere near the 8-tile ceiling), so this game is not held back
the way Playground's games 9-14 are - it is added straight into `Rules/Activities.cs`'s visible
menu. `LetterHuntRoundGenerator` mirrors `NumberHuntRoundGenerator` almost exactly (own
`LetterHuntLevel`/`LetterHuntBuffer` difficulty ladder, same two-step Retry -> Hint -> Demonstrate
help ladder, same five-round session), over the lowercase alphabet instead of digits, with a
letter-shape confusable distractor table (b/d, p/q, m/w, n/u) taking the numeral-confusable table's
place from level 5. `LetterHuntScreen` is `NumberHuntScreen`'s closest sibling (same tile grid, same
tweens) with one deliberate difference: every tile shows its letter as a sprite
(`letters/<lowercase>`), never as TMP_Text, since letters carry no order a preliterate child can
infer by sight - the same reasoning Follow Letters in Order's checkpoints already established.
Voice lines added: `activity_letterhunt`, `letterhunt_find/hint/demo`, plus `letter_i`..`letter_z`
(18 new single-letter lines) to complete the alphabet Follow Letters in Order started with
`letter_a`..`letter_h`. Not flipped to `[x]` for the same reason as every other game this session:
no Unity build/test/on-device pass has happened yet in this container.

Immediate next step: continue with the next unblocked M4 game per Adrian's standing instruction
(the M4 plan's own suggested School order after Letter Hunt), watching School's own activity count
against `TileLayout.MaxTiles = 8` as more are added - it is currently at 3 of 8, far from the
ceiling that blocks Playground. Still watching for Adrian's decision on that Playground ceiling;
once he answers, Playground's `SHORTEST_PATH`..`TANGRAM_CONSTRUCTION` can be added to
`Activities.cs` retroactively (or the building list screen made scrollable/paged instead).

`ADDITION` (School's next game after Letter Hunt, per the M4 plan's suggested Mathematics order) is
also now written end to end and added straight into `Activities.cs`'s visible menu (School is at 4
of 8 activities now, still nowhere near the ceiling). `AdditionRoundGenerator` is Number Hunt's own
"guess the target numeral among tiles" shape again - own difficulty ladder
(`AdditionLevel`/`AdditionBuffer`), same tile-count table, same two-step help ladder - except the
target is a computed sum (`A + B`) rather than a spoken number, and a guaranteed off-by-one
distractor (the classic addition slip) replaces Number Hunt's visual-numeral-confusable table from
level 3. `AdditionScreen` adds one new piece above the answer tiles: at low levels (1-3) two static
object groups (Count's own `CountObject`/`objects/*` sprites, purely decorative - no counting-phase
taps or badges, so no 240-unit tap-target concern) the child can count to find the sum; at levels
4-6 the objects are dropped for a bare equation, shown as digit numerals (the only text ever
allowed by the no-reading audit) either side of new `symbols/plus`/`symbols/equals`/`icons/question`
placeholder sprites - never literal "+"/"="/"?" text, since those characters would fail
`OnlyDigitsAreShownExceptInTheSpeechBubble`. New voice lines: `activity_addition`,
`addition_find/hint/demo`. Not flipped to `[x]` - no Unity pass yet, same as every game this
session; the object-group layout especially (up to 9 icons a group, 4-per-row) is an untested guess
at what reads clearly on a real screen.

`SUBTRACTION` (School's next game after Addition, per the M4 plan's Mathematics order) is also now
written end to end and added to `Activities.cs`'s visible menu (School is at 5 of 8 activities now).
`SubtractionRoundGenerator` is Addition's own generator again - own difficulty ladder
(`SubtractionLevel`/`SubtractionBuffer`), same off-by-one distractor from level 3 - except `B` is
drawn no bigger than `A` so the difference is never negative. `SubtractionScreen` is Addition's own
screen again, differing only in what the objects show: instead of two groups combining, one group
of `A` starts full and the last `B` of its own objects are dimmed with a small overlaid "taken
away" mark rather than removed from the layout outright, so the group's starting size stays visible
alongside what was taken from it - the still-bright, un-marked objects are the difference to count.
At higher levels the objects drop for a bare `A - B = ?` equation, same digit-numerals-plus-symbol-
sprites shape as Addition's (new `symbols/minus` placeholder, reusing `symbols/equals`/
`icons/question`). New voice lines: `activity_subtraction`, `subtraction_find/hint/demo`. Not
flipped to `[x]` - no Unity pass yet, same as every game this session.

`WHICH_HAS_MORE` (School's next game, taken out of the plan's own suggested order - see below) is
also now written end to end and added to `Activities.cs`'s visible menu (School is at 6 of 8
activities now). Its own difficulty ladder is `WhichHasMoreLevel`/`WhichHasMoreBuffer`. Unlike every
game so far this session, levels 1-3 are a genuinely new interaction, not a numeral-tile pick: two
object groups sit inside two large buttons side by side (each a real 380x380 tap target, well over
the 240-unit floor, holding a small non-interactive icon grid), and the child taps the one with
more - `WhichHasMoreScreen` reuses the same Retry/Hint/Demonstrate ladder, just pointed at whichever
group button is correct instead of a numeral tile. From level 4, the plan's own "later, 'how many
more' asks for the numeric difference" extension kicks in: both groups become a static, non-
interactive comparison and the question becomes numeric, reusing Addition/Subtraction's own answer-
tile mechanic exactly (same guaranteed off-by-one distractor from level 5) to ask for the
difference. New voice lines: `activity_which_has_more`, `whichhasmore_find` (levels 1-3),
`whichhasmore_difference` (levels 4-6), `whichhasmore_hint/demo`. Not flipped to `[x]` - no Unity
pass yet; the two-large-button compare layout especially is untested on a real screen, and is this
session's first game with two genuinely different interaction shapes gated by level in the same
screen, so it deserves an especially careful look before its own Status checkbox flips.

Reordering note: `ONE_MORE_ONE_LESS` is next in the plan's own suggested Mathematics order
(Addition → Subtraction → One More/One Less → Which Has More → ...), but its DRAG & DROP mechanic
(dragging a duck into/out of a pond at low levels) is a new, untested-in-this-session gesture this
session chose not to rush past the numeral-tile/compare-button games already validated by pattern
reuse this session. Which Has More was built first instead since it reuses more of Addition/
Subtraction's own answer-tile machinery. `ONE_MORE_ONE_LESS` is still open and next.

`ONE_MORE_ONE_LESS` is also now written end to end and added to `Activities.cs`'s visible menu -
**School is now at 7 of 8 activities**, one away from the same `TileLayout.MaxTiles` ceiling
blocking Playground (see the open flag above); the next School game after this one will need to be
held out of `Activities.cs` the same way Playground's games 9-14 are, until Adrian's ceiling
decision lands. Own difficulty ladder: `OneMoreOneLessLevel`/`OneMoreOneLessBuffer`. This is the
session's first true DRAG & DROP mechanic in School (every earlier School game reused Number Hunt's
numeral-tile pick): levels 1-3 show a pond holding `StartCount` ducks, and the child drags a single
duck either into the pond ("one more") or out to a tray beside it ("one less") -
`OneMoreOneLessScreen` reuses `DragItem`/Jigsaw's own "snap within radius" pattern for the drop
check, sized straight at the 240-unit tap floor (no exemption needed, since there's only ever one
draggable duck). Hint glows the correct target (the pond or the tray, per the plan's own note) rather
than pointing at a tile; Demo has the hand carry the duck to the target once and then reset it to
its start, since the child still has to drag it themselves (`FingerMazeScreen`'s "hand retraces,
child repeats" shape, not Jigsaw's "hand places it for good"). From level 4 the objects drop for a
numeric "one more/less than N?" question, reusing Addition/Subtraction's own answer-tile mechanic
exactly, with the classic mistake of answering with the original count guaranteed among the choices.
New voice lines: `activity_one_more_one_less`, `onemoreoneless_more/less` (the question, by
direction), `onemoreoneless_hint/demo` (drag levels), `onemoreoneless_tilehint/tiledemo` (numeric
levels). Not flipped to `[x]` - no Unity pass yet, and this one especially needs an on-device check:
the drag-drop-in-a-pond gesture and its snap radius are untested guesses, and the draggable duck's
start/target position can visually overlap the static pond ducks at the same anchor point since
this session has no way to lay out a real pond photo/sprite to check against.

`NUMBER_ORDERING` is also now written end to end and added to `Activities.cs`'s visible menu -
**School is now at 8 of 8 activities, its own `TileLayout.MaxTiles` ceiling**, the same one already
blocking Playground's games 9-14. Own difficulty ladder: `NumberOrderingLevel`/`NumberOrderingBuffer`.
This is a SEQUENCE game: NumberHuntScreen's own tile grid and positions, but the child must tap
every tile in order (ascending, descending from level 5) rather than pick one target -
`NumberOrderingScreen` mirrors Follow Numbers in Order's own sequence-tap shape exactly (Playground):
a wrong tap never eliminates a tile since it may still be due later, and Demonstrate only shows the
remaining order without locking any of it in, so the child must tap through the whole remainder
themselves afterwards. Reused Follow Numbers in Order's own numeral range table (and its already-
authored `num_1`..`num_20` voice lines - no new per-number lines needed here). New voice lines:
`activity_number_ordering`, `numberordering_ascending/descending` (the round's opening line, by
direction), `numberordering_hint/demo`. Not flipped to `[x]` - no Unity pass yet, same as every game
this session.

**Both School and Playground hit their own 8-activity ceiling here, and Adrian has now resolved it:
"Scroll."** A building's game menu scrolls vertically instead of being capped at a fixed tile count.
`Rules/TileLayout.cs` no longer has a `MaxTiles` constant or an upper bound on `Compute(count)` - it
lays out any number of tiles in rows of up to 4 columns, growing downward from the top of a `Content`
rect (`ContentHeight(count)` returns how tall that rect needs to be), instead of the old fixed-height
box centred design. `App/Screens/BuildingScreen.cs` now builds a `ScrollRect` (`RectMask2D` + an
invisible raycastable `Image` on one GameObject, doubling as the `ScrollRect`'s own viewport since
`viewport` is left unassigned) sized to the existing safe area, with tiles parented under its
`Content` child instead of directly under `Root`. This is deliberately generic - the same
`TileLayout`/`BuildingScreen` code will carry Brain Gym's 21 activities later with no further changes
needed. `BuildingScreen.TilePosition` (used only by `TutorialGuide` for School's first tile) still
returns a valid Root-space position, since a freshly-built building screen's menu always starts
scrolled to the top. Playground's games 9-14 (`shortest_path`, `avoid_obstacles`,
`collect_everything`, `rotate_piece`, `jigsaw`, `tangram`) are now added to `Rules/Activities.cs` and
live in the Playground menu - **Playground is now at 14 of 14 activities**. `TileLayoutTests.cs` and
`ActivitiesTests.cs` are updated for the no-cap design (covering counts up to 21, ahead of Brain
Gym). Not flipped to `[x]` - no Unity pass yet, and the scroll gesture (drag feel, snap/inertia,
whether all 14 Playground tiles read clearly while scrolling) is untested guesswork until Adrian
tries it on device.

Immediate next step: continue building further M4 content (more Literacy or Mathematics games under
School) with Rules/screen/tests written and audited as usual, and added straight to
`Rules/Activities.cs` - the building menu now scrolls, so there is no ceiling left to hold games out
for.

Adrian confirmed (2026-09-27): continue M4.2 in the plan's own suggested order, then move to M4.3
(Store's Shopping game) once School's Mathematics and Literacy clusters are both done.

`MISSING_NUMBER` (School's next game after Number Ordering, per the M4.2 plan's Mathematics order)
is now written end to end and added to `Activities.cs`'s visible menu - School is at 9 activities.
Own difficulty ladder: `MissingNumberLevel`/`MissingNumberBuffer`. This is Addition's own shape read
backwards: `A + ? = Sum` instead of `A + B = ?`. `MissingNumberScreen` is `AdditionScreen`'s closest
sibling (same answer-tile grid, tweens, help ladder) with two differences: the problem display shows
a "mystery box" (a plain question-mark placeholder, never an object count, so the objects-shown
levels can't hand the child the answer by counting it directly) in the hidden addend's place, and the
known total (`Sum`) is now shown too - as an object group at low levels, a bare digit at high levels
- so the child has something to count/reason from. Reuses Addition's exact level tables
(`OperandMaxByLevel`/`ShowObjectsByLevel`/`TileCountByLevel`), since it's the same arithmetic fact
family asked with a different piece missing. Distractor design: the classic missing-addend slip -
answering with the total instead of the hidden piece - is guaranteed among the choices (it's always
different from the answer here), plus the usual off-by-one guarantee from level 3. New voice lines:
`activity_missing_number`, `missingnumber_find/hint/demo`. Not flipped to `[x]` - no Unity pass yet;
the mystery-box/object-group layout (five slots across the problem field instead of Addition's three)
is an untested guess on spacing until Adrian sees it on device.

Immediate next step: continue Mathematics with `NUMBER_LINE`, then `MULTIPLICATION`, per the M4.2
plan's suggested order, then start the Literacy cluster with `UPPERCASE_TO_LOWERCASE`.

`NUMBER_LINE` (School's next game after Missing Number) is now written end to end and added to
`Activities.cs`'s visible menu - School is at 10 activities. Own difficulty ladder:
`NumberLineLevel`/`NumberLineBuffer`. This is the plan's first genuinely new Mathematics mechanic
(the answer-tile family so far has all been Addition/Number Hunt reskins): Eva's character hops
`Hops` spaces forward or back from `Start` along a 0..`LineMax` number line, child taps the landing
number. `NumberLineScreen` still reuses the family's own answer-tile grid for the actual tap
(Addition's `PositionsFor`/tile mechanics, unchanged) rather than making every position on the line
itself a tap target - a level-6 line runs 0..20, and 21 individually-240-unit tap targets side by
side would either overflow the screen or force dots too small to read, the same ceiling
Jigsaw/Tangram's cramped piece grids hit. So the line drawn above the tiles is deliberately
decorative (a bar, a handful of dot markers with digit labels - digits are the one text form the
no-reading audit allows anywhere, a hop-character marker at `Start`) and only shows a short window
around Start/Landing, not the whole line, keeping it legible regardless of level. Progression: line
length and hop distance both grow with level (`LineMaxByLevel`/`HopsMaxByLevel`); backward hops (the
harder, less intuitive direction) are introduced only from level 4. Distractor design: the classic
off-by-one slip (landing one space short or long) guaranteed from level 3, same shape as
Addition/Missing Number. Demonstrate has one flourish beyond the family's usual "hand taps the
tile": the hop-character marker visibly moves from Start to Landing and back along the line first,
since the plan explicitly calls for the hop itself to be shown, before the hand points at and taps
the correct tile as usual. New voice lines: `activity_number_line`, `numberline_more/less` (the
round's opening line, by hop direction), `numberline_hint/demo`. Not flipped to `[x]` - no Unity
pass yet, and the number-line layout (dot spacing, marker size, whether the hop reads clearly to a
4-5 year old) is an untested guess until Adrian sees it on device - more so than most games this
session, since it's the first genuinely new visual element (not a reskinned answer-tile problem
display) since Playground's drag mechanics.

Immediate next step: continue Mathematics with `MULTIPLICATION` to close out the cluster, then start
the Literacy cluster with `UPPERCASE_TO_LOWERCASE`.

`MULTIPLICATION` (School's next and last Mathematics game) is now written end to end and added to
`Activities.cs`'s visible menu - School is at 11 activities, closing out the Mathematics cluster.
Own difficulty ladder: `MultiplicationLevel`/`MultiplicationBuffer`. Reuses Addition's exact
answer-tile mechanic; the problem itself is a `Rows x Cols` object grid the child counts as a group
of groups, rather than two groups added together. Per the plan's own "introduce x notation late",
the object grid stays through every level except the last, which drops to a bare `Rows x Cols = ?`
equation - later than Addition/Subtraction/Missing Number, which drop objects from the halfway
level. One deliberate departure from the rest of the family: Hint doesn't point at the answer tile
at all - the hand traces across the grid's top row then down its first column (or, at the bare-
equation level, touches the rows numeral then the cols numeral), inviting a recount without
revealing the total, exactly as the plan's own wording asks ("hand traces one row, then one column,
to invite a recount"). Only Demonstrate reveals the answer (hand taps the correct tile, only it
stays interactive), same as every other game here. Distractor design reuses the family's usual
off-by-one guarantee from level 3. New voice lines: `activity_multiplication`,
`multiplication_find/hint/demo`. Not flipped to `[x]` - no Unity pass yet, and the row/column trace
gesture (whether it reads as "recount this" to a 4-5 year old, distinct from a hint that just points
at the answer) is an untested design guess until Adrian sees it on device.

**Mathematics is now closed out: all 8 games from the M4.2 plan (Addition through Multiplication)
are built and live in School's menu.** Immediate next step: start the Literacy cluster with
`UPPERCASE_TO_LOWERCASE`, which defines the MATCH presenter the other nine Literacy games reuse, per
the plan's own suggested order (Uppercase to Lowercase → Beginning Sound → Rhyming → Word to Image →
Image to Word → Letter to Sound → Missing Letter → Build a Word → Scrambled Word → Simple Sentence
Builder).

`UPPER_LOWER_CASE` is now written end to end and added to `Activities.cs`'s visible menu - School is
at 12 activities, and this is the cluster's first MATCH game. Own difficulty ladder:
`UppercaseToLowercaseLevel`/`UppercaseToLowercaseBuffer`. Rather than inventing a new MATCH shell,
this reuses Playground's own Item to Shadow screen shape almost unchanged (one decorative target
tile above a row of up to 4 tappable choice tiles) - an uppercase letter sits in the target tile
(new sprite convention `letters/upper_<letter>`), its lowercase match is one of the choice tiles
(`letters/<letter>`, the same sprite Letter Hunt/Follow Letters in Order already use). Also reuses
Letter Hunt's own alphabet-pool and shape-confusable tables (`b/d`, `p/q`, `m/w`, `n/u`) rather than
inventing new ones - the same confusion pairs apply to matching a letter's case as to finding it.
New voice lines: `activity_uppercase_to_lowercase`, `uppercasetolowercase_find/hint/demo`. Not
flipped to `[x]` - no Unity pass yet, and the new `letters/upper_<letter>` sprite convention (26 more
placeholder assets, alongside the existing 26 lowercase ones) is unverified until real letter art
exists.

Immediate next step: continue the Literacy cluster with `BEGINNING_SOUND`, reusing this same MATCH
shell over an audio-led prompt (Eva speaks a word, child taps the picture whose name starts with the
same sound) instead of a shown letter.

`BEGINNING_SOUND` is now written end to end and added to `Activities.cs`'s visible menu - School is
at 13 activities. Own difficulty ladder: `BeginningSoundLevel`/`BeginningSoundBuffer`. Reuses
Uppercase to Lowercase's own MATCH shell minus its shown target tile, since the prompt here is
entirely spoken. One documented simplification: the plan calls for "Eva speaks a word", but this
session has no per-word audio to author (per the plan's own "content datasets are authored content"
note), so the round's spoken prompt reuses the alphabet's own single-letter lines
(`letter_<x>`, already authored for Letter Hunt/Follow Letters in Order) as the sound cue instead of
a full spoken word - a stand-in until real word audio exists, not the eventual version. Content pool
is a small curated 12-letter catalogue (one picture per letter: apple, ball, cat, dog, fish, goat,
hat, jam, kite, lion, moon, nest), growing with level the same shape as Letter Hunt's own alphabet
pool; a sound-alike confusable pair (g/k, m/n) is guaranteed among the choices from level 5, scoped
to this catalogue's own letters rather than reusing Letter Hunt's shape-confusable pairs, since sound
confusion and shape confusion aren't the same thing. New voice lines: `activity_beginning_sound`,
`beginningsound_find/hint/demo`. Not flipped to `[x]` - no Unity pass yet, and the picture content
(new `beginningsound/<item>` sprites, 12 placeholders) plus the letter-name-as-sound-cue compromise
both need Adrian's read before this is considered done, not just built.

`RHYMING` is now written end to end and added to `Activities.cs`'s visible menu - School is at 14
activities. Own difficulty ladder: `RhymingLevel`/`RhymingBuffer`. Reuses Uppercase to Lowercase's
MATCH shell with its target tile kept (unlike Beginning Sound), since the child needs to see which
word is being asked about while listening for the rhyme - the picture is a memory aid, but the match
itself is still by sound. Unlike Beginning Sound, this game did *not* take the letter-name-as-sound-cue
shortcut: rhyme is a property of a whole word's sound, not reducible to a single letter or starting
sound, so the round's spoken prompt needed the real target word, not a stand-in. 16 new per-word
voice lines (`word_cat` through `word_jug`) were authored instead - a deliberate, cheaper-than-it-sounds
divergence from Beginning Sound's shortcut, since the catalogue is intentionally small (four rhyme
families of four words each: `-at`, `-og`, `-an`, `-ug`). Distractor choices are guaranteed to come
from a family other than the target's own (a same-family word would also rhyme, making the round
ambiguous - covered by a dedicated test), with a designated "near family" (e.g. `-at`↔`-an`,
`-og`↔`-ug`) guaranteed among the choices from level 3 as the harder false-friend distractor. New
voice lines: `activity_rhyming`, `rhyming_find/hint/demo`, plus the 16 `word_<key>` lines above. Not
flipped to `[x]` - no Unity pass yet, and the new `rhyming/<key>` sprite convention (16 placeholders)
needs Adrian's read, same as every other placeholder-content game this session.

`WORD_TO_IMAGE` is now written end to end and added to `Activities.cs`'s visible menu - School is
at 15 activities. Own difficulty ladder: `WordToImageLevel`/`WordToImageBuffer`. Reuses Uppercase to
Lowercase's MATCH shell with its target tile kept (like Rhyming), but the target tile shows a
picture of the printed word (`words/<word>` sprite, never `TMP_Text` - this stays compliant with
the no-reading audit, the same way a letter is shown as a sprite rather than text) rather than a
photo of an object. Per spec 4.9 the print is decorative/optional support: Eva also speaks the word
right after (`word_<key>`, reusing Rhyming's per-word voice-line convention - `cat`/`hat`/`dog` are
shared with Rhyming's own catalogue, 9 new words were added: `fog`, `fun`, `cup`, `cap`, `box`,
`fox`, `bed`, `red`), and gameplay never depends on reading it. Content pool is 6 pairs of visually
similar printed words (same length, one letter apart - `cat`/`hat`, `dog`/`fog`, `sun`/`fun`,
`cup`/`cap`, `box`/`fox`, `bed`/`red`), growing with level the same shape as Beginning Sound's own
pool; each pair's partner is guaranteed among the choices from level 5 as the harder
look-alike-in-print distractor, a new confusable category (alongside Letter Hunt's shape confusion,
Beginning Sound's sound confusion, and Rhyming's family confusion) scoped to how two short printed
words can look alike to a pre-reader. New voice lines: `activity_word_to_image`,
`wordtoimage_find/hint/demo`, plus the 9 new `word_<key>` lines above. Not flipped to `[x]` - no
Unity pass yet, and both the new `words/<word>` and `wordtoimage/<word>` sprite conventions (21
placeholders combined) need Adrian's read, same as every other placeholder-content game this
session.

`IMAGE_TO_WORD` is now written end to end and added to `Activities.cs`'s visible menu - School is
at 16 activities. Own difficulty ladder: `ImageToWordLevel`/`ImageToWordBuffer` - a separate
progression from Word to Image's own, even though the content pool (catalogue, pool-size table,
confusable pairs) is identical, since matching picture-to-word and word-to-picture aren't the same
skill. This game is a straight tile-assignment swap of Word to Image's screen rather than a new
shell: the target tile shows the picture (`wordtoimage/<word>`, the sprite set Word to Image uses
for its choices) and the choice tiles show the printed words (`words/<word>`, the sprite set Word
to Image uses for its target) - no new art convention, no new voice-line words, both sprite sets and
all 21 `word_<key>` lines are shared with Word to Image/Rhyming as-is. New voice lines:
`activity_image_to_word`, `imagetoword_find/hint/demo`. Not flipped to `[x]` - no Unity pass yet.

`LETTER_TO_SOUND` is now written end to end and added to `Activities.cs`'s visible menu - School is
at 17 activities. Own difficulty ladder: `LetterToSoundLevel`/`LetterToSoundBuffer` - a separate
progression from Beginning Sound's, even though the content pool is identical, since matching
letter-to-sound and sound-to-letter aren't the same skill. Reuses Uppercase to Lowercase's own MATCH
shell (keeps the target tile, unlike Beginning Sound) showing the letter itself
(`letters/upper_<letter>`), with choice tiles reusing Beginning Sound's own picture sprites
(`beginningsound/<key>`) as-is - both games read the same letter-to-picture catalogue in opposite
directions, so no new content pool or art convention was needed. **Deferred**: the spec's own
higher-level variant ("later blends letters, C+A+T=CAT") was not built this pass - it needs its own
spelling presenter and word-content authoring, a separate effort from this simple single-letter
match; flagged here for Adrian's read, same as every other scope note this session. New voice
lines: `activity_letter_to_sound`, `lettertosound_find/hint/demo` (reuses the existing `letter_<x>`
lines as the spoken letter name, same as Letter Hunt/Follow Letters in Order). Not flipped to
`[x]` - no Unity pass yet.

`MISSING_LETTER` is now written end to end and added to `Activities.cs`'s visible menu - School is
at 18 activities, and this is the cluster's first spelling-composition game (TAP-THE-TARGET, not
MATCH). Own difficulty ladder: `MissingLetterLevel`/`MissingLetterBuffer`. Reuses Missing Number's
own answer-tile mechanic and problem-field shape (a hidden slot between two known values) read as
letters instead of digits - "C_T" is the word's first and last letters either side of a mystery box
(`icons/question`, never a letter sprite, so it can't give the answer away), and the child taps the
missing middle letter among up to 6 choice tiles (Letter Hunt's own letter-tile shape: a background
tile plus a child letter sprite, never a numeral or TMP_Text). Reuses the same 3-letter catalogue as
Word to Image/Image to Word (`cat`, `hat`, `dog`, `fog`, `sun`, `fun`, `cup`, `cap`, `box`, `fox`,
`bed`, `red`) so the middle letter is always the one hidden, matching the spec's own "C_T" example
exactly (`cat` → `C_T`, missing `a`). Distractor guarantee reuses Letter Hunt/Uppercase to
Lowercase's own shape-confusable pairs (b/d, p/q, m/w, n/u) from level 3, since the same shape
confusions apply to guessing a missing letter; the remaining tiles are filled from letters that
actually appear in the level's own word pool, never a letter the child hasn't seen among these
games. New voice lines: `activity_missing_letter`, `missingletter_find/hint/demo`. Not flipped to
`[x]` - no Unity pass yet.

`BUILD_A_WORD` is now written end to end and added to `Activities.cs`'s visible menu - School is at
19 activities. Own difficulty ladder: `BuildAWordLevel`/`BuildAWordBuffer`. Reuses Missing Letter's
own answer-tile mechanic almost unchanged - only the hidden position moves from the middle letter
to the last one ("CA_" is the word's first two letters beside a mystery box), and Eva speaks the
whole target word aloud (`word_<Word>`, Rhyming's per-word voice-line convention) before the tiles
go interactive, so the child's ear confirms what they're building as well as their eye - this is
the "(spoken)" half of the spec's own description that Missing Letter's purely visual prompt didn't
need. Same 3-letter catalogue and shape-confusable pairs as Missing Letter. New voice lines:
`activity_build_a_word`, `buildaword_find/hint/demo` (all 12 `word_<key>` lines were already
authored by Rhyming/Word to Image, so no new per-word audio was needed). Not flipped to `[x]` - no
Unity pass yet.

`SCRAMBLED_WORD` is now written end to end and added to `Activities.cs`'s visible menu - School is
at 20 activities, and this is the cluster's first drag interaction. Own difficulty ladder:
`ScrambledWordLevel`/`ScrambledWordBuffer` (RoundsPerSession = 3, not 5 - fewer, longer rounds than
a tap game, same reasoning as Jigsaw's own session length). Reuses Jigsaw's exact "snap when close
to its own correct region" mechanic (`DragItem`, a per-piece HomePosition/TrayPosition pair) with
letters standing in for puzzle pieces: each letter has a fixed home slot (its position in reading
order) and starts scattered in the tray under a shuffled slot. Same 3-letter catalogue as Missing
Letter/Build a Word, so every round has exactly 3 slots - a scope simplification, since this
session has no longer-word content to draw on. A picture of the target word
(`wordtoimage/<word>`, Word to Image's own sprite set) sits above the board as a memory aid, and Eva
speaks the whole word aloud (`word_<Word>`) before the tiles go interactive - ordering letters
purely from a spoken word with no visual anchor would be too hard for this age group. Same
Hint/Demo shape as Jigsaw (glow the correct slot; drag one letter home and hand back control). New
voice lines: `activity_scrambled_word`, `scrambledword_find/hint/demo` (no new per-word audio
needed, all 12 `word_<key>` lines already exist). Not flipped to `[x]` - no Unity pass yet.

`SENTENCE_BUILDER` is now written end to end and added to `Activities.cs`'s visible menu - School
is at 21 activities, and **this closes out M4.2's Literacy cluster and all of M4.2** (Mathematics:
Missing Number, Number Line, Multiplication; Literacy: all 10 games from Uppercase to Lowercase
through Simple Sentence Builder). Own difficulty ladder: `SentenceBuilderLevel`/
`SentenceBuilderBuffer` (RoundsPerSession = 3, same reasoning as Jigsaw/Scrambled Word). Reuses
Scrambled Word's own drag-and-snap mechanic with sentence pieces standing in for letters: each
piece has a fixed home slot (its position in the spoken sentence) and starts scattered in the tray.
A small curated catalogue of 6 two-slot sentences (`cat_cup` → "The cat has a cup.", etc.) stands in
for real sentence content, a placeholder pool flagged for a real authoring pass same as every other
content catalogue this session - each needs its own full spoken-sentence line (`sentence_<key>`)
rather than concatenating single-word audio, since a sentence reads naturally only spoken whole.
Progression is two-fold, per the spec's own "pictograms first, words gradually replacing pictures
at higher levels": the sentence pool grows with level, and pictograms (`wordtoimage/<word>`) are
gradually replaced by printed words (`words/<word>`) - 0 of 2 slots at levels 1-2, 1 of 2 from level
3, both from level 5. Same Hint/Demo shape as Scrambled Word/Jigsaw. New voice lines:
`activity_sentence_builder`, `sentencebuilder_find/hint/demo`, plus the 6 `sentence_<key>` lines
above. Not flipped to `[x]` - no Unity pass yet.

`SHOPPING` (M4.3, Store's second Activity) is now written end to end. Its own presenter, not the
shared Activities/`BuildingScreen` menu system Store never joined: reached via a small icon button
bolted onto `StoreScreen`'s own shelf (see below), not a `BuildingId` entry, so it has no
`activity_shopping` voice line. Own difficulty ladder: `ShoppingLevel`/`ShoppingBuffer`.

The brief's 7-step curriculum (recognize coins → notes → exact payment → simple addition →
subtraction/change → compare prices → budget) doesn't map one-to-one onto the shared 6-level
`DifficultyLadder`, a type every other game this session also depends on - widening it for one new
activity was judged too invasive, so "recognize coins" and "recognize notes" are merged into one
combined Recognize level whose denomination pool already spans both, giving exactly 6
`ShoppingMode` values (`Recognize, ExactPayment, Addition, Change, ComparePrices, Budget`) mapped
one level each. Unlike every other game this session, level and mode are the same thing here - the
round's *shape* changes with level, not just its numbers - since this is the brief's own progressive
curriculum, not a difficulty knob on one fixed mechanic. A generic, non-real-currency denomination
system (`1, 2, 5, 10, 20, 50, 100`, `money/coin_<v>`/`money/note_<v>` sprites) stands in for the
brief's coins/notes, another placeholder catalogue flagged for a real art/content pass.

Recognize/ExactPayment/Addition/Change reuse Addition's/WhichHasMore's answer-tile mechanic, with a
tile's content mode-dependent (a bare numeral, a single coin/note sprite, or a pair of coin sprites
for Addition's "combo" tiles); ComparePrices/Budget instead reuse WhichHasMore's direct-tap
group-button shape, generalized from 2 buttons to up to 3 for Budget's three-item shelf, each
showing an item icon plus a price tag (the same coin-icon-plus-digit convention as the Furniture
Store's own shelf). Same two-step Hint/Demo help ladder throughout.

Navigation: Store was never part of the `BuildingId`/Activities/`BuildingScreen` menu system
School/Playground use - it is (and stays) its own dedicated screen with an already tightly-tuned
layout. Rather than the larger refactor a literal `BuildingId` entry would require (touching
`MapScreen`, the tutorial's FirstPurchase step, and several test files - `ScreenId.Store` is also
already claimed by the Furniture Store's own shelf scene, which `BuildingScreen`'s own convention of
naming its ScreenId after its BuildingId would collide with), Store gets a small icon button bolted
onto `StoreScreen`'s own shelf, in the "Bubble button" clearance zone that screen's own layout
comments have reserved since Task 11 - a speech-bubble replay button `Hud.cs` documents as removed
(`SetBubbleButtonVisible` is a no-op stub) and never actually occupies, so this is genuinely free
space, not a repurposing of a button that exists. That icon opens a new, deliberately separate
`StoreActivitiesScreen` - a small scrolling tile menu (reusing `BuildingScreen`'s own `TileLayout`
math, but not `BuildingScreen` itself, for the `ScreenId` reason above) listing Store's non-shelf
activities, so each new dressing-cluster game is just one more tile rather than another bolted-on
icon competing for the same corner. Flagged here for Adrian's review since Store is the first-run
tutorial's own screen, though it changes nothing about the tutorial flow itself (FirstPurchase still
targets the Furniture Store shelf, unaffected).

New voice lines: `activity_shopping`, `shopping_find/hint/demo`. Not flipped to `[x]` - no Unity
pass yet.

`DRESS_THE_CHARACTER` (the dressing cluster's first game) is now written end to end and added as
`StoreActivitiesScreen`'s second tile. Reuses Jigsaw's own DragItem-based "snap when close to its
own correct region" mechanic unchanged, but with one real difference from Jigsaw and every other
DRAG & DROP game this session: per the brief, "no single correct answer at low levels (any
combination is fine, reward is for completing a full outfit)" - so a piece's home is simply its
body-part slot's fixed position (Head/Top/Bottom/Feet, laid out as a row of 4 rather than a stacked
dress-up-doll body, so every piece stays a full 240-unit tap target with no Jigsaw-style small-piece
exemption needed), and *which* item variant fills that slot is drawn at random from a small
per-slot placeholder catalogue that grows with level - difficulty here is purely "more item variety
to recognize," never a harder placement puzzle. A themed "does this outfit suit the occasion" goal
is left to Dress for the Occasion, the cluster's next game, rather than folded into this one. Same
Hint/Demo shape as Jigsaw (the next empty slot glows; the hand drags one piece home, then hands
control back). Own difficulty ladder: `DressTheCharacterLevel`/`DressTheCharacterBuffer`. New voice
lines: `activity_dress_the_character`, `dressthecharacter_find/hint/demo`. Not flipped to `[x]` - no
Unity pass yet.

`DRESS_FOR_OCCASION` (the dressing cluster's second game) is now written end to end and added as
`StoreActivitiesScreen`'s third tile. Level doubles as which of the brief's 6 occasions (school,
beach, winter, birthday, sports, camping) is being dressed for, in that listed order - the same
"level doubles as content" shape Shopping already established - so difficulty here progresses
through the occasion list itself as the child succeeds, not through a growing pool. Reuses Dress the
Character's DragItem snap mechanic, but the shelf now holds 2 wrong-occasion distractors alongside
the 3 correct items (one Top/Bottom/Feet each - Head is left out here to keep the shelf a manageable
5 items at the usual 240-unit tap size); dragging a distractor onto any slot, or a correct item onto
the wrong slot, is this game's mistake, and the piece floats back to the shelf rather than sitting
wherever it was dropped. Per the brief, Hint glows the correct next item *on the shelf* (not the
slot, unlike Dress the Character) and Demo drags that item on before handing control back. Own
difficulty ladder: `DressForOccasionLevel`/`DressForOccasionBuffer`. New voice lines:
`activity_dress_for_occasion`, `occasion_school/beach/winter/birthday/sports/camping`,
`dressforoccasion_hint/demo` (no generic `_find` line - each occasion's own line does that job). Not
flipped to `[x]` - no Unity pass yet.

`PACK_A_SUITCASE` (the dressing cluster's third and last game) is now written end to end and added
as `StoreActivitiesScreen`'s fourth tile, closing out both the dressing cluster and all of M4.3.
Level doubles as which trip is being packed for, reusing `Occasion` directly rather than a new
`TripType` enum - the same 6 labels (school, beach, winter, birthday, sports, camping) in the same
order as Dress for the Occasion, so a child who just learned "beach" there meets the identical word
here, reinforcing rather than introducing a fresh vocabulary. Reuses Dress for the Occasion's
distractor-shelf shape unchanged (3 correct items + 2 wrong-trip distractors, shuffled onto one
5-item tray row), but the target is one suitcase with 3 interchangeable slots rather than 3 named
body-part slots: because packing has no "which slot" identity to match, any correct item may land in
any unfilled slot, first-come-first-served - the one real structural difference from Dress for the
Occasion, where a correct item in the wrong slot still counts as a mistake. A distractor dragged
into the suitcase is this game's mistake, same as a wrong-slot drag in Dress for the Occasion; the
piece floats back to the shelf rather than sitting wherever it was dropped. Hint glows the correct
next item on the shelf; Demo drags that item into the next open slot before handing control back -
placing is cumulative, same as the rest of the cluster. Own difficulty ladder:
`PackASuitcaseLevel`/`PackASuitcaseBuffer`. New voice lines: `activity_pack_a_suitcase`,
`trip_school/beach/winter/birthday/sports/camping` (named `trip_...` rather than `occasion_...`,
since Eva's line here is about a trip, not an occasion, even though both reuse the same enum),
`packasuitcase_hint/demo` (no generic `_find` line - each trip's own line does that job). Not
flipped to `[x]` - no Unity pass yet.

**M4.3 is now complete: Shopping plus all three dressing-cluster games (Dress the Character, Dress
for the Occasion, Pack a Suitcase) are built and reachable from `StoreActivitiesScreen`.**
Immediate next step: per Adrian's standing "continue developing 4.2 and move to 4.3 when done"
instruction, the natural continuation is Section 6, **Zoo & Farm** (a new building, not yet
registered anywhere - `BuildingId`, `Activities.cs`, `MapScreen`, and its own `ScreenId`s all still
need adding, unlike Store's activities which slotted into an existing building). All ten animal rows
share one MATCH/SORT presenter over a common animal-content dataset (id, habitat, mother, food,
sound key, footprint sprite, covering, domestic/wild, land/sea/air) built once; suggested build order
follows the table above: Animal → Habitat, then Mother, Food, Footprint, Body Covering, Sound,
Domestic vs Wild, Land/Sea/Air, Animal Babies, Animal Classification, with Geography last since it
needs its own separate content dataset (countries/continents/flags/landmarks) rather than the shared
animal dataset.
