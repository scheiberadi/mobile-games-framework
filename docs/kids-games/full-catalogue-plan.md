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
| **SEQUENCE** (arrange in order) | Plant Growth, Number Ordering, Sequence Ordering | `[ ]` |
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
- `[cut]` Morning Routine — House carries no games, per 2026-09-26 direction
- `[cut]` Clean Your Room — House carries no games, per 2026-09-26 direction
- `[cut]` Cook a Meal — House carries no games, per 2026-09-26 direction
- `[cut]` Clock — House carries no games, per 2026-09-26 direction
- `[cut]` Calendar — House carries no games, per 2026-09-26 direction

Already implemented: character creator, persistent house with room slots, furniture drag-drop
placement, coins/progression meta-layer.

## 2. School — `BuildingId.School`

| id | Game | Domain | Skills | Mechanic | Status |
|---|---|---|---|---|---|
| `COUNT_OBJECTS` | Count the Objects | Mathematics | counting, number recognition | TAP-THE-TARGET (bespoke, has its own object-counting phase) | `[x]` |
| `NUMBER_HUNT` | Number Hunt | Mathematics | number recognition | TAP-THE-TARGET | `[x]` |
| `LETTER_HUNT` | Letter Hunt | Literacy | letter recognition | TAP-THE-TARGET (reuses Number Hunt's shape almost exactly, target letter spoken not shown) | `[ ]` next up |
| `ADDITION` | Addition | Mathematics | addition | TAP-THE-TARGET, visual objects → symbolic later | `[ ]` |
| `SUBTRACTION` | Subtraction | Mathematics | subtraction | TAP-THE-TARGET, visible removal → symbolic later | `[ ]` |
| `MULTIPLICATION` | Multiplication | Mathematics | multiplication | TAP-THE-TARGET, visual groups (rows×cols) → notation later | `[ ]` |
| `NUMBER_ORDERING` | Number Ordering | Mathematics | ordering | SEQUENCE, ascending → descending | `[ ]` |
| `MISSING_NUMBER` | Missing Number | Mathematics | arithmetic, algebraic thinking | TAP-THE-TARGET (2 + ? = 5) | `[ ]` |
| `ONE_MORE_ONE_LESS` | One More / One Less | Mathematics | counting, +/-1 | DRAG & DROP → numeric answer later | `[ ]` |
| `WHICH_HAS_MORE` | Which Has More? | Mathematics | comparison | TAP-THE-TARGET, later "how many more" | `[ ]` |
| `NUMBER_LINE` | Number Line | Mathematics | number line, +/- | own mechanic (character hops N spaces) | `[ ]` |

Note: brief's item L ("Counting/Number Hunt variants — reuse the existing system") is a build
instruction, not a distinct game — folded into how the above are implemented, not a catalogue row.

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

Build order note: the original M3 plan already picked Finger Maze/Jigsaw/Pattern Completion as
Playground's first 2-3 — the brief now asks for all 9, so Playground alone is roughly as big as
School.

## 4. Store — `BuildingId` not yet split into two activities

| id | Game | Domain | Skills | Mechanic | Status |
|---|---|---|---|---|---|
| — | Furniture Store | (meta) | — | DRAG & DROP-adjacent (buy → placeable in House) | `[x]` |
| `SHOPPING` | Shopping Game | Mathematics | coins, notes, addition, subtraction, prices, change, budgeting | own mechanic (shop interaction, not an equation screen); levels 1-7 per the brief (coins → notes → exact payment → +→ −/change → compare prices → budget) | `[ ]` |

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
| `ANIMAL_HABITAT` | Animal → Habitat | MATCH | `[ ]` |
| `ANIMAL_MOTHER` | Animal → Mother | MATCH | `[ ]` |
| `ANIMAL_FOOD` | Animal → Food | MATCH | `[ ]` |
| `ANIMAL_SOUND` | Animal → Sound | MATCH (audio-led) | `[ ]` |
| `ANIMAL_FOOTPRINT` | Animal → Footprint | MATCH | `[ ]` |
| `ANIMAL_COVERING` | Animal → Body Covering | MATCH | `[ ]` |
| `DOMESTIC_VS_WILD` | Domestic vs Wild | SORT | `[ ]` |
| `LAND_SEA_AIR` | Land / Sea / Air | SORT | `[ ]` |
| `ANIMAL_BABIES` | Animal Babies | MATCH/CHOOSE (identify baby vs adult) | `[ ]` |
| `ANIMAL_CLASSIFICATION` | Animal Classification | SORT (multi-attribute, higher levels) | `[ ]` |

All ten share one MATCH/SORT presenter over a common animal-content dataset (id, habitat, mother,
food, sound key, footprint sprite, covering, domestic/wild, land/sea/air) — build the dataset once.

## 7. Science Lab — new building, doesn't exist yet

| id | Game | Mechanic | Status |
|---|---|---|---|
| `SINK_OR_FLOAT` | Sink or Float | predict + own mini-simulation (drop, observe) | `[ ]` |
| `MAGNET` | Magnet Game | predict + own mini-simulation | `[ ]` |
| `LIVING_VS_NONLIVING` | Living vs Non-Living | SORT | `[ ]` |
| `PLANT_GROWTH` | Plant Growth | SEQUENCE | `[ ]` |
| `HUMAN_SENSES` | Human Body / Senses | MATCH (sense organ → sense) | `[ ]` |
| `HEALTHY_VS_UNHEALTHY` | Healthy vs Unhealthy | SORT | `[ ]` |
| `WEATHER` | Weather | MATCH | `[ ]` |
| `DRESS_FOR_WEATHER` | Dress for the Weather | MATCH/CHOOSE | `[ ]` |
| `CAUSE_AND_EFFECT` | Cause and Effect | own mechanic (act, then observe outcome) — shares its shape with Workshop's BUILD→TEST→OBSERVE | `[ ]` |

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

The five Build-a-X games share one assembly presenter (slots for parts, then a test animation) —
build it once against whichever of the five ships first.

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

TRACE is Art Studio's one new must-have mechanic — Trace Shapes/Letters/Numbers all sit directly
on it.

## 10. Brain Gym — new building, doesn't exist yet

| id | Game | Mechanic | Status |
|---|---|---|---|
| `CLASSIC_MEMORY` | Classic Memory | own mechanic (flip-and-match pairs) | `[ ]` |
| `REMEMBER_THE_SEQUENCE` | Remember the Sequence | own mechanic (show, hide, reproduce order) | `[ ]` |
| `WHATS_DISAPPEARED` | What Disappeared? | CHOOSE (show, hide, remove one, identify) | `[ ]` |
| `SIMON_SAYS` | Simon Says | shares Remember the Sequence's mechanic | `[ ]` |
| `REMEMBER_THE_LOCATION` | Remember the Location | own mechanic (spatial recall) | `[ ]` |
| `SAME_OR_DIFFERENT` | Same or Different? | CHOOSE | `[ ]` |
| `MATCH_ROTATION` | Match Rotation | CHOOSE (rotated-object comparison) | `[ ]` |
| `WHICH_IS_BIGGER` | Which Is Bigger? | TAP-THE-TARGET | `[ ]` |
| `COMPLETE_THE_PICTURE` | Complete the Picture | TAP-THE-TARGET (missing-section identification) | `[ ]` |
| `FIND_THE_DIFFERENCES` | Find the Differences | TAP-THE-TARGET (spot differences between 2 scenes) | `[ ]` |
| `SPOT_THE_OBJECT` | Spot the Object | TAP-THE-TARGET (hidden object in scene) | `[ ]` |
| `FOLLOW_THE_PATH` | Follow the Path | NAVIGATION-adjacent (visual tracking, not movement) | `[ ]` |
| `WHATS_BEHIND` | What's Behind the Object? | CHOOSE (spatial relationship) | `[ ]` |
| `PERSPECTIVE` | Perspective | CHOOSE (simple 3D-style spatial reasoning) | `[ ]` |
| `COPY_THE_CONSTRUCTION` | Copy the Construction | DRAG & DROP (recreate with blocks) | `[ ]` |
| `FIND_THE_MISSING_PIECE` | Find the Missing Piece | TAP-THE-TARGET | `[ ]` |
| `SORTING` | Sorting | SORT (size/type/category) | `[ ]` |
| `SEQUENCE_ORDERING` | Sequence Ordering | SEQUENCE | `[ ]` |

Biggest single building by game count (18) — expect to build its own shared memory/compare
presenter early and reskin most of the rest on top of TAP-THE-TARGET/CHOOSE/SORT/SEQUENCE.

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

School's `LETTER_HUNT` — same shape as `NUMBER_HUNT` (already-proven pattern: round generator +
tests in Rules, persisted level/buffer, a screen mirroring `NumberHuntScreen`), so it's the
fastest way to keep School moving before Playground needs to be stood up from scratch.
