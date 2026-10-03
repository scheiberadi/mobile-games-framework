# Arcade art prompts for ChatGPT (M4.10, last of the 5 buildings with no gameplay art yet)

Read `Rules/Arcade.cs` first if anything here looks arbitrary - every sprite key below is taken directly
from its `Config()` method, not invented. Six of Arcade's seven games reduce to the shared `MatchScreen`
presenter (`Rules/MatchRoundBuilder.cs`): each needs a **target** sprite (what's shown/asked about) and a
small set of **choice** sprites (what the child taps), under two different prefixes even when they depict
the same kind of thing - see each game's own section for exactly which is which, it is not always obvious
from the prefix name alone. Platformer is the odd one out (reuses `SequenceScreen` directly, one prefix
only).

Style for everything (same as every other building): soft polished 3D-look children's mobile-game
illustration, warm rounded shapes, thin brown outlines, gentle even lighting, no shadows cast onto
neighbouring cells, no text or letters anywhere **except plain digits where the game's own content is a
number** (Balloon Popping's numerals - digits are allowed by the no-reading audit, letters and words are
not), no people. Background: plain solid magenta (#ff00ff) everywhere, including between cells - not a
checkered/transparent placeholder, an actual solid magenta fill.

Same sheet workflow as every other building (`docs/kids-games/m4-handover.md`'s "sheet workflow" section and
its "things that went wrong" list apply unchanged here): generate, paste the result back, convert/save under
`art/eva/arcade/ai/`, add a `SHEETS` entry to `tools/art-import/cut-sheets.js` (`dir: 'arcade/ai'`, `resDir:
'arcade'`, `grid` per the layout each batch below states, `size: 512`), cut with `--install`, visually
verify, commit each batch separately and push immediately.

**A recurring shape below, worth stating once**: for the five "self-referential" games (Whack-a-Mole,
Fishing, Space Shooter, Fruit Catcher, Treasure Hunt), the same underlying thing (a mole, a fish, a ship, a
fruit, a treasure symbol) needs **two different renderings** - a small "card" version (the target, shown as
what the child is looking for) and an "in-scene" choice version (a bigger tile the child actually taps).
They must clearly depict the *same* thing two ways, not two different things.

## Batch 1: Balloon Popping (8) — `arcade/balloonrule_<id>` (targets) + `arcade/balloon_<value>` (choices)

Targets are six balloons each showing ONE digit (the number being judged); choices are two small generic
icons standing for "odd" and "even" (there is no text-free standard symbol for this, so: **odd = a single
loose balloon on its own**, **even = two balloons neatly paired together** - a kid-legible "one alone" vs
"two together" visual, not text. Flag this design choice to Adrian same as every other invented convention
this session).

"Draw a sprite sheet of 8 individual icons for a children's mobile game, arranged in a grid of 4 columns x 2
rows, evenly spaced with generous plain margin around each one so they can be cut apart afterwards, all at a
similar visual size and level of detail. [style paragraph above]. The 8 icons, in reading order:

1. A single round balloon on a short string, with the digit 1 printed on it.
2. The same balloon style with the digit 2 on it.
3. The same balloon style with the digit 3 on it.
4. The same balloon style with the digit 4 on it.
5. The same balloon style with the digit 5 on it.
6. The same balloon style with the digit 6 on it.
7. ONE single balloon on its own, no digit on it, floating alone with visible empty space around it
   (standing for 'odd' - one, unpaired).
8. TWO balloons of the same colour tied together side by side on one string, no digits on them (standing
   for 'even' - a matched pair).

File name: arcade_balloonpopping_sheet.png."

## Batch 2: Whack-a-Mole (12) — `arcade/molecard_<id>` (targets, items 1-6) + `arcade/mole_<id>` (choices, items 7-12)

The 6 moles have no inherent visual meaning of their own (unlike Fishing's colours or Space Shooter's
shapes) - distinguish them ONLY by colour/pattern, never by a letter or label (the underlying ids a-f are
internal, never shown). Card = a simple close-up mole face/portrait; choice = the same mole popping up out
of a round dirt hole, ready to be tapped.

"Draw a sprite sheet of 12 individual icons for a children's mobile game, arranged in a grid of 4 columns x
3 rows, evenly spaced with generous plain margin around each one so they can be cut apart afterwards, all at
a similar visual size and level of detail. [style paragraph above]. Items 1-6 (top two rows) are six
DIFFERENT moles' close-up portrait cards (head and shoulders, friendly cartoon-mole face) - each a clearly
different colour/pattern so they're easy to tell apart at a glance: (1) plain brown, (2) grey, (3) black,
(4) brown with a lighter belly patch, (5) reddish-brown, (6) grey with small dark spots. Items 7-12 (bottom
row plus one more) are the SAME six moles again, in the same colour order, but now shown popping up out of a
round dirt hole (head and front paws visible above the hole rim, rest hidden) instead of as a portrait card.

File name: arcade_whackamole_sheet.png."

## Batch 3: Fishing (12) — `arcade/fishcard_<color>` (targets, items 1-6) + `arcade/fish_<color>` (choices, items 7-12)

Card = a simple side-view fish portrait; choice = the same fish swimming, ready to be "caught".

"Draw a sprite sheet of 12 individual icons for a children's mobile game, arranged in a grid of 4 columns x
3 rows, evenly spaced with generous plain margin around each one so they can be cut apart afterwards, all at
a similar visual size and level of detail. [style paragraph above]. Items 1-6 (top two rows) are six simple
side-view fish portrait cards, one per colour, in this order: red, blue, green, yellow, purple, orange -
same fish shape and fin style throughout, only the colour changes. Items 7-12 (bottom row plus one more) are
the SAME six fish again, same colour order, shown swimming (a gentle curved swimming pose, small bubbles
optional) instead of the static portrait.

File name: arcade_fishing_sheet.png."

## Batch 4: Space Shooter (12) — `arcade/shapecard_<shape>` (targets, items 1-6) + `arcade/ship_<shape>` (choices, items 7-12)

Card = the plain shape/symbol alone; choice = a small friendly rocket/spaceship marked with that same symbol
on its hull.

"Draw a sprite sheet of 12 individual icons for a children's mobile game, arranged in a grid of 4 columns x
3 rows, evenly spaced with generous plain margin around each one so they can be cut apart afterwards, all at
a similar visual size and level of detail. [style paragraph above]. Items 1-6 (top two rows) are six plain
symbol cards, one shape each, in this order: a circle, a square, a triangle, a star, a heart, a diamond - a
single bold-coloured flat shape, simple and clean. Items 7-12 (bottom row plus one more) are six small,
friendly, rounded cartoon spaceships, same body shape and colour scheme throughout, each with ONE of the six
symbols above painted clearly on its hull, in the same order (circle-marked ship, square-marked ship, and so
on) - the symbol is the only thing that changes between the six ships.

File name: arcade_spaceshooter_sheet.png."

## Batch 5: Fruit Catcher (12) — `arcade/fruitcard_<fruit>` (targets, items 1-6) + `arcade/fruit_<fruit>` (choices, items 7-12)

Card and choice are both just the fruit itself - drawn twice (once as a still portrait, once falling/tumbling
for the catching game) since the code expects two separate sprite keys.

"Draw a sprite sheet of 12 individual icons for a children's mobile game, arranged in a grid of 4 columns x
3 rows, evenly spaced with generous plain margin around each one so they can be cut apart afterwards, all at
a similar visual size and level of detail. [style paragraph above]. Items 1-6 (top two rows) are six simple
still fruit portraits, in this order: an apple, a banana, a bunch of grapes, an orange, a pear, a plum - each
fruit whole and clearly recognisable, a small leaf or stem on each for character. Items 7-12 (bottom row
plus one more) are the SAME six fruits again, same order, each drawn tumbling/falling at a slight angle (as
if caught mid-air) instead of sitting still.

File name: arcade_fruitcatcher_sheet.png."

## Batch 6: Treasure Hunt (12) — `arcade/mapsymbol_<item>` (targets, items 1-6) + `arcade/treasure_<item>` (choices, items 7-12)

Per `Rules/Arcade.cs`'s own comment, this pool is a symbol per Arcade's other five games plus a gem - a
generic version of each, not the exact art from those other sheets (those show the SPECIFIC coloured
fish/ship/etc from a round; this game just needs one simple, recognisable stand-in icon per category). Map
symbol = a small hand-drawn-map-style ink icon; treasure = the same subject as a solid painted icon.

"Draw a sprite sheet of 12 individual icons for a children's mobile game, arranged in a grid of 4 columns x
3 rows, evenly spaced with generous plain margin around each one so they can be cut apart afterwards, all at
a similar visual size and level of detail. [style paragraph above]. Items 1-6 (top two rows) are six small
treasure-map-style symbol icons (drawn as if inked onto an old map corner - simple, slightly sketchy linework
but still in this game's own warm rounded style, not scratchy or scary), in this order: a balloon, a mole, a
fish, a ship, a piece of fruit, a gem. Items 7-12 (bottom row plus one more) are the SAME six subjects again,
same order, but drawn as solid, fully painted icons (not the sketchy map-symbol style) - a plain balloon, a
plain mole face, a plain fish, a plain ship, a plain piece of fruit, a sparkling gem.

File name: arcade_treasurehunt_sheet.png."

## Batch 7: Platformer (6) — `arcade/platform_p1`..`p6`

Numbered platforms, tapped in ascending order (Brain Gym's Sequence Ordering precedent) - digits are fine
here (a number IS the content, not decorative text).

"Draw a sprite sheet of 6 individual platform tiles for a children's mobile game, arranged in a grid of 3
columns x 2 rows, evenly spaced with generous plain margin around each one so they can be cut apart
afterwards, all at a similar visual size and level of detail. [style paragraph above]. Each tile: a small
flat platform/ledge shape (as if floating in a simple arcade level - a short chunky rounded rectangle,
seen from a slight front-on angle, like a solid little cloud-platform or block), with ONE large clear digit
printed on its top surface. The 6 tiles, each showing its own digit 1 through 6 in the same style and
position on the platform, only the number changing: 1, 2, 3, 4, 5, 6.

File name: arcade_platformer_sheet.png."

## Checklist

- every icon reads instantly as what it's meant to be, similar visual size/weight across all 7 sheets
- each game's card/target version and choice/in-scene version clearly depict the SAME subject two ways -
  same mole colour, same fish colour, same ship symbol, same fruit, same treasure subject
- the 6 moles are distinguishable by colour/pattern alone, never by a hidden letter or label
- Balloon Popping's odd/even icons read as "one alone" vs "a matched pair", not as a random balloon count -
  if this invented convention doesn't read clearly to Adrian, it needs a different design, flagged here
- only digits appear as "text" anywhere (Balloon Popping's numerals, Platformer's numbers) - no letters, no
  words, anywhere else
- nothing touching a cell edge or bleeding into a neighbour
- if a piece is wrong, ask ChatGPT for a redo with the same prompt

## Batch 8: real-time game props (8) — `arcade/prop_<name>`

Added 2026-10-01 for the answer-variety plan's real-time versions (Balloon Popping, Whack-a-Mole, Fishing, Space Shooter, Fruit
Catcher, Treasure Hunt; `docs/kids-games/answer-variety-plan.md`). Not used by code yet. Deliberately no treasure chest (the
project avoids loot-box imagery): Treasure Hunt uses dirt mounds and a sparkle.

"Draw a sprite sheet of 8 individual game props for a children's mobile game, arranged in a grid of 4 columns x 2 rows, evenly
spaced with generous plain margin around each one so they can be cut apart afterwards, all at a similar visual size and level of
detail. [style paragraph above]. In reading order: 1. prop_basket: a woven wicker fruit basket seen from the front, open at the
top; 2. prop_hook: a fishing hook on a short line with a small red-and-white float above it; 3. prop_mole_hole: an EMPTY round dark
hole in the ground with a ring of soft dirt around it; 4. prop_dirt_mound: a small mound of fresh brown dirt with a few small
stones on it; 5. prop_pop: a bright, cheerful star-shaped burst of colourful confetti pieces (the moment a balloon pops, no
balloon remaining); 6. prop_laser: a short glowing yellow-white star-tipped energy bolt, pointing upward; 7. prop_sparkle: a cluster
of golden sparkles and a small gold star (the 'found it' effect); 8. prop_pond_lilypad: a round green lily pad with a small pink
flower on blue water ripples. File name: arcade_props_sheet.png."

## Batch 9: Fruit Catcher basket v2 (2026-10-02) - `arcade/prop_basket`

The in-game basket matches the basket in the Fruit Catcher menu icon: a wide, open, woven basket with TWO small loop handles, one on each side (not one tall handle over the top), so fruit can drop in from above. Attach `EvasLearningWorld/Assets/Eva/Resources/Art/activities/fruit_catcher.png` as the style reference. Single image on solid magenta, saved as `arcade_basket_v2.png`; cut with `tools/art-import/cut-sheets.js` (entry `arcade_basket_v2`) and installed over `prop_basket`.

## Batch 10: Fishing redesign (2026-10-03) - underwater scene, boat, fish, rod and hook

Fishing is now a side-view underwater game: the child and Eva sit in a boat on the lake surface (top right), the lake fills the rest of the screen, fish of three sizes swim across in rows, a tap sends the hook down. Four images, each saved under the name below and attached together with `EvasLearningWorld/Assets/Eva/Resources/Art/activities/fishing.png` as the style reference (the user's own sketch is a composition reference only, the style stays the app's soft glossy 3D cartoon).

1. `fishing_bg.png` (landscape 1920x900, no magenta): "Draw a landscape background for a children's mobile game, 1920x900, soft polished 3D-look cartoon style like the attached picture, warm rounded shapes, no text, no people, no boat, no fish. Top 28% is sky with two or three soft clouds and a small green island on the horizon at the left. A clear straight horizon line at 28% from the top. Below it the lake seen side-on like a cut-away: light turquoise at the surface fading to deep blue at the bottom, soft light rays, floating bubbles, a sandy bottom with seaweed, rocks and a few shells along the bottom edge. The middle of the water stays open and calm because fish swim over it. Nothing in the top-right sky area."
2. `fishing_boat.png` (single image on solid magenta #ff00ff): "Draw one small empty wooden rowing boat in side view, about 2.7:1 wide, flat top edge so two characters can sit in it, planks visible, warm brown, thin brown outlines, soft polished 3D-look cartoon style like the attached picture. No people, no oars, no fishing rod, no water, no text. Plain solid magenta background."
3. `fishing_fish_sheet.png` (6 fish on solid magenta, 3 columns x 2 rows, generous margin): "Draw a sprite sheet of 6 friendly cartoon fish in strict SIDE view, arranged in a grid of 3 columns x 2 rows with generous plain margin around each so they can be cut apart. ALL six face RIGHT. The body is horizontal and straight (no curved or upright pose; they must look like they swim sideways), same body length and same style for all six, different colours and patterns: 1. orange with white stripes, 2. blue, 3. green with dark stripes, 4. yellow, 5. red with white spots, 6. purple. Big friendly eye, small smile, thin brown outlines, soft polished 3D-look cartoon style like the attached picture. No bubbles, no text. Plain solid magenta background."
4. `fishing_props.png` (2 items on solid magenta, 2 columns x 1 row): "Draw a sprite sheet of 2 items arranged in 2 columns x 1 row with generous plain margin: 1. a child's fishing rod in side view, held diagonal, brown rod with a small red reel and a thin line coming off the tip, about 4:1 wide; 2. a shiny metal fishing hook with a small pink worm on it, hook pointing down, small and simple. Thin brown outlines, soft polished 3D-look cartoon style like the attached picture. No text, no hands. Plain solid magenta background."

## Batch 11: Space Shooter redesign - asteroids and astronaut costumes (2026-10-03)

Space Shooter breaks asteroids instead of popping shapes, and the child and Eva wear space suits on this screen only (the child is just an astronaut whose face is hidden behind the visor; Eva shows her head inside a glass bubble). Three images, each attached with `EvasLearningWorld/Assets/Eva/Resources/Art/activities/space_shooter.png` as the style reference; the Eva one also with `art/eva/cat-v2/eva_cat_reference.png`.

1. `space_asteroids.png` (6 asteroids on solid magenta #ff00ff, 3 columns x 2 rows, generous margin): "Draw a sprite sheet of 6 cartoon asteroids for a children's space game, arranged in a grid of 3 columns x 2 rows with generous plain margin around each so they can be cut apart. Each is a lumpy rock with a different shape (round, long and potato-shaped, angular, with a notch, flat, chunky), warm grey-brown with a few craters and small cracks, one slightly bluish, one slightly reddish, soft polished 3D-look cartoon style like the attached picture, thin dark outlines, friendly and not scary, no faces, no fire, no smoke, no trails. Similar size for all six. Plain solid magenta background."
2. `space_astronaut_player.png` (single image on solid magenta): "Draw one child astronaut standing, full body, front view, feet together, arms slightly away from the body, tall proportions about 1:2.2 (width:height), in a clean white spacesuit with blue and yellow patches and a small yellow star on the chest, white gloves and boots, a round white helmet with a fully reflective golden-orange visor so no face is visible at all, a small life-support backpack just visible at the sides. Soft polished 3D-look children's mobile-game illustration like the attached picture, thin brown outlines, gentle even lighting. No text, no shadow on the ground. Plain solid magenta background."
3. `space_astronaut_eva.png` (single image on solid magenta): "Draw the same black fluffy cat as in the attached cat reference (same face, big green eyes, same proportions, same sitting pose, facing the same direction) wearing a small white spacesuit with a blue and yellow patch, white gloves on the front paws, and a big round transparent glass bubble helmet around her head with a thin metal collar ring: her whole head, ears and face are clearly visible inside the glass, with a few soft reflection highlights on the glass. Her fluffy tail comes out of the back of the suit and stays visible. Soft polished 3D-look children's mobile-game illustration like the attached pictures, thin brown outlines, gentle even lighting. No text, no shadow on the ground. Plain solid magenta background."

## Batch 12: Platformer = Bunny Run (2026-10-03) - forest run with a river, bunny, ground block, carrot, lily pad

Platformer becomes a side-scrolling run: the bunny runs by itself, a tap makes it jump over rivers, carrots are collected. Four images, each attached with `EvasLearningWorld/Assets/Eva/Resources/Art/activities/platformer.png` as the style reference (the bunny in the app's soft glossy 3D cartoon style, NOT realistic).

1. `platformer_bg.png` (landscape 1920x900, no magenta): "Draw a landscape background for a children's mobile side-scrolling game, 1920x900, soft polished 3D-look cartoon style like the attached picture, warm rounded shapes, no text, no people, no animals. A bright sunny sky with two or three soft clouds, gentle green rolling hills in two layers, a few round friendly trees and bushes and some flowers in the distance. The bottom 30% of the picture is a plain soft light-green meadow with no objects (a ground block will be drawn over it). Calm, not busy, the middle stays open."
2. `platformer_ground.png` (single image on solid magenta #ff00ff): "Draw one wide block of ground in side view, about 4:1 wide, flat grassy top edge, a thick layer of brown soil below with a few small stones and tiny roots, both short ends rounded, a thin darker outline, soft polished 3D-look cartoon style like the attached picture. Nothing standing on it, no flowers, no text. Plain solid magenta background."
3. `platformer_bunny_sheet.png` (4 poses on solid magenta, 2 columns x 2 rows, generous margin): "Draw a sprite sheet of the SAME cute white-and-cream bunny with long ears and a little pink nose in strict SIDE view, all four facing RIGHT, same size and the same style, arranged in a grid of 2 columns x 2 rows with generous plain margin around each so they can be cut apart: 1. running, front legs stretched forward and back legs pushed back, ears flying back; 2. running, the opposite stride (legs crossed the other way), ears slightly lower; 3. jumping up high, body stretched, all four legs tucked, ears up; 4. swimming in water, only the head, ears and front paws visible, little splash droplets around, happy face. Big friendly eyes, thin brown outlines, soft polished 3D-look cartoon style like the attached picture. No carrots, no text. Plain solid magenta background."
4. `platformer_props.png` (2 items on solid magenta, 2 columns x 1 row, generous margin): "Draw a sprite sheet of 2 items arranged in 2 columns x 1 row with generous plain margin: 1. one big orange carrot with green leaves, lying slightly tilted, shiny, simple; 2. one round green lily pad seen from a slight angle, with a small pink flower on it and a small notch, floating shape with a soft ripple ring around it. Thin brown outlines, soft polished 3D-look cartoon style like the attached picture. No text. Plain solid magenta background."

## Batch 13: Bunny Run v2 (2026-10-03) - hop poses, extras, world 1 ground strip and obstacles

User dislikes the water, the ground blocks, the monotony and the two-frame run (2026-10-03). New design: a continuous trail with obstacles to jump over, a new world per level (6 worlds), a hopping bunny with a full pose set, a golden star bonus and flying friends in the sky. World 1 = the existing `platformer_bg.png` meadow. Each later world needs three images: `platformer_wN_bg.png` (landscape 1920x900 like world 1), `platformer_wN_ground.png` (a strip like world 1's) and `platformer_wN_obstacles.png` (3 obstacles, low / medium / tall). Planned worlds: 2 forest (mushroom, tree stump, bush), 3 autumn (pumpkin, leaf pile, log), 4 beach (sandcastle, bucket, big shell), 5 snow (small snowman, snowball pile, ice block), 6 twilight with fireflies (glowing mushroom, rock, lantern).

Images attached: `activities/platformer.png` (style) and, for the poses, `platformer_bunny_sheet.png` (the bunny).

1. `platformer_bunny_poses.png` (8 poses on magenta, 4 columns x 2 rows): "Draw a sprite sheet of the SAME cute cream-coloured bunny as in the attached bunny picture (same face, same ears, same proportions and colours, same soft polished 3D-look cartoon style), strict SIDE view, all facing RIGHT, all the same size, arranged in a grid of 4 columns x 2 rows with generous plain margin around each so they can be cut apart. A hopping sequence: 1. crouching low and about to hop, body squashed, ears pressed back; 2. pushing off the ground, body stretched long and diagonal upward, hind legs fully extended behind, ears streaming back; 3. in mid-air at the top of the jump, curled and tucked, legs folded, ears up; 4. coming down, front paws reaching forward and down, hind legs trailing, ears flapping upward; 5. landing, body squashed low on the ground, legs bent, ears bouncing forward; 6. stumbling after bumping into something, tilted forward and wobbly, eyes wide and surprised (not sad, no tears), a few small yellow stars circling above the head; 7. cheering, standing upright on the hind legs with both front paws raised up, big happy smile; 8. sitting still and looking forward, calm and friendly. No ground, no shadows, no text. Plain solid magenta background."
2. `platformer_extras.png` (4 items on magenta, 2 x 2): "Draw a sprite sheet of 4 items arranged in a grid of 2 columns x 2 rows with generous plain margin around each so they can be cut apart: 1. one golden star with a soft glow, chunky and rounded, shiny; 2. a small bluebird flying, wings up, side view facing right; 3. a colourful butterfly with open wings; 4. a friendly fat bumblebee flying, side view facing right. Thin brown outlines, soft polished 3D-look cartoon style like the attached picture. No text. Plain solid magenta background."
3. `platformer_w1_ground.png` (on magenta): "Draw one long straight horizontal strip of meadow ground in side view, about 8:1 wide. The top edge is perfectly flat, short mowed bright green grass with a few tiny white flowers, below it a layer of warm brown soil that fills the rest of the strip, with only very small subtle pebbles. BOTH SHORT ENDS ARE CUT STRAIGHT AND VERTICAL (not rounded) so copies can be placed end to end. No outline around the strip, no objects standing on it. Soft polished 3D-look cartoon style like the attached picture. Plain solid magenta background."
4. `platformer_w1_obstacles.png` (3 on magenta, 3 x 1): "Draw a sprite sheet of 3 friendly obstacles for a children's meadow game, arranged in 3 columns x 1 row with generous plain margin around each: 1. a small grey rock with a bit of moss, low and rounded; 2. a fallen log lying on its side with the rings visible at one end, medium height; 3. a cute hedgehog standing still, side view facing right, with a happy face, a bit taller. Thin brown outlines, soft polished 3D-look cartoon style like the attached picture. No ground, no shadow, no text. Plain solid magenta background."

## Batch 14: Bunny Run worlds 2-6 (2026-10-03) - three images per world

Every world is `platformer_wN_bg.png` + `platformer_wN_ground.png` + `platformer_wN_obstacles.png`, N = 2..6. Attach to every prompt `activities/platformer.png` (style) and, for the ground and obstacles, also `platformer_w1_ground.png` / `platformer_w1_obstacles.png` (so the strip proportions and the size of the obstacles stay the same as in world 1). Sprite names in the game: `w{N}_bg`, `w{N}_ground`, `w{N}_obstacle1..3` (low, medium, tall, left to right). Cut with the same `cut-sheets.js` entries as world 1 (new entries per world).

The three prompts of each world share one template. Background: "Draw a landscape background for a children's mobile side-scrolling game, 1920x900, soft polished 3D-look cartoon style like the attached picture, warm rounded shapes, no text, no people, no animals. <SKY AND SCENERY>. The bottom 30% of the picture is a plain <FLOOR> with no objects (a ground strip will be drawn over it). Calm, not busy, the middle stays open." Ground: "Draw one long straight horizontal strip of <GROUND> in side view, about 8:1 wide. The top edge is perfectly flat, <TOP>, below it a layer of <SOIL> that fills the rest of the strip, with only very small subtle details. BOTH SHORT ENDS ARE CUT STRAIGHT AND VERTICAL (not rounded) so copies can be placed end to end. No outline around the strip, no objects standing on it. Soft polished 3D-look cartoon style like the attached picture. Plain solid magenta background." Obstacles: "Draw a sprite sheet of 3 friendly obstacles for a children's <SETTING> game, arranged in 3 columns x 1 row with generous plain margin around each: <1 LOW>; <2 MEDIUM>; <3 TALL>. Thin brown outlines, soft polished 3D-look cartoon style like the attached picture. No ground, no shadow, no text. Plain solid magenta background."

### World 2: forest
1. `platformer_w2_bg.png`: Draw a landscape background for a children's mobile side-scrolling game, 1920x900, soft polished 3D-look cartoon style like the attached picture, warm rounded shapes, no text, no people, no animals. A friendly sunny forest: tall round-topped trees in two layers with thick brown trunks, soft golden light rays falling between the trees, bright green leaves, a few small flowers and ferns in the distance, pale blue sky showing at the top. The bottom 30% of the picture is a plain soft mossy-green forest floor with no objects (a ground strip will be drawn over it). Calm, not busy, the middle stays open.
2. `platformer_w2_ground.png`: Draw one long straight horizontal strip of forest floor in side view, about 8:1 wide. The top edge is perfectly flat, soft deep-green moss with a few tiny clover leaves and two or three small fallen leaves, below it a layer of dark warm brown soil that fills the rest of the strip, with only very small subtle roots and pebbles. BOTH SHORT ENDS ARE CUT STRAIGHT AND VERTICAL (not rounded) so copies can be placed end to end. No outline around the strip, no objects standing on it. Soft polished 3D-look cartoon style like the attached picture. Plain solid magenta background.
3. `platformer_w2_obstacles.png`: Draw a sprite sheet of 3 friendly obstacles for a children's forest game, arranged in 3 columns x 1 row with generous plain margin around each: 1. a small red mushroom with white spots, low and rounded; 2. a tree stump with the rings visible on top and a little moss, medium height; 3. a round leafy green bush with a few small pink berries, a bit taller. Thin brown outlines, soft polished 3D-look cartoon style like the attached picture. No ground, no shadow, no text. Plain solid magenta background.

### World 3: autumn
1. `platformer_w3_bg.png`: Draw a landscape background for a children's mobile side-scrolling game, 1920x900, soft polished 3D-look cartoon style like the attached picture, warm rounded shapes, no text, no people, no animals. A cosy autumn countryside: a soft peach-and-pale-yellow sky with two or three clouds, gentle rolling hills in two layers, round trees with orange, red and golden leaves, a few leaves drifting in the air, a small wooden fence far away. The bottom 30% of the picture is a plain soft golden-green meadow with no objects (a ground strip will be drawn over it). Calm, not busy, the middle stays open.
2. `platformer_w3_ground.png`: Draw one long straight horizontal strip of autumn ground in side view, about 8:1 wide. The top edge is perfectly flat, short golden-green grass with a few tiny red and orange fallen leaves, below it a layer of warm brown soil that fills the rest of the strip, with only very small subtle pebbles. BOTH SHORT ENDS ARE CUT STRAIGHT AND VERTICAL (not rounded) so copies can be placed end to end. No outline around the strip, no objects standing on it. Soft polished 3D-look cartoon style like the attached picture. Plain solid magenta background.
3. `platformer_w3_obstacles.png`: Draw a sprite sheet of 3 friendly obstacles for a children's autumn game, arranged in 3 columns x 1 row with generous plain margin around each: 1. a low soft pile of red, orange and yellow leaves; 2. a fallen log lying on its side with the rings visible at one end and a few orange leaves on it, medium height; 3. a big round orange pumpkin with a short green stem and a curly vine, a bit taller. Thin brown outlines, soft polished 3D-look cartoon style like the attached picture. No ground, no shadow, no text. Plain solid magenta background.

### World 4: beach
1. `platformer_w4_bg.png`: Draw a landscape background for a children's mobile side-scrolling game, 1920x900, soft polished 3D-look cartoon style like the attached picture, warm rounded shapes, no text, no people, no animals. A sunny tropical beach seen from the side: a bright blue sky with a few clouds, a calm turquoise sea with a clear horizon and tiny white wave lines, a small sailboat far away, two or three friendly palm trees at the sides, a little green island in the distance. The bottom 30% of the picture is a plain soft pale-sand beach with no objects (a ground strip will be drawn over it). Calm, not busy, the middle stays open.
2. `platformer_w4_ground.png`: Draw one long straight horizontal strip of beach sand in side view, about 8:1 wide. The top edge is perfectly flat, smooth pale golden sand with a few tiny seashells and a little starfish, below it a layer of slightly darker damp sand that fills the rest of the strip, with only very small subtle pebbles. BOTH SHORT ENDS ARE CUT STRAIGHT AND VERTICAL (not rounded) so copies can be placed end to end. No outline around the strip, no objects standing on it. Soft polished 3D-look cartoon style like the attached picture. Plain solid magenta background.
3. `platformer_w4_obstacles.png`: Draw a sprite sheet of 3 friendly obstacles for a children's beach game, arranged in 3 columns x 1 row with generous plain margin around each: 1. a big pink spiral seashell, low; 2. a small red toy bucket standing upside down with a little yellow spade leaning on it, medium height; 3. a cute sandcastle with two small towers, little flags and a tiny door, a bit taller. Thin brown outlines, soft polished 3D-look cartoon style like the attached picture. No ground, no shadow, no text. Plain solid magenta background.

### World 5: snow
1. `platformer_w5_bg.png`: Draw a landscape background for a children's mobile side-scrolling game, 1920x900, soft polished 3D-look cartoon style like the attached picture, warm rounded shapes, no text, no people, no animals. A gentle winter landscape: a pale blue sky with soft pink-white clouds, snowy rolling hills in two layers, round green pine trees with thick snow on their branches, a few snowflakes falling softly, a small cosy cabin far away with a little smoke. The bottom 30% of the picture is a plain soft white-and-pale-blue snow field with no objects (a ground strip will be drawn over it). Calm, not busy, the middle stays open.
2. `platformer_w5_ground.png`: Draw one long straight horizontal strip of snowy ground in side view, about 8:1 wide. The top edge is perfectly flat, smooth fresh white snow with soft light-blue shading and a few tiny sparkles, below it a layer of frozen light brown earth with pale blue ice at the top that fills the rest of the strip, with only very small subtle pebbles. BOTH SHORT ENDS ARE CUT STRAIGHT AND VERTICAL (not rounded) so copies can be placed end to end. No outline around the strip, no objects standing on it. Soft polished 3D-look cartoon style like the attached picture. Plain solid magenta background.
3. `platformer_w5_obstacles.png`: Draw a sprite sheet of 3 friendly obstacles for a children's winter game, arranged in 3 columns x 1 row with generous plain margin around each: 1. a low pile of three round snowballs; 2. a square block of clear light-blue ice with a soft shine, medium height; 3. a small cute snowman with a red scarf, carrot nose and two stick arms, a bit taller. Thin brown outlines, soft polished 3D-look cartoon style like the attached picture. No ground, no shadow, no text. Plain solid magenta background.

### World 6: twilight with fireflies
1. `platformer_w6_bg.png`: Draw a landscape background for a children's mobile side-scrolling game, 1920x900, soft polished 3D-look cartoon style like the attached picture, warm rounded shapes, no text, no people, no animals. A magical dusk countryside: a gradient sky from deep violet at the top to warm pink-orange at the horizon, a few small twinkling stars and a big friendly crescent moon, soft dark-teal rolling hills in two layers, round trees as dark soft silhouettes, many small glowing yellow fireflies floating in the air, a few glowing flowers in the distance. The bottom 30% of the picture is a plain deep blue-green meadow with no objects (a ground strip will be drawn over it). Calm, not busy and not scary, the middle stays open.
2. `platformer_w6_ground.png`: Draw one long straight horizontal strip of twilight meadow ground in side view, about 8:1 wide. The top edge is perfectly flat, short deep-teal grass with a few tiny softly glowing blue and yellow flowers, below it a layer of dark purple-brown soil that fills the rest of the strip, with only very small subtle pebbles. BOTH SHORT ENDS ARE CUT STRAIGHT AND VERTICAL (not rounded) so copies can be placed end to end. No outline around the strip, no objects standing on it. Soft polished 3D-look cartoon style like the attached picture. Plain solid magenta background.
3. `platformer_w6_obstacles.png`: Draw a sprite sheet of 3 friendly obstacles for a children's twilight game, arranged in 3 columns x 1 row with generous plain margin around each: 1. a small glowing blue mushroom, low and rounded; 2. a grey-purple rock with a few softly glowing crystals, medium height; 3. an old-fashioned lantern on a short wooden post with a warm yellow glow, a bit taller. Thin brown outlines, soft polished 3D-look cartoon style like the attached picture. No ground, no shadow, no text. Plain solid magenta background.

## Batch 15: Treasure Hunt = metal detector (2026-10-03) - sand map, detector, treasures, junk, props

Treasure Hunt becomes a search game: the child drags the metal detector over a big sand map, the detector beeps faster and finally continuously over a hidden spot, the spot is marked with a red X, tapping digs until the treasure comes out. Five images, each attached with `EvasLearningWorld/Assets/Eva/Resources/Art/activities/treasure_hunt.png` as the style reference. Sprite names in the game: `treasure/bg`, `detector`, `chest crown gem coins ring key`, `can boot bottle`, `mark hole shovel`.

1. `treasure_bg.png` (landscape 1920x900, no magenta): Draw a landscape background for a children's mobile game, 1920x900, soft polished 3D-look cartoon style like the attached picture, warm rounded shapes, no text, no people, no animals. A sunny sandy beach seen from above with a slight angle (a treasure-map view): warm golden sand with soft ripples and tiny grains, a few small starfish and shells only along the very edges, a narrow band of turquoise water with white foam along the top edge, and the soft shadows of two palm leaves in the corners. The middle 70% of the picture is plain calm sand with no objects at all, because the game draws marks and holes on it.
2. `treasure_detector.png` (single image on solid magenta #ff00ff): Draw one cute child-sized metal detector, about 1:1.5 (width:height), seen from above at a slight angle: a wide round flat search coil at the bottom (red with a yellow rim), a short silver pole going up from its centre to a rounded yellow handle, and a small round clear-glass light on the pole (white, so it can be tinted by the game). Thin brown outlines, soft polished 3D-look cartoon style like the attached picture. No hands, no text, no shadow. Plain solid magenta background.
3. `treasure_items.png` (6 treasures on solid magenta, 3 columns x 2 rows, generous margin): Draw a sprite sheet of 6 shiny treasures for a children's game, arranged in a grid of 3 columns x 2 rows with generous plain margin around each so they can be cut apart, all about the same size, front view: 1. a small wooden treasure chest with gold bands, closed; 2. a golden crown with three round coloured jewels; 3. a big sparkling red gem (ruby) with soft highlights; 4. a little heap of gold coins; 5. a gold ring with a blue gem; 6. an old golden key with a round head. Thin brown outlines, soft polished 3D-look cartoon style like the attached picture, friendly and shiny. No text, no sand. Plain solid magenta background.
4. `treasure_junk.png` (3 items on solid magenta, 3 columns x 1 row, generous margin): Draw a sprite sheet of 3 funny junk items that a child can find on a beach, arranged in 3 columns x 1 row with generous plain margin around each: 1. a dented old tin can; 2. a wet old boot with a bit of seaweed on it; 3. an empty green glass bottle. Dull colours (grey, brown, green) compared to treasure, but friendly, thin brown outlines, soft polished 3D-look cartoon style like the attached picture. No text, no sand. Plain solid magenta background.
5. `treasure_props.png` (3 items on solid magenta, 3 columns x 1 row, generous margin): Draw a sprite sheet of 3 items for a children's treasure hunt, arranged in 3 columns x 1 row with generous plain margin around each: 1. a bright red X made of two thick crossed painted strokes, seen from above, lying flat; 2. a small child's shovel with a wooden handle and a blue metal blade, side view, diagonal, with a few grains of sand on it; 3. an empty round hole dug in sand seen from above at a slight angle, dark damp sand inside and a ring of loose sand heaped around it. Thin brown outlines, soft polished 3D-look cartoon style like the attached picture. No text. Plain solid magenta background.
