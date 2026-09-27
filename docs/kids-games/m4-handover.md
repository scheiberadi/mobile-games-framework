# M4 handover — for a fresh session

Rewritten 2026-09-27, superseding the previous version of this file. Read this whole file before
doing anything else, **including the "Session-start gotcha" and "Things that went wrong" sections
below** — they're not optional background, they're what stops you repeating mistakes the last
session made. Nothing from any prior session's memory carries over.

## Session-start gotcha: verify your checkout before trusting anything

The last session opened on a stale local checkout — `git status` reported "up to date with
origin" but that was a lie from before the previous session's final pushes landed. It spent real
effort concluding this very file didn't exist, before Adrian pointed at the exact commit on
`origin/claude/eva-m4-full-content` that proved it did. **Don't trust your initial checkout.**
First command, before reading anything else:

```
git fetch origin claude/eva-m4-full-content && git checkout -B claude/eva-m4-full-content origin/claude/eva-m4-full-content
```

Then read this file. If it still looks stale or contradicts what Adrian says, fetch again before
concluding anything is missing.

## Where things stand

Branch: `claude/eva-m4-full-content`. Push directly to it — no separate PR branch dance needed as
of this writing (check `git branch -r` / any open PR before assuming that's still true). HEAD at
the time of writing: `5add5cc`.

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
   **This is what every session so far has been doing, building-by-building, and it's the one with
   real progress:**
   - **M4.1 Playground: fully done.** All 9 games that need custom art have it. Prompts + process
     notes: `art/eva/playground/PROMPTS.md`.
   - **Known but deliberately deferred bug**: `Rules/Jigsaw.cs` reuses the same sprite keys
     (`jigsaw/piece_<row>_<col>`) across every difficulty level even though each level's grid is a
     different size (2x2 up to 5x5) — so at low levels the child currently sees a mismatched
     corner-crop of the picture instead of the whole thing sliced correctly. Adrian said "for
     later" when this was flagged — don't fix it unless he asks again.
   - **M4.4 Zoo & Farm: fully done**, all 11 games (the 10 animal games plus Geography, 28 images:
     11 flags, 6 continents, 11 landmarks). Prompts + process notes: `art/eva/zoofarm/PROMPTS.md`.
   - **M4.5 Science Lab: 13 of 17 batches done** (see `art/eva/sciencelab/PROMPTS.md` for the full
     17-batch plan, already written). Done: batch 1 (Sink or Float objects), batch 2 (Magnet
     objects), batch 3 (Living vs Non-Living objects), batch 4 (Healthy vs Unhealthy foods), batch 5
     (8 sorting buckets), batch 6 (Human Senses organs + symbols), batch 7 (8 weather scenes),
     batch 8 (15 small choice icons: clothing/measure-levels/seasons/day-night), batch 9 (8 cause
     scenes), batch 10 (8 effect scenes — two items, light_on and popped_balloon, needed a redo after
     Adrian's feedback; see "Things that went wrong" #5 and #6 for the grid-mode and chroma-key
     pitfalls hit on this pair), batch 11 (9 cooking-measure cups — the prompt laid the sheet out by
     ingredient, water/flour/milk × full/half/empty, but `CookingMeasuresItems` groups ids by
     fill-level bucket instead, a/b/c=full, d/e/f=half, g/h/i=empty; the `SHEETS.sciencelab_cups`
     entry in `cut-sheets.js` already has the remapped `names` array, clean cut otherwise, no
     surprises), batch 12 and batch 13 (Seasons' full 12 activity scenes, both halves — clean cuts,
     no fragment/fringe issues; Seasons is now fully art-covered). **Still to do: batches 14-17** —
     Day/Night's 10 activity scenes (2 sub-batches), Space objects, Plant Growth's 5 stages. The
     prompt text for every remaining batch is already written in `art/eva/sciencelab/PROMPTS.md` —
     just paste batch 14's prompt to Adrian next, no re-planning needed. **Adrian hit his ChatGPT
     plan limit at the end of this session** ("till tomorrow") — the fresh session picking this back
     up should expect Adrian may not be able to generate images right away; don't assume the limit
     has reset, ask or wait for him to send the next sheet rather than prompting immediately.
   - **The other 5 buildings — Workshop (M4.6), Art Studio (M4.7), Brain Gym (M4.8), Friends' Park
     (M4.9), Arcade (M4.10) — haven't been looked at at all** for this gameplay-art layer. Each
     needs the same treatment as the buildings above: read every game's `Rules/*.cs` (and its
     `App/Screens/*Screen.cs` if the Rules file doesn't make the sprite keys obvious) to find every
     sprite-key prefix it constructs, work out the full unique art inventory, batch it into sheets,
     and write prompts to a new `art/eva/<building>/PROMPTS.md`, same shape as the others.

## The sheet workflow (how the gameplay-art layer actually gets built)

This is the process that's produced everything in layer 3 above, refined session over session —
read the whole thing, not just the happy path, since most of it exists because something broke
before the fix was added.

1. **Find what art a building's games need.** Read the `Rules/*.cs` file(s) for that building's
   games. Sprite keys are always string-built (e.g. `"zoofarm/animal_" + a.Id`), usually with a
   comment nearby saying so — grep for `Sprite`, `sprite key`, or the building's own name-prefix.
   Cross-check against `MatchRoundBuilder`/similar shared presenters if the Rules file just passes a
   prefix through rather than building keys directly. List every unique sprite name needed.
2. **Batch into sheets.** One ChatGPT image generation per sheet, ~5-15 items each (up to 15 has
   worked fine for simple single-object icons; richer scene-style content works best smaller, ~6-8).
   Group logically (e.g. "2 pool entries per sheet" for Which Doesn't Make Sense's 40 scenes).
3. **Write the prompt** into that building's `art/eva/<building>/PROMPTS.md`, following the style
   guide already established (soft polished 3D-look children's illustration, warm rounded shapes,
   thin brown outlines, no text/letters, no people). **Always ask for magenta explicitly and rule
   out fake transparency** — see "Things that went wrong" below for why; every prompt should end its
   background sentence with something like "not a checkered/transparent placeholder, an actual
   solid magenta fill". Give Adrian the literal prompt text to paste into ChatGPT.
4. **He pastes back the generated sheet image.** In a *cloud* session (this one), the image only
   comes with a usable `source: /tmp/claude-*/.../images/N.webp` path when it arrives as a fresh,
   standalone message — see "Things that went wrong" #1 below if a paste arrives without one. Save
   it into the repo as `art/eva/<building>/ai/sheet_<name>.png` (convert from webp with
   PIL/Pillow — `pip install Pillow` if not already available in the container):
   ```
   python3 -c "
   from PIL import Image
   img = Image.open('<source path>').convert('RGBA')
   img.save('art/eva/<building>/ai/sheet_<name>.png')
   print('size:', img.size)
   print('corner (2,2):', img.getpixel((2,2)))
   "
   ```
   **Check that corner-pixel print every time** — see "Things that went wrong" #2.
5. **Add a `SHEETS` entry** to `tools/art-import/cut-sheets.js` for that sheet: `file`, `dir` (source
   folder under `art/eva/`), `names` (the sprite names, in the order the cutter will find them —
   see the grid-mode note below), `outDir` (staging copy under `art/eva/<building>/out/...`),
   `resDir` (destination folder name under `Resources/Art/`), `size` (512 has been standard; 256 for
   small UI icons). Set `bg: 'alpha'` instead of the default magenta chroma-key only if the sheet
   genuinely came back with a real transparent background (mode has an `A` channel *and* the corner
   pixel's alpha is near 0 — both, not just one).
   - **Default to `grid: { cols, rows }` for anything richer than plain isolated object icons** —
     scenes, sheets with foliage/water/decorative sparkle elements, anything where neighbouring
     cells' content might visually touch, or where one item might be visually taller/wider than its
     cell (e.g. a tower whose base overhangs into the row below). Plain whole-image blob detection
     (the default, no `grid` key) is fine for sheets of small isolated icons with generous margins
     (objects, buckets, flags, buttons) — see "Things that went wrong" #4 for exactly what goes
     wrong without `grid` and why it's cheaper to default to it than to redo a cut after the fact.
6. **Run it**: `node tools/art-import/cut-sheets.js <key> --install`. It writes each cut sprite both
   to the `out/` staging copy and (with `--install`) straight into
   `Resources/Art/<resDir>/<name>.png`. Check the console output: it should report exactly as many
   items as `names.length` with no `expected N items, found M` warning — if it warns, stop, don't
   `--install`, and see "Things that went wrong" #4.
7. **Visually verify a few crops before committing** — `Read` at least 2-3 of the freshly cut PNGs
   under `out/`, including any that look suspicious (an unusual aspect ratio compared to its
   siblings is the tell — see #4 below). Don't rely on the console dimensions alone.
8. **Verify + commit + push** every batch as its own commit, immediately — don't batch multiple
   sheets into one commit, and don't wait until a whole building is done to push. Adrian is
   following along in real time and expects each result pushed right after it's cut.

## Things that went wrong (read before you hit the same wall)

**1. A pasted image without a `source:` path.** If Adrian's paste arrives merged into a "while you
were working" system envelope (i.e. you were mid-tool-call when he sent it), the image may show up
with no accompanying file path — there's nothing on disk to find, searching the container's whole
filesystem will come up empty, and there is no workaround. Ask him to paste it again as a plain,
standalone message (not while you're running another tool) — that's what makes the harness attach
the `source: /tmp/claude-*/.../images/N.webp` annotation you actually need.

**2. "Real transparency" comes back as a fake checkerboard.** Asked once for "transparent
background instead of magenta if your tool supports it," ChatGPT returned an image that visually
*looks* transparent (a grey/white checker pattern) but is actually a flat opaque RGB image with that
checker pattern baked in as literal pixels — no alpha channel at all. The cutter can't chroma-key a
checker pattern. Always check: `img.mode` should contain `A`, and the corner pixel's 4th value
should be near 0 (fully transparent), not 255. If either check fails, the "transparency" is fake —
reject the sheet and ask Adrian to regenerate with a prompt that asks for magenta *only*, explicitly
ruling out a checkered placeholder (see every prompt in the two finished buildings' PROMPTS.md files
for the exact wording now baked into all of them).

**3. A fill colour too close to magenta gets chroma-keyed away.** Geography's Oceania continent
came back with a purple fill; the chroma-key formula (`min(r,b) - g`) reads deep purple/lavender/
pink almost the same as magenta, so the cutter treated the continent's own fill as background and
keyed it out, leaving an outline-only sprite. If a prompt asks the model to pick its own colour for
something (a fill, a swatch, "a different colour per item"), the prompt should steer away from
purple/magenta/pink/lavender specifically, and every cut should be visually checked (not just
dimension-checked) before committing, since a keyed-out fill still produces a plausible-looking
bounding box.

**4. Touching or fragmented content breaks plain blob detection two different ways**, both of which
happened on richer sheets (landmark scenes, a busy weather icon, foods sitting close together):
   - **Merge**: two neighbouring cells' content visually touches (tree canopies, water, a decorative
     element) and the blob detector fuses them into one blob spanning both cells — this either
     shows up as an explicit `expected N found M` warning (fewer blobs than names), or silently
     produces one sprite with two food items stacked on top of each other in it (Science Lab's
     donut+pizza — same warning, easy to miss if you don't read it).
   - **Fragment**: a single item's own decorative detail (a sparkle, a wisp of a swirl, a stray
     highlight) sits just far enough from the item's main shape that the detector treats it as its
     *own* separate blob. This doesn't trigger a count mismatch in the obvious direction — it can
     produce *more* blobs than expected, silently shifting every subsequent item's name assignment
     by one slot in reading order (Science Lab's "windy" sprite ended up as two stray dots because a
     later item's sparkle fragment sorted into its position). **A found-count that's higher than
     expected is just as much a bug as one that's lower** — don't assume extra blobs are harmless.
   - **The fix both times**: `cut-sheets.js`'s `grid: { cols, rows }` mode (added this session,
     search the file for `gridBoxes`/`largestBlobInRect`). It cuts each nominal grid cell
     independently and keeps only the *largest* connected blob within it, so a small bleed fragment
     from a neighbour or a stray decorative bit never gets mistaken for the cell's real content. It
     also auto-widens the last item's cell through an incomplete row's unused trailing columns, so
     content that legitimately overflows past its own column (Sydney Opera House's boat did this)
     doesn't get clipped. **Given how often this comes up on anything richer than a plain isolated
     icon, don't wait for a mismatch warning to reach for `grid` — default to it up front per the
     rule in step 5 above.** It costs nothing on a sheet that wouldn't have needed it.

**5. `grid` mode's largest-blob rule can silently eat real content that's legitimately disconnected
from the item's main shape.** Hit this twice in the same session, both in Science Lab's Cause and
Effect batches:
   - Batch 9's "rain" cell: a cloud plus 11 individual raindrops drawn below it with a visible gap —
     each raindrop is its own tiny blob, none close to the cloud's size, so the cutter kept the cloud
     and dropped every drop.
   - Batch 10's redone "popped balloon" cell: a burst drawn as ~5 separate rubber scraps flying apart
     from a central point — the cutter kept only the single largest scrap and both nearby spark
     lines, dropping the other four scraps.

   No warning fires either time, because grid mode always finds exactly one blob per cell — this
   isn't a count mismatch, just a silently wrong crop. **Actually look at every cut, not just count
   them** (step 7 already says this — this is why). If a cut is missing real content that was
   visibly disconnected in the source scene (rain from a cloud, a burst's flying pieces, anything
   drawn with intentional separation), don't change the shared `largestBlobInRect` gap/threshold —
   that'll risk merging unrelated neighbours on every *other* sheet using grid mode. Instead
   special-cut just that one sprite from the full cell's blob union (all non-bg content in that
   cell's rect, not just the largest component) with a one-off script, using the pipeline's *exact*
   `keyed()` chroma-key function (copy it verbatim, don't approximate it — a cruder hand-rolled key
   threshold left a faint magenta fringe on the first `cause_rain` attempt, caught and redone once
   the mistake was noticed) and the same crop/composite/resize steps as the pipeline (extract →
   composite onto a square canvas sized off `Math.max(w,h)*1.08` → resize to `spec.size`). See the
   commits that added Science Lab's `cause_rain` and `effect_popped_balloon` for the exact approach:
   compute each significant blob's bbox in the cell, union them, pass that box straight to a
   `cutBox()` helper.

**6. A soft painted glow/aura around an object doesn't chroma-key cleanly against magenta**, even
with the pipeline's partial-alpha fade formula. Science Lab's `effect_light_on` (redrawn as a light
bulb after Adrian rejected a glowing-switch first pass) had radiating rays plus a soft pink-tinted
halo *around* the bulb glass — the halo's colour blends gradually into magenta so it partially
survives keying as a visible violet-pink ring, worse than the usual few-pixel edge fringe because
the blend band is wide (a deliberate gradient, not just anti-aliasing). Fixed by dropping the
faintest remaining halo pixels (low alpha) to fully transparent after cutting — the rays alone still
read as "glowing" without the halo, so nothing essential is lost — but a thin trace can remain since
color and "is this the unwanted halo vs. the object's own highlight" aren't cleanly separable by
pixel colour alone. **Prefer avoiding this up front**: for any future icon with an intentional
glow/radiance effect, add "no soft glow or halo blending into the background, just the object and
[rays/sparkles/etc.] on a clean magenta background" to the prompt, same spirit as the checkered-
placeholder and magenta-adjacent-fill rules above.

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
- **One commit per sheet/batch**, pushed immediately (see step 8 above).
- `tools/art-import/` needs `npm install` once per fresh container (for `sharp`); Pillow needs
  `pip install Pillow` once too (used for webp→PNG conversion and quick pixel checks, not part of
  the committed tooling).
- If ChatGPT ignores a "no X" instruction (it did for "no baby animal" on Zoo & Farm's mother
  sheet, twice), don't keep re-prompting blindly — ask Adrian directly rather than guessing at a
  fourth prompt variant; he may already be re-trying on his end.
- When a sheet is rejected/bad, clean up its unstaged output (`rm` the sheet PNG, the `out/`
  subfolder, and any `--install`ed files) before moving on, so `git status` stays legible for the
  next commit.

## Opening prompt for the new session

Adrian will paste something like the block below to start the new session.

---

> Read `docs/kids-games/m4-handover.md` in full first — including the session-start gotcha at the
> top (fetch and re-checkout the branch before trusting anything) and the "things that went wrong"
> section. Science Lab is 13 of 17 batches in; the remaining batches' prompts are already written in
> `art/eva/sciencelab/PROMPTS.md`. Confirm you've read it, then send me batch 14's prompt (Day/Night
> activities, part A) to paste into ChatGPT.
