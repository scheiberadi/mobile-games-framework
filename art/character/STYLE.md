# M5 character system: visual style lock

**Task 2 of `docs/superpowers/plans/2026-09-27-m5-character-system.md`.** This is the canonical setup every
wardrobe/hair/face sheet is judged against from now on - both the small reference set this task generates
and every later expansion batch (Task 3's v1 list, Task 8's ongoing breadth). A wrongly-posed or wrongly-
scaled item breaks the rig even if its colours are perfect, so **canonical-setup compatibility matters more
than palette consistency** when checking a sheet against this doc.

Attach `reference-guide.png` (same folder, regenerate with `node tools/art-import/character-guide.js` if
`RigFactory.cs`'s constants ever change - see that script's own header comment) to every prompt in
`PROMPTS.md`. It is a proportions diagram only, not a style example - it shows a plain grey placeholder
silhouette with labelled boxes, never the finished art style below.

## Look

- **Palette**: matches every other building's own style guide already (`art/eva/*/PROMPTS.md`) - soft,
  warm, saturated-but-not-neon colours, nothing that reads as scary or garish for a 5-8-year-old.
  Skin/Hair/Eye tint ranges are already fixed by code (`App/Characters/Palette.cs`): 5 skin swatches
  (unchanged from M1), 6 hair colours, 6 eye colours (both locked at Task 1/Gate 1) - wardrobe item
  colours (a t-shirt's print, a shoe's colour) are independent of these and free to vary sheet to sheet,
  the same way Science Lab's object colours vary freely from its own building-wide palette.
- **Outline/shading**: soft polished 3D-look children's mobile-game illustration, warm rounded shapes,
  thin brown outlines, gentle even lighting from the front, no strong cast shadows, no glow/halo blending
  into the background (see `docs/kids-games/m4-handover.md`'s finding #6 - the same pitfall applies here).
  Identical language to every existing `art/eva/*/PROMPTS.md` on purpose: the player character must not
  look like it was illustrated by a different artist than the world around it.
- **Layer conventions (what's baked into an item's own art vs. tinted at runtime)**, fixed at Task 1 and
  restated here since it directly changes how each category is *prompted*:
  - **Tinted at runtime, drawn as neutral/pale art**: Face (`characters/char_head_<n>`, tinted by Skin),
    the torso/arm/leg base shapes (unchanged, already neutral), Hair - both back and front pieces
    (tinted by HairColor), the Eye iris band (tinted by EyeColor). Prompts for these must ask for a pale,
    neutral/off-white base tone, similar to how the existing `char_torso`/`char_arm`/`char_leg` placeholder
    art already works - never ask ChatGPT to pick a "final" skin or hair colour itself.
  - **Full colour, never tinted**: every wardrobe item - Top, Bottom, Dress, Shoes, Glasses. A striped
    shirt and a shirt with a star print are genuinely different illustrations (per the design spec), so
    these are prompted with their real, final colours and pattern, like any other object sprite in this
    game (Science Lab's objects, Zoo & Farm's animals).
- **Symmetry constraint (a real rig limitation, not a style choice)**: `RigFactory` uses the exact same
  unflipped sprite for both left and right legs, and the same unflipped sprite for both left and right
  arms (`characters/char_leg`/`char_arm` today; the same convention continues for Shoes - one Shoes image
  is shown on both feet, unmirrored). **Never design a Shoes item with an asymmetric left/right detail**
  (a bow only on one side, a buckle that only makes sense on a right foot) - it will look wrong on
  whichever foot doesn't match. A shoe silhouette that reads correctly either unflipped or mirrored (most
  sneakers/sandals/boots, viewed dead-on) is what to ask for.

## Canonical setup

- **Pose**: a plain, symmetric standing pose, facing the camera dead-on. Arms hang straight down but held
  slightly away from the body (a visible gap at the torso, not touching it - the rig's arms are a separate
  moving part). Legs together, straight, no bend. No tilt, no three-quarter or side angle, no perspective -
  a flat, dead-front orthographic view, matching how every other in-game sprite in this project is drawn.
- **Proportions**: the implied reference figure is **3.5 head-heights tall** (`RigFactory.CanonicalHeight`
  / `HeadSize` = 224/64 = 3.5 exactly - not a rounded approximation). Shoulders sit about 1.03 head-widths
  apart centre-to-centre (`ShoulderX*2/HeadSize`); the torso is about 1.22 head-widths wide and 1.4
  head-heights tall; legs (ground to hip) are about 1.1 head-heights; arms are about 1.2 head-heights long.
  **No prompt needs to restate these numbers** - `reference-guide.png`'s silhouette already encodes them
  visually; just keep every item's own scale consistent with what the guide shows.
- **Canvas**: each item is generated on its own cell of a sheet (grid mode almost always applies here -
  clothing/hair pieces are rarely simple isolated icons, per the plan's own note), same pipeline as every
  other building (`tools/art-import/cut-sheets.js`, 512x512 per cut sprite is the project standard). Within
  its own cell, an item is drawn at the scale it would appear on the reference figure above - e.g. a
  t-shirt fills roughly the vertical band the guide's blue "Top" box shows, not the whole cell edge to
  edge - with generous plain magenta margin on every side for clean cutting, exactly like every existing
  `art/eva/*/PROMPTS.md` sheet.
- **Anchor/attachment points per slot** (where RigFactory actually places each rect - see
  `docs/superpowers/spikes/character-rig.md` for the full occlusion-case reasoning): Top and Dress are
  anchored so they reach slightly past the torso's own width toward the shoulders (proving they can cover
  the arm attachment seam); Bottom sits at the hip, reaching a little above the leg tops; Shoes overlap the
  bottom ~35% of the leg; Hair-back is centred on the head, about 1.25x its width/height so it peeks out
  around the face silhouette; Hair-front is a band across the *upper* portion of the face box, its
  coverage varying by hairstyle (see `reference-guide.png`'s three dashed example lines); Glasses and the
  Eye iris band both sit within the face box too (Glasses over everything, per the resolved occlusion
  order). None of this changes what to draw - it changes where an item's own "weight"/silhouette should
  sit within its cell so it reads correctly once the rig positions it.
- **Bounding-box rules per slot** (as a fraction of the reference figure's own total height, ground to top
  of head = 100% - also see the coloured boxes in `reference-guide.png`):

  | Slot | Vertical span (approx.) | Notes |
  |---|---|---|
  | Face | top 71%-100% | a rounded head/face shape, no hair drawn on it - hair is separate layers |
  | Hair (back) | 71%-107% | taller/wider than Face by design - peeks out past the face silhouette |
  | Hair (front) | within the Face band, upper 12%-60% of it depending on style | a band, not a full cap |
  | Glasses | within the Face band, centred on the eye line (~32%-52% up the face) | |
  | Top | ~29%-74% | reaches past the torso's own sides toward the shoulders |
  | Dress | ~13%-71% | replaces Top+Bottom's whole span at once, never drawn alongside either |
  | Bottom | ~31%-53% | hip band, a little above the leg tops |
  | Shoes | 0%-11% | bottom band of the leg only |

## Explicit v1 asset list (proposed - review before Task 3 generates it)

Not part of this task's own small output (below) - this is what Task 3 generates next, once approved.
Counts are illustrated-asset counts; Skin/HairColor/EyeColor are tints already implemented (5/6/6 swatches,
zero extra art). "Pieces" for a haircut means the back+front pair `reference-guide.png` shows as two
separate boxes - both are needed per style, not one image split in two after the fact.

| Category | Boy | Girl | Shared | Images |
|---|---|---|---|---|
| Faces | 3 | 3 | - | 6 |
| Haircuts (back+front pair each) | - | - | 3 styles x 2 pieces | 6 |
| T-shirts | 4 | 4 | - | 8 |
| Bottoms (pants/shorts \| pants/skirt) | 3 | 3 | - | 6 |
| Dresses | - | 3 | - | 3 |
| Shoes | 3 | 3 | - | 6 |
| Glasses | - | - | 3 | 3 |
| **Total** | | | | **38** |

Flagged assumptions (confirm-or-correct, same spirit as the design spec's own open assumptions):

- **Haircuts shared across both genders for v1** (not gendered yet, unlike the ceiling list's eventual
  10 boy / 20 girl split) - cheaper for proving the mechanism out, since a style genuinely can suit either
  gender at this small a count. Happy to split into boy-only/girl-only sets instead if preferred.
- **Shoes gendered (3 boy + 3 girl), matching the ceiling list's own 10-boy/10-girl split** - the Task 2
  reference set already generates one boy style (sneakers) and one distinct girl style (sandals) on this
  assumption; flagging in case a shared/unisex shoe catalogue was actually intended instead (cheaper: 3
  images instead of 6).
- **Glasses shared/unisex** - per the spec's own note that "glasses shapes aren't inherently gendered the
  way clothing is." The Task 2 reference set generates one shared glasses style on this assumption.
  38 images is roughly 5 sheets at the project's own established ~6-8-items-per-sheet rate for richer
  content (per `docs/kids-games/m4-handover.md`) - a similar size to Science Lab's own batches.

## What this task actually generates now

A small, real, illustrated reference set - one of each category, not the v1 breadth above - to nail the
look and prove the canonical setup holds across independently generated sheets before committing to volume.
See `PROMPTS.md` in this folder for the two ready-to-paste prompts (boy set, girl set).
