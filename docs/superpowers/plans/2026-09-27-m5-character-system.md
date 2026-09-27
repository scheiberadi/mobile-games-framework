# M5 plan: real player character, a working dress-up loop, character everywhere, Eva polish

**For review only. Nothing in this plan is executed until the user approves it.** Design context
and the decisions this plan is built on: `docs/superpowers/specs/2026-09-27-character-system-design.md`
— read that first, this doc is the task breakdown only. M4 (`claude/eva-m4-full-content`) is still
open (Science Lab 13/17 batches, five buildings' gameplay art and all menu-tile/road art entirely
unstarted); this plan does not close M4 or compete with it for a branch — see "Sequencing" below.

## Goal

Replace the flat placeholder player character with a real boy/girl character system (10 face types
per gender, hair, eyes, clothing, glasses), make Dress the Character actually dress that character
and offer to keep the look, put the character next to Eva on every gameplay screen, and give Eva a
lower-priority motion/polish pass — in that order, each gated by the user's review before the next
phase's art generation starts.

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
**not yet confirmed by the user**: M5's Task 1 (data model + UI mechanics, no new art) can start
immediately since it costs no ChatGPT generation; M5's art tasks (2 onward) interleave with M4's
remaining batches at the user's pace, sending whichever queue he prefers each session, rather than
one blocking the other outright. Flag this as an open question on plan review, same as the
face/skin-tone and shared-vs-gendered-shirt assumptions in the spec.

## Decisions already made by the user

See the spec's "Decisions made this session" — restated in one line each for this plan's own
task-gating: boy/girl (no animal heads); 10 faces/gender; wardrobe ceiling counts per the spec;
visible joy reaction on every item change; Dress the Character rebuilt around the real character
with an end-of-round "keep this look?" prompt; character on every gameplay screen; Eva motion/polish
pass, lower priority. Production-volume reality (~170-180 assets at ceiling breadth) means art ships
as a **small v1 wardrobe first**, then grows batch by batch like Science Lab, never blocking the
mechanism on full breadth being in hand.

## Execution order

1. Task 1: data model + rig spike (no new art — reuse/placeholder shapes only)
2. **User review and approval of the spike (hard gate)** — confirms the slot model (Dress
   overriding Top+Bottom), face/skin independence, hair/eye colour counts, and whether wardrobe
   items are free picks or earned/purchased
3. Task 2: v1 wardrobe art (small breadth, real illustrated quality) + `CreatorScreen` rebuild
4. Task 3: Dress the Character rebuild (real preview rig, "keep this look?" prompt)
5. Task 4: joy reactions (creation + dress-up)
6. Task 5: character-everywhere spike (a handful of real screens, incl. a cramped one)
7. **User review and approval of the spike (hard gate)** — confirms the companion-strip shape
   before the ~120-screen retrofit
8. Task 6: character-everywhere full rollout
9. Task 7: wardrobe breadth expansion (ongoing content batches, same cadence as Science Lab)
10. Task 8: Eva motion/polish pass (lower priority; can slot in wherever the user wants a break
    from wardrobe-batch generation)
11. Task 9: acceptance and M5 summary

## Tasks

### Task 1: Data model + rig spike (small, disposable, decision only)

**Question:** does the expanded `CharacterLook`/rig shape (Gender, Face, Skin, HairStyle,
HairColor, EyeColor, Top/Bottom-or-Dress, Shoes, Glasses) hold together mechanically before any real
art is spent on it?

- Expand `CharacterLook` and `Palette` per the spec's "Data model and rig" section, using
  crude placeholder shapes (coloured rectangles are fine) for every new slot.
  - Resolve as part of this task, not before: exact hair/eye colour counts (propose 6-8 each);
  whether eyes are a separate layer or baked into Face art with a tintable iris; whether Bottom
  (Pants/Skirt) and Dress are mutually exclusive at the data level (`Bottom` and `Dress` cannot
  both be set) or the screen simply prevents picking both.
- Extend `RigFactory`/`CharacterRig.ApplyLook` to layer Hair over Face, optional Glasses over Face,
  and a Dress mode that swaps Top+Bottom rendering for one combined layer.
- Do **not** rebuild `CreatorScreen`'s full UI yet — enough of a harness to see the rig assemble
  correctly with placeholder shapes in every slot combination (including Dress mode) is sufficient.
- **Output**: `docs/superpowers/spikes/character-rig.md` with the resolved data-model questions
  above, the final slot list, and anything that didn't hold together (e.g. if Dress mode turns out
  to need a fundamentally different rig topology, not just a swapped sprite).
- **Gate**: the user reviews the resolved data model and rig mechanics (screenshots/description,
  since this container can't run Unity) before any real wardrobe art is generated.

### Task 2: v1 wardrobe art + `CreatorScreen` rebuild

Only after Task 1 is approved.

- Pick a small v1 breadth per category (propose: 3 faces/gender, 3 haircuts, 4 hair colours, 4 eye
  colours, 4 t-shirts, 2-3 bottoms, 2-3 dresses for girls, 3 shoes, 3 glasses — enough real variety
  to feel like a creator, not a wall of placeholder). Confirm the exact v1 numbers with the user
  before generating (they may want a different starting breadth than this proposal).
- Write prompts and batch into ChatGPT sheets, same discipline as Science Lab's `PROMPTS.md`
  workflow: one `art/character/PROMPTS.md`, one commit per cut sheet, pushed immediately, every cut
  visually verified (not just count-checked) per `m4-handover.md`'s "things that went wrong"
  lessons — this system inherits every chroma-key/grid-mode pitfall already documented there.
  Grid mode is almost certainly the default here (clothing items are rarely simple isolated icons).
- Rebuild `CreatorScreen`'s UI for the full category list. The current 3-row layout does not fit
  9+ categories in the audited 900-tall frame — this needs a real UI decision (tabs/pages by
  category, a scrollable rail, category-switch buttons), not a naive row-per-category stack. Whatever
  shape is chosen must keep every button at or above `EvaUi.MinTap` and respect the same
  non-overlap discipline the current screen's own layout comments already model.
- **Check**: on-device (Adrian's machine) walk through every category, confirm the live preview
  updates correctly in every combination including Dress mode, confirm Confirm() still saves and
  routes to Map correctly.

### Task 3: Dress the Character rebuild

- Replace the four abstract slot icons with a live `CharacterRig` preview of the child's actual
  character (gender-correct, using their saved `Progress.Look` as the starting point for the round,
  not a fresh default). Dragging a piece onto its slot visually equips it on the rig in real time,
  not just an icon snapping into an outline.
- Replace the placeholder catalogue (`cap`/`hat`/`beanie`/`sunhat` etc.) with the real wardrobe
  items from Task 2's v1 breadth, respecting gender (a boy's round draws from boy items, a girl's
  from girl items, including Dress as a valid Bottom-slot outcome for girls per the data model).
- **New end-of-round beat**: after a completed outfit, ask "keep this look?" (voice + a simple
  yes/no choice, matching the existing confirm-button pattern elsewhere). Yes writes the assembled
  `CharacterLook` into `Progress.Look` (same contract as `CreatorScreen.Confirm()`, `Progress.
  HasCharacter` already true so no tutorial-event side effect needed) and reflects immediately
  everywhere the character is shown (Map, and later, every screen from Task 6). No discards the
  round's outfit and keeps the child's existing saved look, same as today's implicit behaviour.
- Extend `DressTheCharacterTests.cs` for the new round shape and the keep/discard persistence
  contract.
- **Check**: on-device, a full round with a kept look changes the character everywhere it's shown;
  a discarded round leaves the existing look untouched.

### Task 4: Joy reactions

- On every item pick in both `CreatorScreen` and the rebuilt Dress the Character, replace/augment
  today's subtle "hop" with a visibly delighted reaction — reuse `CharacterRig.Cheer()` if its
  existing animation reads as genuine joy at this frequency (every single tap), or add a lighter
  dedicated "pleased" state if `Cheer` is too big to trigger on every tap without feeling
  repetitive/annoying. This is a judgement call to make by actually watching it play, not a fixed
  spec — flag which one shipped in the M5 summary.
- **Check**: on-device, rapid-tapping through options doesn't feel spammy or break animation state
  (mirrors `CharacterRig.Wave()`'s existing "ignore while busy" guard — reuse that pattern if
  needed).

### Task 5: Character-everywhere spike

- Extend the `Hud`/`Navigator` shared-chrome mechanism with the companion-strip concept from the
  spec: a fixed-position, fixed-size player-character-left/Eva-right pairing that a screen opts into
  without hand-rolling its own layout math.
- Test it against a small, deliberately varied set of real screens, not just an empty one: the
  six-answer-tile counting screen at a high level (already flagged as tight in the M3 plan), a
  drag-and-drop screen (Jigsaw or Dress the Character itself), and a simple tap-the-target screen.
- **Output**: `docs/superpowers/spikes/character-everywhere.md` with the companion strip's final
  size/position, which (if any) screens need individual layout adjustment to make room, and an
  honest estimate of how many of the ~120 screens are "free" (chrome fits with zero screen-specific
  change) vs. need a per-screen touch.
- **Gate**: the user approves the companion strip's look and confirms the rollout is worth the
  per-screen cost the spike surfaces, before Task 6 touches ~120 files.

### Task 6: Character-everywhere full rollout

Only after Task 5 is approved.

- Roll the approved companion strip out screen by screen, building's building, committing as
  reasonably-sized batches (not one 120-file commit) — same discipline as every other large sweep
  this project has done.
- Re-verify the existing per-screen `TapTarget`/layout comments and adjust wherever the strip
  actually collides with something (the spike's estimate from Task 5 sizes this work; this task is
  where it's paid off screen by screen).
- **Check**: representative on-device pass across several buildings, not just the ones from the
  spike; existing editmode layout/overlap tests (wherever they exist per-screen) still pass.

### Task 7: Wardrobe breadth expansion (ongoing)

- Same batch-by-batch cadence as Science Lab's remaining work: grow each category from its v1
  count toward the spec's ceiling counts (20 t-shirts/gender, etc.) as separate content sessions,
  each batch its own commit, pushed immediately, visually verified per sheet.
- No fixed task order beyond "keep going" — sequence by whatever the user wants generated next in
  a given session, same as how Science Lab's batch order has worked all along.

### Task 8: Eva motion/polish pass (lower priority)

- Richer motion for `CatMotion` (evaluate whether the code-driven transform approach can be pushed
  further, or whether specific moments — e.g. reacting to the player's own outfit change — warrant
  a small set of additional drawn poses/layers, mirroring M3's spike-first approach rather than
  assuming which is right).
- New animation states, at minimum: a reaction to the player character's outfit changing (ties into
  Task 4's joy-reaction moments — Eva should visibly notice, not just the player character).
- Art-detail/polish pass on existing cat sprites per the user's own "art detail" answer, scoped by
  what actually looks dated once the player character is next to her at the new quality bar.
- Explicitly the lowest-priority task in this plan — slot it in whenever the user wants a change of
  pace from wardrobe-batch generation, not before Tasks 1-6 are substantially done.

### Task 9: Acceptance and M5 summary

**Technical (necessary, not sufficient, run on Adrian's machine)**: cold editmode run, no
regression versus the pre-M5 baseline.

**Product gate (Adrian's device play, whole loop)**: character creation feels like an actual
creation experience, not "pick a smiley + 2 swatches"; Dress the Character visibly dresses the
child's own character and the "keep this look?" moment works and persists; the character appears
correctly (position, mirroring, no overlap) across a representative spread of screens per building;
joy reactions read as genuine, not spammy; Eva's polish pass (if done this round) doesn't regress
her fixed identity requirements from M3.

Write `docs/superpowers/spikes/M5-summary.md` in the M3-summary format.

## Risks

- **Art volume is the single biggest risk.** ~170-180 assets at ceiling breadth, already up against
  a ChatGPT plan limit hit mid-session on a much smaller ask. Mitigated by the v1-then-expand
  structure (Task 2 then Task 7) — nothing blocks on full breadth existing.
- **The companion-strip rollout (Task 6) touches ~120 files.** Likely the single largest mechanical
  change this project has done in one sweep. Mitigated by gating it behind its own spike (Task 5)
  and batching the rollout rather than one giant commit.
- **Dress-mode rig topology** (a one-piece garment replacing two independent slots) may turn out to
  need more rig rework than a simple sprite swap once actually built in Task 1 — the spike is
  specifically there to surface this before wardrobe art is spent assuming a shape that doesn't
  hold.
- **Joy-reaction fatigue**: a big cheer animation firing on every single tap through a 20-item list
  could read as annoying rather than delightful — Task 4 explicitly treats this as a judgement call
  to watch and adjust, not a fixed spec.
- **This container cannot run Unity or the editmode suite.** Every "check" in this plan is deferred
  to Adrian's own machine; work here can go stale relative to what actually compiles/runs until he
  verifies it.

## Deferred out of M5

See the spec's "Deferred out of this milestone" — body-type/height customization, accessories
beyond glasses, seasonal/event wardrobe items, a monetization-shaped wardrobe-unlock system, and any
change to Eva's fixed visual identity (only her motion/polish/reactions are in scope).
