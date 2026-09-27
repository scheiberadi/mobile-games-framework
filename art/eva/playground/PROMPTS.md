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
