# M4 scope audit: original brainstorm vs. the full-catalogue tracker

Cross-checks every item in `docs/kids-games/game-modes-backlog.md`'s 17 brainstorm categories (plus
meta features) against `docs/kids-games/full-catalogue-plan.md` (the tracker) and
`docs/superpowers/plans/2026-09-26-m4-full-content-plan.md` (the M4 plan). Requested 2026-09-26
after the first M4 plan draft turned out to silently drop several brainstorm items — this is the
"don't assume omission is intentional" pass.

Legend: **Covered** = represented under some POI already · **Gap** = in the brainstorm, absent
from the catalogue/plan entirely · **Orphaned** = was assigned somewhere (House) that no longer
takes games · **Overlap** = close enough to an existing entry that it's the same game under a
different name, not a separate one.

## 1. Logic and classification

| Backlog item | Status | POI | Note |
|---|---|---|---|
| Sort by size | Covered | Brain Gym → `SORTING` | folded into Sorting's "size/type/category" scope |
| Sort by type | Covered | Brain Gym → `SORTING` | same entry as above |
| Recycling (sort waste) | **Gap** | none | a themed reskin of Sorting, not currently a row anywhere |
| Match item to category | **Gap** | none | distinct from MATCH-to-a-specific-thing (habitat/mother/etc.) — a generic "which category" tap |
| Animal to habitat | Covered | Zoo & Farm → `ANIMAL_HABITAT` | |
| Animal to mother | Covered | Zoo & Farm → `ANIMAL_MOTHER` | |
| Item to shadow | **Gap** | none | match an object to its silhouette — not the same as Match Rotation (that's "is this rotated copy the same object", not silhouette matching) |
| Odd one out | Covered | Playground → `ODD_ONE_OUT` | |
| What's missing | Covered | Playground → `WHATS_MISSING` | |
| Complete the pattern | Covered | Playground → `PATTERN_COMPLETION` | |
| Sequence ordering | Covered | Brain Gym → `SEQUENCE_ORDERING` | |
| Which doesn't make sense | Covered | Playground → `WHICH_DOESNT_MAKE_SENSE` | |

**Gaps to place:** Recycling, Match item to category, Item to shadow.

## 2. Math and numbers

| Backlog item | Status | POI |
|---|---|---|
| Counting | Covered | School → `COUNT_OBJECTS` |
| Addition | Covered | School → `ADDITION` |
| Subtraction | Covered | School → `SUBTRACTION` |
| Multiplication | Covered | School → `MULTIPLICATION` |
| Number ordering asc/desc | Covered | School → `NUMBER_ORDERING` |
| Number hunt | Covered | School → `NUMBER_HUNT` (built) |
| Count the objects | Covered | School → `COUNT_OBJECTS` (built) |
| One more / one less | Covered | School → `ONE_MORE_ONE_LESS` |
| Which has more | Covered | School → `WHICH_HAS_MORE` |
| Number line | Covered | School → `NUMBER_LINE` |
| Missing number | Covered | School → `MISSING_NUMBER` |
| Money / shop | Covered | Store → `SHOPPING` |

**Fully covered — no gaps.**

## 3. Letters and reading

| Backlog item | Status | POI | Note |
|---|---|---|---|
| Letter hunt | Covered | School → `LETTER_HUNT` | next up to build |
| Uppercase to lowercase | **Gap** | none | the *entire rest of Literacy* was missing from the big catalogue prompt — only Letter Hunt made it in |
| Letter to sound | **Gap** | none | |
| Build a word | **Gap** | none | |
| Scrambled word | **Gap** | none | |
| Missing letter | **Gap** | none | |
| Beginning sound | **Gap** | none | |
| Rhyming | **Gap** | none | |
| Word to image | **Gap** | none | |
| Image to word | **Gap** | none | |
| Simple sentence builder | **Gap** | none | |

**This is the single biggest gap**: 10 of 11 Literacy games are unrepresented anywhere in the
catalogue or M4 plan. School is Literacy's obvious building (design doc 4.8 already names Literacy
as one of School's domains). Proposing all 10 join School as `4.2b` unless you want a different
split.

## 4. Spatial reasoning and puzzles

| Backlog item | Status | POI | Note |
|---|---|---|---|
| Puzzle blocks / tangram with ghost silhouette | **Gap** | none | distinct from Jigsaw (Jigsaw reassembles a cut photo; this is free-form shape placement against a silhouette) |
| Rotate the piece | **Gap** | none | an interactive rotate action; Brain Gym's Match Rotation only judges whether two shown rotations match, it never lets the child rotate anything |
| Jigsaw | Covered | Playground → `JIGSAW` | |
| What's behind the object | Covered | Brain Gym → `WHATS_BEHIND` | |
| Perspective | Covered | Brain Gym → `PERSPECTIVE` | |
| Copy the construction | Covered | Brain Gym → `COPY_THE_CONSTRUCTION` | |
| Find the missing piece | Covered | Brain Gym → `FIND_THE_MISSING_PIECE` | |

**Gaps to place:** Tangram/silhouette construction, Rotate the piece — both fit Playground
alongside Jigsaw (same spatial-reasoning domain, same DRAG & DROP family).

## 5. Mazes and pathfinding

| Backlog item | Status | POI | Note |
|---|---|---|---|
| Finger maze | Covered | Playground → `FINGER_MAZE` | |
| Avoid obstacles | Covered | Playground → `AVOID_OBSTACLES` | |
| Collect everything | Covered | Playground → `COLLECT_EVERYTHING` | |
| Follow numbers in order | **Gap/Overlap** | none directly | Arcade's Platformer ("jump on 1-2-3-4-5") is the mini-game skin of this idea, but the backlog also wants it as a plain Playground maze/path game, not just the Arcade reskin — treating as a real gap, not fully covered by Platformer alone |
| Follow letters in order | **Gap/Overlap** | none directly | same relationship to Platformer as above |
| Shortest path | Covered | Playground → `SHORTEST_PATH` | |

**Gaps to place:** Follow Numbers in Order, Follow Letters in Order — both fit Playground next to
Finger Maze (same NAVIGATION mechanic, ordered-checkpoint variant).

## 6. Creativity

| Backlog item | Status | POI | Note |
|---|---|---|---|
| Free drawing | **Gap** | none | an open canvas with no goal — every other Art Studio entry is guided/goal-directed |
| Guided drawing | Covered | Art Studio → `GUIDED_DRAWING` | |
| Trace shapes | Covered | Art Studio → `TRACE_SHAPES` | |
| Trace letters | Covered | Art Studio → `TRACE_LETTERS` | |
| Trace numbers | Covered | Art Studio → `TRACE_NUMBERS` | |
| Finish the drawing | Covered | Art Studio → `FINISH_THE_DRAWING` | |
| Color by number | Covered | Art Studio → `COLOR_BY_NUMBER` | |
| Color by instruction | Covered | Art Studio → `COLOR_BY_INSTRUCTION` | |
| Draw what you hear | Covered | Art Studio → `DRAW_WHAT_YOU_HEAR` | |
| AI-generated drawing challenges | Covered | Art Studio → `DRAWING_CHALLENGES` | |

**Gap to place:** Free Drawing — Art Studio, obviously (only building it could belong to).

## 7. Character / life simulation

| Backlog item | Status | POI | Note |
|---|---|---|---|
| Dress the character | **Gap** | none | the base dressing game — closest existing feature is the Creator's clothing picker, but that's character *creation*, not a replayable dressing *game* |
| Dress for the weather | Covered | Science Lab → `DRESS_FOR_WEATHER` | |
| Dress for the occasion | **Gap** | none | |
| Pack a suitcase | **Gap** | none | |
| Morning routine | **Orphaned** | was House, House now furniture-only | needs a new home or an explicit cut |
| Clean your room | **Orphaned** | was House | same |
| Cook a meal | **Orphaned** | was House | same |

**No natural existing building** for the dressing-game cluster (Dress the Character/Occasion,
Pack a Suitcase) — none of the 11 locations is a wardrobe/dressing building. Needs your call: fold
into an existing building (Store, as a second wardrobe-shopping angle?) or leave out of v1 scope.
The three House-orphaned items need the same kind of call — see "Open decisions" below.

## 8. World knowledge

| Backlog item | Status | POI | Note |
|---|---|---|---|
| Animal world (habitat/food/baby/sound/footprint/covering/domestic-wild/land-sea-air) | Covered | Zoo & Farm | all 8 facets present as separate `ANIMAL_*` rows |
| Geography (Romania, continents, flags, landmarks, animals by continent, foods by country, globe) | **Gap** | none | entirely absent — no building in the 11-location world is geography-flavored |
| Seasons | **Gap** | none | closest neighbor is Science Lab's Weather, but Weather ≠ Seasons |
| Day/night activities | **Gap** | none | closest neighbor is the orphaned House Clock |
| Space (planets, Earth/Moon, astronaut gear, planet order, gravity) | **Gap** | none | |

**Biggest structural gap**: Geography/Seasons/Day-night/Space have no assigned building at all,
and none of the 11 planned locations is naturally "world knowledge" the way School is naturally
Math/Literacy. Candidates: fold into Zoo & Farm (rename its scope to general world-knowledge, it
already carries Animal World) or Science Lab, or treat as a future 12th location. Flagging for
your decision rather than picking one.

## 9. Early science

| Backlog item | Status | POI |
|---|---|---|
| Sink or float | Covered | Science Lab → `SINK_OR_FLOAT` |
| Magnet game | Covered | Science Lab → `MAGNET` |
| Living vs non-living | Covered | Science Lab → `LIVING_VS_NONLIVING` |
| Plant growth stages | Covered | Science Lab → `PLANT_GROWTH` |
| Human body and five senses | Covered | Science Lab → `HUMAN_SENSES` |
| Healthy vs unhealthy | Covered | Science Lab → `HEALTHY_VS_UNHEALTHY` |
| Animal sounds | Covered | Zoo & Farm → `ANIMAL_SOUND` (reassigned by domain, not dropped) |
| Weather | Covered | Science Lab → `WEATHER` |

**Fully covered — no gaps** (Animal Sounds moved to Zoo & Farm in the big catalogue prompt itself,
which is a sensible domain fit, not an omission).

## 10. Memory

| Backlog item | Status | POI |
|---|---|---|
| Classic memory | Covered | Brain Gym → `CLASSIC_MEMORY` |
| Remember the sequence | Covered | Brain Gym → `REMEMBER_THE_SEQUENCE` |
| What disappeared | Covered | Brain Gym → `WHATS_DISAPPEARED` |
| Simon Says | Covered | Brain Gym → `SIMON_SAYS` |
| Remember the location | Covered | Brain Gym → `REMEMBER_THE_LOCATION` |

**Fully covered — no gaps.**

## 11. Listening and comprehension

| Backlog item | Status | POI |
|---|---|---|
| Follow 1/2/3 instructions | Covered | Friends' Park → `FOLLOW_1/2/3_INSTRUCTION` |
| Listen and choose | Covered | Friends' Park → `LISTEN_AND_CHOOSE` |
| Listen for details | Covered | Friends' Park → `LISTEN_FOR_DETAILS` |

**Fully covered — no gaps.**

## 12. Visual perception

| Backlog item | Status | POI |
|---|---|---|
| Find the differences | Covered | Brain Gym → `FIND_THE_DIFFERENCES` |
| Spot the hidden object | Covered | Brain Gym → `SPOT_THE_OBJECT` |
| Same or different | Covered | Brain Gym → `SAME_OR_DIFFERENT` |
| Match rotation | Covered | Brain Gym → `MATCH_ROTATION` |
| Which is bigger | Covered | Brain Gym → `WHICH_IS_BIGGER` |
| Complete the picture | Covered | Brain Gym → `COMPLETE_THE_PICTURE` |
| Follow the path through visual noise | Covered | Brain Gym → `FOLLOW_THE_PATH` |

**Fully covered — no gaps.**

## 13. Problem solving

| Backlog item | Status | POI | Note |
|---|---|---|---|
| Help the character (dog, bone, fence) | **Gap** | none | a small scenario-solving game, distinct from Tool Selection (which is "pick the right tool," not "solve this character's problem") |
| Tool selection | Covered | Workshop → `TOOL_SELECTION` | |
| Cause and effect | Covered | Science Lab → `CAUSE_AND_EFFECT` | |
| Simple physics | Covered | Workshop → `SIMPLE_PHYSICS` | |
| Bridge building | Covered | Workshop → `BRIDGE_BUILDING` | |
| Balance / scale | Covered | Workshop → `BALANCE` | |

**Gap to place:** Help the Character — fits Workshop (problem-solving building) next to Tool
Selection.

## 14. Real-life skills

| Backlog item | Status | POI | Note |
|---|---|---|---|
| Shopping | Covered | Store → `SHOPPING` | |
| Clock | **Orphaned** | was House | needs a new home or explicit cut |
| Calendar | **Orphaned** | was House | same |
| Cooking measures | **Gap** | none | no natural existing building (closest would have been House's Cook a Meal, also orphaned) |
| Sort laundry / chores | **Gap** | none | closest would have been House's Clean Your Room, also orphaned; could also be a themed reskin of Brain Gym's Sorting |
| Road safety | Covered | Friends' Park → `ROAD_SAFETY` | |
| Safety scenarios | Covered | Friends' Park → `SAFETY_SCENARIOS` | |

## 15. Social and emotional

| Backlog item | Status | POI |
|---|---|---|
| Emotion matching | Covered | Friends' Park → `EMOTION_MATCHING` |
| What would you do | Covered | Friends' Park → `WHAT_WOULD_YOU_DO` |
| Facial expression game | Covered | Friends' Park → `FACIAL_EXPRESSION` |
| Empathy | Covered | Friends' Park → `EMPATHY` |
| Social situations | Covered | Friends' Park → `SOCIAL_SITUATIONS` |

**Fully covered — no gaps** (interaction ambiguity on Facial Expression Game is fixed below, not a
scope gap).

## 16. Building / engineering

| Backlog item | Status | POI | Note |
|---|---|---|---|
| Build a car/rocket/house/boat/robot | Covered | Workshop → `BUILD_A_*` (5 rows) | |
| Test your creation | Covered (folded in) | Workshop | each Build-a-X's own test step, not a separate row — this is a design choice, not an omission |

**Fully covered — no gaps.**

## 17. Mini-games that don't look educational

| Backlog item | Status | POI |
|---|---|---|
| Balloon popping | Covered | Arcade → `BALLOON_POPPING` |
| Fruit catcher | Covered | Arcade → `FRUIT_CATCHER` |
| Space shooter | Covered | Arcade → `SPACE_SHOOTER` |
| Fishing | Covered | Arcade → `FISHING` |
| Whack-a-mole | Covered | Arcade → `WHACK_A_MOLE` |
| Platformer | Covered | Arcade → `PLATFORMER` |
| Treasure hunt | Covered (reward mechanic fixed below) | Arcade → `TREASURE_HUNT` |

**Fully covered — no gaps** (Treasure Hunt's reward-reveal wording fixed below, not a scope gap).

## Meta features

| Backlog item | Status | Note |
|---|---|---|
| Character creator | Implemented | existing |
| Little world (House reward/collection layer) | Implemented | existing |
| Adaptive difficulty | Implemented (as `DifficultyLadder`) | underlying system, not a building/game row |
| Daily Adventure | **Intentionally deferred** | backlog's own note: "meta features, discuss and accept one by one, after the first modes exist" — not dropped by this plan, just not yet due |
| Parent view (progress, behind parent gate) | **Not yet built** | design doc lists it as part of M3 ("local parent progress view"); it's a screen, not a building/game row, so it doesn't belong in the game catalogue — flagging so it isn't lost between the two docs |
| Engine with reusable interaction primitives | Implemented (as the "Shared reuse mechanics" section) | conceptually covered, ongoing as each mechanic gets built |

## Open decisions this audit surfaces (nothing below is assigned yet — waiting on your call)

1. **The 3 House-orphaned games** (Morning Routine, Clean Your Room, Cook a Meal) and **Clock,
   Calendar** (also orphaned): reassign to another POI (candidates below) or confirm a permanent
   cut. My candidates if reassigning: Clock/Calendar → Friends' Park (a "daily life" corner) or a
   new small "House" exception just for these two (tension with "House is furniture-only");
   Morning Routine/Clean Your Room/Cook a Meal → no clean existing fit, closest is Brain Gym
   (Sequence Ordering/Sorting reskins) or cut outright.
2. **World knowledge cluster** (Geography, Seasons, Day/night, Space): no building fits. Fold into
   Zoo & Farm (broaden it from "animals" to "world knowledge" generally) or Science Lab, or treat
   as a future 12th location beyond the current 11.
3. **Dressing-game cluster** (Dress the Character, Dress for the Occasion, Pack a Suitcase): no
   building fits (Store is closest, as a "second wardrobe" angle next to Shopping, but that's a
   stretch). Could also just stay a Creator-only feature (pick clothes at creation) rather than a
   replayable game.
4. **Cooking measures**, **Sort laundry/chores**: no clean fit once House's Cook a Meal/Clean Your
   Room are gone. Options: cut, or reskin onto Brain Gym's Sorting (laundry) / Science Lab
   (cooking measures, as a measurement game).
5. Below (not blocking, smaller): Recycling and Match Item to Category → proposed into Brain Gym's
   Sorting cluster; Item to Shadow, Tangram/silhouette construction, Rotate the Piece → proposed
   into Playground; Follow Numbers/Letters in Order → proposed into Playground; Free Drawing →
   proposed into Art Studio; Help the Character → proposed into Workshop. These five have a
   natural building even though they weren't in the original big-catalogue prompt, so the revised
   plan below slots them in directly rather than parking them as open decisions — say so if you'd
   rather review them first too.

Everything in decisions 1-4 is left **unassigned** in the revised plan below (not force-fit into
a POI) until you answer. Decision 5's items are added directly since their building is unambiguous.
