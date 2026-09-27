# Sudoku Localization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add 11-language support to "NoAdsGuy's Sudoku" — English, Spanish, Portuguese, German, French, Japanese, Korean, Russian, Chinese (Simplified), Italian, and Romanian — defaulting to the device's system language on first launch and changeable afterward from Settings.

**Architecture:** A new Unity-aware framework assembly (`MobileGamesFramework.Localization`, mirroring the existing `MobileGamesFramework.UI` split) provides a reusable `Language` enum, a key→string `LocalizationTable` with English fallback, and a `LocalizationLoader` that detects `Application.systemLanguage` and loads JSON content from `Resources`. Sudoku supplies its own translated JSON content plus a thin `Loc` static facade and `SudokuLocalizationSettings` persistence adapter (same shape as the existing `SudokuAudioSettings`). Every hardcoded UI string across the game's five screens is rewritten to call `Loc.Get(...)`.

**Tech Stack:** Unity 6000.5.10f1, C#, NUnit (EditMode tests), `UnityEngine.Resources` + `JsonUtility` for content loading, `PlayerPrefs`-backed persistence via the existing `IKeyValueStore` pattern.

**Spec:** [docs/superpowers/specs/2026-09-15-sudoku-localization-design.md](../specs/2026-09-15-sudoku-localization-design.md)

## Global Constraints

- 11 languages exactly: English, Spanish, Portuguese, German, French, Japanese, Korean, Russian, Chinese (Simplified), Italian, Romanian. No others.
- First launch with no stored language preference: detect `Application.systemLanguage`, map to the closest of the 11 (exact match only, no fuzzy matching), fall back to English if unmapped, and persist that choice immediately so it behaves identically to an explicit pick from then on.
- Every later launch uses the persisted choice; system language is never re-checked after the first launch.
- Changing language in Settings persists it and reloads the current scene (`SceneManager.LoadScene` on the active scene name) — the same mechanism the Back button already uses — rather than inventing new UI-refresh machinery.
- Missing key in the active language falls back to English; missing/corrupt language file falls back to English entirely. Neither case may crash or show a blank label — worst case is the raw key string.
- **`Unity.exe -batchmode -runTests` hangs indefinitely on this project** (confirmed this session: stuck indefinitely at `Executing IPrebuildSetup for: Unity.PerformanceTesting.Editor.TestRunBuilder`, matching a prior session's note). Do not attempt to run automated tests via batchmode in any task below. Tests are still written for every task where they add real value (pure C# logic), but "run the test" steps are replaced with a note to verify by inspection; actual regression coverage for this plan comes from Task 14's full compile-check build (Unity compiles all script assemblies, tests included, before running any batchmode method — a broken test file fails the build the same as broken production code) and Task 15's on-device manual verification.
- Debug builds (`AndroidApkBuilder.BuildSudoku`) mutate `ProjectSettings/ProjectSettings.asset`'s `Android:` application identifier and `ProjectSettings/AndroidResolverDependencies.xml`'s `bundleId` from the committed release value `com.noadsguy.sudoku` to `com.mobilegamesframework.game02_sudoku`. After every debug build in this plan, revert both back to `com.noadsguy.sudoku` via `Edit` before committing (never `git checkout`).
- Debug build command (run via Bash `run_in_background: true`, several minutes):
  ```bash
  "/c/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Unity.exe" -batchmode -quit -projectPath "C:/Users/schei/mobile-games-framework" -executeMethod AndroidApkBuilder.BuildSudoku -logFile "<log path>"
  ```
  Confirm success via `BUILD_TOTAL_ERRORS: 0` in the log. Never start a second batchmode invocation while one is still running against this project (exclusive project lock).
- `adb` full path: `/c/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe`. Debug package name: `com.mobilegamesframework.game02_sudoku`.
- Commit after every task. Never use `git checkout`/`reset --hard`/`clean` to revert the identifier mutation — use `Edit`.

---

## Task 1: `Language` enum and native display names

**Files:**
- Create: `Assets/Framework/Localization/MobileGamesFramework.Localization.asmdef`
- Create: `Assets/Framework/Localization/Language.cs`
- Modify: `Assets/Framework/Tests/MobileGamesFramework.Tests.asmdef` (add reference)
- Test: `Assets/Framework/Tests/LanguageInfoTests.cs`

**Interfaces:**
- Produces: `namespace MobileGamesFramework.Localization { public enum Language { English, Spanish, Portuguese, German, French, Japanese, Korean, Russian, ChineseSimplified, Italian, Romanian } }`, `public static class LanguageInfo { public static string Code(Language language); public static string NativeName(Language language); }`

- [ ] **Step 1: Create the new assembly definition**

Create `Assets/Framework/Localization/MobileGamesFramework.Localization.asmdef`:

```json
{
    "name": "MobileGamesFramework.Localization",
    "rootNamespace": "MobileGamesFramework.Localization",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

This mirrors `Assets/Framework/UI/MobileGamesFramework.UI.asmdef` — a Unity-aware sibling to the pure-C# root `MobileGamesFramework` assembly. `noEngineReferences: false` allows `UnityEngine.Resources`/`JsonUtility`/`Application`/`SystemLanguage` (needed by Task 3) without listing them explicitly — Unity includes the default `UnityEngine` core module automatically.

- [ ] **Step 2: Add the new assembly as a reference for `MobileGamesFramework.Tests`**

In `Assets/Framework/Tests/MobileGamesFramework.Tests.asmdef`, add `"MobileGamesFramework.Localization"` to the `"references"` array (alongside the existing `"MobileGamesFramework"`):

```json
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "MobileGamesFramework",
        "MobileGamesFramework.Localization"
    ],
```

- [ ] **Step 3: Write the failing test**

Create `Assets/Framework/Tests/LanguageInfoTests.cs`:

```csharp
using System;
using NUnit.Framework;
using MobileGamesFramework.Localization;

namespace MobileGamesFramework.Tests
{
    public class LanguageInfoTests
    {
        [Test]
        public void Code_EveryLanguage_ReturnsNonEmptyUniqueCode()
        {
            var codes = new System.Collections.Generic.HashSet<string>();
            foreach (Language language in Enum.GetValues(typeof(Language)))
            {
                var code = LanguageInfo.Code(language);
                Assert.IsNotEmpty(code, $"{language} has an empty code");
                Assert.IsTrue(codes.Add(code), $"{language}'s code '{code}' collides with another language");
            }
        }

        [Test]
        public void NativeName_EveryLanguage_ReturnsNonEmptyName()
        {
            foreach (Language language in Enum.GetValues(typeof(Language)))
            {
                Assert.IsNotEmpty(LanguageInfo.NativeName(language), $"{language} has an empty native name");
            }
        }

        [Test]
        public void Code_English_ReturnsEn()
        {
            Assert.AreEqual("en", LanguageInfo.Code(Language.English));
        }

        [Test]
        public void Code_Romanian_ReturnsRo()
        {
            Assert.AreEqual("ro", LanguageInfo.Code(Language.Romanian));
        }

        [Test]
        public void NativeName_Romanian_ReturnsRomana()
        {
            Assert.AreEqual("Română", LanguageInfo.NativeName(Language.Romanian));
        }
    }
}
```

- [ ] **Step 2 (verify): Confirm this cannot be run automatically**

Per Global Constraints, `-runTests` hangs on this project. Do not attempt to run it. Instead, re-read the test file and the `Language`/`LanguageInfo` implementation you're about to write in Step 4 side by side and confirm every assertion matches the implementation exactly (same enum names, same code strings, same native names) before moving on. Real verification happens in Task 14's compile-check build.

- [ ] **Step 4: Write the implementation**

Create `Assets/Framework/Localization/Language.cs`:

```csharp
namespace MobileGamesFramework.Localization
{
    public enum Language
    {
        English,
        Spanish,
        Portuguese,
        German,
        French,
        Japanese,
        Korean,
        Russian,
        ChineseSimplified,
        Italian,
        Romanian
    }

    public static class LanguageInfo
    {
        // ISO-ish code used as the Resources filename for this language's content
        // (see LocalizationLoader) - kept separate from NativeName since a display
        // string and a filename-safe identifier are different concerns.
        public static string Code(Language language)
        {
            switch (language)
            {
                case Language.English: return "en";
                case Language.Spanish: return "es";
                case Language.Portuguese: return "pt";
                case Language.German: return "de";
                case Language.French: return "fr";
                case Language.Japanese: return "ja";
                case Language.Korean: return "ko";
                case Language.Russian: return "ru";
                case Language.ChineseSimplified: return "zh-Hans";
                case Language.Italian: return "it";
                case Language.Romanian: return "ro";
                default: return "en";
            }
        }

        // Always shown in the language itself (not translated into the currently
        // active language) so a player can find their own language in a picker
        // even if they can't read whatever language is currently active.
        public static string NativeName(Language language)
        {
            switch (language)
            {
                case Language.English: return "English";
                case Language.Spanish: return "Español";
                case Language.Portuguese: return "Português";
                case Language.German: return "Deutsch";
                case Language.French: return "Français";
                case Language.Japanese: return "日本語";
                case Language.Korean: return "한국어";
                case Language.Russian: return "Русский";
                case Language.ChineseSimplified: return "简体中文";
                case Language.Italian: return "Italiano";
                case Language.Romanian: return "Română";
                default: return language.ToString();
            }
        }
    }
}
```

- [ ] **Step 5: Commit**

```bash
git add Assets/Framework/Localization/ Assets/Framework/Tests/MobileGamesFramework.Tests.asmdef Assets/Framework/Tests/LanguageInfoTests.cs
git commit -m "feat: add Language enum and native-name table to localization framework module"
```

---

## Task 2: `LocalizationTable` (key lookup with fallback)

**Files:**
- Create: `Assets/Framework/Localization/LocalizationTable.cs`
- Test: `Assets/Framework/Tests/LocalizationTableTests.cs`

**Interfaces:**
- Consumes: nothing from Task 1 directly (works on plain `Dictionary<string,string>`, not `Language`)
- Produces: `public class LocalizationTable { public LocalizationTable(Dictionary<string,string> values, LocalizationTable fallback = null); public string Get(string key); public string Get(string key, params object[] args); }`

- [ ] **Step 1: Write the failing tests**

Create `Assets/Framework/Tests/LocalizationTableTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using MobileGamesFramework.Localization;

namespace MobileGamesFramework.Tests
{
    public class LocalizationTableTests
    {
        [Test]
        public void Get_KeyPresent_ReturnsValue()
        {
            var table = new LocalizationTable(new Dictionary<string, string> { { "hello", "Hello" } });

            Assert.AreEqual("Hello", table.Get("hello"));
        }

        [Test]
        public void Get_KeyMissingNoFallback_ReturnsKeyItself()
        {
            var table = new LocalizationTable(new Dictionary<string, string>());

            Assert.AreEqual("missing.key", table.Get("missing.key"));
        }

        [Test]
        public void Get_KeyMissingFromPrimary_FallsBackToFallbackTable()
        {
            var fallback = new LocalizationTable(new Dictionary<string, string> { { "hello", "Hello" } });
            var table = new LocalizationTable(new Dictionary<string, string>(), fallback);

            Assert.AreEqual("Hello", table.Get("hello"));
        }

        [Test]
        public void Get_KeyPresentInBoth_PrefersPrimaryOverFallback()
        {
            var fallback = new LocalizationTable(new Dictionary<string, string> { { "hello", "Hello" } });
            var table = new LocalizationTable(new Dictionary<string, string> { { "hello", "Bonjour" } }, fallback);

            Assert.AreEqual("Bonjour", table.Get("hello"));
        }

        [Test]
        public void Get_KeyMissingFromBoth_ReturnsKeyItself()
        {
            var fallback = new LocalizationTable(new Dictionary<string, string>());
            var table = new LocalizationTable(new Dictionary<string, string>(), fallback);

            Assert.AreEqual("missing.key", table.Get("missing.key"));
        }

        [Test]
        public void GetWithArgs_FormatsTemplate()
        {
            var table = new LocalizationTable(new Dictionary<string, string> { { "hintsLeft", "Hints left: {0}" } });

            Assert.AreEqual("Hints left: 3", table.Get("hintsLeft", 3));
        }

        [Test]
        public void GetWithArgs_MultiplePlaceholders_FormatsAllOfThem()
        {
            var table = new LocalizationTable(new Dictionary<string, string> { { "time", "Time: {0}   Best: {1}" } });

            Assert.AreEqual("Time: 01:23   Best: 00:45", table.Get("time", "01:23", "00:45"));
        }
    }
}
```

- [ ] **Step 2 (verify): Confirm by inspection**

Per Global Constraints, do not attempt `-runTests`. Re-read these assertions against the implementation in Step 3 before continuing.

- [ ] **Step 3: Write the implementation**

Create `Assets/Framework/Localization/LocalizationTable.cs`:

```csharp
using System.Collections.Generic;

namespace MobileGamesFramework.Localization
{
    public class LocalizationTable
    {
        private readonly Dictionary<string, string> _values;
        private readonly LocalizationTable _fallback;

        public LocalizationTable(Dictionary<string, string> values, LocalizationTable fallback = null)
        {
            _values = values;
            _fallback = fallback;
        }

        // Falls back to the fallback table on a miss, and to the raw key itself if
        // even the fallback doesn't have it - a visibly broken label beats a blank
        // one or a crash, and is easy to spot in a screenshot during verification.
        public string Get(string key)
        {
            if (_values.TryGetValue(key, out var value)) return value;
            if (_fallback != null) return _fallback.Get(key);
            return key;
        }

        public string Get(string key, params object[] args) => string.Format(Get(key), args);
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add Assets/Framework/Localization/LocalizationTable.cs Assets/Framework/Tests/LocalizationTableTests.cs
git commit -m "feat: add LocalizationTable key lookup with fallback"
```

---

## Task 3: `LocalizationLoader` (system-language detection + JSON loading)

**Files:**
- Create: `Assets/Framework/Localization/LocalizationLoader.cs`
- Test: `Assets/Framework/Tests/LocalizationLoaderTests.cs`

**Interfaces:**
- Consumes: `Language`, `LanguageInfo.Code` (Task 1)
- Produces: `public static class LocalizationLoader { public static Language DetectSystemLanguage(); public static Language MapSystemLanguage(SystemLanguage systemLanguage); public static Dictionary<string,string> LoadValues(Language language, string resourcesFolder); }`

- [ ] **Step 1: Write the failing tests (mapping only - `LoadValues` needs `Resources.Load` and isn't unit-tested, see Step 2)**

Create `Assets/Framework/Tests/LocalizationLoaderTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using MobileGamesFramework.Localization;

namespace MobileGamesFramework.Tests
{
    public class LocalizationLoaderTests
    {
        [TestCase(SystemLanguage.English, Language.English)]
        [TestCase(SystemLanguage.Spanish, Language.Spanish)]
        [TestCase(SystemLanguage.Portuguese, Language.Portuguese)]
        [TestCase(SystemLanguage.German, Language.German)]
        [TestCase(SystemLanguage.French, Language.French)]
        [TestCase(SystemLanguage.Japanese, Language.Japanese)]
        [TestCase(SystemLanguage.Korean, Language.Korean)]
        [TestCase(SystemLanguage.Russian, Language.Russian)]
        [TestCase(SystemLanguage.Italian, Language.Italian)]
        [TestCase(SystemLanguage.Romanian, Language.Romanian)]
        public void MapSystemLanguage_DirectlySupportedLanguage_MapsExactly(SystemLanguage systemLanguage, Language expected)
        {
            Assert.AreEqual(expected, LocalizationLoader.MapSystemLanguage(systemLanguage));
        }

        [TestCase(SystemLanguage.Chinese)]
        [TestCase(SystemLanguage.ChineseSimplified)]
        [TestCase(SystemLanguage.ChineseTraditional)]
        public void MapSystemLanguage_AnyChineseVariant_MapsToChineseSimplified(SystemLanguage systemLanguage)
        {
            Assert.AreEqual(Language.ChineseSimplified, LocalizationLoader.MapSystemLanguage(systemLanguage));
        }

        [TestCase(SystemLanguage.Thai)]
        [TestCase(SystemLanguage.Arabic)]
        [TestCase(SystemLanguage.Hindi)]
        [TestCase(SystemLanguage.Dutch)]
        [TestCase(SystemLanguage.Unknown)]
        public void MapSystemLanguage_UnsupportedLanguage_FallsBackToEnglish(SystemLanguage systemLanguage)
        {
            Assert.AreEqual(Language.English, LocalizationLoader.MapSystemLanguage(systemLanguage));
        }
    }
}
```

- [ ] **Step 2 (verify): Confirm by inspection**

Same note as prior tasks - `-runTests` hangs, verify by re-reading the mapping switch statement you write in Step 3 against every `TestCase` above.

- [ ] **Step 3: Write the implementation**

Create `Assets/Framework/Localization/LocalizationLoader.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MobileGamesFramework.Localization
{
    public static class LocalizationLoader
    {
        [Serializable]
        private class LocalizationEntry
        {
            public string key;
            public string value;
        }

        [Serializable]
        private class LocalizationFile
        {
            public LocalizationEntry[] entries;
        }

        public static Language DetectSystemLanguage() => MapSystemLanguage(Application.systemLanguage);

        // Exact matches only - no partial/fuzzy matching, per the design spec. Any
        // SystemLanguage value not listed here (including all three Chinese
        // variants, which map to the single ChineseSimplified content we ship)
        // falls back to English.
        public static Language MapSystemLanguage(SystemLanguage systemLanguage)
        {
            switch (systemLanguage)
            {
                case SystemLanguage.English: return Language.English;
                case SystemLanguage.Spanish: return Language.Spanish;
                case SystemLanguage.Portuguese: return Language.Portuguese;
                case SystemLanguage.German: return Language.German;
                case SystemLanguage.French: return Language.French;
                case SystemLanguage.Japanese: return Language.Japanese;
                case SystemLanguage.Korean: return Language.Korean;
                case SystemLanguage.Russian: return Language.Russian;
                case SystemLanguage.Chinese: return Language.ChineseSimplified;
                case SystemLanguage.ChineseSimplified: return Language.ChineseSimplified;
                case SystemLanguage.ChineseTraditional: return Language.ChineseSimplified;
                case SystemLanguage.Italian: return Language.Italian;
                case SystemLanguage.Romanian: return Language.Romanian;
                default: return Language.English;
            }
        }

        // Not unit-tested (needs Resources.Load, which needs a running Unity
        // instance) - covered by Task 14's compile-check build and Task 15's
        // on-device verification instead.
        public static Dictionary<string, string> LoadValues(Language language, string resourcesFolder)
        {
            var values = new Dictionary<string, string>();
            var path = $"{resourcesFolder}/{LanguageInfo.Code(language)}";
            var asset = Resources.Load<TextAsset>(path);
            if (asset == null)
            {
                Debug.LogWarning($"Localization file not found at Resources/{path}");
                return values;
            }

            var file = JsonUtility.FromJson<LocalizationFile>(asset.text);
            if (file?.entries == null) return values;

            foreach (var entry in file.entries)
                values[entry.key] = entry.value;

            return values;
        }
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add Assets/Framework/Localization/LocalizationLoader.cs Assets/Framework/Tests/LocalizationLoaderTests.cs
git commit -m "feat: add LocalizationLoader with system-language detection and JSON content loading"
```

---

## Task 4: `SudokuLocalizationSettings` (persisted language choice)

**Files:**
- Create: `Assets/Games/Game02_Sudoku/Scripts/SudokuLocalizationSettings.cs`
- Modify: `Assets/Games/Game02_Sudoku/Scripts/Game02_Sudoku.asmdef` (add reference)
- Modify: `Assets/Games/Game02_Sudoku/Tests/Game02_Sudoku.Tests.asmdef` (add reference)
- Test: `Assets/Games/Game02_Sudoku/Tests/SudokuLocalizationSettingsTests.cs`

**Interfaces:**
- Consumes: `IKeyValueStore` (existing), `Language`, `LanguageInfo` (Task 1)
- Produces: `public class SudokuLocalizationSettings { public SudokuLocalizationSettings(IKeyValueStore store); public Language? StoredLanguage { get; } public void SetLanguage(Language language); }`

- [ ] **Step 1: Add the new assembly as a reference for `Game02_Sudoku` and its tests**

In `Assets/Games/Game02_Sudoku/Scripts/Game02_Sudoku.asmdef`, add `"MobileGamesFramework.Localization"` to `"references"`:

```json
    "references": [
        "MobileGamesFramework",
        "MobileGamesFramework.UI",
        "MobileGamesFramework.Localization",
        "UnityEngine.UI",
        "Unity.InputSystem",
        "Unity.Purchasing"
    ],
```

In `Assets/Games/Game02_Sudoku/Tests/Game02_Sudoku.Tests.asmdef`, add it to `"references"` too:

```json
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "MobileGamesFramework",
        "MobileGamesFramework.Localization",
        "Game02_Sudoku"
    ],
```

- [ ] **Step 2: Write the failing tests**

Create `Assets/Games/Game02_Sudoku/Tests/SudokuLocalizationSettingsTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using MobileGamesFramework.Persistence;
using MobileGamesFramework.Localization;
using Game02_Sudoku;

namespace Game02_Sudoku.Tests
{
    public class SudokuLocalizationSettingsTests
    {
        private class FakeKeyValueStore : IKeyValueStore
        {
            private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

            public string GetString(string key, string defaultValue) =>
                _values.TryGetValue(key, out var value) ? value : defaultValue;

            public void SetString(string key, string value) => _values[key] = value;
        }

        [Test]
        public void StoredLanguage_Initially_ReturnsNull()
        {
            var settings = new SudokuLocalizationSettings(new FakeKeyValueStore());

            Assert.IsNull(settings.StoredLanguage);
        }

        [Test]
        public void SetLanguage_ThenStoredLanguage_ReturnsThatLanguage()
        {
            var settings = new SudokuLocalizationSettings(new FakeKeyValueStore());

            settings.SetLanguage(Language.Romanian);

            Assert.AreEqual(Language.Romanian, settings.StoredLanguage);
        }

        [Test]
        public void SetLanguage_Twice_PersistsTheLatestValue()
        {
            var settings = new SudokuLocalizationSettings(new FakeKeyValueStore());

            settings.SetLanguage(Language.Romanian);
            settings.SetLanguage(Language.German);

            Assert.AreEqual(Language.German, settings.StoredLanguage);
        }

        [Test]
        public void StoredLanguage_UnparseableStoredValue_ReturnsNull()
        {
            var store = new FakeKeyValueStore();
            store.SetString("settings.language", "not-a-real-language");
            var settings = new SudokuLocalizationSettings(store);

            Assert.IsNull(settings.StoredLanguage);
        }
    }
}
```

- [ ] **Step 3 (verify): Confirm by inspection**

Same note as prior tasks.

- [ ] **Step 4: Write the implementation**

Create `Assets/Games/Game02_Sudoku/Scripts/SudokuLocalizationSettings.cs`:

```csharp
using MobileGamesFramework.Persistence;
using MobileGamesFramework.Localization;

namespace Game02_Sudoku
{
    public class SudokuLocalizationSettings
    {
        private const string LanguageKey = "settings.language";

        private readonly IKeyValueStore _store;

        public SudokuLocalizationSettings(IKeyValueStore store)
        {
            _store = store;
        }

        // Null means "never set" (first launch) - distinct from any real Language
        // value, which is what tells Loc whether to auto-detect from the system.
        public Language? StoredLanguage
        {
            get
            {
                var raw = _store.GetString(LanguageKey, "");
                if (string.IsNullOrEmpty(raw)) return null;
                return System.Enum.TryParse<Language>(raw, out var language) ? language : (Language?)null;
            }
        }

        public void SetLanguage(Language language) => _store.SetString(LanguageKey, language.ToString());
    }
}
```

- [ ] **Step 5: Commit**

```bash
git add Assets/Games/Game02_Sudoku/Scripts/SudokuLocalizationSettings.cs Assets/Games/Game02_Sudoku/Scripts/Game02_Sudoku.asmdef Assets/Games/Game02_Sudoku/Tests/Game02_Sudoku.Tests.asmdef Assets/Games/Game02_Sudoku/Tests/SudokuLocalizationSettingsTests.cs
git commit -m "feat: add SudokuLocalizationSettings for persisted language choice"
```

---

## Task 5: `Loc` static facade

**Files:**
- Create: `Assets/Games/Game02_Sudoku/Scripts/Loc.cs`

**Interfaces:**
- Consumes: `SudokuLocalizationSettings` (Task 4), `LocalizationLoader`, `LocalizationTable`, `Language`, `LanguageInfo` (Tasks 1-3), `PlayerPrefsStore` (existing, `Game02_Sudoku` namespace), `Difficulty` (existing enum)
- Produces: `public static class Loc { public static Language CurrentLanguage { get; } public static void SetLanguage(Language language); public static string Get(string key); public static string Get(string key, params object[] args); public static string Difficulty(Difficulty difficulty); }`

No automated test for this task: it's a static-state orchestration facade over `Resources.Load` (via `LocalizationLoader`), which isn't unit-testable in this project (see Global Constraints). It's exercised end-to-end by every controller task from Task 10 onward and by Task 15's on-device verification.

- [ ] **Step 1: Write the implementation**

Create `Assets/Games/Game02_Sudoku/Scripts/Loc.cs`:

```csharp
using MobileGamesFramework.Localization;
using MobileGamesFramework.Persistence;

namespace Game02_Sudoku
{
    public static class Loc
    {
        private const string ResourcesFolder = "Localization/Sudoku";

        private static LocalizationTable _table;
        private static Language _currentLanguage;

        private static SudokuLocalizationSettings Settings => new SudokuLocalizationSettings(new PlayerPrefsStore());

        private static void EnsureLoaded()
        {
            if (_table != null) return;

            var settings = Settings;
            var language = settings.StoredLanguage;
            if (!language.HasValue)
            {
                // First launch: detect once, then persist immediately so every
                // later launch (and every later Loc call this session) behaves
                // exactly like an explicit user pick from here on.
                language = LocalizationLoader.DetectSystemLanguage();
                settings.SetLanguage(language.Value);
            }

            LoadLanguage(language.Value);
        }

        private static void LoadLanguage(Language language)
        {
            var englishTable = new LocalizationTable(LocalizationLoader.LoadValues(Language.English, ResourcesFolder));

            _table = language == Language.English
                ? englishTable
                : new LocalizationTable(LocalizationLoader.LoadValues(language, ResourcesFolder), englishTable);

            _currentLanguage = language;
        }

        public static Language CurrentLanguage
        {
            get { EnsureLoaded(); return _currentLanguage; }
        }

        // Called by the Settings screen's language picker. Persists the choice and
        // reloads this static cache; the caller is still responsible for reloading
        // the active scene so on-screen text actually refreshes (see SudokuSettingsController).
        public static void SetLanguage(Language language)
        {
            Settings.SetLanguage(language);
            LoadLanguage(language);
        }

        public static string Get(string key)
        {
            EnsureLoaded();
            return _table.Get(key);
        }

        public static string Get(string key, params object[] args)
        {
            EnsureLoaded();
            return _table.Get(key, args);
        }

        // Difficulty enum names (Easy/Medium/Hard/Expert) map 1:1 to
        // difficulty.easy/medium/hard/expert keys by construction - see
        // Resources/Localization/Sudoku/en.json for the canonical key list.
        public static string Difficulty(Difficulty difficulty) =>
            Get($"difficulty.{difficulty.ToString().ToLowerInvariant()}");
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Games/Game02_Sudoku/Scripts/Loc.cs
git commit -m "feat: add Loc static facade for Sudoku string lookups"
```

---

## Task 6: `SudokuCustomPuzzleError` enum (replace string errors with a code)

Custom-puzzle validation errors are currently hardcoded English strings inside pure game logic (`SudokuCustomPuzzle.TryBuild`). Localizing them means the caller, not the pure logic class, should own the display string - so this task changes `TryBuild` to return an error *code*, and a later task (11) maps that code to `Loc.Get(...)` in `SudokuController`.

**Files:**
- Create: `Assets/Games/Game02_Sudoku/Scripts/SudokuCustomPuzzleError.cs`
- Modify: `Assets/Games/Game02_Sudoku/Scripts/SudokuCustomPuzzle.cs`
- Modify: `Assets/Games/Game02_Sudoku/Tests/SudokuCustomPuzzleTests.cs`

**Interfaces:**
- Produces: `public enum SudokuCustomPuzzleError { None, ConflictingNumbers, NoSolution, MultipleSolutions }`, and changes `SudokuCustomPuzzle.TryBuild`'s signature from `out string error` to `out SudokuCustomPuzzleError error`.

- [ ] **Step 1: Update the existing tests first (they currently assert on a string)**

In `Assets/Games/Game02_Sudoku/Tests/SudokuCustomPuzzleTests.cs`, replace the four string-based assertions:

```csharp
        [Test]
        public void TryBuild_BoardWithConflict_Fails()
        {
            var board = SudokuBoardFactory.CreateEmpty();
            Set(board, 0, 0, 5);
            Set(board, 0, 1, 5);

            var success = SudokuCustomPuzzle.TryBuild(board, out var puzzle, out var error);

            Assert.IsFalse(success);
            Assert.IsNull(puzzle);
            Assert.AreEqual(SudokuCustomPuzzleError.ConflictingNumbers, error);
        }

        [Test]
        public void TryBuild_EmptyBoard_FailsAsNotUnique()
        {
            var board = SudokuBoardFactory.CreateEmpty();

            var success = SudokuCustomPuzzle.TryBuild(board, out var puzzle, out var error);

            Assert.IsFalse(success);
            Assert.IsNull(puzzle);
            Assert.AreEqual(SudokuCustomPuzzleError.NoSolution, error);
        }
```

(The remaining two `TryBuild` tests - `TryBuild_ValidUniqueBoard_Succeeds` etc. - currently do `Assert.IsNull(error)`; change those to `Assert.AreEqual(SudokuCustomPuzzleError.None, error)`.)

- [ ] **Step 2 (verify): Confirm by inspection**

Per Global Constraints, `-runTests` hangs - verify these assertions match the implementation you write in Step 4 by reading both side by side.

- [ ] **Step 3: Create the error enum**

Create `Assets/Games/Game02_Sudoku/Scripts/SudokuCustomPuzzleError.cs`:

```csharp
namespace Game02_Sudoku
{
    public enum SudokuCustomPuzzleError
    {
        None,
        ConflictingNumbers,
        NoSolution,
        MultipleSolutions
    }
}
```

- [ ] **Step 4: Update `SudokuCustomPuzzle.TryBuild`**

In `Assets/Games/Game02_Sudoku/Scripts/SudokuCustomPuzzle.cs`, replace the whole file:

```csharp
using MobileGamesFramework.Grid;

namespace Game02_Sudoku
{
    public static class SudokuCustomPuzzle
    {
        public static bool TryBuild(GridCore<SudokuCell> board, out SudokuPuzzle puzzle, out SudokuCustomPuzzleError error)
        {
            puzzle = null;

            if (SudokuSolver.FindConflicts(board).Count > 0)
            {
                error = SudokuCustomPuzzleError.ConflictingNumbers;
                return false;
            }

            var solutionCount = SudokuSolver.CountSolutions(board, 2);
            if (solutionCount == 0)
            {
                error = SudokuCustomPuzzleError.NoSolution;
                return false;
            }

            if (solutionCount > 1)
            {
                error = SudokuCustomPuzzleError.MultipleSolutions;
                return false;
            }

            if (!SudokuSolver.TrySolve(board, null, out var solution))
            {
                error = SudokuCustomPuzzleError.NoSolution;
                return false;
            }

            var givenBoard = board.Clone();
            foreach (var pos in givenBoard.AllPositions())
            {
                var cell = givenBoard.Get(pos).Value;
                if (cell.Value == 0) continue;
                cell.IsGiven = true;
                givenBoard.Set(pos, cell);
            }

            puzzle = new SudokuPuzzle { Board = givenBoard, Solution = solution };
            error = SudokuCustomPuzzleError.None;
            return true;
        }
    }
}
```

- [ ] **Step 5: Commit**

```bash
git add Assets/Games/Game02_Sudoku/Scripts/SudokuCustomPuzzleError.cs Assets/Games/Game02_Sudoku/Scripts/SudokuCustomPuzzle.cs Assets/Games/Game02_Sudoku/Tests/SudokuCustomPuzzleTests.cs
git commit -m "refactor: SudokuCustomPuzzle.TryBuild returns an error code instead of an English string"
```

Note: `SudokuController` (which calls `TryBuild` and stores its `out error` in the `_editError` field) is NOT updated in this task - it still has a `string _editError` field referencing the old signature. This will not compile until Task 11. That's expected and fine within this plan's task boundaries (Task 11 fixes it); if executing tasks out of order or pausing mid-plan, be aware the project won't build between Task 6 and Task 11.

---

## Task 7: Author the English localization content (authoritative key list)

**Files:**
- Create: `Assets/Resources/Localization/Sudoku/en.json`

This is the authoritative source of every translatable key in the game - every other language file (Task 8) must contain exactly this same key set. 50 keys total.

- [ ] **Step 1: Write the file**

Create `Assets/Resources/Localization/Sudoku/en.json`:

```json
{
  "entries": [
    { "key": "common.back", "value": "Back" },
    { "key": "common.cancel", "value": "Cancel" },
    { "key": "menu.newGame", "value": "New Game" },
    { "key": "menu.continueGame", "value": "Continue" },
    { "key": "menu.highScores", "value": "High Scores" },
    { "key": "menu.exitGame", "value": "Exit Game" },
    { "key": "menu.tagline", "value": "No ads. Ever." },
    { "key": "menu.chooseDifficulty", "value": "Choose Difficulty" },
    { "key": "menu.custom", "value": "Custom" },
    { "key": "difficulty.easy", "value": "Easy" },
    { "key": "difficulty.medium", "value": "Medium" },
    { "key": "difficulty.hard", "value": "Hard" },
    { "key": "difficulty.expert", "value": "Expert" },
    { "key": "play.clear", "value": "Clear" },
    { "key": "play.verify", "value": "Verify" },
    { "key": "play.undo", "value": "Undo" },
    { "key": "play.hint", "value": "Hint" },
    { "key": "play.autofill", "value": "Autofill" },
    { "key": "play.generate", "value": "Generate" },
    { "key": "play.start", "value": "Start" },
    { "key": "play.clearGrid", "value": "Clear Grid" },
    { "key": "play.watchAdHint", "value": "Watch Ad +1 Hint" },
    { "key": "play.hintsLeft", "value": "Hints left: {0}" },
    { "key": "play.solved", "value": "Solved!" },
    { "key": "play.time", "value": "Time: {0}" },
    { "key": "play.timeWithBest", "value": "Time: {0}   Best: {1}" },
    { "key": "editor.buildingHint", "value": "Building custom puzzle — pick a number, then tap cells to fill." },
    { "key": "editor.errorConflict", "value": "Board has conflicting numbers." },
    { "key": "editor.errorNoSolution", "value": "No valid solution exists for this puzzle." },
    { "key": "editor.errorMultipleSolutions", "value": "Puzzle has multiple solutions — add more numbers." },
    { "key": "popup.generateBody", "value": "Generate a puzzle to start from -\nyou can still edit it before hitting Start." },
    { "key": "popup.successTime", "value": "Time: {0}" },
    { "key": "popup.successTimeAutofilled", "value": "Time: {0} (autofilled - not recorded)" },
    { "key": "popup.newPuzzle", "value": "New Puzzle" },
    { "key": "popup.menu", "value": "Menu" },
    { "key": "settings.title", "value": "Settings" },
    { "key": "settings.resetData", "value": "Reset Data" },
    { "key": "settings.musicOn", "value": "Music: On" },
    { "key": "settings.musicOff", "value": "Music: Off" },
    { "key": "settings.sfxOn", "value": "Sound Effects: On" },
    { "key": "settings.sfxOff", "value": "Sound Effects: Off" },
    { "key": "settings.adsTestOn", "value": "Ads (testing): On" },
    { "key": "settings.adsTestOff", "value": "Ads (testing): Off" },
    { "key": "settings.resetConfirmBody", "value": "Reset your saved game and all\nhigh scores? This can't be undone." },
    { "key": "settings.reset", "value": "Reset" },
    { "key": "settings.language", "value": "Language" },
    { "key": "highscores.title", "value": "High Scores" },
    { "key": "highscores.noTimes", "value": "No times recorded yet." },
    { "key": "highscores.completed", "value": "Completed: {0}" },
    { "key": "highscores.clearLeaderboard", "value": "Clear Leaderboard" }
  ]
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Resources/Localization/Sudoku/en.json
git commit -m "feat: author English localization content for Sudoku"
```

---

## Task 8: Author the remaining 10 language files

**Files:**
- Create: `Assets/Resources/Localization/Sudoku/es.json`
- Create: `Assets/Resources/Localization/Sudoku/pt.json`
- Create: `Assets/Resources/Localization/Sudoku/de.json`
- Create: `Assets/Resources/Localization/Sudoku/fr.json`
- Create: `Assets/Resources/Localization/Sudoku/ja.json`
- Create: `Assets/Resources/Localization/Sudoku/ko.json`
- Create: `Assets/Resources/Localization/Sudoku/ru.json`
- Create: `Assets/Resources/Localization/Sudoku/zh-Hans.json`
- Create: `Assets/Resources/Localization/Sudoku/it.json`
- Create: `Assets/Resources/Localization/Sudoku/ro.json`

Each file uses the identical 50-key set and `{"entries":[{"key":...,"value":...}]}` schema as Task 7's `en.json`, translated. Every `{0}`/`{1}` placeholder must appear verbatim (untranslated) in every language, in the same key.

- [ ] **Step 1: Spanish** - create `Assets/Resources/Localization/Sudoku/es.json`:

```json
{
  "entries": [
    { "key": "common.back", "value": "Atrás" },
    { "key": "common.cancel", "value": "Cancelar" },
    { "key": "menu.newGame", "value": "Nueva Partida" },
    { "key": "menu.continueGame", "value": "Continuar" },
    { "key": "menu.highScores", "value": "Puntuaciones" },
    { "key": "menu.exitGame", "value": "Salir" },
    { "key": "menu.tagline", "value": "Sin anuncios. Nunca." },
    { "key": "menu.chooseDifficulty", "value": "Elige Dificultad" },
    { "key": "menu.custom", "value": "Personalizado" },
    { "key": "difficulty.easy", "value": "Fácil" },
    { "key": "difficulty.medium", "value": "Medio" },
    { "key": "difficulty.hard", "value": "Difícil" },
    { "key": "difficulty.expert", "value": "Experto" },
    { "key": "play.clear", "value": "Borrar" },
    { "key": "play.verify", "value": "Verificar" },
    { "key": "play.undo", "value": "Deshacer" },
    { "key": "play.hint", "value": "Pista" },
    { "key": "play.autofill", "value": "Autocompletar" },
    { "key": "play.generate", "value": "Generar" },
    { "key": "play.start", "value": "Iniciar" },
    { "key": "play.clearGrid", "value": "Borrar Tablero" },
    { "key": "play.watchAdHint", "value": "Ver Anuncio +1 Pista" },
    { "key": "play.hintsLeft", "value": "Pistas restantes: {0}" },
    { "key": "play.solved", "value": "¡Resuelto!" },
    { "key": "play.time", "value": "Tiempo: {0}" },
    { "key": "play.timeWithBest", "value": "Tiempo: {0}   Mejor: {1}" },
    { "key": "editor.buildingHint", "value": "Creando puzzle personalizado — elige un número y toca las celdas para rellenar." },
    { "key": "editor.errorConflict", "value": "El tablero tiene números en conflicto." },
    { "key": "editor.errorNoSolution", "value": "No existe una solución válida para este puzzle." },
    { "key": "editor.errorMultipleSolutions", "value": "El puzzle tiene varias soluciones — añade más números." },
    { "key": "popup.generateBody", "value": "Genera un puzzle desde el que empezar -\ntodavía puedes editarlo antes de pulsar Iniciar." },
    { "key": "popup.successTime", "value": "Tiempo: {0}" },
    { "key": "popup.successTimeAutofilled", "value": "Tiempo: {0} (autocompletado - no registrado)" },
    { "key": "popup.newPuzzle", "value": "Nuevo Puzzle" },
    { "key": "popup.menu", "value": "Menú" },
    { "key": "settings.title", "value": "Ajustes" },
    { "key": "settings.resetData", "value": "Restablecer Datos" },
    { "key": "settings.musicOn", "value": "Música: Activada" },
    { "key": "settings.musicOff", "value": "Música: Desactivada" },
    { "key": "settings.sfxOn", "value": "Efectos de Sonido: Activados" },
    { "key": "settings.sfxOff", "value": "Efectos de Sonido: Desactivados" },
    { "key": "settings.adsTestOn", "value": "Anuncios (prueba): Activados" },
    { "key": "settings.adsTestOff", "value": "Anuncios (prueba): Desactivados" },
    { "key": "settings.resetConfirmBody", "value": "¿Restablecer tu partida guardada y todas\nlas puntuaciones? Esto no se puede deshacer." },
    { "key": "settings.reset", "value": "Restablecer" },
    { "key": "settings.language", "value": "Idioma" },
    { "key": "highscores.title", "value": "Puntuaciones" },
    { "key": "highscores.noTimes", "value": "Aún no hay tiempos registrados." },
    { "key": "highscores.completed", "value": "Completados: {0}" },
    { "key": "highscores.clearLeaderboard", "value": "Borrar Tabla de Puntuaciones" }
  ]
}
```

- [ ] **Step 2: Portuguese** - create `Assets/Resources/Localization/Sudoku/pt.json`:

```json
{
  "entries": [
    { "key": "common.back", "value": "Voltar" },
    { "key": "common.cancel", "value": "Cancelar" },
    { "key": "menu.newGame", "value": "Novo Jogo" },
    { "key": "menu.continueGame", "value": "Continuar" },
    { "key": "menu.highScores", "value": "Recordes" },
    { "key": "menu.exitGame", "value": "Sair do Jogo" },
    { "key": "menu.tagline", "value": "Sem anúncios. Nunca." },
    { "key": "menu.chooseDifficulty", "value": "Escolha a Dificuldade" },
    { "key": "menu.custom", "value": "Personalizado" },
    { "key": "difficulty.easy", "value": "Fácil" },
    { "key": "difficulty.medium", "value": "Médio" },
    { "key": "difficulty.hard", "value": "Difícil" },
    { "key": "difficulty.expert", "value": "Especialista" },
    { "key": "play.clear", "value": "Limpar" },
    { "key": "play.verify", "value": "Verificar" },
    { "key": "play.undo", "value": "Desfazer" },
    { "key": "play.hint", "value": "Dica" },
    { "key": "play.autofill", "value": "Preencher Automático" },
    { "key": "play.generate", "value": "Gerar" },
    { "key": "play.start", "value": "Iniciar" },
    { "key": "play.clearGrid", "value": "Limpar Tabuleiro" },
    { "key": "play.watchAdHint", "value": "Assistir Anúncio +1 Dica" },
    { "key": "play.hintsLeft", "value": "Dicas restantes: {0}" },
    { "key": "play.solved", "value": "Resolvido!" },
    { "key": "play.time", "value": "Tempo: {0}" },
    { "key": "play.timeWithBest", "value": "Tempo: {0}   Melhor: {1}" },
    { "key": "editor.buildingHint", "value": "Criando quebra-cabeça personalizado — escolha um número e toque nas células para preencher." },
    { "key": "editor.errorConflict", "value": "O tabuleiro tem números conflitantes." },
    { "key": "editor.errorNoSolution", "value": "Não existe solução válida para este quebra-cabeça." },
    { "key": "editor.errorMultipleSolutions", "value": "O quebra-cabeça tem várias soluções — adicione mais números." },
    { "key": "popup.generateBody", "value": "Gere um quebra-cabeça para começar -\nvocê ainda pode editá-lo antes de tocar em Iniciar." },
    { "key": "popup.successTime", "value": "Tempo: {0}" },
    { "key": "popup.successTimeAutofilled", "value": "Tempo: {0} (preenchido automaticamente - não registrado)" },
    { "key": "popup.newPuzzle", "value": "Novo Quebra-cabeça" },
    { "key": "popup.menu", "value": "Menu" },
    { "key": "settings.title", "value": "Configurações" },
    { "key": "settings.resetData", "value": "Redefinir Dados" },
    { "key": "settings.musicOn", "value": "Música: Ativada" },
    { "key": "settings.musicOff", "value": "Música: Desativada" },
    { "key": "settings.sfxOn", "value": "Efeitos Sonoros: Ativados" },
    { "key": "settings.sfxOff", "value": "Efeitos Sonoros: Desativados" },
    { "key": "settings.adsTestOn", "value": "Anúncios (teste): Ativados" },
    { "key": "settings.adsTestOff", "value": "Anúncios (teste): Desativados" },
    { "key": "settings.resetConfirmBody", "value": "Redefinir seu jogo salvo e todos\nos recordes? Isso não pode ser desfeito." },
    { "key": "settings.reset", "value": "Redefinir" },
    { "key": "settings.language", "value": "Idioma" },
    { "key": "highscores.title", "value": "Recordes" },
    { "key": "highscores.noTimes", "value": "Nenhum tempo registrado ainda." },
    { "key": "highscores.completed", "value": "Concluídos: {0}" },
    { "key": "highscores.clearLeaderboard", "value": "Limpar Ranking" }
  ]
}
```

- [ ] **Step 3: German** - create `Assets/Resources/Localization/Sudoku/de.json`:

```json
{
  "entries": [
    { "key": "common.back", "value": "Zurück" },
    { "key": "common.cancel", "value": "Abbrechen" },
    { "key": "menu.newGame", "value": "Neues Spiel" },
    { "key": "menu.continueGame", "value": "Fortsetzen" },
    { "key": "menu.highScores", "value": "Bestenliste" },
    { "key": "menu.exitGame", "value": "Spiel beenden" },
    { "key": "menu.tagline", "value": "Keine Werbung. Nie." },
    { "key": "menu.chooseDifficulty", "value": "Schwierigkeit wählen" },
    { "key": "menu.custom", "value": "Benutzerdefiniert" },
    { "key": "difficulty.easy", "value": "Leicht" },
    { "key": "difficulty.medium", "value": "Mittel" },
    { "key": "difficulty.hard", "value": "Schwer" },
    { "key": "difficulty.expert", "value": "Experte" },
    { "key": "play.clear", "value": "Löschen" },
    { "key": "play.verify", "value": "Prüfen" },
    { "key": "play.undo", "value": "Rückgängig" },
    { "key": "play.hint", "value": "Tipp" },
    { "key": "play.autofill", "value": "Automatisch ausfüllen" },
    { "key": "play.generate", "value": "Generieren" },
    { "key": "play.start", "value": "Start" },
    { "key": "play.clearGrid", "value": "Raster löschen" },
    { "key": "play.watchAdHint", "value": "Werbung ansehen +1 Tipp" },
    { "key": "play.hintsLeft", "value": "Verbleibende Tipps: {0}" },
    { "key": "play.solved", "value": "Gelöst!" },
    { "key": "play.time", "value": "Zeit: {0}" },
    { "key": "play.timeWithBest", "value": "Zeit: {0}   Beste: {1}" },
    { "key": "editor.buildingHint", "value": "Eigenes Rätsel erstellen — Zahl wählen, dann Zellen antippen, um sie zu füllen." },
    { "key": "editor.errorConflict", "value": "Das Raster enthält widersprüchliche Zahlen." },
    { "key": "editor.errorNoSolution", "value": "Für dieses Rätsel gibt es keine gültige Lösung." },
    { "key": "editor.errorMultipleSolutions", "value": "Das Rätsel hat mehrere Lösungen — füge weitere Zahlen hinzu." },
    { "key": "popup.generateBody", "value": "Erzeuge ein Rätsel als Ausgangspunkt -\ndu kannst es vor dem Start noch bearbeiten." },
    { "key": "popup.successTime", "value": "Zeit: {0}" },
    { "key": "popup.successTimeAutofilled", "value": "Zeit: {0} (automatisch ausgefüllt - nicht gewertet)" },
    { "key": "popup.newPuzzle", "value": "Neues Rätsel" },
    { "key": "popup.menu", "value": "Menü" },
    { "key": "settings.title", "value": "Einstellungen" },
    { "key": "settings.resetData", "value": "Daten zurücksetzen" },
    { "key": "settings.musicOn", "value": "Musik: An" },
    { "key": "settings.musicOff", "value": "Musik: Aus" },
    { "key": "settings.sfxOn", "value": "Soundeffekte: An" },
    { "key": "settings.sfxOff", "value": "Soundeffekte: Aus" },
    { "key": "settings.adsTestOn", "value": "Werbung (Test): An" },
    { "key": "settings.adsTestOff", "value": "Werbung (Test): Aus" },
    { "key": "settings.resetConfirmBody", "value": "Gespeichertes Spiel und alle\nBestzeiten zurücksetzen? Das kann nicht rückgängig gemacht werden." },
    { "key": "settings.reset", "value": "Zurücksetzen" },
    { "key": "settings.language", "value": "Sprache" },
    { "key": "highscores.title", "value": "Bestenliste" },
    { "key": "highscores.noTimes", "value": "Noch keine Zeiten aufgezeichnet." },
    { "key": "highscores.completed", "value": "Abgeschlossen: {0}" },
    { "key": "highscores.clearLeaderboard", "value": "Bestenliste löschen" }
  ]
}
```

- [ ] **Step 4: French** - create `Assets/Resources/Localization/Sudoku/fr.json`:

```json
{
  "entries": [
    { "key": "common.back", "value": "Retour" },
    { "key": "common.cancel", "value": "Annuler" },
    { "key": "menu.newGame", "value": "Nouvelle Partie" },
    { "key": "menu.continueGame", "value": "Continuer" },
    { "key": "menu.highScores", "value": "Meilleurs Scores" },
    { "key": "menu.exitGame", "value": "Quitter" },
    { "key": "menu.tagline", "value": "Sans pub. Jamais." },
    { "key": "menu.chooseDifficulty", "value": "Choisir la Difficulté" },
    { "key": "menu.custom", "value": "Personnalisé" },
    { "key": "difficulty.easy", "value": "Facile" },
    { "key": "difficulty.medium", "value": "Moyen" },
    { "key": "difficulty.hard", "value": "Difficile" },
    { "key": "difficulty.expert", "value": "Expert" },
    { "key": "play.clear", "value": "Effacer" },
    { "key": "play.verify", "value": "Vérifier" },
    { "key": "play.undo", "value": "Annuler le coup" },
    { "key": "play.hint", "value": "Indice" },
    { "key": "play.autofill", "value": "Remplissage Auto" },
    { "key": "play.generate", "value": "Générer" },
    { "key": "play.start", "value": "Démarrer" },
    { "key": "play.clearGrid", "value": "Effacer la Grille" },
    { "key": "play.watchAdHint", "value": "Voir une Pub +1 Indice" },
    { "key": "play.hintsLeft", "value": "Indices restants : {0}" },
    { "key": "play.solved", "value": "Résolu !" },
    { "key": "play.time", "value": "Temps : {0}" },
    { "key": "play.timeWithBest", "value": "Temps : {0}   Meilleur : {1}" },
    { "key": "editor.buildingHint", "value": "Création d'une grille personnalisée — choisissez un chiffre, puis touchez les cases à remplir." },
    { "key": "editor.errorConflict", "value": "La grille contient des chiffres en conflit." },
    { "key": "editor.errorNoSolution", "value": "Aucune solution valide n'existe pour cette grille." },
    { "key": "editor.errorMultipleSolutions", "value": "Cette grille a plusieurs solutions — ajoutez d'autres chiffres." },
    { "key": "popup.generateBody", "value": "Générez une grille de départ -\nvous pourrez encore la modifier avant d'appuyer sur Démarrer." },
    { "key": "popup.successTime", "value": "Temps : {0}" },
    { "key": "popup.successTimeAutofilled", "value": "Temps : {0} (rempli automatiquement - non enregistré)" },
    { "key": "popup.newPuzzle", "value": "Nouvelle Grille" },
    { "key": "popup.menu", "value": "Menu" },
    { "key": "settings.title", "value": "Réglages" },
    { "key": "settings.resetData", "value": "Réinitialiser les Données" },
    { "key": "settings.musicOn", "value": "Musique : Activée" },
    { "key": "settings.musicOff", "value": "Musique : Désactivée" },
    { "key": "settings.sfxOn", "value": "Effets Sonores : Activés" },
    { "key": "settings.sfxOff", "value": "Effets Sonores : Désactivés" },
    { "key": "settings.adsTestOn", "value": "Pubs (test) : Activées" },
    { "key": "settings.adsTestOff", "value": "Pubs (test) : Désactivées" },
    { "key": "settings.resetConfirmBody", "value": "Réinitialiser votre partie sauvegardée et tous\nles meilleurs scores ? Action irréversible." },
    { "key": "settings.reset", "value": "Réinitialiser" },
    { "key": "settings.language", "value": "Langue" },
    { "key": "highscores.title", "value": "Meilleurs Scores" },
    { "key": "highscores.noTimes", "value": "Aucun temps enregistré pour l'instant." },
    { "key": "highscores.completed", "value": "Terminées : {0}" },
    { "key": "highscores.clearLeaderboard", "value": "Effacer le Classement" }
  ]
}
```

- [ ] **Step 5: Japanese** - create `Assets/Resources/Localization/Sudoku/ja.json`:

```json
{
  "entries": [
    { "key": "common.back", "value": "戻る" },
    { "key": "common.cancel", "value": "キャンセル" },
    { "key": "menu.newGame", "value": "新しいゲーム" },
    { "key": "menu.continueGame", "value": "続きから" },
    { "key": "menu.highScores", "value": "ハイスコア" },
    { "key": "menu.exitGame", "value": "ゲームを終了" },
    { "key": "menu.tagline", "value": "広告なし。ずっと。" },
    { "key": "menu.chooseDifficulty", "value": "難易度を選択" },
    { "key": "menu.custom", "value": "カスタム" },
    { "key": "difficulty.easy", "value": "かんたん" },
    { "key": "difficulty.medium", "value": "ふつう" },
    { "key": "difficulty.hard", "value": "むずかしい" },
    { "key": "difficulty.expert", "value": "エキスパート" },
    { "key": "play.clear", "value": "消去" },
    { "key": "play.verify", "value": "確認" },
    { "key": "play.undo", "value": "元に戻す" },
    { "key": "play.hint", "value": "ヒント" },
    { "key": "play.autofill", "value": "自動入力" },
    { "key": "play.generate", "value": "生成" },
    { "key": "play.start", "value": "スタート" },
    { "key": "play.clearGrid", "value": "グリッドを消去" },
    { "key": "play.watchAdHint", "value": "広告を見て+1ヒント" },
    { "key": "play.hintsLeft", "value": "残りヒント数: {0}" },
    { "key": "play.solved", "value": "完成!" },
    { "key": "play.time", "value": "タイム: {0}" },
    { "key": "play.timeWithBest", "value": "タイム: {0}   ベスト: {1}" },
    { "key": "editor.buildingHint", "value": "カスタムパズルを作成中 — 数字を選んでマスをタップして入力してください。" },
    { "key": "editor.errorConflict", "value": "盤面に重複する数字があります。" },
    { "key": "editor.errorNoSolution", "value": "このパズルには有効な解答がありません。" },
    { "key": "editor.errorMultipleSolutions", "value": "このパズルには複数の解答があります — 数字を追加してください。" },
    { "key": "popup.generateBody", "value": "元になるパズルを生成します -\nスタートを押す前に編集することもできます。" },
    { "key": "popup.successTime", "value": "タイム: {0}" },
    { "key": "popup.successTimeAutofilled", "value": "タイム: {0}(自動入力 - 記録されません)" },
    { "key": "popup.newPuzzle", "value": "新しいパズル" },
    { "key": "popup.menu", "value": "メニュー" },
    { "key": "settings.title", "value": "設定" },
    { "key": "settings.resetData", "value": "データをリセット" },
    { "key": "settings.musicOn", "value": "音楽: オン" },
    { "key": "settings.musicOff", "value": "音楽: オフ" },
    { "key": "settings.sfxOn", "value": "効果音: オン" },
    { "key": "settings.sfxOff", "value": "効果音: オフ" },
    { "key": "settings.adsTestOn", "value": "広告(テスト): オン" },
    { "key": "settings.adsTestOff", "value": "広告(テスト): オフ" },
    { "key": "settings.resetConfirmBody", "value": "保存したゲームとすべての\nハイスコアをリセットしますか?元に戻せません。" },
    { "key": "settings.reset", "value": "リセット" },
    { "key": "settings.language", "value": "言語" },
    { "key": "highscores.title", "value": "ハイスコア" },
    { "key": "highscores.noTimes", "value": "記録されたタイムはまだありません。" },
    { "key": "highscores.completed", "value": "完了: {0}" },
    { "key": "highscores.clearLeaderboard", "value": "ハイスコアをリセット" }
  ]
}
```

- [ ] **Step 6: Korean** - create `Assets/Resources/Localization/Sudoku/ko.json`:

```json
{
  "entries": [
    { "key": "common.back", "value": "뒤로" },
    { "key": "common.cancel", "value": "취소" },
    { "key": "menu.newGame", "value": "새 게임" },
    { "key": "menu.continueGame", "value": "이어하기" },
    { "key": "menu.highScores", "value": "최고 기록" },
    { "key": "menu.exitGame", "value": "게임 종료" },
    { "key": "menu.tagline", "value": "광고 없음. 영원히." },
    { "key": "menu.chooseDifficulty", "value": "난이도 선택" },
    { "key": "menu.custom", "value": "커스텀" },
    { "key": "difficulty.easy", "value": "쉬움" },
    { "key": "difficulty.medium", "value": "보통" },
    { "key": "difficulty.hard", "value": "어려움" },
    { "key": "difficulty.expert", "value": "전문가" },
    { "key": "play.clear", "value": "지우기" },
    { "key": "play.verify", "value": "검증" },
    { "key": "play.undo", "value": "실행 취소" },
    { "key": "play.hint", "value": "힌트" },
    { "key": "play.autofill", "value": "자동 채우기" },
    { "key": "play.generate", "value": "생성" },
    { "key": "play.start", "value": "시작" },
    { "key": "play.clearGrid", "value": "판 지우기" },
    { "key": "play.watchAdHint", "value": "광고 보고 +1 힌트" },
    { "key": "play.hintsLeft", "value": "남은 힌트: {0}" },
    { "key": "play.solved", "value": "완료!" },
    { "key": "play.time", "value": "시간: {0}" },
    { "key": "play.timeWithBest", "value": "시간: {0}   최고 기록: {1}" },
    { "key": "editor.buildingHint", "value": "커스텀 퍼즐 만드는 중 — 숫자를 선택한 다음 칸을 눌러 채우세요." },
    { "key": "editor.errorConflict", "value": "보드에 충돌하는 숫자가 있습니다." },
    { "key": "editor.errorNoSolution", "value": "이 퍼즐에는 유효한 해가 없습니다." },
    { "key": "editor.errorMultipleSolutions", "value": "이 퍼즐에는 여러 개의 해가 있습니다 — 숫자를 더 추가하세요." },
    { "key": "popup.generateBody", "value": "시작할 퍼즐을 생성합니다 -\n시작을 누르기 전에 편집할 수 있습니다." },
    { "key": "popup.successTime", "value": "시간: {0}" },
    { "key": "popup.successTimeAutofilled", "value": "시간: {0} (자동 채우기 - 기록되지 않음)" },
    { "key": "popup.newPuzzle", "value": "새 퍼즐" },
    { "key": "popup.menu", "value": "메뉴" },
    { "key": "settings.title", "value": "설정" },
    { "key": "settings.resetData", "value": "데이터 초기화" },
    { "key": "settings.musicOn", "value": "음악: 켜짐" },
    { "key": "settings.musicOff", "value": "음악: 꺼짐" },
    { "key": "settings.sfxOn", "value": "효과음: 켜짐" },
    { "key": "settings.sfxOff", "value": "효과음: 꺼짐" },
    { "key": "settings.adsTestOn", "value": "광고(테스트): 켜짐" },
    { "key": "settings.adsTestOff", "value": "광고(테스트): 꺼짐" },
    { "key": "settings.resetConfirmBody", "value": "저장된 게임과 모든\n최고 기록을 초기화할까요? 되돌릴 수 없습니다." },
    { "key": "settings.reset", "value": "초기화" },
    { "key": "settings.language", "value": "언어" },
    { "key": "highscores.title", "value": "최고 기록" },
    { "key": "highscores.noTimes", "value": "아직 기록된 시간이 없습니다." },
    { "key": "highscores.completed", "value": "완료: {0}" },
    { "key": "highscores.clearLeaderboard", "value": "최고 기록 지우기" }
  ]
}
```

- [ ] **Step 7: Russian** - create `Assets/Resources/Localization/Sudoku/ru.json`:

```json
{
  "entries": [
    { "key": "common.back", "value": "Назад" },
    { "key": "common.cancel", "value": "Отмена" },
    { "key": "menu.newGame", "value": "Новая игра" },
    { "key": "menu.continueGame", "value": "Продолжить" },
    { "key": "menu.highScores", "value": "Рекорды" },
    { "key": "menu.exitGame", "value": "Выйти из игры" },
    { "key": "menu.tagline", "value": "Без рекламы. Никогда." },
    { "key": "menu.chooseDifficulty", "value": "Выберите сложность" },
    { "key": "menu.custom", "value": "Свой вариант" },
    { "key": "difficulty.easy", "value": "Лёгкий" },
    { "key": "difficulty.medium", "value": "Средний" },
    { "key": "difficulty.hard", "value": "Сложный" },
    { "key": "difficulty.expert", "value": "Эксперт" },
    { "key": "play.clear", "value": "Очистить" },
    { "key": "play.verify", "value": "Проверить" },
    { "key": "play.undo", "value": "Отменить ход" },
    { "key": "play.hint", "value": "Подсказка" },
    { "key": "play.autofill", "value": "Автозаполнение" },
    { "key": "play.generate", "value": "Сгенерировать" },
    { "key": "play.start", "value": "Старт" },
    { "key": "play.clearGrid", "value": "Очистить поле" },
    { "key": "play.watchAdHint", "value": "Смотреть рекламу +1 подсказка" },
    { "key": "play.hintsLeft", "value": "Осталось подсказок: {0}" },
    { "key": "play.solved", "value": "Решено!" },
    { "key": "play.time", "value": "Время: {0}" },
    { "key": "play.timeWithBest", "value": "Время: {0}   Лучшее: {1}" },
    { "key": "editor.buildingHint", "value": "Создание своей судоку — выберите цифру, затем нажимайте на клетки, чтобы заполнить их." },
    { "key": "editor.errorConflict", "value": "На поле есть конфликтующие цифры." },
    { "key": "editor.errorNoSolution", "value": "Для этой головоломки не существует верного решения." },
    { "key": "editor.errorMultipleSolutions", "value": "У этой головоломки несколько решений — добавьте ещё цифр." },
    { "key": "popup.generateBody", "value": "Сгенерировать головоломку для начала -\nвы всё ещё сможете отредактировать её перед началом игры." },
    { "key": "popup.successTime", "value": "Время: {0}" },
    { "key": "popup.successTimeAutofilled", "value": "Время: {0} (автозаполнение - не засчитано)" },
    { "key": "popup.newPuzzle", "value": "Новая головоломка" },
    { "key": "popup.menu", "value": "Меню" },
    { "key": "settings.title", "value": "Настройки" },
    { "key": "settings.resetData", "value": "Сбросить данные" },
    { "key": "settings.musicOn", "value": "Музыка: Вкл" },
    { "key": "settings.musicOff", "value": "Музыка: Выкл" },
    { "key": "settings.sfxOn", "value": "Звуковые эффекты: Вкл" },
    { "key": "settings.sfxOff", "value": "Звуковые эффекты: Выкл" },
    { "key": "settings.adsTestOn", "value": "Реклама (тест): Вкл" },
    { "key": "settings.adsTestOff", "value": "Реклама (тест): Выкл" },
    { "key": "settings.resetConfirmBody", "value": "Сбросить сохранённую игру и все\nрекорды? Это действие нельзя отменить." },
    { "key": "settings.reset", "value": "Сбросить" },
    { "key": "settings.language", "value": "Язык" },
    { "key": "highscores.title", "value": "Рекорды" },
    { "key": "highscores.noTimes", "value": "Пока нет сохранённых результатов." },
    { "key": "highscores.completed", "value": "Завершено: {0}" },
    { "key": "highscores.clearLeaderboard", "value": "Очистить таблицу рекордов" }
  ]
}
```

- [ ] **Step 8: Chinese (Simplified)** - create `Assets/Resources/Localization/Sudoku/zh-Hans.json`:

```json
{
  "entries": [
    { "key": "common.back", "value": "返回" },
    { "key": "common.cancel", "value": "取消" },
    { "key": "menu.newGame", "value": "新游戏" },
    { "key": "menu.continueGame", "value": "继续" },
    { "key": "menu.highScores", "value": "排行榜" },
    { "key": "menu.exitGame", "value": "退出游戏" },
    { "key": "menu.tagline", "value": "无广告。永远。" },
    { "key": "menu.chooseDifficulty", "value": "选择难度" },
    { "key": "menu.custom", "value": "自定义" },
    { "key": "difficulty.easy", "value": "简单" },
    { "key": "difficulty.medium", "value": "中等" },
    { "key": "difficulty.hard", "value": "困难" },
    { "key": "difficulty.expert", "value": "专家" },
    { "key": "play.clear", "value": "清除" },
    { "key": "play.verify", "value": "验证" },
    { "key": "play.undo", "value": "撤销" },
    { "key": "play.hint", "value": "提示" },
    { "key": "play.autofill", "value": "自动填充" },
    { "key": "play.generate", "value": "生成" },
    { "key": "play.start", "value": "开始" },
    { "key": "play.clearGrid", "value": "清空棋盘" },
    { "key": "play.watchAdHint", "value": "观看广告 +1 提示" },
    { "key": "play.hintsLeft", "value": "剩余提示: {0}" },
    { "key": "play.solved", "value": "完成!" },
    { "key": "play.time", "value": "时间: {0}" },
    { "key": "play.timeWithBest", "value": "时间: {0}   最佳: {1}" },
    { "key": "editor.buildingHint", "value": "正在制作自定义拼图 — 选择一个数字,然后点击格子填入。" },
    { "key": "editor.errorConflict", "value": "棋盘上有冲突的数字。" },
    { "key": "editor.errorNoSolution", "value": "这个拼图不存在有效解。" },
    { "key": "editor.errorMultipleSolutions", "value": "这个拼图有多个解 — 请添加更多数字。" },
    { "key": "popup.generateBody", "value": "生成一个拼图作为起点 -\n在点击开始前你仍可以编辑它。" },
    { "key": "popup.successTime", "value": "时间: {0}" },
    { "key": "popup.successTimeAutofilled", "value": "时间: {0}(自动填充 - 不计入记录)" },
    { "key": "popup.newPuzzle", "value": "新拼图" },
    { "key": "popup.menu", "value": "菜单" },
    { "key": "settings.title", "value": "设置" },
    { "key": "settings.resetData", "value": "重置数据" },
    { "key": "settings.musicOn", "value": "音乐: 开" },
    { "key": "settings.musicOff", "value": "音乐: 关" },
    { "key": "settings.sfxOn", "value": "音效: 开" },
    { "key": "settings.sfxOff", "value": "音效: 关" },
    { "key": "settings.adsTestOn", "value": "广告(测试): 开" },
    { "key": "settings.adsTestOff", "value": "广告(测试): 关" },
    { "key": "settings.resetConfirmBody", "value": "要重置已保存的游戏和所有\n排行榜记录吗?此操作无法撤销。" },
    { "key": "settings.reset", "value": "重置" },
    { "key": "settings.language", "value": "语言" },
    { "key": "highscores.title", "value": "排行榜" },
    { "key": "highscores.noTimes", "value": "暂无记录的时间。" },
    { "key": "highscores.completed", "value": "已完成: {0}" },
    { "key": "highscores.clearLeaderboard", "value": "清空排行榜" }
  ]
}
```

- [ ] **Step 9: Italian** - create `Assets/Resources/Localization/Sudoku/it.json`:

```json
{
  "entries": [
    { "key": "common.back", "value": "Indietro" },
    { "key": "common.cancel", "value": "Annulla" },
    { "key": "menu.newGame", "value": "Nuova Partita" },
    { "key": "menu.continueGame", "value": "Continua" },
    { "key": "menu.highScores", "value": "Classifica" },
    { "key": "menu.exitGame", "value": "Esci dal Gioco" },
    { "key": "menu.tagline", "value": "Niente pubblicità. Mai." },
    { "key": "menu.chooseDifficulty", "value": "Scegli Difficoltà" },
    { "key": "menu.custom", "value": "Personalizzata" },
    { "key": "difficulty.easy", "value": "Facile" },
    { "key": "difficulty.medium", "value": "Medio" },
    { "key": "difficulty.hard", "value": "Difficile" },
    { "key": "difficulty.expert", "value": "Esperto" },
    { "key": "play.clear", "value": "Cancella" },
    { "key": "play.verify", "value": "Verifica" },
    { "key": "play.undo", "value": "Annulla Mossa" },
    { "key": "play.hint", "value": "Suggerimento" },
    { "key": "play.autofill", "value": "Compilazione Automatica" },
    { "key": "play.generate", "value": "Genera" },
    { "key": "play.start", "value": "Inizia" },
    { "key": "play.clearGrid", "value": "Cancella Griglia" },
    { "key": "play.watchAdHint", "value": "Guarda Pubblicità +1 Suggerimento" },
    { "key": "play.hintsLeft", "value": "Suggerimenti rimasti: {0}" },
    { "key": "play.solved", "value": "Risolto!" },
    { "key": "play.time", "value": "Tempo: {0}" },
    { "key": "play.timeWithBest", "value": "Tempo: {0}   Migliore: {1}" },
    { "key": "editor.buildingHint", "value": "Creazione puzzle personalizzato — scegli un numero, poi tocca le celle per riempirle." },
    { "key": "editor.errorConflict", "value": "La griglia ha numeri in conflitto." },
    { "key": "editor.errorNoSolution", "value": "Non esiste una soluzione valida per questo puzzle." },
    { "key": "editor.errorMultipleSolutions", "value": "Il puzzle ha più soluzioni — aggiungi altri numeri." },
    { "key": "popup.generateBody", "value": "Genera un puzzle da cui partire -\npuoi ancora modificarlo prima di premere Inizia." },
    { "key": "popup.successTime", "value": "Tempo: {0}" },
    { "key": "popup.successTimeAutofilled", "value": "Tempo: {0} (auto-completato - non registrato)" },
    { "key": "popup.newPuzzle", "value": "Nuovo Puzzle" },
    { "key": "popup.menu", "value": "Menu" },
    { "key": "settings.title", "value": "Impostazioni" },
    { "key": "settings.resetData", "value": "Ripristina Dati" },
    { "key": "settings.musicOn", "value": "Musica: Attiva" },
    { "key": "settings.musicOff", "value": "Musica: Disattivata" },
    { "key": "settings.sfxOn", "value": "Effetti Sonori: Attivi" },
    { "key": "settings.sfxOff", "value": "Effetti Sonori: Disattivati" },
    { "key": "settings.adsTestOn", "value": "Pubblicità (test): Attiva" },
    { "key": "settings.adsTestOff", "value": "Pubblicità (test): Disattivata" },
    { "key": "settings.resetConfirmBody", "value": "Ripristinare la partita salvata e tutta\nla classifica? Non si può annullare." },
    { "key": "settings.reset", "value": "Ripristina" },
    { "key": "settings.language", "value": "Lingua" },
    { "key": "highscores.title", "value": "Classifica" },
    { "key": "highscores.noTimes", "value": "Nessun tempo registrato ancora." },
    { "key": "highscores.completed", "value": "Completati: {0}" },
    { "key": "highscores.clearLeaderboard", "value": "Cancella Classifica" }
  ]
}
```

- [ ] **Step 10: Romanian** - create `Assets/Resources/Localization/Sudoku/ro.json`:

```json
{
  "entries": [
    { "key": "common.back", "value": "Înapoi" },
    { "key": "common.cancel", "value": "Anulează" },
    { "key": "menu.newGame", "value": "Joc Nou" },
    { "key": "menu.continueGame", "value": "Continuă" },
    { "key": "menu.highScores", "value": "Recorduri" },
    { "key": "menu.exitGame", "value": "Ieși din Joc" },
    { "key": "menu.tagline", "value": "Fără reclame. Niciodată." },
    { "key": "menu.chooseDifficulty", "value": "Alege Dificultatea" },
    { "key": "menu.custom", "value": "Personalizat" },
    { "key": "difficulty.easy", "value": "Ușor" },
    { "key": "difficulty.medium", "value": "Mediu" },
    { "key": "difficulty.hard", "value": "Dificil" },
    { "key": "difficulty.expert", "value": "Expert" },
    { "key": "play.clear", "value": "Șterge" },
    { "key": "play.verify", "value": "Verifică" },
    { "key": "play.undo", "value": "Revocă" },
    { "key": "play.hint", "value": "Indiciu" },
    { "key": "play.autofill", "value": "Completare Automată" },
    { "key": "play.generate", "value": "Generează" },
    { "key": "play.start", "value": "Start" },
    { "key": "play.clearGrid", "value": "Șterge Grila" },
    { "key": "play.watchAdHint", "value": "Urmărește Reclamă +1 Indiciu" },
    { "key": "play.hintsLeft", "value": "Indicii rămase: {0}" },
    { "key": "play.solved", "value": "Rezolvat!" },
    { "key": "play.time", "value": "Timp: {0}" },
    { "key": "play.timeWithBest", "value": "Timp: {0}   Cel mai bun: {1}" },
    { "key": "editor.buildingHint", "value": "Se construiește puzzle-ul personalizat — alege un număr, apoi atinge celulele pentru a le completa." },
    { "key": "editor.errorConflict", "value": "Grila are numere aflate în conflict." },
    { "key": "editor.errorNoSolution", "value": "Nu există o soluție validă pentru acest puzzle." },
    { "key": "editor.errorMultipleSolutions", "value": "Puzzle-ul are mai multe soluții — adaugă mai multe numere." },
    { "key": "popup.generateBody", "value": "Generează un puzzle de la care să pornești -\nîl poți edita în continuare înainte de a apăsa Start." },
    { "key": "popup.successTime", "value": "Timp: {0}" },
    { "key": "popup.successTimeAutofilled", "value": "Timp: {0} (completat automat - nu a fost înregistrat)" },
    { "key": "popup.newPuzzle", "value": "Puzzle Nou" },
    { "key": "popup.menu", "value": "Meniu" },
    { "key": "settings.title", "value": "Setări" },
    { "key": "settings.resetData", "value": "Resetează Datele" },
    { "key": "settings.musicOn", "value": "Muzică: Activată" },
    { "key": "settings.musicOff", "value": "Muzică: Dezactivată" },
    { "key": "settings.sfxOn", "value": "Efecte Sonore: Activate" },
    { "key": "settings.sfxOff", "value": "Efecte Sonore: Dezactivate" },
    { "key": "settings.adsTestOn", "value": "Reclame (test): Activate" },
    { "key": "settings.adsTestOff", "value": "Reclame (test): Dezactivate" },
    { "key": "settings.resetConfirmBody", "value": "Resetezi jocul salvat și toate\nrecordurile? Nu poate fi anulat." },
    { "key": "settings.reset", "value": "Resetează" },
    { "key": "settings.language", "value": "Limbă" },
    { "key": "highscores.title", "value": "Recorduri" },
    { "key": "highscores.noTimes", "value": "Niciun timp înregistrat încă." },
    { "key": "highscores.completed", "value": "Finalizate: {0}" },
    { "key": "highscores.clearLeaderboard", "value": "Șterge Clasamentul" }
  ]
}
```

- [ ] **Step 11: Commit**

```bash
git add Assets/Resources/Localization/Sudoku/es.json Assets/Resources/Localization/Sudoku/pt.json Assets/Resources/Localization/Sudoku/de.json Assets/Resources/Localization/Sudoku/fr.json Assets/Resources/Localization/Sudoku/ja.json Assets/Resources/Localization/Sudoku/ko.json Assets/Resources/Localization/Sudoku/ru.json Assets/Resources/Localization/Sudoku/zh-Hans.json Assets/Resources/Localization/Sudoku/it.json Assets/Resources/Localization/Sudoku/ro.json
git commit -m "feat: author Spanish, Portuguese, German, French, Japanese, Korean, Russian, Chinese (Simplified), Italian, and Romanian localization content for Sudoku"
```

---

## Task 9: `UiFactory.CreateBackButton` label parameter

Every screen's Back button currently hardcodes the English word "Back" inside the shared framework helper. Give it an optional label parameter (default `"Back"`, so `Game01_2048` - which doesn't use `CreateBackButton` at all today, confirmed by search - and any other non-localized caller keep working unchanged) so Sudoku can pass its own translated label.

**Files:**
- Modify: `Assets/Framework/UI/UiFactory.cs:121-127`
- Modify: `Assets/Games/Game02_Sudoku/Scripts/SudokuUi.cs:24-29`

**Interfaces:**
- Produces: `UiFactory.CreateBackButton(Transform parent, UnityAction onClick, string label = "Back")`, `SudokuUi.CreateBackButton(Transform parent, UnityAction onClick, string label)`

No new automated test - this is a signature change to existing, already-untested UI-building helpers (consistent with the rest of `UiFactory`/`SudokuUi`, which have no unit tests today since they require a live Unity instance). Verified via Task 14's compile-check and Task 15's on-device pass.

- [ ] **Step 1: Add the label parameter to `UiFactory.CreateBackButton`**

In `Assets/Framework/UI/UiFactory.cs`, replace lines 119-127:

```csharp
        // Standard top-left back button used by every screen with a "back" action -
        // keeps placement consistent across games instead of each screen picking its own.
        // label defaults to "Back" for callers that don't localize (e.g. Game01_2048);
        // Sudoku passes its own translated label via SudokuUi.CreateBackButton.
        public static Button CreateBackButton(Transform parent, UnityAction onClick, string label = "Back")
        {
            // Anchored to the canvas's actual top-left corner (not a fixed offset from
            // center) so it sits flush in the corner on every device, regardless of how
            // tall the canvas ends up in canvas-units for that screen's aspect ratio.
            return CreateButton(parent, label, new Vector2(20, -20), new Vector2(110, 50), true, onClick, new Vector2(0f, 1f));
        }
```

- [ ] **Step 2: Thread the label through `SudokuUi.CreateBackButton`**

In `Assets/Games/Game02_Sudoku/Scripts/SudokuUi.cs`, replace lines 24-29:

```csharp
        public static Button CreateBackButton(Transform parent, UnityAction onClick, string label)
        {
            var button = UiFactory.CreateBackButton(parent, onClick, label);
            Retint(button);
            return button;
        }
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Framework/UI/UiFactory.cs Assets/Games/Game02_Sudoku/Scripts/SudokuUi.cs
git commit -m "feat: allow CreateBackButton to take a translated label"
```

Note: the three call sites (`SudokuController.cs:518`, `SudokuHighScoresController.cs:86`, `SudokuSettingsController.cs:69`) now fail to compile since `SudokuUi.CreateBackButton` no longer has a default for `label`. They're fixed in Tasks 11-13. This is expected within this plan's task boundaries.

---

## Task 10: Rewire `SudokuMenuController`

**Files:**
- Modify: `Assets/Games/Game02_Sudoku/Scripts/SudokuMenuController.cs`

**Interfaces:**
- Consumes: `Loc.Get(string)`, `Loc.Difficulty(Difficulty)` (Task 5)

- [ ] **Step 1: Replace every hardcoded string with `Loc.Get(...)`**

In `Assets/Games/Game02_Sudoku/Scripts/SudokuMenuController.cs`, apply these replacements:

Line 34 (`"New Game"` button):
```csharp
            SudokuUi.CreateButton(canvas.transform, Loc.Get("menu.newGame"), new Vector2(0, 100), new Vector2(320, 68), true, () =>
```

Line 39 (`"Continue"` button):
```csharp
            SudokuUi.CreateButton(canvas.transform, Loc.Get("menu.continueGame"), new Vector2(0, 15), new Vector2(320, 68), hasSave, () =>
```

Line 46 (`"High Scores"` button):
```csharp
            SudokuUi.CreateButton(canvas.transform, Loc.Get("menu.highScores"), new Vector2(0, -70), new Vector2(320, 68), true, () =>
```

Line 51 (`"Exit Game"` button):
```csharp
            SudokuUi.CreateButton(canvas.transform, Loc.Get("menu.exitGame"), new Vector2(0, -155), new Vector2(320, 68), true, () =>
```

Line 80 (tagline text - "NoAdsGuy's Sudoku" brand title on line 75 stays hardcoded, it's a proper noun/brand name, not translated content):
```csharp
            tagline.text = Loc.Get("menu.tagline");
```

Line 119 (`"Choose Difficulty"` popup label):
```csharp
            label.text = Loc.Get("menu.chooseDifficulty");
```

Line 129 (difficulty buttons - use `Loc.Difficulty` instead of `difficulty.ToString()`):
```csharp
                SudokuUi.CreateButton(panel.transform, Loc.Difficulty(difficulty), new Vector2(x, y), new Vector2(150, 50), true, () =>
```

Line 139 (`"Custom"` button):
```csharp
            SudokuUi.CreateButton(panel.transform, Loc.Get("menu.custom"), new Vector2(0, -45), new Vector2(150, 50), true, () =>
```

Line 146 (`"Cancel"` button):
```csharp
            SudokuUi.CreateButton(panel.transform, Loc.Get("common.cancel"), new Vector2(0, -110), new Vector2(150, 40), true, () =>
```

There is no `CreateBackButton` call in this file (the main menu is the root screen, no Back button) - no change needed for Task 9's signature update here.

- [ ] **Step 2: Commit**

```bash
git add Assets/Games/Game02_Sudoku/Scripts/SudokuMenuController.cs
git commit -m "feat: localize SudokuMenuController strings"
```

---

## Task 11: Rewire `SudokuController` (Play + Editor screens, popups, custom-puzzle errors)

The largest of the rewiring tasks: the main Play/Editor screen, both popups, and the `SudokuCustomPuzzleError` display mapping from Task 6.

**Files:**
- Modify: `Assets/Games/Game02_Sudoku/Scripts/SudokuController.cs`

**Interfaces:**
- Consumes: `Loc.Get(string)`, `Loc.Get(string, object[])`, `Loc.Difficulty(Difficulty)` (Task 5), `SudokuCustomPuzzleError` (Task 6)

- [ ] **Step 1: Change `_editError`'s type and update every place it's set or read**

Field declaration (line 43) - change from `string` to the enum:
```csharp
        private SudokuCustomPuzzleError? _editError;
```

`ApplyActiveToolToEditorCell` (lines 174, 181) - both `_editError = null;` stay as `_editError = null;` (still valid: `SudokuCustomPuzzleError?` is nullable, `null` still means "no error").

`ClearEditor` (line 296) - `_editError = null;` stays as-is.

`StartCustomGame` (lines 304-309) - `TryBuild`'s `out error` now yields a `SudokuCustomPuzzleError`, and assigning it to `_editError` (a `SudokuCustomPuzzleError?`) needs no cast (implicit conversion from `T` to `T?`):
```csharp
            if (!SudokuCustomPuzzle.TryBuild(_editBoard, out var puzzle, out var error))
            {
                _editError = error;
                Refresh();
                return;
            }
```
(unchanged from current code - this already compiles correctly against the new signature since `error` is now `SudokuCustomPuzzleError` and `_editError` is `SudokuCustomPuzzleError?`)

`StartCustomGame` success path (line 318) - `_editError = null;` stays as-is.

`RefreshEditor` (line 456) - replace the string-literal fallback with a mapping through `Loc`:
```csharp
            _statusText.text = _editError.HasValue ? EditorErrorMessage(_editError.Value) : Loc.Get("editor.buildingHint");
```

Add a new private method right after `RefreshEditor` (after line 457's closing brace):
```csharp
        private static string EditorErrorMessage(SudokuCustomPuzzleError error)
        {
            switch (error)
            {
                case SudokuCustomPuzzleError.ConflictingNumbers: return Loc.Get("editor.errorConflict");
                case SudokuCustomPuzzleError.NoSolution: return Loc.Get("editor.errorNoSolution");
                case SudokuCustomPuzzleError.MultipleSolutions: return Loc.Get("editor.errorMultipleSolutions");
                default: return "";
            }
        }
```

- [ ] **Step 2: Replace remaining hardcoded strings in `BuildUi`, popups, and status text**

Line 395 (`_statusText.text` in `RefreshPlay`):
```csharp
            _statusText.text = _game.IsComplete ? Loc.Get("play.solved") : Loc.Get("play.hintsLeft", _game.HintsRemaining);
```

Line 394 (`_difficultyText.text` in `RefreshPlay`):
```csharp
            _difficultyText.text = Loc.Difficulty(_difficulty);
```

`UpdateTimeText` (lines 328-332) - both the "Time: {0}" and "Time: {0}   Best: {1}" branches:
```csharp
        private void UpdateTimeText()
        {
            var times = _leaderboardStore.GetTimes(_difficulty);
            _timeText.text = times.Count > 0
                ? Loc.Get("play.timeWithBest", FormatTime(_elapsedSeconds), FormatTime(times[0]))
                : Loc.Get("play.time", FormatTime(_elapsedSeconds));
        }
```

`ShowSuccessPopup` (lines 398-405) - both success-time branches:
```csharp
        private void ShowSuccessPopup()
        {
            _successTimeText.text = _game.HasUsedAutofill
                ? Loc.Get("popup.successTimeAutofilled", FormatTime(_elapsedSeconds))
                : Loc.Get("popup.successTime", FormatTime(_elapsedSeconds));
            _successPopup.SetActive(true);
            if (_audioSettings.SfxEnabled) SudokuAudio.PlaySuccess(this, _audioSource);
        }
```

Line 518 (`CreateBackButton` call - add the now-required label argument from Task 9):
```csharp
            SudokuUi.CreateBackButton(canvas.transform, ReturnToMenu, Loc.Get("common.back"));
```

Line 530 (`"Clear"` button):
```csharp
            _clearEntriesButton = SudokuUi.CreateButton(canvas.transform, Loc.Get("play.clear"), new Vector2(-110, 340), new Vector2(190, 44), true, ClearEntriesAction);
```

Line 531 (`"Verify"` button):
```csharp
            _verifyButton = SudokuUi.CreateButton(canvas.transform, Loc.Get("play.verify"), new Vector2(110, 340), new Vector2(190, 44), true, Verify);
```

Lines 605-607 (`"Undo"`, `"Hint"`, `"Autofill"` buttons):
```csharp
            _undoButton = SudokuUi.CreateButton(canvas.transform, Loc.Get("play.undo"), new Vector2(-180, 282), new Vector2(150, 44), false, UndoMove, bottomAnchor);
            _hintButton = SudokuUi.CreateButton(canvas.transform, Loc.Get("play.hint"), new Vector2(0, 282), new Vector2(150, 44), true, UseHint, bottomAnchor);
            _autofillButton = SudokuUi.CreateButton(canvas.transform, Loc.Get("play.autofill"), new Vector2(180, 282), new Vector2(150, 44), true, Autofill, bottomAnchor);
```

Lines 609-615 (`"Generate"`, `"Start"`, `"Clear Grid"`, `"Watch Ad +1 Hint"` buttons):
```csharp
            _generateButton = SudokuUi.CreateButton(canvas.transform, Loc.Get("play.generate"), new Vector2(-240, 220), new Vector2(220, 44), false, () =>
            {
                _generateDifficultyPopup.SetActive(true);
            }, bottomAnchor);
            _startButton = SudokuUi.CreateButton(canvas.transform, Loc.Get("play.start"), new Vector2(0, 220), new Vector2(220, 44), false, StartCustomGame, bottomAnchor);
            _clearEditorButton = SudokuUi.CreateButton(canvas.transform, Loc.Get("play.clearGrid"), new Vector2(240, 220), new Vector2(220, 44), false, ClearEditor, bottomAnchor);
            _watchAdButton = SudokuUi.CreateButton(canvas.transform, Loc.Get("play.watchAdHint"), new Vector2(0, 220), new Vector2(220, 44), false, WatchAdForHint, bottomAnchor);
```

Line 646 (Generate-difficulty popup body):
```csharp
            label.text = Loc.Get("popup.generateBody");
```

Lines 649-652 (Easy/Medium/Hard/Expert buttons in the Generate-difficulty popup):
```csharp
            SudokuUi.CreateButton(panel.transform, Loc.Difficulty(Difficulty.Easy), new Vector2(0, 70), new Vector2(220, 46), true, () => GenerateForEditor(Difficulty.Easy));
            SudokuUi.CreateButton(panel.transform, Loc.Difficulty(Difficulty.Medium), new Vector2(0, 15), new Vector2(220, 46), true, () => GenerateForEditor(Difficulty.Medium));
            SudokuUi.CreateButton(panel.transform, Loc.Difficulty(Difficulty.Hard), new Vector2(0, -40), new Vector2(220, 46), true, () => GenerateForEditor(Difficulty.Hard));
            SudokuUi.CreateButton(panel.transform, Loc.Difficulty(Difficulty.Expert), new Vector2(0, -95), new Vector2(220, 46), true, () => GenerateForEditor(Difficulty.Expert));
```

Line 654 (Cancel button in that same popup):
```csharp
            SudokuUi.CreateButton(panel.transform, Loc.Get("common.cancel"), new Vector2(0, -165), new Vector2(220, 40), true, () =>
```

Line 728 (Success popup "Solved!" label):
```csharp
            label.text = Loc.Get("play.solved");
```

Lines 734-735 (New Puzzle / Menu buttons in the success popup):
```csharp
            SudokuUi.CreateButton(panel.transform, Loc.Get("popup.newPuzzle"), new Vector2(0, -20), new Vector2(260, 50), true, PlayAgain);
            SudokuUi.CreateButton(panel.transform, Loc.Get("popup.menu"), new Vector2(0, -90), new Vector2(260, 50), true, ReturnToMenu);
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Games/Game02_Sudoku/Scripts/SudokuController.cs
git commit -m "feat: localize SudokuController Play/Editor screens and custom-puzzle error messages"
```

---

## Task 12: Rewire `SudokuSettingsController` and add the language picker

**Files:**
- Modify: `Assets/Games/Game02_Sudoku/Scripts/SudokuSettingsController.cs`

**Interfaces:**
- Consumes: `Loc.Get(string)` (Task 5), `Loc.SetLanguage(Language)` (Task 5), `Loc.CurrentLanguage` (Task 5), `LanguageInfo.NativeName(Language)` (Task 1)

- [ ] **Step 1: Localize the existing toggle labels**

Replace the label-producing methods (lines 37, 53, 54):

```csharp
        private string AdsToggleLabel() => _adsTestSettings.AdsDisabledForTesting ? Loc.Get("settings.adsTestOff") : Loc.Get("settings.adsTestOn");

        private string MusicToggleLabel() => _audioSettings.MusicEnabled ? Loc.Get("settings.musicOn") : Loc.Get("settings.musicOff");
        private string SfxToggleLabel() => _audioSettings.SfxEnabled ? Loc.Get("settings.sfxOn") : Loc.Get("settings.sfxOff");
```

- [ ] **Step 2: Add a `CycleLanguage` method and localize the rest of `BuildUi`**

Add a new field near the top of the class (alongside the other button fields):
```csharp
        private Button _languageButton;
```

Add a `CycleLanguage` method (place it near `ToggleSfx`):
```csharp
        // Cycles through the 11 languages in enum declaration order and reloads this
        // scene so every string on screen - here and on every other screen - rebuilds
        // in the new language, the same way the Back button already reloads scenes.
        private void CycleLanguage()
        {
            var languages = (Language[])System.Enum.GetValues(typeof(Language));
            var currentIndex = System.Array.IndexOf(languages, Loc.CurrentLanguage);
            var next = languages[(currentIndex + 1) % languages.Length];
            Loc.SetLanguage(next);
            SceneManager.LoadScene("SudokuSettings");
        }

        private string LanguageButtonLabel() => $"{Loc.Get("settings.language")}: {LanguageInfo.NativeName(Loc.CurrentLanguage)}";
```

Add the `using MobileGamesFramework.Localization;` directive at the top of the file (alongside the existing `using` lines).

Replace `BuildUi` (lines 64-92) in full:

```csharp
        private void BuildUi()
        {
            var canvas = UiFactory.CreateCanvas();
            UiFactory.CreateBackground(canvas.transform, new Color(0.75f, 0.85f, 0.97f), new Color(0.98f, 0.98f, 1f));

            SudokuUi.CreateBackButton(canvas.transform, () =>
            {
                SceneManager.LoadScene("SudokuMenu");
            }, Loc.Get("common.back"));

            var title = UiFactory.CreateText(canvas.transform, "Title", 40, TextAnchor.MiddleCenter);
            title.text = Loc.Get("settings.title");
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 190), new Vector2(400, 60));

            SudokuUi.CreateButton(canvas.transform, Loc.Get("settings.resetData"), new Vector2(0, 120), new Vector2(260, 50), true, () =>
            {
                _resetConfirmPopup.SetActive(true);
            });

            _musicToggleButton = SudokuUi.CreateButton(canvas.transform, MusicToggleLabel(), new Vector2(0, 55), new Vector2(260, 50), true, ToggleMusic);
            _sfxToggleButton = SudokuUi.CreateButton(canvas.transform, SfxToggleLabel(), new Vector2(0, -10), new Vector2(260, 50), true, ToggleSfx);
            _languageButton = SudokuUi.CreateButton(canvas.transform, LanguageButtonLabel(), new Vector2(0, -75), new Vector2(260, 50), true, CycleLanguage);

            if (Application.isEditor || Debug.isDebugBuild)
            {
                _adsTestToggleButton = SudokuUi.CreateButton(canvas.transform, AdsToggleLabel(), new Vector2(0, -140), new Vector2(260, 50), true, ToggleAdsForTesting);
            }

            BuildResetConfirmPopup(canvas.transform);
        }
```

(Note: the language button takes the vertical slot the Ads-testing toggle used to occupy at `-75`, and the Ads-testing toggle - debug/editor builds only - moves down to `-140` to make room. This keeps every row on the same 65-unit vertical rhythm the rest of the screen already uses.)

- [ ] **Step 3: Localize the reset-confirmation popup**

Replace `BuildResetConfirmPopup` (lines 94-125):

```csharp
        private void BuildResetConfirmPopup(Transform parent)
        {
            _resetConfirmPopup = new GameObject("ResetConfirmPopup", typeof(Image));
            _resetConfirmPopup.transform.SetParent(parent, false);
            UiFactory.SetRect(_resetConfirmPopup.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _resetConfirmPopup.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            var panel = new GameObject("Panel", typeof(Image));
            panel.transform.SetParent(_resetConfirmPopup.transform, false);
            UiFactory.SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 260));
            var panelImage = panel.GetComponent<Image>();
            panelImage.sprite = RoundedRectSprite.Get();
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.96f, 0.94f, 0.90f);

            var label = UiFactory.CreateText(panel.transform, "Label", 20, TextAnchor.MiddleCenter);
            label.text = Loc.Get("settings.resetConfirmBody");
            UiFactory.SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(320, 90));

            SudokuUi.CreateButton(panel.transform, Loc.Get("settings.reset"), new Vector2(0, -30), new Vector2(220, 50), true, () =>
            {
                ResetAllData();
                _resetConfirmPopup.SetActive(false);
            });

            SudokuUi.CreateButton(panel.transform, Loc.Get("common.cancel"), new Vector2(0, -95), new Vector2(220, 44), true, () =>
            {
                _resetConfirmPopup.SetActive(false);
            });

            _resetConfirmPopup.SetActive(false);
        }
```

- [ ] **Step 4: Commit**

```bash
git add Assets/Games/Game02_Sudoku/Scripts/SudokuSettingsController.cs
git commit -m "feat: localize SudokuSettingsController and add language picker"
```

---

## Task 13: Rewire `SudokuHighScoresController`

**Files:**
- Modify: `Assets/Games/Game02_Sudoku/Scripts/SudokuHighScoresController.cs`

**Interfaces:**
- Consumes: `Loc.Get(string)`, `Loc.Get(string, object[])`, `Loc.Difficulty(Difficulty)` (Task 5)

- [ ] **Step 1: Replace hardcoded strings**

Line 56 (`"No times recorded yet."`):
```csharp
                _listText.text = Loc.Get("highscores.noTimes");
```

Line 66 (`"Completed: {0}"`):
```csharp
            _completedText.text = Loc.Get("highscores.completed", _leaderboardStore.GetCompletedCount(_selectedDifficulty));
```

Line 86 (`CreateBackButton` call - add the required label argument from Task 9):
```csharp
            SudokuUi.CreateBackButton(canvas.transform, () =>
            {
                SceneManager.LoadScene("SudokuMenu");
            }, Loc.Get("common.back"));
```

Line 92 (`"High Scores"` title):
```csharp
            title.text = Loc.Get("highscores.title");
```

Line 99 (difficulty tab buttons - use `Loc.Difficulty` instead of `difficulty.ToString()`):
```csharp
                _difficultyButtons[i] = SudokuUi.CreateButton(canvas.transform, Loc.Difficulty(difficulty), new Vector2(x, 330), new Vector2(100, 46), true, () => SelectDifficulty(difficulty));
```

Line 123 (`"Clear Leaderboard"` button):
```csharp
            SudokuUi.CreateButton(canvas.transform, Loc.Get("highscores.clearLeaderboard"), new Vector2(0, -480), new Vector2(280, 46), true, ClearLeaderboard);
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Games/Game02_Sudoku/Scripts/SudokuHighScoresController.cs
git commit -m "feat: localize SudokuHighScoresController"
```

---

## Task 14: Full compile-check build

Everything up to this point has been written without any automated test execution (blocked by the batchmode `-runTests` hang - see Global Constraints). This task is the first point every new file, every asmdef reference, and every rewired controller actually compiles together.

**Files:** none (build/verification only)

- [ ] **Step 1: Run a debug build**

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Unity.exe" -batchmode -quit -projectPath "C:/Users/schei/mobile-games-framework" -executeMethod AndroidApkBuilder.BuildSudoku -logFile "C:/Users/schei/mobile-games-framework/build_localization_compilecheck.log"
```

Run via Bash `run_in_background: true` (several minutes). Do not start any other batchmode Unity invocation while this runs.

- [ ] **Step 2: Confirm success**

Check for `BUILD_TOTAL_ERRORS: 0` in the log. If there are compile errors, they'll typically be:
- A missed `Loc.Get(...)` rewiring site with a leftover string-vs-`Text` type mismatch
- A missing `using MobileGamesFramework.Localization;` in a file that now references `Language`
- A missing asmdef reference (see Task 1 Step 2, Task 4 Step 1)
- A leftover reference to the old `SudokuCustomPuzzle.TryBuild`'s `out string error` signature

Fix any found and re-run the build until `BUILD_TOTAL_ERRORS: 0`.

- [ ] **Step 3: Revert the debug-build identifier mutation**

Per Global Constraints, `AndroidApkBuilder.BuildSudoku` overwrites the release application identifier with the debug one in two files. Check and revert both:

```bash
cd /c/Users/schei/mobile-games-framework
grep -n "Android: com" ProjectSettings/ProjectSettings.asset
grep -n "bundleId" ProjectSettings/AndroidResolverDependencies.xml
```

If either shows `com.mobilegamesframework.game02_sudoku`, use `Edit` to change it back to `com.noadsguy.sudoku` in both files (never `git checkout`).

- [ ] **Step 4: Remove the stray build log and commit**

```bash
cd /c/Users/schei/mobile-games-framework
rm -f build_localization_compilecheck.log
git status --short
```

If `ProjectSettings/ProjectSettings.asset` or `ProjectSettings/AndroidResolverDependencies.xml` show as modified after the Step 3 revert (they shouldn't, if the revert matched the committed value exactly), investigate before committing anything.

No commit needed for this task if the working tree is clean after Step 3's revert (the build itself produces no source changes) - this task exists purely to catch compile errors before Task 15's on-device pass.

---

## Task 15: On-device verification and final commit

**Files:** none (verification only)

- [ ] **Step 1: Install and launch on the connected device**

```bash
ADB="/c/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe"
PKG=com.mobilegamesframework.game02_sudoku
"$ADB" install -r "C:/Users/schei/mobile-games-framework/Builds/Android/mobile-games-framework-sudoku.apk"
"$ADB" shell input keyevent KEYCODE_WAKEUP
"$ADB" shell am force-stop $PKG
"$ADB" shell monkey -p $PKG -c android.intent.category.LAUNCHER 1
```

(This installs whatever debug APK Task 14 already built - no need to rebuild unless Task 14's fixes weren't yet built into an APK.)

- [ ] **Step 2: Verify English (system default on a fresh install, or via a cleared app first)**

Screenshot the main menu, the difficulty popup, a Play screen (any difficulty), the Settings screen, and the High Scores screen. Confirm every button/label reads as expected English text (matching Task 7's `en.json`), no missing-key raw strings (e.g. literally seeing `"play.hint"` on screen instead of "Hint" would mean a key mismatch between `SudokuController.cs` and `en.json` - go find and fix the mismatched key).

- [ ] **Step 3: Verify Romanian via the language picker**

From the Settings screen, tap the language button repeatedly (or note it's alphabetical/enum-order - English, Spanish, Portuguese, German, French, Japanese, Korean, Russian, Chinese (Simplified), Italian, Romanian - so from a fresh English state it takes 10 taps to reach Romanian) until it reads "Română". Screenshot the Settings screen, then navigate to the main menu, difficulty popup, a Play screen, and High Scores, confirming every string switched to Romanian and matches Task 8's `ro.json`.

- [ ] **Step 4: Verify German (flagged in the spec as a longer-string risk) for overflow**

Cycle the language picker to German. Screenshot every screen again, specifically checking for text clipping or wrapping in fixed-width buttons - German translations (e.g. "Automatisch ausfüllen" for Autofill, "Werbung ansehen +1 Tipp" for Watch Ad +1 Hint) are meaningfully longer than their English source and are exactly the kind of string the spec flagged as a follow-up risk, not a blocker.

- [ ] **Step 5: Document any overflow found**

If any German (or other) string visibly clips or overflows its button, do not attempt to fix font sizes/wrapping as part of this plan (out of scope per the spec's Known Risks section) - note it in the final commit message or report it back, since it's expected follow-up polish.

- [ ] **Step 6: Revert the debug-build identifier mutation again (Task 14's build may have been superseded by re-runs during fixes)**

Repeat Task 14 Step 3's check-and-revert.

- [ ] **Step 7: Final commit**

```bash
cd /c/Users/schei/mobile-games-framework
git status --short
```

Confirm the working tree is clean (everything should already be committed task-by-task). If anything is still uncommitted (e.g. a fix made during Task 14's compile-check loop), commit it now with a message describing what was fixed.

---

## Self-Review Notes

**Spec coverage:** every Goals-section item has a task - system-language detection/fallback (Tasks 3, 5), persisted override via Settings (Tasks 4, 5, 12), every screen's strings (Tasks 10-13), the 11-language content (Tasks 7-8), error-string localization (Task 6, 11), testing per the spec's Testing section (unit tests in Tasks 1-4 and 6; on-device manual check across English/Romanian/a long-string language in Task 15, matching the spec's explicit German-or-Russian call-out).

**Deviation from spec noted and justified:** the spec described the framework test coverage as "its own test assembly (mirroring `MobileGamesFramework.Tests`)" - this plan instead adds the new module as a reference to the *existing* `MobileGamesFramework.Tests` assembly (Task 1, Step 2), consistent with how every other Framework module's tests already live in one shared `Assets/Framework/Tests/` folder/assembly rather than one per module. This is a simplification within the spec's intent (test coverage exists), not a scope change.

**Known constraint discovered during planning (not in the original spec):** `Unity.exe -batchmode -runTests` hangs indefinitely on this project, confirmed by a bounded probe this session. This is called out in Global Constraints and threaded through every task with automated tests (1-4, 6) - tests are still written for their documentation/regression value and are verified by inspection plus the Task 14 compile-check, not by execution.
