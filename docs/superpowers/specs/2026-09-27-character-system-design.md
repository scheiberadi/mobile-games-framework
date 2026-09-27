# Character system design: real player customization, a working dress-up loop, and the character everywhere

**For review only. Nothing here is executed until the user approves the paired plan**
(`docs/superpowers/plans/2026-09-27-m5-character-system.md`). Written from a live design
conversation on 2026-09-27, right after M4's Science Lab batch 13 landed (Science Lab is 13/17
content batches in; the other five M4.6-4.10 buildings' gameplay art, all 122 menu-tile icons, and
all building+road art are still entirely unstarted — see `docs/kids-games/m4-handover.md`).

## Why now

The current player character (`Resources/Art/characters/char_*.png`, driven by `CreatorScreen` /
`RigFactory.CreatePlayer` / `CharacterRig.ApplyLook`) is flat placeholder art: a beige circle with
two dots and a curved line for a face, a plain rounded rectangle for a torso, and legs tinted the
same colour as the arms — so today's character has no hair, no pants, and no real face. It was
never revisited after M0/M1 stood up the rig mechanics. Everything else in the game (Eva, Science
Lab, Zoo & Farm, Playground) has since moved to real AI-illustrated art through the ChatGPT-sheet
pipeline (`docs/kids-games/m4-handover.md`'s "sheet workflow"); the player character is now the
worst-looking thing in the app by a wide margin, and the user wants to fix that properly rather than
patch it.

## What exists today (verified against the code, not assumed)

- **`CreatorScreen`** (first-run only, `Progress.HasCharacter == false`): picks a head (4 options,
  same flat art for every kid), a skin tint (5 swatches), a shirt tint (5 swatches). Confirm saves
  `Progress.Look` and routes to Map. Solid UI mechanics (live preview, selection rings, a bounce on
  every pick) — worth keeping, not worth discarding.
- **`RigFactory.CreatePlayer`**: a 6-part uGUI cutout (Head, Torso, ArmL, ArmR, LegL, LegR) on an
  `Animator`-driven skeleton (`Anim/Rig` controller: Wave, Cheer, Talking, idle). `ApplyLook` swaps
  the head sprite and tints torso (shirt colour) and every other part (arms/legs/head) with the same
  skin colour — legs currently render as bare skin-coloured limbs, i.e. no pants exist at all today.
- **Eva** (`RigFactory.CreateEva`): a *separate*, non-customizable, real-illustrated cat
  (`art/eva/cat/*.png`, layered Body/Head/Ears/Eyes/Mouth/Tail/Legs), driven by code (`CatMotion`),
  not the `Animator`. Genuinely good art already — fur texture, shading, the works. This is the
  target quality bar for the player character, and confirms there's no leftover `.svg` pipeline to
  worry about (`grep` for `*.svg` across the whole Unity project returns nothing) — Eva's own
  "reads a bit stiff/procedural" motion is a real, separate finding, addressed below.
- **`DressTheCharacterScreen`/`Rules/DressTheCharacter.cs`** (Store, a *replayable mini-game*, not
  character creation): drag-and-drop onto **four abstract labelled slot icons** (Head/Top/Bottom/
  Feet) with a fixed catalogue of made-up placeholder item ids (`cap`, `hat`, `beanie`, `sunhat`,
  ...). **Important finding: nothing is drawn on a body today.** Eva stands nearby (`RigFactory.
  CreateEva`) cheering the child on — she is decoration, never the one being dressed, and nothing
  needs to change about that role. The "outfit" is only ever four floating icons snapping into slot
  outlines; there is no character visualization to redirect onto the player rig, because there is no
  character visualization at all yet. This game needs a real preview rig added, not swapped.
- **Old backlog idea, explicitly superseded**: `docs/kids-games/game-modes-backlog.md:173` still
  lists *"Character creator (animal head, gender, colour, clothes; shared human body)"* — an
  anthro-animal-head option. **Dropped per this session's decision**: boy/girl only, no animal
  heads. Update that backlog line when this plan lands.

## Decisions made this session (treat as fixed; only the user changes these)

1. **Gender replaces the old animal-head idea.** Character creation starts with Boy / Girl.
2. **10 face types per gender** (20 illustrated face shapes total). Skin tone stays a *separate*
   tintable dimension layered under the face shape (today's 5-swatch `Palette.Skin`, kept) —
   otherwise the count balloons from 20 faces to 100 (20 x 5 fully-painted skin variants). Flagged
   as an assumption below, not asked as a blocking question, per the user's own steer to write the
   plan and let them correct it on review rather than round-trip more chat questions first.
3. **Wardrobe breadth** (ceiling counts, not necessarily the day-one shipped count — see "Production
   reality" below for why):
   - **Boys**: 20 t-shirts, 5-10 pants, 10 shoes, 10 haircuts, eye colour choice, hair colour
     choice, 10 glasses types.
   - **Girls**: 20 t-shirts, 20 pants-or-skirts, 20 dresses, 10 shoes, 20 haircuts, eye colour
     choice, hair colour choice, 10 glasses types.
4. **The character reacts with joy to every item change** — a visible, delighted reaction on
   every pick, not just the existing subtle "hop" scale-punch.
5. **A "randomize" button** rolls a full valid look in one tap (one pick per category, respecting
   the Bottom-vs-Dress exclusivity from the data model) — added after the design review below
   flagged it was missing; not part of the original brainstorm but a natural fit for a 9+-category
   picker aimed at young kids who won't want to tap through every row by hand. Triggers the same
   joy reaction as a manual pick (point 4) — arguably the moment that reaction matters most, since
   the whole point is the character visibly delighted by its own new random look.
6. **Dress the Character gets rebuilt to dress the child's own boy/girl character** (not an
   abstract slot row), and **at the end of a round asks "keep this look?"** — accepting writes the
   assembled outfit into `Progress.Look` as the character's persistent real appearance, same
   contract as `CreatorScreen.Confirm()`.
7. **The character appears in every gameplay screen, no exceptions** — Eva on the right, the
   player's character on the left, flanking the play area, across all ~120+ mini-games plus the hub
   screens. This is the single largest and riskiest piece of this plan (see "Screen integration"
   below) and is scoped as its own gated phase.
8. **Eva gets a parallel, lower-priority rework**: richer motion (today's `CatMotion` is code-driven
   transforms on static illustrated pieces — more lifelike movement is wanted, not necessarily
   frame-by-frame drawn animation, which is a spike question, not a fixed requirement), more art
   polish, and new animation states including reacting to the player character's outfit changes.
   Confirmed explicitly lower priority than the player character and dress-up work.
9. **Plan shape**: one phased milestone plan, spike-gated before the big art-generation push starts
   — same pattern as `docs/superpowers/plans/2026-09-24-eva-m3-real-cat.md`'s spike-then-approve
   gate for the real-cat rework.

## Assumptions this spec makes (confirm-or-correct on plan review, not a blocking question)

- Skin tone stays tint-based (5 swatches) and independent of the 10 face shapes per gender (point 2
  above) — keeps face-art volume at 20, not 100.
- Hair colour and eye colour are each a small tint palette (existing precedent: 5 skin swatches, 5
  shirt swatches) rather than fully painted per-colour hair/eye art — propose 6-8 hair colours and
  6-8 eye colours; exact counts are a Task 1 (data model) decision, not a design blocker.
- Glasses (10 types) are a shared catalogue across both genders unless the user says otherwise —
  glasses shapes aren't inherently gendered the way clothing is.
- T-shirts are gender-specific art (20 for boys, a separate 20 for girls), matching how the user
  listed them as two independent lists, not one shared 20-item catalogue worn by both. If the intent
  was actually a shared print/pattern catalogue re-cut per gender's body shape, that roughly halves
  the shirt art volume — worth the user confirming, since it's a meaningful production-cost lever.
- A "Bottom" outfit slot is Pants *or* Skirt (mutually exclusive, one choice); a Dress instead
  *replaces* both Top and Bottom at once as a single one-piece garment. This is a real rig question
  (see "Data model and rig" below), not just a UI grouping detail.

## Data model and rig (technical shape, decided at Task 1, sketched here for the plan's sizing)

`CharacterLook` grows from 3 fields (`Head`, `Skin`, `Shirt`) to roughly: `Gender`, `Face`, `Skin`,
`HairStyle`, `HairColor`, `EyeColor`, `Top` (nullable — absent when `Dress` is set), `Bottom`
(nullable, Pants-or-Skirt, absent when `Dress` is set), `Dress` (nullable, girls only), `Shoes`,
`Glasses` (nullable — "none" is a valid, default choice).

The rig gains real layers instead of 6 flat tinted parts: Face (tinted by Skin) with baked
expression, Hair (separate layer, tinted by HairColor, swapped by HairStyle, drawn behind/around the
face per style), Eyes (tinted by EyeColor, likely baked into the Face art with a tintable iris region
rather than a fully separate movable layer — a Task 1 call), Glasses (optional overlay layer), Top,
Bottom, Shoes as separate swappable (not tinted — see "shared vs. separate art" below) layers, and a
Dress mode that swaps Top+Bottom's rendering for one combined garment layer instead of two.

**Shared vs. separate art per clothing item**: unlike skin/shirt colour today, individual t-shirts,
pants, dresses etc. are not simple tint swaps of one base shape — a striped shirt and a shirt with a
cartoon star print are genuinely different illustrations. Each of the ~100+ clothing items needs its
own piece of art (batched into ChatGPT sheets the same way Science Lab's 512x512 items are, not
generated one-by-one), not a tint-multiply trick. This is the main driver of the art volume called
out below.

## Production reality (the part a design conversation glosses over and a plan can't)

Tallying the ceiling counts in point 3 above: 20 faces + roughly 30 haircuts (10 boy + 20 girl) +
roughly 108 clothing pieces (20+8+10 boy, 20+20+20+10 girl, using the low end of the 5-10 boys'-pants
range) + 10 glasses is **on the order of 170-180 individual illustrated assets**, before hair/eye
colour swatches (cheap, tint-based) are even counted. At the batching rate this session actually
achieved for Science Lab (6-9 items per ChatGPT sheet, one sheet per approval round, occasionally
needing a corrective re-do sheet as batch 10's light bulb and balloon did), that is **20-30+ sheets**
just for the character wardrobe — on top of Science Lab's own remaining 4 batches, five entirely
unstarted buildings' gameplay art, all 122 menu-tile icons, and all building+road art still owed to
M4. This session also hit Adrian's ChatGPT plan limit mid-session generating a fraction of this.

This isn't a reason not to do it — it's the reason the plan (not this spec) proposes shipping a
**small v1 wardrobe first** (a handful of items per category, enough for the creation screen, the
dress-up loop, and the character-everywhere integration to all be real and working end-to-end) and
then **filling in breadth as ongoing content batches**, exactly the way Science Lab's 17-batch plan
is already being executed — batch by batch, each one committed and pushed the moment it's cut,
never blocking the mechanism on having every asset in hand first.

## Screen integration ("Eva right, character left, everywhere")

This is an engineering risk, not an art one. `CreatorScreen`'s own layout comments show how tightly
audited this codebase's screens already are: the production canvas is exactly 900 units tall
(`y in [-450, 450]` is the *real* on-device frame, not a guide), every `TapTarget` rect must fit
inside it, and there's an established (if manual, not an automated iterate-all-140-screens test)
20-unit overlap tolerance individual screens' comments cite. `docs/superpowers/plans/
2026-09-24-eva-m3-real-cat.md`'s own risk list notes some screens (six answer tiles at the
counting screen's higher levels) already have "little slack." Bolting a ~500-560px-tall character
rig (today's `RigFactory` preview/Eva heights) onto the left and right edges of ~120 already-tuned
screens, unmodified, risks eating into play areas that don't have that space to give.

Proposed approach (a Task 1/spike question, not decided here): extend the existing shared "chrome"
mechanism (`Hud`/`Navigator`, which already toggles per-screen visibility of shared elements like
the Home button) with a **companion strip** — sized deliberately smaller than today's full preview
height, docked at fixed screen edges outside each screen's audited play area, not overlapping any
existing `TapTarget`. Whether that's a small idle-loop portrait, a corner silhouette, or something
else worth showing at full character height only on hub/result screens is exactly what Task 1's
spike should settle by testing it against a few real, already-cramped screens (the six-tile counting
screen is the natural stress test) before committing to a shape that has to be retrofitted across
~120 files.

## Deferred out of this milestone

Body-type/height customization, accessories beyond glasses (hats, jewellery, backpacks), seasonal
or event-limited wardrobe items, a market/currency-gated unlock system for wardrobe pieces (today's
Store/`Owned` coin-purchase model is furniture-only; whether wardrobe items are free-picks-in-Creator
vs. earned-like-furniture is a Task 1 decision but full monetization-shaped unlock mechanics are not
in scope), multiplayer/sharing looks, and any change to Eva's fixed identity (`docs/superpowers/
plans/2026-09-24-eva-m3-real-cat.md`'s "Eva requirement" — black, fluffy, dwarf-proportioned,
bobtail — stays fixed; only her motion/polish/reactions change).
