# M4 implementation plan: full content build-out, split by POI

**Scope:** every game in `docs/kids-games/full-catalogue-plan.md` (the tracker — keep that doc's
Status column current as work lands; this plan is the *order and shape* of the work). Localization
(previously M4) ships alongside M6 instead. Revised 2026-09-26 after a scope audit
(`docs/kids-games/m4-scope-audit.md`) found the first draft silently dropped several brainstorm
games; all confirmed-placeable ones are folded in below, and the audit's remaining open questions
were resolved the same day (House-orphaned games cut, dressing cluster → Store, world-knowledge
cluster split between Zoo & Farm/Science Lab, Sort Laundry/Chores → Brain Gym — see each subpart).
Only Daily Adventure and the parent progress view (non-game, meta/screen items) stay out of this
plan.

**Branch:** `claude/eva-m4-full-content`.

**Altitude of this plan:** one subpart per POI, each with (a) the "stand up the place"
prerequisite where it doesn't exist yet, (b) every game with its concrete interaction, difficulty
parameter, and a concrete Hint/Demonstrate idea, (c) a suggested build order inside the subpart.
Exact level tables, exact pixel layouts etc. are still nailed down when a game is actually built
(its own commit + spike notes), same as Count/Number Hunt — this plan fixes *what*, *in what
order*, and *roughly how the help ladder works for it*, not the last design detail.

**Every game reuses the same Retry step** (per `HelpLadder`/`CoinPayout`, unchanged from
Count/Number Hunt): 1st mistake is always a gentle wobble + soft "try again" voice line. The
Hint/Demo notes below describe only what's specific to that game — its 2nd-mistake hint and
3rd-mistake demonstration.

**Numbering is a draft proposal except 4.1** (Playground, the user's fixed pick). Order is
unchanged from the first draft per explicit instruction — this revision only adds missing games,
expands Brain Gym to its full explicit list, fixes two design ambiguities (Facial Expression Game,
Treasure Hunt's reward), and adds a Hint/Demo idea to every entry.

## Order and rationale (unchanged from the first draft)

| # | POI | Why here |
|---|---|---|
| 4.1 | Playground | user's pick: fastest unblock after School |
| 4.2 | School (remaining games) | no new place/screen needed, same pattern as Count/Number Hunt/Letter Hunt |
| 4.3 | Store (Shopping game) | no new place needed either; small, self-contained |
| 4.4 | Zoo & Farm | introduces MATCH and SORT, reused the most broadly below |
| 4.5 | Science Lab | reuses MATCH/SORT/SEQUENCE immediately, adds one new "predict → tiny simulation" shape |
| 4.6 | Brain Gym | biggest building (21 games); reuses TAP/MATCH/SORT/SEQUENCE/CHOOSE heavily, adds its own memory/compare mechanic |
| 4.7 | Friends' Park | reuses MATCH/CHOOSE, adds the "follow N instructions" mechanic |
| 4.8 | Art Studio | introduces TRACE, otherwise standalone |
| 4.9 | Workshop | introduces BUILD→TEST→OBSERVE, otherwise standalone |
| 4.10 | Arcade | last on purpose: every arcade game reskins a mechanic built somewhere above |

House is not a subpart: furniture-only, confirmed again 2026-09-26.

---

## 4.1 Playground

**Stand up the place** (mechanical, same shape every new POI below needs):
- `Rules/Places.cs`: add `PlaceId.Playground` + its `Place` entry (tap box, road waypoints,
  standing spot, sprite keys, voice key) — coordinates need real map-composition work, not just
  code.
- `Rules/Activities.cs`: add `BuildingId.Playground`.
- `App/Screens/Navigator.cs` + `App/EvaGame.cs`: add `ScreenId.Playground`, register
  `new BuildingScreen(BuildingId.Playground)` — `BuildingScreen` is already generic over
  `BuildingId`, so this is a two-line addition, not a new screen class.
- `BuildingScreen.BackdropFor`: add a `playground_list_bg`-style key (auto-placeholder sprite
  until real art exists).
- Map art (road + building sprite) is a real design task, not filler — flag for the user's art
  pass same as Number Hunt's tile-layout geometry was flagged for on-device verification.

**Games** (suggested build order: Pattern Completion → Odd One Out → What's Missing → Which
Doesn't Make Sense → Item to Shadow → Finger Maze → Follow Numbers/Letters in Order → Shortest
Path → Avoid Obstacles → Collect Everything → Rotate the Piece → Jigsaw → Tangram Construction;
cheapest/most-Number-Hunt-like first, NAVIGATION cluster together, DRAG&DROP cluster last):

- **Pattern Completion** — TAP-THE-TARGET. Eva shows a sequence with one blank, child taps the
  tile that continues it. Progression: AB → ABB → ABC → longer/less obvious repeats. Hint: hand
  points at the correct tile. Demo: hand taps it, only that tile stays interactive.
- **Odd One Out** — TAP-THE-TARGET. 4-5 objects, one doesn't belong; child taps it. Progression:
  obvious category → color/function/habitat → abstract. Hint: the odd object pulses. Demo: hand
  taps it, only it stays interactive.
- **What's Missing?** — TAP-THE-TARGET + a brief show/hide beat (sequence or group shown, one
  element removed, child picks what's gone from choices). Progression: set size, exposure time.
  Hint: the original sequence briefly replays once more before the choices re-enable. Demo: hand
  taps the correct choice, only it stays interactive.
- **Which Doesn't Make Sense?** — TAP-THE-TARGET over an authored content list (cow in ocean, fish
  in tree, ...); a curated pool with rotation like the parent-gate question pool. Hint: the
  impossible element visually wobbles/glows on its own. Demo: hand taps the correct picture.
- **Item to Shadow** — MATCH. An object is shown, child taps its matching silhouette among
  choices. Progression: silhouette similarity (distinct shapes → near-identical outlines). Hint:
  hand points at the correct silhouette. Demo: hand taps it, only it stays interactive.
- **Finger Maze** — new NAVIGATION mechanic: drag the character along a path from start to
  finish, wide corridors, no timer. Progression: maze size, obstacle count. Hint: the correct next
  stretch of path glows. Demo: the hand drags the character through the whole maze itself, then
  resets the character to start so the child repeats the now-highlighted path themselves.
- **Follow Numbers in Order** — reuses Finger Maze's renderer/drag; checkpoints are numbered, must
  be touched ascending. Progression: checkpoint count, numeral range. Hint: the next correct
  checkpoint pulses. Demo: hand drags through the correct order once, then the child repeats it.
- **Follow Letters in Order** — same as above with lettered checkpoints (a fixed target sequence
  Eva names first, e.g. "A, B, C"). Hint/Demo: same shape as Follow Numbers.
- **Shortest Path** — reuses Finger Maze's maze renderer; child picks between two/three drawn
  routes (tap to choose) rather than dragging. Progression: route count, how different their
  lengths are. Hint: the shorter route's start point pulses. Demo: hand traces the shorter route,
  then only tapping that route counts.
- **Avoid Obstacles** — reuses Finger Maze's drag mechanic with hazard tiles that end the round
  softly (gentle "oops, try again", not a fail state) instead of just walls. Hint: hazard tiles
  flash briefly. Demo: hand drags the character through safely, then resets for the child.
- **Collect Everything** — reuses Finger Maze's drag mechanic; the maze includes N pickups that
  must all be touched before the finish counts. Hint: the nearest uncollected pickup pulses. Demo:
  hand collects one pickup to show the motion, then resets for the child to finish the rest.
- **Rotate the Piece** — new mechanic: a rotate handle/gesture on a shown piece, child rotates it
  to match a target orientation (or fit a slot). Progression: rotation steps (90° only → any
  angle), piece complexity. Hint: an arrow shows which way to rotate. Demo: the piece animates
  through the correct rotation itself, then resets so the child repeats the gesture.
- **Jigsaw** — DRAG & DROP, reusing `DragItem` (generic: id, sprite, position, size,
  `BeginDrag`/`EndDrag` events, no built-in slot concept) with a new "snap when close to its own
  correct region" check. Progression: piece count (4 → 6 → 9 → 16 → 25+) — needs a piece-cutting
  content pipeline (source image sliced into N pieces). Hint: the correct region for the currently
  -held/nearest piece glows. Demo: hand drags one piece home, then the child finishes the rest.
- **Tangram / Puzzle Blocks** — DRAG & DROP over a ghost silhouette (free-form shape placement,
  distinct from Jigsaw's photo reassembly); reuses `DragItem` plus Jigsaw's snap-to-region logic
  with shape pieces instead of cut-photo pieces. Progression: shape count, silhouette complexity.
  Hint: the correct spot for the currently-held piece glows. Demo: hand places one shape, child
  finishes the rest.

---

## 4.2 School — remaining games

No new place/screen; each is a new `Activity` entry + its own Rules generator/tests/persisted
level/screen, exactly the Count/Number Hunt/Letter Hunt shape.

### Mathematics (suggested order: Addition → Subtraction → One More/One Less → Which Has More →
Number Ordering → Missing Number → Number Line → Multiplication → Follow Numbers in Order is
built in Playground, not here, despite being Math-adjacent, since it's a NAVIGATION game)

- **Addition** — TAP-THE-TARGET. Two visual groups (e.g. 2 apples + 3 apples), child taps the sum
  among numeral tiles. Progression: operand range, then drop the objects for bare `2 + 3 = ?`.
  Hint: hand points at the correct tile. Demo: hand taps it, only that tile stays interactive.
- **Subtraction** — same shape, objects visibly removed from a group first. Hint/Demo: same as
  Addition.
- **Multiplication** — visual rows × columns grid, child taps the total. Progression: grid size,
  then introduce `×` notation late. Hint: the hand traces one row, then one column, to invite a
  recount. Demo: hand taps the correct tile, only it stays interactive.
- **Number Ordering** — SEQUENCE: N numeral tiles shown scrambled, child taps them in order
  (ascending, later descending). Hint: the next-correct tile pulses. Demo: hand taps tiles in
  order once, then the sequence resets for the child to repeat.
- **Missing Number** — TAP-THE-TARGET: `2 + ? = 5` shown visually (objects, not just symbols, at
  low levels), child taps the missing value. Hint: hand points at the correct tile. Demo: hand
  taps it, only it stays interactive.
- **One More / One Less** — DRAG & DROP at low levels (drag a duck into/out of a pond), numeric
  tap answer at higher levels. Hint: the pond (or the correct numeral tile) glows. Demo: hand
  performs the action once, child repeats/confirms.
- **Which Has More?** — TAP-THE-TARGET: two groups shown, child taps the bigger one; later "how
  many more" asks for the numeric difference. Hint: hand points at the bigger group. Demo: hand
  taps it, only it stays interactive.
- **Number Line** — new small mechanic: a number line with Eva's character, hops N spaces
  forward/back; child taps the landing number. Hint: the correct landing number pulses. Demo: the
  character hops there itself, then resets for the child to tap it.

### Literacy beyond Letter Hunt (suggested order: Uppercase to Lowercase → Beginning Sound →
Rhyming → Word to Image → Image to Word → Letter to Sound → Missing Letter → Build a Word →
Scrambled Word → Simple Sentence Builder — MATCH-shaped ones first, spelling-composition ones
last since they're the most novel interaction)

- **Uppercase to Lowercase** — MATCH: an uppercase letter is shown/spoken, child taps its
  lowercase match among choices. Hint: hand points at the correct tile. Demo: hand taps it, only
  it stays interactive.
- **Beginning Sound** — MATCH, audio-led: Eva plays/says a word, child taps the picture whose name
  starts with the same sound. Hint/Demo: same shape as Uppercase to Lowercase.
- **Rhyming** — MATCH, audio-led: Eva says a word, child taps the picture that rhymes. Hint/Demo:
  same shape.
- **Word to Image** — MATCH: a short word is shown, child taps the matching picture (word is
  decorative/optional support per spec 4.9 — Eva also speaks it, gameplay never depends on
  reading it). Hint/Demo: same shape.
- **Image to Word** — MATCH, reverse direction of the above; same presenter.
- **Letter to Sound** — MATCH, audio-led (a letter shown, child taps the picture whose word starts
  with its sound); later blends letters (C+A+T=CAT) as a higher-level variant of the same
  presenter. Hint/Demo: same shape.
- **Missing Letter** — TAP-THE-TARGET: `C_T` shown, child taps the missing letter among choices.
  Hint: hand points at the correct tile. Demo: hand taps it, only it stays interactive.
- **Build a Word** — TAP-THE-TARGET: `CA_` shown with letter choices, child taps the one that
  completes a real (spoken) word. Hint/Demo: same shape as Missing Letter.
- **Scrambled Word** — DRAG & DROP: shuffled letters, child drags them into reading order to match
  the word Eva speaks. Progression: word length. Hint: the next-correct letter pulses. Demo: hand
  drags letters into place once, then resets for the child.
- **Simple Sentence Builder** — DRAG & DROP: pictograms first (drag pictures into a short spoken
  sentence's slots), words gradually replacing pictures at higher levels. Hint: the next-correct
  piece pulses. Demo: hand places one piece, child finishes the rest.

---

## 4.3 Store — Shopping game

No new place; Store already exists, this adds its second `Activity`. Own presenter (a mini shop
scene: coins/notes, a priced item, a "pay" action), matching the brief's "must feel like an actual
shop interaction, not an equation screen." Levels 1-7 per the brief (recognize coins → notes →
exact payment → simple addition → subtraction/change → compare prices → budget within an amount)
map onto `DifficultyLadder` (extend its range if 7 distinct levels are wanted; decide at build
time). Hint: the correct coin/note (or the cheaper item, at the compare-prices level) glows. Demo:
hand pays/selects correctly once, then resets the till for the child to repeat. New coin/note art
assets needed (auto-placeholder covers it meanwhile).

**Dressing-game cluster** (assigned here 2026-09-26, reuses the Furniture Store's shelf-of-choices
presentation plus `DragItem`):

- **Dress the Character** — DRAG & DROP: a shelf of clothing items, child drags them onto the
  character. No single "correct" answer at low levels (any combination is fine, reward is for
  completing a full outfit); higher levels add a themed goal (see Dress for the Occasion). Hint:
  the next empty slot (head/top/bottom/feet) glows. Demo: hand drags one item on, child finishes.
- **Dress for the Occasion** — MATCH/DRAG & DROP: Eva names an occasion (school, beach, winter,
  birthday, sports, camping), child drags the matching items onto the character. Hint: the correct
  next item glows on the shelf. Demo: hand drags it on, child finishes the outfit.
- **Pack a Suitcase** — DRAG & DROP: given a trip type (spoken by Eva), child drags the appropriate
  items into a suitcase from a mixed shelf. Hint: the next correct item glows. Demo: hand packs
  one item, child finishes.

---

## 4.4 Zoo & Farm

**Stand up the place** (same mechanical steps as 4.1, new `PlaceId.ZooFarm`/`BuildingId.ZooFarm`,
map art needed).

All ten games share one MATCH/SORT presenter over a common animal-content dataset (per-animal:
id, habitat, mother, food, sound key, footprint sprite, covering, domestic/wild, land/sea/air) —
build the dataset and the MATCH presenter once. Suggested order: Animal → Habitat (first, defines
the dataset schema) → Mother → Food → Footprint → Body Covering → Sound (five more MATCH variants,
cheap once the first lands) → Domestic vs Wild → Land/Sea/Air (SORT) → Animal Babies → Animal
Classification (composes the others, built last).

- **Animal → Habitat / Mother / Food / Footprint / Body Covering** — MATCH: Eva shows or names an
  animal, child taps the matching habitat/mother/food/footprint/covering among choices.
  Progression: distractor count, visual similarity. Hint: hand points at the correct choice. Demo:
  hand taps it, only it stays interactive.
- **Animal → Sound** — same MATCH shape, audio-led (Eva plays a sound, child taps the animal).
  Hint/Demo: same as above.
- **Domestic vs Wild / Land, Sea, Air** — SORT: child drags or taps animals into 2-3 buckets.
  Progression: animal count per round. Hint: the correct bucket for the currently-considered
  animal glows. Demo: hand sorts one animal, child finishes the rest.
- **Animal Babies** — MATCH/CHOOSE: identify which of two pictures is the baby. Hint/Demo: same
  MATCH shape as the habitat cluster.
- **Animal Classification** — SORT, combines 2+ attributes (e.g. "wild AND lives in water"),
  built last, composing the dataset the other nine already established. Hint: the bucket glows.
  Demo: hand sorts one animal correctly, child finishes.
- **Geography** — MATCH/CHOOSE (assigned here 2026-09-26): Eva asks/names (which is Romania,
  continents, flags, landmarks, animals by continent, foods by country), child taps the matching
  choice on a simplified globe/map or among picture choices. Own small content dataset (countries,
  flags, landmarks) built separately from the animal dataset, reusing the same MATCH presenter.
  Progression: distractor count/similarity, then which-continent/which-country compound questions.
  Hint: hand points at the correct choice. Demo: hand taps it, only it stays interactive.

---

## 4.5 Science Lab

**Stand up the place** (same mechanical steps).

- **Living vs Non-Living** — SORT, reuses Zoo & Farm's SORT presenter. Hint: bucket glows. Demo:
  hand sorts one, child finishes.
- **Plant Growth** — SEQUENCE: child arranges seed → sprout → plant → flower in order. Hint: the
  next-correct stage pulses. Demo: hand places one stage, child finishes.
- **Human Body / Senses** — MATCH (organ → sense). Hint: hand points at the correct choice. Demo:
  hand taps it, only it stays interactive.
- **Weather** — MATCH (scene → weather word/icon spoken by Eva). Hint/Demo: same as Human Senses.
- **Dress for the Weather** — MATCH/CHOOSE: given a weather scene, child taps the appropriate
  clothing item among choices. Hint/Demo: same shape.
- **Healthy vs Unhealthy** — SORT, age-appropriate, no moralizing tone. Hint/Demo: same as Living
  vs Non-Living.
- **Sink or Float** — new "predict, then a tiny drop animation, then observe" shape: child
  predicts (taps sink/float icon), the object visibly drops into water and settles. Hint: the
  object wobbles toward the correct prediction icon. Demo: the object drops and settles on its
  own first (showing the true answer), then the round repeats for the child to predict again.
- **Magnet Game** — same predict → observe shape, magnet instead of water. Hint/Demo: same as
  Sink or Float.
- **Cause and Effect** — the simplest BUILD→TEST→OBSERVE shape (act once, e.g. water the plant,
  observe result). Hint: the correct action's icon pulses. Demo: hand performs the action, child
  repeats/confirms it.
- **Cooking Measures** (assigned here 2026-09-26) — own small mechanic: a measuring cup/scale and
  a recipe step (e.g. "we need 2 cups"), child pours/adds until it matches; low levels are
  more/less/enough comparisons, higher levels introduce simple counting of measures. Hint: the
  target fill line glows. Demo: hand pours to the correct level once, then resets for the child.
- **Seasons** (assigned here 2026-09-26) — MATCH: a scene or activity is shown, child taps the
  matching season among four icons. Reuses the Weather/Human Senses MATCH presenter. Hint: hand
  points at the correct season. Demo: hand taps it, only it stays interactive.
- **Day/Night Activities** (assigned here 2026-09-26) — MATCH: an activity is shown (or spoken),
  child taps day or night. Same presenter as Seasons. Hint/Demo: same as Seasons.
- **Space** (assigned here 2026-09-26) — MATCH at low levels (planet/astronaut-gear → name, Earth
  vs Moon), SEQUENCE at higher levels (order planets by size or distance). Hint: hand points at
  the correct choice (MATCH) or the next-correct position pulses (SEQUENCE). Demo: hand
  taps/places it, only that choice stays interactive or the child finishes the rest.

---

## 4.6 Brain Gym

**Stand up the place** (same mechanical steps). Biggest single building — 21 games (18 from the
original catalogue prompt + Recycling, Match Item to Category and Sort Laundry/Chores, added by
the 2026-09-26 audit). All 21 listed individually below so the count is auditable against the
tracker.

Suggested order: **Classic Memory** first (defines the new show→hide→recall mechanic), then the
Remember/Simon cluster, then the CHOOSE/TAP visual-comparison cluster, then the two DRAG&DROP/
reskin games last.

1. **Classic Memory** — flip-and-match pairs. New mechanic, built first. Hint: two matching cards
   briefly flip face-up together. Demo: hand flips and matches one pair, child finishes the rest.
2. **Remember the Sequence** — show a sequence, hide it, child reproduces the order by tapping.
   Hint: the sequence replays once more, slower. Demo: hand taps the sequence itself, then it
   resets for the child to repeat.
3. **Simon Says** — same mechanic as Remember the Sequence, colour/sound sequence skin. Hint/Demo:
   same as Remember the Sequence.
4. **What Disappeared?** — CHOOSE: show objects, hide, remove one, child identifies which is gone.
   Hint: the empty spot where it was pulses. Demo: hand taps the correct answer.
5. **Remember the Location** — spatial variant of Classic Memory (remember *where*, not *what*).
   Hint: the correct spot glows briefly. Demo: hand taps it, only it stays interactive.
6. **Same or Different?** — CHOOSE: two images/scenes, child taps same/different. Hint: the
   differing detail (or, if identical, a reassuring highlight of a matching detail) pulses. Demo:
   hand taps the correct answer.
7. **Match Rotation** — CHOOSE: is this rotated shape the same object? Hint: the shape ghost-
   rotates back to upright briefly. Demo: hand taps the correct answer.
8. **Which Is Bigger?** — TAP-THE-TARGET: two objects, child taps the bigger one. Hint: hand
   points at it. Demo: hand taps it, only it stays interactive.
9. **Complete the Picture** — TAP-THE-TARGET: pick the piece that completes a shown picture's
   missing section. Hint: hand points at the correct choice. Demo: hand taps it.
10. **Find the Differences** — TAP-THE-TARGET: two near-identical scenes, tap each difference.
    Hint: the nearest untapped difference pulses. Demo: hand taps one, child finds the rest.
11. **Spot the Object** — TAP-THE-TARGET: find a hidden object in a busy scene. Hint: the object's
    area pulses. Demo: hand taps it.
12. **Follow the Path** — visual tracking (not movement): child's eyes/finger trace a line through
    visual noise to find where it leads, then taps the endpoint. Hint: the correct path segment
    highlights. Demo: hand traces the path, then taps the endpoint.
13. **What's Behind the Object?** — CHOOSE: spatial relationship (behind/in front/under). Hint:
    the hidden part briefly becomes semi-transparent. Demo: hand taps the correct answer.
14. **Perspective** — CHOOSE: same object/scene from another side. Hint: a brief rotate-preview of
    the object. Demo: hand taps the correct answer.
15. **Copy the Construction** — DRAG & DROP: observe a shown block arrangement, recreate it;
    reuses `DragItem` the way Jigsaw/Tangram (4.1) do. Hint: the next-correct block position
    glows. Demo: hand places one block, child finishes.
16. **Find the Missing Piece** — TAP-THE-TARGET: identify which puzzle piece is missing. Hint:
    hand points at the correct choice. Demo: hand taps it.
17. **Sorting** — SORT (size/type/visual-semantic category), reuses Zoo & Farm's SORT presenter
    over generic (non-animal) content. Hint: bucket glows. Demo: hand sorts one, child finishes.
18. **Sequence Ordering** — SEQUENCE, reskin of Science Lab's Plant Growth presenter over generic
    event content. Hint: next-correct step pulses. Demo: hand places one step, child finishes.
19. **Recycling** — SORT, themed reskin of Sorting: waste items into the right bin. Hint/Demo:
    same as Sorting.
20. **Match Item to Category** — MATCH, generic "which category" tap (distinct from Zoo & Farm's
    animal-specific MATCH rows). Hint: hand points at the correct category. Demo: hand taps it.
21. **Sort Laundry / Chores** — SORT, themed reskin of Sorting (laundry by type/color, or chores
    by room; assigned here 2026-09-26). Hint/Demo: same as Sorting.

---

## 4.7 Friends' Park

**Stand up the place** (same mechanical steps).

- **Emotion Matching** — MATCH: Eva names an emotion (happy/sad/angry/scared/...), child taps the
  matching face among several character portraits. Hint: hand points at the correct face. Demo:
  hand taps it, only it stays interactive.
- **Facial Expression Game** — same MATCH shape and presenter as Emotion Matching, run in reverse
  framing: a face is shown, Eva asks "how does this one feel?" and the child taps the matching
  emotion word/icon among choices (decided now: MATCH, not an open-ended expression-builder — that
  would need face-construction tooling this building doesn't otherwise have). Hint/Demo: same as
  Emotion Matching.
- **What Would You Do?**, **Empathy**, **Social Situations** — CHOOSE over short authored
  scenarios (scenario narrated, child taps the appropriate response among 2-3 pictured choices);
  one shared "scenario → choice" presenter and content pool covers all three. Hint: the correct
  choice pulses. Demo: hand taps it.
- **Listen and Choose** — CHOOSE, audio-led (Eva speaks a sentence, child taps the matching
  picture). Hint: hand points at the correct picture. Demo: hand taps it.
- **Listen for Details** — same shape as Listen and Choose, denser sentence and more similar-
  looking distractor pictures. Hint/Demo: same as Listen and Choose.
- **Follow 1/2/3 Instructions** — one presenter parameterized by instruction count: Eva speaks N
  chained actions, child performs them via drag/tap on a small scene. Hint: the next unperformed
  instruction's target pulses. Demo: hand performs the next step, child does the rest.
- **Road Safety** — TAP-THE-TARGET: cross on green, wait on red. Hint: the light or the correct
  action pulses. Demo: hand taps the correct action.
- **Safety Scenarios** — CHOOSE, reuses the "scenario → choice" presenter, handled gently (hot
  stove, stranger, lost). Hint: the correct choice pulses. Demo: hand taps it.

---

## 4.8 Art Studio

**Stand up the place** (same mechanical steps). Introduces **TRACE** (finger follows a path within
tolerance) — everything else here either sits directly on it or is its own small thing.

- **Trace Shapes** — build TRACE first (circle, triangle, square, star; progressively more complex
  shapes). Hint: the path glows brighter/pulses. Demo: the path traces itself once (a moving dot),
  then the child repeats it.
- **Trace Letters**, **Trace Numbers** — same TRACE mechanic, glyph outlines as path data.
  Hint/Demo: same as Trace Shapes.
- **Color by Number** — TAP-THE-TARGET: numbered regions, child taps a color then the matching
  region. Hint: the correct region pulses. Demo: hand taps color then region, only that region
  stays interactive after.
- **Color by Instruction** — CHOOSE variant (Eva speaks "make the roof red" instead of a number
  key). Hint/Demo: same as Color by Number.
- **Guided Drawing** — SEQUENCE + light TRACE: builds a picture step by step (roof → walls → door
  → windows). Hint: the next step's target/path pulses. Demo: hand performs the next step, child
  continues.
- **Finish the Drawing** — TAP or DRAG & DROP: half a picture shown, child completes the missing
  half from a couple of piece choices. Hint: the correct piece pulses. Demo: hand places it.
- **Draw What You Hear** — CHOOSE/DRAG & DROP combo: Eva narrates a simple scene ("a big yellow
  circle, a small blue triangle inside it"), child assembles it from shape pieces. Hint: the
  next-needed piece and its target spot both pulse. Demo: hand places one piece, child finishes.
- **Drawing Challenges** — reuses Guided Drawing's presenter over an offline-authored content pack
  (no runtime AI). Hint/Demo: same as Guided Drawing.
- **Free Drawing** — open canvas, no goal, no round structure and so no help ladder or per-round
  reward; a flat per-session coin on exit instead (design note, not a scope gap: this is the one
  game in the catalogue that isn't round-based, so "Hint/Demo" doesn't apply the same way — it
  gets a gentle idle prompt from Eva after a period of inactivity instead, e.g. "try drawing a
  sun!").

---

## 4.9 Workshop

**Stand up the place** (same mechanical steps). Shared pattern: **BUILD → TEST → OBSERVE**,
reusing `DragItem` for assembly (parts snap onto a chassis, same shape as House's furniture
placement) plus one new short "test" animation per game family.

- **Build a Car** — build the assembly presenter here first (parts: body, wheels, windows; slots
  fixed per part type). Test: the car visibly drives. Hint: the next-needed part's slot glows.
  Demo: hand places one part, child finishes; hand also demonstrates the test action once if the
  child hasn't triggered it after assembly completes.
- **Build a Rocket**, **Build a House**, **Build a Boat**, **Build a Robot** — same assembly
  presenter, new part sets and a matching test animation each (rocket launches, boat floats/sinks
  if assembled wrong at higher levels, robot does a small idle wave). Hint/Demo: same shape as
  Build a Car.
- **Bridge Building** — assembly presenter variant: blocks span a gap instead of snapping to a
  fixed chassis; test is a car crossing successfully. Hint: the next-needed block's position
  glows. Demo: hand places one block, child finishes.
- **Balance** — its own small mechanic: a scale, child adds/removes weights until level. Hint: the
  heavier side pulses. Demo: hand adds/removes one weight, child finishes leveling it.
- **Tool Selection** — TAP-THE-TARGET: a problem is shown (e.g. "cut this apple"), child taps the
  right tool among choices. Hint: hand points at the correct tool. Demo: hand taps it.
- **Simple Physics** — place ramps/blocks so a ball reaches a target; closest in shape to Finger
  Maze's "arrange, then verify" loop, build after Finger Maze exists. Hint: the correct next piece
  placement glows. Demo: hand places one piece and runs the ball partway, child finishes.
- **Help the Character** — CHOOSE/TAP-THE-TARGET: a small scenario is shown (dog, bone, fence),
  child picks the action that solves it. Hint: the correct action's icon pulses. Demo: hand taps
  it, only it stays interactive.

---

## 4.10 Arcade

**Stand up the place** (same mechanical steps). Deliberately last: every game here reskins a
mechanic already built above with an arcade coat of paint.

- **Balloon Popping** — TAP-THE-TARGET on moving/floating targets; content rule (even numbers, >5,
  target letter, correct answer) plugs into whichever Rules generator that content already has
  (Number Hunt/Letter Hunt/Addition's). Hint: the correct balloon glows. Demo: hand pops it, round
  pauses briefly then resumes for the child.
- **Whack-a-Mole** — same TAP-THE-TARGET-on-a-timer shape as Balloon Popping. Hint/Demo: same.
- **Fruit Catcher** — new "moving basket, drag left/right, things fall" mechanic; content rule
  decides valid catches. Hint: the correct falling item glows before it arrives. Demo: the basket
  auto-catches one correct item, then control returns to the child.
- **Fishing** — reuses Fruit Catcher's "catch the right one" rule with a cast/reel-in interaction.
  Hint: the correct fish glows. Demo: the line auto-catches it once, then control returns.
- **Space Shooter** — new "aim and tap to shoot a floating target" mechanic; answer targets float
  like Balloon Popping's. Hint: the correct target glows. Demo: the ship auto-fires at it once,
  then control returns.
- **Platformer** — reuses Finger Maze's path-following idea, reskinned as jump-through sequenced
  platforms (tap to jump) instead of a drag. Hint: the next-correct platform pulses. Demo: the
  character auto-jumps the sequence once, then resets for the child.
- **Treasure Hunt** — a meta-game chaining 3-4 small challenges, each borrowed from an
  already-built mechanic (a quick MATCH, a quick TAP-THE-TARGET, etc.). Reward is deterministic,
  not randomized: the coin payout for the run is computed the normal way (via `CoinPayout`/
  `HelpLadder`, same as any other game's per-round result, summed across the run's challenges)
  *before* the chest ever opens; the chest-opening animation always reveals that exact,
  already-known total — a reveal, never a randomized loot roll, consistent with the product's "no
  chests, loot boxes or random rewards" rule. Hint/Demo are per-challenge, inherited from whichever
  mechanic that challenge borrows.

---

## Open decisions carried over from the scope audit

All game-level unassigned items were resolved 2026-09-26 (House-orphaned games cut outright;
dressing cluster → Store 4.3; Geography → Zoo & Farm 4.4; Cooking Measures/Seasons/Day-Night/
Space → Science Lab 4.5; Sort Laundry/Chores → Brain Gym 4.6 — all folded into the subparts above).
Two non-game items remain out of this plan (they're screens/meta-features, not catalogue rows):
Daily Adventure (intentionally deferred per the original backlog) and the parent progress view
(owed per the design doc's M3 scope, tracked in the full-catalogue doc only so it isn't lost).

## Other open items (not blocking, still real)

- Map composition for 8 new places: real road/standing-spot coordinates and building art are a
  design pass, not just code — same caveat Number Hunt's hand-computed tile layout carried.
- Content datasets (Zoo & Farm's animal facts, Which Doesn't Make Sense's scenario pool, Friends'
  Park's social scenarios, Art Studio's letter/shape trace paths, Literacy's word lists) are
  authored content, not generated — each needs its own small content-authoring pass before its
  game is playable with "real content."
