# Eva cat-art redo: ChatGPT prompts (replaces the procedural SVG cat)

**Why this exists**: Eva's current look (`art/eva/cat/*.svg`, rendered to `Resources/Art/cat/*.png`) is
code-generated fur-texture geometry (`art/eva/cat/gen.js`, explicitly labelled "SPIKE (throwaway)" in its own
header), not an illustration in the same style as everything else the game shows a child. The user asked for
real ChatGPT-illustrated art instead, matching the house style already established for buildings, wardrobe
items and every object in the game (`art/eva/*/PROMPTS.md`, `art/character/STYLE.md`).

**This does not touch the rig, the animation, or Eva's fixed identity** - only the 11 source images change.
`App/Characters/RigFactory.cs`'s `CreateEva`/`CatMotion` are untouched; the new art drops into the exact
same pivot layout the current SVGs use.

## Eva's fixed identity (unchanged - from `docs/superpowers/plans/2026-09-24-eva-m3-real-cat.md`, "not to be
softened, reinterpreted or stylized")

Every prompt below must produce a cat that is:
- a real-looking, four-legged cat (this front-facing sitting pose shows her two FRONT legs/paws only - her
  hind legs are tucked under her, out of frame, which is normal for a cat sitting facing the viewer, not a
  contradiction of "four-legged");
- **black**;
- **fluffy, approximately midway between a shorthair and a Persian**, with visible fluff around the cheeks
  and chest;
- **bright green (yellow-green) eyes** so the face reads clearly against the black coat (NOT amber - an amber
  draft was corrected by the user);
- **dwarf-cat proportions**, with a **normal, long, fluffy cat tail** (the user reversed the earlier bobtail
  decision after seeing it: the real cat's tail is barely visible and a missing tail looked weird; the tail
  may sway; see the saved memory `eva-character-real-cat`);
- no humanoid anatomy, no humanoid gestures.

**Reconciling "a real cat, not a cartoon cat" (M3's own words) with this game's established rendering style
(soft polished 3D-look, warm rounded shapes, thin brown outlines, gentle shading)**: match the game's
*rendering technique* - the soft shading, warm palette, rounded volumes, thin outline treatment every other
asset uses - but keep her *anatomy and proportions* believably feline and true to the identity above. Not a
chibi/kawaii stylization (no oversized eyes, no exaggerated head-to-body ratio beyond real dwarf-cat
proportions, no simplified triangle-shaped cartoon cat face). Think "a real cat, warmly and softly painted,"
not "a cartoon character that happens to be a cat."

## Likeness photos (recommended for Prompt 0)

The user's own cat is the model. Photos are in `C:UsersscheiDownloadsPozePisica` - attach 2-3 of them to Prompt 0 so
the face, fluff and colouring come out like her (still rendered in the game's soft illustrated style, not
photographic).

## Pose and canvas (measured from the current rig - see `cat-guide.png`)

Front-facing, sitting pose (facing the viewer dead-on, not a side profile), matching the exact proportions
in `cat-guide.png` (attach this to every prompt below): a wide low body/haunches, chest fluff visible in
front, round head sitting above/overlapping the top of the body, two symmetric ears on top, eyes and a small
mouth centred in the face, two front paws visible side by side beneath the chest, the long fluffy tail lying
along the ground and curling out to her right side (the viewer's right), a soft ground shadow beneath her. This is the same pose the
current rig already animates (breathing/idle, ear movement, head tilt, a jaw/body talk cue, a cheer/greet
bounce, the tail essentially static) - **do not change the pose or proportions the guide shows**, only the
rendering style and quality.

## Workflow (different from every other building's PROMPTS.md - read this before generating anything)

Every other `art/eva/*/PROMPTS.md` uses `tools/art-import/cut-sheets.js`, which crops each item to its own
tight square. **That tool is wrong for this one.** A cat part has to land at a *specific rectangle* on a
canvas shared with every other part, or the pieces won't line up into one cat. Use
`tools/art-import/compose-cat-parts.js` instead:

```
node tools/art-import/compose-cat-parts.js <sheet.png> <Name1> <Name2> ... --grid=COLSxROWS --install
```

It chroma-keys the magenta background (same `keyed()` function as `cut-sheets.js`, copied verbatim), finds
each named part's own content within its grid cell, fits (not stretches) it into that part's own measured
box from `cat-boxes.json`, and pastes it onto a full transparent 1000x1000 canvas - `--install` also writes
straight into `Resources/Art/cat/cat_<name>.png` (lowercase-first, matching the existing filenames exactly).
Part names (case-sensitive) are exactly: `Body`, `Chest`, `Head`, `EarL`, `EarR`, `Eyes`, `Mouth`, `LegL`,
`LegR`, `Tail`, `Shadow`. **Visually check every composited part against `cat-guide.png` before installing**
- the fit-and-centre placement is a reasonable first attempt, not guaranteed pixel-perfect for every part;
re-run with a manually cropped/adjusted source image if a part lands looking wrong (too small, oddly
cropped) rather than accepting a bad fit.

Same sheet-generation cautions as every other building (`docs/kids-games/m4-handover.md`'s "things that went
wrong"): explicit solid magenta background, reject fake checkerboard "transparency", watch for a fill colour
too close to magenta (black fur is safely far from magenta, but a light amber/green eye highlight could
theoretically drift close to a keyed-out hue - check the cut eyes specifically), no soft glow/halo.

## Prompt 0: full reference illustration (lock the look before cutting any layer)

No attachment needed (or optionally attach `cat-guide.png` for proportions). "Draw a single, complete
illustration of a house cat for a children's mobile game, sitting and facing the viewer directly (front-on,
not a side profile), on a plain solid magenta (#ff00ff) background - not a checkered/transparent
placeholder, an actual solid magenta fill. The cat: solid black fur, fluffy - visibly longer, softer fur
around the cheeks and chest (midway between a shorthair and a Persian, not a full long-haired coat), bright
green (yellow-green) eyes that read clearly against the black fur, a compact dwarf-cat build (a slightly rounder,
more compact head-to-body proportion than an average cat, without becoming cartoonish or big-eyed), and a
normal long, fluffy cat tail lying along the ground and curling out to her own right side. Two front paws visible together beneath her chest; hind legs tucked under her, not visible. Style:
soft polished 3D-look illustration, warm rounded volumes, thin brown outlines, gentle even lighting from the
front, soft shading that gives her fur volume without looking like a cartoon character - she should read as
a real, convincing cat rendered warmly, not a stylized mascot. No text, no other characters, no background
scenery. File name: eva_cat_reference.png."

Then: "If your tool can produce a real transparent background instead of magenta, use that."

**Check before moving on**: does this actually look like a real cat (per the identity checklist above), not
a cartoon mascot? Does the tail read as a normal long fluffy cat tail? Is the black fur
readable (not just a black silhouette with no visible form/shading)? If any of this is wrong, regenerate
Prompt 0 again before spending effort on the 11-part sheets below - every later prompt is judged against
this reference image for consistency, the same way `art/character/STYLE.md`'s wardrobe sheets are judged
against its own style-lock reference.

## Prompt 1: body group (5 parts) — Body, Chest, LegL, LegR, Shadow

Attach `cat-guide.png` and the Prompt 0 reference image (for style/colour/fur matching). "Attached: a
proportions guide (grey boxes, ignore its colours/labels - just use it for scale and placement) and a
reference illustration of the cat this sheet's parts belong to (match its exact fur colour, style and
rendering quality). Draw a sprite sheet of 5 individual, ISOLATED body parts of that same cat, each on its
own plain solid magenta (#ff00ff) background, arranged in a grid of 3 columns x 2 rows (5 parts, the 6th
cell left blank/plain magenta). Each part drawn alone, as if cut from the reference illustration, at a
consistent scale with the others and with the guide's own box sizes. No text, no other parts of the cat
touching each other within a cell. The 5 parts, in reading order (left to right, top to bottom):

1. Body: the cat's main body/haunches mass alone (no head, no legs, no tail) - the black fluffy body
   silhouette with its own fur shading, roughly matching the guide's brown 'Body' box proportions (wide,
   low).
2. Chest: the fluffier chest-fur patch alone (the visibly longer/softer fur in front of the body, per the
   identity - lighter/fluffier in texture than the body but still black), matching the guide's blue 'Chest'
   box (narrower than Body, centred).
3. LegL: one front paw/lower-leg alone, black fluffy fur, matching the guide's left 'LegL' box (tall and
   narrow).
4. LegR: the matching front paw/lower-leg for the other side, same style, matching the guide's 'LegR' box
   immediately beside LegL - the two paws should look like a natural pair, not two different legs.
5. Shadow: a simple soft dark ground shadow ellipse alone (not part of the cat's body - a plain soft-edged
   dark blob, matching the guide's wide flat 'Shadow' box).

File name: eva_cat_body_sheet.png."

Then: "If your tool can produce a real transparent background instead of magenta, use that."

## Prompt 2: head group (6 parts) — Head, EarL, EarR, Eyes, Mouth, Tail

Attach `cat-guide.png` and the Prompt 0 reference image. "Attached: a proportions guide (grey boxes, ignore
colours/labels) and a reference illustration of the cat this sheet's parts belong to (match its exact fur
colour, style and rendering quality). Draw a sprite sheet of 6 individual, ISOLATED parts of that same cat,
each on its own plain solid magenta (#ff00ff) background, arranged in a grid of 3 columns x 2 rows. Each
part alone, as if cut from the reference illustration, at a consistent scale with the others and with the
guide's own box sizes. No text. The 6 parts, in reading order:

1. Head: the cat's head/face shape alone (no ears, no eyes, no mouth drawn on it - those are separate
   pieces below, drawn to layer on top of this one), round dwarf-cat proportions, black fluffy fur with
   cheek fluff, matching the guide's green 'Head' box.
2. EarL: one ear alone (the cat's own left ear, on the viewer's left), small and rounded, matching the
   guide's 'EarL' box.
3. EarR: the matching right ear (viewer's right), a mirrored pair with EarL, matching the guide's 'EarR'
   box.
4. Eyes: just the pair of bright green (yellow-green) eyes alone (both eyes together in one piece, no other face
   detail), matching the guide's black 'Eyes' box (a wide short band).
5. Mouth: just the small mouth/muzzle detail alone, matching the guide's red 'Mouth' box (small, centred).
6. Tail: the long, fluffy cat tail alone, lying low and curling out to the right, matching the guide's 'Tail'
   box position (lower right; a wide, low box).

File name: eva_cat_head_sheet.png."

Then: "If your tool can produce a real transparent background instead of magenta, use that."

## Checklist

- Prompt 0's reference is approved (reads as a real, warmly-rendered cat, not a cartoon mascot) before
  either of the two part sheets is even generated - regenerate Prompt 0 first if it isn't right yet.
- both sheets' parts visually match Prompt 0's fur colour/style/lighting, not just their own cell in
  isolation - a part that looks like a different cat needs a redo, not a colour-correct in code.
- EarL/EarR read as a natural mirrored pair, not two different ear shapes; same for LegL/LegR.
- the Tail is a normal long fluffy cat tail (not a short bobtail) and fits the wide, low Tail box.
- Eyes read as bright green against black fur, not lost in the silhouette.
- black fur has visible form/shading (fluff, volume) rather than reading as a flat black silhouette.
- after compositing with `compose-cat-parts.js`, open each `out_<Name>.png` and sanity-check it against
  `cat-guide.png`'s box for that part before `--install`-ing (see the Workflow section above).
- commit each sheet + its composited, installed parts together, same "one commit per batch, pushed
  immediately" discipline as every other building.
