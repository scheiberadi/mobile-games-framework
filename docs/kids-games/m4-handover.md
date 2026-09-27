# M4 handover — for a fresh session

Rewritten 2026-09-27, superseding the previous version of this file (which predates the gameplay
object art work below and is now stale on several points — e.g. it claimed art was "fully
outstanding" with zero pieces done, no longer true). Read this whole file before doing anything
else; nothing from any prior session's memory carries over.

## Where things stand

Branch: `claude/eva-m4-full-content`. Push directly to it — no separate PR branch dance needed as
of this writing (check `git branch -r` / any open PR before assuming that's still true). HEAD at
the time of writing: `26239b4`.

**Code and voice audio are complete** (see the "Code/voice" section below for the unchanged
details). **Art has three separate layers, at three different stages**:

1. **Menu-tile icons** — one small illustrated icon per game, shown on its building's game-select
   menu. **Prompts exist for all 122 games** (`docs/kids-games/m4-chatgpt-prompts/activities_*.txt`,
   one ready-to-paste `.txt` per game) **but none have been generated or imported yet** — only the
   pre-M4 `count.png` exists in `Resources/Art/activities/`. Nobody has started this layer.
2. **Building + road art** — the 8 non-pre-M4 buildings' exterior art and the dirt paths to them.
   **Prompts exist** (`docs/kids-games/m4-chatgpt-prompts/place_*.txt` / `road_*.txt`, with road
   guide images in `docs/kids-games/m4-road-guides/`) **but again nothing generated/imported yet** —
   `Resources/Art/world/` only has House/School/Store (pre-M4). Nobody has started this layer either.
3. **In-game gameplay object art** — the actual objects/pictures each game renders while being
   played (an odd-one-out's animals, a match game's choice pictures, a jigsaw's source photo, etc.).
   **This is what the current session has been doing, building-by-building, and it's the one with
   real progress:**
   - **M4.1 Playground: fully done.** All 9 games that need custom art have it: Odd One Out (20
     objects), Item to Shadow (16 objects + 16 derived silhouettes), Pattern Completion/What's
     Missing (5 shared shape tiles), Jigsaw (1 source photo sliced into a 5x5 grid), Tangram (7
     pieces), Rotate the Piece (6 pieces), Which Doesn't Make Sense (40 scenes). The other 5
     Playground games (Finger Maze, Follow Numbers/Letters, Shortest Path, Avoid Obstacles, Collect
     Everything) render procedurally and need no art. Prompts + process notes:
     `art/eva/playground/PROMPTS.md`.
   - **Known but deliberately deferred bug**: `Rules/Jigsaw.cs` reuses the same sprite keys
     (`jigsaw/piece_<row>_<col>`) across every difficulty level even though each level's grid is a
     different size (2x2 up to 5x5) — so at low levels the child currently sees a mismatched
     corner-crop of the picture instead of the whole thing sliced correctly. Adrian said "for
     later" when this was flagged (2026-09-27) — don't fix it unless he asks again.
   - **M4.4 Zoo & Farm: fully done**, all 11 games (the 10 animal games plus Geography). Batch 8
     (9 sorting-bucket icons) landed. Geography's 28 images (11 flags, 6 continents, 11 landmarks)
     landed across batches 9-11, with two content-specific cutting snags worth knowing about before
     the next building hits something similar: a continent's fill colour (purple, for Oceania) was
     close enough to the `#ff00ff` magenta chroma-key to get keyed out along with the background -
     regenerate in a different colour rather than fight the chroma-key tolerance; and a richer
     landmark scene sheet had neighbouring cells' foliage touching (merged a whole row into one
     blob) and a tall item overhanging into the row below (Eiffel Tower's base, Statue of Liberty's
     pedestal bled into the next row) - `cut-sheets.js` now has a `grid: {cols, rows}` mode for this
     (largest connected blob per nominal cell, see `geo_landmarks` in `tools/art-import/cut-sheets.js`
     and its comment) - reach for it on any future sheet with touching foliage/water between cells
     or a tall foreground element. Prompts + process notes: `art/eva/zoofarm/PROMPTS.md`.
   - **The other 6 buildings — Science Lab (M4.5), Workshop (M4.6), Art Studio (M4.7), Brain Gym
     (M4.8), Friends' Park (M4.9), Arcade (M4.10) — haven't been looked at at all** for this
     gameplay-art layer. Each needs the same treatment as Playground/Zoo & Farm got: read every
     game's `Rules/*.cs` (and its `App/Screens/*Screen.cs` if the Rules file doesn't make the
     sprite keys obvious) to find every `EvaUi.Sprite("...")`/sprite-key-prefix string it
     constructs, work out the full unique art inventory, batch it into sheets of roughly 5-15 items
     each (see "The sheet workflow" below), and write prompts to a new
     `art/eva/<building>/PROMPTS.md`, same shape as the other two.

## The sheet workflow (how the gameplay-art layer actually gets built)

This is the process that produced everything in layer 3 above, and should be reused as-is for the
remaining buildings:

1. **Find what art a building's games need.** Read the `Rules/*.cs` file(s) for that building's
   games. Sprite keys are always string-built (e.g. `"zoofarm/animal_" + a.Id`), usually with a
   comment nearby saying so — grep for `Sprite`, `sprite key`, or the building's own name-prefix.
   Cross-check against `MatchRoundBuilder`/similar shared presenters if the Rules file just passes a
   prefix through rather than building keys directly. List every unique sprite name needed.
2. **Batch into sheets.** One `node`/ChatGPT image generation per sheet, ~5-15 items each (up to 29
   has worked for simple single-object icons; richer scene-style content works best smaller, ~8).
   Group logically (e.g. "2 pool entries per sheet" for Which Doesn't Make Sense's 40 scenes).
3. **Write the prompt** into that building's `art/eva/<building>/PROMPTS.md`, following the style
   guide already established (soft polished 3D-look children's illustration, warm rounded shapes,
   thin brown outlines, no text/letters, no people; magenta `#ff00ff` background, or let the tool
   use real transparency if it offers one — both work with the cutter). Give Adrian the literal
   prompt text to paste into ChatGPT.
4. **He pastes back the generated sheet image** (arrives as a message attachment, saved somewhere
   under `/tmp/claude-*/.../images/*.webp` — check the message for its `source:` path). Save it into
   the repo as `art/eva/<building>/ai/sheet_<name>.png` (convert from webp with PIL/Pillow —
   `pip install Pillow` if not already available in the container).
5. **Add a `SHEETS` entry** to `tools/art-import/cut-sheets.js` for that sheet: `file`, `dir` (source
   folder under `art/eva/`), `names` (the sprite names in the sheet's reading order — left-to-right,
   top-to-bottom), `outDir` (staging copy under `art/eva/<building>/out/...`), `resDir` (destination
   folder name under `Resources/Art/`), `size` (512 has been standard; 256 for small UI icons). Set
   `bg: 'alpha'` instead of the default magenta chroma-key if the sheet already came back with a
   real transparent background (check a corner pixel's alpha with PIL — `img.getpixel((2,2))`).
6. **Run it**: `node tools/art-import/cut-sheets.js <key> --install`. It blob-detects each item,
   crops it to a square sprite with padding, and writes it both to the `out/` staging copy and (with
   `--install`) straight into `Resources/Art/<resDir>/<name>.png`. Check the console output: it
   should report exactly as many items as `names.length`, in the right names — if it reports fewer,
   some cells merged into one blob (usually because two items in the sheet ended up too close
   together, e.g. the "mother animal" sheet's first two attempts drew a baby right next to the
   mother in every cell, which both looked wrong content-wise *and* bridged the magenta gap between
   cells). Don't `--install` a bad cut; delete the bad output and either accept a redraw or ask
   Adrian to regenerate with a clearer prompt.
7. **Verify + commit + push** every batch as its own commit, immediately — don't batch multiple
   sheets into one commit, and don't wait until a whole building is done to push. Adrian is
   following along in real time and expects each result pushed right after it's cut.

Useful established facts from doing this three-plus times:
- Unity auto-configures any new PNG under `Resources/Art/` as a sprite on first import
  (`EvasLearningWorld/Assets/Editor/EvaArtImporter.cs`, an `AssetPostprocessor`) — nothing to do
  there manually.
- `tools/art-import/` needs `npm install` once per fresh container (for `sharp`); Pillow needs
  `pip install Pillow` once too (used for webp→PNG conversion and quick pixel checks, not part of
  the committed tooling).
- If ChatGPT ignores a "no X" instruction (it did twice for "no baby animal" on the mother sheet),
  don't keep re-prompting blindly — ask Adrian directly rather than guessing at a fourth prompt
  variant; he may already be re-trying on his end.
- When a sheet is rejected/bad, clean up its unstaged output (`rm` the sheet PNG, the `out/`
  subfolder, and any `--install`ed files) before moving on, so `git status` stays legible for the
  next commit.

## Code/voice status (unchanged from before this session; still accurate)

**M4 is complete in code.** All ten buildings have their full game lists built: Rules generators,
difficulty ladders, help ladders, screens/presenters, tests. See
`docs/kids-games/full-catalogue-plan.md` for the per-building game list and every design call made
to simplify a game to an existing presenter (MATCH/SORT, SEQUENCE, ASSEMBLY, TRACE, MEMORY BOARD,
SEQUENCE RECALL) instead of a bespoke mechanic — those are flagged there for Adrian's review, not
decided unilaterally.

**Voice audio is complete: all 1022 lines have a generated `.mp3`.**
`EvasLearningWorld/Assets/Eva/Resources/Voice/voice-lines.txt` has every line every building's code
references. `Resources/Voice/en/` has a matching `.mp3` for every one of those 1022 keys.

Missing sprites silently render as a colored placeholder rectangle (`EvaUi.Sprite`,
`Assets/Eva/App/Ui/EvaUi.cs:53`), so the whole game already runs end-to-end even with most of M4's
art still missing — it just looks like placeholders in the areas not yet covered above.

### Generating voice audio (only if more lines get added later)

```
NODE_USE_ENV_PROXY=1 node tools/voice/generate.js EvasLearningWorld/Assets/Eva/Resources/Voice/voice-lines.txt EvasLearningWorld/Assets/Eva/Resources/Voice/en
```

- `NODE_USE_ENV_PROXY=1` is required in a cloud session that has Google TTS proxy credentials set
  up for this project's environment. If that isn't available, the script also accepts a
  `GOOGLE_TTS_API_KEY` env var instead. **Never commit the key, print it, or put it in a file.**
- Only generates clips whose `.mp3` doesn't already exist — safe to re-run after adding new lines.

## Working conventions worth knowing

- **Files meant for Adrian go in git**, not anywhere ephemeral — commit and push every prompt doc,
  every generated sprite, every tool change, right away.
- **This repo has no Unity project files usable in a cloud container** — don't try to run Unity,
  build an APK, or run the NUnit tests here; that's Adrian's own machine or Remote Control.
- **One commit per sheet/batch**, pushed immediately (see step 7 above).

## Opening prompt for the new session

Adrian will paste something like the block below to start the new session.

---

> Read `docs/kids-games/m4-handover.md` in full first. Zoo & Farm (including Geography) is done for
> gameplay art. Next up: the same treatment for Science Lab, then Workshop, Art Studio, Brain Gym,
> Friends' Park, Arcade. Confirm you've read it, then start on Science Lab: read its `Rules/*.cs`,
> work out the art inventory, batch it, write `art/eva/sciencelab/PROMPTS.md`, and give me the first
> prompt to paste into ChatGPT.
