# Sudoku Play Store Release Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Get the Sudoku game onto the Google Play Store: a signed `.aab` release build, store-listing assets (icon, feature graphic, screenshots), a published privacy policy, and a concrete Play Console submission runbook.

**Architecture:** Extend the existing `AndroidApkBuilder.cs` Editor tool with a release-build method that signs a real `.aab` from env-var-supplied keystore credentials. Add a second small Editor tool (`StoreAssetGenerator.cs`) that reuses `IconGenerator.GenerateSudokuIcon()` to produce Play's required hi-res icon and feature graphic. Everything else (privacy policy page, screenshots, listing copy, submission steps) is static content under `docs/`.

**Tech Stack:** Unity 6000.5.10f1 batchmode builds, Android `keytool` (bundled with Unity's OpenJDK), `adb` (bundled with Unity's Android SDK), GitHub Pages for the privacy policy.

**Spec:** [docs/superpowers/specs/2026-09-02-sudoku-play-store-release-design.md](../specs/2026-09-02-sudoku-play-store-release-design.md)

## Global Constraints

- Package identity stays `com.mobilegamesframework.game02_sudoku` (already set in `AndroidApkBuilder.BuildSudoku()` — do not change).
- Ads/IAP stay off for this release (`SudokuController.AdsEnabled = false`, unchanged) — all store-listing content (Data Safety form, content rating, description) must describe an ad-free, no-purchase, no-data-collection app.
- Target API level: already satisfied automatically (`AndroidTargetSdkVersion: 0` = highest installed = 37, above the required 36) — no PlayerSettings change needed anywhere in this plan.
- The release keystore and its password are secrets: never write them into a committed file or into a plan/spec document. They live only in `Keystores/` (gitignored) and are passed to builds via environment variables.
- Play requires the account owner (not Claude) to create the Play Console app entry, agree to Play's policies, and upload the build — those are documented as a runbook, not automated.

---

### Task 1: Release keystore

**Files:**
- Modify: `.gitignore`
- Create (untracked, gitignored): `Keystores/sudoku-release.keystore`, `Keystores/sudoku-release-keystore-password.txt`

**Interfaces:**
- Produces: a keystore file and password file on disk that Task 2's build reads via the `SUDOKU_KEYSTORE_PATH`/`SUDOKU_KEYSTORE_PASS`/`SUDOKU_KEY_ALIAS`/`SUDOKU_KEY_ALIAS_PASS` environment variables (alias name fixed as `sudoku-upload`).

- [ ] **Step 1: Add the ignore entry**

Add this block to `.gitignore` (anywhere near the existing "Builds" section):

```
# Release signing (never commit)
Keystores/
```

- [ ] **Step 2: Generate the keystore**

```bash
mkdir -p "C:/Users/schei/mobile-games-framework/Keystores"
openssl rand -base64 24 > "C:/Users/schei/mobile-games-framework/Keystores/sudoku-release-keystore-password.txt"
KEYSTORE_PASS=$(cat "C:/Users/schei/mobile-games-framework/Keystores/sudoku-release-keystore-password.txt")
"/c/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/keytool.exe" -genkeypair -v \
  -keystore "C:/Users/schei/mobile-games-framework/Keystores/sudoku-release.keystore" \
  -alias sudoku-upload -keyalg RSA -keysize 2048 -validity 10000 \
  -storepass "$KEYSTORE_PASS" -keypass "$KEYSTORE_PASS" \
  -dname "CN=Sudoku, OU=MobileGamesFramework, O=MobileGamesFramework, L=Unknown, S=Unknown, C=US"
```

- [ ] **Step 3: Verify it was created and is ignored**

```bash
ls -la "C:/Users/schei/mobile-games-framework/Keystores/"
git -C "C:/Users/schei/mobile-games-framework" status --short
```

Expected: both files listed by `ls`; `git status` shows nothing under `Keystores/` (only the `.gitignore` edit shows as modified).

- [ ] **Step 4: Tell the user to back up the password file**

Print this message verbatim to the user before continuing: "Generated `Keystores/sudoku-release.keystore` and its password file. Back both up somewhere durable (password manager, external drive) — if you lose this keystore, you can never publish another update to this same Play Store listing. Play App Signing will re-sign for distribution, but this upload key itself has no recovery path."

- [ ] **Step 5: Commit the `.gitignore` change**

```bash
git -C "C:/Users/schei/mobile-games-framework" add .gitignore
git -C "C:/Users/schei/mobile-games-framework" commit -m "$(cat <<'EOF'
build: ignore local release keystore files

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: Signed `.aab` release build

**Files:**
- Modify: `Assets/Editor/AndroidApkBuilder.cs`

**Interfaces:**
- Consumes: `Keystores/sudoku-release.keystore` and its password from Task 1, via env vars.
- Produces: `AndroidApkBuilder.BuildSudokuRelease()` (executable via `-executeMethod`), which writes `Builds/Android/mobile-games-framework-sudoku-release.aab` and logs `BUILD_RESULT`/`BUILD_TOTAL_ERRORS`/`BUILD_TOTAL_WARNINGS` the same way `BuildSudoku()` already does.

- [ ] **Step 1: Add the release build method**

Add this to `Assets/Editor/AndroidApkBuilder.cs`, after the existing `BuildSudoku()` method:

```csharp
    public static void BuildSudokuRelease()
    {
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.mobilegamesframework.game02_sudoku");
        PlayerSettings.productName = "Sudoku";
        IconGenerator.SaveAndSetAndroidIcon(IconGenerator.GenerateSudokuIcon(), "Assets/Icons/icon_sudoku.png");

        var scenes = new[]
        {
            "Assets/Scenes/SudokuMenu.unity",
            "Assets/Scenes/Sudoku.unity",
            "Assets/Scenes/SudokuSettings.unity",
            "Assets/Scenes/SudokuHighScores.unity"
        };

        RunReleaseBuild(scenes, "Builds/Android/mobile-games-framework-sudoku-release.aab");
    }

    private static void RunReleaseBuild(string[] scenes, string locationPathName)
    {
        PlayerSettings.Android.requestedVisibleInsets = AndroidWindowInsetsType.StatusBars | AndroidWindowInsetsType.NavigationBars;

        var keystorePath = System.Environment.GetEnvironmentVariable("SUDOKU_KEYSTORE_PATH");
        var keystorePass = System.Environment.GetEnvironmentVariable("SUDOKU_KEYSTORE_PASS");
        var keyAlias = System.Environment.GetEnvironmentVariable("SUDOKU_KEY_ALIAS");
        var keyAliasPass = System.Environment.GetEnvironmentVariable("SUDOKU_KEY_ALIAS_PASS");

        if (string.IsNullOrEmpty(keystorePath))
        {
            UnityEngine.Debug.LogError("BUILD_RESULT: Failed - SUDOKU_KEYSTORE_PATH not set");
            EditorApplication.Exit(1);
            return;
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystorePath;
        PlayerSettings.Android.keystorePass = keystorePass;
        PlayerSettings.Android.keyaliasName = keyAlias;
        PlayerSettings.Android.keyaliasPass = keyAliasPass;
        PlayerSettings.Android.bundleVersionCode += 1;

        var previousBuildAppBundle = EditorUserBuildSettings.buildAppBundle;
        EditorUserBuildSettings.buildAppBundle = true;
        try
        {
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = locationPathName,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            UnityEngine.Debug.Log($"BUILD_RESULT: {report.summary.result}");
            UnityEngine.Debug.Log($"BUILD_TOTAL_ERRORS: {report.summary.totalErrors}");
            UnityEngine.Debug.Log($"BUILD_TOTAL_WARNINGS: {report.summary.totalWarnings}");

            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = previousBuildAppBundle;
            PlayerSettings.Android.useCustomKeystore = false;
        }
    }
```

- [ ] **Step 2: Run the release build and verify**

```bash
export SUDOKU_KEYSTORE_PATH="C:/Users/schei/mobile-games-framework/Keystores/sudoku-release.keystore"
export SUDOKU_KEYSTORE_PASS=$(cat "C:/Users/schei/mobile-games-framework/Keystores/sudoku-release-keystore-password.txt")
export SUDOKU_KEY_ALIAS="sudoku-upload"
export SUDOKU_KEY_ALIAS_PASS=$SUDOKU_KEYSTORE_PASS
"/c/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Unity.exe" -batchmode -nographics \
  -projectPath "C:/Users/schei/mobile-games-framework" \
  -executeMethod AndroidApkBuilder.BuildSudokuRelease \
  -logFile "C:/Users/schei/mobile-games-framework/build_sudoku_release.log" -quit
grep -E "BUILD_RESULT|BUILD_TOTAL_ERRORS|error CS" "C:/Users/schei/mobile-games-framework/build_sudoku_release.log"
ls -la "C:/Users/schei/mobile-games-framework/Builds/Android/mobile-games-framework-sudoku-release.aab"
```

Expected: `BUILD_RESULT: Succeeded`, `BUILD_TOTAL_ERRORS: 0`, and the `.aab` file exists.

- [ ] **Step 3: Confirm `ProjectSettings.asset` picked up the version bump, then commit the code change**

```bash
git -C "C:/Users/schei/mobile-games-framework" diff --stat ProjectSettings/ProjectSettings.asset
rm -f "C:/Users/schei/mobile-games-framework/build_sudoku_release.log"
git -C "C:/Users/schei/mobile-games-framework" add Assets/Editor/AndroidApkBuilder.cs ProjectSettings/ProjectSettings.asset
git -C "C:/Users/schei/mobile-games-framework" commit -m "$(cat <<'EOF'
feat: add signed .aab release build for Sudoku Play Store submission

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: Store graphics (hi-res icon + feature graphic)

**Files:**
- Create: `Assets/Editor/StoreAssetGenerator.cs`
- Create (output, committed): `docs/store-assets/icon-512.png`, `docs/store-assets/feature-graphic-1024x500.png`

**Interfaces:**
- Consumes: `IconGenerator.GenerateSudokuIcon()` (existing, returns a 1024×1024 `Texture2D` with a transparent background and blue rounded-square Sudoku icon).
- Produces: two opaque PNG files under `docs/store-assets/` for later use in Task 6's Play Console runbook.

- [ ] **Step 1: Write the generator**

Create `Assets/Editor/StoreAssetGenerator.cs`:

```csharp
using System.IO;
using UnityEditor;
using UnityEngine;

public static class StoreAssetGenerator
{
    private static readonly Color BackgroundTop = new Color(0.75f, 0.85f, 0.97f);
    private static readonly Color BackgroundBottom = new Color(0.98f, 0.98f, 1f);

    public static void GenerateAll()
    {
        GenerateHiResIcon();
        GenerateFeatureGraphic();
        Debug.Log("STORE_ASSETS_DONE");
    }

    private static void GenerateHiResIcon()
    {
        var source = IconGenerator.GenerateSudokuIcon();
        var opaque = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
        for (var y = 0; y < source.height; y++)
        for (var x = 0; x < source.width; x++)
        {
            var pixel = source.GetPixel(x, y);
            opaque.SetPixel(x, y, Color.Lerp(BackgroundTop, pixel, pixel.a));
        }
        opaque.Apply();

        SavePng(Resize(opaque, 512, 512), "docs/store-assets/icon-512.png");
    }

    private static void GenerateFeatureGraphic()
    {
        const int width = 1024;
        const int height = 500;
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        for (var y = 0; y < height; y++)
        {
            var color = Color.Lerp(BackgroundBottom, BackgroundTop, (float)y / (height - 1));
            for (var x = 0; x < width; x++)
                texture.SetPixel(x, y, color);
        }

        const int iconSize = 400;
        var icon = Resize(IconGenerator.GenerateSudokuIcon(), iconSize, iconSize);
        var offsetX = (width - iconSize) / 2;
        var offsetY = (height - iconSize) / 2;
        for (var y = 0; y < iconSize; y++)
        for (var x = 0; x < iconSize; x++)
        {
            var pixel = icon.GetPixel(x, y);
            if (pixel.a < 0.01f) continue;
            var destX = offsetX + x;
            var destY = offsetY + y;
            texture.SetPixel(destX, destY, Color.Lerp(texture.GetPixel(destX, destY), pixel, pixel.a));
        }
        texture.Apply();

        SavePng(texture, "docs/store-assets/feature-graphic-1024x500.png");
    }

    private static Texture2D Resize(Texture2D source, int width, int height)
    {
        var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var srcX = x * source.width / width;
            var srcY = y * source.height / height;
            result.SetPixel(x, y, source.GetPixel(srcX, srcY));
        }
        result.Apply();
        return result;
    }

    private static void SavePng(Texture2D texture, string relativePath)
    {
        var fullPath = Path.Combine(Application.dataPath, "..", relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllBytes(fullPath, texture.EncodeToPNG());
        Debug.Log($"STORE_ASSET_SAVED: {relativePath}");
    }
}
```

- [ ] **Step 2: Run it and verify output**

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Unity.exe" -batchmode -nographics \
  -projectPath "C:/Users/schei/mobile-games-framework" \
  -executeMethod StoreAssetGenerator.GenerateAll \
  -logFile "C:/Users/schei/mobile-games-framework/store_assets.log" -quit
grep -E "STORE_ASSET_SAVED|STORE_ASSETS_DONE|error CS" "C:/Users/schei/mobile-games-framework/store_assets.log"
ls -la "C:/Users/schei/mobile-games-framework/docs/store-assets/"
```

Expected: both `STORE_ASSET_SAVED` lines, `STORE_ASSETS_DONE`, and both PNG files present with non-zero size.

- [ ] **Step 3: Commit**

```bash
rm -f "C:/Users/schei/mobile-games-framework/store_assets.log"
git -C "C:/Users/schei/mobile-games-framework" add Assets/Editor/StoreAssetGenerator.cs docs/store-assets/icon-512.png docs/store-assets/feature-graphic-1024x500.png
git -C "C:/Users/schei/mobile-games-framework" commit -m "$(cat <<'EOF'
feat: generate Play Store hi-res icon and feature graphic

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 4: Privacy policy page

**Files:**
- Create: `docs/privacy/sudoku.html`

**Interfaces:**
- Produces: a static HTML page that, once GitHub Pages is enabled on this repo (manual step, see below), becomes reachable at a public URL to paste into Play Console's "Privacy policy" field.

- [ ] **Step 1: Write the page**

Create `docs/privacy/sudoku.html`:

```html
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>Sudoku — Privacy Policy</title>
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>
  body { font-family: system-ui, sans-serif; max-width: 640px; margin: 40px auto; padding: 0 20px; line-height: 1.6; color: #222; }
  h1 { font-size: 1.6em; }
  h2 { font-size: 1.15em; margin-top: 1.5em; }
</style>
</head>
<body>
<h1>Sudoku — Privacy Policy</h1>
<p>Effective date: September 2, 2026</p>

<p>This Privacy Policy explains what happens to your data when you use the Sudoku app.</p>

<h2>Data we collect</h2>
<p>None. Sudoku does not require an account, does not connect to the internet, and does not collect, store, or transmit any personal information.</p>

<h2>Data stored on your device</h2>
<p>Your game progress, settings, and high scores are saved locally on your device using Android's standard app storage. This data never leaves your device, and is deleted automatically if you uninstall the app or use the in-app Reset Data option in Settings.</p>

<h2>Advertising and purchases</h2>
<p>This version of Sudoku does not display ads or offer in-app purchases.</p>

<h2>Children's privacy</h2>
<p>Sudoku does not knowingly collect any information from anyone, including children.</p>

<h2>Changes to this policy</h2>
<p>If a future version of the app adds advertising, in-app purchases, or any data collection, this page will be updated first.</p>

<h2>Contact</h2>
<p>Questions about this policy can be sent to <a href="mailto:scheiberadi@gmail.com">scheiberadi@gmail.com</a>.</p>
</body>
</html>
```

- [ ] **Step 2: Commit**

```bash
git -C "C:/Users/schei/mobile-games-framework" add docs/privacy/sudoku.html
git -C "C:/Users/schei/mobile-games-framework" commit -m "$(cat <<'EOF'
docs: add Sudoku privacy policy page

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git -C "C:/Users/schei/mobile-games-framework" push
```

- [ ] **Step 3: Ask the user to enable GitHub Pages (manual — repo setting, not automated)**

Tell the user: "Please enable GitHub Pages on the repo yourself: Settings → Pages → Source: 'Deploy from a branch' → Branch: `master` / folder `/docs` → Save. Once it's live (usually a minute or two), the privacy policy will be at `https://scheiberadi.github.io/mobile-games-framework/privacy/sudoku.html` — confirm that URL loads before we use it in Play Console." Do not attempt to flip this setting via `gh api` or any other tool — it's a repo-settings change reserved for the user per the standing safety rules.

---

### Task 5: Screenshots

**Files:**
- Create (output, committed): `docs/store-assets/screenshots/menu.png`, `docs/store-assets/screenshots/gameplay.png`, `docs/store-assets/screenshots/success.png`, `docs/store-assets/screenshots/highscores.png`

**Interfaces:**
- Consumes: the existing debug APK at `Builds/Android/mobile-games-framework-sudoku.apk` (already built and verified this session) and a connected Android device.

- [ ] **Step 1: Confirm device and install the app**

```bash
ADB="/c/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe"
"$ADB" devices
"$ADB" install -r "C:/Users/schei/mobile-games-framework/Builds/Android/mobile-games-framework-sudoku.apk"
```

Expected: one device listed as `device` (not `unauthorized`/`offline`); install reports `Success`. If no device is listed, stop and ask the user to connect their phone with USB debugging enabled before continuing.

- [ ] **Step 2: Capture each screen (one at a time, with the user)**

For each of the four screens below: ask the user to open that screen on their device (or navigate it yourself if you can drive the device), then run the matching capture command.

Menu screen:
```bash
"$ADB" shell screencap -p /sdcard/screenshot.png
"$ADB" pull /sdcard/screenshot.png "C:/Users/schei/mobile-games-framework/docs/store-assets/screenshots/menu.png"
```

Active gameplay grid (mid-puzzle, some numbers filled in):
```bash
"$ADB" shell screencap -p /sdcard/screenshot.png
"$ADB" pull /sdcard/screenshot.png "C:/Users/schei/mobile-games-framework/docs/store-assets/screenshots/gameplay.png"
```

Success popup (finish a puzzle, or use Autofill then Verify to trigger it):
```bash
"$ADB" shell screencap -p /sdcard/screenshot.png
"$ADB" pull /sdcard/screenshot.png "C:/Users/schei/mobile-games-framework/docs/store-assets/screenshots/success.png"
```

High Scores page:
```bash
"$ADB" shell screencap -p /sdcard/screenshot.png
"$ADB" pull /sdcard/screenshot.png "C:/Users/schei/mobile-games-framework/docs/store-assets/screenshots/highscores.png"
```

- [ ] **Step 3: Verify all four exist and commit**

```bash
ls -la "C:/Users/schei/mobile-games-framework/docs/store-assets/screenshots/"
git -C "C:/Users/schei/mobile-games-framework" add docs/store-assets/screenshots/
git -C "C:/Users/schei/mobile-games-framework" commit -m "$(cat <<'EOF'
docs: add Play Store screenshots for Sudoku

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git -C "C:/Users/schei/mobile-games-framework" push
```

---

### Task 6: Store listing copy + Play Console runbook

**Files:**
- Create: `docs/store-assets/play-console-runbook.md`

**Interfaces:**
- Consumes: the icon/feature graphic from Task 3, the screenshots from Task 5, and the privacy policy URL confirmed in Task 4.
- Produces: a single checklist document the user follows inside Play Console to actually submit the app (manual — account/policy-agreement/upload actions only the account owner can perform).

- [ ] **Step 1: Write the runbook**

Create `docs/store-assets/play-console-runbook.md`:

```markdown
# Sudoku — Play Console Submission Runbook

Everything below is done by hand in Play Console (play.google.com/console) —
none of it is automatable from here, since it requires your account login
and agreement to Play's developer policies.

## 1. Create the app

- App name: `Sudoku`
- Default language: English (United States)
- App or game: Game
- Free or paid: Free
- Package name: `com.mobilegamesframework.game02_sudoku` (fixed — matches the build)

## 2. Store listing

**Short description** (80 char max):
```
Classic Sudoku puzzles with four difficulty levels and daily play.
```

**Full description**:
```
Sharpen your mind with classic Sudoku, redesigned for a clean, modern
mobile experience.

FEATURES
- Four difficulty levels: Easy, Medium, Hard, and Expert, each puzzle
  guaranteed to have a unique solution.
- Custom mode: build your own puzzle from a blank grid, validated
  automatically so every custom puzzle you play has exactly one solution.
- Notes mode: pencil in candidate numbers in a clean 3x3 mini-grid inside
  each cell.
- Undo, Verify, and Autofill: check your work against the solution at any
  time, or reveal the answer when you're stuck (autofilled puzzles are
  marked and excluded from your high scores, so your leaderboard always
  reflects real solves).
- Dedicated High Scores page: your best times for each difficulty,
  ranked, with the option to clear a leaderboard and start fresh.
- Simple, distraction-free interface: number-first input, tap a number
  then tap the cells you want to fill.

No account required. No ads. No internet connection needed. Your
progress and times are saved privately on your own device.

Whether you're a Sudoku beginner working through Easy puzzles or an
expert chasing your best Expert time, Sudoku gives you a clean board
and gets out of your way.
```

**Graphics** (upload from `docs/store-assets/`):
- App icon: `icon-512.png`
- Feature graphic: `feature-graphic-1024x500.png`
- Phone screenshots (upload all four, in this order): `screenshots/menu.png`, `screenshots/gameplay.png`, `screenshots/success.png`, `screenshots/highscores.png`

**Category**: Puzzle

**Contact details**: your email; no website required (skip that field)

**Privacy policy URL**: the GitHub Pages URL confirmed in Task 4, e.g.
`https://scheiberadi.github.io/mobile-games-framework/privacy/sudoku.html`

## 3. Content rating questionnaire

Answer as: no violence, no sexual content, no profanity, no controlled
substances, no gambling (real or simulated), no user-generated content,
no shared/social features, no location sharing. This should produce an
"Everyone" rating.

## 4. Data safety form

Since ads/IAP are off for this release:
- Does your app collect or share any user data? **No**
- Data is encrypted in transit? N/A (no data collected)
- Users can request data deletion? N/A (no data collected)

## 5. App content declarations

- Ads: **No, my app does not contain ads**
- Target audience and content: pick an age range that fits a general
  puzzle game (e.g. 13+, or Everyone if prompted) — no specific
  children's-app declarations apply since there's no ad/data collection
  and no child-directed marketing.
- Government apps / COVID-19 apps / financial features: **No** to all.

## 6. Upload the build

- Go to **Release → Testing → Closed testing** (NOT Production — this
  account is new and Play requires a closed test first).
- Create a closed testing track, upload
  `Builds/Android/mobile-games-framework-sudoku-release.aab`.
- Fill in release notes, e.g. "Initial release."

## 7. Add testers

- In the closed testing track, add testers via email list (paste the
  email addresses of 12+ people willing to install and open the app) or
  a Google Group.
- Copy the opt-in URL Play Console generates and share it with your
  testers — they must open it and accept before they count.

## 8. Wait out the test window

- Play requires the closed test to run for **14 days** with **12+
  testers who have opted in** before the "Promote to production" option
  becomes available for a new developer account.
- Once eligible, go to the closed testing release, click **Promote
  release → Production**, and complete the rollout.
```

- [ ] **Step 2: Commit**

```bash
git -C "C:/Users/schei/mobile-games-framework" add docs/store-assets/play-console-runbook.md
git -C "C:/Users/schei/mobile-games-framework" commit -m "$(cat <<'EOF'
docs: add Play Console submission runbook for Sudoku

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git -C "C:/Users/schei/mobile-games-framework" push
```

- [ ] **Step 3: Report to the user**

Tell the user: everything buildable/writable is done and pushed. Point them at `docs/store-assets/play-console-runbook.md` and ask them to work through it in Play Console, starting with creating the app entry — offer to answer questions about any specific field as they go.
