# M5 handover — for a fresh session

Written 2026-10-01, end of the session that did Tasks 1/2/4. Read this whole file before doing
anything else — nothing from this session's memory carries over. The M5 plan
(`docs/superpowers/plans/2026-09-27-m5-character-system.md`) and spec
(`docs/superpowers/specs/2026-09-27-character-system-design.md`) are the actual source of truth for
scope/decisions; this file is "what happened, what's left, what to watch for," not a replacement for
either.

## Session-start gotcha: verify your branch before trusting anything

M5 lives on its **own branch, separate from M4**:

```
git fetch origin claude/eva-m5-character-system && git checkout -B claude/eva-m5-character-system origin/claude/eva-m5-character-system
```

`claude/eva-m4-full-content` is a **different, actively-worked branch** (another, likely local/PC,
session has been pushing M4 content to it independently). **Do not push M5 work to the M4 branch,
and do not assume M4-branch commits are visible here** — the two diverge from a shared ancestor
(`af6d880`) and are not meant to be merged by this session. If you're ever unsure which branch
you're on, check before committing anything.

**A real constraint hit this session, not hypothetical**: this cloud container's auto-mode safety
classifier denies a direct `git push` to the M4 branch ("Modify Shared Resources"), even when you
have a locally-prepared commit meant for it. If something ends up committed to the wrong branch,
don't attempt a workaround — ask the user, who can push themselves or explicitly approve it live. (This
once happened with M4 art prompts, now resolved - see "Arcade/Workshop prompts: resolved" below.)

## Where things stand

Branch: `claude/eva-m5-character-system`. HEAD when this list was written: `8fd45ca` (later commits exist; see `git log`). Full commit list
since the branches diverged (`af6d880..HEAD`, oldest first):

```
52a96d2 Dedupe overlapping stone paths, shrink stones, retire School/Store road art   [pre-M5, inherited]
98b98d5 Import 3 more activity icons; recover image_to_word from a black background    [pre-M5, inherited]
4849b8a Add missing .meta files for art committed from cloud sessions                  [pre-M5, inherited]
6c6ef6c M5 Task 1: character data model + rig spike (no new art)
f65a23b M5 Task 2: character visual style lock + proposed v1 asset list
6954fa4 M5: lock v1 asset list with gendered haircuts (46 images)
6e4f4b8 M5 Task 4: rebuild Dress the Character around the real wardrobe model
5699c1c M5: prepare Task 3's full v1 wardrobe prompt batch (46 images, 7 sheets)
2d39517 Fix Dress the Character's wardrobe sprite lookup to match its own naming
08be793 CreatorScreen: use real v1 boy t-shirt ids instead of arbitrary placeholders
d7c1e4b Clarify PROMPTS.md: face/haircut ids aren't wired into code yet
e60462c Prepare ChatGPT prompts to replace Eva's procedural cat art
afb43d0 Note Eva's art-redo prep in the M5 plan's progress log
8fa8015 M4 Arcade: write gameplay-art prompts (7 sheets, 74 images)      [M4 scope, since moved to M4 and reverted here]
8fd45ca M4 Workshop: write gameplay-art prompts (6 sheets, 61 images)   [M4 scope, since moved to M4 and reverted here]
```

**No Unity build or on-device pass has happened in this container for any of this** — same caveat
every M4/M5 session has carried. "Done" below means "code written and manually cross-checked against
the rest of the codebase," not "verified running."

### Task-by-task status against the plan's execution order

1. **Task 1 (data model + rig spike): done, Gate 1 approved.** Full writeup:
   `docs/superpowers/spikes/character-rig.md`. The user approved it with explicit instructions: keep
   6 hair/eye colours, keep Invalidate-not-Migrate for old saves, keep the interim `CreatorScreen`,
   don't expand Task 1 further.
2. **Task 2 (style lock + v1 asset list): done, approved with amendments.** `art/character/STYLE.md`
   (canonical setup/proportions/bounding boxes, derived straight from `RigFactory`'s constants) +
   `art/character/PROMPTS.md`'s first section (2 small reference-set prompts: boy 7 items, girl 6
   items) + `art/character/reference-guide.png` (generated proportions diagram, attached to every
   character prompt). **The user's approval came with two mandatory amendments, already applied**:
   (a) haircuts must be gendered, not shared — boys get 3 short styles, girls get 4 named styles
   (long, short, ponytail, pigtails); (b) the **ceiling counts** (Task 8's eventual target, not v1)
   are reconfirmed verbatim from the original brief: *"For boys: 20 t-shirts, 5-10 pants, 10 shoes,
   10 haircuts, eye color, hair colors, 10 glasses types. For girls: 20 t-shirts, 20 pants or skirts,
   20 dresses, 10 shoes, 20 haircuts, eye color, hair colors, 10 glasses types."* Recorded verbatim
   again in `STYLE.md`'s own "Ceiling" section — don't let this drift if it's ever restated.
3. **Task 3 (v1 wardrobe art + CreatorScreen rebuild): CODE PREPARATION DONE, ART NOT GENERATED, UNITY
   VALIDATION PENDING.** Status words are used strictly here: code written is not compiled, compiled is not
   device-validated, and none of it is generated art.
   - **Prompts prepared**: `art/character/PROMPTS.md` has the full v1 batch appended after the reference
     set: 7 sheets, 46 images, concrete per-item designs (colours/prints/styles, all 4 girl haircuts + 3 boy
     haircuts). Nothing has been run through ChatGPT yet (access was unavailable). The normal order still
     applies: **generate and review the small Task 2 reference set first, confirm it matches `STYLE.md`'s
     canonical setup, and only then run the full 46-image batch** - don't skip straight to it.
   - **CreatorScreen rebuild code is written** (2026-10-01): a paged category rail (prev/next arrows,
     current category's picture, randomize dice, up to 6 big choices, green check) with no text, driven by
     `Rules/CharacterCreator.cs` (+ `Tests/CharacterCreatorTests.cs`); `WardrobeCatalog` is populated with
     the v1 items and Dress the Character reads from it.
   - **No v1 wardrobe art has been generated or imported.** Everything shows placeholder boxes: faces use
     the 4 legacy heads, haircut/wardrobe icons are hash-coloured placeholders, and `icons/dice` has no art.
   - **Not compiled or run in Unity** (no Unity here): the new code and tests have never been compiled,
     the layout audit tests have not been run, and nothing is device-validated. The plan's on-device
     checklist for Task 3 is still entirely open.
4. **Task 4 (Dress the Character rebuild): done, code-only, not on-device verified.** Live
   `CharacterRig` preview (mirrors Eva), pieces equip onto the rig in real time, "keep this look?"
   prompt (new voice line generated and committed) before writing into `Progress.Look`.
   `Rules/DressTheCharacter.cs` rebuilt onto `Rules.WardrobeSlot`/gendered pools instead of the old
   fixed four-slot model. `CharacterLookTests.cs` (new) covers `Clone()`/`Normalize()` directly.
5. **Task 5 (joy reactions): CODE WRITTEN, UNITY/DEVICE VALIDATION PENDING.** `App/Characters/JoyReactions.cs`
   is written: a light "Pleased" bounce on every pick and a "Big" reaction (`Cheer()` + larger bounce) for
   randomize, used by both `CreatorScreen` and Dress the Character. It has not been compiled or run in
   Unity or on a device. Whether the timing and frequency feel delightful rather than repetitive or
   spammy is a judgement only real-device play can make, so that stays pending (the constants in
   `JoyReactions` are the tuning knobs).
6. **Task 6 (character-everywhere spike): CLOUD-SIDE SPIKE COMPLETE — UNITY/DEVICE VALIDATION PENDING.**
   Built in claude.ai with no Unity or device access: nothing compiled, no tests run, no rendered geometry
   measured, no screen seen. Gate 2 is NOT approved. The shared `CompanionPair` + `CompanionLayout`
   (Corner/Open, player left, Eva right, no TapTargets, raycasts off) exist but no screen uses them; Count,
   Jigsaw and Free Drawing are untouched; no third layout. Count is not exempt from the every-minigame
   pairing rule. The structural/estimated/needs-validation split and the 12-step PC checklist are in
   `docs/superpowers/spikes/character-everywhere.md`. Task 7 has not started.
7. **Tasks 7-10**: not started.

### Also done, ahead of the plan's own sequencing (explicit user request)

**Eva's art-redo prep** (`art/eva/cat-v2/`): the user explicitly asked for this now rather than
waiting for Tasks 1-7 ("pt Eva vreau alte imagini, nu cele generate de tine, deci am nevoie de
prompts" — wants genuinely new ChatGPT-illustrated art for Eva, replacing the current procedural SVG
look in `art/eva/cat/gen.js`, which is self-labelled a throwaway spike). Produced: `cat-boxes.json`
(real bounding boxes of all 11 current rig layers, measured with Pillow `getbbox()` on the actual
PNGs, not guessed), `cat-guide.png`/`cat-guide.js` (visual guide), `compose-cat-parts.js` (new
compositor — unlike wardrobe items, each cat layer must land at one *specific measured rectangle* on
a shared canvas, not just be cropped to its own square), and `PROMPTS.md` (a reference-illustration
prompt plus two part-sheet prompts covering all 11 layers). Style reconciles M3's fixed "real cat,
not cartoonish" identity with the game's "soft polished 3D-look" house style: match the rendering
technique, keep anatomy/proportions believably realistic. **Nothing generated yet** — prompts only,
same ChatGPT-access blocker as Task 3.

## Arcade/Workshop prompts: resolved

The Arcade and Workshop gameplay-art prompts were M4 scope. The user ruled "M4. M5 is just about character +
Eva redesign", and both were moved:

- Arcade prompts moved to M4: `7695cd7`
- Workshop prompts moved to M4: `9019362`
- M5 reverts: `e61d488` (Arcade) and `a6a4452` (Workshop)

Therefore M5 contains neither M4 art-prompt set.

**Going forward: do not write any more M4 building-art prompts (Art Studio, Brain Gym, Friends'
Park) from this M5 session**, even if asked "anything else to do?" during an idle stretch — that
question should be answered with more M5 work (Task 3 follow-through once ChatGPT is back, or
Task 5/6 prep), not by picking up M4's backlog.

## Bugs caught and fixed this session (both self-caught, no user report)

1. **Deleted a shared enum by accident.** Rewriting `Rules/DressTheCharacter.cs` for the new
   `WardrobeSlot` model, I removed the old `ClothingSlot` enum entirely — not realizing
   `DressForOccasion.cs`/`PackASuitcase.cs` (unrelated games) also depend on it, which would have
   broken compilation project-wide. Caught via a deliberate `grep -rn "ClothingSlot\."` sweep before
   considering Task 4 done. Fixed by restoring the original enum with a comment explaining it's
   shared. **Lesson: before deleting any type/field that looks local to one file, grep the whole repo
   for it, not just the file you're editing.**
2. **Stale sprite-key prefix.** `DressTheCharacterScreen.cs` still looked up wardrobe sprites under
   the old `"dressup/"` prefix after the data model and `STYLE.md` had settled on `"character/"` —
   would have silently kept showing placeholders forever even once real art existed under the
   documented convention. Caught cross-checking Task 3's prompts against the actual code. Fixed both
   lookup sites (tray icon, initial drag icon) plus two now-inaccurate comments in the unrelated
   games' files that had claimed a shared sprite convention with Dress the Character.

## Working conventions (same ones M4 established, read `m4-handover.md` too if unfamiliar)

- Same ChatGPT-sheet pipeline: plain solid magenta (#ff00ff) background (never fake/checkered
  transparency), `tools/art-import/cut-sheets.js`'s `keyed()` chroma-key (copy verbatim, never
  approximate), `grid: {cols,rows}` mode preferred over blob detection for anything richer than
  isolated icons, one commit per generated/cut sheet pushed immediately, attach a reference/guide
  image whenever proportions matter (same pattern for `art/character/reference-guide.png` and
  `art/eva/cat-v2/cat-guide.png` as for the map's `layout-guide.png`).
- `art/character/` needs its own compositor eventually too if any wardrobe item ever needs
  multi-piece placement like Eva's cat parts do — none do yet (every wardrobe item is a simple
  cropped-to-its-own-square icon, per `STYLE.md`), but if that changes, look at
  `tools/art-import/compose-cat-parts.js` first rather than writing a new approach from scratch.
- No Unity/C# compiler in this container — verification here means `node --check` for JS, careful
  manual cross-referencing/grepping for C#, and ad-hoc Pillow smoke tests, not real compilation.
  Catching the two bugs above happened entirely this way.
- The no-reading-audit rule (only digits may appear as visible text in gameplay art) applies to
  every prompt written, M4 or M5 scope alike — already baked into the M4 art prompts too
  (e.g. Whack-a-Mole's ids distinguished by colour/pattern, never shown as letters).
- `docs/superpowers/plans/2026-09-27-m5-character-system.md` has a "Progress" log appended after
  each major milestone — keep appending to it rather than letting this handover file become the only
  record; it records the 2026-10-01 progress (CreatorScreen rebuild, joy reactions, scope note).

## Next steps, in order

1. **Once ChatGPT access is back**, run Task 2's small reference-set prompts first (not straight to
   the 46-image v1 batch), confirm they match `STYLE.md`'s canonical setup, then run Task 3's 7-sheet
   batch. Same for `art/eva/cat-v2/PROMPTS.md`'s reference illustration + 2 part sheets.
2. **Unity validation of what is already written** (needs the PC): compile; run `CharacterCreatorTests`,
   `CharacterLookTests`, `DressTheCharacterTests` and the Creator layout audit tests; then play Task 3's
   `CreatorScreen` and Task 5's reactions on a device. When the Task 3 art lands, swap it in under the sprite
   keys already used (no code change expected).
3. **Task 5's feel and Task 6's layouts** need on-device judgement calls this container can't make - flag
   to the user rather than guessing when reached. (Task 6 is cloud-side complete, validation pending.)
4. Stay off M4 building-art prompts (Art Studio, Brain Gym, Friends' Park) unless the user explicitly
   asks again.
