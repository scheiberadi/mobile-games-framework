# M5 plan: real player character, a working dress-up loop, character everywhere, Eva polish

**For review only. Nothing in this plan is executed until the user approves it.** Design context
and the decisions this plan is built on: `docs/superpowers/specs/2026-09-27-character-system-design.md`
— read that first, this doc is the task breakdown only. M4 (`claude/eva-m4-full-content`) is still
open (Science Lab 13/17 batches, five buildings' gameplay art and all menu-tile/road art entirely
unstarted); this plan does not close M4 or compete with it for a branch — see "Sequencing" below.

**Exactly two hard gates**, per the user's explicit instruction after reviewing the first draft:
approve the data-model/rig spike (Task 1) before any real wardrobe art is generated, and approve
the character-everywhere presentation spike (Task 6) before the broad screen-integration rollout.
The visual style lock (Task 2) is a review checkpoint the user naturally sees as sheets come back,
not a third formal hard-stop gate.

## Goal

Replace the flat placeholder player character with a real boy/girl character system (10 face types
per gender, hair, eyes, clothing, glasses), make Dress the Character actually dress that character
and offer to keep the look, put the character next to Eva on every gameplay screen at a small
gameplay-appropriate size, and give Eva's motion/art the minimum coherence pass needed once the
player character sets the new quality bar — in that order, gated by the two approvals above.

## Global constraints (unchanged from M1-M4)

- No reading anywhere; voice-first. No ads, IAP, accounts, analytics, server, runtime AI.
  Deterministic rewards only.
- Landscape design frame; tap targets >= `EvaUi.MinTap` (240) everywhere, including every new
  wardrobe/creator button.
- Art: the established ChatGPT-sheet pipeline (magenta background, `tools/art-import/cut-sheets.js`,
  one commit per cut sheet, pushed immediately) — not a new pipeline, not SVGs, not runtime AI
  generation.
- This is a cloud-container session: no Unity, no on-device test/build here (per
  `docs/kids-games/m4-handover.md`). Every "on-device check" below is Adrian's own machine.
  Editmode tests (`bash tools/run-editmode-tests.sh ".../EvasLearningWorld"`) are likewise run on
  his machine, not here — this plan's own execution can prepare code and art but cannot self-verify
  either.
- Adrian's ChatGPT plan limit is a real, already-hit constraint this session. No task below assumes
  unlimited generation capacity in one sitting.

## Sequencing relative to M4

M4's content debt (4 remaining Science Lab batches, five buildings' gameplay art, 122 menu-tile
icons, all building+road art) does not go away because this plan exists. Recommended default,
**not yet confirmed by the user**: M5's Task 1 (data model + rig mechanics, no new art) can start
immediately since it costs no ChatGPT generation; M5's art tasks (2 onward) interleave with M4's
remaining batches at the user's pace, sending whichever queue he prefers each session, rather than
one blocking the other outright. Flag this as an open question on plan review, same as the
face/skin-tone and shared-vs-gendered-shirt assumptions in the spec.

## Decisions already made by the user

See the spec's "Decisions made this session" for the full list; the ones that reshaped this plan's
structure after the first-draft review:

- Wardrobe ceiling counts (spec point 3) are a backlog target, **not** a requirement for declaring
  M5 successful — see Task 8 and the acceptance criteria in Task 10.
- The character-everywhere requirement (spec point 7) stays absolute for every minigame that
  actually exists — both characters, no exceptions, never removed for being "crowded" — but at
  small, standardized gameplay-scale sizes (2-3 layouts, not today's hub-preview scale), built as a
  shared component new screens inherit automatically. The rollout onto *existing* screens targets
  already-finished, actively-played buildings, not a blanket sweep across M4's five entirely
  unstarted buildings just to reach a total count.
- A visual style lock, plus an explicit enumerated v1 asset list (spec point 10), both happen
  before wardrobe volume is generated — the style reference so every later sheet is judged against
  approved references instead of drifting sheet to sheet, and the locked list so Task 3 generates
  exactly what was approved rather than an amount decided ad hoc while cutting sheets.
- `CreatorScreen`'s rebuild (spec point 11) must work for a non-reading 4-5-year-old: large visual
  category navigation, a small number of large choices on screen at once, no required text.
- `CharacterLook`'s growth is a real save-breaking change (spec's "Save compatibility" section) —
  Task 1 must handle it explicitly, not let `JsonUtility` silently default old saves into a
  wrong-looking character.
- Every wearable item carries a small, fixed metadata contract (spec point 12), decided once in
  Task 1 and applied to every item generated afterward, so adding items doesn't need per-item code
  fixes.
- Wardrobe items are free to pick, not coin-gated (spec point 13) — the existing coin/`Owned`
  economy stays scoped to House furniture only.
- Eva's rework (spec point 8) is real but deliberately minimal and last: establish the player
  character's quality bar first, then make only what coherence with Eva actually requires.

## Execution order

1. Task 1: data model + rig spike, including the wardrobe item metadata contract and save-version
   migration (no new art)
2. **Gate 1 (hard): user approves Task 1's spike** — data model, item contract, rig mechanics, and
   the migrate-or-invalidate save decision, before any real wardrobe art is generated
3. Task 2: visual style lock + explicit v1 asset list (review checkpoint, not a hard gate)
4. Task 3: v1 wardrobe art, exactly Task 2's locked list, judged against Task 2's style reference,
   + `CreatorScreen` rebuild (non-reader-friendly navigation, randomize button)
5. Task 4: Dress the Character rebuild (real preview rig, "keep this look?" prompt)
6. Task 5: joy reactions (creation + dress-up + randomize)
7. Task 6: character-everywhere spike — determines the 2-3 standard small gameplay layouts against
   a deliberately varied set of real screens
8. **Gate 2 (hard): user approves Task 6's spike** — the standard layouts, before the
   screen-integration rollout
9. Task 7: character-everywhere rollout — new minigames inherit the shared component automatically
   and unconditionally; existing, already-finished/actively-played screens are retrofitted; M4's
   five entirely-unstarted buildings are explicitly not a retrofit target (they inherit it when
   that M4 work happens)
10. Task 8: wardrobe breadth expansion — ongoing content backlog, explicitly outside this
    milestone's completion bar, same cadence as Science Lab
11. Task 9: Eva's minimum-coherence motion/art pass — lower priority, done after Tasks 1-7 establish
    the player character's quality bar, scoped to only what coherence actually needs
12. Task 10: acceptance and M5 summary

## Tasks

### Task 1: Data model + rig spike, including save migration (small, disposable, decision only)

**Question:** does the expanded `CharacterLook`/rig shape (Gender, Face, Skin, HairStyle,
HairColor, EyeColor, Top/Bottom-or-Dress, Shoes, Glasses) hold together mechanically, and can an
existing save load into it safely, before any real art is spent on it?

- Expand `CharacterLook` and `Palette` per the spec's "Data model and rig" section, using
  crude placeholder shapes (coloured rectangles are fine) for every new slot.
  - Resolve as part of this task, not before: exact hair/eye colour counts (propose 6-8 each);
  whether eyes are a separate layer or baked into Face art with a tintable iris; whether Bottom
  (Pants/Skirt) and Dress are mutually exclusive at the data level (`Bottom` and `Dress` cannot
  both be set) or the screen simply prevents picking both. (Wardrobe items are decided as free
  picks, not earned/purchased — see "Decisions already made" — so no `Owned`-style unlock check is
  needed here, unlike `HouseLayout`.)
  - **`Dress` is a distinct category, not a third Bottom option.** Model it, name it, and describe
  it everywhere as a separate one-piece garment that replaces the *rendering* of both `Top` and
  `Bottom` at once — never as "one more choice in the Bottom slot." Whether the exclusivity is
  enforced at the data level (setting `Dress` clears `Top`/`Bottom`) or only at the UI level is this
  task's call; the concept distinction is fixed regardless.
- **Wardrobe item metadata contract (small and fixed, not a generic wardrobe engine)**: before any
  clothing art is generated, define the minimum per-item data every wearable needs, and apply it
  uniformly to every category from Task 3 onward: a unique stable ID (sprite-key convention, e.g.
  `character/top_<id>`); which gender(s) it's valid for; its slot/category; its layer/draw order
  relative to the other slots; its rig attachment/alignment convention (which canonical-rig anchor
  it's positioned against, so a generated item lines up without a bespoke per-item offset); and
  whether it participates in the Dress/Bottom exclusivity rule. The goal is that adding item #47 to
  a category is "one catalogue entry plus its art," never "one entry, then debug its placement."
- Extend `RigFactory`/`CharacterRig.ApplyLook` to layer Hair over Face, optional Glasses over Face,
  and a Dress mode that swaps Top+Bottom rendering for one combined layer.
- **Save migration (required, not optional)**: bump `PlayerProgress.Version` to 2 and make
  `SaveStore.Load()` actually branch on it (today the field exists but is never read). Pick one,
  explicitly, and document the choice in the spike output:
  - **Migrate**: map the old 3-field `Look` (`Head`/`Skin`/`Shirt`) onto a sensible starting point
    in the new model (e.g. old `Skin` index carries straight across since skin tone stays the same
    5-swatch dimension; old `Head`/`Shirt` map to a specific chosen Gender/Face/Top combination) —
    the character looks *different* after the upgrade but the save isn't wiped.
  - **Invalidate**: detect the old version and deliberately reset `Look` to a fresh
    `CharacterLook` (and, if appropriate, `HasCharacter = false` so the child re-runs Creator) —
    acceptable and likely simpler given these are development saves, not live user data, but it
    must be a *deliberate, tested* reset, not silent `JsonUtility` default-filling of new fields
    onto an old save.
  - Either way: never let an old save load into the new model with unexamined zero-defaults in the
    new fields.
  - **Required test**: one `SaveStore` test that feeds `Load()` a hard-coded JSON fixture shaped
    like a genuine M1-era save (the old 3-field `Look`, no `Version` bump) and asserts the chosen
    behaviour — either the specific migrated `CharacterLook` values, or the specific invalidated/
    reset state — not just "it doesn't throw."
- Do **not** rebuild `CreatorScreen`'s full UI yet — enough of a harness to see the rig assemble
  correctly with placeholder shapes in every slot combination (including Dress mode) is sufficient.
- **Output**: `docs/superpowers/spikes/character-rig.md` with the resolved data-model questions
  above, the final slot list, the item metadata contract's exact fields, the migrate-vs-invalidate
  decision and why, and anything that didn't hold together (e.g. if Dress mode turns out to need a
  fundamentally different rig topology, not just a swapped sprite).
- **Gate 1 (hard)**: the user reviews the resolved data model, rig mechanics, and the save-migration
  decision (screenshots/description, since this container can't run Unity) before any real
  wardrobe art is generated.

### Task 2: Visual style lock + explicit v1 asset list

Only after Gate 1. No full v1 breadth generation yet — this task's own art output is deliberately
small; its main deliverable is the **locked scope** the next task executes against.

- Generate one small, real, illustrated reference set: a boy's face, hairstyle, t-shirt, bottom,
  and shoes; the same categories for a girl; one glasses style. Enough to nail down, not enough to
  commit real production volume to before it's approved.
- Write `art/character/STYLE.md` (or fold into `art/character/PROMPTS.md`'s header) capturing what
  this reference set establishes: palette (how skin/hair/eye tint ranges relate to each other),
  proportions relative to the existing rig's `RigFactory` canonical dimensions, outline/shading
  treatment (matching the "soft polished 3D-look" language already used throughout
  `art/eva/*/PROMPTS.md`), and layer conventions (what's baked into an item's own art vs. tinted at
  runtime).
- **Write the explicit v1 asset list — an enumerated count per category, not a proposal to refine
  later.** Starting point (confirm the exact numbers with the user as part of this task, not left
  open into Task 3): 3 faces/gender, 3 haircuts, 4 hair colours, 4 eye colours, 4 t-shirts, 2-3
  bottoms, 2-3 dresses for girls, 3 shoes, 3 glasses — enough real variety to feel like a creator,
  not a wall of placeholder. Once approved, this list *is* the v1 scope: Task 3 generates exactly
  it, not "roughly this many, adjusted as sheets come back."
- Every wardrobe sheet from Task 3 onward — v1 now, and every later expansion batch in Task 8 — is
  visually checked against the style reference specifically, not just "does this look good on its
  own," the same way every cut sheet already gets visually verified per `m4-handover.md`'s "things
  that went wrong" lessons.
- **Checkpoint (not a hard gate)**: the user reviews both the reference set and the locked v1 list
  together — the same natural review that already happens on every ChatGPT sheet paste-back —
  before Task 3's larger volume starts.

### Task 3: v1 wardrobe art + `CreatorScreen` rebuild

Only after Task 2's style reference and v1 asset list are both locked.

- Generate exactly Task 2's locked v1 asset list — no scope changes mid-batch; a category that
  turns out to need a different count goes back through Task 2's checkpoint, not decided ad hoc
  while cutting sheets.
- Write prompts and batch into ChatGPT sheets, same discipline as Science Lab's `PROMPTS.md`
  workflow: one `art/character/PROMPTS.md`, one commit per cut sheet, pushed immediately, every cut
  visually verified against Task 2's style-lock references (not just count-checked) per
  `m4-handover.md`'s "things that went wrong" lessons — this system inherits every chroma-key/
  grid-mode pitfall already documented there. Grid mode is almost certainly the default here
  (clothing items are rarely simple isolated icons). Every item's sprite key, gender, slot, draw
  order, and rig-attachment metadata follows Task 1's item contract as it's added to the catalogue.
- **Rebuild `CreatorScreen`'s UI for a non-reading 4-5-year-old, not a dense category list.** The
  current 3-row layout does not fit 9+ categories in the audited 900-tall frame, and a longer list
  of small labelled rows is exactly the wrong shape for this audience regardless. Use large visual
  category navigation (big icon tabs a child recognizes by picture, not text) showing a small
  number of large choices at once, with the live character preview doing most of the
  communicating — no category or option needs a text label to be understood. Whatever concrete
  shape this takes (tabs, a paged rail, etc.), every button stays at or above `EvaUi.MinTap` and
  respects the same non-overlap discipline the current screen's own layout comments already model.
- **Randomize button**: one `EvaUi.IconButton` (e.g. a dice/shuffle icon) that rolls one uniform
  pick per category — respecting Bottom-vs-Dress exclusivity from the Task 1 data model — updates
  every selection indicator and the live preview in one go, and fires the same joy reaction Task 5
  wires up for a manual pick. Needs its own `TapTarget`-sized slot in whatever category-navigation
  shape this task lands on; size/position it alongside the Confirm button, not competing with a
  category tab for space.
- **Check**: on-device (Adrian's machine) walk through every category, confirm the live preview
  updates correctly in every combination including Dress mode, confirm Confirm() still saves and
  routes to Map correctly, confirm randomize never produces an invalid combination (e.g. both
  Bottom and Dress set) across repeated taps, confirm a child who can't read can still navigate
  every category by icon alone.

### Task 4: Dress the Character rebuild

- Replace the four abstract slot icons with a live `CharacterRig` preview of the child's actual
  character (gender-correct, using their saved `Progress.Look` as the starting point for the round,
  not a fresh default). Dragging a piece onto its slot visually equips it on the rig in real time,
  not just an icon snapping into an outline.
- Replace the placeholder catalogue (`cap`/`hat`/`beanie`/`sunhat` etc.) with the real wardrobe
  items from Task 3's v1 breadth, respecting gender (a boy's round draws from boy items, a girl's
  from girl items). **`Dress` is not a Bottom-slot item** — per Task 1's data model, it's a
  distinct category that replaces both Top and Bottom at once, so a girl's round that draws a Dress
  needs its own handling in `DressTheCharacterRound`'s fixed-four-slots assumption (`Rules/
  DressTheCharacter.cs`'s `ClothingSlot` enum is Head/Top/Bottom/Feet today): either Dress occupies
  both the Top and Bottom slot positions as one piece the child drags once, or the round model
  grows a distinct Dress slot that, when drawn, suppresses Top and Bottom for that round. Resolve
  this as part of this task, following whichever enforcement approach Task 1 picked for the
  exclusivity rule.
- **New end-of-round beat**: after a completed outfit, ask "keep this look?" (voice + a simple
  yes/no choice, matching the existing confirm-button pattern elsewhere). Yes writes the assembled
  `CharacterLook` into `Progress.Look` (same contract as `CreatorScreen.Confirm()`, `Progress.
  HasCharacter` already true so no tutorial-event side effect needed) and reflects immediately
  everywhere the character is shown (Map, and later, every screen from Task 7). No discards the
  round's outfit and keeps the child's existing saved look, same as today's implicit behaviour.
- Extend `DressTheCharacterTests.cs` for the new round shape and the keep/discard persistence
  contract.
- **Check**: on-device, a full round with a kept look changes the character everywhere it's shown;
  a discarded round leaves the existing look untouched.

### Task 5: Joy reactions

- On every item pick in both `CreatorScreen` and the rebuilt Dress the Character, replace/augment
  today's subtle "hop" with a visibly delighted reaction — reuse `CharacterRig.Cheer()` if its
  existing animation reads as genuine joy at this frequency (every single tap), or add a lighter
  dedicated "pleased" state if `Cheer` is too big to trigger on every tap without feeling
  repetitive/annoying. This is a judgement call to make by actually watching it play, not a fixed
  spec — flag which one shipped in the M5 summary.
- The randomize button (Task 3) fires the bigger of the two reactions regardless of which one wins
  above for individual taps — a full-look reroll is the one moment in this system where the biggest
  available "delighted with myself" animation is warranted, not the per-tap one.
- **Check**: on-device, rapid-tapping through options doesn't feel spammy or break animation state
  (mirrors `CharacterRig.Wave()`'s existing "ignore while busy" guard — reuse that pattern if
  needed).

### Task 6: Character-everywhere spike

- Extend the `Hud`/`Navigator` shared-chrome mechanism with a companion-pairing component from the
  spec's "Screen integration" section: small, gameplay-scale (explicitly **not** today's
  ~500-560px hub-preview height), player-character-left/Eva-right, that a screen opts into as a
  shared component rather than hand-rolling its own layout math.
- **Test against these screens specifically, not an empty one**:
  - **Count the Objects** (the dense, six-answer-tile counting screen at a high difficulty level) —
    already flagged as tight on space in the M3 plan; the real stress test for "does the pairing
    genuinely stay out of the way at its smallest-margin screen."
  - **A drag-and-drop screen** (Jigsaw or Dress the Character itself) — proves the pairing survives
    a screen where the play area itself moves/resizes content during interaction.
  - **A visually open screen** (e.g. Free Drawing, or another screen with a large single canvas and
    minimal existing chrome) — the opposite end of the spectrum, where the pairing could plausibly
    be shown larger; determines whether a second, bigger standard layout is actually warranted or
    whether one small default covers everything.
- From these three, settle on **2-3 standard layouts** (not a bespoke size per screen): most likely
  one default small corner pairing that fits the large majority of screens unchanged, plus one or
  two alternates for screen shapes the default doesn't suit. Confirm at each candidate size that
  the characters stay genuinely recognizable and expressive — small enough to be out of the way
  does not mean small enough to become an illegible blob.
- **Output**: `docs/superpowers/spikes/character-everywhere.md` with the 2-3 standard layouts'
  final sizes/positions, which of the three test screens used which layout and why, and an honest
  estimate of how many of the *existing, finished, actively-played* screens (not the full ~120 —
  see Task 7's scope) are expected to take a standard layout unmodified vs. need individual
  adjustment because the standard genuinely conflicts with that screen's own UI.
- **Gate 2 (hard)**: the user approves the standard layouts — confirming they read as small,
  present, recognizable, and never in the way — before Task 7's rollout begins.

### Task 7: Character-everywhere rollout

Only after Gate 2. **Not a mandatory bespoke retrofit of ~120 files, and not gated on reaching a
total screen count.** The companion-pairing component from Task 6 is shared infrastructure: any
screen built on it gets both characters by construction, at whichever of the 2-3 standard layouts
fits. The product requirement — every minigame has the player character on the left and Eva on the
right — stays absolute for every minigame that exists; this task's actual scope of *work* is split
in two, deliberately different in effort:

- **New minigames, from this point on, inherit the pairing automatically and unconditionally** —
  nothing further to do per new screen beyond using the shared component, same as any other shared
  chrome (`Hud`, `Navigator`). This is not lower priority than anything else; it's simply free once
  the component exists. It includes the five buildings M4 hasn't built out yet: when their
  gameplay art and screens are actually built (separately-sequenced M4 work, not part of this
  task), they use the shared component from day one, at zero extra M5 cost.
- **Existing, already-finished, actively-played screens** — the buildings with real content today
  (Playground, Zoo & Farm, School, Store, and Science Lab as its remaining batches land) — get the
  pairing retrofitted, building by building, committing as reasonably-sized batches (not one giant
  commit). The large majority should take a standard layout with no per-screen change beyond wiring
  it in; only individually touch a screen's own layout where Task 6's spike (or this rollout) finds
  a genuine conflict with the standard.
- **Explicitly out of this task's scope**: retrofitting screens inside M4's five entirely-unstarted
  buildings. Their code exists (M4 is code-complete) but their art/layout is still placeholder and
  may still shift as that content actually gets built — spending retrofit effort there now, ahead
  of that work, risks redoing it. They get the pairing through the "new/being-built screens
  inherit automatically" path above, whenever that M4 work actually happens, not through a separate
  M5 sweep now.
- Re-verify the existing per-screen `TapTarget`/layout comments on any screen that does need
  individual adjustment, and confirm the pairing never overlaps a `TapTarget`, never sits in the
  learning interaction's way, and never shrinks the usable play area below what that game needs.
- **Check**: representative on-device pass across the buildings actually in scope (per the second
  bullet above), not just the ones from the spike; existing editmode layout/overlap tests (wherever
  they exist per-screen) still pass.

### Task 8: Wardrobe breadth expansion (ongoing, outside this milestone's completion bar)

- The v1 wardrobe (Task 3) is what proves the system works — this task grows it toward the spec's
  ceiling counts (20 t-shirts/gender, etc.) but **is not required for M5 to be considered done**
  (see Task 10's acceptance criteria). It runs as an ongoing content backlog alongside the rest of
  the game's art production (Science Lab's remaining batches, the five unstarted buildings, etc.),
  not as a blocking phase of this milestone.
- Same batch-by-batch cadence as Science Lab: each batch judged against Task 2's style-lock
  references, its own commit, pushed immediately, visually verified per sheet.
- No fixed task order beyond "keep going" — sequence by whatever the user wants generated next in
  a given content session, same as how Science Lab's batch order has worked all along.

### Task 9: Eva's minimum-coherence motion/art pass (lower priority, sequenced last)

Only substantively started after Tasks 1-7 have established the player character's new quality
bar — the point of this ordering is to evaluate Eva *beside* that bar, not in the abstract.

- Look at Eva next to the finished player character and identify specifically what reads as
  incoherent or dated by comparison — not an open-ended "improve Eva" brief. Candidates raised
  during design: `CatMotion`'s code-driven transform motion reading stiff/procedural next to the
  player rig's new richness, and any art-detail gap now visible side by side.
- Make the **minimum** changes needed for the two characters to feel like they belong in the same
  game together — richer motion where the side-by-side comparison actually shows a problem, new
  animation states only where genuinely needed (at minimum: Eva visibly reacting to the player
  character's outfit changes, tying into Task 5's joy-reaction moments), art polish only where it's
  now visibly dated. This is explicitly not a full Eva rework — mirror M3's spike-first discipline
  (`docs/superpowers/plans/2026-09-24-eva-m3-real-cat.md`) if any single change is big enough to
  warrant its own small spike, rather than assuming which approach is right.
- Eva's fixed identity requirements from M3 (black, fluffy, dwarf-proportioned, bobtail) are
  unaffected regardless of what else changes here.
- Explicitly the lowest-priority task in this plan — slot it in whenever the user wants a change of
  pace from wardrobe-batch generation, not before Tasks 1-7 are substantially done.

### Task 10: Acceptance and M5 summary

**Technical (necessary, not sufficient, run on Adrian's machine)**: cold editmode run, no
regression versus the pre-M5 baseline, including the Task 1 save-migration test passing against a
real old-format save fixture.

**Product gate (Adrian's device play, whole loop)**: character creation feels like an actual
creation experience a non-reading child can navigate unaided, not "pick a smiley + 2 swatches";
wardrobe items are freely pickable with no coin/unlock friction; Dress the Character visibly dresses
the child's own character and the "keep this look?" moment works and persists; the character
pairing appears correctly (small, recognizable, never overlapping, never stealing input) across a
representative spread of screens in every *finished, in-scope* building per Task 7 (not the five
buildings M4 hasn't built out yet — those are out of scope until that M4 work lands); joy reactions
read as genuine, not spammy; an old save loads safely (migrated or cleanly reset, never silently
wrong); Eva's coherence pass (if done this round) doesn't regress her fixed identity requirements
from M3. **Full wardrobe breadth (Task 8's ceiling counts) and the five not-yet-built M4 buildings
are both explicitly not part of this gate** — v1 breadth working end-to-end, on the screens that
actually exist today, is what M5 is judged on.

Write `docs/superpowers/spikes/M5-summary.md` in the M3-summary format.

## Risks

- **Art volume is still a real risk, just no longer a milestone-blocking one.** ~170-180 assets at
  ceiling breadth, already up against a ChatGPT plan limit hit mid-session on a much smaller ask.
  Mitigated by Task 8 being explicit backlog rather than an M5 completion requirement.
- **The character-everywhere rollout (Task 7) still touches a real number of files**, even though
  it's now explicitly scoped to finished/actively-played screens rather than all ~120 — the
  shared-component approach reduces the *design* risk per screen, and scoping out M4's five
  unstarted buildings cuts the volume meaningfully, but wiring in every existing, finished screen
  remains real mechanical work. Mitigated by gating the standard layouts behind their own spike
  (Task 6) and batching the rollout rather than one giant commit.
- **The five unstarted M4 buildings could keep their screens "temporarily" without the pairing
  for a long time** if M4's own content work stalls, even though the product requirement is meant
  to be absolute once a minigame exists — the "inherit automatically" mechanism only fires when
  those screens are actually built. Not a flaw in this plan's scoping (spending M5 effort on
  not-yet-real screens would be worse), but worth tracking as a follow-up check whenever M4's
  remaining buildings do land, so the automatic inheritance is verified in practice, not just
  assumed.
- **Dress-mode rig topology** (a one-piece garment replacing two independent slots) may turn out to
  need more rig rework than a simple sprite swap once actually built in Task 1 — the spike is
  specifically there to surface this before wardrobe art is spent assuming a shape that doesn't
  hold. The same wrinkle reaches `DressTheCharacterRound`'s fixed Head/Top/Bottom/Feet slot model
  (Task 4) — a round that draws a Dress doesn't cleanly fit "exactly four independent slots" and
  needs its own resolution, following whichever exclusivity mechanism Task 1 picks.
- **Save migration is easy to get wrong silently.** `JsonUtility`'s default-filling behavior means
  a missed case in Task 1 could ship a save bug that never throws an exception and is hard to
  notice — the required fixture-based test exists specifically to catch this before it's live.
- **Joy-reaction fatigue**: a big cheer animation firing on every single tap through a 20-item list
  could read as annoying rather than delightful — Task 5 explicitly treats this as a judgement call
  to watch and adjust, not a fixed spec.
- **The 2-3 standard layouts might not actually cover every screen shape** this app has — Task 6's
  honest estimate of "how many screens need individual adjustment" is the check on this; if that
  number comes back much higher than expected, that's a reason to revisit the layout count at
  Gate 2, not to silently absorb the cost in Task 7.
- **This container cannot run Unity or the editmode suite.** Every "check" in this plan is deferred
  to Adrian's own machine; work here can go stale relative to what actually compiles/runs until he
  verifies it.

## Deferred out of M5

See the spec's "Deferred out of this milestone" — body-type/height customization, accessories
beyond glasses, seasonal/event wardrobe items, a monetization-shaped wardrobe-unlock system, and any
change to Eva's fixed visual identity (only her motion/art coherence and reactions are in scope, and
only the minimum needed per Task 9).
