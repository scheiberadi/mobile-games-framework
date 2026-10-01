# Art Studio art prompts for ChatGPT (M4.7)

Written 2026-10-01 against `Rules/ArtStudio.cs` and `docs/kids-games/answer-variety-plan.md` (Art Studio section), so
nothing here needs regenerating when the drag/paint presenters land. Every sprite key below comes from the code, not
from guesswork.

What the code needs (sprite keys `artstudio/<name>`):

- Free Drawing: `swatch_<red|orange|yellow|green|blue|purple>` and `stamp_<circle|star|heart|sun|tree|flower>`.
- Color by Number / Color by Instruction (MATCH today, paint later): choices `swatch_<color>` (same swatches, plus
  `swatch_brown`), targets `numbered_region_<part>` (digit on the part) and `plain_region_<part>`, parts =
  roof, wall, door, window, sun, tree. Colour per part: roof red, wall yellow, door brown, window blue, sun orange,
  tree green.
- Finish the Drawing: target `half_<id>`, choice `piece_<id>` for sun, flower, house, tree, car, balloon. **Not
  generated as halves:** ChatGPT draws six COMPLETE pictures and `tools/art-import` splits each down the middle in
  code, so the half and the missing piece always line up exactly (and Brain Gym's Complete the Picture, which uses the
  same six items, reuses them). Sprite keys of the complete pictures: `artstudio/full_<id>`.
- Draw What You Hear: choices `scene_scene1` .. `scene_scene6` (audio-led, no target picture). The scenes are listed
  below; the spoken lines must describe exactly these.
- Guided Drawing: `step_<roof|walls|door|windows>`; Drawing Challenges: `challenge_step_<body|head|arms|legs>`
  (progressive drawing stages, tapped in order, same idea as Science Lab's Plant Growth stages).
- Trace games: paths are drawn in code. `pencil_tip` and `trace_path` are generated in code too (no ChatGPT needed).

Same sheet workflow as every other building (`docs/kids-games/m4-handover.md`): generate, save under
`art/eva/artstudio/ai/`, add a `SHEETS` entry to `tools/art-import/cut-sheets.js` (`dir: 'artstudio/ai'`, `resDir:
'artstudio'`, `size: 512`), cut with `--install`, verify, commit each batch.

Style paragraph (pasted into every prompt below): "Style for everything: soft polished 3D-look children's mobile-game
illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no shadows cast onto neighbouring cells, no
text or letters anywhere, no people. Background: plain solid magenta (#ff00ff) everywhere, including between cells -
not a checkered/transparent placeholder, an actual solid magenta fill."

## Batch 1: paint swatches (7) and stamps (6) — `artstudio/swatch_<color>`, `artstudio/stamp_<id>`

"Draw a sprite sheet of 13 individual icons for a children's mobile game, arranged in a grid of 5 columns x 3 rows (the
last 2 cells left blank/plain magenta), evenly spaced with generous plain margin around each one so they can be cut apart
afterwards, all at a similar visual size and level of detail. [style paragraph]. Items 1-7 are paint swatches, each one a
single round glossy dab/blob of thick paint with a tiny brush stroke through it, in a clearly saturated, clearly
different colour: (1) red, (2) orange, (3) yellow, (4) green, (5) blue, (6) purple, (7) brown. Items 8-13 are chunky
kid-sticker stamp shapes, each one a single bold colourful shape: (8) a circle, (9) a five-pointed star, (10) a heart,
(11) a simple sun (a circle with short rays, no face), (12) a simple round tree with a brown trunk, (13) a simple flower with
five petals. File name: artstudio_swatches_stamps_sheet.png."

## Batch 2: scene parts to colour, plain and numbered (12) — `artstudio/plain_region_<part>`, `artstudio/numbered_region_<part>`

Parts: roof, wall, door, window, sun, tree. Plain = the part as an uncoloured outline shape; numbered = the SAME shape with
a big single digit on it (digits are allowed, letters are not): roof 1, wall 2, door 3, window 4, sun 5, tree 6.

"Draw a sprite sheet of 12 individual colouring-page shapes for a children's mobile game, arranged in a grid of 4 columns
x 3 rows, evenly spaced with generous plain margin around each one so they can be cut apart afterwards, all at a similar
visual size. Every shape is UNCOLOURED: a clean thick brown outline with a plain very pale cream/white fill, like a blank
colouring-book area, so a colour can be dropped onto it later. [style paragraph, except: the shapes are line-art style with
a pale fill, not shaded]. Items 1-6 (top two rows) are the six parts WITHOUT any number: (1) a triangular house roof, (2) a
square house wall, (3) a rounded house door, (4) a square window with a cross bar, (5) a sun (circle with short rays), (6) a
round-topped tree with a trunk. Items 7-12 (third row and the remaining cells) are the SAME six shapes again, drawn exactly
the same, but each with ONE big, clear, friendly number in the middle, in this order: roof 1, wall 2, door 3, window 4, sun
5, tree 6 (digits only, no other text). File name: artstudio_regions_sheet.png."

## Batch 3: the coloured house scene, blank and finished (2) — `artstudio/coloringpage_blank`, `artstudio/coloringpage_done`

Prepared for the Paint presenter in the answer-variety plan (Color by Number as "pick a colour, tap the regions"). One
scene with exactly the six parts above; blank = outline with the six numbers, done = the same scene fully coloured
(roof red, wall yellow, door brown, window blue, sun orange, tree green).

"Draw a sprite sheet of 2 individual images for a children's mobile game, side by side in 2 columns x 1 row with generous
plain margin. [style paragraph, except the left image is line-art]. Both images show the SAME simple children's picture:
a small house with a triangular roof, a square wall, a rounded door and one square window, a sun in the upper corner and a
round tree beside the house. Image 1: a blank colouring page - thick clean brown outlines, every region left plain pale
cream/white, with a big single digit in each region: roof 1, wall 2, door 3, window 4, sun 5, tree 6 (digits only). Image
2: exactly the same drawing (same lines, same positions) fully coloured: roof red, wall yellow, door brown, window blue, sun
orange, tree green, with no digits. File name: artstudio_coloringpage_sheet.png."

## Batch 4: six complete pictures for Finish the Drawing / Complete the Picture (6) — `artstudio/full_<id>`

"Draw a sprite sheet of 6 individual simple pictures for a children's mobile game, arranged in a grid of 3 columns x 2
rows, evenly spaced with generous plain margin around each one so they can be cut apart afterwards, all at a similar visual
size and level of detail. IMPORTANT: every picture must be centred and left-right SYMMETRIC enough that cutting it exactly
down the vertical middle gives two halves that each look like a sensible half of the picture (the subject centred on the
cell, filling it, standing on the middle line). [style paragraph]. The 6 pictures, in reading order: (1) a sun (round
face-free sun with short rays), (2) a flower with a stem, two leaves and five petals, (3) a house, front view, with a roof,
door and two windows, (4) a tree with a round crown and a trunk, (5) a toy car seen from the front, (6) a round balloon on a
string. File name: artstudio_full_pictures_sheet.png."

Then import: `full_<id>` is installed as-is, plus code splits each into `half_<id>` (left half kept, right half empty) and
`piece_<id>` (the right half alone, same canvas position) - see the matching `SHEETS` entry once the sheet exists.

## Batch 5: Draw What You Hear scenes (6) — `artstudio/scene_scene1` .. `scene_scene6`

Eva narrates one of these; the child picks the matching picture. Six clearly different, simple scenes (no people), each a
square card with a soft rounded frame:

"Draw a sprite sheet of 6 individual small scene pictures for a children's mobile game, arranged in a grid of 3 columns x 2
rows, evenly spaced with generous plain margin around each one so they can be cut apart afterwards, all at a similar visual
size and level of detail. Each picture is a simple scene on its own soft rounded-square card with a pale sky or ground
background colour (the card is part of the picture; the magenta is only around the cards). [style paragraph]. The 6 scenes, in
reading order: (1) a yellow sun above a red house with a green tree beside it; (2) a boat with a white sail on blue waves
under a sun; (3) a garden with three flowers and a butterfly; (4) a kite flying high above a green hill; (5) a red car on a
road passing a green tree; (6) a big apple tree with red apples and a small bird flying in the sky. File name:
artstudio_scenes_sheet.png."

The voice lines for `scene1..scene6` must say exactly these sentences.

## Batch 6: drawing step stages (8) — `artstudio/step_<roof|walls|door|windows>`, `artstudio/challenge_step_<body|head|arms|legs>`

Progressive drawing stages: each tile shows the drawing with everything up to and including that step, the newest part in a
brighter/bolder colour. Order of steps is the order in the code: house = roof, walls, door, windows; teddy bear = body, head,
arms, legs.

"Draw a sprite sheet of 8 individual drawing-stage pictures for a children's mobile game, arranged in a grid of 4 columns x
2 rows, evenly spaced with generous plain margin around each one so they can be cut apart afterwards, all at a similar visual
size, each picture centred on the same position inside its own cell so the stages line up. [style paragraph]. Top row: a
house being drawn in four stages, the same house each time: (1) only the red roof, (2) the roof plus the yellow walls, (3)
roof, walls and a brown door, (4) the finished house with roof, walls, door and two blue windows. Bottom row: a friendly teddy
bear being drawn in four stages, the same bear each time: (5) only the round body, (6) body plus the head with a small
face, (7) body, head and two arms, (8) the finished bear with body, head, two arms and two legs. In every picture the newest
part added in that step is drawn in a slightly brighter, bolder colour than the parts that were already there. File name:
artstudio_steps_sheet.png."

## Checklist

- every shape/scene reads instantly; similar weight and detail across the batch
- Batch 2: the plain and numbered shapes are identical apart from the digit; digits are legible, no letters anywhere
- Batch 4: each picture is centred and cuts cleanly down the middle (nothing important sits exactly on the cut that would
  look broken as a half)
- Batch 5: six scenes that cannot be confused with one another when only 3-4 are on screen
- Batch 6: the four stages of each drawing are clearly the same drawing, growing; the newest part is visible
- nothing touching a cell edge or bleeding into a neighbour; if a piece is wrong, ask for a redo with the same prompt
