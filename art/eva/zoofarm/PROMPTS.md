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

## Batch 8: 9 sorting buckets — `zoofarm/bucket_<name>`

Shared across Domestic vs Wild (2), Land/Sea/Air (3) and Classification's high-level compound split
(4 more). Each is a small basket/bin icon carrying a simple symbol for its category, not a real
scene, so they read consistently as "a bucket to sort into" rather than another habitat picture.

"Draw a sprite sheet of 9 individual sorting-basket icons for a children's mobile game, arranged in
a grid of 3 columns x 3 rows, evenly spaced with generous plain margin around each one so they can
be cut apart afterwards. Each icon is the same style of small wooden basket, but holding or marked
with a different simple symbol for its category: a red barn silhouette for 'domestic', a green palm
tree silhouette for 'wild', a small brown grassy mound for 'land', a blue wave for 'sea', a white
cloud for 'air', a red barn on a small grassy mound for 'domestic + land', a red barn beside a blue
wave for 'domestic + water', a green palm tree on a small grassy mound for 'wild + land', a green
palm tree beside a blue wave for 'wild + water'. All baskets drawn at the same size and level of
detail. Style: soft polished 3D-look children's illustration, warm rounded shapes, thin brown
outlines, gentle even lighting, no shadows cast onto neighbouring cells, no text, letters or numbers
anywhere, no people or animals. Background: plain solid magenta (#ff00ff) everywhere, including
between cells. The 9 baskets, in reading order (left to right, top to bottom): 1. domestic,
2. wild, 3. land, 4. sea, 5. air, 6. domestic + land, 7. domestic + water, 8. wild + land, 9. wild +
water. File name: zoofarm_buckets_sheet.png."

Then: "If your tool can produce a real transparent background instead of magenta, use that."

Check before slicing: same basket shape throughout (only the symbol changes), each symbol/combo
instantly readable and clearly different from the others, nothing touching a cell edge or bleeding
into a neighbour.

## Geography (the building's 11th game): 28 images across 3 batches

`Rules/Geography.cs`'s 11-country table (id, continent): romania/europe, france/europe,
spain/europe, usa/north_america, brazil/south_america, egypt/africa, kenya/africa, china/asia,
japan/asia, india/asia, australia/oceania. Three MATCH modes gate by level (Flag → Continent →
Landmark), each its own sprite-key prefix: `geo/flag_<id>`, `geo/continent_<name>` (6 unique
continents), `geo/landmark_<id>` (one landmark per country, reuses the country `id`).

## Batch 9: 11 flags — `geo/flag_<id>`

"Draw a sprite sheet of 11 individual national flag icons for a children's mobile game, arranged in
a grid of 4 columns x 3 rows (one empty cell at the end), evenly spaced with generous plain margin
around each one so they can be cut apart afterwards. Each flag is shown as a simple rounded-corner
rectangular banner with a gentle wave/fold, accurate to its country's real flag colours and pattern,
no coat-of-arms fine detail beyond simple shapes, no readable text or letters anywhere even if the
real flag has some. All flags drawn at the same size and angle. Style: soft polished 3D-look
children's illustration, gentle even lighting, thin dark outline around each banner, no shadows cast
onto neighbouring cells, no people. Background: plain solid magenta (#ff00ff) everywhere, including
between cells. The 11 flags, in reading order (left to right, top to bottom): 1. Romania, 2. France,
3. Spain, 4. USA, 5. Brazil, 6. Egypt, 7. Kenya, 8. China, 9. Japan, 10. India, 11. Australia. File
name: geo_flags_sheet.png."

Then: "If your tool can produce a real transparent background instead of magenta, use that."

Check before slicing: each flag's colours/pattern correct and recognisable, no readable text
rendered anywhere, similar size/angle, nothing touching a cell edge or bleeding into a neighbour.

## Batch 10: 6 continents — `geo/continent_<name>`

"Draw a sprite sheet of 6 individual continent icons for a children's mobile game, arranged in a
single row of 6, evenly spaced with generous plain margin around each one so they can be cut apart
afterwards. Each icon is a small simplified silhouette map shape of that continent, as if cut from a
world map, filled with a single warm solid colour (a different colour per continent), no other
landmasses visible, no country borders, no text, letters or numbers, no people or animals. All
drawn at a similar visual size. Style: soft polished 3D-look children's illustration, thin brown
outline around each silhouette, gentle even lighting, no shadows cast onto neighbouring cells.
Background: plain solid magenta (#ff00ff) everywhere, including between cells. The 6 continents,
left to right: 1. Europe, 2. North America, 3. South America, 4. Africa, 5. Asia, 6. Oceania. File
name: geo_continents_sheet.png."

Then: "If your tool can produce a real transparent background instead of magenta, use that."

Check before slicing: each silhouette instantly recognisable as its continent's real shape and
clearly different from the others, similar size, nothing touching a cell edge or bleeding into a
neighbour.

## Batch 11: 11 landmarks — `geo/landmark_<id>`

One iconic, instantly-recognisable-to-a-child landmark per country: 1. Romania — a turreted
fairytale hilltop castle (Bran Castle), 2. France — the Eiffel Tower, 3. Spain — the Sagrada
Família's spired towers, 4. USA — the Statue of Liberty, 5. Brazil — the Christ the Redeemer statue
on its mountain, 6. Egypt — the Great Pyramids of Giza with the Sphinx, 7. Kenya — snow-capped Mount
Kilimanjaro, 8. China — the Great Wall winding over hills, 9. Japan — Mount Fuji with a small red
torii gate, 10. India — the Taj Mahal, 11. Australia — the Sydney Opera House.

"Draw a sprite sheet of 11 individual famous-landmark icons for a children's mobile game, arranged
in a grid of 4 columns x 3 rows (one empty cell at the end), evenly spaced with generous plain
margin around each one so they can be cut apart afterwards. Each icon is a small simple scene
showing just that one landmark, instantly recognisable, no other landmarks or buildings in the same
cell, all drawn at a similar visual size and level of detail. A landmark that is itself a statue of
a human figure (the Statue of Liberty, Christ the Redeemer) may show that statue - it is the
landmark, not a person in the scene - but do not add any other people, tourists or characters
anywhere. Style: soft polished 3D-look children's illustration, warm rounded shapes, thin brown
outlines, gentle even lighting, no shadows cast onto neighbouring cells, no text, letters or numbers
anywhere. Background: plain solid magenta (#ff00ff) everywhere, including between cells. The 11
landmarks, in reading order (left to right, top to bottom): 1. Bran Castle (Romania), 2. Eiffel
Tower (France), 3. Sagrada Família (Spain), 4. Statue of Liberty (USA), 5. Christ the Redeemer
(Brazil), 6. Pyramids of Giza with Sphinx (Egypt), 7. Mount Kilimanjaro (Kenya), 8. Great Wall of
China (China), 9. Mount Fuji with a torii gate (Japan), 10. Taj Mahal (India), 11. Sydney Opera
House (Australia). File name: geo_landmarks_sheet.png."

Then: "If your tool can produce a real transparent background instead of magenta, use that."

Check before slicing: each landmark instantly recognisable and clearly different from the others,
similar size/detail, nothing touching a cell edge or bleeding into a neighbour.

# Roster expansion (2026-10-05): 15 -> 37 animals, about 5 per habitat

The user found the habitat game lopsided (farm 7, ocean 1). 22 new animals; Mother game reuses the adult
sprite for the mother (no `mother_<id>` for the new ones). Art: adult + baby + footprint per animal (footprint
only where a print makes sense), plus 5 foods and 1 covering. Save the sheets in Downloads under the exact
names below, attach `sheet_zoofarm_animals.png` (or babies / footprints / foods) from `art/eva/zoofarm/ai` as the
style reference.

New animals by habitat (reading order inside each sheet):
- savanna: giraffe, zebra, rhino
- pond: turtle, swan, beaver
- ocean: dolphin, whale, shark, octopus
- forest: fox, bear, deer, squirrel
- mountain: goat, wolf, llama, snow leopard
- jungle: monkey, tiger, parrot, gorilla

## Batch 9: 22 new adult animals — `zoofarm/animal_<id>` (2 sheets of 11)

Sheet 1 (`sheet_zoofarm_animals_new1.png`): 1. giraffe, 2. zebra, 3. rhino, 4. turtle, 5. swan, 6. beaver,
7. dolphin, 8. whale, 9. shark, 10. octopus, 11. fox.

Sheet 2 (`sheet_zoofarm_animals_new2.png`): 1. bear, 2. deer, 3. squirrel, 4. goat, 5. wolf, 6. llama,
7. snow leopard, 8. monkey, 9. tiger, 10. parrot, 11. gorilla.

Prompt (same wording as Batch 1, 4 columns x 3 rows with the last cell empty): "Draw a sprite sheet of 11
individual animal portraits ... in the same style as the attached sheet ..."

## Batch 10: 22 new baby animals — `zoofarm/baby_<id>` (2 sheets of 11)

`sheet_zoofarm_babies_new1.png`: giraffe calf, zebra foal, rhino calf, baby turtle, cygnet, beaver kit, dolphin
calf, whale calf, baby shark, baby octopus, fox kit.
`sheet_zoofarm_babies_new2.png`: bear cub, fawn, baby squirrel, kid (baby goat), wolf pup, cria (baby llama),
snow leopard cub, baby monkey, tiger cub, parrot chick, baby gorilla.

## Batch 11: CANCELLED (no new footprints)

The Footprint game is hard at 4-5 years: cow/sheep/goat/deer/giraffe are all split hooves, lion/dog/cat/tiger/wolf/fox/bear
all paws. Footprint now only uses animals whose print is unique and recognisable: elephant, horse, duck, chicken, frog,
dog (the paw), cow (the split hoof). Lion, sheep, pig, cat and every new animal have Footprint = null.

## Batch 12: 5 foods + 1 covering (1 sheet of 6)

`sheet_zoofarm_extras_new.png`: `food_fish`, `food_honey`, `food_nuts`, `food_banana`, `food_shrimp`,
`covering_shell`.

## Batch 13: Feeding game, conveyor belt + thought bubble (2 single images, magenta background)

`zoofarm_belt.png` (cut with `node tools/art-import/cut-sheets.js zoofarm_belt --install`, specs `zoofarm_belt` / `zoofarm_bubble`),
then `node tools/art-import/make-feeding-art.js` writes `belt.png` (trimmed, one slat period = 88 px), `belt_slats.png` (one period
of the slat pattern, scrolled in code as a repeating texture so the still belt looks like it moves) and `bubble.png` (trimmed).
The measured numbers (slat period 91 px between x 181 and 1000, light surface rows 4-46) are in `make-feeding-art.js`; re-measure
if the belt is regenerated. Prompts: one wide conveyor belt seen from the front with evenly spaced dark cross-stripes, a darker rubber
edge, a wood body and a round roller at each end, nothing on it; one empty wide cloud-shaped thought bubble with two small circles as its tail.

## Batch 14: Domestic vs Wild, its own background (1 image) - `world/domestic_wild_bg`

Own scene for the Domestic vs Wild game (not the shared `zoofarm_bg`): farm on the left (barn behind a fenced yard, open gate), forest on
the right (trees behind a fenced yard, open gate), one dirt road from the bottom that forks into two paths, each ending at a gate.
Both yards are EMPTY in the picture; the game stands its own animal sprites in them (2 already there + the ones the child sorts).
Importer (to write): cover-resize to 1920 wide, crop to 900 high, `Resources/Art/world/domestic_wild_bg.png`. Positions the code will use
(1920x900 frame, canvas units x -960..960, y 450..-450): farm gate ~(-480,-20), forest gate ~(100,-20), fork ~(-200,-130), road start
at the bottom edge x ~-200; farm yard x -680..-300, forest yard x -80..280, both y -20..180. Re-measure on the real art.

```
Cute, friendly 3D-rendered storybook / mobile-game art style for a preschool learning app: warm, glossy, toy-like look, soft rounded shapes, thick soft outlines, saturated warm colors, gentle ambient lighting with soft shadows, no text or watermarks anywhere in the image, no people, animals or characters anywhere in the image (all fenced yards stay EMPTY of animals). Wide view filling the entire frame edge to edge - no border, no vignette, no letterboxing, no magenta, no transparency. Canvas aspect ratio approximately 1920x900 (width x height); if you can only make a taller image, keep ALL important things inside its middle 70% in height, the top and bottom 15% are only extra sky and grass that will be cropped off.

Image: one sunny daytime scene split into two halves that share one dirt road. LEFT HALF = a farm: a red wooden barn at the back (upper left, large, with a hay loft door), behind a low wooden fence that encloses an empty grassy yard in front of the barn. The yard occupies about 22% of the picture width (centred at 25% from the left) and about 22% of its height (from 29% to 52% from the top). The front side of the fence runs along 52% from the top and has a wide OPEN wooden gate in its middle, at 25% from the left. RIGHT HALF = a forest: tall friendly trees, bushes, mossy rocks and flowers at the back and all the way to the right edge, with a low wooden fence enclosing an empty grassy clearing in front of the trees. The clearing is about 19% of the picture width (centred at 55% from the left) and spans 29% to 52% from the top. Its front fence also runs along 52% from the top with a wide OPEN wooden gate in its middle, at 55% from the left. Both fences are the same style, the same height and on the same line, so they look like two pens side by side; the barn side looks tidy and sunny, the forest side looks wilder with darker green trees.

THE ROAD: one wide light-brown dirt road starts at the bottom edge of the picture centred at about 40% from the left, where it is about 14% of the picture width wide, and runs straight up to a fork at about 65% from the top. There it splits into two narrower paths (each about 7% wide): the LEFT path curves gently up to the farm gate and ends exactly at the open gate (25% from the left, 55% from the top); the RIGHT path curves gently up to the forest gate and ends exactly at the open gate (55% from the left, 55% from the top). Between and around the paths: green meadow with a few flowers and small stones. Keep the bottom-right corner (right 28% and bottom 25% of the picture) calm meadow with no objects, and keep the lower-middle around the start of the road calm and uncluttered, because game pieces will be placed on top there. Output filename: domestic_wild_bg.png
```

## Batch 15: Land / Sea / Air, its own background (1 image) - `world/land_sea_air_bg`

Three clear horizontal bands: SKY (top 34%), LAND meadow (34%-62%), WATER (62%-100%, bay shape, sandy beach in the bottom-right corner
for the companion pair). A wooden pedestal in the land band at the left is where the current animal waits. All three bands are EMPTY of
animals; the game stands its own animal sprites in them (about 5 already there + the ones the child sorts). Drop anywhere in a band.
Importer: same as import-domestic-wild-bg.js (cover-resize to 1920 wide, crop to 900 high). Layout numbers to measure on the real art.

```
Cute, friendly 3D-rendered storybook / mobile-game art style for a preschool learning app (same look as the attached farm-and-forest background, attach domestic_wild_bg2.png as the style reference): warm, glossy, toy-like look, soft rounded shapes, thick soft outlines, saturated warm colors, gentle ambient lighting with soft shadows, no text or watermarks anywhere in the image, no people, no animals or characters anywhere in the image (all three zones stay EMPTY). Wide view filling the entire frame edge to edge - no border, no vignette, no letterboxing, no magenta, no transparency. Canvas aspect ratio approximately 1920x900 (width x height); if you can only make a taller image, keep ALL important things inside its middle 70% in height.

Image: one sunny scene made of THREE CLEARLY SEPARATE HORIZONTAL ZONES, so a child sees at a glance "up in the air", "on the land", "in the water". Every zone is a big open area with nothing standing in it, because animals will be placed there later.
1. AIR (top 34% of the picture height, full width): a big bright blue sky with a few soft white clouds, mostly open and empty. A small friendly sun may sit in the top-right corner. At the bottom of the sky a distant line of low blue hills and tiny trees marks the horizon (at 34% from the top).
2. LAND (from 34% to 62% from the top, full width): a wide open green meadow, flat and empty in the middle, with a few flowers and small bushes only at the far left and far right edges. At the left of this zone, at 20% from the left and 54% from the top, a round wooden tree-stump pedestal about 16% of the picture width wide with a flat top (an empty stage where one animal will wait), nothing else near it.
3. WATER (from 62% to 100% from the top): a big calm clear blue lake or sea with gentle small waves, open and empty, shaped like a wide bay. Between the meadow and the water runs a clear curved sandy shore with a few pebbles (at about 62% from the top). The water covers the whole bottom of the picture EXCEPT the bottom-right corner: the right 28% of the width and the bottom 27% of the height is a calm, empty sandy beach (no objects, no shells), where the shoreline curves up to meet the meadow.
The three zones must differ strongly in colour (blue sky, green meadow, deeper blue water) with clear edges between them (horizon line, sandy shore). Nothing floats in the sky or the water: no birds, no boats, no fish, no rocks in the middle of the water. Output filename: land_sea_air_bg.png
```
