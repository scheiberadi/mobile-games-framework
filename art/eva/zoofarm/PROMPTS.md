# M4.4 Zoo & Farm — art prompts for ChatGPT

Covers 11 games sharing two small datasets:
- 10 animal games (`Rules/ZooFarm.cs`) reuse one 15-animal table across Habitat, Mother, Food,
  Sound, Footprint, Covering, Babies, Domestic vs Wild, Land/Sea/Air, Classification.
- Geography (`Rules/Geography.cs`, the building's 11th game) reuses the same MATCH shell over an
  11-country table: Flag, Continent, Landmark.

Style for everything: same soft polished 3D-look children's mobile-game illustration as the rest of
the game (`art/eva/house`, `art/eva/map`, `art/eva/playground`): warm, rounded, thin brown outlines,
no text or letters, no people.

## The 15 animals

cow, lion, duck, owl, sheep, fish, horse, eagle, pig, snake, chicken, frog, dog, cat, elephant

## Batch 1: 15 animal portraits — `zoofarm/animal_<id>`

The shared "target" picture for every game except Animal -> Sound (shown as the prompt: "here's an
animal, now pick its habitat/food/mother/etc").

"Draw a sprite sheet of 15 individual animal portraits for a children's mobile game, arranged in a
grid of 5 columns x 3 rows, evenly spaced with generous plain margin around each one so they can be
cut apart afterwards. Each animal is a single friendly-looking adult, front-or-3/4-view, centred in
its own cell, all animals drawn at a similar visual size and level of detail. Style: soft polished
3D-look children's illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no
shadows cast onto neighbouring cells, no text, letters or numbers anywhere, no people. Background:
plain solid magenta (#ff00ff) everywhere, including between cells. The 15 animals, in reading order
(left to right, top to bottom): 1. cow, 2. lion, 3. duck, 4. owl, 5. sheep, 6. fish, 7. horse,
8. eagle, 9. pig, 10. snake, 11. chicken, 12. frog, 13. dog, 14. cat, 15. elephant. File name:
zoofarm_animals_sheet.png."

Then: "If your tool can produce a real transparent background instead of magenta, use that."

Check before slicing: every animal instantly recognisable as its species, similar size/detail,
nothing touching a cell edge or bleeding into a neighbour.

## Batch 2: 15 baby animals — `zoofarm/baby_<id>`

The choice pictures for the Babies game (child sees the adult, picks its baby). Same 15, same order.

"Draw a sprite sheet of 15 individual baby-animal portraits for a children's mobile game, arranged
in a grid of 5 columns x 3 rows, evenly spaced with generous plain margin around each one so they
can be cut apart afterwards. Each is the baby/young version of its species (smaller, rounder,
softer features than an adult), single animal, front-or-3/4-view, centred in its own cell, all
drawn at a similar visual size and level of detail. Style: soft polished 3D-look children's
illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no shadows cast onto
neighbouring cells, no text, letters or numbers anywhere, no people. Background: plain solid
magenta (#ff00ff) everywhere, including between cells. The 15 baby animals, in reading order (left
to right, top to bottom): 1. calf (baby cow), 2. lion cub, 3. duckling, 4. owlet (baby owl),
5. lamb (baby sheep), 6. baby fish, 7. foal (baby horse), 8. eaglet (baby eagle), 9. piglet,
10. baby snake, 11. chick (baby chicken), 12. tadpole-legged baby frog, 13. puppy, 14. kitten,
15. baby elephant. File name: zoofarm_babies_sheet.png."

Then: "If your tool can produce a real transparent background instead of magenta, use that."

Check before slicing: each baby clearly reads as young/small (not just a smaller copy of the adult
sheet), still recognisable as its species, similar size/detail, nothing touching a cell edge or
bleeding into a neighbour.

## Batch 3: 15 mother animals — `zoofarm/mother_<id>` (retry - v1 put a baby in every cell, too
cluttered and it broke the cutter by merging neighbouring cells)

The choice pictures for the Mother game (child sees the animal, picks its mother). SOLO animal only
- no baby in the picture. Reads as "the mother" purely through a warm, gentle expression/pose, same
one-subject-per-cell cleanliness as Batch 1 and 2.

"Draw a sprite sheet of 15 individual mother-animal portraits for a children's mobile game,
arranged in a grid of 5 columns x 3 rows, evenly spaced with generous plain margin around each one
so they can be cut apart afterwards. Each cell shows exactly ONE animal only - the adult female of
its species, with a warm, gentle, nurturing expression (soft eyes, gentle smile) - but no baby, no
second animal and no extra objects anywhere in the picture. Single animal, centred in its own cell,
all drawn at a similar visual size and level of detail. Style: soft polished 3D-look children's
illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no shadows cast onto
neighbouring cells, no text, letters or numbers anywhere, no people. Background: plain solid
magenta (#ff00ff) everywhere, including between cells. The 15 mothers, in reading order (left to
right, top to bottom): 1. mother cow, 2. mother lion, 3. mother duck, 4. mother owl, 5. mother
sheep, 6. mother fish, 7. mother horse, 8. mother eagle, 9. mother pig, 10. mother snake, 11. mother
chicken, 12. mother frog, 13. mother dog, 14. mother cat, 15. mother elephant. File name:
zoofarm_mothers_sheet.png."

Then: "If your tool can produce a real transparent background instead of magenta, use that."

Check before slicing: exactly one animal per cell (no baby, no extra creature), each reads as
gentle/maternal through expression alone, still recognisable as its species, similar size/detail,
nothing touching a cell edge or bleeding into a neighbour.

## Batch 4: 11 footprints — `zoofarm/footprint_<id>`

Only the 11 animals with a real footprint/pawprint (owl, fish, eagle and snake are excluded - talons,
fins and a trail don't fit this game): cow, lion, duck, sheep, horse, pig, chicken, frog, dog, cat,
elephant.

"Draw a sprite sheet of 11 individual animal footprint/track icons for a children's mobile game,
arranged in a grid of 4 columns x 3 rows (one empty cell at the end), evenly spaced with generous
plain margin around each one so they can be cut apart afterwards. Each icon shows just the print(s)
a single walking step of that animal would leave (a paw print, a hoof print, a bird-foot print, a
webbed print etc. as fits the animal), seen from directly above, as a simple flat silhouette-style
mark - not the animal itself. All drawn at a similar visual size and level of detail. Style: soft
polished 3D-look children's illustration, warm brown/grey print tones, thin darker outline, gentle
even lighting, no shadows cast onto neighbouring cells, no text, letters or numbers anywhere, no
people or animals. Background: plain solid magenta (#ff00ff) everywhere, including between cells.
The 11 footprints, in reading order (left to right, top to bottom): 1. cow hoof print, 2. lion paw
print, 3. duck webbed footprint, 4. sheep hoof print, 5. horse hoof print, 6. pig hoof print,
7. chicken foot print, 8. frog webbed footprint, 9. dog paw print, 10. cat paw print, 11. elephant
foot print. File name: zoofarm_footprints_sheet.png."

Then: "If your tool can produce a real transparent background instead of magenta, use that."

Check before slicing: each print shape clearly different from the others (hoof vs paw vs webbed vs
bird-foot), similar size/detail, nothing touching a cell edge or bleeding into a neighbour.

## Batch 5: 11 foods — `zoofarm/food_<name>`

The 11 unique foods across the 15-animal table (several animals share one, e.g. cow and sheep both
eat grass): grass, meat, seeds, mice, plankton, hay, feed, insects, kibble, catfood, leaves.

"Draw a sprite sheet of 11 individual animal-food icons for a children's mobile game, arranged in a
grid of 4 columns x 3 rows (one empty cell at the end), evenly spaced with generous plain margin
around each one so they can be cut apart afterwards. Each icon shows a small appealing pile/serving
of that food, simple and clearly readable at a glance, all drawn at a similar visual size and level
of detail. Style: soft polished 3D-look children's illustration, warm rounded shapes, thin brown
outlines, gentle even lighting, no shadows cast onto neighbouring cells, no text, letters or numbers
anywhere, no people or animals. Background: plain solid magenta (#ff00ff) everywhere, including
between cells. The 11 foods, in reading order (left to right, top to bottom): 1. a small pile of
green grass, 2. a piece of raw meat, 3. a small pile of birdseed, 4. two small grey mice, 5. a
cluster of tiny plankton specks, 6. a bundle of hay, 7. a small pile of pig feed pellets, 8. a couple
of small insects, 9. a scoop of dry dog kibble, 10. a scoop of cat food, 11. a few green leaves.
File name: zoofarm_foods_sheet.png."

Then: "If your tool can produce a real transparent background instead of magenta, use that."

Check before slicing: each food type clearly different from the others at a glance, similar
size/detail, nothing touching a cell edge or bleeding into a neighbour.

## Batch 6: 7 habitats — `zoofarm/habitat_<name>`

The 7 unique habitats across the 15-animal table: farm, savanna, pond, forest, ocean, mountain,
jungle. Small scene icons (a patch of that environment), not just a color swatch.

"Draw a sprite sheet of 7 individual habitat/environment icons for a children's mobile game,
arranged in a single row of 7, evenly spaced with generous plain margin around each one so they can
be cut apart afterwards. Each icon is a small simple scene giving just enough cues to instantly
recognise that place (a fence and a barn roof for farm, tall dry grass and an acacia tree for
savanna, lily pads and cattails for pond, tree trunks and undergrowth for forest, waves and a shell
for ocean, a snowy rocky peak for mountain, dense broad leaves and vines for jungle), no animals or
people in any of them, all drawn at a similar visual size and level of detail. Style: soft polished
3D-look children's illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no
shadows cast onto neighbouring cells, no text, letters or numbers anywhere. Background: plain solid
magenta (#ff00ff) everywhere, including between cells. The 7 habitats, left to right: 1. farm,
2. savanna, 3. pond, 4. forest, 5. ocean, 6. mountain, 7. jungle. File name:
zoofarm_habitats_sheet.png."

Then: "If your tool can produce a real transparent background instead of magenta, use that."

Check before slicing: each habitat instantly recognisable and clearly different from the others,
similar size/detail, nothing touching a cell edge or bleeding into a neighbour.

## Batch 7: 5 coverings — `zoofarm/covering_<name>`

The 5 unique body coverings across the 15-animal table: fur, feathers, wool, scales, skin. Small
texture swatches, not attached to any animal.

"Draw a sprite sheet of 5 individual animal body-covering texture swatches for a children's mobile
game, arranged in a single row of 5, evenly spaced with generous plain margin around each one so
they can be cut apart afterwards. Each is a small rounded patch showing just that texture up close
(soft fur tufts, layered feathers, curly wool, overlapping scales, smooth bare skin) - a material
sample, not an animal or any part of one. All drawn at a similar visual size and level of detail.
Style: soft polished 3D-look children's illustration, thin brown outline around each patch, gentle
even lighting, no shadows cast onto neighbouring cells, no text, letters or numbers anywhere, no
people or animals. Background: plain solid magenta (#ff00ff) everywhere, including between cells.
The 5 coverings, left to right: 1. brown fur, 2. tan feathers, 3. white wool, 4. green scales,
5. grey bare skin. File name: zoofarm_coverings_sheet.png."

Then: "If your tool can produce a real transparent background instead of magenta, use that."

Check before slicing: each texture instantly recognisable and clearly different from the others,
similar size, nothing touching a cell edge or bleeding into a neighbour.
