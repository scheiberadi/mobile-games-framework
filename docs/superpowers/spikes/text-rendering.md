# Spike: text rendering for all 11 languages

## Question

Can TextMeshPro with bundled OFL Noto Sans fonts show Latin, Cyrillic, Romanian comma-below letters, Japanese, Korean and Simplified Chinese on the phone with no empty boxes, using dynamic font assets and fallbacks?

## Verdict

PASS. The user looked at ja, ko and zh-Hans on the phone and judged them fine. Everything measurable passed: `TEXT_MISSING 0`, no `.notdef` glyphs in the laid-out text, and the scaled screenshot shows all eleven lines with correct letter shapes, including the Romanian comma-below s and t. (Their check was needed because a scaled screenshot can hide defects; it is a single manual observation with no captured artifact.)

## Decision

Keep the decision made up front: TextMeshPro (already in `com.unity.ugui` 2.5.0, no new package) with a Noto Sans base font asset and Noto Sans JP, KR and SC as dynamic fallback assets. No change is needed to spec section 4.9. One production note for later: with the fallback order JP, KR, SC, a Han character that exists in more than one of those fonts always takes the first font's glyph shape, so Chinese text can get Japanese letter forms. M4 should pick the CJK font per language (for example by setting the fallback order or the font per language) rather than rely on one shared fallback list.

## How the fonts were added

- TMP essentials were imported from a batchmode method with `TMP_PackageResourceImporter.ImportResources(true, false, false)`. Note that `AssetDatabase.ImportPackage` is queued, so the method must run without `-quit` and exit from the `importPackageCompleted` callback; with `-quit` nothing was imported.
- Fonts were downloaded from official sources only: Noto Sans from `github.com/google/fonts` (`ofl/notosans`, with its `OFL.txt`) and the CJK fonts from `github.com/notofonts/noto-cjk` (`Sans/OTF/Japanese`, `Korean`, `SimplifiedChinese`), all under the SIL Open Font License. They live in `EvasLearningWorld/Assets/Fonts/`.
- First attempt: variable TTFs from Google Fonts for all four families (JP 9.6 MB, KR 10.4 MB, SC 17.8 MB, Noto Sans 2.0 MB). Everything rendered and `TEXT_MISSING` was 0, but the CJK lines came out visibly thin because the variable CJK fonts load at their lightest default weight. Not acceptable for a child reading.
- Second attempt (kept): static Regular OTFs from noto-cjk (about 15.7 MiB each) for ja, ko and zh-Hans. The CJK lines now have the same weight as the Latin lines. Noto Sans stays the Google Fonts variable file, which renders at Regular weight and is only 2.0 MB.
- Each font got a dynamic TMP font asset via `TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true)`, saved with its material and atlas texture as sub-assets. The three CJK assets are in the base asset's fallback list. The builder is `Assets/Spikes/Text/Editor/TextSpikeBuild.cs`; the scene script is `Assets/Spikes/Text/TextSpike.cs`.

## Evidence

- The scene shows one line per language (en, es, pt, de, fr, it, ro, ru, ja, ko, zh-Hans). The Romanian line contains `ă â î ș ț Ă Â Î Ș Ț` written with U+0219, U+021B, U+0218 and U+021A.
- The script calls `HasCharacters(joinedText, out missing, searchFallbacks: true, tryAddCharacter: true)` on the base font asset and shows the result on screen, because logcat output from release-mode builds was unreliable in an earlier spike.
- On-screen text read from the screenshot of the final build on the Samsung Galaxy S25 Ultra: `TEXT_MISSING 0   (laid out .notdef: 0)`. The second number counts visible characters in the laid-out text that resolved to glyph index 0.
- Screenshot observation: all eleven lines drawn, no empty boxes, Latin diacritics correct, Romanian s and t show the comma below (not a cedilla), Cyrillic correct, ja, ko and zh-Hans lines drawn with glyphs of the same weight as the Latin lines.
- The first build also showed the app bootstrap info text overlapping the spike text; the spike now removes that overlay in `Start`. This is spike-only.
- `tools/check-apk-compliance.sh`: no `AD_ID`, launcher activity present. No package was added to `Packages/manifest.json`; `ComplianceTests` untouched.

## APK size

- Before fonts: 23,607,854 bytes (22.5 MiB), the earlier `eva-debug.apk` built from the empty Boot scene (Task 4 baseline, a comparable debug build, without TMP resources).
- After variable fonts (first attempt): 48,441,466 bytes (46.2 MiB).
- After static CJK fonts (kept): 66,760,032 bytes (63.7 MiB), an increase of about 43.2 MB. This compares a debug APK with the earlier Boot debug APK, and the increase includes about 4 MB of TextMesh Pro essentials, so the fonts alone are somewhat less. Whether the OTFs compress poorly was not measured.

## Deferred

Trimming the CJK fonts to the characters actually used (or shipping them as a smaller static subset) is deferred to M4 (localization). The size above is the untrimmed cost. Font choice per language (see Decision) is also an M4 concern.

## Files

Fonts and their font assets are git-ignored (about 50 MB in `Assets/Fonts/`). Paths are under `EvasLearningWorld/`:

- `Assets/Fonts/NotoSans-Variable.ttf` (2.0 MB)
- `Assets/Fonts/NotoSansCJKjp-Regular.otf` (15.7 MB)
- `Assets/Fonts/NotoSansCJKkr-Regular.otf` (15.7 MB)
- `Assets/Fonts/NotoSansCJKsc-Regular.otf` (15.7 MB)
- Four dynamic font assets `Assets/Fonts/* SDF.asset` (about 6 to 8 KB each), `Assets/Fonts/OFL.txt`, and their `.meta` files.
- `Assets/TextMesh Pro/` (TMP essentials, about 4 MB, includes LiberationSans).

The four font files are the large ones. Whether to commit them, use Git LFS, or fetch them by script is left to the user. These paths are git-ignored and must be re-fetched (the Noto sources listed in this document) to rebuild the text spike.
