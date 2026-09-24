# Spike: real-cat Eva rig (M3 Task 1)

Status: built and installed on the phone (Galaxy S25 Ultra); awaiting the user's approval. Spike code and art are uncommitted; the spike code is throwaway.

## Verdict

Layered pose-set cutout with code/clip-driven part motion. Not a skeletal or skinned quadruped: one front-facing sitting ("Egyptian sit") pose, parts moved by rotation/scale/offset. Cheap, reads as a cat at 560 px, and matches the existing `CharacterRig` cutout approach.

## What was built

- `art/eva/cat/gen.js`: throwaway seeded generator drawing the cat as 11 SVG layers on one shared 1000x1000 canvas. Black, fluffy (between shorthair and Persian), yellow-green eyes, dwarf proportions, normal long tail lying on the ground beside the cat. No white chest patch (removed at the user's request). Rendered with `tools/svg2png` into `Assets/Eva/Resources/Art/cat/` (existing `EvaArtImporter`, load with `EvaUi.Sprite("cat/cat_body")`). `art/eva/cat/preview/preview.svg` stacks all layers for review.
- `Assets/Spikes/Cat/CatSpike.cs` + `Editor/CatSpikeBuild.cs`: code-driven rig, no Animator. Buttons: TALK (toggle), CHEER, GREET, BG (Map/School/House/Store), MAP/CNT (placement), ANGRY.
- Build: `EVA_SCENES="Assets/Spikes/Cat/CatSpike.unity" bash tools/build-eva-debug.sh` succeeds, 0 errors. Normal build unaffected.

## Minimum layer list for Task 2 (11 layers) and pivots (art coordinates, 1000x1000)

| Layer | Pivot | Motion |
|---|---|---|
| shadow | none | static |
| tail | (700, 915) | slow sway +-2 deg idle, livelier when talking, lash +-14 deg at ~6 Hz when angry |
| body | (500, 940) | breathing, scaleY +-1.2% at 0.25 Hz; squash/stretch in hops |
| legL, legR | (452, 740) / (548, 740) | scaleY -22% and +-10 deg tuck in the air during cheer |
| chest | body-attached | follows body |
| earL, earR | (390, 320) / (610, 320), sit behind the head image | random single-ear twitch every 3-6 s; perked on cheer; angry: 9 deg out, height 80% |
| head | (500, 510) | sway +-1.2 deg, bob while talking, tilt on greet, dropped 10 px when angry |
| eyes | (500, 380) | blink every 3-5 s; narrowed to 75% when angry |
| mouth | (500, 478) | overlay flapping irregularly while talking |

Rig scale: art ground at y=940, root pivot y 0.06, displayed ~560 px tall in the 1440x900 canvas (same as `EvaHeight`).

## Motion summary

- Idle: breathing, tiny head sway/bob, ear twitch, blink, slow tail sway.
- Talk: mouth flap, tiny head bob, livelier tail.
- Cheer: two hops, squash/stretch, ears perked, front legs tucked.
- Greet: two small hops with a head tilt. Cats cannot wave, so `Wave()` becomes this bounce.
- Angry / wrong answer (1.6 s): tail lash, ears 9 deg out at 80% height, eyes narrowed, head dropped.

## Angry-pose history (still not confirmed on the device)

Ears out 14 deg read as "alert". 24 deg plus narrower eyes was much worse (airplane-wing ears, exposed the top of the head because the ear pivot sits at the ear base behind the head image). Latest build (9 deg, scale 0.8, head -10 px, eyes 0.75, fast tail lash) has not been judged. Fallback if still poor: tail lash + narrowed eyes + brief head lower, no ear rotation.

## Placement notes

- Eva appears only on Map and Count (`RigFactory` users: MapScreen, CountScreen, CreatorScreen player).
- The long tail clips at the screen edge with the current Map offset `(-260, 40)` (`MapScreen.cs:21-23`). The spike uses `(-350, 40)`. Production Map layout needs the same ~90 px left shift, or a shorter tail.
- Count: center anchor `(520, -120)`, root mirrored with negative x scale to face left (`CountScreen.cs:22-23`).

## Known art imperfections

- Body outline still slightly regular (sawtooth).
- Single front-facing pose only.
- Ears slightly tall.
- Legs a bit uniform/flat against the body.
- Mouth overlay is old amber-era art; check it still matches.

## Recommendation for production Eva

Keep the `CharacterRig` API (`Root`, `Animator`, `Wave()`, `Cheer()`, `SetTalking(bool)`, `ApplyLook`) with `Wave()` re-implemented as the greeting bounce, and add an angry/wrong-answer reaction. The existing `BigCheer` scale punch on the rig root (`CountScreen` ~line 733, `_evaBaseScale`) still works. Add a second stand/leap pose only if the user finds the sit-pose bounce does not read as a cat cheering. A full cutout quadruped is not justified unless walking is wanted.
