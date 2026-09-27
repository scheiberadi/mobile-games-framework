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
3. **Wardrobe breadth** (ceiling counts, an ongoing backlog target, **not a requirement for
   declaring this milestone successful** — see "Production reality" below and the plan's Task 8):
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
7. **The character appears in every gameplay screen, no exceptions, as a fixed product
   requirement** — both characters (player on the left, Eva on the right) are part of the game's
   presentation and identity and are never removed from a screen just because it's crowded.
   **What changes from the first draft of this plan**: they must be small at gameplay scale (not
   today's ~500-560-unit hub-preview size), built as a shared presentation component so a screen
   gets them by construction rather than through a mandatory bespoke per-screen retrofit, and they
   must never obscure content, overlap a `TapTarget`, steal input, or reduce the usable learning
   area. See "Screen integration" below — rewritten after review to fix this shape, since the first
   draft under-specified size and over-specified "retrofit ~120 files" as the plan of record.
8. **Eva's rework is real but deliberately minimal and sequenced last.** Establish the player
   character's new quality bar first (Tasks 1-6/7 of the plan); only then evaluate Eva beside it
   and make the *minimum* motion/art changes needed for the two to feel coherent together — not an
   open-ended "richer motion + full polish + new states" project in its own right. Her fixed
   identity (`docs/superpowers/plans/2026-09-24-eva-m3-real-cat.md` — black, fluffy,
   dwarf-proportioned, bobtail) is unaffected either way.
9. **Plan shape**: one phased milestone plan with exactly **two hard gates** before the two
   riskiest spends — real wardrobe-art generation, and the ~120-screen integration sweep — same
   pattern as `docs/superpowers/plans/2026-09-24-eva-m3-real-cat.md`'s spike-then-approve gate for
   the real-cat rework. A third checkpoint (the visual style lock, point 10) is a review moment but
   not declared a third hard gate, since the user asked to keep exactly two.
10. **A visual style lock precedes the large wardrobe volume.** Before generating v1-breadth (let
    alone ceiling-breadth) wardrobe art, produce one small approved reference set — a boy's face,
    hair, shirt, bottom, and shoes, and the same for a girl, plus one glasses style — and write down
    the palette, proportions, outline/shading treatment, and layer conventions those references
    establish. Every wardrobe sheet generated afterward (v1 and every later expansion batch) is
    judged against this reference set, not against "does this look nice in isolation," to prevent
    the kind of stylistic drift that's easy to miss one ChatGPT sheet at a time.
11. **`CreatorScreen`'s rebuild must work for a 4-5-year-old non-reader.** 9+ categories cannot
    become a dense list of labelled rows. Large visual category navigation, a small number of large
    choices visible at once, no required text labels — the icons themselves and the live character
    preview do the communicating, same principle the rest of this app already follows
    (`docs/kids-games/game-modes-backlog.md`'s "no reading anywhere" constraint applies to the
    creator screen exactly as much as any game screen).

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

**Save compatibility is a real breaking change, not a footnote.** `SaveStore.Load()`
(`App/Save/SaveStore.cs`) deserializes `PlayerProgress` (and its `Look` field) with `JsonUtility`,
which silently leaves any field absent from the saved JSON at its C# default — it does not error,
warn, or flag anything. Today's `CharacterLook` (`Head`/`Skin`/`Shirt`, three ints) deserializing
into tomorrow's much larger `CharacterLook` would silently produce a "valid-looking" but wrong
character (`Gender` defaulting to whichever enum value is `0`, every new field zeroed) instead of
failing loudly or migrating correctly — exactly the silent-default failure mode to avoid.
`PlayerProgress.Version` already exists as a field (`Rules/Progress.cs:23`) but `SaveStore.Load()`
never actually branches on it today — it's unused for its intended purpose. There's already an
in-place migration precedent to follow in the same method (the "lamp was replaced by the toy chest"
item-id remap, and the `bedroom_` to `kids_` slot-id rename), so this isn't a new pattern for the
codebase, just one that needs to actually apply to `Look` this time. See the plan's Task 1 for the
concrete migrate-or-invalidate decision and its required test.

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
never blocking the mechanism on having every asset in hand first. **The ceiling counts in point 3
are a backlog target, not a completion requirement for this milestone**: M5 is done when the
system genuinely works end-to-end with v1 breadth, not when every category reaches 10-20 items —
see the plan's Task 8 and its acceptance criteria.

## Screen integration ("Eva right, character left, everywhere")

**This is a fixed product requirement, not an optional nice-to-have.** Both characters appear on
every gameplay screen, always — they're part of the game's presentation and identity, and a
crowded screen is never a reason to drop them. What was wrong with the first draft of this plan
was the *shape* of how to get there, not whether to do it: it treated the companion pairing as a
single fixed size (today's ~500-560px hub-preview height) and treated the rollout as a presumed
120-file bespoke retrofit. Both are corrected here.

**Size**: the character pairing is never rendered at hub-preview scale inside a mini-game. Each
gameplay screen reserves a small, deliberately modest amount of chrome for the two characters —
small enough that it never competes with the learning interaction for space, never overlaps a
`TapTarget`, never sits between the child's finger and anything they need to tap, and never shrinks
the usable play area below what the game actually needs. `CreatorScreen`'s own layout comments show
how tightly audited this codebase's screens already are: the production canvas is exactly 900 units
tall (`y in [-450, 450]` is the *real* on-device frame, not a guide), every `TapTarget` rect must
fit inside it, and `docs/superpowers/plans/2026-09-24-eva-m3-real-cat.md`'s own risk list notes some
screens (six answer tiles at the counting screen's higher levels) already have "little slack" —
that screen is exactly why "small" has to mean genuinely small, sized against the tightest real
screen, not the roomiest one.

**Shared, not bespoke-per-screen.** Build the pairing as a shared presentation component (extending
the existing `Hud`/`Navigator` chrome mechanism, which already toggles per-screen visibility of
elements like the Home button) with **2-3 standard gameplay-scale layouts** — e.g. a default corner
pairing that fits the large majority of screens unchanged, plus one or two alternate arrangements
for screen shapes that don't suit the default (a screen that's already full-bleed edge-to-edge, for
instance). A screen gets the pairing by using the shared component and, where relevant, picking
which of the 2-3 standard layouts fits it — not by every screen author hand-rolling its own
position and size. **New minigames inherit this automatically** by building on the shared component
from day one; **existing screens are only individually touched where the standard layout genuinely
conflicts** with that screen's own UI, not as a blanket 120-file work item assumed up front.

The character-everywhere spike (plan Task 6, one of the two hard gates) is where the
2-3 standard sizes/positions actually get decided and proven, against a deliberately varied set of
real screens — not asserted here. See the plan for which screens it must cover and what it has to
demonstrate before the full rollout is approved.

## Deferred out of this milestone

Body-type/height customization, accessories beyond glasses (hats, jewellery, backpacks), seasonal
or event-limited wardrobe items, a market/currency-gated unlock system for wardrobe pieces (today's
Store/`Owned` coin-purchase model is furniture-only; whether wardrobe items are free-picks-in-Creator
vs. earned-like-furniture is a Task 1 decision but full monetization-shaped unlock mechanics are not
in scope), multiplayer/sharing looks, and any change to Eva's fixed identity (`docs/superpowers/
plans/2026-09-24-eva-m3-real-cat.md`'s "Eva requirement" — black, fluffy, dwarf-proportioned,
bobtail — stays fixed; only her motion/polish/reactions change).
