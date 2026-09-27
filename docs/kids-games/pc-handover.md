# Handover: cloud sessions → PC session (2026-09-27)

Adrian is moving from Claude Code on the web (cloud, no Unity) to a PC session (local machine, can
actually run Unity) to test what's landed on-device. This doc is **not** a cold-start "read this
first" doc for a fresh session — it's a summary of what changed on the shared repo while working in
the cloud, for a PC session that already has its own context/task to hand back to. Use it to catch
up on the repo state, then resume whatever you were already doing; nothing here asks you to start
new work.

**Do not start M5 (the character system plan below) on the PC session unless Adrian explicitly asks
for it.** It's designed, written up, and approved-in-principle, but deliberately not begun.

## Where to get the code

Repo: `scheiberadi/mobile-games-framework`. Branch: `claude/eva-m4-full-content`. All work described
below is committed and pushed there (working tree was clean, `git status` confirmed nothing pending,
as of this handover). Same gotcha the cloud sessions kept hitting — don't trust a stale local
checkout, fetch first:

```
git fetch origin claude/eva-m4-full-content && git checkout -B claude/eva-m4-full-content origin/claude/eva-m4-full-content
```

Two tool dependencies the cloud sessions needed per fresh container, probably already satisfied on
a PC that's worked on this repo before, but worth checking: `tools/art-import/` needs `npm install`
(for `sharp`, used by `cut-sheets.js`); Pillow (`pip install Pillow`) was used for quick webp→PNG
conversion and pixel checks, not part of committed tooling.

**Unlike the cloud sessions, this is not a constraint here**: `docs/kids-games/m4-handover.md`
repeatedly notes "no Unity project files usable in a cloud container" — that limitation is cloud-
specific. On PC, actually open the project in Unity, run the editmode suite
(`bash tools/run-editmode-tests.sh ".../EvasLearningWorld"`), build, and play on-device — all the
things the cloud sessions could prepare code/art for but never verify themselves.

## Provenance: three cloud sessions produced this

This work was done across three Claude Code on the web (claude.ai) sessions:
- https://claude.ai/code/session_018g1VtZb2jiVCNt2A2se33q
- https://claude.ai/code/session_01Co3xqh2ePu43UTYCkyyz56
- https://claude.ai/code/session_01AJaJLdikv9f2zK3SzeokeT (the session that wrote this handover)

Their individual transcripts aren't available to a PC session — but everything they produced is
captured in the repo itself: 66 commits on this branch, the code, the art assets, and two living
docs (`docs/kids-games/m4-handover.md` for M4 art production, plus the M5 planning docs below) that
were written specifically so a new session never needs the old transcripts. The summary below is
built from the actual repo state (git log, the handover doc's own running status), not from reading
those transcripts.

## What's done: M4 content (gameplay art, building by building)

Full detail and the exact continuation point live in `docs/kids-games/m4-handover.md` — **read that
file in full before touching any M4 art or content**, it has the sheet-cutting workflow, several
hard-won pitfalls (chroma-key edge cases, grid-mode blob-detection failure modes, save-migration
gotchas), and the precise next-batch prompt. Status as of this handover:

- **Code is complete for all of M4** — every building's games, Rules generators, difficulty
  ladders, screens, tests already exist (per `docs/kids-games/full-catalogue-plan.md`). Missing art
  renders as a placeholder rectangle, so the app already runs end-to-end even where art is missing.
- **M4.1 Playground: fully done**, all 9 games needing custom art.
- **M4.4 Zoo & Farm (incl. Geography): fully done**, all 11 games, 28 images.
- **M4.5 Science Lab: 13 of 17 art batches done.** Remaining 4 batches' prompts are already written
  in `art/eva/sciencelab/PROMPTS.md` — next one to send is batch 14 (Day/Night activities, part A).
  Adrian hit his ChatGPT plan limit mid-session generating this; may or may not have reset since.
- **Five buildings — Workshop, Art Studio, Brain Gym, Friends' Park, Arcade — have no gameplay art
  started at all.** Same for all 122 menu-tile icons and all building+road art. Each needs the same
  read-the-code / batch-the-art / cut-and-verify treatment `m4-handover.md` documents.

## What's designed but not started: M5, the character system

A separate, large piece of work: replacing today's flat placeholder player character (no hair, no
pants, a beige-circle face) with a real boy/girl customization system, a working Dress the Character
loop that actually dresses that character and offers to save the look, the character appearing
alongside Eva on every gameplay screen, and a minimal Eva motion/art coherence pass once the player
character sets the new quality bar.

This was designed across several rounds of review in the third cloud session and is **approved in
principle, execution-ready, but explicitly not started**:
- `docs/superpowers/specs/2026-09-27-character-system-design.md` — the design: what exists today
  (verified against the actual code), every decision made, and the assumptions flagged for
  confirmation.
- `docs/superpowers/plans/2026-09-27-m5-character-system.md` — the phased task breakdown, gated by
  two hard approval points: Task 1 (data model/rig/save-migration/wardrobe-item-contract spike)
  before any real wardrobe art is generated, and Task 6 (character-everywhere presentation layouts)
  before the broader screen-integration rollout.

**Milestone renumbering**: the character system took the "M5" slot, which collided with an older
roadmap that already had M5 = Device QA and M6 = Release. Those got pushed to M6 and M7
respectively — see the roadmap in `docs/superpowers/specs/2026-09-21-evas-learning-world-design.md`
section 12 for the current numbering, and search for "(was M5)"/"(was M6)" across `docs/` for every
place that got annotated during the renumbering, in case anything reads stale.

## What to actually do here

Nothing from this doc specifically. Resume whatever you (the PC session) were already working on —
this is purely a "here's what changed upstream while you were doing that" briefing. If Adrian asks
to continue M4 art, start from `m4-handover.md`'s own instructions. If he asks to start M5, start
from Task 1 of the plan above. Otherwise, this doc has done its job once you've absorbed it.
