# Sudoku localization design

Date: 2026-09-15
Status: approved, pending implementation plan

## Overview

Add multi-language support to "NoAdsGuy's Sudoku": 11 languages total, defaulting
to the device's system language on first launch, changeable afterward from the
Settings screen. The localization engine (string-table lookup, system-language
detection, persistence) is built as a reusable framework module so future games
in this repo can adopt it without re-implementing the mechanism; only the actual
translated string content is Sudoku-specific.

## Goals

- Every user-visible string in the Sudoku game (main menu, Play screen header
  and status text, number pad / tool buttons, Settings screen, all popups,
  custom-puzzle-editor error text) is looked up through a string table instead
  of hardcoded as a literal.
- First launch with no stored preference: detect `Application.systemLanguage`,
  map it to the closest supported language, default to that. Unsupported system
  languages fall back to English.
- Settings screen gets a language picker. Changing it persists the choice and
  refreshes the current screen's text immediately.
- Once a language is explicitly chosen (whether auto-detected or picked by the
  user), it persists across launches regardless of later system-language changes.

## Non-goals

- No in-app translation editor or hot-reloading of string content.
- No per-string pluralization rules (English-style "singular vs plural" is out
  of scope; templated counts just interpolate the number, e.g. "Hints left: 3").
- No RTL layout support — none of the 11 target languages are RTL scripts.
- No professional/human translation review pass in this iteration. Content is
  AI-generated; the user may correct individual strings later.
- No localization for other games in the repo (2048, etc.) — only the engine is
  built generically; Sudoku is the only game wired up to use it right now.

## Language list

11 languages: English, Spanish, Portuguese, German, French, Japanese, Korean,
Russian, Chinese (Simplified), Italian, Romanian.

## Architecture

A new Unity-aware framework module, mirroring the existing split between the
engine-agnostic `MobileGamesFramework` assembly and the Unity-referencing
`MobileGamesFramework.UI` assembly:

```
Assets/Framework/Localization/
  Language.cs              - enum of the 11 supported languages
  LocalizationTable.cs      - key -> string lookup for one loaded language
  LocalizationLoader.cs     - Resources.Load + JSON parse + SystemLanguage mapping
  MobileGamesFramework.Localization.asmdef  (references UnityEngine, no UI dep)
```

This loader logic (JSON parsing, `Application.systemLanguage` → `Language`
mapping, English fallback for unmapped values) is identical for every game that
adopts it, which is why it belongs in a shared framework assembly rather than
being duplicated per game the way the thin `PlayerPrefsStore` adapter is —
that precedent is for boilerplate Unity adapters behind a framework interface,
not for meaningful shared logic.

Per-game pieces (Sudoku):

```
Assets/Games/Game02_Sudoku/
  Resources/Localization/en.json, es.json, pt.json, de.json, fr.json,
                          ja.json, ko.json, ru.json, zh-Hans.json, it.json, ro.json
  Scripts/SudokuLocalizationSettings.cs  - persists chosen Language via PlayerPrefsStore
  Scripts/Loc.cs                         - static Get(key) / Get(key, args) facade
```

`Loc` is a thin static wrapper: on first access it asks
`SudokuLocalizationSettings` for the persisted (or freshly-detected) language,
asks `LocalizationLoader` to load that language's JSON into a
`LocalizationTable`, and caches it. `SudokuController`, `SudokuMenuController`,
and `SudokuSettingsController` call `Loc.Get("play.hintsLeft", hintsRemaining)`
etc. instead of string literals.

## Data flow

**First launch (no stored preference):**
`SudokuLocalizationSettings` has no saved value → `LocalizationLoader` reads
`Application.systemLanguage`, maps it to the closest of the 11 `Language`
values (exact match, e.g. `SystemLanguage.Romanian` → `Language.Romanian`; no
partial/fuzzy matching), falls back to `Language.English` if unmapped → that
choice is persisted immediately (so it behaves identically to an explicit user
pick from here on).

**Every subsequent launch:** the persisted value is used as-is; system language
is not re-checked.

**Changing language in Settings:** the picker calls
`SudokuLocalizationSettings.SetLanguage(...)`, which persists it and clears
`Loc`'s cached table. The Settings screen then reloads the current scene
(`SceneManager.LoadScene` on the active scene name) — the same mechanism the
Back button already uses elsewhere — so every screen rebuilds its UI text from
scratch in the new language. No separate "refresh all live text" machinery is
needed since every screen already fully rebuilds via `BuildUi()`/`Refresh()`.

## Content format

One JSON file per language under `Resources/Localization/`, flat key → string
maps:

```json
{
  "menu.newGame": "New Game",
  "menu.continue": "Continue",
  "play.hintsLeft": "Hints left: {0}",
  "play.difficultyLabel.medium": "Medium",
  ...
}
```

`JsonUtility` can't deserialize a `Dictionary` directly, so the loader reads
into a small `[Serializable]` wrapper (`{ Entry[] entries }` where
`Entry = { string key; string value; }`) and builds the dictionary from that
array at load time.

Key naming: `<screen>.<element>`, e.g. `menu.newGame`, `settings.musicOn`,
`editor.buildingHint`, `popup.resetConfirm.body`. Difficulty names get their
own namespace (`difficulty.easy` etc.) since they're displayed in multiple
places (header label, difficulty-picker popup).

Templated strings use `{0}`, `{1}`, ... placeholders consumed via
`string.Format` inside `Loc.Get(key, params object[] args)`.

Estimated scope: ~60-80 keys, all 11 languages authored by me (AI-generated,
not professionally reviewed — flagged to the user as acceptable for this pass).

## Error handling / fallback

- Missing key in the active language's JSON: fall back to the English value
  for that key if present, otherwise return the raw key string itself (visibly
  broken but never a crash or blank label) and log a warning in
  `Debug.isDebugBuild`/editor builds only.
- Missing/corrupt language JSON file entirely: `LocalizationLoader` falls back
  to English and logs a warning (debug builds only).
- Unmapped `Application.systemLanguage` value: falls back to `Language.English`
  as the first-launch default (see Data flow above).

## Testing

- `MobileGamesFramework.Localization` gets its own test assembly (mirroring
  `MobileGamesFramework.Tests`) covering: `LocalizationTable` key lookup and
  fallback-to-English-on-miss behavior, and the `SystemLanguage` → `Language`
  mapping table (every enum value maps to something sane, unmapped values fall
  back to English) — these are pure logic, testable without loading real
  Resources/JSON.
- JSON parsing itself (`LocalizationLoader`) needs `Resources.Load`, so it's
  exercised via on-device/in-editor manual verification rather than an
  automated unit test, consistent with how this project already handles
  Resources-dependent code.
- Manual on-device verification: cycle through at least English, Romanian, and
  one language with typically longer strings (German or Russian) across every
  screen, checking for text overflow/clipping in buttons and headers. Any
  overflow found gets flagged as a follow-up font-size/wrap tweak rather than
  blocking the initial rollout.

## Known risks

- Some translations will run longer than their English source and may overflow
  fixed-width buttons/labels (the Sudoku UI uses fixed pixel-size rects
  throughout, not auto-sizing text). This is expected to need follow-up polish
  after the initial pass, not a blocker to landing the feature.
- AI-generated translations are not human-reviewed; quality may vary by
  language, especially for shorter idiomatic UI strings ("Undo", "Hint") vs.
  longer sentences (the reset-confirmation popup body).
