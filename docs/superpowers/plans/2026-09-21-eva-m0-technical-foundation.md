# Eva's Learning World: M0 Technical Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Get a separate, ad-free Unity project for Eva running on the real device with a working test runner and the shared framework, and answer only the technical questions that could block the first playable vertical slice (M1).

**Architecture:** Move `Assets/Framework` into an embedded UPM package used by the existing project and by a new sibling project `EvasLearningWorld/`. Spikes are small disposable prototypes in `Assets/Spikes`, each ending in a short written verdict.

**Tech Stack:** Unity 6000.5.10f1 (batchmode CLI), NUnit edit-mode tests, embedded UPM package, uGUI and TextMeshPro, Node 24 tooling, bundletool/adb from the Unity-bundled Android SDK, Git Bash.

**Spec:** `docs/superpowers/specs/2026-09-21-evas-learning-world-design.md` (sections 6 to 9, 11, 12). Decision log: `docs/kids-games/game-modes-backlog.md`.

## Global Constraints

- **M0 scope rule:** do not add architecture, abstractions, tooling or validation whose only purpose is future-proofing. When a spike can answer its question with a small disposable prototype, build that. Anything that does not block the first playable vertical slice is deferred (see "Deferred out of M0" at the end).
- **Spike time and scope limit:** a spike answers its technical question, records evidence and a verdict, and stops. No production architecture, no polish, no extra features. If a spike has not produced a verdict after about two focused attempts at the mechanism, stop, record what was tried as PARTIAL or FAIL, and report to the main session.
- **Audience baseline (spec 4.0):** UX baseline is a 4 to 5-year-old who may not read yet; learning progression reaches about age 8. Any child-facing spike UI uses large targets (at least 80 dp) and needs no reading.
- Unity editor is exactly `6000.5.10f1` (`C:\Program Files\Unity\Hub\Editor\6000.5.10f1\Editor\Unity.exe`). Only ONE Unity batchmode process at a time (a second one exits silently with code 1). Do not edit sources during a build or test run. Run Unity with `run_in_background` and check in short bounded waits.
- Landscape only, min SDK 26, ARM64, IL2CPP.
- The Eva project contains NO ad, analytics, purchasing or services packages, no advertising ID, no own networking code, no runtime AI. Play performs pack downloads, not our code.
- Local save only; backup is Android Auto Backup only.
- Never print keystore passwords or API keys; keys come from environment variables only.
- Standing user rule: commit only when the user asks; never push. Every "Commit" step means: ask first, and if not asked, just report.
- Do not commit Unity byproducts (`Assets/Plugins/Android/mainTemplate.gradle`, `PerformanceTestRun*.json`); do not touch `Builds/`, `Keystores/`, `Library/`.
- A Sudoku debug build mutates the bundle id in `ProjectSettings/ProjectSettings.asset` and `ProjectSettings/AndroidResolverDependencies.xml`; restore it to `com.noadsguy.sudoku`.
- Test device: Samsung Galaxy S25 Ultra, serial `R3CY30NNA6W`, gesture navigation only. Never send `KEYCODE_BACK` or tap by guessed coordinates; ask the user to touch the phone when interaction is needed; delete screenshots when done.
- Persisted files (code, docs, commit messages) are written in normal prose.

## Spike report format

Each spike ends with `docs/superpowers/spikes/<name>.md` containing three headings: `## Verdict` (PASS, PARTIAL or FAIL plus one line), `## Evidence` (what was run and observed), `## Decision` (what the design does now, including any spec edit needed).

## Files

```
tools/env.sh, run-editmode-tests.sh, build-eva-debug.sh, eva-install.sh, check-apk-compliance.sh
tools/svg2png/            (Task 9)
art/spike/apple.svg       (Task 9)
Packages/com.noadsguy.framework/   framework moved from Assets/Framework
EvasLearningWorld/
  Packages/manifest.json
  Assets/Eva/App/         EvasLearningWorld.App
  Assets/Eva/Tests/       EvasLearningWorld.Tests
  Assets/Editor/          EvaProjectSetup, EvaAndroidBuilder, EvaSpikeScenes, EvaAndroidPostProcess
  Assets/Spikes/          disposable
docs/superpowers/spikes/  one short report per spike
```

---

### Task 1: Working edit-mode test runner (existing project)

The user reports Sudoku's Unity tests do not work and nobody has diagnosed it. The framework move needs this safety net first.

**Files:** Create `tools/env.sh`, `tools/run-editmode-tests.sh`.

**Produces:** `env.sh` variables `REPO_ROOT`, `UNITY_DIR`, `UNITY`, `ADB`, `AAPT2`, `DEVICE`, `EVA_PROJECT`; `tools/run-editmode-tests.sh <absolute project path>` prints the `<test-run ...>` summary and returns Unity's exit code.

- [ ] **Step 1: Write `tools/env.sh`**

```bash
#!/usr/bin/env bash
# Shared paths for the Unity and Android helper scripts. Source this file; do not run it.
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd -W)"
UNITY_DIR="/c/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor"
UNITY="$UNITY_DIR/Unity.exe"
ANDROID_SDK="$UNITY_DIR/Data/PlaybackEngines/AndroidPlayer/SDK"
ADB="$ANDROID_SDK/platform-tools/adb.exe"
AAPT2="$ANDROID_SDK/build-tools/36.0.0/aapt2.exe"
DEVICE="R3CY30NNA6W"
EVA_PROJECT="$REPO_ROOT/EvasLearningWorld"
```

- [ ] **Step 2: Write `tools/run-editmode-tests.sh`**

```bash
#!/usr/bin/env bash
# Usage: tools/run-editmode-tests.sh <absolute project path>
# Do NOT pass -quit: it is incompatible with -runTests.
set -u
source "$(dirname "${BASH_SOURCE[0]}")/env.sh"
PROJECT="${1:?absolute project path required}"
RESULTS="$PROJECT/Logs/editmode-results.xml"
LOG="$PROJECT/Logs/editmode.log"
mkdir -p "$PROJECT/Logs"
rm -f "$RESULTS"
"$UNITY" -batchmode -nographics -projectPath "$PROJECT" -runTests -testPlatform EditMode -testResults "$RESULTS" -logFile "$LOG"
code=$?
echo "unity exit code: $code"
if [ -f "$RESULTS" ]; then grep -o '<test-run [^>]*' "$RESULTS" | head -1; else tail -n 40 "$LOG"; fi
exit $code
```

- [ ] **Step 3: Run it (background, check every 60 s, cap 10 minutes)**

Run: `bash tools/run-editmode-tests.sh "C:/Users/schei/mobile-games-framework"`
Expected when healthy: `unity exit code: 0` and `result="Passed"`. Note the `total` as the baseline for Task 2.

- [ ] **Step 4: If it fails, diagnose from `Logs/editmode.log`, in this order**

1. "another Unity instance is running": close every Unity process and rerun.
2. `error CS`: fix the compile error; it is a real regression.
3. `total="0"`: check that both test asmdefs have `"overrideReferences": true`, `nunit.framework.dll`, `"defineConstraints": ["UNITY_INCLUDE_TESTS"]`, and that `com.unity.test-framework` is present in `Packages/manifest.json`.
4. Anything else: fix the first `Exception` in the log and say what it was in the commit message.

- [ ] **Step 5: Commit (ask first):** `chore: add edit-mode test runner wrapper for the Unity project`

---

### Task 2: Move the framework into a local UPM package

**Files:** Move `Assets/Framework/*` to `Packages/com.noadsguy.framework/`; delete `Assets/Framework.meta`; create `Packages/com.noadsguy.framework/package.json`; modify `Packages/manifest.json` (add `testables`).

**Consumes:** the Task 1 baseline test total. **Produces:** package `com.noadsguy.framework` with unchanged assembly names (`MobileGamesFramework`, `.UI`, `.Localization`, `.Tests`).

- [ ] **Step 1:** `git status --short` must print nothing. If not, stop and report.

- [ ] **Step 2: Move with git (keeps GUIDs and history)**

```bash
PKG=Packages/com.noadsguy.framework
mkdir -p "$PKG"
for name in GridCore Localization Monetization Persistence Tests UI Undo \
            GridCore.meta Localization.meta Monetization.meta Persistence.meta Tests.meta UI.meta Undo.meta \
            MobileGamesFramework.asmdef MobileGamesFramework.asmdef.meta; do
  git mv "Assets/Framework/$name" "$PKG/$name"
done
git rm -q Assets/Framework.meta
rmdir Assets/Framework
```

- [ ] **Step 3: Write `Packages/com.noadsguy.framework/package.json`**

```json
{
  "name": "com.noadsguy.framework",
  "version": "0.1.0",
  "displayName": "NoAdsGuy Mobile Games Framework",
  "description": "Shared grid, localization, persistence, undo and UI helpers for the NoAdsGuy mobile games.",
  "unity": "6000.0",
  "dependencies": { "com.unity.ugui": "2.0.0", "com.unity.inputsystem": "1.11.0" }
}
```

- [ ] **Step 4:** In `Packages/manifest.json` add `"testables": ["com.noadsguy.framework"]` at the top level. Then run `grep -rn "Assets/Framework" --include=*.cs --include=*.json --include=*.sh . --exclude-dir=Library --exclude-dir=Temp --exclude-dir=Logs` and fix any live hit (historical docs stay as they are).

- [ ] **Step 5: Tests must match the baseline.** Run `bash tools/run-editmode-tests.sh "C:/Users/schei/mobile-games-framework"` (background). Expected: `Passed` and the same `total` as Task 1. A lower total means package tests were not found: recheck Step 4.

- [ ] **Step 6: Sudoku still builds (background, cap 15 minutes)**

```bash
source tools/env.sh
"$UNITY" -batchmode -quit -projectPath "$REPO_ROOT" -executeMethod AndroidApkBuilder.BuildSudoku -logFile "$REPO_ROOT/Logs/sudoku-after-move.log"
grep -E "BUILD_RESULT|BUILD_TOTAL_ERRORS" "$REPO_ROOT/Logs/sudoku-after-move.log"
sed -i 's/com.mobilegamesframework.game02_sudoku/com.noadsguy.sudoku/' ProjectSettings/ProjectSettings.asset ProjectSettings/AndroidResolverDependencies.xml
git status --short
```
Expected: `BUILD_RESULT: Succeeded`, `BUILD_TOTAL_ERRORS: 0`, and git status shows only the moves, `package.json`, new `.meta` files and the manifest change (no `ProjectSettings` diff). 2048 shares the same assemblies, so tests plus this build cover it.

- [ ] **Step 7: Commit (ask first):** `refactor: move Assets/Framework into the local package com.noadsguy.framework` (include the new `.meta` files).

---

### Task 3: Eva project skeleton (landscape, no ad SDKs, tests green)

**Files:** Create `EvasLearningWorld/` (via `-createProject`); modify `EvasLearningWorld/Packages/manifest.json` and `Packages/com.noadsguy.framework/UI/UiFactory.cs`; create `Assets/Eva/App/EvasLearningWorld.App.asmdef`, `EvaBootstrap.cs`, `Assets/Eva/Tests/EvasLearningWorld.Tests.asmdef`, `ComplianceTests.cs`, `ProjectSettingsTests.cs`, `Assets/Editor/EvaProjectSetup.cs`.

**Produces:** assemblies `EvasLearningWorld.App` and `EvasLearningWorld.Tests`; `UiFactory.CreateCanvas(Vector2? referenceResolution = null, float matchWidthOrHeight = 0f)`; batchmode entry `EvaProjectSetup.Apply()` (idempotent; sets the settings and writes `Assets/Scenes/Boot.unity`). There is deliberately no `Rules` assembly yet: it is created in M1 with the first real rule.

- [ ] **Step 1: Create the project (background, cap 10 minutes)**

```bash
source tools/env.sh
"$UNITY" -batchmode -quit -createProject "$EVA_PROJECT" -logFile "$REPO_ROOT/Logs/eva-create.log"
```
Expected: `EvasLearningWorld/ProjectSettings/ProjectVersion.txt` says `6000.5.10f1`.

- [ ] **Step 2: Replace `EvasLearningWorld/Packages/manifest.json` (minimum set) and delete `Packages/packages-lock.json`**

```json
{
  "dependencies": {
    "com.noadsguy.framework": "file:../../Packages/com.noadsguy.framework",
    "com.unity.inputsystem": "1.20.0",
    "com.unity.test-framework": "1.7.0",
    "com.unity.ugui": "2.5.0",
    "com.unity.modules.androidjni": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.imageconversion": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0"
  },
  "testables": ["com.noadsguy.framework"]
}
```
More packages (particles, 2D feature set, and so on) are added when a task needs them.

- [ ] **Step 3: Make the framework canvas parameterizable (existing calls unchanged)** in `Packages/com.noadsguy.framework/UI/UiFactory.cs`: change the signature to `CreateCanvas(Vector2? referenceResolution = null, float matchWidthOrHeight = 0f)`, set `scaler.referenceResolution = referenceResolution ?? new Vector2(800, 900);` and `scaler.matchWidthOrHeight = matchWidthOrHeight;`. Keep the existing explanatory comment.

- [ ] **Step 4: Write the two asmdefs**

`Assets/Eva/App/EvasLearningWorld.App.asmdef`:
```json
{ "name": "EvasLearningWorld.App", "rootNamespace": "EvasLearningWorld.App",
  "references": ["MobileGamesFramework", "MobileGamesFramework.UI", "MobileGamesFramework.Localization", "UnityEngine.UI", "Unity.InputSystem"],
  "autoReferenced": true }
```
`Assets/Eva/Tests/EvasLearningWorld.Tests.asmdef`:
```json
{ "name": "EvasLearningWorld.Tests", "rootNamespace": "EvasLearningWorld.Tests",
  "references": ["UnityEngine.TestRunner", "UnityEditor.TestRunner", "EvasLearningWorld.App"],
  "includePlatforms": ["Editor"], "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"], "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"] }
```

- [ ] **Step 5: Write the failing tests**

`Assets/Eva/Tests/ComplianceTests.cs` (guards the Families constraint that no ad, analytics, purchasing or services package is ever linked, even transitively):
```csharp
using System.IO;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class ComplianceTests
    {
        private static readonly string[] Forbidden =
        {
            "com.google.ads.mobile", "com.google.external-dependency-manager", "com.unity.purchasing",
            "com.unity.modules.unityanalytics", "com.unity.analytics", "com.unity.services",
            "com.unity.ads", "com.unity.monetization"
        };

        [Test]
        public void PackagesLockHasNoAdsAnalyticsOrPurchasing()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "packages-lock.json");
            Assert.IsTrue(File.Exists(path), "open the project once so Unity writes packages-lock.json");
            var text = File.ReadAllText(path);
            foreach (var id in Forbidden)
                Assert.That(text, Does.Not.Contain("\"" + id), "forbidden package present: " + id);
        }
    }
}
```
`Assets/Eva/Tests/ProjectSettingsTests.cs`:
```csharp
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

namespace EvasLearningWorld.Tests
{
    public class ProjectSettingsTests
    {
        [Test]
        public void OrientationIsLandscapeOnly()
        {
            Assert.AreEqual(UIOrientation.AutoRotation, PlayerSettings.defaultInterfaceOrientation);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToPortrait);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToPortraitUpsideDown);
            Assert.IsTrue(PlayerSettings.allowedAutorotateToLandscapeLeft);
            Assert.IsTrue(PlayerSettings.allowedAutorotateToLandscapeRight);
        }

        [Test]
        public void AndroidIsArm64Il2CppApi26AndInternetIsNotForced()
        {
            Assert.AreEqual(ScriptingImplementation.IL2CPP, PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android));
            Assert.AreEqual(AndroidArchitecture.ARM64, PlayerSettings.Android.targetArchitectures);
            Assert.GreaterOrEqual((int)PlayerSettings.Android.minSdkVersion, (int)AndroidSdkVersions.AndroidApiLevel26);
            Assert.IsFalse(PlayerSettings.Android.forceInternetPermission);
        }
    }
}
```
Run `bash tools/run-editmode-tests.sh "C:/Users/schei/mobile-games-framework/EvasLearningWorld"`. Expected: the compliance test passes once Unity has written the lock file; `ProjectSettingsTests` FAIL (default settings are portrait/auto). That is the expected failing state.

- [ ] **Step 6: Write the setup script and bootstrap**

`Assets/Editor/EvaProjectSetup.cs`:
```csharp
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EvaProjectSetup
{
    // Unity.exe -batchmode -quit -projectPath <eva> -executeMethod EvaProjectSetup.Apply
    // The application id is a development id; the permanent package id is chosen right
    // before the first Play Store publish.
    public static void Apply()
    {
        var android = NamedBuildTarget.Android;
        PlayerSettings.companyName = "NoAdsGuy";
        PlayerSettings.productName = "Eva";
        PlayerSettings.SetApplicationIdentifier(android, "com.noadsguy.evas.dev");
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.forceInternetPermission = false;
        PlayerSettings.Android.forceSDCardPermission = false;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        EvaSpikeScenes.Create("Assets/Scenes/Boot.unity", null);
        AssetDatabase.SaveAssets();
    }
}
```
`Assets/Editor/EvaSpikeScenes.cs` (helper used by Boot and by every spike scene; a scene is one camera plus an optional component on an empty object):
```csharp
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EvaSpikeScenes
{
    public static void Create(string scenePath, Type componentType)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.75f, 0.91f, 1f);
        if (componentType != null)
            new GameObject(componentType.Name).AddComponent(componentType);
        Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
        EditorSceneManager.SaveScene(scene, scenePath);
    }
}
```
`Assets/Eva/App/EvaBootstrap.cs` (M0 only: proves the package, the landscape canvas and the frame rate setting on device):
```csharp
using MobileGamesFramework.UI;
using UnityEngine;

namespace EvasLearningWorld.App
{
    public static class EvaBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            Application.targetFrameRate = 60;
            var canvas = UiFactory.CreateCanvas(new Vector2(1600, 900), 1f);
            UiFactory.CreateBackground(canvas.transform, new Color(0.75f, 0.91f, 1f), new Color(0.91f, 0.97f, 0.88f));
            var text = UiFactory.CreateText(canvas.transform, "Info", 48, TextAnchor.MiddleCenter);
            UiFactory.SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.text = "Eva's Learning World\n" + Screen.width + "x" + Screen.height + "  " + Screen.orientation
                + "\nsafe area: " + Screen.safeArea;
        }
    }
}
```
Spike scenes are used for spikes, but the bootstrap runs in every scene, so spike scenes show its text behind their own UI; that is fine.

- [ ] **Step 7: Apply, then run all tests (background)**

```bash
source tools/env.sh
"$UNITY" -batchmode -quit -projectPath "$EVA_PROJECT" -executeMethod EvaProjectSetup.Apply -logFile "$EVA_PROJECT/Logs/eva-setup.log"
bash tools/run-editmode-tests.sh "$EVA_PROJECT"
```
Expected: `Passed`, `failed="0"`; total = framework tests plus 3 Eva tests. If `ComplianceTests` fails, remove whatever pulled the package in; never weaken the test. Also rerun the existing project's tests to confirm the `UiFactory` change broke nothing.

- [ ] **Step 8: Commit (ask first):** `feat: add Eva project skeleton with landscape settings and compliance guards`. Check first that `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Builds/` under `EvasLearningWorld` are ignored.

---

### Task 4: Debug APK on the device, landscape, no AD_ID

**Files:** Create `Assets/Editor/EvaAndroidBuilder.cs`, `tools/build-eva-debug.sh`, `tools/eva-install.sh`, `tools/check-apk-compliance.sh`.

**Produces:** `EvaAndroidBuilder.BuildDebug()` (scene list from env var `EVA_SCENES`, semicolon separated, default `Assets/Scenes/Boot.unity`; output `Builds/Android/eva-debug.apk`); `tools/eva-install.sh <apk> <package> [screenshot.png]`; `tools/check-apk-compliance.sh <apk>`. All later spikes reuse these.

- [ ] **Step 1: Write the builder**

```csharp
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class EvaAndroidBuilder
{
    // Unity.exe -batchmode -quit -projectPath <eva> -executeMethod EvaAndroidBuilder.BuildDebug
    public static void BuildDebug()
    {
        EvaProjectSetup.Apply();
        var sceneList = Environment.GetEnvironmentVariable("EVA_SCENES");
        var scenes = string.IsNullOrEmpty(sceneList)
            ? new[] { "Assets/Scenes/Boot.unity" }
            : sceneList.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        EditorUserBuildSettings.buildAppBundle = false; // a stuck AAB flag would make this "APK" an AAB
        Directory.CreateDirectory("Builds/Android");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/Android/eva-debug.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        Debug.Log("BUILD_RESULT: " + report.summary.result);
        Debug.Log("BUILD_TOTAL_ERRORS: " + report.summary.totalErrors);
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
```

- [ ] **Step 2: Write the three scripts**

`tools/build-eva-debug.sh`:
```bash
#!/usr/bin/env bash
# Usage: [EVA_SCENES="a.unity;b.unity"] tools/build-eva-debug.sh
set -u
source "$(dirname "${BASH_SOURCE[0]}")/env.sh"
LOG="$EVA_PROJECT/Logs/eva-build.log"
mkdir -p "$EVA_PROJECT/Logs"
"$UNITY" -batchmode -quit -projectPath "$EVA_PROJECT" -executeMethod EvaAndroidBuilder.BuildDebug -logFile "$LOG"
code=$?
echo "unity exit code: $code"
grep -E "BUILD_RESULT|BUILD_TOTAL_ERRORS" "$LOG"
exit $code
```
`tools/eva-install.sh` (never taps the screen):
```bash
#!/usr/bin/env bash
# Usage: tools/eva-install.sh <apk> <package> [screenshot.png]
set -u
source "$(dirname "${BASH_SOURCE[0]}")/env.sh"
"$ADB" -s "$DEVICE" install -r "${1:?apk}" || exit 1
"$ADB" -s "$DEVICE" shell monkey -p "${2:?package}" -c android.intent.category.LAUNCHER 1 >/dev/null
if [ -n "${3:-}" ]; then sleep 6; "$ADB" -s "$DEVICE" exec-out screencap -p > "$3"; echo "screenshot: $3"; fi
```
`tools/check-apk-compliance.sh`:
```bash
#!/usr/bin/env bash
# Usage: tools/check-apk-compliance.sh <apk>  (fails when AD_ID is declared; prints all permissions)
set -u
source "$(dirname "${BASH_SOURCE[0]}")/env.sh"
PERMS="$("$AAPT2" dump permissions "${1:?apk}")"
echo "$PERMS"
if echo "$PERMS" | grep -q "permission.AD_ID"; then echo "FAIL: AD_ID present"; exit 1; fi
echo "OK: no AD_ID"
```

- [ ] **Step 3: Build (background, cap 20 minutes; the first Android build of a new project is slow)** with `bash tools/build-eva-debug.sh`. Expected `BUILD_RESULT: Succeeded`, `BUILD_TOTAL_ERRORS: 0`.

- [ ] **Step 4:** `bash tools/check-apk-compliance.sh EvasLearningWorld/Builds/Android/eva-debug.apk` → `OK: no AD_ID`. Note any network-related permission in the list and remove it unless it is a debug-only artefact.

- [ ] **Step 5: Install and look.** Ask the user not to touch the phone, run `bash tools/eva-install.sh EvasLearningWorld/Builds/Android/eva-debug.apk com.noadsguy.evas.dev "$TEMP/eva-m0.png"`, and read the PNG. Expected: gradient, the title text, a landscape resolution (about 2340x1080) and a safe-area line. Delete the screenshot.

- [ ] **Step 6: Commit (ask first):** `feat: add Eva debug APK builder, install script and compliance check`.

---

### Task 5: Spike, Play Asset Delivery (optional language pack)

The riskiest spike. Question only: can English always be local while an optional language pack is delivered by Google Play, with clean English fallback when the pack is not obtained?

**Files:** `Assets/Spikes/Pad/*` (disposable), `docs/superpowers/spikes/play-asset-delivery.md`; `EvasLearningWorld/Packages/manifest.json` only if the chosen mechanism needs a package.

- [ ] **Step 1: Find the mechanism from current sources, not memory.** Read the current Unity 6 manual on Play Asset Delivery / Android asset packs (WebSearch, WebFetch), and check the installed editor for asset-pack support (`grep -rli assetpack "$UNITY_DIR/Data/PlaybackEngines/AndroidPlayer" --include=*.xml --include=*.gradle --include=*.txt`). Choose in this order: (1) Unity-native asset packs if documented for this version; (2) Google's Play Asset Delivery Unity plugin (check its Unity 6 compatibility and licence on its repository first); (3) a hand-authored Gradle asset-pack module plus `AndroidJavaObject` calls to Play's `AssetPackManager` (Sudoku's logs show the library is not linked by default). Write down which was chosen and why.

- [ ] **Step 2: Build the smallest prototype.** English is a plain file inside the app (always local). `voice_ro` is an on-demand pack holding one small file. Prototype scene (create via `EvaSpikeScenes.Create`, one MonoBehaviour): at start it reads the English file immediately and shows it; then it requests `voice_ro`; when the request completes it reads and shows the Romanian file; if the request does not complete successfully it simply keeps English. Log markers: `PAD_EN_OK`, `PAD_REQUEST_START`, `PAD_RO_OK`, `PAD_RO_FAILED`.

- [ ] **Step 3: Build a debug-signed AAB and install it with bundletool local testing (background)**

```bash
source tools/env.sh
JAVA="$UNITY_DIR/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/java.exe"
BT="$UNITY_DIR/Data/PlaybackEngines/AndroidPlayer/Tools/bundletool-all-1.17.2.jar"
"$JAVA" -jar "$BT" build-apks --bundle=EvasLearningWorld/Builds/Android/pad-spike.aab --output=EvasLearningWorld/Builds/Android/pad-spike.apks --local-testing --connected-device --adb="$ADB" --overwrite
"$JAVA" -jar "$BT" install-apks --apks=EvasLearningWorld/Builds/Android/pad-spike.apks --adb="$ADB"
"$ADB" -s "$DEVICE" shell monkey -p com.noadsguy.evas.dev -c android.intent.category.LAUNCHER 1
"$ADB" -s "$DEVICE" logcat -d | grep PAD_
```
Expected: `PAD_EN_OK` first (English usable before any request), then `PAD_RO_OK`. Use a throwaway builder method (`buildAppBundle = true`, Unity's default debug key). Never touch Sudoku's release keystore.

- [ ] **Step 4: Scope of failure testing.** Do not invent artificial failures. The fallback is a plain branch (`if not delivered, keep English`), which the English-first start already exercises. The real-condition case (first launch offline, Play unavailable) can only be observed through a Play internal test track install, so it is recorded as a check for M5, not simulated here.

- [ ] **Step 5: Write the report.** PASS: give the exact recipe (versions, Gradle or manifest changes, build flags, entry points) so M1 can build `Voice` on it. FAIL or unstable: decision is the spec's fallback, bundling Romanian in the app; estimate size (about 600 lines at about 4 s each is about 2400 s of audio, about 14 MB per language at 48 kbps mono OGG, well under Play's 200 MB base limit) and edit spec sections 7 and 8 accordingly.

- [ ] **Step 6: Commit (ask first):** `spike: play asset delivery for the optional language voice pack`.

---

### Task 6: Spike, Auto Backup of the local save

Question only: is the eventual local save (PlayerPrefs, kept as Android shared preferences) included in Auto Backup?

**Files:** `Assets/Editor/EvaAndroidPostProcess.cs` (kept for the product), `Assets/Spikes/Backup/BackupProbe.cs` (disposable), `docs/superpowers/spikes/auto-backup.md`.

- [ ] **Step 1: Post-process that declares backup rules in the generated launcher manifest**

```csharp
using System.IO;
using System.Xml.Linq;
using UnityEditor.Android;

public class EvaAndroidPostProcess : IPostGenerateGradleAndroidProject
{
    private const string Full =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<full-backup-content>\n    <include domain=\"sharedpref\" path=\".\"/>\n</full-backup-content>\n";
    private const string Extraction =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<data-extraction-rules>\n    <cloud-backup>\n        <include domain=\"sharedpref\" path=\".\"/>\n    </cloud-backup>\n    <device-transfer>\n        <include domain=\"sharedpref\" path=\".\"/>\n    </device-transfer>\n</data-extraction-rules>\n";

    public int callbackOrder => 0;

    public void OnPostGenerateGradleAndroidProject(string unityLibraryPath)
    {
        var launcher = Path.GetFullPath(Path.Combine(unityLibraryPath, "..", "launcher"));
        var manifest = Path.Combine(launcher, "src", "main", "AndroidManifest.xml");
        var xmlDir = Path.Combine(launcher, "src", "main", "res", "xml");
        Directory.CreateDirectory(xmlDir);
        File.WriteAllText(Path.Combine(xmlDir, "eva_full_backup_content.xml"), Full);
        File.WriteAllText(Path.Combine(xmlDir, "eva_data_extraction_rules.xml"), Extraction);

        XNamespace a = "http://schemas.android.com/apk/res/android";
        var doc = XDocument.Load(manifest);
        var app = doc.Root.Element("application");
        app.SetAttributeValue(a + "allowBackup", "true");
        app.SetAttributeValue(a + "fullBackupContent", "@xml/eva_full_backup_content");
        app.SetAttributeValue(a + "dataExtractionRules", "@xml/eva_data_extraction_rules");
        doc.Save(manifest);
    }
}
```
If the launcher manifest is not at that path, list `EvasLearningWorld/Temp/gradleOut` and fix the path. Do not use the Main Manifest override slot (it broke Sudoku's launcher activity before).

- [ ] **Step 2: Probe.** A MonoBehaviour in a spike scene (`EvaSpikeScenes.Create`) that increments `PlayerPrefs` int `eva.spike.launches`, saves, and shows `launches: N`. Build with `EVA_SCENES=...` and install with `tools/eva-install.sh`.

- [ ] **Step 3: Record the configuration.** With `source tools/env.sh`, run `"$AAPT2" dump xmltree <apk> --file AndroidManifest.xml | grep -iE "allowBackup|fullBackupContent|dataExtractionRules"` and `"$AAPT2" dump badging <apk> | grep -E "sdkVersion|targetSdkVersion"`. Expected: `allowBackup` true and both rule references. Write down the minSdk, targetSdk and the two rule files used (older Android uses `fullBackupContent`, Android 12 and up uses `dataExtractionRules`).

- [ ] **Step 4: One practical restore attempt (best effort).** With the user's phone unlocked and Google Backup on: launch twice (`launches: 2`), then `"$ADB" -s "$DEVICE" shell bmgr backupnow com.noadsguy.evas.dev`, `bmgr list sets`, uninstall, reinstall, `bmgr restore <token> com.noadsguy.evas.dev`, launch. Expected `launches: 3`. If the transport is Samsung's or `bmgr` is inconclusive, mark the restore NOT VERIFIED and rely on the manifest evidence; the full restore check moves to M5 with a Play internal test install. Do not claim a restore that was not observed.

- [ ] **Step 5: Report and commit (ask first):** `spike: android auto backup rules for the local save`. PASS when manifest evidence plus an observed restore; PARTIAL when only the manifest is verified.

---

### Task 7: Spike, production text path for all planned scripts

Decision made up front: the production text path is **TextMeshPro** (part of `com.unity.ugui` 2.x) with **bundled Noto Sans fonts** (OFL licence): a base font covering Latin, Cyrillic and Romanian, plus Noto Sans JP, KR and SC as fallback fonts, all as dynamic font assets. Gameplay never depends on reading, so this only needs to prove reliability, not a font architecture. If it fails, the report names the smallest change that works.

**Files:** `Assets/Fonts/*` (font files and TMP font assets), `Assets/Spikes/Text/TextSpike.cs`, `docs/superpowers/spikes/text-rendering.md`.

- [ ] **Step 1: Import the TMP essential resources** (`Window > TextMeshPro > Import TMP Essential Resources` in the editor, or call `TMPro.EditorUtilities.TMP_PackageResourceImporter.ImportResources` from a batchmode method).

- [ ] **Step 2: Add the fonts.** Download Noto Sans, Noto Sans JP, Noto Sans KR and Noto Sans SC from fonts.google.com/noto (OFL, free) into `Assets/Fonts/`. Create a dynamic TMP font asset for each (`TMP_FontAsset.CreateFontAsset(font)` with dynamic atlas population, or the Font Asset Creator window) and add the three CJK assets to the base asset's fallback list.

- [ ] **Step 3: Spike scene with one `TMP_Text`** showing one line per language: en, es, pt, de, fr, it, ro (must include `ă â î ș ț Ă Â Î Ș Ț`), ru, ja, ko, zh-Hans. The script also calls `HasCharacters` on the font asset for the joined text and logs `TEXT_MISSING <count>`.

- [ ] **Step 4: Build, install, screenshot, user eyeballs it.** Expected: no empty boxes and `TEXT_MISSING 0`. Ask the user to check ja, ko and zh-Hans by eye (a scaled screenshot can hide defects). Record the APK size before and after the fonts.

- [ ] **Step 5: Report and commit (ask first):** `spike: TextMeshPro with bundled Noto fonts renders all 11 languages`. Record that trimming the CJK fonts to the characters actually used is deferred to M4 (localization). Update spec section 4.9 only if the decision changed.

---

### Task 8: Spike, cutout character rig

Question only: is a simple hierarchical cutout character (sprites parented under body parts, animated by Animator clips, with swappable parts) cheap, reliable and smooth enough for Eva and the player character? No deformable bones, no skinning, no extra 2D packages.

**Files:** `Assets/Spikes/Rig/*` (disposable), `docs/superpowers/spikes/cutout-rig.md`.

- [ ] **Step 1: Build a disposable prototype with an editor method** (run in batchmode, then build the scene into an APK). It generates its own placeholder sprites (coloured rounded shapes written to PNG and imported as Sprites) so it does not depend on the SVG spike. Requirements:
  - A root object with an `Animator`; child `SpriteRenderer` parts: torso, head, two arms, two legs, nested so the head and arms move with the torso.
  - Arms and legs use a top pivot (custom sprite pivot) so they rotate at the shoulder or hip; explicit `sortingOrder` so the head is in front and limbs behind.
  - Two `AnimationClip`s written from code with `AnimationClip.SetCurve` (property names `localPosition.y` and `localEulerAnglesRaw.z`): `Idle` (looping gentle body bob) and `Wave` (right arm raises, swings, returns). An `AnimatorController` with a `Wave` trigger, default state `Idle`, and `Wave` returning to `Idle`.
  - Swappable parts: three head sprites cycled by a button; one button triggers Wave; a small text shows the smoothed fps. Use `UiFactory` for the buttons and text.

- [ ] **Step 2: Run it on the device.** Build the scene with `EVA_SCENES`, install with `tools/eva-install.sh`, and ask the user to tap the buttons. Expected: idle loops, Wave raises the arm and returns, heads swap, fps stays at or near 60.

- [ ] **Step 3: Report and commit (ask first):** `spike: cutout character rig with idle, wave and swappable head`. PASS when all of the above holds. Note pivot and sorting gotchas found. Decision: M1 builds Eva and the player from this pattern with real SVG-derived parts.

---

### Task 9: Spike, SVG to PNG

Question only: can art authored as SVG (with gradients and soft shadows, our style 3) be rasterized to correct PNGs by a small script?

**Files:** `art/spike/apple.svg`, `tools/svg2png/package.json`, `svg2png.js`, `svg2png.test.js`, `docs/superpowers/spikes/svg-to-png.md`.

- [ ] **Step 1: Sample art `art/spike/apple.svg`**

```xml
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
  <defs>
    <radialGradient id="body" cx="0.35" cy="0.3" r="0.8">
      <stop offset="0" stop-color="#FF8A7A"/><stop offset="1" stop-color="#D62D20"/>
    </radialGradient>
    <filter id="soft" x="-30%" y="-30%" width="160%" height="170%">
      <feDropShadow dx="0" dy="6" stdDeviation="4" flood-color="#000" flood-opacity="0.35"/>
    </filter>
  </defs>
  <g filter="url(#soft)"><circle cx="50" cy="45" r="30" fill="url(#body)"/></g>
</svg>
```

- [ ] **Step 2: `tools/svg2png/package.json`**, then `cd tools/svg2png && npm install`:

```json
{ "name": "svg2png", "private": true, "scripts": { "test": "node --test" },
  "dependencies": { "@resvg/resvg-js": "^2.6.2" } }
```
Make sure `tools/svg2png/node_modules/` is git-ignored.

- [ ] **Step 3: Failing test `svg2png.test.js`** (run `npm test`, expect `Cannot find module './svg2png'`):

```js
const test = require('node:test');
const assert = require('node:assert');
const fs = require('node:fs');
const path = require('node:path');
const { renderSvg } = require('./svg2png');

const apple = fs.readFileSync(path.join(__dirname, '..', '..', 'art', 'spike', 'apple.svg'));
const px = (img, x, y) => Array.from(img.pixels.slice((y * img.width + x) * 4, (y * img.width + x) * 4 + 4));

test('renders at the requested width, body opaque, corner transparent', () => {
  const img = renderSvg(apple, 200);
  assert.strictEqual(img.width, 200);
  assert.strictEqual(px(img, 100, 90)[3], 255);
  assert.strictEqual(px(img, 2, 2)[3], 0);
});

test('radial gradient: highlight is lighter than the rim', () => {
  const img = renderSvg(apple, 200);
  assert.ok(px(img, 85, 75)[1] > px(img, 120, 130)[1] + 20);
});

test('drop shadow: soft semi-transparent pixels below the body', () => {
  const alpha = px(renderSvg(apple, 200), 100, 160)[3];
  assert.ok(alpha > 0 && alpha < 255, 'shadow alpha=' + alpha);
});
```

- [ ] **Step 4: Implement `svg2png.js`**, then `npm test` must pass:

```js
const fs = require('node:fs');
const path = require('node:path');
const { Resvg } = require('@resvg/resvg-js');

function renderSvg(svgBuffer, widthPx) {
  const image = new Resvg(svgBuffer, { fitTo: { mode: 'width', value: widthPx } }).render();
  return { width: image.width, height: image.height, pixels: image.pixels, asPng: () => image.asPng() };
}

module.exports = { renderSvg };

if (require.main === module) {
  const [inDir, outDir, width] = process.argv.slice(2);
  if (!inDir || !outDir) { console.error('usage: node svg2png.js <inDir> <outDir> [widthPx]'); process.exit(1); }
  fs.mkdirSync(outDir, { recursive: true });
  for (const file of fs.readdirSync(inDir).filter((f) => f.toLowerCase().endsWith('.svg'))) {
    const img = renderSvg(fs.readFileSync(path.join(inDir, file)), Number(width) || 512);
    fs.writeFileSync(path.join(outDir, file.replace(/\.svg$/i, '.png')), img.asPng());
    console.log(file + ' -> ' + img.width + 'x' + img.height);
  }
}
```
If the shadow test fails because `feDropShadow` is unsupported, rewrite the filter as `feGaussianBlur` + `feOffset` + `feMerge`, retest, and record it (the art style depends on shadows).

- [ ] **Step 5:** Convert the sample (`node tools/svg2png/svg2png.js art/spike "$TEMP/svgspike" 400`), view the PNG (glossy apple, soft shadow, transparent background), write the report, and commit (ask first): `feat: add SVG to PNG rasterizer with gradient and shadow tests`.

---

### Task 10: Spike, voice audition (English and Romanian)

Question only: which tool and voices give the warm, friendly female guide voice for English now and Romanian later? Needs the user's ears and their approval before any account or payment. No scripts are built: use each tool's own web page.

**Files:** `docs/superpowers/spikes/voice-audition.md`.

- [ ] **Step 1: Check current terms first (WebFetch, do not use memory).** For ElevenLabs, Microsoft Azure AI Speech (neural voices) and Google Cloud Text-to-Speech record: free-tier size, cheapest paid plan, whether output may be used commercially in a shipped app on the free tier, attribution requirements, and the date checked.

- [ ] **Step 2: Test lines (keep exactly; Romanian diacritics `ă â î ș ț` must survive).**

| key | en | ro |
|---|---|---|
| greeting | Hello! I'm Eva. I'm so happy to see you. Let's play together! | Bună! Eu sunt Eva. Mă bucur atât de mult să te văd. Hai să ne jucăm împreună! |
| retry | Oops, not quite. Try again, you can do it! | Hopa, nu chiar. Mai încearcă o dată, tu poți! |
| cheer | Wonderful! You did it! Here are your coins. | Minunat! Ai reușit! Uite monedele tale. |

- [ ] **Step 3: Ask the user which tools they are willing to try,** report the Step 1 findings, and let the user create any account or key themselves. Never ask for a key in chat.

- [ ] **Step 4: The user generates the six lines per candidate in each tool's web studio and ranks them** on warmth, clarity for a 5-year-old, naturalness and Romanian diacritics. Do not decide for them.

- [ ] **Step 5: Report and commit (ask first):** `spike: voice tool audition for English and Romanian`. Include the terms table, the ranking, the chosen tool and voices, and a cost estimate for about 600 lines per language. The production voice-line script is built in M1 after the choice; Romanian recording waits for M4. Fill in spec section 7.

---

### Task 11: M0 exit review

**Files:** `docs/superpowers/spikes/M0-summary.md`; edits to the spec and backlog only where a verdict changed a decision.

- [ ] **Step 1: Cold verification (background, one at a time).** Run both test suites (`tools/run-editmode-tests.sh` for the repo root and for `EvasLearningWorld`), `bash tools/build-eva-debug.sh`, then `bash tools/check-apk-compliance.sh EvasLearningWorld/Builds/Android/eva-debug.apk`. Expected: all pass, `OK: no AD_ID`. The default Boot scene build must show only the M0 info screen (spikes live in their own scenes, so nothing leaks).

- [ ] **Step 2: Write `M0-summary.md`.** One line per item: framework package move, test runner, Eva project on device, and the six spikes (Play Asset Delivery, Auto Backup, text rendering, cutout rig, SVG to PNG, voice audition), each with verdict and decision. List spec edits made and the inputs Plan 2 (M1) takes from the spikes.

- [ ] **Step 3: Apply spec edits** named in the reports (for example bundling Romanian if delivery failed) and add one dated entry to the backlog decision log.

- [ ] **Step 4: Ask the user to judge M0** and whether to write Plan 2 (the vertical slice). Plan 2 is written only after this answer.

- [ ] **Step 5: Commit (ask first):** `docs: M0 exit summary and spec updates from the spikes`.

---

## Deferred out of M0 (not blocking the vertical slice)

- The `Rules` assembly, level ladder, help ladder, coin logic, save schema: built in M1 with their first real use and tests.
- Content schema, content validator, offline AI content generation tooling: after the slice, when a second content-driven mode exists.
- Voice-line generation script, voice text pipeline, Romanian recording, the other 9 languages, font trimming: M1 (English only) and M4.
- Release builder, release keystore, R8, symbols, store listing, final package id: M6.
- Parent gate, parent progress view, settings screen: after the slice.
- Real art, the final Eva and player rigs, music, sound effects: M1 onward.
- Failure-condition testing of pack delivery and the full Auto Backup restore on a Play-installed build: M5.
