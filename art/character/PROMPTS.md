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
