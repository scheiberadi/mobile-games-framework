# M4 implementation plan: full content build-out, split by POI

**Scope:** every building/game in `docs/kids-games/full-catalogue-plan.md` (the tracker — keep
that doc's Status column current as work lands; this plan is the *order and shape* of the work,
that doc is the *checklist*). Localization (previously M4) ships alongside M6 instead.

**Branch:** `claude/eva-m4-full-content`.

**Altitude of this plan:** one subpart per POI (building), each with (a) the "stand up the place"
prerequisite where it doesn't exist yet, (b) every game in that building with its concrete
interaction, difficulty parameter and help/hint idea sketched at the same level Number Hunt's
spike notes were written up, and (c) a suggested build order inside the subpart. Exact level
tables, exact pixel layouts etc. are nailed down when a game is actually built (its own commit +
spike notes), same as Count/Number Hunt/Letter Hunt — this plan fixes *what* and *in what order*,
not the last design detail.

**Numbering is a draft proposal except 4.1**, which the user fixed as Playground. Everything after
4.1 is ordered by dependency (cheapest wins and shared-mechanic groundwork first), open to
reordering on review.

## Order and rationale

| # | POI | Why here |
|---|---|---|
| 4.1 | Playground | user's pick: fastest unblock after School (reuses TAP-THE-TARGET; NAVIGATION and DRAG&DROP-jigsaw are its only new mechanics) |
| 4.2 | School (remaining 9 games) | no new place/screen needed, same pattern as Count/Number Hunt/Letter Hunt, purely incremental |
| 4.3 | Store (Shopping game) | no new place needed either; small, self-contained, closes out the 3 already-existing POIs before new ones start |
| 4.4 | Zoo & Farm | introduces MATCH and SORT, the two mechanics reused the most broadly below |
| 4.5 | Science Lab | reuses MATCH/SORT/SEQUENCE immediately, adds one new "predict → tiny simulation" shape |
| 4.6 | Brain Gym | biggest building (18 games); reuses TAP/MATCH/SORT/SEQUENCE/CHOOSE heavily once they exist, adds its own memory/compare mechanic |
| 4.7 | Friends' Park | reuses MATCH/CHOOSE, adds the "follow N instructions" mechanic |
| 4.8 | Art Studio | introduces TRACE, otherwise standalone from the rest |
| 4.9 | Workshop | introduces BUILD→TEST→OBSERVE, otherwise standalone |
| 4.10 | Arcade | last on purpose: every arcade game reskins a mechanic built somewhere above, so it's cheapest once everything else exists |

House is not a subpart: furniture-only per the 2026-09-26 direction, and "more furniture" is a
content/art backlog item, not a game.

---

## 4.1 Playground

**Stand up the place** (mechanical, same shape every new POI below needs):
- `Rules/Places.cs`: add `PlaceId.Playground` + its `Place` entry (tap box, road waypoints,
  standing spot, sprite keys, voice key) — coordinates need real map-composition work, not just
  code, since the map world was sized for ~11 places but only House/School/Store are placed today.
- `Rules/Activities.cs`: add `BuildingId.Playground`.
- `App/Screens/Navigator.cs` + `App/EvaGame.cs`: add `ScreenId.Playground`, register
  `new BuildingScreen(BuildingId.Playground)` — `BuildingScreen` is already generic over
  `BuildingId` (confirmed by reading it), so this is a two-line addition, not a new screen class.
- `BuildingScreen.BackdropFor`: add a `playground_list_bg`-style key (auto-placeholder sprite
  until real art exists, same as every other icon).
- Map art (road + building sprite) is a real design task, not filler — flag for the user's art
  pass same as Number Hunt's tile-layout geometry was flagged for on-device verification.

**Games** (suggested build order: Pattern Completion → Odd One Out → What's Missing → Which
Doesn't Make Sense → Finger Maze → Shortest Path → Avoid Obstacles → Collect Everything → Jigsaw,
cheapest/most-Number-Hunt-like first, NAVIGATION cluster together, Jigsaw last since it needs new
DRAG&DROP-snap-without-a-fixed-slot-id logic):

- **Pattern Completion** — TAP-THE-TARGET. Eva shows a sequence with one blank, child taps the
  tile that continues it. Progression: AB → ABB → ABC → longer/less obvious repeats. Nearly
  identical shape to Number Hunt (round generator picks a pattern + distractor tiles).
- **Odd One Out** — TAP-THE-TARGET. 4-5 objects, one doesn't belong; child taps it. Progression:
  obvious category → color/function/habitat → abstract. Needs a small content set (grouped
  object families) rather than pure generation.
- **What's Missing?** — TAP-THE-TARGET + a brief show/hide beat (sequence or group shown, one
  element removed, child picks what's gone from choices). Progression: set size, exposure time.
- **Which Doesn't Make Sense?** — TAP-THE-TARGET over an authored content list (cow in ocean,
  fish in tree, ...); no generator, a curated pool with rotation like the parent-gate question
  pool.
- **Finger Maze** — new NAVIGATION mechanic: drag the character along a path from start to
  finish, wide corridors, no timer. Progression: maze size, obstacle count. First game to need
  actual path/maze data + drag-follows-a-corridor input, so it anchors the mechanic.
- **Shortest Path** — reuses Finger Maze's maze renderer; child picks between two/three drawn
  routes (tap to choose) rather than dragging. Progression: route count, how different their
  lengths are.
- **Avoid Obstacles** — reuses Finger Maze's drag mechanic with hazard tiles that end the round
  softly (gentle "oops, try again", not a fail state) instead of just walls.
- **Collect Everything** — reuses Finger Maze's drag mechanic; the maze includes N pickups that
  must all be touched before the finish counts as the correct end.
- **Jigsaw** — DRAG & DROP, reusing `DragItem` (confirmed generic: takes an id, sprite, position,
  size, `BeginDrag`/`EndDrag` events, no built-in notion of slots) with a new "snap when close to
  its own correct region" check instead of House's fixed-slot-per-item-id lookup. Progression:
  piece count (4 → 6 → 9 → 16 → 25+, per the brief) — needs a piece-cutting content pipeline
  (source image sliced into N pieces), the one genuinely new content-production tool this subpart
  needs.

---

## 4.2 School — remaining 9 games

No new place/screen; each is a new `Activity` entry + its own Rules generator/tests/persisted
level/screen, exactly the Count/Number Hunt/Letter Hunt shape. Suggested order: Addition →
Subtraction → One More/One Less → Which Has More → Number Ordering → Missing Number → Number
Line → Multiplication (grid visuals are the one with new art needs) — grouped so the "objects
change by N" family (Addition/Subtraction/One More-Less) shares one round-generator shape before
branching into the more different ones.

- **Addition** — TAP-THE-TARGET. Two visual groups (e.g. 2 apples + 3 apples), child taps the sum
  among numeral tiles. Progression: operand range, then drop the objects for bare `2 + 3 = ?`.
- **Subtraction** — same shape, objects visibly removed from a group first.
- **Multiplication** — visual rows × columns grid, child taps the total. Progression: grid size,
  then introduce `×` notation late.
- **Number Ordering** — SEQUENCE: N numeral tiles shown scrambled, child taps them in order
  (ascending, later descending). New interaction (ordered-tap-sequence) but small.
- **Missing Number** — TAP-THE-TARGET: `2 + ? = 5` shown visually (objects, not just symbols, at
  low levels), child taps the missing value.
- **One More / One Less** — DRAG & DROP at low levels (drag a duck into/out of a pond), numeric
  tap answer at higher levels.
- **Which Has More?** — TAP-THE-TARGET: two groups shown, child taps the bigger one; later "how
  many more" asks for the numeric difference.
- **Number Line** — new small mechanic: a number line with Eva's character, hops N spaces
  forward/back; child taps the landing number. Reused later if a "jump forward" idea is wanted
  elsewhere, but self-contained for now.

---

## 4.3 Store — Shopping game

No new place; Store already exists, this adds its second `Activity`. Own presenter (not a
tap-numeral-tile game): a mini shop scene (coins/notes, a priced item, a "pay" action), matching
the brief's "must feel like an actual shop interaction, not an equation screen." Levels 1-7 per
the brief (recognize coins → notes → exact payment → simple addition → subtraction/change →
compare prices → budget within an amount) map directly onto the existing `DifficultyLadder`
(1-6 range reused, or extended if 7 distinct levels are wanted — decide at build time). New coin/
note art assets needed (auto-placeholder covers it meanwhile).

---

## 4.4 Zoo & Farm

**Stand up the place** (same mechanical steps as 4.1, new `PlaceId.ZooFarm`/`BuildingId.ZooFarm`,
map art needed).

All ten games share one MATCH/SORT presenter over a common animal-content dataset (per-animal:
id, habitat, mother, food, sound key, footprint sprite, covering, domestic/wild, land/sea/air) —
build the dataset and the MATCH presenter once, then SORT reuses the same dataset with a
different question shape. Suggested order: Animal → Habitat (first, defines MATCH + the
dataset schema) → Animal → Mother → Animal → Food → Animal → Footprint → Animal → Body Covering
(five MATCH variants, cheap once the first lands) → Domestic vs Wild → Land/Sea/Air (SORT, reuses
the same dataset) → Animal Babies → Animal Classification (multi-attribute, highest level, built
last since it composes the others' data).

- **MATCH shape** (Habitat/Mother/Food/Footprint/Covering/Sound): Eva shows or names an animal,
  child taps the matching habitat/mother/food/footprint/covering/sound among choices. Progression:
  distractor count, how visually similar the distractors are.
- **Animal → Sound**: same MATCH shape, audio-led (Eva plays a sound, child taps the animal).
- **SORT shape** (Domestic vs Wild, Land/Sea/Air): child drags or taps animals into 2-3 buckets.
  Progression: animal count per round, ambiguity (e.g. a "sometimes domestic" edge case at higher
  levels only if it stays fair).
- **Animal Babies**: MATCH/CHOOSE variant — identify which of two pictures is the baby.
- **Animal Classification**: combines 2+ attributes at once (e.g. "wild AND lives in water") —
  built last, composing the dataset the other nine already established.

---

## 4.5 Science Lab

**Stand up the place** (same mechanical steps).

- **Living vs Non-Living** — SORT, cheapest, build first (reuses Zoo & Farm's SORT presenter
  directly over a new content set).
- **Plant Growth** — SEQUENCE: child arranges seed → sprout → plant → flower in order. First
  SEQUENCE game (Number Ordering in 4.2 may land first chronologically; whichever comes first
  defines the shared ordered-tap-sequence mechanic, this one just reuses it).
- **Human Body / Senses** — MATCH (organ → sense), reuses Zoo & Farm's MATCH presenter over a new
  small dataset.
- **Weather** — MATCH (scene → weather word/icon spoken by Eva).
- **Dress for the Weather** — MATCH/CHOOSE: given a weather scene, child taps the appropriate
  clothing item among choices.
- **Healthy vs Unhealthy** — SORT, age-appropriate, no moralizing tone per the brief.
- **Sink or Float** — new small "predict, then a tiny physics-ish drop animation, then observe"
  shape: child predicts (taps sink/float icon), the object visibly drops into water and settles;
  correctness is about the prediction, the animation is feedback not the test itself.
- **Magnet Game** — same predict → observe shape as Sink or Float, reuses its presenter with a
  magnet instead of water.
- **Cause and Effect** — the simplest version of Workshop's BUILD→TEST→OBSERVE (act once, e.g.
  water the plant, observe result) — built here since it's Science-flavored, but shares real
  plumbing with 4.9 Workshop; whichever of the two lands first defines the shared shape.

---

## 4.6 Brain Gym

**Stand up the place** (same mechanical steps). Biggest single building (18 games) — expect this
subpart to take the longest.

Suggested order: build the one genuinely new mechanic (show → hide → recall) once via **Classic
Memory** (simplest form of it), then fan out.

- **Classic Memory** — flip-and-match pairs. New mechanic, built first in this subpart.
- **Remember the Sequence** / **Simon Says** — same underlying "show a sequence, then have the
  child reproduce it" shape; Simon Says is a reskin (colour/sound sequence) of Remember the
  Sequence, build together.
- **What Disappeared?** — CHOOSE: show objects, hide, remove one, child identifies which is gone.
  Reuses the show/hide timing from Classic Memory.
- **Remember the Location** — spatial variant of Classic Memory (remember *where*, not *what*).
- **Same or Different?**, **Match Rotation**, **Which Is Bigger?**, **Find the Differences**,
  **Spot the Object**, **Find the Missing Piece**, **Complete the Picture** — all CHOOSE or
  TAP-THE-TARGET over paired/annotated images; one shared "compare two images" or "tap the region"
  presenter covers most of these, build the first (Same or Different?) as the template.
- **Follow the Path**, **What's Behind the Object?**, **Perspective** — CHOOSE, visual reasoning
  over a static scene; group together, likely small variations on one presenter.
- **Copy the Construction** — DRAG & DROP: recreate a shown block arrangement; reuses `DragItem`
  the way Jigsaw (4.1) does, build after Jigsaw exists so the snap-to-region logic is proven.
- **Sorting** — thin reskin of Zoo & Farm's SORT presenter over generic (non-animal) content.
- **Sequence Ordering** — thin reskin of Science Lab's SEQUENCE presenter (Plant Growth) over
  generic event content.

---

## 4.7 Friends' Park

**Stand up the place** (same mechanical steps).

- **Emotion Matching** — MATCH (face/scene → emotion word spoken by Eva), reuses the Zoo & Farm
  MATCH presenter directly. Build first, cheapest.
- **Facial Expression Game** — MATCH variant, or "make the character look X" if a simple
  expression-builder interaction is wanted instead — decide the exact interaction at build time.
- **What Would You Do?**, **Empathy**, **Social Situations** — CHOOSE over short authored
  scenarios (scenario shown/narrated, child taps the appropriate response among 2-3 pictured
  choices); one shared "scenario → choice" presenter and content pool covers all three.
- **Listen and Choose**, **Listen for Details** — CHOOSE, audio-led (Eva speaks a sentence, child
  taps the matching picture); "for Details" is the same shape with a denser sentence and more
  similar-looking distractor pictures.
- **Follow 1/2/3 Instructions** — one presenter parameterized by instruction count: Eva speaks N
  chained actions, child performs them via drag/tap on a small scene. Build the 1-instruction
  case first, 2 and 3 are the same presenter with a longer instruction list.
- **Road Safety** — TAP-THE-TARGET: cross on green, wait on red; small standalone.
- **Safety Scenarios** — CHOOSE, reuses the "scenario → choice" presenter from What Would You Do?,
  handled gently per the brief (hot stove, stranger, lost).

---

## 4.8 Art Studio

**Stand up the place** (same mechanical steps). Introduces **TRACE**, this building's one new
mechanic (finger follows a path within tolerance) — everything else here either sits directly on
it or is its own small thing.

- **Trace Shapes** — build TRACE first here (circle, triangle, square, star; progressively more
  complex shapes as the path gets longer/tighter tolerance).
- **Trace Letters**, **Trace Numbers** — same TRACE mechanic, different path data (glyph
  outlines) and progression (uppercase → lowercase for letters; digit complexity for numbers).
- **Color by Number** — TAP-THE-TARGET: numbered regions, child taps the correct color swatch
  then the region (or taps a color then paints the matching region) — small standalone.
- **Color by Instruction** — CHOOSE variant of the above (Eva speaks "make the roof red" instead
  of a number key).
- **Guided Drawing** — SEQUENCE + light TRACE: builds a picture step by step (roof → walls → door
  → windows), each step is a small trace or tap-to-place action.
- **Finish the Drawing** — TAP or DRAG & DROP: half a picture shown, child completes the missing
  half from a couple of piece choices.
- **Draw What You Hear** — CHOOSE/DRAG & DROP combo: Eva narrates a simple scene ("a big yellow
  circle, a small blue triangle inside it"), child assembles it from shape pieces — the most
  composite game in this building, build last once TRACE/placement pieces both exist.
- **Drawing Challenges** — reuses Guided Drawing's presenter over an offline-authored content
  pack (no runtime AI, per the brief); effectively "more Guided Drawing content," not new code.

---

## 4.9 Workshop

**Stand up the place** (same mechanical steps). Shared pattern: **BUILD → TEST → OBSERVE**,
reusing `DragItem` for assembly (drag parts onto a chassis, each snapping into its own slot,
exactly House's furniture-placement shape) plus one new short "test" animation per game family.

- **Build a Car** — build the assembly presenter here first (parts: body, wheels, windows; slots
  fixed per part type). Test: the car visibly drives.
- **Build a Rocket**, **Build a House**, **Build a Boat**, **Build a Robot** — same assembly
  presenter, new part sets and a matching test animation each (rocket launches, boat floats/sinks
  if assembled wrong at higher levels, robot does a small idle wave). Build in whatever order
  matches available art first.
- **Bridge Building** — assembly presenter variant: blocks span a gap instead of parts snapping to
  a fixed chassis; test is a car crossing successfully.
- **Balance** — its own small mechanic: a scale, child adds/removes weights until level;
  self-contained, no assembly reuse.
- **Tool Selection** — TAP-THE-TARGET: a problem is shown (e.g. "cut this apple"), child taps the
  right tool among choices; no assembly, simplest game in this building.
- **Simple Physics** — its own mechanic: place ramps/blocks so a ball reaches a target, closest
  in shape to Finger Maze's puzzle-then-verify loop from 4.1; build after Finger Maze exists so
  the "arrange pieces, then run a short simulation" pattern is proven once.

---

## 4.10 Arcade

**Stand up the place** (same mechanical steps). Deliberately last: every game here reskins a
mechanic already built above with an arcade coat of paint, so this subpart should be the cheapest
per game once 4.1-4.9 exist.

- **Balloon Popping** — TAP-THE-TARGET on moving/floating targets; content rule (even numbers,
  >5, target letter, correct answer) plugs into whichever Rules generator that content already
  has (Number Hunt/Letter Hunt/Addition's).
- **Whack-a-Mole** — same TAP-THE-TARGET-on-a-timer shape as Balloon Popping, different visual.
- **Fruit Catcher** — new small "moving basket, drag left/right, things fall" mechanic; content
  rule decides valid catches.
- **Fishing** — reuses Fruit Catcher's "catch the right one" rule shape with a cast/reel-in
  interaction instead of a moving basket.
- **Space Shooter** — new small "aim and tap to shoot a floating target" mechanic; answer targets
  float like Balloon Popping's, aiming is the new part.
- **Platformer** — reuses Finger Maze's (4.1) path-following idea, reskinned as jump-through
  sequenced platforms (tap to jump) instead of a drag.
- **Treasure Hunt** — a meta-game: chains 3-4 small challenges, each borrowed from an already-built
  mechanic (a quick MATCH, a quick TAP-THE-TARGET, etc.), ending in a reward chest animation.
  Built last of all, once there's a real menu of mechanics to draw from.

---

## Open decisions to flag before/while building (not blocking review of this plan, but real)

- Map composition for 8 new places: real road/standing-spot coordinates and building art are a
  design pass, not just code — same caveat Number Hunt's hand-computed tile layout carried.
  Playground's placement is the first one needed for 4.1.
- Content datasets (Zoo & Farm's animal facts, Which Doesn't Make Sense's scenario pool, Friends'
  Park's social scenarios, Art Studio's letter/shape trace paths) are authored content, not
  generated — each needs its own small content-authoring pass before its game is playable with
  "real content," per the brief's own bar for calling a game done.
- A couple of games (Facial Expression Game, Draw What You Hear, Treasure Hunt) have more than one
  reasonable concrete interaction; this plan notes the options, final call happens at build time
  the same way Number Hunt's design decisions were picked and recorded in its own spike notes.
