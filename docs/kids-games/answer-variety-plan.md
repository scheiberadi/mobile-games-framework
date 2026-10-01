# Plan: answer-method variety (fixing "almost every game is pick-one-of-N")

Status: **direction approved by the user on 2026-10-01, with the constraints below. Nothing is built.** The only
next step is the two prototypes in `docs/kids-games/answer-variety-prototypes.md` (Item to Shadow and Sorting);
nothing else is rolled out until both have been built and judged on the PC/device.

Original note: PLAN ONLY. It comes from user feedback on 2026-10-01: after playing several
games the user found them almost all the same (a multiple-choice question, Eva asks, the child taps one
answer) and "that's not fun, at all". The user's example: Item to Shadow should be drag and drop.

Method: every screen registered in `App/EvaGame.cs` was read for how the child answers (grep for
`DragItem`, `PathDragger`, `RotateDragger`, the shared presenter classes and their header comments). This is a
static reading of the code, not a play-through; "today" below is what the code says, not what was seen on a
device. Mechanisms here are proposals for discussion, not decisions.

## 0. Approved constraints (from the user's review, 2026-10-01)

1. **Two prototypes only, first:** Item to Shadow (drag-to-target, DT) and Sorting (drop-sort, DS). No rollout to
   other games until both are implemented and tested on the PC/device. The prototypes must show the new
   interaction is more engaging, more intuitive for a 4-5 year old, visually clearer and meaningfully different
   from "tap one correct answer". A mechanically different interaction that still feels like a quiz is not enough.
2. **Preserve the educational logic.** Keep the `Rules/` generators wherever possible; change mainly the
   presentation/interaction layer. Do not rewrite answer generation just to create variety.
3. **Do not force variety.** Genuine choice games stay choice games where the act of choosing is the activity
   (What Would You Do, Social Situations, Safety Scenarios). Outcome animations are fine.
4. **"About a third per building" is a design target, not a quota.** A building may have several games on one
   mechanism if they benefit; what matters is the overall experience not feeling repetitive.
5. **"Drag = fun" must be earned.** Every conversion must have a real gameplay affordance: spatial or action
   feedback. Avoid dragging an answer token from A to B with no consequence. Rows below that fail this test are
   marked **HOLD**, not scheduled.
6. **Every proposed change documents** what the child physically does, what is different from today, the
   immediate feedback/consequence, and why it fits the learning goal (the "Why more game-like" column).
7. **Order of work:** (1) DT prototype, (2) DS prototype, (3) PC/device evaluation, (4) adjust the shared
   presenters, (5) small low-risk conversions on existing `DragItem` screens, (6) roll DT/DS out, (7) hotspot
   presenter, (8) arcade real-time, (9) paint and remaining one-offs.
8. **No broad art generation yet.** The prototypes define exactly what art the reusable patterns need; the art
   spec is updated from that first.
9. **Accessibility is a hard requirement:** 4-5 year old non-reader baseline, forgiving drags using `DragItem`
   snapping, validated on a real device, not judged from code.

## 1. What exists today

| Mechanism | Where it is implemented | Used by (count) |
|---|---|---|
| Tap one of up to 4-6 answer tiles | `NumberHunt`, `Addition`, `WordToImage`... (each their own copy of the same shell) and the shared `MatchScreen` | about 85 of the ~125 games (see section 3) |
| Tap items in order | `NumberOrdering`, `FollowNumbers/Letters`, shared `SequenceScreen`, `SequenceRecallScreen` | about 12 |
| Drag to a slot and snap (`DragItem`) | Jigsaw, Tangram, Dress x3, Pack a Suitcase, Scrambled Word, Sentence Builder, One More/One Less, shared `AssemblyScreen` | about 17 |
| Drag along a path (`PathDragger`) | Finger Maze, Shortest Path, Avoid Obstacles, Collect Everything, shared `TraceScreen` | about 7 |
| Rotate (`RotateDragger`) | Rotate the Piece | 1 |
| Flip cards | `MemoryBoardScreen` (Classic Memory) | 1 |
| Free draw | `FreeDrawingScreen` | 1 |

The biggest single cause: **63 games across Zoo & Farm, Science Lab, Workshop, Art Studio, Brain Gym,
Friends' Park and Arcade are all instances of one `MatchScreen`**, a target (picture or sound) plus up to 4
tappable tiles, one correct. That includes games whose names promise action (Balloon Popping, Whack-a-Mole,
Fishing, Space Shooter, Fruit Catcher, Treasure Hunt, Sink or Float, Magnet, Recycling, Find the Differences).
The School and Playground games repeat the same shell with different art (`NumberHunt`'s shell is
copied by about 20 screens).

## 2. Rules every replacement must keep

- Non-reader child (4-5): picture and voice only, digits are the only visible text.
- Every button and drag handle at least `EvaUi.MinTap` (240) and inside the 1440 x 900 frame.
- Keep the existing session shape: 3-5 rounds, difficulty ladder, help ladder (retry, hint, demonstrate with
  the pointer hand), coin payout, end panel. A drag game's hint is the hand dragging, not tapping.
- Reuse existing machinery first (`DragItem`, `PathDragger`, `RotateDragger`, `PointerHand`); each new
  mechanic becomes one shared presenter configured per game, the way `MatchScreen` is now, not one copy per game.
- Keep the round generators in `Rules/` unchanged where possible (they already decide what is correct); only the
  way the child answers changes, so the existing Rules tests keep their value.
- Variety is not "no taps". Some games (What Would You Do, Social Situations, Safety Scenarios) are genuinely
  choose-a-response. The per-building mix is a design guideline (about a third on one mechanism), not a hard quota; the goal is an experience that does not feel repetitive.
- Every new interaction needs the screen audits (tap-target size, overlap, frame) extended to drag handles.

## 3. Game-by-game table

Legend. **Tier:** STRONG = real action and feedback, worth doing; OK = acceptable, moderate gain; HOLD = fails the
"drag must earn its place" test as currently framed, rethink before scheduling; KEEP = already varied, or a
genuine choice. **Effort:** S small change to an existing presenter, M new mode on an existing one, L new shared
presenter. Presenters: **DT** drag-to-target, **DS** drop-sort, **HS** hotspot scene (tap things inside one big
picture), **RT** real-time, **FL** fill/slider, **PA** paint. The last column answers: what the child does,
what feedback/consequence follows, and why it fits the learning goal.

### School (20 games)

| Game | Today | Proposed | Tier / Effort | Why more game-like (does / feedback / fit) |
|---|---|---|---|---|
| Count | tap each object, then numeral tile | keep counting taps, then "give me N": drag N objects into a basket | OK / M | Does: hands over objects one by one. Feedback: basket counts up, stops when N reached. Fit: one-to-one correspondence, the actual counting skill. |
| Number Hunt | tap numeral tile | HS: tap the number among many drifting numbers | OK / M | Does: searches a moving crowd. Feedback: the found number pops and cheers. Fit: visual number recognition among distractors. |
| Letter Hunt | tap letter tile | HS: tap every A in a garden | STRONG / M | Does: finds all copies. Feedback: each found letter blooms, remaining count. Fit: matches the original "find every A" brief. |
| Addition | tap sum tile | drag objects from both groups into one tray, then count | OK / M | Does: physically combines groups. Feedback: tray total updates as items land. Fit: addition as combining. |
| Subtraction | tap difference tile | drag objects away, then count what is left | OK / M | Does: removes objects. Feedback: they leave (fly off), remainder visible. Fit: subtraction as taking away. |
| Which Has More | tap bigger group | balance scale: drop both groups on pans | OK / M | Does: places groups. Feedback: scale tips toward the larger. Fit: comparison made visible. |
| One More / One Less | drag duck in/out of pond | (already drag) | KEEP | - |
| Number Ordering | tap in order | drag numbers into a train in order | OK / M | Does: builds a train. Feedback: car couples on, train rolls when complete. Fit: ordering. |
| Missing Number | tap answer tile | drag tile into the gap | HOLD | Token moved into a slot; little consequence beyond tap. Rethink (e.g. bridge that completes). |
| Number Line | tap answer tile | drag the hopper along the line to the landing dot | STRONG / M | Does: moves the character. Feedback: hops and lands, hops counted. Fit: number line as movement. |
| Multiplication | tap product tile | build the array: drag objects into Rows x Cols grid | STRONG / M | Does: fills a grid. Feedback: rows light up, total shown. Fit: multiplication as arrays. |
| Uppercase to Lowercase | tap tile | drag lowercase to uppercase | HOLD | Token-to-token. Possible reframing: post each letter into the matching mailbox (animation). |
| Beginning Sound | tap picture tile | DS: drag pictures into baskets by starting sound | OK / L | Does: sorts pictures. Feedback: basket jingles the sound. Fit: grouping by initial sound. |
| Rhyming | tap picture tile | drag picture next to its rhyme partner | HOLD | Token pairing; reframing needed. |
| Word to Image / Image to Word | tap tile | drag card onto picture/word | HOLD | Same token problem. Keep as tap until a stronger framing exists. |
| Letter to Sound | tap picture tile | drag letter onto picture | HOLD | Same. |
| Missing Letter | tap letter tile | drag into the gap | HOLD | Same as Missing Number. |
| Build a Word | tap letter tile | drag letters into the empty slots | OK / S | Does: assembles a word. Feedback: slot fills, word is spoken. Fit: spelling as assembling. |
| Scrambled Word, Sentence Builder | drag to slots | (already drag) | KEEP | - |

### Playground (14 games)

| Game | Today | Proposed | Tier / Effort | Why more game-like (does / feedback / fit) |
|---|---|---|---|---|
| Item to Shadow | tap silhouette | **DT prototype:** drag each object onto its shadow | STRONG / L (DT) | Does: moves the object. Feedback: it lands and fills its shadow, wrong shadow rejects it. Fit: shape matching is literally overlaying shape on outline. |
| Pattern Completion | tap shape tile | drag the next shape into the blank | OK / S | Does: places the next piece. Feedback: pattern plays through. Fit: completing a sequence. |
| Odd One Out | tap the odd item | HS, or drag the odd item out of the group | OK / S | Does: removes it. Feedback: it leaves, others settle. Fit: exclusion. |
| What's Missing | tap which is gone | drag the missing item back from a tray | OK / S | Does: restores it. Feedback: gap fills. Fit: memory of a set. |
| Which Doesn't Make Sense | tap impossible picture | HS tap, then drag it to where it belongs | OK / M | Does: finds then fixes. Feedback: cow walks to field. Fit: reasoning about the world. |
| Finger Maze, Follow Numbers/Letters, Shortest Path, Avoid Obstacles, Collect Everything | path drag | (already path drag) | KEEP | - |
| Rotate the Piece, Jigsaw, Tangram | rotate/drag | (already) | KEEP | - |

### Store and House

| Game | Today | Proposed | Tier / Effort | Why more game-like |
|---|---|---|---|---|
| Shopping (Exact Payment, Change) | tap tile | drag coins into the register | STRONG / M | Does: hands over money. Feedback: register total rises, drawer opens when exact. Fit: paying exact amounts. |
| Shopping (Compare Prices, Budget) | tap item | drag affordable items into basket until budget runs out | OK / M | Does: fills basket. Feedback: budget meter drops. Fit: budgeting. |
| Dress x3, Pack a Suitcase, House | drag | (already) | KEEP | - |

### Zoo & Farm (11 games, all `MatchScreen` today)

| Game | Today | Proposed | Tier / Effort | Why more game-like |
|---|---|---|---|---|
| Habitat | tap habitat | DT: drag animal into its habitat scene | STRONG / L (DT) | Does: moves animal home. Feedback: animal settles and reacts in its habitat, wrong habitat: it looks unhappy. Fit: habitat knowledge. |
| Mother | tap mother | drag baby to mother | OK / M | Feedback: mother nuzzles baby. |
| Food | tap food | drag the food to the animal | STRONG / M | Does: feeds it. Feedback: eats happily, or turns away. Fit: diet. |
| Footprint | tap animal | drag animal onto its footprint trail | OK / M | Feedback: animal walks the trail. |
| Covering | tap covering | drag fur/feathers/scales onto outline | OK / M | Feedback: animal gets "dressed". |
| Sound | tap animal | HS: tap the animal making the sound in a barnyard | STRONG / L (HS) | Does: listens and finds. Feedback: animal answers. Fit: sound-animal link. |
| Domestic vs Wild | tap pen | DS: drag animals into two pens | STRONG / L (DS) | Feedback: pens react. |
| Land, Sea or Air | tap zone | DS: three zones | STRONG / M | Feedback: swims/flies/walks away on landing. |
| Animal Babies | tap baby | pair parents and babies | OK / M | As Mother. |
| Animal Classification | tap class | DS: drag into class bins | OK / M | As Sorting. |
| Geography | tap tile | drag animal/landmark onto the map | OK / M | Feedback: lands on region. |

### Science Lab (13 games)

| Game | Today | Proposed | Tier / Effort | Why more game-like |
|---|---|---|---|---|
| Sink or Float | tap bucket (predict) | drag object into a water tank | STRONG / M | Does: drops it in. Feedback: it sinks or floats (the result IS the answer). Fit: experiment, not a quiz. |
| Magnet | tap bucket | drag the magnet over objects | STRONG / M | Does: moves magnet. Feedback: magnetic ones jump to it. Fit: discovery. |
| Living vs Non-living | tap tile | DS: two bins | OK / M | As Sorting. |
| Plant Growth | tap stages in order | drag stage cards into order slots | OK / M | Feedback: plant grows as stages land. |
| Human Senses | tap tile | drag eye/ear/nose/hand/tongue onto what it senses | OK / M | Feedback: sense "activates" (sound, smell lines). |
| Healthy vs Unhealthy | tap tile | feed the character the healthy foods | STRONG / M | Feedback: character happy or "ugh". |
| Weather | tap tile | drag weather symbol to the sky scene | HOLD | Token to scene with little consequence. |
| Dress for Weather | tap tile | drag clothes onto the character | STRONG / M | Reuse Dress presenter; feedback: character comfortable or shivering. |
| Cause and Effect | tap tile | place the cause, watch the effect | STRONG / M | Does: triggers it. Feedback: the effect plays. Fit: causality. |
| Cooking Measures | tap tile | FL: fill the cup to the line | STRONG / L (FL) | Does: pours. Feedback: level rises, overflow. Fit: measuring. |
| Seasons | tap tile | DS: four season columns | OK / M | |
| Day and Night | tap tile | DS or drag the sun | OK / M | Feedback: sky changes. |
| Space | tap tile | drag planets onto orbit marks | OK / M | Feedback: planet starts orbiting. |

### Workshop (10 games)

| Game | Today | Proposed | Tier / Effort | Why more game-like |
|---|---|---|---|---|
| Build a Car/Rocket/House/Boat/Robot, Bridge, Simple Physics | drag parts, then test | (already) | KEEP | - |
| Tool Selection | tap tool | drag tool onto the job | STRONG / M | Feedback: tool does the job. |
| Balance | tap tile | drag weights onto scale until level | STRONG / M | Feedback: scale tips and settles. |
| Help the Character | tap tile | drag the needed object to the character | OK / M | Feedback: character uses it. |

### Art Studio (10 games)

| Game | Today | Proposed | Tier / Effort | Why more game-like |
|---|---|---|---|---|
| Trace x3, Free Drawing | path drag / draw | (already) | KEEP | - |
| Color by Number | tap tile | PA: pick colour, tap regions | STRONG / L (PA) | Feedback: picture colours in. |
| Color by Instruction | tap tile | PA with spoken instruction | OK / M | |
| Finish the Drawing | tap tile | trace the missing half | STRONG / M | |
| Draw What You Hear | tap tile | drag stamps onto a canvas | OK / M | |
| Guided Drawing, Drawing Challenges | tap steps in order | trace each step | STRONG / M | |

### Brain Gym (22 games)

| Game | Today | Proposed | Tier / Effort | Why more game-like |
|---|---|---|---|---|
| Classic Memory | flip cards | (already) | KEEP | - |
| Remember the Sequence, Simon Says | tap in recalled order | (already distinct) | KEEP | - |
| What's Disappeared | tap tile | drag the returning item back | HOLD | Token return; check after Pattern/What's Missing results. |
| Remember the Location | tap tile | tap the cell where it was | OK / S | |
| Same or Different | tap tile | swipe or token | HOLD | |
| Match Rotation | tap tile | rotate piece to match | STRONG / S | `RotateDragger` exists; feedback: piece clicks into place. |
| Which Is Bigger | tap tile | drag crown / order by size | HOLD | |
| Complete the Picture | tap tile | drag piece into hole | OK / S | Feedback: picture completes. |
| Find the Differences | tap tile | HS: tap each difference | STRONG / L (HS) | Feedback: each marked. |
| Spot the Object | tap tile | HS: find in a busy scene | STRONG / M | |
| Follow the Path | tap tile | trace the path | STRONG / S | `PathDragger` exists. |
| What's Behind | tap tile | slide curtain away | OK / M | Feedback: reveals. |
| Perspective | tap tile | rotate view | OK / M | |
| Copy the Construction | tap tile | drag blocks to copy | STRONG / M | |
| Find the Missing Piece | tap tile | drag piece into gap | OK / S | |
| **Sorting** | tap category | **DS prototype:** drag items into bins | STRONG / L (DS) | Does: places objects in meaningful bins. Feedback: bin reacts and fills. Fit: classification is putting things away. |
| Recycling | tap bin | DS: waste into the right bin | STRONG / M | Feedback: bin lid, sound. |
| Match Item to Category | tap tile | DS or keep | OK / M | |
| Sort Laundry / Chores | tap tile | DS: hamper/rooms | STRONG / M | |
| Sequence Ordering | tap in order | drag into slots | OK / M | |

### Friends' Park (13 games)

| Game | Today | Proposed | Tier / Effort | Why more game-like |
|---|---|---|---|---|
| Emotion Matching | tap tile | connect faces to emotions | HOLD | |
| Facial Expression | tap tile | drag eyes/mouth onto a blank face | STRONG / M | Feedback: face changes as built. |
| What Would You Do, Social Situations, Safety Scenarios | tap tile | **Keep as choices**, add outcome animation | KEEP / S | Choosing is the activity (user ruling). |
| Empathy | tap tile | drag the comfort item to the sad character | STRONG / M | Feedback: character cheers up. |
| Listen and Choose | tap tile | drag named item to the character | HOLD | |
| Listen for Details | tap tile | HS: tap the detail | OK / M | |
| Follow 1/2/3 Instructions | tap in order | carry out instructions with the character | STRONG / M | Does: acts. Feedback: character does it. |
| Road Safety | tap tile | walk character across crossing under traffic light | STRONG / L | Feedback: cars stop/go. |

### Arcade (7 games)

| Game | Today | Proposed | Tier / Effort | Why more game-like |
|---|---|---|---|---|
| Balloon Popping | tap tile | RT: tap rising balloons | STRONG / L (RT) | Feedback: pops. |
| Whack-a-Mole | tap tile | RT: tap the right moles | STRONG / M | |
| Fishing | tap tile | RT: drag hook to the right fish | STRONG / M | |
| Space Shooter | tap tile | RT: aim and shoot | OK / M | Check suitability for age. |
| Fruit Catcher | tap tile | RT: drag basket under right fruit | STRONG / M | |
| Treasure Hunt | tap tile | HS: dig where the clue says | OK / M | |
| Platformer | tap in order | RT: tap to jump | OK / L | Check suitability. |

HOLD rows are not dropped; they wait for a stronger framing or for the prototypes' results.

## 4. New shared presenters and what each unlocks

| Presenter | Builds on | Games unlocked (STRONG/OK only, excluding HOLD) |
|---|---|---|
| DT drag-to-target | `DragItem`, Dress for the Occasion's snap shape | about 15 |
| DS drop-sort | `DragItem` with several bins | about 12 |
| HS hotspot scene | tap targets on one picture | about 8 |
| RT real-time | new update loop | about 7 |
| PA paint | `FreeDrawingScreen` canvas | 2-3 |
| FL fill/slider | new | 1-2 |
| Small `DragItem` changes on existing screens | `DragItem` | about 10 |

DT and DS come first, and only as the two prototypes (section 0).

## 5. Phasing (approved order)

1. DT prototype: Item to Shadow.
2. DS prototype: Sorting.
3. PC/device evaluation of both (acceptance criteria in `answer-variety-prototypes.md`).
4. Adjust the shared presenters from what was learned.
5. Small low-risk conversions using existing `DragItem` (S-effort STRONG/OK rows).
6. Roll DT/DS out to more games.
7. Hotspot presenter.
8. Arcade real-time.
9. Paint and the remaining one-off mechanics.

Each phase ends with a PC/device check; none of it can be validated in the cloud container.

## 6. Risks and open questions

- **Art:** the M4 gameplay-art prompts were written around tile layouts. DT and DS need each item as a
  separate cut-out and need target/bin art; some already-planned sheets will need changes. Check before
  generating more.
- **Branch:** M4 content is actively developed on `claude/eva-m4-full-content` (possibly from the PC). This plan
  touches the same screens, so agree an order with whoever is working on M4.
- **Tests:** the Rules generators and their tests can stay; the screen-level audits (overlap, frame floor) need
  drag-handle versions.
- **Accessibility for a 4-5 year old:** drag precision. `DragItem` already has snap radii; they must be
  validated per game on a real device.
- **Questions the user already answered (2026-10-01):** prototype order is Item to Shadow then Sorting;
  choose-a-response games stay choices; the per-building target is a guideline, not a quota.
- **Still open:** whether Arcade games should be real-time or a calmer version for this age (decide when step 8
  is reached, after the prototype results).
