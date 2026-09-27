# M4.1 Playground — object art prompts for ChatGPT

Covers the base "real object" icons needed by two Playground games:

- **Odd One Out** (`Rules/OddOneOut.cs`) — sprite key `oddoneout/<item>`
- **Item to Shadow** (`Rules/ItemToShadow.cs`) — sprite key `itemtoshadow/<key>` (+ a `_silhouette` variant)

Both games draw from overlapping real-world object catalogues, so one rendered icon per object
covers both — just save/copy the same PNG under both sprite-key paths for the 16 objects the two
games share (apple, orange, banana, car, truck, bus, bike).

Style for everything: same soft polished 3D-look children's mobile-game illustration as the house
and map art (`art/eva/house`, `art/eva/map`): warm, rounded, thin brown outlines, no text or
letters. Unlike the map backdrop, animals ARE wanted here — the catalogue needs them. No people.

## The 29 objects

Farm animals: cow, pig, sheep, horse, goat
Wild animals: lion, tiger, bear, elephant, zebra
Vehicles: car, bus, bike, truck, train
Fruits: apple, banana, orange, grape, pear
Round household: ball, balloon
Narrow household: carrot, pencil, candle
Small animals (Item to Shadow only): cat, dog, fox, rabbit

## Attempt 1: one sheet, all 29 objects

"Draw a sprite sheet of 29 individual objects for a children's mobile game, arranged in a clean
grid of 5 columns x 6 rows (one empty cell at the end), evenly spaced with generous plain margin
around each object so they can be cut apart afterwards. Each object is a single item only, centred
in its own cell, all objects drawn at a similar visual size and level of detail. Style: soft
polished 3D-look children's illustration, warm rounded shapes, thin brown outlines, gentle even
lighting, no shadows cast onto neighbouring cells, no text, letters or numbers anywhere, no people.
Background: plain solid magenta (#ff00ff) everywhere, including between cells. The 29 objects, in
reading order (left to right, top to bottom):
1. cow, 2. pig, 3. sheep, 4. horse, 5. goat,
6. lion, 7. tiger, 8. bear, 9. elephant, 10. zebra,
11. car, 12. bus, 13. bike, 14. truck, 15. train,
16. apple, 17. banana, 18. orange, 19. grape, 20. pear,
21. ball, 22. balloon, 23. carrot, 24. pencil, 25. candle,
26. cat, 27. dog, 28. fox, 29. rabbit.
File name: playground_objects_sheet.png."

Then: "If your tool can produce a real transparent background instead of magenta, use that."

Check before slicing: every object readable at a glance, similar size/detail across the sheet,
no object touching its cell's edge or bleeding into a neighbour, no stray text. If it fails on a
handful of objects but most are good, keep the good ones and regenerate only the bad cells as
single images (Attempt 2). If the whole sheet is too cramped/inconsistent to use, fall back to
Attempt 2 for everything.

## Attempt 2 (fallback): one object per image

Only for objects the sheet didn't produce well. One prompt per object, reusing this template —
replace `<OBJECT>`:

"Draw a single <OBJECT>, isolated, for a children's mobile game. Soft polished 3D-look
illustration, warm rounded shapes, thin brown outlines, medium detail, gentle even lighting, no
shadow, no text or letters. Plain solid magenta (#ff00ff) background, small margin around the
object, nothing else in the picture. File name: <object>.png."

## After generating: silhouettes for Item to Shadow

Item to Shadow additionally needs a `_silhouette` version of its 16 objects (apple, orange, ball,
balloon, banana, carrot, pencil, candle, cat, dog, fox, rabbit, car, truck, bus, bike) — a solid
dark shape with the object's exact outline. Don't spend a second ChatGPT generation on these:
derive them from the base PNGs instead (alpha-mask the object, fill solid dark brown/black) with
a small script, e.g. via `sharp` in `tools/art-import/`, so the silhouette always matches its
object exactly. Only re-ask ChatGPT for a silhouette if a specific object's outline turns out too
ambiguous once flattened (e.g. ball vs. balloon looking identical in silhouette).

## Not covered here (separate art types — ask for these next if you want them)

- **Which Doesn't Make Sense** (`whichdoesntmakesense/<scene>`) — 40 small scenario illustrations
  (e.g. "cow in a field", "fish in a tree"), not single objects.
- **Jigsaw** (`jigsaw/piece_<row>_<col>`) — one whole source photo/illustration, sliced into a grid
  after generation (up to 5x5).
- **Tangram** (`tangram/shape_<i>`) — 7 classic tangram-style geometric pieces (triangles, square,
  parallelogram).
- **Rotate the Piece** (`rotatepiece/piece_0`..`piece_5`) — 6 simple shapes with a visible
  "this way up" feature so rotation is obvious.

---

# Pattern Completion / What's Missing — 5 shape tiles

Both games draw from the same 5-symbol pool (`Rules/PatternCompletion.cs`'s `Symbols = {A,B,C,D,E}`,
reused as-is by `Rules/WhatsMissing.cs`) — sprite key `pattern/shape_<letter>` (lowercase). The
letters are just the generator's internal IDs, not glyphs to draw: each maps to a distinct simple
shape icon, no text ever shown to the child. Mapping used below: a=star, b=circle, c=triangle,
d=square, e=heart.

## Attempt 1: one sheet, all 5 shapes

"Draw a sprite sheet of 5 individual toy-like shape icons for a children's mobile game, arranged in
a single row of 5, evenly spaced with generous plain margin around each one so they can be cut
apart afterwards. Each shape is bold, flat-colored and instantly recognisable at a glance (like a
glossy plastic toy token), all drawn at the same size and level of detail. Style: soft polished
3D-look children's illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no
shadows cast onto neighbouring cells, no text, letters or numbers anywhere, no people. Background:
plain solid magenta (#ff00ff) everywhere, including between cells. The 5 shapes, left to right:
1. a yellow five-point star, 2. a red circle, 3. a green triangle, 4. a blue square, 5. a pink
heart. File name: pattern_shapes_sheet.png."

Then: "If your tool can produce a real transparent background instead of magenta, use that."

Check before slicing: all 5 shapes clearly different from each other by color AND outline (not
just color, so it still works if a child is colorblind), similar size, nothing touching a cell
edge or bleeding into a neighbour.

## Attempt 2 (fallback): one shape per image

Only for shapes the sheet didn't produce well. One prompt per shape, reusing this template —
replace `<SHAPE>` and `<COLOR>`:

"Draw a single <COLOR> <SHAPE>, isolated, like a glossy plastic toy token for a children's mobile
game. Soft polished 3D-look illustration, thin brown outline, medium detail, gentle even lighting,
no shadow, no text or letters. Plain solid magenta (#ff00ff) background, small margin around the
shape, nothing else in the picture. File name: shape_<letter>.png."

---

# Jigsaw — one source picture

`Rules/Jigsaw.cs` slices ONE whole picture into a grid (up to 5x5 = 25 pieces at the top level),
sprite key `jigsaw/piece_<row>_<col>`. Needs a square picture with clearly different regions across
the whole frame — if it's too uniform (e.g. plain sky everywhere), a 5x5 piece is impossible for a
toddler to place by look alone.

"Draw a single square picture for a children's jigsaw puzzle: a cheerful outdoor scene with a big
smiling sun in one corner, fluffy white clouds, a colorful rainbow arching across the sky, green
rolling hills below, and a scatter of bright flowers (red, yellow, purple) across the grass -
distinct, differently-colored regions spread across the whole frame so any small square cut from it
still looks unique. Soft polished 3D-look children's illustration, warm rounded shapes, thin brown
outlines, gentle even lighting, no shadows, no text, letters or numbers anywhere, no people or
animals. Fill the entire square frame edge to edge, nothing cut off, no border or vignette. Square
aspect ratio, generate at the highest resolution available (at least 1600x1600). File name:
jigsaw_source.png."

Check before slicing: every part of the frame has something visually distinct nearby (no large flat
empty area), colors spread fairly evenly across the whole square, nothing important right at the
very edge (a 5x5 cut removes almost nothing per piece, but check anyway).

---

# Tangram — 7 piece shapes

`Rules/Tangram.cs`: 7 placeholder pieces, sprite key `tangram/shape_<0-6>`. The game already scales
each piece by its own size factor at runtime, so the art just needs 7 shapes distinct enough to
tell apart at a glance (color + outline, same reasoning as the pattern shapes).

## Attempt 1: one sheet, all 7 pieces

"Draw a sprite sheet of 7 individual toy-like geometric puzzle pieces for a children's mobile game,
arranged in a single row of 7, evenly spaced with generous plain margin around each one so they can
be cut apart afterwards. Each piece is bold, flat-colored and instantly recognisable (like a glossy
plastic tangram puzzle piece), all drawn at the same size and level of detail regardless of the
shape's real proportions. Style: soft polished 3D-look children's illustration, thin brown outlines,
gentle even lighting, no shadows cast onto neighbouring cells, no text, letters or numbers anywhere,
no people. Background: plain solid magenta (#ff00ff) everywhere, including between cells. The 7
pieces, left to right: 1. a red large right triangle, 2. an orange large right triangle (mirrored
from the first), 3. a yellow medium right triangle, 4. a green small right triangle, 5. a teal small
right triangle, 6. a blue square, 7. a purple parallelogram. File name: tangram_pieces_sheet.png."

Then: "If your tool can produce a real transparent background instead of magenta, use that."

Check before slicing: all 7 clearly different shapes (not just colors), similar overall size,
nothing touching a cell edge or bleeding into a neighbour.

## Attempt 2 (fallback): one piece per image

Only for pieces the sheet didn't produce well. Reuse the shape-tile fallback template from the
Pattern Completion section above, replacing `<SHAPE>`/`<COLOR>` and `File name: tangram_shape_<i>.png`.
