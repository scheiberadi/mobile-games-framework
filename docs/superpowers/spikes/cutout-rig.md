# Spike: cutout character rig

## Question

Is a simple hierarchical cutout character (sprites parented under body parts, animated by Animator clips, with swappable parts) cheap, reliable and smooth enough for Eva and the player character? No deformable bones, no skinning, no extra 2D packages.

## Verdict

PASS. The user's tap check passed (buttons work after the input handler fix; the wave looked good at 120 fps). Finding: at a 60 fps cap the wave feels choppy although no frame is dropped, so the app must request 120 fps with vsync off. See "Final verdict" at the end and the per-round sections.

## Decision

- M1 builds Eva and the player character from this pattern with real SVG-derived parts: a root with an Animator, body parts as child SpriteRenderers nested under the torso, clips authored from code or in the editor, and part swaps (head and others) done by replacing `SpriteRenderer.sprite`. No 2D animation or skinning package is needed.
- M1 must request 120 fps with vsync off on 120 Hz phones (the bootstrap's fixed 60 is the wrong default) and choose and measure a battery policy.
- M1 buttons are icon-only (a raised-arm glyph for wave, a face swatch for head swap), about 100 dp or more.
- The Eva project uses the Input System only (`activeInputHandler` 1, set by `EvaProjectSetup.Apply`, guarded by a test).

## Earlier record (kept, superseded by the sections above)

The two sections below are the state after the first screenshot, kept so nothing is lost.

### First verdict (after one screenshot)

PASS after the user tap check (see "User tap check" and "Final verdict" below, which supersede the rest of this section). The text that follows is the state after the first screenshot only and is kept as a record: what had been verified so far is only what one screenshot can show: the build runs on the Samsung Galaxy S25 Ultra, the rig draws with the right sorting and pivots at its rest pose, and the smoothed fps read 60.1. The on-screen torso height read 0.052 while the scene value is 0, which shows the Idle clip was evaluated at least once in the player build. It does not show that Idle loops or is smooth. What still needs a person is the motion itself: that Idle loops smoothly, that Wave raises the arm, swings it and returns to Idle, that fps stays about 60 during a Wave, and that the head button cycles three heads. The user's tap results are now recorded in "User tap check" below.

Verification split (state after the first screenshot; the later fps readings of 60.1, 120.2 and the user's results are in the sections below):

- Verified by the agent from one screenshot of the final build: rig layout and sorting, top-pivot arms hanging from the shoulders, 260 by 260 canvas-unit buttons on the right, fps text 60.1, state Idle, torso y 0.052 (Idle evaluated at least once), arm z 8 (the rest value, so it says nothing about Wave), head 1 of 3.
- Not verified by the agent: any actual motion or looping, the Wave state, Wave returning to Idle, fps during a Wave, head swapping (only head 1 was ever seen). The phone dropped off adb right after the first screenshot, so no further screenshots (which would have shown changing values) could be taken.
- To be reported by the user: the tap results for WAVE (raises, swings, returns to Idle) and HEAD, and whether the motion looks smooth.

### First decision (conditional)

Superseded in part by the "Final verdict" below (M1 must request 120 fps, not about 60). Originally conditional on the user's tap check passing (Idle loops, Wave returns to Idle, heads swap, fps about 60): M1 builds Eva and the player character from this pattern with real SVG-derived parts: a root with an Animator, body parts as child SpriteRenderers nested under the torso, clips authored from code or in the editor, and head (and other part) swaps done by replacing `SpriteRenderer.sprite`. No 2D animation or skinning package is needed.

## What was built

All files are disposable and live in `EvasLearningWorld/Assets/Spikes/Rig/`.

- `Editor/RigSpikeBuild.cs`, run in batchmode with `-executeMethod RigSpikeBuild.CreateScene`. It writes placeholder sprites as PNGs (anti-aliased rounded shapes with a dark outline; a torso, an arm, a leg and three faces), imports them as Sprites at 100 pixels per unit, builds the hierarchy, writes the clips, builds the controller and saves the scene. It reuses `EvaSpikeScenes.Create`.
- Hierarchy: `Rig` (Animator) with `Torso` (sorting order 10) and, under the torso, `Head` (30), `ArmL` (5) and `ArmR` (5). `LegL` and `LegR` (0) are direct children of the root so the feet stay planted while the torso bobs. Arms and legs use a custom sprite pivot of (0.5, 1), the top centre, so they rotate at the shoulder and hip.
- `Idle.anim` (looping, 1.2 s): `Torso` `localPosition.y` bobs between 0 and 0.08, and `Torso/Head` `localEulerAnglesRaw.z` tilts between -2 and 2 degrees. `Wave.anim` (1.6 s, not looping): `Torso/ArmR` `localEulerAnglesRaw.z` goes from the rest angle of 8 up to 150, swings between 120 and 150 three times, and returns to 8. Both were written with `AnimationClip.SetCurve` and smoothed tangents.
- `RigController.controller`: a `Wave` trigger, default state `Idle`, Idle to Wave on the trigger (no exit time, 0.1 s blend), Wave to Idle at exit time 1 (0.1 s blend). The controller and both clips are saved as assets and referenced from the scene, so they are in the player build. The build was made with only this scene (`EVA_SCENES`).
- `RigSpike.cs`: removes the app bootstrap canvas and event system, creates a canvas with `UiFactory`, and adds buttons on the right side, away from the 96 px left safe-area inset (first build: two 260 by 260 canvas-unit buttons, WAVE and HEAD; the current build has three 240 by 240 unit buttons, WAVE, HEAD and FPS, about 90 dp or more on this phone, and a longer readout, and the rig is moved 6 units to the right so the readout does not cover it). The text labels are a deviation from the audience baseline that buttons need no reading; it is acceptable only because an adult tests this disposable spike. M1 buttons must be icon-only (a raised-arm glyph for wave, a face swatch for head swap). Size derivation: reference resolution 1600 by 900 with match height gives 1.2 screen pixels per canvas unit on the 2340 by 1080 screen, so 260 units is 312 px (240 units is 288 px), roughly 105 to 120 dp (about 95 to 110 dp for 240 units) on the S25 Ultra depending on its reported density; "about 100 dp" is conservative. The top-left text (since extended, see the round 3 and round 4 sections) shows smoothed fps, the current state, tap count, head index, torso height and arm angle. HEAD cycles three head sprites; WAVE sets the trigger.

## Evidence

- Unity batchmode scene creation succeeded (`RIG_SCENE_CREATED`), and the debug APK build reported `BUILD_RESULT: Succeeded` with 0 errors. `tools/check-apk-compliance.sh` reported no AD_ID and a launcher activity. No package was added, and `ComplianceTests` was not touched.
- The final scene screenshot shows a smiling yellow head in front of a blue torso, orange arms hanging at a slight outward angle from behind the torso, and purple legs below. Nothing is cut off and the arms hang from their top pivots at rest (rotation about the top is expected, not yet seen).
- fps reading on the device: 60.1 smoothed (the app sets `Application.targetFrameRate` to 60). This is one on-screen reading taken about 6 seconds after launch, while idle. No reading was taken during a Wave.
- Edit-mode suite for the Eva project after the spike: 74 of 74 passing.

## Finding: dead buttons on the first install (input handler)

The first install rendered and ran at 60 fps, but the user's taps on WAVE and HEAD did nothing. Cause: `EvasLearningWorld/ProjectSettings/ProjectSettings.asset` had `activeInputHandler: 0` (old Input Manager only), while `UiFactory.CreateCanvas` creates an `InputSystemUIInputModule` (Input System 1.20.0 is in the Eva manifest). With handler 0 that module receives no touch. The Sudoku project has `activeInputHandler: 1` and works. This is confirmed from the setting itself; the rebuilt app's tap behaviour is still to be confirmed by the user.

Fix, kept in project setup rather than the spike: `EvaProjectSetup.Apply` now sets `activeInputHandler` to 1 (Input System only) through a `SerializedObject` on `ProjectSettings/ProjectSettings.asset`, idempotently, and a new edit-mode test `ProjectUsesTheInputSystemOnly` asserts it (RED with 74 of 75 passing before the fix, GREEN with 75 of 75 after). Running `Apply` in batchmode wrote the value to the file in a single start. For M1: all Eva UI needs handler 1, and any new Eva project or fresh clone should run `Apply` once before building. Only that order was verified (`Apply` run alone, then the build); whether a build that itself runs `Apply` from handler 0 yields an Input System player is unverified, and the APK's input backend was never inspected.

After the fix, a second screenshot of the rebuilt app showed the same layout, fps 60.1 and torso y 0.007. The first screenshot read 0.052, so the value changed between two launches, which is consistent with Idle running, but neither shows a loop or smooth motion.

## Finding: the wave does not feel like 60 fps (investigation, fix round 3)

Report: with the buttons working, the user said the arm wave "definitely doesn't feel like 60fps". Until then the only number was the idle reading of 60.1 fps; nothing had been measured during a wave.

What was checked:

- Animation data (from `Wave.anim` and `Idle.anim` on disk): Wave has one Euler curve on `Torso/ArmR` with 7 keys at 0, 0.3, 0.55, 0.8, 1.05, 1.3 and 1.6 s, free (smooth) tangent mode with computed slopes (0 at the 120 degree troughs, near 0 at the 150 degree crest, about 470 degrees per second at the ends), sample rate 60, no stepped or linear keys. Idle has smooth curves too and `loopTime` on. So the data is not stepped or under-keyed; a choppy look from tangents is ruled out. One caveat about feel: the swing part is only 30 degrees amplitude in 0.25 s per half swing, so it is a small, fast motion, which can look jerky on a 60 Hz panel regardless of data.
- Frame pacing settings: the app bootstrap sets `Application.targetFrameRate` to 60 once, after the first scene loads; `QualitySettings.vSyncCount` is 1 (Android default quality level is Medium); Swappy (optimized frame pacing) is on; the Animator uses Normal update mode and Always Animate culling; the rig is 9 sprite renderers.
- On-device: the readout was extended with the panel refresh rate as Unity reports it, `targetFrameRate`, `vSyncCount`, the worst frame time in the last 2 s, and, since the last WAVE tap, the minimum instantaneous fps, the worst frame time and the count of frames over 25 ms. The idle screenshot of that build reads: panel 60 Hz, target 60, vsync 1, worst frame 16.6 ms. An earlier launch of the same build reported panel 30 Hz at start (the startup value, before Android settled on a mode), so Unity's refresh reading at launch is not stable; the readout now refreshes it every update.
- Fix attempt (spike only): `RigSpike` sets `Application.targetFrameRate` to the panel refresh rate (at least 60) at start, and a third button, FPS, toggles between that and a 60 cap so the user can compare. This round 3 toggle is superseded by the three modes of round 4 (60 cap, 120 with vsync off, 120 plus `SetResolution`). Because the panel was reported as 60 Hz, this did not change the target (60) in the idle screenshot. Whether the panel goes to 120 Hz during touch or a wave could not be checked without taps.

Result so far: cause not found. There is no evidence of stepped or sparse animation data and no idle frame-pacing problem (worst idle frame 16.6 ms at a 60 target). What is not known is what happens during a wave, and whether the panel really runs at 120 Hz while the app requests 60. The user should read the on-screen numbers after tapping WAVE (min fps, worst ms, count over 25 ms, panel Hz, target) and after pressing FPS. If the panel reports 120 Hz during use, M1 should set `Application.targetFrameRate` to the panel rate and design motion for that; if everything reads a clean 60 with no slow frames, the perceived choppiness is a motion-design matter (small fast swing) rather than a rendering one. Do not treat the rig as smooth until the user says so.

M1 decision to make: a frame-rate policy (fixed 60, or match the panel up to 120), decided from these readings. The bootstrap's fixed 60 is the current policy.

## Finding: 60 versus 120 (fix round 4)

The user's readings with the 60 cap: panel 60 Hz, target 60, vsync 1, worst frame 16.6 ms, min fps 60, no frame over 25 ms. Pacing at 60 is therefore clean, and the remaining gap in feel is most likely the panel: the S25 Ultra supports 120 Hz (spec knowledge, not measured here) and Settings > Display > Motion smoothness is Adaptive (user-reported), but the app was asking for 60 (the bootstrap sets `targetFrameRate` 60, and vsync count 1).

What was tried, in the planned order, stopping at the first that works:

- (a) `Application.targetFrameRate = 120` with `QualitySettings.vSyncCount = 0`. This worked on the first attempt: the first screenshot after launch reads measured fps 120.2 (one-second average of real frames), worst frame 8.3 ms, zero frames over 25 ms, target 120, vsync 0. The measured fps is the proof that Unity now produces about 120 frames a second (Unity frames, not confirmed presented frames); `Screen.currentResolution.refreshRateRatio` still reports 60 Hz, so that Hz readout is not trustworthy on this device and must not be used to decide the rate.
- (b) `Screen.SetResolution(width, height, fullScreenMode, RefreshRate 120/1)`: implemented as FPS mode 2, not needed to reach 120 measured fps because (a) already did; not exercised by the agent, but the user tried it and judged it good (see "User tap check").
- (c) Optimized frame pacing: the project's `androidUseSwappy` is already 1 (on), so nothing further to change; not touched.

Note that mode 0 after mode 2 is not a clean 60 baseline, because the 120 Hz `SetResolution` request is not undone when the cycle returns to mode 0 (`RigSpike.cs` `SetFrameRate`). The FPS button now cycles three modes: 0 is the 60 cap with vsync 1, 1 is the 120 request with vsync off (the launch default), 2 is 120 plus `SetResolution`. The readout shows the measured one-second average fps next to the requested target and the panel Hz, plus worst frame and the slow-frame count. What this screenshot cannot show is whether the wave now looks smooth; the user judges that, by comparing mode 0 and mode 1. Also unknown: whether frames are actually presented at 120 (the measured 120 counts Unity frames; with vsync off the swap could in principle outrun the panel), and how much battery 120 costs.

M1 frame-rate policy consequence: the app-wide 60 cap in `EvaBootstrap` is the wrong default if the user confirms 120 looks better. The option is to request 120 with vsync 0 (measured 120.2 fps, 8.3 ms worst frame, with a rig this small) and accept higher power use, and to expose it as a setting or drop back to 60 in menus and idle screens. A game like Eva's is mostly light 2D, so the cost is likely modest, but battery was not measured. Design animation for the higher rate, and never rely on `Screen.currentResolution` to detect it; use a measured frame time instead.

## Pivot and sorting gotchas

- The pivot has to be set on the sprite import, not by offsetting a child object. In the importer, set `spriteAlignment` to `Custom` and then set the pivot (in code: `ReadTextureSettings`, set `spriteAlignment = Custom` and `spritePivot`, `SetTextureSettings`, and also assign `importer.spritePivot`). Both were set here and the pivot came out at (0.5, 1) in the sprite's `.meta`; whether either alone would have been enough was not tested.
- The pivot is a fraction of the sprite, so arms, legs and their replacements must all be authored with the same convention. Head swaps are expected to hold here (not yet verified; only head 1 was seen) because all three heads are 200 by 200 with a centre pivot; a swapped part of a different size or pivot would jump. Real parts need a shared canvas size per slot or per-part offsets.
- Sorting order is absolute per renderer, not relative to the parent. Nesting under the torso does not put the arm behind or in front of it; only `sortingOrder` does. Without explicit orders, sprites at the same order and depth are sorted unpredictably. Limbs are at 0 and 5 and the torso at 10 so the limbs tuck behind, and the head at 30 stays in front.
- Arms should start inside the torso rectangle. The shoulder pivot then sits under the torso (which hides the rounded cap) and the arm is expected to look attached at every angle (only the rest angle has been seen; the raised angles are not yet verified).
- An Animator only writes properties that some clip in the controller animates, so it cannot restore a property another state changed. Wave ends on the rest angle so the arm is where Idle expects it. The default Write Defaults setting would also restore it, but not depending on that is safer. For M1, note the reverse effect: with Write Defaults on, the properties that only Idle animates (torso height, head tilt) snap back to their scene defaults for the duration of Wave, so the body stops bobbing while the arm waves. If that looks wrong, add those curves to Wave or use a layer or avatar mask for the arm.
- A trigger stays set until it is consumed, so mashing WAVE during a wave would queue a second wave that plays as soon as Idle returns. The spike ignores WAVE taps while the Wave state is active (`RigSpike.cs` `OnWave`). Anything that a child can mash needs the same guard. The guard is incomplete: during the 0.1 s Idle to Wave blend the current state is still Idle, so a second tap in that window can still set the trigger again and queue a second wave. M1 should also reset the trigger when Wave is entered or check the next state.
- `AnimationClip.SetCurve` with `localPosition.y` is supported by the on-device reading (torso y 0.052). Whether `localEulerAnglesRaw.z` drives the arm and head correctly on the device is not yet verified (the arm reading of 8 is only the rest value); it is expected to work. A clip only loops in an Animator if `loopTime` is set through `AnimationUtility.SetAnimationClipSettings`; `wrapMode` alone is legacy behaviour.
- Positive z rotation on an arm hanging from a top pivot swings the tip toward +x, so for the arm on screen right, raising it outward is positive z.
- The app bootstrap (once, after the first scene loads) creates an opaque full-screen canvas and an event system; the spike removes both in `Start`, otherwise the canvas would cover the sprites and there would be two event systems.
- Unity 6000.5 marks `FindObjectsByType(FindObjectsSortMode)` and `FindFirstObjectByType` as obsolete; use the overload without the sort mode.

## What was not built

No IK, no blending between more than two states, no per-part colour or outfit variants, no atlas, and no SVG-derived art. Those are M1 work. Frame time was only read as fps; a per-frame cost measurement of many rigs on screen was not done, and one rig with nine sprites is far below any concern for draw calls.

## User tap check (2026-09-21, on the S25 Ultra)

Reported by the user in their own words, not captured as artifacts:

- First install: both buttons did nothing. Cause: the Eva project used the old Input Manager while `UiFactory` builds an Input System UI module; fixed by setting the Input System as the only input handler in `EvaProjectSetup.Apply` (with a test). After the fix the user confirmed "both buttons work".
- WAVE and HEAD: both work. The user did not separately describe that the wave returns to Idle or that three distinct heads cycle; those two details are therefore recorded as not explicitly reported, although the user tapped both buttons repeatedly and raised no problem.
- Smoothness at the 60 fps cap: the wave "definitely doesn't feel like 60fps". On-screen numbers read by the user in that mode: panel 60 Hz, target 60, vsync 1, worst frame 16.6 ms, since-tap minimum 60 fps, 0 frames over 25 ms. So frames were not dropped; the roughness is perceived, on a phone whose panel can run 120 Hz. The user reports Motion smoothness in the phone settings was Adaptive, while the app was requesting a 60 cap. Adaptive lets the system choose the rate, so whether the app's 60 request held the panel at 60 is not separated out by these readings.
- Smoothness when the app requests 120 fps (`targetFrameRate` 120, vsync 0, mode 1) and with the explicit 120 Hz resolution request (mode 2): the user judged both "good". The measured rate at launch was 120.2 fps. `Screen.currentResolution` still reports 60 Hz on this device, so it cannot be trusted as a display-rate readout.

## Final verdict

PASS with one decision for M1: the app must request a 120 fps target with vsync off (or an equivalent) on 120 Hz phones; at the default 60 cap, a fast arm swing feels choppy even though no frame is dropped. The battery cost of 120 fps was not measured; M1 should choose the policy (for example 120 during animation-heavy scenes and lower on static ones) and measure it.
