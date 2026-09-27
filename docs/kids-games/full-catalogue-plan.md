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
| `LETTER_HUNT` | Letter Hunt | Literacy | letter recognition | TAP-THE-TARGET (reuses Number Hunt's shape almost exactly, target letter spoken not shown) | `[ ]` built, awaiting Unity pass |
| `ADDITION` | Addition | Mathematics | addition | TAP-THE-TARGET, visual objects → symbolic later | `[ ]` built, awaiting Unity pass |
| `SUBTRACTION` | Subtraction | Mathematics | subtraction | TAP-THE-TARGET, visible removal → symbolic later | `[ ]` built, awaiting Unity pass |
| `MULTIPLICATION` | Multiplication | Mathematics | multiplication | TAP-THE-TARGET, visual groups (rows×cols) → notation later | `[ ]` |
| `NUMBER_ORDERING` | Number Ordering | Mathematics | ordering | SEQUENCE, ascending → descending | `[ ]` built, awaiting Unity pass |
| `MISSING_NUMBER` | Missing Number | Mathematics | arithmetic, algebraic thinking | TAP-THE-TARGET (2 + ? = 5) | `[ ]` built, awaiting Unity pass |
| `ONE_MORE_ONE_LESS` | One More / One Less | Mathematics | counting, +/-1 | DRAG & DROP → numeric answer later | `[ ]` built, awaiting Unity pass |
| `WHICH_HAS_MORE` | Which Has More? | Mathematics | comparison | TAP-THE-TARGET, later "how many more" | `[ ]` built, awaiting Unity pass |
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
