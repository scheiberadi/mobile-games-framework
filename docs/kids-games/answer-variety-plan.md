# Plan: answer-method variety (fixing "almost every game is pick-one-of-N")

Status: PLAN ONLY. Nothing here is built. It comes from user feedback on 2026-10-01: after playing several
games the user found them almost all the same (a multiple-choice question, Eva asks, the child taps one
answer) and "that's not fun, at all". The user's example: Item to Shadow should be drag and drop.

Method: every screen registered in `App/EvaGame.cs` was read for how the child answers (grep for
`DragItem`, `PathDragger`, `RotateDragger`, the shared presenter classes and their header comments). This is a
static reading of the code, not a play-through; "today" below is what the code says, not what was seen on a
device. Mechanisms here are proposals for discussion, not decisions.

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
  choose-a-response. Target is that no more than about a third of a building's games share one mechanism.
- Every new interaction needs the screen audits (tap-target size, overlap, frame) extended to drag handles.

## 3. Game-by-game table

Legend. Effort: **S** config or small change to an existing presenter, **M** new mode on an existing
presenter, **L** new shared presenter (then reused, see section 4). "Keep" means it is already varied.
New presenters: **DS** drop-sort (drag items into 2-4 bins/zones), **DT** drag-to-target (drag one item onto its
matching target, many pairs per round), **HS** hotspot scene (tap things inside one big picture),
**RT** real-time (moving targets, timed), **FL** fill/slider (adjust an amount), **PA** paint (colour regions).

### School (20 games)

| Game | Mechanism today | Could become | Effort |
|---|---|---|---|
| Count | tap each object, then tap numeral tile | keep the counting taps; end with "give me N" (drag N objects into a basket) | M |
| Number Hunt | tap numeral tile | HS: tap the number among many drifting numbers; or DT numeral onto matching group | M |
| Letter Hunt | tap letter tile | HS: tap every A in a garden (the original brief said "find every A") | M |
| Addition | tap sum tile | drag objects of both groups into a tray, then drop the numeral tile in the answer slot | M |
| Subtraction | tap difference tile | swipe/drag away the taken objects, then drop the numeral | M |
| Which Has More | tap the bigger group | drag the star onto the bigger group, or balance-scale with both groups dropped on pans | M |
| One More / One Less | drag a duck in/out of the pond | Keep | - |
| Number Ordering | tap tiles in order | drag number cars into a train in order (DragItem slots) | M |
| Missing Number | tap answer tile | drag the tile into the gap in the equation | S |
| Number Line | tap answer tile | drag the hopper along the line to the landing dot | M |
| Multiplication | tap product tile | build the array: drag objects into a Rows x Cols grid | M |
| Uppercase to Lowercase | tap lowercase tile | DT: drag each lowercase to its uppercase (3 pairs per round) | M |
| Beginning Sound | tap picture tile | DS: drag pictures into baskets labelled by letter | L (DS) |
| Rhyming | tap rhyming picture | DT: drag the picture to its rhyme partner (several pairs) | M |
| Word to Image | tap picture tile | DT: drag the word card onto its picture | M |
| Image to Word | tap word tile | DT: drag the picture onto its word card | M |
| Letter to Sound | tap picture tile | DT: drag the letter onto the picture that starts with it | M |
| Missing Letter | tap letter tile | drag the letter tile into the gap | S |
| Build a Word | tap letter tile | drag letters into the empty slots (differs from Scrambled Word because the rest is given) | S |
| Scrambled Word / Sentence Builder | drag pieces to slots | Keep | - |

### Playground (14 games)

| Game | Mechanism today | Could become | Effort |
|---|---|---|---|
| Pattern Completion | tap shape tile | drag the next shape into the blank slot | S |
| Odd One Out | tap the odd item | drag the odd item out into the bin ("throw it out"), or HS | S |
| What's Missing | tap which item is gone | drag the missing item from a tray back into the gap | S |
| Which Doesn't Make Sense | tap the impossible picture | HS: tap what is wrong in one scene, then drag it to where it belongs (cow to the field) | M |
| Item to Shadow | tap matching silhouette | **DT: drag the item onto its shadow (the user's example)**, several items per round | M |
| Finger Maze, Follow Numbers, Follow Letters, Shortest Path, Avoid Obstacles, Collect Everything | drag along a path | Keep | - |
| Rotate the Piece, Jigsaw, Tangram | rotate / drag | Keep | - |

### Store and House

| Game | Mechanism today | Could become | Effort |
|---|---|---|---|
| Shopping (Recognize, Exact Payment, Addition, Change) | tap answer tile | drag coins and notes into the register (exact payment, change) | M |
| Shopping (Compare Prices, Budget) | tap an item button | drag the items you can afford into the basket | M |
| Dress the Character, Dress for the Occasion, Pack a Suitcase | drag to slots | Keep | - |
| House (decorate) | drag furniture | Keep | - |

### Zoo & Farm (11 games, all `MatchScreen`)

| Game | Mechanism today | Could become | Effort |
|---|---|---|---|
| Habitat | tap habitat tile | DT: drag the animal into its habitat in a scene of 3-4 habitats | L (DT) |
| Mother | tap mother tile | DT: drag the baby to its mother | M |
| Food | tap food tile | drag the food to the animal's mouth (animal eats, or turns away) | M |
| Footprint | tap animal tile | DT: drag the animal onto its footprint trail | M |
| Covering | tap covering tile | drag fur/feathers/scales onto the animal outline | M |
| Sound | tap animal tile | HS: tap the animal making the sound in a barnyard where several animals move | L (HS) |
| Domestic vs Wild | tap pen tile | DS: drag animals into two pens | L (DS) |
| Land, Sea or Air | tap zone tile | DS: drag animals into three zones | M |
| Animal Babies | tap baby tile | DT: pair parents and babies | M |
| Animal Classification | tap class tile | DS: drag animals into class bins | M |
| Geography | tap tile for a spoken place | drag the animal or landmark onto the map | M |

### Science Lab (13 games; 12 `MatchScreen`, 1 `SequenceScreen`)

| Game | Mechanism today | Could become | Effort |
|---|---|---|---|
| Sink or Float | tap the bucket (predict) | drag the object into a water tank, it sinks or floats | M |
| Magnet | tap the bucket (predict) | drag the magnet over objects: magnetic ones jump to it (tiny physics) | M |
| Living vs Non-living | tap tile | DS: two bins | M |
| Plant Growth | tap stages in order | drag stage cards into ordered slots | M |
| Human Senses | tap tile | DT: drag eye/ear/nose/hand/tongue onto what it senses | M |
| Healthy vs Unhealthy | tap tile | DS: feed the healthy food to the character, bin the rest | M |
| Weather | tap tile | DT: drag the weather symbol onto the matching sky scene | M |
| Dress for Weather | tap tile | reuse the Dress presenter: drag clothes onto the character | M |
| Cause and Effect | tap tile | DT: drag the cause (ball, match, rain) onto the scene and watch the effect | M |
| Cooking Measures | tap tile | FL: drag/hold to fill the cup up to the line | L (FL) |
| Seasons | tap tile | DS: four season columns | M |
| Day and Night | tap tile | DS: day and night bins, or drag the sun | M |
| Space | tap tile | drag planets onto their orbit marks | M |

### Workshop (10 games; 7 `AssemblyScreen`, 3 `MatchScreen`)

| Game | Mechanism today | Could become | Effort |
|---|---|---|---|
| Build a Car, Rocket, House, Boat, Robot, Bridge Building, Simple Physics | drag parts into slots, then test animation | Keep | - |
| Tool Selection | tap tool tile | DT: drag the tool onto the job | M |
| Balance | tap tile | drag weights onto a scale until level (tiny physics) | M |
| Help the Character | tap tile | drag the needed object to the character in a scenario | M |

### Art Studio (10 games)

| Game | Mechanism today | Could become | Effort |
|---|---|---|---|
| Trace Shapes / Letters / Numbers | drag along a path | Keep | - |
| Color by Number | tap tile | PA: pick a colour, tap regions to paint | L (PA) |
| Color by Instruction | tap tile | PA with a spoken instruction | M |
| Finish the Drawing | tap tile | trace the missing half with the finger | M |
| Draw What You Hear | tap tile | drag stickers/stamps onto a canvas | M |
| Guided Drawing | tap steps in order | trace each step on the canvas | M |
| Drawing Challenges | tap steps in order | stamps plus trace | M |
| Free Drawing | free draw | Keep | - |

### Brain Gym (22 games; 17 `MatchScreen`)

| Game | Mechanism today | Could become | Effort |
|---|---|---|---|
| Classic Memory | flip cards | Keep | - |
| Remember the Sequence, Simon Says | tap pads in recalled order | Keep (study phase already differs); real-time pads later | - |
| What's Disappeared | tap tile | drag the returning item back to its spot | M |
| Remember the Location | tap tile | tap the cell where it was, on a grid | S |
| Same or Different | tap tile | drag a "same"/"different" token, or swipe | M |
| Match Rotation | tap tile | rotate the piece (`RotateDragger`) to match | S |
| Which Is Bigger | tap tile | drag a crown to the bigger one, or order by size | M |
| Complete the Picture | tap tile | drag the piece into the hole | S |
| Find the Differences | tap tile | HS: tap each difference in the picture | L (HS) |
| Spot the Object | tap tile | HS: tap the object in a busy scene | M |
| Follow the Path | tap tile | trace the path with the finger (`PathDragger`) | S |
| What's Behind | tap tile | slide a curtain away to look, then answer | M |
| Perspective | tap tile | rotate the view, then drag to answer | M |
| Copy the Construction | tap tile | drag blocks to copy the model | M |
| Find the Missing Piece | tap tile | drag the piece into the gap | S |
| Sorting | tap tile | DS: drag into bins | M |
| Recycling | tap tile | DS: waste into the right bin | M |
| Match Item to Category | tap tile | DS or DT | M |
| Sort Laundry / Chores | tap tile | DS: baskets or rooms | M |
| Sequence Ordering | tap in order | drag into ordered slots | M |

### Friends' Park (13 games; 10 `MatchScreen`, 3 `SequenceScreen`)

| Game | Mechanism today | Could become | Effort |
|---|---|---|---|
| Emotion Matching | tap tile | DT: connect faces to emotions | M |
| Facial Expression | tap tile | drag eyes/mouth onto a blank face to build the emotion | M |
| What Would You Do | tap tile | Keep as a choice (it is a choice), add an outcome animation | S |
| Empathy | tap tile | drag the comfort item (hug, plaster) to the sad character | M |
| Social Situations | tap tile | Keep as a choice with outcome animation | S |
| Listen and Choose | tap tile | drag the named item to the character | M |
| Listen for Details | tap tile | HS: tap the detail in the scene | M |
| Follow 1 / 2 / 3 Instructions | tap in order | drag the character or items to carry out the instructions in order | M |
| Road Safety | tap tile | drag the character across a crossing with a traffic light (path drag with rules) | L |
| Safety Scenarios | tap tile | Keep as a choice with outcome animation | S |

### Arcade (7 games; 6 `MatchScreen`, 1 `SequenceScreen`)

| Game | Mechanism today | Could become | Effort |
|---|---|---|---|
| Balloon Popping | tap tile | RT: tap balloons that float up, pop the right numbers or colours | L (RT) |
| Whack-a-Mole | tap tile | RT: tap the moles that match the prompt | M |
| Fishing | tap tile | RT: drag the hook to the right fish | M |
| Space Shooter | tap tile | RT: drag the ship, tap to shoot the target | M |
| Fruit Catcher | tap tile | RT: drag the basket under the falling right fruit | M |
| Treasure Hunt | tap tile | HS: dig where the clue says | M |
| Platformer | tap in order | RT: tap to jump along a path | L |

## 4. New shared presenters and what each unlocks

| Presenter | Builds on | Games it unlocks (approx.) |
|---|---|---|
| DT drag-to-target | `DragItem`, `Dress for the Occasion` slot shape | about 30 (Item to Shadow, pair games, label games, tool/cause games) |
| DS drop-sort | `DragItem` with several bins | about 15 (all sorting and classification games) |
| HS hotspot scene | tap targets on one picture | about 8 |
| RT real-time | new update loop, `Hud` unchanged | 7 (Arcade) |
| PA paint | `FreeDrawingScreen` canvas | 2-3 |
| FL fill/slider | new | 1-2 |
| Gap/slot drag on existing screens | `DragItem` | about 12 small changes (S effort rows) |

Two presenters (DT, DS) cover roughly half of the work, so they go first.

## 5. Proposed phasing

1. **Prototype the two core presenters** (DT and DS) on one game each with the existing Rules generators:
   Item to Shadow (the user's example) and Sorting. Judge them on a device before building more.
2. **Small gap/slot changes** on existing screens (Missing Number, Missing Letter, Pattern Completion, What's
   Missing, Complete the Picture, Find the Missing Piece), since they reuse `DragItem` almost as-is.
3. **Roll DT and DS across** Zoo & Farm, Science Lab, Brain Gym, then School.
4. **HS hotspot presenter**, then Odd One Out, Find the Differences, Spot the Object.
5. **Arcade real-time** (RT), last: the biggest new code and the most dependent on gameplay art.
6. Art Studio PA and the remaining one-off mechanics.

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
- **Questions for the user:** (a) prototype order, is Item to Shadow plus Sorting the right pair; (b) are
  choose-a-response games (What Would You Do, Social Situations, Safety Scenarios) allowed to stay taps;
  (c) should Arcade games be real-time, or is a calmer tap/drag version acceptable for this age; (d) any games
  you want left alone.
