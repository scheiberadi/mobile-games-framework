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

## Batch 3: 15 mother animals — `zoofarm/mother_<id>`

The choice pictures for the Mother game (child sees the animal, picks its mother). To read as
clearly different from Batch 1's plain animal portrait (same species, so needs its own visual cue),
each mother is shown in a caring pose/context - nuzzling, watching over, or standing close to a
hinted-at little one - rather than just a bigger copy of the same standing pose.

"Draw a sprite sheet of 15 individual mother-animal portraits for a children's mobile game,
arranged in a grid of 5 columns x 3 rows, evenly spaced with generous plain margin around each one
so they can be cut apart afterwards. Each shows the adult female of its species in a warm, caring
pose (nuzzling downward, looking down gently, or standing protectively) so it reads as "a mother"
at a glance, not just a generic adult standing still. Single animal, centred in its own cell, all
drawn at a similar visual size and level of detail. Style: soft polished 3D-look children's
illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no shadows cast onto
neighbouring cells, no text, letters or numbers anywhere, no people. Background: plain solid
magenta (#ff00ff) everywhere, including between cells. The 15 mothers, in reading order (left to
right, top to bottom): 1. mother cow, 2. mother lion, 3. mother duck, 4. mother owl, 5. mother
sheep, 6. mother fish, 7. mother horse, 8. mother eagle, 9. mother pig, 10. mother snake, 11. mother
chicken, 12. mother frog, 13. mother dog, 14. mother cat, 15. mother elephant. File name:
zoofarm_mothers_sheet.png."

Then: "If your tool can produce a real transparent background instead of magenta, use that."

Check before slicing: each mother reads as caring/maternal (not identical to the Batch 1 portrait
of the same species), still recognisable as its species, similar size/detail, nothing touching a
cell edge or bleeding into a neighbour.
