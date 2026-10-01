# Brain Gym art prompts for ChatGPT (M4.8, 22 games)

Written 2026-10-01 against `Rules/BrainGym.cs` and `docs/kids-games/answer-variety-plan.md` (Brain Gym section). Every
sprite key comes from the code. Because the variety plan turns many of these tile games into drag / sort / spot games, the
art is planned as **separate cut-outs** (items, bins, blocks) plus the target pictures, so nothing needs regenerating when
the drag presenters land.

Style paragraph (pasted into every prompt below): "Style for everything: soft polished 3D-look children's mobile-game
illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no shadows cast onto neighbouring cells, no
text or letters anywhere, no people. Background: plain solid magenta (#ff00ff) everywhere, including between cells - not a
checkered/transparent placeholder, an actual solid magenta fill." Every prompt also says: "each item drawn alone, centred in
its own cell with generous plain margin, similar visual size and level of detail, nothing touching a cell edge".

Same sheet workflow as every other building (`docs/kids-games/m4-handover.md`): generate, save under
`art/eva/braingym/ai/`, add a `SHEETS` entry to `tools/art-import/cut-sheets.js` (`dir: 'braingym/ai'`, `resDir:
'braingym'`, `size: 512`), cut with `--install`, verify, commit each batch. **Sheets are numbered in the order they should
be generated**: later ones attach earlier ones so the same objects look the same everywhere.

Derived in code (no ChatGPT): `braingym/rotated_<shape>` (Match Rotation = Batch 8's shape turned 90 degrees);
`braingym/incomplete_<id>` and `braingym/piece_<id>` for Complete the Picture (the six pictures from Art Studio's Batch 4,
`art/eva/artstudio/PROMPTS.md`, split down the middle); `braingym/puzzle_<n>` and `braingym/piece2_<n>` (Batch 17's pictures
with a square cut out / the cut-out square); `braingym/found_<id>` = `item_<id>` (Batch 4); `braingym/match_<id>` and
`ref_<id>` = Batch 7's objects; `braingym/build_<tower>` = `model_<tower>`.

## Batch 1: Classic Memory cards (7) — `braingym/memory_<cat|dog|ball|star|sun|tree>`, `braingym/memory_back`

"Draw a sprite sheet of 7 individual playing-card pictures for a children's mobile game, arranged in a grid of 4 columns x 2
rows (the 8th cell blank/plain magenta). [style]. Each card is a rounded-square card, same size and the same cream card
colour, with ONE simple picture in the middle. The cards, in reading order: (1) a cat's face, (2) a dog's face, (3) a ball,
(4) a star, (5) a sun, (6) a tree, (7) the card BACK: the same card shape with a cheerful pattern (soft blue with a few
small stars) and no picture. Animals are cartoon animals, no people. File name: braingym_memory_sheet.png."

## Batch 2: sequence tiles (12) — `braingym/seqtile_<circle|square|triangle|star|heart|diamond>`, `braingym/pad_<red|blue|green|yellow|purple|orange>`

"Draw a sprite sheet of 12 individual game tiles for a children's mobile game, arranged in a grid of 4 columns x 3 rows.
[style]. Items 1-6 (top two rows plus 2) are six shape tiles, each a rounded-square tile with ONE bold shape in a different
bright colour: (1) a circle, (2) a square, (3) a triangle, (4) a star, (5) a heart, (6) a diamond. Items 7-12 are six
colour pads for a Simon-says game, each a round glossy pad that can light up, one solid saturated colour each: (7) red, (8)
blue, (9) green, (10) yellow, (11) purple, (12) orange. File name: braingym_sequence_tiles_sheet.png."

## Batch 3: daily routine tiles (6) — `braingym/routine_<wake|breakfast|school|play|dinner|sleep>`

Objects only, no people.

"Draw a sprite sheet of 6 individual rounded-square picture tiles for a children's mobile game, arranged in a grid of 3
columns x 2 rows. [style]. Each tile shows ONE part of a child's day as objects, no people: (1) wake up: a bed with the
covers thrown back and a sun rising in the window, (2) breakfast: a bowl of cereal and a glass of milk on a table, (3)
school: a school backpack next to a pencil, (4) play: a ball, blocks and a toy car on the floor, (5) dinner: a plate with food
and a spoon and fork, (6) sleep: a bed with a moon and stars in the window. File name: braingym_routine_sheet.png."

## Batch 4: six everyday objects (6) — `braingym/item_<apple|ball|cup|hat|kite|shoe>`

Also installed as `found_<id>` (Spot the Object). Attach this sheet to Batches 5 and 11.

"Draw a sprite sheet of 6 individual everyday-object icons for a children's mobile game, arranged in a grid of 3 columns x 2
rows. [style]. In reading order: (1) a red apple, (2) a striped beach ball, (3) a blue cup, (4) a yellow hat with a brim, (5)
a red diamond-shaped kite with a tail, (6) a green sneaker. File name: braingym_items_sheet.png."

## Batch 5: What's Disappeared shelf scenes (6) — `braingym/scene_<apple|ball|cup|hat|kite|shoe>`

Attach Batch 4. Each scene is the same shelf with FIVE of the six objects on it; the object named is the one MISSING (an
empty space is left where it was).

"Attached is a sheet of 6 everyday objects (apple, ball, cup, hat, kite, shoe). Draw a sprite sheet of 6 individual small
scenes for a children's mobile game, arranged in a grid of 3 columns x 2 rows. [style]. Every scene is the SAME wooden shelf
with six evenly spaced spots, seen from the front, on a soft rounded card. In every scene FIVE of the six attached objects are
on the shelf, using exactly the same pictures as the attached sheet, in their fixed spots (apple, ball, cup, hat, kite, shoe
from left to right); one spot is left EMPTY. The empty spot, per scene in reading order: (1) the apple's spot, (2) the ball's,
(3) the cup's, (4) the hat's, (5) the kite's, (6) the shoe's. File name: braingym_disappeared_sheet.png."

## Batch 6: Remember the Location (10) — `braingym/scene_scene1..6` (targets), `braingym/position_<posa|posb|posc|posd>` (choices)

Four positions on a 2x2 grid: posa top-left, posb top-right, posc bottom-left, posd bottom-right. Scenes 1-6 show an object in
posa, posb, posc, posd, posb, posc respectively (that is the code's mapping).

"Draw a sprite sheet of 10 individual pictures for a children's mobile game, arranged in a grid of 5 columns x 2 rows. [style].
Items 1-6 are six small scenes, each a soft rounded square card divided into a 2x2 grid of four equal pale cells; ONE bright
object sits in ONE cell and the other three cells are empty: (1) a red star in the top-left cell, (2) a blue ball in the top-
right cell, (3) a yellow flower in the bottom-left cell, (4) a green apple in the bottom-right cell, (5) a purple heart in the
top-right cell, (6) an orange sun in the bottom-left cell. Items 7-10 are four position tiles, each the same empty 2x2 grid card
with exactly ONE cell highlighted in bright yellow: (7) top-left, (8) top-right, (9) bottom-left, (10) bottom-right. File name:
braingym_location_sheet.png."

## Batch 7: Same or Different objects (6) — `braingym/ref_<id>` and `braingym/match_<id>` (same pictures), ids star|heart|cloud|leaf|shell|gem

"Draw a sprite sheet of 6 individual objects for a children's mobile game, arranged in a grid of 3 columns x 2 rows. [style].
In reading order: (1) a yellow star, (2) a red heart, (3) a white fluffy cloud, (4) a green leaf, (5) a pink seashell, (6) a blue
gem. File name: braingym_samediff_sheet.png."

## Batch 8: Match Rotation shapes (6) — `braingym/shape_<circle|square|triangle|star|arrow|heart>`

Each shape has an asymmetric marker so a rotation is visible (the rotated copies are made in code).

"Draw a sprite sheet of 6 individual shape tiles for a children's mobile game, arranged in a grid of 3 columns x 2 rows.
[style]. Each is ONE bold coloured shape that clearly shows which way is up: it has a small white dot near its top edge and a
thin darker stripe along its bottom. In reading order: (1) a circle, (2) a square, (3) a triangle, (4) a five-pointed star, (5)
an arrow pointing up, (6) a heart. File name: braingym_shapes_sheet.png."

## Batch 9: Which Is Bigger animals (6) and size icons (2) — `braingym/sizeitem_<elephant|ant|whale|ladybug|giraffe|mouse>`, `braingym/size_<bigger|smaller>`

Animals are drawn alone; their relative size is NOT shown (the game asks whether the animal is big or small). Size icons: bigger
= a large circle next to a small one with an up-arrow over the large one; smaller = the same with the arrow over the small one.

"Draw a sprite sheet of 8 individual icons for a children's mobile game, arranged in a grid of 4 columns x 2 rows. [style].
Items 1-6 are cartoon animals, each alone, drawn at the same size on the cell (the animal's real size is not shown): (1) an
elephant, (2) an ant, (3) a whale, (4) a ladybug, (5) a giraffe, (6) a mouse. Items 7-8 are two size icons: (7) 'bigger': a
large blue circle with a small blue circle beside it and a big green arrow pointing up over the large circle; (8) 'smaller':
the same two circles with the green arrow pointing up over the SMALL circle. File name: braingym_sizes_sheet.png."

## Batch 10: Find the Differences — scene pairs (6) and difference icons (6)

`braingym/scenepair_<scenea..scenef>` (a wide picture: two panels side by side, left original, right the same with ONE thing
changed; the code can cut it into two panels for the future spot-the-difference version) and `braingym/spot_<scenea..scenef>`
(an icon of the thing that differs). Attach nothing.

"Draw a sprite sheet of 6 individual wide pictures for a children's mobile game, arranged in a grid of 2 columns x 3 rows,
evenly spaced with generous margin. [style]. Each picture is TWO panels side by side (equal size, thin brown frame around
each), showing the SAME simple scene twice, identical in every detail EXCEPT one clear difference in the right panel. The 6
pictures and their one difference: (1) a playground with a slide and a swing - the right panel has no flag on top of the slide;
(2) a kitchen table with a bowl of fruit - the right panel has no apple in the bowl; (3) a garden - the right panel is missing
one of the three flowers; (4) a bedroom with a bed - the right panel has no teddy bear on the bed; (5) a sky over a hill - the
right panel has no cloud; (6) a pond - the right panel has no duck. File name: braingym_differences_sheet.png."

"Draw a sprite sheet of 6 individual icons for a children's mobile game, arranged in a grid of 3 columns x 2 rows. [style]. In
reading order: (1) a small flag, (2) a red apple, (3) a yellow flower, (4) a teddy bear, (5) a white cloud, (6) a yellow duck.
File name: braingym_differences_icons_sheet.png."

## Batch 11: Spot the Object busy scenes (6) — `braingym/hidden_<apple|ball|cup|hat|kite|shoe>`

Attach Batch 4. Each scene is a busy, cluttered picture with the named object hidden among many other things (but still
findable). Choices are Batch 4's icons (`found_<id>`).

"Attached is a sheet of 6 everyday objects (apple, ball, cup, hat, kite, shoe). Draw a sprite sheet of 6 busy scenes for a
children's mobile game, arranged in a grid of 2 columns x 3 rows, each scene a soft rounded rectangle. [style]. Every scene is
a cheerful cluttered picture (a toy-filled playroom, a garden, a market stall, a beach, a bedroom, a park) with lots of
different small objects, and it contains exactly ONE of the attached objects, drawn exactly like the attached one, easy to
find once you look but not in the middle: (1) the apple, (2) the ball, (3) the cup, (4) the hat, (5) the kite, (6) the shoe
- and none of the OTHER five attached objects appear in that scene. File name: braingym_hidden_sheet.png."

## Batch 12: Follow the Path pictures (6) — `braingym/path_<path1..path6>`

Four destination icons sit along the top (house, tree, star, flag, left to right, the same in every picture); a winding path
starts at a small red ball at the bottom and leads to EXACTLY ONE of them, passing some forks to dead ends. Destination per
picture (the code's mapping): path1 house, path2 tree, path3 star, path4 flag, path5 house, path6 tree. (The icons also exist
as choices, see Batch 13.)

"Draw a sprite sheet of 6 individual maze-like pictures for a children's mobile game, arranged in a grid of 3 columns x 2
rows, each on a soft rounded card. [style]. Every picture: at the bottom centre a small red ball (the start); along the top
edge four small destination pictures from left to right: a house, a tree, a star, a flag; between them a winding sandy-colour
path with a few forks that end at nothing, but exactly ONE route connects the ball to ONE destination. The routes, in reading
order: (1) leads to the house, (2) to the tree, (3) to the star, (4) to the flag, (5) to the house by a different route than
(1), (6) to the tree by a different route than (2). File name: braingym_paths_sheet.png."

## Batch 13: small icon sets (12) — destinations, interest categories, rooms

`braingym/destination_<house|tree|star|flag>`, `braingym/categorylabel_<music|sports|reading|art>`,
`braingym/room_<hamper|kitchen|bedroom|bathroom>`.

"Draw a sprite sheet of 12 individual icons for a children's mobile game, arranged in a grid of 4 columns x 3 rows. [style].
Row 1, four destination pictures: (1) a small house, (2) a round tree, (3) a gold star, (4) a red flag on a pole. Row 2, four
category symbols: (5) music: a big musical note, (6) sports: a gold trophy cup, (7) reading: an open book, (8) art: a paint
palette with a brush. Row 3, four home places: (9) a laundry hamper with a shirt sticking out, (10) a kitchen: a sink with a
tap and a plate, (11) a bedroom: a bed with a pillow, (12) a bathroom: a bathtub with bubbles. File name:
braingym_icons_sheet.png."

## Batch 14: What's Behind (12) — `braingym/infront_<cat|dog|ball|box|tree|car>` and `braingym/behind_<same ids>`

Top: a hiding screen with a clue peeking out; bottom: the hidden item shown fully (the reveal).

"Draw a sprite sheet of 12 individual pictures for a children's mobile game, arranged in a grid of 6 columns x 2 rows. [style].
Top row: six pictures of a closed red theatre curtain standing on a small stage, each with a small CLUE of what is hidden
behind it peeking out: (1) cat ears above the curtain and a tail at the side, (2) a dog's nose and a wagging tail at the side,
(3) the top of a ball above the curtain, (4) the corner of a cardboard box sticking out at the side, (5) green tree leaves above
the curtain, (6) the wheels of a car under the curtain. Bottom row: the same six hidden things shown fully, each alone: (7) a
sitting cat, (8) a sitting dog, (9) a ball, (10) a cardboard box, (11) a tree, (12) a toy car. File name:
braingym_behind_sheet.png."

## Batch 15: Perspective (12) — `braingym/viewa_<cube|cup|chair|house|car|ball>`, `braingym/viewb_<same ids>`

"Draw a sprite sheet of 12 individual object pictures for a children's mobile game, arranged in a grid of 6 columns x 2 rows.
[style]. The SAME six objects, each shown twice from different viewpoints. Top row, viewpoint A (seen from the front, at eye
level): (1) a cube with different coloured faces, (2) a cup with a handle, (3) a chair, (4) a house, (5) a car, (6) a ball with
stripes. Bottom row, viewpoint B (the same objects seen from ABOVE, looking down): (7) the cube from the top, (8) the cup from
the top (a circle with the handle at the side), (9) the chair from the top, (10) the house from the top (the roof), (11) the car
from the top, (12) the striped ball from the top. File name: braingym_perspective_sheet.png."

## Batch 16: Copy the Construction — blocks (6) and towers (6)

`braingym/block_<redcube|bluecube|yellowbar|greenbar|orangetriangle|purplearch>` (loose pieces for the drag version) and
`braingym/model_<towera..towerf>` (towers built ONLY from these pieces).

"Draw a sprite sheet of 12 individual pictures for a children's mobile game, arranged in a grid of 4 columns x 3 rows. [style].
Row 1 and the first two cells of row 2 are 6 loose wooden toy blocks, each drawn alone from the front: (1) a red cube, (2) a blue
cube, (3) a yellow long bar, (4) a green long bar, (5) an orange triangle, (6) a purple arch. The remaining six cells are six
different towers built ONLY from copies of those blocks, seen from the front, each 3-5 blocks high, all clearly different:
(7) tower A: red cube, blue cube on top, orange triangle on top; (8) tower B: yellow bar, red cube, green bar; (9) tower C: blue
cube, blue cube, purple arch; (10) tower D: green bar, yellow bar, red cube, orange triangle; (11) tower E: red cube, purple arch,
blue cube; (12) tower F: yellow bar, blue cube, red cube, green bar, orange triangle. File name: braingym_blocks_sheet.png."

## Batch 17: Find the Missing Piece — six complete pictures (6) — `braingym/puzzle_full_<1..6>`

A square is cut out of each in code (hole) and the cut piece becomes the choice. Pictures need clear, distinct detail.

"Draw a sprite sheet of 6 individual square pictures for a children's mobile game, arranged in a grid of 3 columns x 2 rows,
each filling its own soft square card. [style]. Each is a clear, detailed little picture with distinct colour areas: (1) a red
house with a blue door on green grass, (2) a boat with a yellow sail on blue waves, (3) a smiling sun over a hill with a
rainbow, (4) a bunch of three coloured balloons, (5) a fish in a bowl with plants, (6) a butterfly on a flower. File name:
braingym_puzzles_sheet.png."

## Batch 18: Sorting (16) — items `braingym/sortitem_<apple|banana|orange|carrot|tomato|corn|shirt|pants|sock|truck|bus|bike>`, bins `braingym/category_<fruit|vegetable|clothes|vehicle>`

Updated 2026-10-02 for the drop-sort prototype (`Rules/DropSort.cs`): twelve items, three per category, so every bin can take
two or more. Import: `node tools/art-import/cut-sheets.js braingym_sorting --install` (the `SHEETS` entry exists, grid order below).
Attach nothing. Items are separate cut-outs; the bins are open containers.

"Draw a sprite sheet of 16 individual icons for a children's mobile game, arranged in a grid of 4 columns x 4 rows. [style].
Rows 1-3 are twelve everyday things, each alone, in reading order: (1) a red apple, (2) a banana, (3) an orange, (4) an orange
carrot, (5) a red tomato, (6) an ear of yellow corn, (7) a blue t-shirt, (8) a pair of green trousers, (9) a single red sock,
(10) a yellow truck, (11) a green city bus, (12) a blue bicycle. Row 4 is four open sorting boxes of the same shape and size,
each with a clear picture on its front so it needs no label, in this order: (13) a wooden crate with a small apple and a banana
picture (fruit), (14) a wooden crate with a small carrot and a leaf picture (vegetables), (15) a wooden crate with a small shirt
on a hanger picture (clothes), (16) a wooden crate with a small car picture (vehicles). File name: braingym_sorting_sheet.png."

## Batch 19: Recycling (10) — waste `braingym/waste_<bottle|newspaper|jar|bananapeel|can|cardboard>`, bins `braingym/bin_<plastic|paper|glass|organic>`

Plastic/metal bin yellow, paper blue, glass green, organic brown (a standard colour code the picture on the bin also shows).

"Draw a sprite sheet of 10 individual icons for a children's mobile game, arranged in a grid of 5 columns x 2 rows. [style].
Items 1-6 are clean cartoon waste items, each alone: (1) a plastic bottle, (2) a folded newspaper with only grey squiggle
lines instead of text, (3) a glass jar, (4) a banana peel, (5) a metal drink can, (6) a flat piece of cardboard. Items 7-10 are
four recycling bins of the same shape with lids, each a different colour with a simple picture on its front, no text: (7) a
yellow bin with a bottle picture (plastic and metal), (8) a blue bin with a folded paper picture, (9) a green bin with a jar
picture, (10) a brown bin with a leaf picture (organic). File name: braingym_recycling_sheet.png."

## Batch 20: Match Item to Category items (6) — `braingym/catitem_<guitar|ball|book|paintbrush|drum|bat>`

The category symbols (music, sports, reading, art) are in Batch 13.

"Draw a sprite sheet of 6 individual objects for a children's mobile game, arranged in a grid of 3 columns x 2 rows. [style].
In reading order: (1) an acoustic guitar, (2) a football, (3) a picture book, (4) a paintbrush with some paint, (5) a drum with
two drumsticks, (6) a baseball bat. File name: braingym_categoryitems_sheet.png."

## Batch 21: Laundry and chores items (6) — `braingym/choreitem_<shirt|dish|toy|towel|sock|book>`

The destination rooms are in Batch 13.

"Draw a sprite sheet of 6 individual objects for a children's mobile game, arranged in a grid of 3 columns x 2 rows. [style].
In reading order: (1) a dirty t-shirt, (2) a plate with a fork, (3) a toy teddy bear lying down, (4) a folded blue towel, (5) a
single striped sock, (6) a storybook. File name: braingym_chores_sheet.png."

## Checklist

- every object reads instantly and sits at a similar size/detail in its sheet; nothing touches a cell edge
- Batches 5 and 11 use EXACTLY the Batch 4 pictures (attach it); Batch 16's towers use only the six blocks
- Batch 6: exactly one object in one cell per scene; Batch 12: exactly one route reaches one destination
- Batch 8: the marker (dot on top, stripe on the bottom) makes the rotation obvious
- no text or letters anywhere; no people
- if a piece is wrong, ask for a redo of that sheet with the same prompt
