# M4.5 Science Lab — art prompts for ChatGPT

Covers all 13 games (`Rules/ScienceLab.cs`'s 12 MATCH/SORT kinds + `Rules/PlantGrowth.cs`'s one
SEQUENCE game). Unlike Zoo & Farm, these don't share one entity table, so each game (or small group
of games) has its own item list - see `Rules/ScienceLab.cs` for the exact (id, value) pairs and
sprite-key prefixes. 153 images total across 17 batches.

Style for everything: same soft polished 3D-look children's mobile-game illustration as the rest of
the game (`art/eva/house`, `art/eva/map`, `art/eva/playground`, `art/eva/zoofarm`): warm, rounded,
thin brown outlines, no text or letters, no people. Every prompt below asks for plain solid magenta
(#ff00ff) explicitly as an actual fill, not a checkered/transparent placeholder (learned the hard
way on Geography's flags - some tools draw a literal checkerboard pattern as baked pixels instead of
real transparency, which the cutter can't chroma-key).

## Batch 1: 12 Sink or Float objects — `sciencelab/object_<id>`

`SinkOrFloatItems`: rock(sink), leaf(float), key(sink), balloon(float), coin(sink), cork(float),
spoon(sink), sponge(float), marble(sink), rubber_duck(float), hammer(sink), apple(float).

"Draw a sprite sheet of 12 individual everyday-object icons for a children's mobile game, arranged
in a grid of 4 columns x 3 rows, evenly spaced with generous plain margin around each one so they
can be cut apart afterwards. Each object shown alone, simple and clearly readable at a glance, all
drawn at a similar visual size and level of detail. Style: soft polished 3D-look children's
illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no shadows cast onto
neighbouring cells, no text, letters or numbers anywhere, no people or animals. Background: plain
solid magenta (#ff00ff) everywhere, including between cells - not a checkered/transparent
placeholder, an actual solid magenta fill. The 12 objects, in reading order (left to right, top to
bottom): 1. a grey rock, 2. a green leaf, 3. a metal key, 4. a red balloon, 5. a gold coin, 6. a
cork stopper, 7. a spoon, 8. a yellow sponge, 9. a glass marble, 10. a yellow rubber duck, 11. a
hammer, 12. a red apple. File name: sciencelab_sinkfloat_objects_sheet.png."

Check before slicing: each object instantly recognisable and clearly different from the others,
similar size/detail, nothing touching a cell edge or bleeding into a neighbour.

## Batch 2: 12 Magnet objects — `sciencelab/object_<id>`

`MagnetItems`: nail(magnetic), pencil(nonmagnetic), paperclip(magnetic), leaf2(nonmagnetic),
scissors(magnetic), button(nonmagnetic), fork(magnetic), plastic_cup(nonmagnetic), bottle_cap
(magnetic), wooden_block(nonmagnetic), screw(magnetic), cotton_ball(nonmagnetic).

"Draw a sprite sheet of 12 individual everyday-object icons for a children's mobile game, arranged
in a grid of 4 columns x 3 rows, evenly spaced with generous plain margin around each one so they
can be cut apart afterwards. Each object shown alone, simple and clearly readable at a glance, all
drawn at a similar visual size and level of detail. Style: soft polished 3D-look children's
illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no shadows cast onto
neighbouring cells, no text, letters or numbers anywhere, no people or animals. Background: plain
solid magenta (#ff00ff) everywhere, including between cells - not a checkered/transparent
placeholder, an actual solid magenta fill. The 12 objects, in reading order (left to right, top to
bottom): 1. a metal nail, 2. a pencil, 3. a paperclip, 4. a green leaf, 5. scissors, 6. a button,
7. a fork, 8. a plastic cup, 9. a bottle cap, 10. a small wooden block, 11. a screw, 12. a cotton
ball. File name: sciencelab_magnet_objects_sheet.png."

Check before slicing: each object instantly recognisable and clearly different from the others
(especially the leaf here vs. batch 1's leaf - similar is fine, they're separate sprite keys),
similar size/detail, nothing touching a cell edge or bleeding into a neighbour.

## Batch 3: 14 Living vs Non-Living objects — `sciencelab/object_<id>`

`LivingVsNonLivingItems`: dog(living), stone(nonliving), tree(living), car(nonliving), flower
(living), chair(nonliving), goldfish(living), cloud(nonliving), bird(living), ball(nonliving), ant
(living), book(nonliving), cat(living), table(nonliving).

"Draw a sprite sheet of 14 individual object/creature icons for a children's mobile game, arranged
in a grid of 4 columns x 4 rows (two empty cells at the end), evenly spaced with generous plain
margin around each one so they can be cut apart afterwards. Each shown alone, simple and clearly
readable at a glance, all drawn at a similar visual size and level of detail. Style: soft polished
3D-look children's illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no
shadows cast onto neighbouring cells, no text, letters or numbers anywhere, no people. Background:
plain solid magenta (#ff00ff) everywhere, including between cells - not a checkered/transparent
placeholder, an actual solid magenta fill. The 14 items, in reading order (left to right, top to
bottom): 1. a dog, 2. a grey stone, 3. a tree, 4. a car, 5. a flower, 6. a chair, 7. a goldfish in a
bowl, 8. a fluffy cloud, 9. a bird, 10. a ball, 11. an ant, 12. a book, 13. a cat, 14. a table. File
name: sciencelab_livingnonliving_objects_sheet.png."

Check before slicing: each item instantly recognisable and clearly different from the others,
similar size/detail, nothing touching a cell edge or bleeding into a neighbour.

## Batch 4: 14 Healthy vs Unhealthy foods — `sciencelab/food_<id>`

`HealthyVsUnhealthyItems`: apple(healthy), candy(unhealthy), broccoli(healthy), soda(unhealthy),
carrot(healthy), chips(unhealthy), banana(healthy), cake(unhealthy), yogurt(healthy), donut
(unhealthy), grilled_fish(healthy), fries(unhealthy), salad(healthy), pizza(unhealthy).

"Draw a sprite sheet of 14 individual food icons for a children's mobile game, arranged in a grid of
4 columns x 4 rows (two empty cells at the end), evenly spaced with generous plain margin around
each one so they can be cut apart afterwards. Each food shown alone, appetising and clearly readable
at a glance, all drawn at a similar visual size and level of detail. Style: soft polished 3D-look
children's illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no shadows
cast onto neighbouring cells, no text, letters or numbers anywhere, no people. Background: plain
solid magenta (#ff00ff) everywhere, including between cells - not a checkered/transparent
placeholder, an actual solid magenta fill. The 14 foods, in reading order (left to right, top to
bottom): 1. a red apple, 2. a piece of candy, 3. broccoli, 4. a can of soda, 5. a carrot, 6. a bag
of chips, 7. a banana, 8. a slice of cake, 9. a cup of yogurt, 10. a donut, 11. a grilled fish
fillet, 12. a portion of fries, 13. a bowl of salad, 14. a slice of pizza. File name:
sciencelab_healthyunhealthy_foods_sheet.png."

Check before slicing: each food instantly recognisable and clearly different from the others,
similar size/detail, nothing touching a cell edge or bleeding into a neighbour.

## Batch 5: 8 sorting buckets — `sciencelab/bucket_<name>`

Shared across Sink or Float, Magnet, Living vs Non-Living, Healthy vs Unhealthy. Same basket style
as Zoo & Farm's buckets (`art/eva/zoofarm/PROMPTS.md` batch 8), each marked with a simple symbol
instead of a scene.

"Draw a sprite sheet of 8 individual sorting-basket icons for a children's mobile game, arranged in
a grid of 4 columns x 2 rows, evenly spaced with generous plain margin around each one so they can
be cut apart afterwards. Each icon is the same style of small wooden basket, but holding or marked
with a different simple symbol for its category: a blue water droplet for 'sink' (something heavy
that sinks in water), a small white cloud-like puff for 'float' (something light that floats), a
horseshoe magnet shape for 'magnetic', a grey circle with a diagonal line through it for
'non-magnetic', a small green leaf for 'living', a grey stone/gear shape for 'non-living', a red
heart for 'healthy', a small warning-triangle-free red X shape for 'unhealthy' (no literal text or
letters in the X, just a simple crossed-line mark). All baskets drawn at the same size and level of
detail. Style: soft polished 3D-look children's illustration, warm rounded shapes, thin brown
outlines, gentle even lighting, no shadows cast onto neighbouring cells, no text, letters or numbers
anywhere, no people or animals. Background: plain solid magenta (#ff00ff) everywhere, including
between cells - not a checkered/transparent placeholder, an actual solid magenta fill. The 8
baskets, in reading order (left to right, top to bottom): 1. sink, 2. float, 3. magnetic, 4.
non-magnetic, 5. living, 6. non-living, 7. healthy, 8. unhealthy. File name:
sciencelab_buckets_sheet.png."

Check before slicing: same basket shape throughout (only the symbol changes), each symbol instantly
readable and clearly different from the others, nothing touching a cell edge or bleeding into a
neighbour.

## Batch 6: 5 organs + 5 sense icons — `sciencelab/organ_<id>` + `sciencelab/sense_<name>`

`HumanSensesItems`: eye/sight, ear/hearing, nose/smell, tongue/taste, hand/touch. Row 1 is the
target picture (the organ itself); row 2 is the matching choice icon (a simple symbol for the sense
it's used for, not another organ drawing).

"Draw a sprite sheet of 10 icons for a children's mobile game, arranged in a grid of 5 columns x 2
rows, evenly spaced with generous plain margin around each one so they can be cut apart afterwards.
Top row: 5 individual human sense-organ icons, friendly and simple, not attached to a full face or
body - just the organ itself as a standalone rounded icon (an eye, an ear, a nose, a tongue, an open
hand). Bottom row, in the same left-to-right order: 5 small symbol icons representing what each
organ is used for - a rainbow-colored eye-catching burst for sight, a sound-wave/music-note shape
for hearing, a wavy scent-line rising from a flower for smell, a simple taste-drop shape for taste, a
texture/ripple pattern under a fingertip for touch. All icons drawn at a similar visual size and
level of detail. Style: soft polished 3D-look children's illustration, warm rounded shapes, thin
brown outlines, gentle even lighting, no shadows cast onto neighbouring cells, no text, letters or
numbers anywhere, no full people or faces. Background: plain solid magenta (#ff00ff) everywhere,
including between cells - not a checkered/transparent placeholder, an actual solid magenta fill. Top
row left to right: 1. eye, 2. ear, 3. nose, 4. tongue, 5. hand. Bottom row left to right (matching
the sense above it): 6. sight symbol, 7. hearing symbol, 8. smell symbol, 9. taste symbol, 10. touch
symbol. File name: sciencelab_senses_sheet.png."

Check before slicing: organs and their matching sense-symbols clearly different from each other,
nothing touching a cell edge or bleeding into a neighbour.

## Batch 7: 8 weather scenes — `sciencelab/weather_<name>`

Covers both Weather (identify: sunny, rainy, cloudy, snowy, windy, stormy - audio-led, Eva names it)
and Dress for the Weather's target scenes (sunny, rainy, snowy, windy, hot, cold - 4 shared with the
first list, plus hot/cold new). 8 unique weather conditions total.

"Draw a sprite sheet of 8 individual small weather-scene icons for a children's mobile game,
arranged in a grid of 4 columns x 2 rows, evenly spaced with generous plain margin around each one
so they can be cut apart afterwards. Each icon is a small simple sky/scene giving just enough cues
to instantly recognise that weather (a bright sun for sunny, a cloud with rain streaks for rainy, a
plain grey cloud for cloudy, a cloud with snowflakes for snowy, a cloud with motion swirl lines for
windy, a dark cloud with a lightning bolt for stormy, a bright sun with heat wavy lines for hot, a
snowflake with a chilly blue tint for cold), no people or animals in any of them, all drawn at a
similar visual size and level of detail. Style: soft polished 3D-look children's illustration, warm
rounded shapes, thin brown outlines, gentle even lighting, no shadows cast onto neighbouring cells,
no text, letters or numbers anywhere. Background: plain solid magenta (#ff00ff) everywhere,
including between cells - not a checkered/transparent placeholder, an actual solid magenta fill. The
8 weather icons, in reading order (left to right, top to bottom): 1. sunny, 2. rainy, 3. cloudy, 4.
snowy, 5. windy, 6. stormy, 7. hot, 8. cold. File name: sciencelab_weather_sheet.png."

Check before slicing: each weather condition instantly recognisable and clearly different from the
others, similar size/detail, nothing touching a cell edge or bleeding into a neighbour.

## Batch 8: 15 small choice icons — clothing, measure levels, seasons, day/night

Combines four small unrelated icon sets that are all simple/single-symbol (not scenes), to save a
sheet: `sciencelab/clothing_<name>` (6: Dress for the Weather's choices), `sciencelab/measure_<name>`
(3: Cooking Measures' bucket levels), `sciencelab/season_<name>` (4: Seasons' choices),
`sciencelab/daynight_<name>` (2: Day/Night's choices).

"Draw a sprite sheet of 15 individual small icons for a children's mobile game, arranged in a grid
of 5 columns x 3 rows, evenly spaced with generous plain margin around each one so they can be cut
apart afterwards. Each icon shown alone, simple and clearly readable at a glance, all drawn at a
similar visual size and level of detail. Style: soft polished 3D-look children's illustration, warm
rounded shapes, thin brown outlines, gentle even lighting, no shadows cast onto neighbouring cells,
no text, letters or numbers anywhere, no people. Background: plain solid magenta (#ff00ff)
everywhere, including between cells - not a checkered/transparent placeholder, an actual solid
magenta fill. The 15 icons, in reading order (left to right, top to bottom): 1. a sun hat, 2. a
yellow raincoat, 3. a pair of mittens, 4. a jacket, 5. shorts, 6. a scarf, 7. a measuring cup filled
all the way to the top with water, 8. a measuring cup filled halfway with water, 9. an empty
measuring cup, 10. a small spring scene icon (a single blooming flower with a butterfly), 11. a
small summer scene icon (a bright sun over a beach umbrella), 12. a small fall/autumn scene icon (a
tree with orange falling leaves), 13. a small winter scene icon (a snowflake over a snowy hill), 14.
a bright sun icon for 'day', 15. a crescent moon with a star for 'night'. File name:
sciencelab_smallicons_sheet.png."

Check before slicing: each icon instantly recognisable and clearly different from the others,
similar size, nothing touching a cell edge or bleeding into a neighbour.

## Batch 9: 8 cause scenes — `sciencelab/cause_<id>`

`CauseAndEffectItems`' cause half: rain, drop_glass, water_plant, wind, sun_on_icecream, kick_ball,
press_switch, pin_balloon. Small action scenes, richer than a plain object icon.

"Draw a sprite sheet of 8 individual small cause-and-effect scene icons for a children's mobile
game, arranged in a grid of 4 columns x 2 rows, evenly spaced with generous plain margin around each
one so they can be cut apart afterwards. Each icon is a small simple scene showing an action about
to happen or happening, no captions needed, instantly readable, all drawn at a similar visual size
and level of detail. Style: soft polished 3D-look children's illustration, warm rounded shapes, thin
brown outlines, gentle even lighting, no shadows cast onto neighbouring cells, no text, letters or
numbers anywhere, no people (show hands/objects only where an action needs a hand, no full
characters or faces). Background: plain solid magenta (#ff00ff) everywhere, including between cells
- not a checkered/transparent placeholder, an actual solid magenta fill. The 8 scenes, in reading
order (left to right, top to bottom): 1. rain falling from a cloud, 2. a glass mid-fall about to hit
the ground (unbroken), 3. a hand with a watering can pouring water onto a small plant, 4. wind blowing
(shown as visible motion swirl lines bending a small plant), 5. a sun shining directly onto a scoop
of ice cream in a cone, 6. a foot kicking a ball, 7. a hand pressing a wall light switch, 8. a hand
holding a pin next to an inflated balloon. File name: sciencelab_causes_sheet.png."

Check before slicing: each scene instantly readable as its own distinct action, similar size/detail,
nothing touching a cell edge or bleeding into a neighbour.

## Batch 10: 8 effect scenes — `sciencelab/effect_<id>`

`CauseAndEffectItems`' effect half, in the same order as batch 9's causes so each cause's correct
answer is directly below/after it: wet_ground, broken_glass, grown_plant, flying_kite,
melted_icecream, rolling_ball, light_on, popped_balloon.

"Draw a sprite sheet of 8 individual small result/effect scene icons for a children's mobile game,
arranged in a grid of 4 columns x 2 rows, evenly spaced with generous plain margin around each one
so they can be cut apart afterwards. Each icon is a small simple scene showing the result of an
action, no captions needed, instantly readable, all drawn at a similar visual size and level of
detail. Style: soft polished 3D-look children's illustration, warm rounded shapes, thin brown
outlines, gentle even lighting, no shadows cast onto neighbouring cells, no text, letters or numbers
anywhere, no people (hands/objects only, no full characters or faces). Background: plain solid
magenta (#ff00ff) everywhere, including between cells - not a checkered/transparent placeholder, an
actual solid magenta fill. The 8 scenes, in reading order (left to right, top to bottom): 1. a wet
puddled ground, 2. a broken glass in pieces on the ground, 3. a small plant that has grown tall and
healthy, 4. a kite flying high in the sky on a string, 5. a scoop of ice cream melted into a puddle
on its cone, 6. a ball rolling with motion lines behind it, 7. a wall light turned on and glowing,
8. a popped balloon (a burst rubber shape with small torn pieces). File name:
sciencelab_effects_sheet.png."

Check before slicing: each scene instantly readable as its own distinct result, and clearly the
"after" state of the matching cause in batch 9, similar size/detail, nothing touching a cell edge or
bleeding into a neighbour.

## Batch 11: 9 cooking-measure cup targets — `sciencelab/cup_<a-i>`

`CookingMeasuresItems`: a/d/g = full, b/e/h = half, c/f/i = empty, arranged so each row of 3 is one
ingredient at its three fill levels (row order matches the array: a,d,g are one ingredient's
full/half/empty won't be adjacent in the array, so lay out by ingredient instead - see prompt).
Three different ingredients keep the 9 targets visually distinct: water, flour, milk.

"Draw a sprite sheet of 9 individual measuring-cup icons for a children's mobile game, arranged in a
grid of 3 columns x 3 rows, evenly spaced with generous plain margin around each one so they can be
cut apart afterwards. Each icon is the same style of clear glass measuring cup with a handle,
containing a different ingredient at a different fill level. Row 1 is water (blue-tinted, clear): a
cup full of water, a cup half full of water, an empty cup. Row 2 is flour (pale cream/white,
powdery): a cup full of flour, a cup half full of flour, an empty cup. Row 3 is milk (opaque white):
a cup full of milk, a cup half full of milk, an empty cup. All cups drawn at the same size and
angle. Style: soft polished 3D-look children's illustration, warm rounded shapes, thin brown
outlines, gentle even lighting, no shadows cast onto neighbouring cells, no text, letters or numbers
anywhere, no people. Background: plain solid magenta (#ff00ff) everywhere, including between cells -
not a checkered/transparent placeholder, an actual solid magenta fill. The 9 cups, in reading order
(left to right, top to bottom): 1. water full (a), 2. water half (b), 3. water empty (c), 4. flour
full (d), 5. flour half (e), 6. flour empty (f), 7. milk full (g), 8. milk half (h), 9. milk empty
(i). File name: sciencelab_cups_sheet.png."

Check before slicing: fill level (full/half/empty) instantly clear on every cup, the three
ingredients visually distinct from each other, nothing touching a cell edge or bleeding into a
neighbour.

Note for `tools/art-import/cut-sheets.js`'s `SHEETS.sciencelab_cups` entry: the sprite `names` array
must follow the letters' actual array order (a,d,g,b,e,h,c,f,i per `CookingMeasuresItems`), not this
prompt's by-ingredient reading order (a,b,c,d,e,f,g,h,i) - remap when wiring it up.

## Batch 12: 6 spring/summer season activities — `sciencelab/activity_<id>` (part A of Seasons' 12)

`SeasonsItems`, first half: blooming_flowers(spring), swimming(summer), falling_leaves(fall),
building_snowman(winter), planting_seeds(spring), sandcastle(summer).

"Draw a sprite sheet of 6 individual small seasonal-activity scene icons for a children's mobile
game, arranged in a grid of 3 columns x 2 rows, evenly spaced with generous plain margin around each
one so they can be cut apart afterwards. Each icon is a small simple scene, no people/characters
needed to convey the season (show the activity's objects/setting only), instantly readable, all
drawn at a similar visual size and level of detail. Style: soft polished 3D-look children's
illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no shadows cast onto
neighbouring cells, no text, letters or numbers anywhere, no people. Background: plain solid magenta
(#ff00ff) everywhere, including between cells - not a checkered/transparent placeholder, an actual
solid magenta fill. The 6 scenes, in reading order (left to right, top to bottom): 1. blooming
flowers in a spring meadow, 2. a swimming pool or lake with gentle ripples (summer), 3. a tree with
falling orange autumn leaves, 4. a snowman being built in the snow (winter), 5. small seeds being
planted in a row of soil (spring), 6. a sandcastle on a beach (summer). File name:
sciencelab_seasons_activities_a_sheet.png."

Check before slicing: each scene instantly readable as its own season, similar size/detail, nothing
touching a cell edge or bleeding into a neighbour.

## Batch 13: 6 fall/winter season activities — `sciencelab/activity_<id>` (part B of Seasons' 12)

`SeasonsItems`, second half: picking_apples(fall), sledding(winter), rainbow(spring), sunbathing
(summer), raking_leaves(fall), wearing_coat(winter).

"Draw a sprite sheet of 6 individual small seasonal-activity scene icons for a children's mobile
game, arranged in a grid of 3 columns x 2 rows, evenly spaced with generous plain margin around each
one so they can be cut apart afterwards. Each icon is a small simple scene, no people/characters
needed to convey the season (show the activity's objects/setting only, except where a coat needs to
be shown as a garment on its own, not worn by a person), instantly readable, all drawn at a similar
visual size and level of detail. Style: soft polished 3D-look children's illustration, warm rounded
shapes, thin brown outlines, gentle even lighting, no shadows cast onto neighbouring cells, no text,
letters or numbers anywhere, no people. Background: plain solid magenta (#ff00ff) everywhere,
including between cells - not a checkered/transparent placeholder, an actual solid magenta fill. The
6 scenes, in reading order (left to right, top to bottom): 1. a basket full of apples being picked
from an apple tree (fall), 2. a sled going down a snowy hill (winter), 3. a rainbow over a green
meadow after rain (spring), 4. a beach towel and sunglasses under a bright sun (summer), 5. a rake
next to a pile of autumn leaves (fall), 6. a warm winter coat on its own (winter). File name:
sciencelab_seasons_activities_b_sheet.png."

Check before slicing: each scene instantly readable as its own season, similar size/detail, nothing
touching a cell edge or bleeding into a neighbour.

## Batch 14: 5 day activities — `sciencelab/activity_<id>` (part A of Day/Night's 10)

`DayNightItems`, day half: sun(day), breakfast(day), school_bus(day), playing_outside(day),
daytime_walk(day).

"Draw a sprite sheet of 5 individual small daytime-activity scene icons for a children's mobile
game, arranged in a single row of 5, evenly spaced with generous plain margin around each one so
they can be cut apart afterwards. Each icon is a small simple scene, no people/characters needed
(show the activity's objects/setting only, in bright daylight colours), instantly readable, all
drawn at a similar visual size and level of detail. Style: soft polished 3D-look children's
illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no shadows cast onto
neighbouring cells, no text, letters or numbers anywhere, no people. Background: plain solid magenta
(#ff00ff) everywhere, including between cells - not a checkered/transparent placeholder, an actual
solid magenta fill. The 5 scenes, left to right: 1. a bright sun high in a blue sky, 2. a breakfast
plate with toast and a fried egg, 3. a yellow school bus, 4. a ball and a jump rope on green grass
(playing outside), 5. a sunny path/sidewalk scene for a daytime walk. File name:
sciencelab_daynight_activities_a_sheet.png."

Check before slicing: each scene instantly reads as daytime, clearly different from the others,
similar size/detail, nothing touching a cell edge or bleeding into a neighbour.

## Batch 15: 5 night activities — `sciencelab/activity_<id>` (part B of Day/Night's 10)

`DayNightItems`, night half: moon(night), stars(night), sleeping(night), pajamas(night), owl(night).

"Draw a sprite sheet of 5 individual small nighttime-activity scene icons for a children's mobile
game, arranged in a single row of 5, evenly spaced with generous plain margin around each one so
they can be cut apart afterwards. Each icon is a small simple scene, no people/characters needed
(show the activity's objects/setting only, in cool nighttime colours - dark blues/purples), instantly
readable, all drawn at a similar visual size and level of detail. Style: soft polished 3D-look
children's illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no shadows
cast onto neighbouring cells, no text, letters or numbers anywhere, no people. Background: plain
solid magenta (#ff00ff) everywhere, including between cells - not a checkered/transparent
placeholder, an actual solid magenta fill. The 5 scenes, left to right: 1. a crescent moon in a dark
sky, 2. a scatter of twinkling stars in a dark sky, 3. a cozy bed with a blanket turned down
(sleeping), 4. a folded pair of pajamas, 5. an owl perched on a branch at night. File name:
sciencelab_daynight_activities_b_sheet.png."

Check before slicing: each scene instantly reads as nighttime, clearly different from the others,
similar size/detail, nothing touching a cell edge or bleeding into a neighbour.

## Batch 16: 8 space objects — `sciencelab/space_<name>`

`SpaceItems`, audio-led (Eva names it, same shape as Zoo & Farm's Animal -> Sound and Geography's
flags/landmarks): sun, earth, moon, mars, star, rocket, astronaut, saturn.

"Draw a sprite sheet of 8 individual space-themed icons for a children's mobile game, arranged in a
grid of 4 columns x 2 rows, evenly spaced with generous plain margin around each one so they can be
cut apart afterwards. Each icon shown alone against the background, simple and clearly readable at a
glance, all drawn at a similar visual size and level of detail. Style: soft polished 3D-look
children's illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no shadows
cast onto neighbouring cells, no text, letters or numbers anywhere. The astronaut is a friendly
astronaut suit/helmet (a character in a suit is fine here - it's the subject, not a background
person). Background: plain solid magenta (#ff00ff) everywhere, including between cells - not a
checkered/transparent placeholder, an actual solid magenta fill. The 8 objects, in reading order
(left to right, top to bottom): 1. the sun, 2. Earth (blue and green globe), 3. the moon (grey,
cratered), 4. Mars (reddish-orange globe), 5. a twinkling star, 6. a rocket, 7. an astronaut in a
white spacesuit with helmet, 8. Saturn (with its ring). File name: sciencelab_space_sheet.png."

Check before slicing: each object instantly recognisable and clearly different from the others,
similar size/detail, nothing touching a cell edge or bleeding into a neighbour.

## Batch 17: 5 plant growth stages — `sciencelab/stage_<name>`

`PlantGrowthRoundGenerator.Stages`, in growth order: seed, sprout, seedling, flower, fruit. The
game's only SEQUENCE round (child arranges these in order), so growth progression must be visually
obvious at a glance even before reading the order.

"Draw a sprite sheet of 5 individual plant-growth-stage icons for a children's mobile game, arranged
in a single row of 5, evenly spaced with generous plain margin around each one so they can be cut
apart afterwards, showing one consistent plant's life cycle getting visibly bigger/more developed
from left to right. All drawn at a similar visual size (the plant itself grows across the row, but
keep each icon's own canvas size consistent) and level of detail. Style: soft polished 3D-look
children's illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no shadows
cast onto neighbouring cells, no text, letters or numbers anywhere, no people. Background: plain
solid magenta (#ff00ff) everywhere, including between cells - not a checkered/transparent
placeholder, an actual solid magenta fill. The 5 stages, left to right: 1. a single seed sitting on
soil, 2. a small sprout with two tiny leaves just breaking through the soil, 3. a young seedling
with a short stem and a few small leaves, 4. a grown plant with an open flower, 5. the same plant
with ripe fruit growing on it. File name: sciencelab_plantgrowth_stages_sheet.png."

Check before slicing: growth progression obvious at a glance left to right, similar size/framing so
they read as one consistent plant across the sequence, nothing touching a cell edge or bleeding into
a neighbour.

## Batch 18: Day/Night bins and Space orbit backdrop (3) — `sciencelab/bin_day`, `sciencelab/bin_night`, `sciencelab/orbit_backdrop`

Added 2026-10-01 for the answer-variety plan (`docs/kids-games/answer-variety-plan.md`, Science Lab): Day and Night as "drag the
activity to the day or night panel" and Space as "drag the planets onto the orbit marks". Not used by code yet; generated now so
the batch does not need to be reopened later.

"Draw a sprite sheet of 3 individual pictures for a children's mobile game, arranged in a single row of 3, evenly spaced with
generous plain margin around each one so they can be cut apart afterwards. Style: soft polished 3D-look children's illustration,
warm rounded shapes, thin brown outlines, gentle even lighting, no shadows cast onto neighbouring cells, no text, letters or
numbers anywhere, no people. Background: plain solid magenta (#ff00ff) everywhere, including between cells - not a
checkered/transparent placeholder, an actual solid magenta fill. The 3 pictures, left to right: 1. a tall rounded-rectangle
DAY panel: a bright blue sky with a sun and a puffy cloud over green grass, an open frame that a small icon can be dropped onto;
2. a tall rounded-rectangle NIGHT panel of the same size and frame: a deep blue sky with a moon and stars over dark hills; 3. a
wide space picture: a dark blue starry sky with a small yellow sun at the left and four empty dotted circular orbit rings
around it getting bigger, each ring with one small empty round marker on it (the places where planets will be dropped), no
planets drawn. File name: sciencelab_bins_orbit_sheet.png."


## Sink or Float: the tank scene — `world/sink_float_bg`

The full-screen background of Sink or Float (`SinkOrFloatScreen`): the shelves hold the six things, the glass tank is filled by code with
animated water. So the picture must show an EMPTY tank and EMPTY shelves; their positions are measured from the picture afterwards
(`SinkOrFloatScreen` constants). Layout it must follow, as a share of the 32:15 picture: shelf unit x 26-52%, plank tops at y 36%, 62%, 89%;
tank glass inside x 53-86%, y 25-72%, table top at y 73%. Keep the top-left and top-right corners calm (Back button, coin counter).

"Draw a full-screen background illustration for a children's mobile game, wide landscape, exactly 32:15 aspect ratio (about 1920x900
pixels). A cheerful sunny children's science lab seen straight from the front (flat, no perspective tilt), the same soft polished
3D-look children's illustration style as a cosy game scene: warm rounded shapes, thin brown outlines, gentle even lighting, a teal
wall, warm wood, a bright window with trees outside. No people, no animals, no text, letters or numbers anywhere.
The composition is strict, because game objects will be placed on top of it. 1) LEFT-CENTRE, from 26% to 52% of the picture width: one
tall wooden shelving unit with a plain back panel and exactly THREE wide, thick, perfectly horizontal wooden shelf planks, evenly
spaced, the top surface of the planks at 36%, 62% and 89% of the picture height. The shelves are completely EMPTY (nothing on them) so
objects can be put there later. 2) RIGHT-CENTRE, from 53% to 86% of the picture width: one large EMPTY rectangular glass aquarium
tank, front view, perfectly rectangular with thick light-blue glass edges, a rim along the top and a base along the bottom, soft shine
streaks on the glass; NO water and NOTHING inside it (just the pale wall seen through the glass); the inside of the glass spans from
53% to 86% of the width and from 25% to 72% of the height; it stands on a sturdy wooden lab table whose top surface is at 73% of the
height. 3) The far left (0-24% of the width) and the far right (88-100%) are calm, low-detail decoration only: a plant, a microscope,
a few colourful flasks, a window. The top 12% of the picture and the bottom-right corner are kept simple and uncluttered. Nothing in
front of the shelves or the tank. File name: sink_float_bg.png."

Then import: `node tools/art-import/import-scene-bg.js <file> sink_float_bg`, and measure where the plank tops and the glass inside really are
(the picture is only roughly obedient); move `SinkOrFloatScreen`'s `CellX/CellY`, `TankCentre`, `TankWidth/TankHeight` to the measurements.
