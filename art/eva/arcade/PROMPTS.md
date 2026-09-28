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
