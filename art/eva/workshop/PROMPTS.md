# Workshop art prompts for ChatGPT (M4.6, one of the 5 buildings with no gameplay art yet)

Read `Rules/Workshop.cs` first if anything here looks arbitrary - every sprite key below comes directly
from its code, not invented. Seven games (the five Build-a-X games, Bridge Building, Simple Physics) share
one assembly presenter: drag 3 parts onto their own slots from a shelf (some shelf items are distractors -
other builds' own parts, reused, no extra art needed for those), then a **test sprite** shows the finished
build performing its test action. The other three (Tool Selection, Help the Character, Balance) reduce to
the shared `MatchScreen` presenter, same target+choices shape as `art/eva/arcade/PROMPTS.md`'s games.

Same sheet workflow as every other building (`docs/kids-games/m4-handover.md`): generate, paste back, save
under `art/eva/workshop/ai/`, add a `SHEETS` entry to `tools/art-import/cut-sheets.js` (`dir:
'workshop/ai'`, `resDir: 'workshop'`, `grid` per each batch below, `size: 512`), cut with `--install`,
verify, commit each batch separately.

Style for everything: soft polished 3D-look children's mobile-game illustration, warm rounded shapes, thin
brown outlines, gentle even lighting, no shadows cast onto neighbouring cells, no text or letters anywhere,
no people (a hand alone is fine where a scenario needs one, per the established convention). Background:
plain solid magenta (#ff00ff) everywhere, including between cells - not a checkered/transparent placeholder,
an actual solid magenta fill.

## Batch 1: Assembly parts, group A (12) — `workshop/part_<id>` — Car, Rocket, House, Boat

"Draw a sprite sheet of 12 individual toy-like build parts for a children's mobile game, arranged in a grid
of 4 columns x 3 rows, evenly spaced with generous plain margin around each one so they can be cut apart
afterwards, all at a similar visual size and level of detail - simple, chunky, toy-construction-kit pieces,
not realistic mechanical parts. [style paragraph above]. The 12 parts, in reading order:

1. car_body: a simple rounded toy car body/chassis, no wheels or windows attached, bright cheerful colour.
2. car_wheels: a matched pair of car wheels side by side, as one piece.
3. car_windows: a car's window/cabin section alone (a curved glass canopy shape).
4. rocket_body: a rocket's main tube-shaped body section, no fins or nose cone.
5. rocket_fins: a matched set of rocket fins, as one piece.
6. rocket_nosecone: a rocket's pointed nose cone alone.
7. house_walls: a simple box-shaped wall section alone, no roof or door.
8. house_roof: a triangular peaked roof piece alone.
9. house_door: a small house door alone, rounded and friendly.
10. boat_hull: a simple curved boat hull alone, no sail or mast.
11. boat_sail: a triangular sail alone.
12. boat_mast: a boat mast pole alone.

File name: workshop_parts_a_sheet.png."

## Batch 2: Assembly parts, group B (9) — `workshop/part_<id>` — Robot, Bridge, Simple Physics

"Draw a sprite sheet of 9 individual toy-like build parts for a children's mobile game, arranged in a grid
of 3 columns x 3 rows, evenly spaced with generous plain margin around each one so they can be cut apart
afterwards, all at a similar visual size and level of detail - simple, chunky, toy-construction-kit pieces.
[style paragraph above]. The 9 parts, in reading order:

1. robot_body: a simple boxy robot torso alone, no arms or head.
2. robot_arms: a matched pair of simple robot arms, as one piece.
3. robot_head: a friendly boxy robot head alone, simple face details painted on.
4. bridge_block_a: one simple rectangular building block, a distinct bright colour.
5. bridge_block_b: a second building block, a different bright colour, same construction-kit style.
6. bridge_block_c: a third building block, a third bright colour, same style, all three clearly a matched
   set despite the different colours.
7. physics_ramp: a simple inclined ramp piece alone (a wedge/triangle block).
8. physics_block: a simple plain box/block piece alone.
9. physics_balltrack: a short curved track/channel piece alone, with one small ball sitting in it.

File name: workshop_parts_b_sheet.png."

## Batch 3: Test sprites (7) — `workshop/test_<kind>`

The finished, fully-assembled build performing its own test action - shown once every slot is filled.

"Draw a sprite sheet of 7 individual small scene icons for a children's mobile game, arranged in a grid of 4
columns x 2 rows (the 8th cell left blank/plain magenta), evenly spaced with generous plain margin around
each one so they can be cut apart afterwards, all at a similar visual size and level of detail. [style
paragraph above]. The 7 scenes, in reading order:

1. test_car: the completed toy car (body+wheels+windows together, matching Batch 1's parts) shown driving,
   with a couple of small motion lines behind it.
2. test_rocket: the completed toy rocket (body+fins+nosecone together) launching upward, with a small
   flame/smoke puff beneath it.
3. test_house: the completed toy house (walls+roof+door together) standing finished, a small friendly
   detail like a wisp of chimney smoke optional.
4. test_boat: the completed toy boat (hull+sail+mast together) floating on a few simple wave lines.
5. test_robot: the completed toy robot (body+arms+head together) with one arm raised in a friendly wave
   pose.
6. test_bridge: a small bridge built from the three coloured blocks (Batch 2, items 4-6), spanning a gap,
   with a small toy car crossing over it.
7. test_simplephysics: a small ball rolled down the ramp piece (Batch 2, item 7) and reaching a simple
   target/goal marker.

File name: workshop_tests_sheet.png."

## Batch 4: Tool Selection (12) — `workshop/problem_<id>` (targets, 1-6) + `workshop/tool_<id>` (choices, 7-12)

"Draw a sprite sheet of 12 individual icons for a children's mobile game, arranged in a grid of 4 columns x
3 rows, evenly spaced with generous plain margin around each one so they can be cut apart afterwards, all at
a similar visual size and level of detail. [style paragraph above]. Items 1-6 (top two rows) are six small
problem scenes (a hand may appear where natural, no full people): (1) a whole apple next to a cutting board,
needing to be cut, (2) a nail sticking half-way out of a wood plank, needing to be hammered in, (3) a loose
screw part-way into a board, needing tightening, (4) a sheet of paper, needing to be cut, (5) a wooden
plank, needing to be cut, (6) a nut/bolt that's loose, needing tightening. Items 7-12 (bottom row plus one
more) are six simple tool icons, one per problem's solution, in this order: a kitchen knife, a hammer, a
screwdriver, scissors, a hand saw, a wrench.

File name: workshop_toolselection_sheet.png."

## Batch 5: Help the Character (12) — `workshop/scenario_<id>` (targets, 1-6) + `workshop/action_<id>` (choices, 7-12)

"Draw a sprite sheet of 12 individual icons for a children's mobile game, arranged in a grid of 4 columns x
3 rows, evenly spaced with generous plain margin around each one so they can be cut apart afterwards, all at
a similar visual size and level of detail. [style paragraph above]. Items 1-6 (top two rows) are six small,
gentle scenario scenes (animals only, no people, handled kindly - never scary or sad-looking, just a simple
need): (1) a dog sitting by an empty food bowl looking hopeful, (2) a small wilting potted plant, (3) a
bird looking a little cold/fluffed up, (4) a small kitten looking around as if lost, (5) a room with a few
toys scattered on the floor, (6) a bicycle with one flat tyre. Items 7-12 (bottom row plus one more) are six
simple solution icons, one per scenario, in this order: a bone, a watering can, a small cosy nest, a leash
leading toward a small house shape, a broom, a bicycle pump.

File name: workshop_helpthecharacter_sheet.png."

## Batch 6: Balance (10) — `workshop/scale_<id>` (targets, 1-6, plus the level scale) + `workshop/weight_<id>` (choices)

Six tipped scale scenes (slight/medium/strong on each side, five distinct tilt angles), plus (added 2026-10-01 for the
answer-variety plan's drag-the-weights-until-level version, see `docs/kids-games/answer-variety-plan.md`) one **level
scale** (`scale_level`, not used by code yet), and only 3 DISTINCT weight icons (small/medium/large) - the code reuses
the same weight icon for e.g. both "light_left" and "light_right".

"Draw a sprite sheet of 10 individual icons for a children's mobile game, arranged in a grid of 4 columns x 3 rows (the
last 2 cells left blank/plain magenta), evenly spaced with generous plain margin around each one so they can be cut apart
afterwards, all at a similar visual size and level of detail. [style paragraph above]. Items 1-6 are six simple
balance-scale scenes, the same two-pan scale drawn each time, tipped by a different amount/direction (no weights on it
yet): (1) tipped slightly to the left, (2) tipped strongly to the left, (3) tipped slightly to the right, (4) tipped
strongly to the right, (5) tipped a medium amount to the left, (6) tipped a medium amount to the right - five distinct
tilt angles total, slight/medium/strong clearly different from each other, strong being the most tilted. Item 7 is the
same scale perfectly level and balanced (both pans at the same height). Items 8-10 are three simple weight icons, each
drawn alone as a separate object, of clearly increasing size: a small weight, a medium weight, a large weight - a
consistent shape/style across all three, only the size changing.

File name: workshop_balance_sheet.png."

## Checklist

- every icon/scene reads instantly as what it's meant to be, similar visual weight across all 6 sheets
- Batch 1/2's parts look like they belong to the SAME toy-construction-kit family, even across different
  builds (car parts and rocket parts shouldn't look like two different art styles)
- Batch 3's test scenes use the SAME build parts drawn in Batches 1-2 assembled together, not new designs (attach both part sheets to Batch 3's prompt)
- bridge_block_a/b/c (Batch 2) read as a matched set of building blocks despite different colours
- Balance's six scale tilts (Batch 6) are visually distinguishable by tilt AMOUNT - slight vs medium vs
  strong must actually look different, not just left vs right
- nothing touching a cell edge or bleeding into a neighbour
- if a piece is wrong, ask ChatGPT for a redo with the same prompt
