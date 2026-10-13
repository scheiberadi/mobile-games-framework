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

## Batch 7: Workshop rework - 5-7 parts per build game, dotted-slot assembly (6 sheets; Simple Physics later)

Replaces Batches 1-3's 3-part art for six of the build games (Build a Car/Rocket/House/Boat/Robot, Bridge
Building, Simple Physics). New screen: a dotted outline of the finished build in the middle (one dotted slot
per part, generated in code from each part's own alpha), parts in a bottom tray, drag each part to its slot.

One sheet per game, one image each. Each sheet has the FINISHED build on top (flat side or front view, no 3/4
perspective) and the SAME pieces taken apart below, drawn at exactly the same size and angle, so the finished
picture is the reference for where each piece goes (slot offsets are measured against it). Pieces that come
in pairs (wheels, fins, arms, legs, windows, planks) are accepted in either slot of the pair.
Save under `art/eva/workshop/ai/` with the file name given in each prompt.

### 7a. Car (7 pieces)

"Draw ONE image for a children's mobile game showing a toy car in two ways on a plain solid magenta (#ff00ff)
background (an actual solid magenta fill, not a checkered or transparent pattern). TOP HALF: the finished red
toy car, complete, in a flat straight-on SIDE VIEW (no 3/4 perspective, no tilt), centred, facing right.
BOTTOM HALF: the same car taken apart into 7 separate pieces laid out in two rows (4 on top, 3 below) with
generous plain margin around each so they can be cut apart. CRITICAL: every piece in the bottom half must be
drawn at exactly the same size, shape, colours and viewing angle as it appears in the finished car above, so
that putting each piece back in its place rebuilds the finished picture exactly. Draw every piece complete,
including any part that would be hidden behind a neighbour. The 7 pieces: 1. the car body (red, with the
roof, an EMPTY window opening and two empty round wheel arches, no door, no lights, no wheels), 2. the back
wheel, 3. the front wheel (identical to the back wheel), 4. the window glass (light blue with a white shine,
shaped to fill the body's window opening), 5. the door (a red door panel with a small yellow handle, sits
on the body's side), 6. the headlight (a round yellow lamp, sits on the front), 7. the tail pipe (a small grey
exhaust pipe, sits at the back low). Style: soft polished 3D-look children's illustration, warm rounded
chunky toy shapes, thin brown outlines, gentle even lighting, no shadows cast onto neighbouring pieces, no
text, letters or numbers anywhere, no people or animals. File name: workshop_build_car_sheet.png."

### 7b. Rocket (6 pieces)

"Draw ONE image for a children's mobile game showing a toy rocket in two ways on a plain solid magenta
(#ff00ff) background (an actual solid magenta fill, not a checkered or transparent pattern). TOP HALF: the
finished toy rocket, complete, standing upright pointing up, in a flat straight-on side view (no 3/4
perspective), centred. BOTTOM HALF: the same rocket taken apart into 6 separate pieces laid out in two rows
(3 and 3) with generous plain margin around each so they can be cut apart. CRITICAL: every piece in the
bottom half must be drawn at exactly the same size, shape, colours and viewing angle as it appears in the
finished rocket above, so that putting each piece back in its place rebuilds the finished picture exactly.
Draw every piece complete, including any part that would be hidden behind a neighbour. The 6 pieces: 1. the
rocket body (a white and light grey tube with a red stripe, flat top and flat bottom, an EMPTY round window
area is fine to be plain body), 2. the nose cone (a red cone that sits on top of the body), 3. the round
window (a blue porthole with a silver ring, sits on the body), 4. the left fin (red, sits at the lower left of
the body), 5. the right fin (red, a mirror image of the left fin, sits at the lower right), 6. the flame (an
orange and yellow flame that sits under the body, pointing down). Style: soft polished 3D-look children's
illustration, warm rounded chunky toy shapes, thin brown outlines, gentle even lighting, no shadows cast onto
neighbouring pieces, no text, letters or numbers anywhere, no people or animals. File name:
workshop_build_rocket_sheet.png."

### 7c. House (7 pieces)

"Draw ONE image for a children's mobile game showing a toy house in two ways on a plain solid magenta
(#ff00ff) background (an actual solid magenta fill, not a checkered or transparent pattern). TOP HALF: the
finished little house, complete, in a flat straight-on FRONT view (no 3/4 perspective), centred. BOTTOM HALF:
the same house taken apart into 7 separate pieces laid out in two rows (4 on top, 3 below) with generous
plain margin around each so they can be cut apart. CRITICAL: every piece in the bottom half must be drawn at
exactly the same size, shape, colours and viewing angle as it appears in the finished house above, so that
putting each piece back in its place rebuilds the finished picture exactly. Draw every piece complete,
including any part that would be hidden behind a neighbour. The 7 pieces: 1. the walls (a plain cream
rectangle wall with no door and no windows), 2. the roof (a red tiled triangular roof that overhangs the
walls a little), 3. the door (a brown wooden door with a round golden knob), 4. the left window (blue glass
in a yellow frame with a cross bar), 5. the right window (identical to the left window), 6. the chimney (a
small red brick chimney that sits on the roof), 7. the steps (two grey stone steps that sit at the foot of
the door). Style: soft polished 3D-look children's illustration, warm rounded chunky toy shapes, thin brown
outlines, gentle even lighting, no shadows cast onto neighbouring pieces, no text, letters or numbers
anywhere, no people or animals. File name: workshop_build_house_sheet.png."

### 7d. Boat (6 pieces)

"Draw ONE image for a children's mobile game showing a toy sailing boat in two ways on a plain solid magenta
(#ff00ff) background (an actual solid magenta fill, not a checkered or transparent pattern). TOP HALF: the
finished toy sailing boat, complete, in a flat straight-on SIDE view (no 3/4 perspective), centred, no water.
BOTTOM HALF: the same boat taken apart into 6 separate pieces laid out in two rows (3 and 3) with generous
plain margin around each so they can be cut apart. CRITICAL: every piece in the bottom half must be drawn at
exactly the same size, shape, colours and viewing angle as it appears in the finished boat above, so that
putting each piece back in its place rebuilds the finished picture exactly. Draw every piece complete,
including any part that would be hidden behind a neighbour. The 6 pieces: 1. the hull (a red and white boat
body with a curved bottom), 2. the mast (a tall wooden pole), 3. the big sail (a large white triangle that
hangs on the mast), 4. the small sail (a smaller yellow triangle in front of the mast), 5. the flag (a small
red pennant that sits at the top of the mast), 6. the life ring (a red and white ring that hangs on the side
of the hull). Style: soft polished 3D-look children's illustration, warm rounded chunky toy shapes, thin brown
outlines, gentle even lighting, no shadows cast onto neighbouring pieces, no text, letters or numbers
anywhere, no people or animals. File name: workshop_build_boat_sheet.png."

### 7e. Robot (7 pieces)

"Draw ONE image for a children's mobile game showing a friendly toy robot in two ways on a plain solid
magenta (#ff00ff) background (an actual solid magenta fill, not a checkered or transparent pattern). TOP HALF:
the finished robot, complete, standing, in a flat straight-on FRONT view, centred. BOTTOM HALF: the same robot
taken apart into 7 separate pieces laid out in two rows (4 on top, 3 below) with generous plain margin around
each so they can be cut apart. CRITICAL: every piece in the bottom half must be drawn at exactly the same
size, shape, colours and viewing angle as it appears in the finished robot above, so that putting each piece
back in its place rebuilds the finished picture exactly. Draw every piece complete, including any part that
would be hidden behind a neighbour. The 7 pieces: 1. the body (a blue-grey box torso with a round yellow
button and two small lights, no head, no limbs), 2. the head (a square head with two big round friendly eyes
and a smile, no antenna), 3. the left arm (a grey arm with a round hand, hangs at the robot's left side),
4. the right arm (a mirror image of the left arm), 5. the left leg (a grey leg with a flat foot), 6. the right
leg (a mirror image of the left leg), 7. the antenna (a thin stalk with a red ball on top, sits on the head).
Style: soft polished 3D-look children's illustration, warm rounded chunky toy shapes, thin brown outlines,
gentle even lighting, no shadows cast onto neighbouring pieces, no text, letters or numbers anywhere, no
people or animals. File name: workshop_build_robot_sheet.png."

### 7f. Bridge (7 pieces, v5: raised view, railing on both sides)

v1 flat deck on pillars: read as a bench. v2 stone arch: read as a tunnel. v3/v4 river + banks + pier + deck + railing worked as a bridge, but in a flat side view the near and far railings coincide, so it looked like a railing on one side only. v5 uses a slightly raised side view (deck top visible, near and far rails), each railing piece holds both its near and far rail.

"Draw ONE image for a children's mobile game showing a toy wooden bridge over a river in two ways on a plain solid magenta (#ff00ff) background (an actual solid magenta fill, not a checkered or transparent pattern). VIEW: for the whole image use the same slightly raised side view, as if looking at the bridge from the side and a little from above (about 20 degrees down), the bridge running left to right across the picture. From this view the top of the bridge deck is visible as a wide wooden strip, and there is a railing along BOTH edges of the deck: a near railing in front (lower on screen) and a far railing behind (higher on screen). TOP HALF: the finished scene, complete, centred, nothing behind it. A blue river runs across the whole width with small white wave marks, with a grassy bank on the left and a grassy bank on the right (green grass on top, brown earth underneath, both banks the same height, each bank taller than the water). Standing in the middle of the river is one grey stone pier (a stone block pillar rising out of the water up to the height of the banks). A long wooden bridge deck made of exactly 7 planks rests on top of the left bank, the stone pier and the right bank, crossing the river at the same height as the bank tops. On top of the deck stand two short, chunky wooden railing sections: one over the left half of the deck and one over the right half, with a clear gap between them in the middle. Each railing section includes BOTH its near rail and its far rail: three thick round posts and two thick rounded rails on the near edge of the deck, and the same on the far edge of the deck, so the bridge has a railing on both sides. Everything is bold and sturdy toy-like, clearly thick enough to be easy to see on a small phone screen. BOTTOM HALF: the same scene taken apart into 7 separate pieces laid out in two rows (4 on top, 3 below) with generous plain margin around each so they can be cut apart. CRITICAL: every piece in the bottom half must be drawn at exactly the same size (do NOT shrink or enlarge any piece), the same shape, the same colours and the same viewing angle as it appears in the finished scene above, so that putting each piece back in its place rebuilds the finished picture exactly. Draw every piece complete, including any part that would be hidden behind a neighbour. The bridge deck piece must show exactly the same 7 planks and the same length as in the finished scene. The 7 pieces: 1. the left bank (grass on top, brown earth below, flat bottom), 2. the right bank (a mirror image of the left bank), 3. the river (one wide blue water strip with white wave marks, flat top and flat bottom, spanning the gap between the two banks), 4. the stone pier (a grey stone block pillar, flat bottom and flat top), 5. the bridge deck (one long wooden strip of 7 planks, same as in the finished scene), 6. the left railing section (the near rail and the far rail together as one piece, chunky wooden, with their posts), 7. the right railing section (identical to the left railing section). Style: soft polished 3D-look children's illustration, warm rounded chunky toy shapes, thin brown outlines, gentle even lighting, no shadows cast onto neighbouring pieces, no text, letters or numbers anywhere, no people or animals. File name: workshop_build_bridge_sheet.png."
