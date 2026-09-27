# M4 art & sound audit (2026-09-27)

Checked against `origin/claude/eva-m4-full-content-82lmj0` (commit `370ec02`, PR #1's head at time of writing — the branch also matching PR #5 up to Art Studio).

## How assets are loaded (so you know what "missing" looks like in-app)

- **Sprites**: `EvaUi.Sprite(name)` (`Assets/Eva/App/Ui/EvaUi.cs:53`) loads `Resources/Art/<name>`. If the file isn't there, it silently falls back to a **generated placeholder**: a rounded rectangle whose color is derived from the name string. No error, no visible gap — just a colored blob instead of art.
- **Voice**: `Voice.Say(key)` (`Assets/Eva/App/Audio/Voice.cs:39`) loads `Resources/Voice/en/<key>.mp3`. If missing, playback is **silently skipped**, but Eva still "speaks" for a computed duration (`0.06s` per character of the line's text from `VoiceLines.cs`) so the speech bubble and game flow time out the same as if audio played. So a build with zero audio still runs correctly, just silently.

Net effect: **the app is fully playable right now with no art or sound generated at all** — nothing crashes or blocks, it just looks/sounds like placeholders throughout every M4 building.

## Building (map) images

`tools/art-import/places-layout.json` defines tap positions on the world map for all 10 buildings, but actual art (`Resources/Art/world/place_<id>.png`, `<id>_icon.png`, `<id>_bg.png`, `road_<id>.png`) only exists for buildings that predate M4:

| Building | Map art present? |
|---|---|
| House | ✅ (`place_house`, `house_bg`, `house_icon`) |
| School | ✅ (`place_school`, `school_bg`, `school_icon`, `school_list_bg`, `road_school`) |
| Store | ✅ (`place_store`, `store_bg`, `store_icon`, `road_store`) |
| Playground (M4.1) | ❌ none |
| Zoo & Farm (M4.4) | ❌ none |
| Science Lab (M4.5) | ❌ none |
| Workshop (M4.6) | ❌ none |
| Art Studio (M4.7) | ❌ none |
| Brain Gym (M4.8) | ❌ none |
| Friends' Park (M4.9) | ❌ none |
| Arcade (M4.10) | ❌ none |

**8 of the 10 M4 buildings have no map/building image at all.** Each one renders as a placeholder blob at its map position and shows a placeholder background when entered (screens fall back to reused backgrounds like `world/school_bg` or `world/playground_bg` where code hardcodes one, otherwise a generated blob).

## Per-game menu tiles (and in-game sprites)

Every activity is registered with an `IconSprite` key of the form `"activities/<id>"` (`Assets/Eva/Rules/Activities.cs`), used for its menu tile in `BuildingScreen.cs`. Counted **119 activities** defined across all buildings (plus 4 more Store entries via a separate `StoreActivitiesScreen` list) — effectively all of M1–M4's content.

`Resources/Art/activities/` contains exactly **one file: `count.png`** (School's Count game, predates M4). Every other activity's menu tile — all ~118 M4 (and earlier) activities — renders as a placeholder rectangle. Same story for in-game sprites: code references sprite folders like `words/`, `wordtoimage/`, `letters/`, `tangram/`, `fingermaze/`, `zoofarm/`, `sciencelab/`, etc., and **none of those folders exist** under `Resources/Art/` — only `cat/`, `characters/`, `house/`, `icons/`, `objects/`, `world/`, and the single `activities/count.png` do, all pre-M4 generic/reusable art (icons, the cat companion rig, character creator parts, a few household objects).

## Voice / sound

`Resources/Voice/en/` has **55 `.mp3` files**, all for the pre-M4 Count game (`activity_count`, `count_intro`, `count_q_apple`, etc.) — nothing has been synthesized for any M4 content.

There **is** a text manifest at `Resources/Voice/voice-lines.txt` with 532 line entries (key + spoken text, ready to feed the TTS generator) — and it does cover a good chunk of M4: Science Lab (124 lines), Zoo & Farm (52), plus scattered entries for Playground/School games (pattern, jigsaw, tangram, addition, subtraction, etc.) and Store. But **Workshop, Art Studio, Brain Gym, Friends' Park, and Arcade have no entries in the manifest at all** — their line text hasn't even been drafted yet, let alone recorded.

So: manifest text exists for part of M4, but **zero audio has actually been generated for any M4 building** — the `tools/voice/generate.js` script that turns the manifest into `.mp3`s was never run (it needs a `GOOGLE_TTS_API_KEY` env var, which isn't part of any commit).

## What it would take to fill the gaps

1. **Voice**: write the missing manifest entries (Workshop/Art Studio/Brain Gym/Friends' Park/Arcade — plus a review pass on the existing 532 lines), get a `GOOGLE_TTS_API_KEY`, then run `node tools/voice/generate.js Resources/Voice/voice-lines.txt Resources/Voice/en`. This is scriptable/automatable in bulk once the manifest is complete and the key is available.
2. **Building map art**: 8 buildings need a place icon + background + road art, matching the visual style of House/School/Store. `tools/art-import/import-map-art.js` / `map-art.js` exist as the pipeline but currently expect **hand-authored or AI-generated source images already sitting in a local Downloads folder** (`tools/art-import/import-activity-art.js` is hardcoded to `C:/Users/schei/Downloads/...` per-asset) — i.e. these tools cut and composite art someone already produced, they don't generate art themselves. So this is real design/art work, not a script run.
3. **Menu tiles + in-game sprites**: ~118 activities' tile art, plus per-game in-game sprites (words, images, tangram pieces, letters, animal art for Zoo & Farm/Science Lab, etc.) — the same story: the `art-import` tooling composites/frames pre-made source art, it doesn't create it. This is the largest chunk of remaining work and needs either commissioned/AI-generated art per asset or a decision to keep some games on generic/shared art longer.

**Bottom line: nothing described as "placeholder" in the building threads has been replaced.** All M4 art and audio is still outstanding; only pre-M4 assets (Count game, generic icons/objects/characters, House/School/Store map art) exist in the repo.
