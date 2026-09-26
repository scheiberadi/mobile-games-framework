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

Already implemented: character creator, persistent house with room slots, furniture drag-drop
placement, coins/progression meta-layer.

`[cut]` Morning Routine, Clean Your Room, Cook a Meal, Clock, Calendar — explicitly out of scope
(user decision, 2026-09-26): House carries no games and these five don't move elsewhere either.

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
| `UPPER_LOWER_CASE` | Uppercase to Lowercase | Literacy | letter recognition | MATCH | `[ ]` added by the 2026-09-26 scope audit |
| `LETTER_TO_SOUND` | Letter to Sound | Literacy | phonics | MATCH (audio-led; later blends into C+A+T=CAT) | `[ ]` added by the audit |
| `BUILD_A_WORD` | Build a Word | Literacy | phonics, spelling | TAP-THE-TARGET (CA_ with letter choices) | `[ ]` added by the audit |
| `SCRAMBLED_WORD` | Scrambled Word | Literacy | spelling | DRAG & DROP (reorder letters) | `[ ]` added by the audit |
| `MISSING_LETTER` | Missing Letter | Literacy | spelling | TAP-THE-TARGET (C_T) | `[ ]` added by the audit |
| `BEGINNING_SOUND` | Beginning Sound | Literacy | phonics | MATCH (audio-led) | `[ ]` added by the audit |
| `RHYMING` | Rhyming | Literacy | phonological awareness | MATCH (audio-led) | `[ ]` added by the audit |
| `WORD_TO_IMAGE` | Word to Image | Literacy | reading readiness | MATCH | `[ ]` added by the audit |
| `IMAGE_TO_WORD` | Image to Word | Literacy | reading readiness | MATCH | `[ ]` added by the audit |
| `SENTENCE_BUILDER` | Simple Sentence Builder | Literacy | sentence construction | DRAG & DROP (pictograms → words) | `[ ]` added by the audit |

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

## 4. Store — `BuildingId` not yet split into two activities

| id | Game | Domain | Skills | Mechanic | Status |
|---|---|---|---|---|---|
| — | Furniture Store | (meta) | — | DRAG & DROP-adjacent (buy → placeable in House) | `[x]` |
| `SHOPPING` | Shopping Game | Mathematics | coins, notes, addition, subtraction, prices, change, budgeting | own mechanic (shop interaction, not an equation screen); levels 1-7 per the brief (coins → notes → exact payment → +→ −/change → compare prices → budget) | `[ ]` |
| `DRESS_THE_CHARACTER` | Dress the Character | Executive function | fine motor, categorization | DRAG & DROP (clothes onto the character) | `[ ]` added by the audit, assigned to Store 2026-09-26 |
| `DRESS_FOR_OCCASION` | Dress for the Occasion | Executive function | categorization | MATCH/DRAG & DROP (school, beach, winter, birthday, sports, camping → matching outfit) | `[ ]` added by the audit, assigned to Store 2026-09-26 |
| `PACK_A_SUITCASE` | Pack a Suitcase | Executive function | planning, categorization | DRAG & DROP (pick items appropriate to a trip) | `[ ]` added by the audit, assigned to Store 2026-09-26 |

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
| `GEOGRAPHY` | Geography | MATCH/CHOOSE (which is Romania, continents, flags, landmarks, animals by continent, foods by country, globe) | `[ ]` added by the audit, assigned to Zoo & Farm 2026-09-26 |

All ten animal rows share one MATCH/SORT presenter over a common animal-content dataset (id,
habitat, mother, food, sound key, footprint sprite, covering, domestic/wild, land/sea/air) — build
the dataset once. Geography needs its own content dataset (countries/continents/flags/landmarks)
but reuses the same MATCH presenter; assigned here since it's the closest existing
"world-knowledge" building (its Animal World content already spans different world regions).

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
| `COOKING_MEASURES` | Cooking Measures | own mechanic (predict/compare quantities: more/less/enough, simple measuring cups) | `[ ]` added by the audit, assigned to Science Lab 2026-09-26 |
| `SEASONS` | Seasons | MATCH (scene/activity → season) | `[ ]` added by the audit, assigned to Science Lab 2026-09-26 |
| `DAY_NIGHT` | Day/Night Activities | MATCH (activity → time of day) | `[ ]` added by the audit, assigned to Science Lab 2026-09-26 |
| `SPACE` | Space | MATCH/SEQUENCE (planets, Earth/Moon, astronaut gear, planet size and order, gravity — later levels) | `[ ]` added by the audit, assigned to Science Lab 2026-09-26 |

Cooking Measures/Seasons/Day-Night/Space were unassigned after the first audit pass (no building
fit "world knowledge" beyond animals); assigned to Science Lab 2026-09-26. Seasons/Day-Night/Space
reuse the MATCH presenter Weather/Human Senses already establish; Cooking Measures is closer to
Sink or Float/Magnet's predict-and-observe shape.

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
| `RECYCLING` | Recycling | SORT (themed reskin of Sorting: waste into the right bin) | `[ ]` added by the audit |
| `MATCH_ITEM_TO_CATEGORY` | Match Item to Category | MATCH (generic "which category" tap, distinct from Zoo & Farm's animal-specific MATCH rows) | `[ ]` added by the audit |
| `SORT_LAUNDRY_CHORES` | Sort Laundry / Chores | SORT, themed reskin of Sorting (laundry by type/color, or chores by room) | `[ ]` added by the audit, assigned to Brain Gym 2026-09-26 |

Biggest single building by game count (18 original + Recycling, Match Item to Category, Sort
Laundry/Chores = 21) — expect to build its own shared memory/compare presenter early and reskin
most of the rest on top of TAP-THE-TARGET/CHOOSE/SORT/SEQUENCE. Recycling, Match Item to Category
and Sort Laundry/Chores were all missing from the original big-catalogue prompt (2026-09-26 audit
finding) but reuse mechanics Brain Gym already has.

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
