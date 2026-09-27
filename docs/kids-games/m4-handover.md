# M4 handover — for a fresh session

This project moved from a claude.ai Project (per-thread sessions with shared memory) to a plain
cloud session, so nothing below assumes any memory carries over. Read this whole file before doing
anything else.

## Where things stand

Branch: `claude/eva-m4-full-content`, mirrored onto PR #1's actual head branch
(`claude/eva-m4-full-content-82lmj0` at the time of writing — **check `git branch -r` and the PR's
current head before pushing**, in case it changed). Both branches are at the same commit as of this
handover: `51207ee`. PR #1 (https://github.com/scheiberadi/mobile-games-framework/pull/1) is the
single PR for all of this — never open a second one; if your own session gets a differently-named
designated branch, reset it onto `claude/eva-m4-full-content` before building
(`git fetch origin claude/eva-m4-full-content && git checkout -B <your-branch> origin/claude/eva-m4-full-content`).

**M4 is complete in code.** All ten buildings (Playground, School, Store, Zoo & Farm, Science Lab,
Workshop, Art Studio, Brain Gym, Friends' Park, Arcade) have their full game lists built: Rules
generators, difficulty ladders, help ladders, screens/presenters, tests. See
`docs/kids-games/full-catalogue-plan.md` for the per-building game list and every design call made
to simplify a game to an existing presenter (MATCH/SORT, SEQUENCE, ASSEMBLY, TRACE, MEMORY BOARD,
SEQUENCE RECALL) instead of a bespoke mechanic — those are flagged there for Adrian's review, not
decided unilaterally.

**Voice audio is complete: all 1022 lines have a generated `.mp3`.**
`EvasLearningWorld/Assets/Eva/Resources/Voice/voice-lines.txt` has every line every building's code
references (verified programmatically against the actual voice keys in `Rules/*.cs` and
`App/EvaGame.cs`'s screen registrations — no gaps, no duplicates). `Resources/Voice/en/` has a
matching `.mp3` for every one of those 1022 keys (55 pre-existed for the Count game, 967 were
generated in this project). See "Generating voice audio" below if more lines are ever added.

**Art is the one thing still fully outstanding — zero pieces of M4 art exist.** Missing sprites
silently render as a colored placeholder rectangle (`EvaUi.Sprite`, `Assets/Eva/App/Ui/EvaUi.cs:53`)
and missing voice clips silently skip playback (`Voice.Say`, `Assets/Eva/App/Audio/Voice.cs:39`), so
the whole game already runs end-to-end — it just looks like placeholders everywhere in M4. Full
inventory of what's missing: `docs/kids-games/m4-art-and-sound-audit.md`. Short version:
- Map/building art exists only for House, School, Store (all pre-M4). The other 8 buildings have
  none.
- Menu tile art exists only for the pre-M4 Count game (`Resources/Art/activities/count.png`). The
  other 122 games have none.
- `tools/art-import/*` only *composites* already-produced source art (it expects files dropped in a
  local folder, magenta-keyed for transparency) — it doesn't generate art itself. Filling this gap
  is real art/content work.

## What's left, in order

1. **Generate the art with ChatGPT.** `docs/kids-games/m4-chatgpt-prompts/` has one ready-to-paste
   `.txt` prompt per missing image — 8 road textures, 8 building/POI images, 122 activity menu
   tiles. Each prompt is fully self-contained (style, scene, size, background all baked in): open
   the file, copy the whole thing into ChatGPT, save the result exactly as the prompt's filename
   says (e.g. `road_workshop.txt` → save as `road_workshop.png`).
   - **Road prompts need an attached reference image.** They follow the same "draw a path exactly
     along this red line, from the blue dot to the green dot" workflow Adrian already used
     successfully for the School/Store roads — the reference images are in
     `docs/kids-games/m4-road-guides/` (`road_<building>_guide.png`), generated with
     `node tools/art-import/map-art.js roadguides` (needs `npm install` in `tools/art-import/`
     first, for `sharp`). Attach the matching guide alongside its `.txt` prompt.
   - Building/road prompts ask for a flat magenta (`#FF00FF`) background so the import script can
     key it out to transparency. Tile prompts ask for the finished picture-book-style rounded tile
     (frame + green play badge included) directly, since there's no per-tile import script yet —
     these get saved straight into `Resources/Art/activities/<id>.png`, no processing needed.
2. **Extend `tools/art-import/import-map-art.js`.** It currently only imports House/School/Store
   (hardcoded at the bottom of the file). Once the 8 new road/building images exist, extend it to
   loop over every place in `places-layout.json` instead of the hardcoded three, then run it to
   magenta-key, trim and resize each into `Resources/Art/world/`.
3. **Import the 122 tiles** — no script needed, they're saved directly since they're already the
   finished tile.
4. **Unity build + phone test.** No Unity is available in a cloud container, so nothing in M4 has
   ever actually been built or run — not even the pure C# NUnit tests (there's no `.csproj`/`.sln`
   here). Adrian does this pass himself on his own machine or via Remote Control. New `.mp3` files
   have no `.meta` yet; Unity generates those automatically the first time the project opens with
   them present — nothing to do about that from a cloud session.
5. **Review the placeholder content and simplification calls.** Every dataset (word lists, animal
   sets, scenario text, etc.) is placeholder pending a real content pass, and every "built as MATCH
   instead of a bespoke mechanic" call is flagged in `docs/kids-games/full-catalogue-plan.md` for
   Adrian's review — nothing there should be treated as final without his sign-off.

## Generating voice audio (only if more lines get added later)

```
NODE_USE_ENV_PROXY=1 node tools/voice/generate.js EvasLearningWorld/Assets/Eva/Resources/Voice/voice-lines.txt EvasLearningWorld/Assets/Eva/Resources/Voice/en
```

- `NODE_USE_ENV_PROXY=1` is required in a cloud session that has Google TTS proxy credentials set
  up for this project's environment (Node's built-in `fetch` doesn't honor `HTTPS_PROXY` without
  it — every request 403s otherwise, even with a live credential).
- If that environment isn't available (a different project, a fresh environment with no proxy
  credential configured), the script also accepts a `GOOGLE_TTS_API_KEY` environment variable
  instead — it calls the endpoint with `?key=...` when that's set, and without any key param when
  it's not. **Never commit the key, print it, or put it in a file.**
- The script only generates clips whose `.mp3` doesn't already exist, so it's always safe to re-run
  after adding new lines.

## Working conventions worth knowing

- **One prompt per image file, never a doc with a table.** Adrian's workflow is: open a `.txt`,
  copy it whole into ChatGPT, save the result. Don't go back to a markdown table format for these.
- **The road-guide workflow** (red centerline + blue start dot + green end dot, generated from
  `places-layout.json` via `map-art.js roadguides`) is proven and should be reused for any future
  road/path art — don't freehand a road prompt without a guide image.
- **Files meant for Adrian go in git**, not anywhere ephemeral — he reads this repo across
  sessions, so anything worth keeping (prompts, guides, docs like this one) belongs committed and
  pushed, not left in a session-only scratch space.
- **PR #1 is the only PR.** Any session that gets its own auto-created PR on its own designated
  branch should close it as a duplicate and keep pushing to PR #1's actual head branch instead.
- **This repo has no Unity project files usable in a cloud container** — don't try to run Unity,
  build an APK, or run the NUnit tests here; that's Adrian's own machine or Remote Control.

## Opening prompt for the new session

Adrian will paste something like the block below to start the new session. Read this handover doc
fully before doing anything else, then wait for him to say whether to continue with images or audio
(audio is actually already done — see above — so in practice this means images, unless more lines
get added to the manifest).

---

> Read `docs/kids-games/m4-handover.md` in full first — it has the current state of M4 (code is
> done, voice audio is done, art is the only thing left) and the working conventions to follow.
> Then tell me you've read it and ask whether to continue with the ChatGPT image generation/import
> or something else, before doing any work.
