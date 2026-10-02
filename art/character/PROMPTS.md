# Character reference-set prompts for ChatGPT (M5 Task 2)

**This is Task 2's own small output only** - one of each category, to lock the style and prove the
canonical setup holds across independently generated sheets. It is deliberately not the v1 breadth
(`STYLE.md`'s proposed asset list) - do not generate the full wardrobe from these prompts; Task 3 gets its
own batch once the v1 list is approved.

Read `STYLE.md` in this folder first - it defines the palette, outline/shading, tinted-vs-full-colour layer
rule, and the proportions/bounding-box conventions every prompt below assumes. Attach `reference-guide.png`
(same folder) to both prompts below - it is a plain proportions diagram (grey silhouette, labelled boxes),
never shown as a finished style reference, just so the model can see where each item must land instead of
only reading it in text.

Same sheet workflow as every other building (`docs/kids-games/m4-handover.md`'s "sheet workflow" section
and its "things that went wrong" list apply unchanged here - magenta background, grid mode, chroma-key
pitfalls, one commit per cut sheet): generate, paste the result back, convert/save under `art/character/ai/`,
add a `SHEETS` entry to `tools/art-import/cut-sheets.js`, cut with `--install`, visually verify, commit.

Shared paragraph for both prompts below (stated once here, restated in each prompt so it can be pasted
standalone): "Attached is a proportions reference guide for a children's mobile game character - grey
silhouette and labelled boxes only, not a style example. Style for every item: soft polished 3D-look
children's mobile-game illustration, warm rounded shapes, thin brown outlines, gentle even lighting from
the front, no strong cast shadows, no glow or halo blending into the background, no text, letters or
numbers anywhere, no people other than what's described below. Pose/view for anything body-shaped: dead-on
front view, no tilt, no three-quarter angle, no perspective, matching the attached guide's own proportions
(a figure 3.5 head-heights tall) - draw each item at the scale it would appear on that figure, positioned
within its own cell the same way the guide's matching coloured box shows, not filling the whole cell edge
to edge."

## Prompt 1: boy reference set (7 items) — `character/face_boy_ref`, `character/hairboy_ref_back` /
`_front`, `character/top_boy_ref`, `character/bottom_boy_ref`, `character/shoes_ref`, `character/glasses_ref`

Attach `reference-guide.png`.

"[shared paragraph above] Background: plain solid magenta (#ff00ff) everywhere, including between cells -
not a checkered/transparent placeholder, an actual solid magenta fill. Arrange the 7 items in a grid of 4
columns x 2 rows, evenly spaced with generous plain margin around each one so they can be cut apart
afterwards; leave the 8th (last) cell as plain magenta, empty - only cells 1-7 are used. The 7 items, in
reading order (left to right, top to bottom):

1. A boy's face/head shape alone (matches the guide's green 'Face' box): a simple, friendly, gently rounded
   child's face and head outline seen from the front, a small warm smile, eyes drawn only as soft closed-
   looking gentle almond outlines with no iris colour filled in (the iris is tinted separately after
   import - leave it as a pale unfilled shape), small ears just visible at the sides, short simple hairline
   suggestion only at the very edge (the real hairstyle is separate, drawn next). Colour: a single pale,
   neutral cream/off-white tone all over, not any specific skin colour - this base gets tinted many
   different skin colours after import, so it must read clearly at a pale, neutral tone, not white-as-
   highlight or grey-as-shadow.
2. The same hairstyle's BACK piece (matches the guide's brown 'Hair (back)' box): a short, simple, tidy
   boyish haircut's back-and-sides silhouette, drawn as if peeking out from behind/around the head shape
   above (wider than the face box, per the guide) - no face detail on this piece, just the hair shape.
   Colour: a single pale neutral tone (not a real hair colour - tinted after import, same reasoning as the
   face).
3. The SAME hairstyle's FRONT piece (fringe only, matches the guide's dashed lines inside the face box): a
   small front fringe/hairline band across just the upper portion of the face shape, roughly the guide's
   middle dashed example (about a third of the way down the face) - short and neat, not covering the eyes.
   Same pale neutral tone as item 2 (both pieces of one haircut must clearly match each other).
4. A boy's plain short-sleeved t-shirt (matches the guide's blue 'Top' box), drawn on its own, no body
   inside it - simple, cheerful, a single bold colour with one small friendly graphic on the chest (a star
   or a simple stripe), full real colour (this one is NOT tinted after import - draw its final colour).
5. A pair of boy's shorts (matches the guide's brown 'Bottom' box), drawn on their own, no legs inside them
   - simple, a solid cheerful colour, full real colour (not tinted).
6. A pair of simple sneakers/trainers (matches the guide's red 'Shoes' box), drawn as ONE shoe shape only
   (the same picture is shown unflipped on both feet in-app, so it must NOT have an asymmetric left/right-
   only detail like a single-side buckle or bow - a plain symmetric-reading sneaker silhouette). Full real
   colour (not tinted).
7. A single pair of round, friendly children's-style glasses (matches the guide's green box, drawn over the
   eye line) - simple frames, no lenses tinted or reflective, full real colour (not tinted); this one is
   shared/unisex, used for both the boy and girl sets.

File name: character_boy_ref_sheet.png."

Then: "If your tool can produce a real transparent background instead of magenta, use that."

## Prompt 2: girl reference set (6 items) — `character/face_girl_ref`, `character/hairgirl_ref_back` /
`_front`, `character/top_girl_ref`, `character/bottom_girl_ref`, `character/shoes_girl_ref`

Attach `reference-guide.png`.

"[shared paragraph above] Background: plain solid magenta (#ff00ff) everywhere, including between cells -
not a checkered/transparent placeholder, an actual solid magenta fill. Arrange the 6 items in a grid of 3
columns x 2 rows, evenly spaced with generous plain margin around each one so they can be cut apart
afterwards. The 6 items, in reading order (left to right, top to bottom):

1. A girl's face/head shape alone (matches the guide's green 'Face' box) - same construction as the boy
   face (simple, friendly, gently rounded, small warm smile, soft unfilled almond eye outlines, small ears,
   only a bare hairline suggestion), drawn distinctly feminine only through very soft shape cues (e.g.
   slightly less square a jaw), never through colour or makeup-style detail. Same single pale neutral
   cream/off-white tone as the boy face, for the same tint-after-import reason.
2. A girl's hairstyle's BACK piece (matches the guide's brown 'Hair (back)' box): a simple, tidy shoulder-
   length or ponytail silhouette, peeking out from behind/around the head shape, no face detail on this
   piece. Same pale neutral tone as the boy hair pieces (this is a genuinely different hairstyle to the
   boy's, but the SAME neutral tinting convention).
3. The SAME hairstyle's FRONT piece (fringe only, matches the guide's dashed lines): a small front section
   across the upper portion of the face shape, not covering the eyes, matching item 2's tone exactly.
4. A girl's plain short-sleeved t-shirt (matches the guide's blue 'Top' box), drawn on its own, no body
   inside it - simple, cheerful, a single bold colour with one small friendly graphic (a flower or a simple
   heart), full real colour (not tinted).
5. A pair of girl's shorts or a simple skirt (matches the guide's brown 'Bottom' box), drawn on its own, no
   legs inside it - simple, a solid cheerful colour, full real colour (not tinted).
6. A pair of simple strap sandals or shoes (matches the guide's red 'Shoes' box), drawn as ONE shoe shape
   only (shown unflipped on both feet in-app - no asymmetric left/right-only detail). Full real colour (not
   tinted).

File name: character_girl_ref_sheet.png."

Then: "If your tool can produce a real transparent background instead of magenta, use that."

---

# Task 3: v1 wardrobe batch (46 images, 7 sheets)

**Prepared ahead of schedule while ChatGPT access was unavailable, so it's ready to fire the moment it's
back.** Normally Task 3 waits until the reference set above (Prompts 1-2) is generated *and* visually
approved against `STYLE.md` - that checkpoint hasn't happened yet, only the v1 asset-list numbers have been
approved (see `STYLE.md`'s own v1 section). **Recommended order once ChatGPT is available again: run
Prompts 1-2 first, confirm they match `STYLE.md`, then start these 7.** If a redo of Prompts 1-2 changes the
locked style, re-check any of these 7 already generated against the corrected style before cutting them.

Every prompt below still needs the **shared paragraph from the top of this document** prepended when pasted
into ChatGPT (style, pose/view, proportions) - it isn't repeated per-prompt again here to keep this section
readable; only attach `reference-guide.png` and add the background/grid/reading-order text shown for each.
Same sheet workflow, same magenta/grid/chroma-key cautions as every other building
(`docs/kids-games/m4-handover.md`).

Sprite key convention throughout: `character/<category>_<id>`. Clothing/shoes/glasses ids (Sheets 4-7:
`top_boy_0..3`, `bottom_girl_0..2`, `dress_girl_0..2`, `shoes_boy_0..2`, `glasses_0..2`, etc.) match exactly
what `Rules/DressTheCharacter.cs`'s placeholder catalogue already expects - importing under these names
needs no code change there. Faces and haircuts (Sheets 1-3: `face_boy_<0-2>`, `hairboy_<style>_back`/
`_front`, `hairgirl_<style>_back`/`_front`) are **not** consumed by any code yet - `CharacterLook.Face`/
`HairStyle` are only chosen through `CreatorScreen` today, which still uses the legacy `characters/
char_head_<n>` sprites and placeholder-coloured hair (see `RigFactory`/`CharacterRig`). Wiring these real
ids into `RigFactory`'s face/hair sprite lookup is part of Task 3's own "CreatorScreen rebuild" - importing
the art under these names now doesn't break anything, it just isn't shown yet until that wiring lands.

## Sheet 1: Faces (6) — 3 boy, 3 girl — `character/face_boy_<0-2>`, `character/face_girl_<0-2>`

Attach `reference-guide.png`. "Background: plain solid magenta (#ff00ff) everywhere, including between
cells. Arrange the 6 items in a grid of 3 columns x 2 rows. Every face: pale neutral cream/off-white tone
only (tinted after import - never draw a real skin colour), soft unfilled almond eye outlines, small ears,
only a bare hairline suggestion at the edge (real hair is separate). The 6 faces, in reading order:

1. Boy face A: round cheeks, a big warm open smile.
2. Boy face B: a slightly narrower face, a small closed-mouth smile, a few tiny freckles across the nose.
3. Boy face C: a rounder chin, one small dimple, a cheerful open smile.
4. Girl face A: round cheeks, a soft gentle smile.
5. Girl face B: a slightly heart-shaped face, a small smile, a few tiny freckles.
6. Girl face C: a rounder face, a cheerful dimpled smile.

All 6 at the same scale and detail level, distinguishable from each other but clearly the same 'family' of
character. File name: character_faces_sheet.png."

## Sheet 2: Boy haircuts (6) — 3 styles, back+front pair each — `character/hairboy_<style>_back` / `_front`

Attach `reference-guide.png`. "Background: plain solid magenta everywhere. Arrange in a grid of 3 columns x
2 rows: column order is style 1, 2, 3; top row is each style's BACK piece, bottom row is that same style's
FRONT (fringe) piece - so item 1 and item 4 are the same haircut's two pieces, and so on. Every piece: pale
neutral tone only (tinted after import), no face detail on the back pieces. A back piece is the
hair-and-sides silhouette peeking out around the head (matches the guide's brown box); a front piece is a
narrow fringe band across just the upper face (matches the guide's dashed lines), never covering the eyes.
The 3 boy styles, all short (per the approved v1 list - boys get short-hair variety, not long styles):

1. (top) / 4. (bottom): style 1 - a short, very neat crop, barely any fringe (short all over, no long pieces).
2. (top) / 5. (bottom): style 2 - short with a neat side part and a small side-swept fringe.
3. (top) / 6. (bottom): short and slightly tousled/textured on top, a small fringe standing up a little.

File name: character_hairboy_sheet.png."

## Sheet 3: Girl haircuts (8) — 4 named styles, back+front pair each — `character/hairgirl_<style>_back` / `_front`

Attach `reference-guide.png`. "Background: plain solid magenta everywhere. Arrange in a grid of 4 columns x
2 rows: column order is style 1, 2, 3, 4; top row is each style's BACK piece, bottom row is that same
style's FRONT (fringe) piece - so item 1 and item 5 are the same haircut's two pieces, and so on. Same
pale-neutral-tone/no-face-detail rules as the boy sheet. The 4 girl styles (long, short, ponytail, pigtails
- matching the approved v1 list exactly):

1. (top) / 5. (bottom): 'long' - straight hair reaching past the shoulders, centre-parted, a soft fringe.
2. (top) / 6. (bottom): 'short' - a chin-length bob, small neat fringe.
3. (top) / 7. (bottom): 'ponytail' - hair gathered back into one tail at the back/top of the head, a small
   side-swept fringe on the front piece.
4. (top) / 8. (bottom): 'pigtails' - hair gathered into two bunches, one on each side of the head (draw both
   bunches on the BACK piece so the silhouette reads as pigtails even though the front piece is just the
   fringe band), a straight-across small fringe.

File name: character_hairgirl_sheet.png."

## Sheet 4: T-shirts (8) — 4 boy, 4 girl — `character/top_boy_<0-3>`, `character/top_girl_<0-3>`

Attach `reference-guide.png`. "Background: plain solid magenta everywhere. Arrange in a grid of 4 columns x
2 rows (top row boy, bottom row girl). Every item: a plain short-sleeved t-shirt drawn on its own (no body
inside it), matching the guide's blue 'Top' box in scale/position, full real colour (NOT tinted after
import - draw the final colour). The 8 shirts, in reading order:

1. Boy: solid blue tee with a small yellow star on the chest.
2. Boy: solid green tee with a simple orange stripe across the chest.
3. Boy: solid red tee with a small rocket print.
4. Boy: solid orange tee with a small friendly dinosaur print.
5. Girl: solid pink tee with a small white heart.
6. Girl: solid purple tee with a small flower print.
7. Girl: solid yellow tee with a small rainbow print.
8. Girl: solid teal tee with a small butterfly print.

File name: character_tops_sheet.png."

## Sheet 5: Bottoms (6) — 3 boy, 3 girl — `character/bottom_boy_<0-2>`, `character/bottom_girl_<0-2>`

Attach `reference-guide.png`. "Background: plain solid magenta everywhere. Arrange in a grid of 3 columns x
2 rows (top row boy, bottom row girl). Every item: drawn on its own (no legs inside it), matching the
guide's brown 'Bottom' box in scale/position, full real colour (not tinted). The 6 items:

1. Boy: navy blue shorts.
2. Boy: khaki/tan shorts.
3. Boy: grey jogger-style trousers (elastic ankle cuffs).
4. Girl: a simple denim-blue A-line skirt.
5. Girl: pink shorts.
6. Girl: lavender leggings.

File name: character_bottoms_sheet.png."

## Sheet 6: Dresses (3) + Glasses (3) — `character/dress_girl_<0-2>`, `character/glasses_<0-2>`

Combines two small unrelated sets to save a sheet, same as Science Lab's own precedent for small unrelated
icon sets (`docs/kids-games/m4-handover.md`). Attach `reference-guide.png`. "Background: plain solid magenta
everywhere. Arrange in a grid of 3 columns x 2 rows (top row dresses, bottom row glasses). Dresses: a
one-piece garment drawn on its own (no body inside it), matching the guide's magenta-dashed 'Dress' box in
scale/position (taller than a Top, reaching down toward the knees), full real colour (not tinted). Glasses:
simple frames only, no lenses tinted or reflective, matching the guide's green face-box eye line, full real
colour (not tinted); these 3 are shared/unisex. The 6 items:

1. Yellow sundress with small white polka dots.
2. Light blue dress with a small flower print.
3. Coral/red dress with a plain bow at the waist.
4. Round orange-framed glasses.
5. Rectangular blue-framed glasses.
6. Round pink-framed glasses.

File name: character_dresses_glasses_sheet.png."

## Sheet 7: Shoes (6) — 3 boy, 3 girl — `character/shoes_boy_<0-2>`, `character/shoes_girl_<0-2>`

Attach `reference-guide.png`. "Background: plain solid magenta everywhere. Arrange in a grid of 3 columns x
2 rows (top row boy, bottom row girl). Every item: ONE shoe shape only, drawn on its own, matching the
guide's red 'Shoes' box in scale/position, full real colour (not tinted). **Every shoe must read correctly
either as-is or mirrored** - the same picture is shown unflipped on both feet in-app, so no asymmetric
left/right-only detail (a single-side buckle, an off-centre lace bow, etc.). Boy shoes are sneakers/
trainers; girl shoes are strap sandals. The 6 items:

1. Boy: blue and white sneakers.
2. Boy: red sneakers with white soles.
3. Boy: green sneakers.
4. Girl: pink strap sandals.
5. Girl: white sandals with a small centred flower detail on the strap (centred, not off to one side).
6. Girl: purple strap sandals.

File name: character_shoes_sheet.png."

## Checklist (same spirit as every other building's PROMPTS.md)

- every item reads instantly as what it's meant to be, at a similar visual weight/detail to its neighbours
- the two hair pieces of each set (back + front) are clearly the same haircut/tone, not two different ones
- Face/Hair-back/Hair-front pieces come back pale and neutral, NOT already tinted a specific skin/hair
  colour - if the model picked its own colour anyway, ask for a redo with "plain pale cream, no colour
  choice of your own" made explicit
- wardrobe items (t-shirt, bottom, shoes, glasses) come back in real, final colour - these are correct as
  colourful, unlike the face/hair pieces
- neither Shoes item has an asymmetric left/right-only detail (see STYLE.md's symmetry constraint)
- nothing touching a cell edge or bleeding into a neighbour; the unused 8th cell on the boy sheet is plain
  magenta or otherwise ignorable, not a stray 8th item
- if a piece is wrong, ask ChatGPT for a redo with the same prompt (never hand-edit proportions in code to
  match a mis-scaled asset - the guide is the source of truth, not whatever came back)

Task 3 sheets specifically:

- each haircut sheet's top-row/bottom-row pairing is correct (item N's back piece matches item N's front
  piece in tone AND silhouette family - a redo of one piece alone will desync the pair, redo both together)
- pigtails (girl style 4) actually reads as two bunches, not a single ponytail redrawn
- every shoe in Sheet 7 passes the mirror-test: cover one foot, mentally flip the sprite, check it still
  looks like a normal shoe (see STYLE.md's symmetry constraint - this is the one item type most likely to
  fail it if a "detail" sneaks onto only one side)
- Dress (Sheet 6) reads as one continuous garment reaching toward the knees, not a Top redrawn taller

## (SUPERSEDED by Prompt 3b) Prompt 3: player base body (3 items) — `characters/char_torso`, `characters/char_arm`, `characters/char_leg` (replaces the old mockup)

Attach `reference-guide.png`. The rig draws the arms IN FRONT of the clothes and the torso/arm/leg are tinted with the
chosen skin colour at runtime, so all three must be a single pale neutral tone. Cut with a new `SHEETS` entry (blob mode,
names `torso`, `arm`, `leg`, installed over the old `char_*` files).

"Attached is a proportions reference guide for a children's mobile game character - grey silhouette and labelled boxes only, not a style example. Style: soft polished 3D-look children's mobile-game illustration, warm rounded shapes, thin brown outlines, gentle even lighting from the front, no strong cast shadows, no glow or halo blending into the background, no text, letters or numbers anywhere. Background: plain solid magenta (#ff00ff) everywhere, an actual solid magenta fill, not a checkered/transparent placeholder. Draw 3 separate body parts of one small child, side by side in one row with wide plain magenta gaps so they can be cut apart. Dead-on front view, no tilt, no perspective. Colour for all three: ONE single pale, neutral cream/off-white skin tone (not any specific skin colour - it gets tinted many different skin colours after import, so no pink, no tan, no grey shadows). Bare skin only: no clothes, no underwear, no shoes, no socks, no hair.
1. TORSO: a child's chest and belly as one soft rounded rectangle, about 0.87 times as wide as it is tall, slightly narrower at the waist, rounded shoulders, a short neck stub at the very top (the head is added separately), a flat straight bottom edge (the legs attach there) and NO arms attached - the shoulders end in plain rounded edges.
2. ARM: ONE arm hanging straight down, a slim soft tube about 1 to 3.25 (width to height), with a simple rounded mitten-style hand at the bottom (thumb suggested, no separate fingers), a rounded shoulder end at the top. The same picture is used unflipped for both arms, so keep it plain and symmetric-looking.
3. LEG: ONE leg, straight, about 1 to 2 (width to height), a rounded hip end at the top, a small simple bare foot at the bottom pointing straight down/forward (a short rounded foot shape, no toes detail). The same picture is used for both legs.
Draw the three at the same scale relative to each other as the attached guide's silhouette: torso about 78 by 90, arm about 24 by 78, leg about 34 by 70 (relative units). File name: character_body_base_sheet.png."

Then: "If your tool can produce a real transparent background instead of magenta, use that."

## Prompt 3b: player base body as ONE piece (user, 2026-10-02: the torso/arm/leg parts look horrible; one single body like the cat) — `characters/char_body`, `characters/char_underwear`

Attach `reference-guide.png`. Replaces Prompt 3. The rig then shows ONE body picture (tinted with the chosen skin colour at runtime, so pale neutral tone only) with the head, hair, clothes and shoes still drawn on top as separate pieces; the whole body bobs and squashes instead of swinging limbs. Cut with a new `SHEETS` entry (blob mode, tight, names `body`, `underwear`).

"Attached is a proportions reference guide for a children's mobile game character - grey silhouette and labelled boxes only, not a style example. Style: soft polished 3D-look children's mobile-game illustration, warm rounded shapes, thin brown outlines, gentle even lighting from the front, no strong cast shadows, no glow or halo blending into the background, no text, letters or numbers anywhere. Background: plain solid magenta (#ff00ff) everywhere, an actual solid magenta fill, not a checkered/transparent placeholder. Draw 2 separate pictures side by side with a wide plain magenta gap. 1. BODY: the whole body of one small child as ONE single connected picture, dead-on front view, no tilt, standing straight and relaxed, feet together, arms hanging straight down with a small clear gap between each arm and the torso so clothes can be drawn over them, rounded mitten-style hands (thumb suggested), short neck stub at the top (the head is added separately, do NOT draw a head, face or hair), simple bare feet. Smooth plain cartoon body, no navel, no anatomical detail, nothing below the waist other than plain smooth legs. Colour: ONE single pale, neutral cream/off-white skin tone (not any specific skin colour - it gets tinted many different skin colours after import, so no pink, no tan, no grey shadows). Proportions like the attached guide's silhouette. 2. UNDERWEAR: simple plain white children's briefs, shown flat from the front, about as wide as the body's hips in picture 1, soft rounded shape with a thin waistband, no print, no text. File name: character_body_one_piece_sheet.png."

If your tool can produce a real transparent background instead of magenta, use that.
