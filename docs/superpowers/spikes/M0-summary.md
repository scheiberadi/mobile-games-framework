# M0 exit summary: technical foundation for Eva's Learning World

Date: 2026-09-21. Unity 6000.5.10f1, Samsung Galaxy S25 Ultra (SM-S938B, Android 16), branch `eva-m0`. Nothing from M0 is committed yet: every change is in the working tree and the commit waits for the user's approval.

Each line below gives the verdict, the decision it led to, and what the evidence really is. "Agent" means the coding agent observed it (a test result, a build, a screenshot it read); "user" means the user reported it. Several device results are single observations with no saved artifact (screenshots are deleted after reading), and that is said where it applies.

## Items

1. **Framework package move: PASS.** `Assets/Framework` now lives in the local package `Packages/com.noadsguy.framework` (registered as testable). The repo-root suite is still 187 of 187 and the Sudoku debug build succeeded with 0 errors after the move (agent, task 2). Decision: Eva and the existing project share the framework as a package. GUIDs were kept through the `.meta` files, but the move is not staged, so git shows it as deletes plus untracked files until it is added.

2. **Test runner: PASS.** `tools/run-editmode-tests.sh` works for both projects: repo root 187 of 187 and Eva 75 of 75 in the cold verification run (agent, task 11 step 1). Getting there needed a real fix: `SudokuSolver` hung on a board with conflicting givens (the runner appeared to hang) and one stale Sudoku test expectation was wrong. Both fixes are in the working tree and uncommitted. Decision: keep the wrapper as the way to run tests; do not pass `-quit` with `-runTests`.

3. **Eva project on device: PASS.** The Eva project builds a debug APK (`com.noadsguy.evas.dev`, about 23.6 MB for the Boot scene), passes the compliance check (no AD_ID, launchable activity present) and, at task 4, installed and launched on the phone in landscape with a safe-area line and the title visible (agent screenshot, deleted). In the cold verification the default Boot build compiled and passed the compliance check again, and the final default Boot build was then installed on the phone: the screen shows only the title, `2340x1080 LandscapeLeft` and the safe-area line (controller screenshot, deleted), so nothing from the spikes leaks into it. Decision: keep the project skeleton; the default Boot scene is meant to show only the M0 info screen (spikes have their own scenes).

4. **Play Asset Delivery: PASS.** Unity-native asset packs (a `voice_ro.androidpack` folder, an app bundle, `AndroidAssetPacks`) deliver an optional pack with English staying inside the app. Observed by the agent on the phone through bundletool local testing: the screen showed the English line and the Romanian line, and the Romanian file exists only in the pack split. That on-device result is a single observation with no saved artifact. A real Play download, first launch offline and Play being unavailable are not tested (M5, internal test track). Decision: language voice packs are on-demand asset packs, English in the app, no own networking code, Romanian not bundled. Cost: the Play library adds six permissions (see open items).

5. **Auto Backup: PASS (same-device restore).** `EvaAndroidPostProcess` writes the backup attributes and rule files into the generated launcher manifest, including the shared preferences (PlayerPrefs). Agent observed on the phone: `bmgr backupnow`, uninstall, reinstall, `bmgr restore`, and the counter came back as `launches: 3`, through Google's backup transport. This is a same-device `bmgr` restore of a debug adb-installed build. Only the Android 12 and newer rules path was exercised; the Android 8 to 11 path and device-to-device transfer were not. Decision: keep the post-process as the product's backup declaration; check a real Play install on a new phone in M5.

6. **Text rendering: PASS.** TextMeshPro with a Noto Sans base font plus Noto Sans JP, KR and SC as dynamic fallbacks shows all 11 language lines on the phone with `TEXT_MISSING 0` and no `.notdef` glyphs (agent, read from the on-screen counters and a screenshot). The user looked at ja, ko and zh-Hans on the phone and judged them fine (user-reported, single manual observation, no artifact). Decision: no change to spec section 4.9. Cost: the APK grew by about 43 MB (untrimmed; a debug APK compared with the earlier Boot debug APK, including about 4 MB of TMP essentials). Known issue for M4: with the fallback order JP, KR, SC, a shared Han character takes the Japanese shape, so Chinese text can show Japanese forms.

7. **Cutout rig: PASS with two findings.** The hierarchical sprite rig with Idle and Wave clips and a swappable head runs on the phone (agent screenshots for layout and fps; user tap check for motion, wave, return to Idle and head swap). Finding 1: the user's taps first did nothing, because the Eva project used the old Input Manager while the UI module needs the Input System; fixed in `EvaProjectSetup.Apply` with a test (75 of 75), and the user then confirmed both buttons work. Finding 2: at a 60 fps cap the wave felt choppy to the user although no frame was dropped (60 fps, zero slow frames); requesting 120 fps with vsync off measured 120.2 fps and the user judged it good. Not measured: battery cost, and whether the frames are actually presented at 120 (the measured rate counts Unity frames). Decision: build Eva and the player from this pattern; request 120 fps on 120 Hz phones.

8. **SVG to PNG: PASS.** `tools/svg2png/svg2png.js` with resvg-js 2.6.2 renders a radial gradient and a `feDropShadow` soft shadow to a correct transparent PNG; three tests pass and the pixel values were measured (agent). Decision: author art as SVG and rasterize with this script. Not tested: text and fonts inside SVG, masks and clip paths, blend modes, batch speed.

9. **Voice audition: PASS with a caveat.** The user listened to Google Chirp 3 HD only (they chose not to audition ElevenLabs or Azure) and said "Leda sounds good" (user-reported; no per-language ranking). Decision: Google Chirp 3 HD, voice Leda (`en-US-Chirp3-HD-Leda`, `ro-RO-Chirp3-HD-Leda`). The user's remark did not separately confirm Romanian pronunciation and diacritics, so that is confirmed at the first Romanian generation (M4). Cost: about 600 lines per language is about 36k characters, inside the free 1 million characters per month, so about zero (30 US dollars per 1 million after that). Vendor terms were researched from the vendors' pages; Google's terms were not checked for a child-directed clause beyond the pages read (privacy work, M6). A Google Cloud account and API key will be needed later, kept in an environment variable and never in the repository.

## Cold verification (task 11 step 1, agent, 2026-09-21)

- Repo-root suite: 187 of 187 passed. Eva suite: 75 of 75 passed.
- Default Boot build: `BUILD_RESULT: Succeeded`, 0 errors, APK 24,710,330 bytes. Compliance script: `OK: no AD_ID`, launchable activity present. Permissions in that build: `INTERNET` and Unity's own `DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION` only.
- `ProjectSettings.asset` of the Sudoku project is unmodified and still carries `com.noadsguy.sudoku`.
- Boot-scene-only check on the phone: done after the cold verification; only the M0 info screen (title, resolution and orientation, safe area) is shown.

## Spec and backlog edits made at the M0 exit

Spec `docs/superpowers/specs/2026-09-21-evas-learning-world-design.md`, small edits only, nothing restructured:

- Section 6: frame rate note (request 120 fps with vsync off on 120 Hz phones; battery policy in M1).
- Section 7: voice tool and voice decided (Google Chirp 3 HD, Leda), cost, the Romanian confirmation still owed, the terms caveat and the API key rule.
- Section 8.1: the Eva project uses the Input System only, set by `EvaProjectSetup.Apply`.
- Section 8.6: backup verified in the spike (same-device restore), Play install check stays in M5.
- Section 9: the extra permissions added by Play asset delivery, and that they must not be stripped.
- Section 4.9: unchanged (the text spike did not change it).

Backlog `docs/kids-games/game-modes-backlog.md`: one dated entry, "M0 EXIT OUTCOMES (2026-09-21)", added at the end of the decision log.

Spike docs touched at the same time: `voice-audition.md` (verdict, decision, cost), `auto-backup.md` (same-device qualifier), `text-rendering.md` (font paths and sizes), `play-asset-delivery.md` (spec impact line).

## What Plan 2 (M1, the vertical slice) takes from the spikes

- **Input System only.** `EvaProjectSetup.Apply` sets `activeInputHandler` to 1 and a test guards it. All Eva UI depends on it, and any fresh clone should run `Apply` once before building. Only "run `Apply`, then build" was verified.
- **Bootstrap lifetime.** `EvaBootstrap` runs once, after the first scene loads (`RuntimeInitializeOnLoadMethod(AfterSceneLoad)`), not in every scene, and its objects (canvas, event system) are not `DontDestroyOnLoad`, so they are destroyed on the first scene change. M1's scene flow must account for that (own the canvas and event system per scene, or make them persistent on purpose).
- **Frame rate.** Request 120 fps with vsync off on 120 Hz phones and replace the bootstrap's fixed 60. Decide and measure the battery policy (for example lower rates on static screens). Never use `Screen.currentResolution` to detect the rate on this phone; use measured frame time.
- **Icon-only buttons.** The child baseline is a 4 to 5-year-old who may not read, so M1 buttons carry icons, not text (raised-arm glyph, face swatch). The spike buttons used text labels only because an adult tested them. Size: about 100 dp or more.
- **Play asset delivery recipe.** Folder `voice_<lang>.androidpack/src/main/assets`, name starts with a letter and uses only letters, digits and underscores; on-demand by default; build an app bundle (`buildAppBundle = true`, otherwise the folder is silently ignored); read at runtime with `AndroidAssetPacks.DownloadAssetPackAsync(new[] { name })` (the one-argument overload, because the two-argument callback overload returns void), then `GetAssetPackPath(name)` and plain `System.IO`; test with bundletool `--local-testing`; the phone must be awake and unlocked. English audio ships in the app; keep English whenever the pack is absent.
- **Auto Backup.** Keep `EvaAndroidPostProcess` (do not use the Main Manifest override slot, it removed the launcher activity). Save through PlayerPrefs or another shared preferences file. The current rule includes all shared preferences; narrowing to the save file is a later nicety. Same-device `bmgr` restore is observed; the Play install restore check is M5.
- **Text.** TextMeshPro (already in `com.unity.ugui`) with a Noto Sans base asset and dynamic fallbacks. Untrimmed CJK fonts cost about 43 MB of APK. In M4, pick the CJK font per language (zh-Hans may show Japanese glyph forms otherwise) and trim the fonts to the characters actually used.
- **Cutout rig pattern.** A root with an Animator, body parts as child SpriteRenderers, part swaps by replacing `SpriteRenderer.sprite`. Gotchas: set the pivot in the sprite import (custom alignment) with one convention per slot; sorting order is absolute per renderer, not relative to the parent; a trigger stays set until consumed, so guard against mashing (the spike guard has a hole during the 0.1 s blend); Write Defaults makes Idle-only properties snap back during Wave; real parts need a shared canvas size or offsets.
- **SVG to PNG.** Use `tools/svg2png` (resvg-js 2.6.2) at the needed pixel width; verify text, masks and blend modes when real art needs them.
- **Voice.** Google Chirp 3 HD, Leda. Build the English voice-line script in M1; Romanian waits for M4, where the pronunciation and diacritics are confirmed. Needs the user's Google Cloud account and API key in an environment variable.

## Rulings made during M0 (from the ledger)

- R1: no commits anywhere (the user commits only when explicitly approved); implementers skip the plan's commit steps and task diffs come from working-tree snapshots. Cost if wrong: none, nothing irreversible.
- R2: no separate worktree; branch `eva-m0` was created in place, because a worktree would duplicate the multi-GB Unity Library and break device and Unity path assumptions. Cost if wrong: minimal.
- R3: task 2 step 1 was narrowed to "no uncommitted changes under `Assets/`, `Packages/`, `ProjectSettings/`"; the unrelated modified files under `docs/store-assets` are not ours and were left alone. Cost if wrong: none.
- R4: if task 5 needed `com.google.external-dependency-manager` (a build-time tool, not an ad SDK), the implementer could remove that one id from the compliance forbid list and say so. Cost if wrong: a slightly looser guard, one line to revert. In the end it was not needed and the test was not relaxed.
- R5: large spike assets (Noto CJK fonts, pack test artifacts, APK and AAB) must not be added to anything tracked; if not git-ignored, implementers tell the controller. Cost if wrong: repo bloat only if committed later. The fonts are now git-ignored.
- R6: implementers and reviewers use sonnet; the task 5 implementer and the final review use opus; Unity tasks run strictly one at a time with bounded background waits.
- R7 (user): a spike gets about two focused attempts at a mechanism, then PARTIAL or FAIL is recorded; no production architecture or polish.
- R8: the `Assets/Plugins/Android/AndroidManifest.xml` override in task 4 was removed because it deleted the launcher activity; stripping INTERNET was deferred. Cost if wrong: INTERNET stays in the manifest (which turned out to be required, see R10).
- R9: with the phone absent, tasks that need no phone ran first (task 9 SVG, task 10 preparation), and the device steps followed when it was connected. Cost if wrong: the spike order differs from the plan, no rework.
- R10: `INTERNET` is required by Play asset delivery, so task 6 must not strip it; it is recorded for the M6 compliance review. Cost if wrong: the Families data-safety wording must mention Play-managed network use.
- R11: task 5 minors deferred (English via `Resources` and not StreamingAssets, a `runInBackground` leftover in ProjectSettings, no source URLs, and the spec-impact line needing an INTERNET note at the M0 exit, now done). Cost if wrong: none.
- R12: task 8 did not start until the user had looked at the text scene on the phone, because installing the rig would replace the app and the scene. Cost if wrong: one turn of waiting.
- R13: the input handler fix lives in `EvaProjectSetup.Apply` (kept code) with a settings test, not in the spike. Cost if wrong: one more edit to task 3 code and a re-review of that small diff (done, approved).
- R14: the last round of task 8 doc fixes was verified by controller grep instead of a sixth reviewer, because it was doc-only wording. Cost if wrong: a stale doc phrase remains.

## Known open items and deferred minors

Decisions and checks that belong to the user or to later milestones:

- **Large fonts, now git-ignored** (`Assets/Fonts/`, `Assets/TextMesh Pro/` and their `.meta` files; they must be re-fetched to rebuild the text spike). About 50 MB in `EvasLearningWorld/Assets/Fonts` (three 15.7 MB CJK OTFs plus a 2.0 MB Noto Sans) and about 4 MB in `EvasLearningWorld/Assets/TextMesh Pro`. The final decision (commit, Git LFS, or fetch by script) is left to the user and M4.
- **`tools/svg2png/package-lock.json`** is new and untracked; the user decides whether to keep it. `.gitignore` gained `node_modules/`.
- **Sudoku production fix uncommitted.** `SudokuSolver` now rejects conflicting givens (the fix for the hang) and one Sudoku test expectation was corrected. Both are in the working tree only.
- **Build and compliance coverage.** `EditorBuildSettings` has no scenes, so the project builds only through `tools/build-eva-debug.sh`, and `ComplianceTests` only greps `packages-lock.json` (a raw `.aar` or a Gradle dependency under `Assets/Plugins/Android` would not be caught): M6 compliance review.
- **Play permissions added by asset delivery**, to review in M6 for Families policy and the data-safety form: `INTERNET`, `ACCESS_NETWORK_STATE`, `FOREGROUND_SERVICE`, `FOREGROUND_SERVICE_DATA_SYNC`, `WAKE_LOCK`, `RECEIVE_BOOT_COMPLETED`. Present in the pack spike's build, not in the default Boot build (which has `INTERNET` only).
- **Romanian voice check** (pronunciation and diacritics with Leda) at the first Romanian generation in M4; Korean voice quality before its pack is committed.
- **Google terms** checked for a child-directed clause only in the pages read; belongs to the M6 privacy work.
- **Failure-condition testing** of pack delivery, and the Auto Backup restore on a Play-installed build (including the Android 8 to 11 rule path and device transfer): M5.
- **Battery cost of 120 fps** not measured: M1.

Deferred minors from the ledger (none blocks the vertical slice):

- Task 1: no column or box conflict regression test for `SudokuSolver`; the `TryBuild_EmptyBoard` test name could be renamed.
- Task 3: `Boot.unity` is not in EditorBuildSettings (the builder passes scenes explicitly); the `UiFactory` comment still says portrait-only; `Apply` idempotence is untested; the ARM64/IL2CPP settings test passes on defaults, so it is a regression guard only.
- Task 4: `check-apk-compliance.sh` prints OK for AD_ID if `aapt2 permissions` fails on a bad APK (the badging check still fails afterwards).
- Task 5: English is read from `Resources`, not StreamingAssets; `runInBackground` left over in ProjectSettings; no source URLs recorded; the INTERNET note for the spec (now done).
- Task 6: only the `dataExtractionRules` path was exercised; `sharedpref path="."` backs up every shared preferences file, so narrow it to the save file later.
- Task 7: the spike's overlay removal via `GameObject.Find` is fragile (spike only); the 202-line `Boot.unity` change is `Apply` regenerating the scene, not explained in the report; the +43 MB includes about 4 MB of TMP essentials and compares a debug APK with the earlier Boot debug APK (now stated in the text spike doc); the claim that OTFs compress poorly is unmeasured.
- Task 8: the wave guard has a hole during the 0.1 s blend; Write Defaults snaps Idle-only properties during Wave; the Idle loop seam tangents were not checked by eye; a redundant `SaveAssets` in `EvaProjectSetup.cs`; a test uses `LoadAllAssetsAtPath[0]`; the rig spike's mode 0 after mode 2 keeps the `SetResolution` request; the first fps bucket is skewed by load.
- Task 9: the "transparent background" wording in the SVG doc was overstated (transparency was confirmed by pixel test, the viewer showed white); the tests assert raw pixels, not a PNG round trip.

## Next step

Ask the user to judge M0 and whether to write Plan 2, the vertical slice. Plan 2 is written only after that answer. The M0 commit (`docs: M0 exit summary and spec updates from the spikes`, and the code and tooling that go with it) also waits for the user's approval.
