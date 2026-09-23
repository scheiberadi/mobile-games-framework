# Eva's Learning World, M1 Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A genuinely playable, no-reading-required loop on the S25: create a character, see the house, go to School, play "Count the objects" with Eva's spoken help, earn coins, buy furniture in the Store, place it in the house, quit, relaunch, and find everything restored.

**Architecture:** One Unity scene, one persistent uGUI canvas (1600 x 900 reference, match height), code-built screens shown by a tiny navigator. Characters are the M0 cutout pattern on `RectTransform`s (`Image` parts under an `Animator`). Game rules live in an engine-free `Rules` assembly with edit-mode tests; everything else is thin presenters. Save is one JSON string in PlayerPrefs. Eva's voice is pre-generated English MP3 clips played from `Resources`. Nothing is built for later milestones.

**Tech Stack:** Unity 6000.5.10f1, C#, uGUI + TextMeshPro (digits only), Input System only, resvg-js (`tools/svg2png`) for art, Node for the voice generator, Google Cloud Text-to-Speech (Chirp 3 HD, voice Leda) for the offline voice generation.

**Spec:** `docs/superpowers/specs/2026-09-21-evas-learning-world-design.md` (sections 4.1 to 4.6, 5.1, 6, 7, 8). M0 findings: `docs/superpowers/spikes/M0-summary.md`.

## Global Constraints

- **Scope rule (from the user, binding):** for every feature ask "is it required to make this loop fun and independently playable for a 4 to 5-year-old?" If not, it is deferred (see "Deferred out of M1"). No generic activity engine, no content generation system, no parent progress, no multi-language voice, no second mode, no future-proofing architecture, tooling or validation.
- **Audience baseline (spec 4.0):** the child is 4 to 5, may not read, plays alone. Instructions are spoken by Eva and shown by demonstration. No text is ever required. The only text on any child-facing screen is digits (numerals, coin counts) in TextMeshPro. The optional speech bubble (hidden until tapped) is the single exception and is never needed.
- **Targets:** every tappable or draggable element has a hit area of at least 240 x 240 canvas units (about 100 dp on the S25 Ultra: the canvas is 900 units tall for 1080 px). Main buttons 240 to 300. Icon-only buttons. Single taps and simple short drags only; forgive near misses.
- **Layout:** design inside a central 1440 x 900 frame (tablet aspect); backgrounds fill the whole canvas; important content also stays inside `Screen.safeArea` (`SafeAreaPanel`). Landscape only.
- **Input:** Input System only (`activeInputHandler` 1, set by `EvaProjectSetup.Apply`). Never use the old `Input` class.
- **Frame rate:** request 120 fps with vsync off (`Application.targetFrameRate = 120`, `QualitySettings.vSyncCount = 0`). Do not optimise battery in M1 unless it becomes a real problem. Never read the panel rate from `Screen.currentResolution`.
- **Text path:** TextMeshPro (`TextMeshProUGUI`), digits only in M1. TMP Essentials (`Assets/TextMesh Pro`, about 4 MB) become tracked files in Task 1. Spike fonts (`Assets/Fonts`) stay git-ignored and unused.
- **Animation:** hierarchical cutout characters animated by `Animator` clips (the M0 rig pattern), on `RectTransform`s. Pivot is the `RectTransform` pivot; draw order is sibling order. No skinning, no extra 2D packages.
- **Voice:** English only. Google Chirp 3 HD voice Leda, generated offline into `Assets/Eva/Resources/Voice/en/<key>.mp3`, no runtime TTS. Key names use letters, digits and underscores only. The API key comes from the environment variable `GOOGLE_TTS_API_KEY`, is never printed, never put in a file, never asked for in chat.
- **English is always local.** No language packs, no Play Asset Delivery work in M1.
- **Compliance (unchanged):** no ad, analytics, purchasing or services packages, no advertising ID, no own networking code, no runtime AI. `ComplianceTests` stay green. The only permission expected in the APK is `INTERNET` (Unity debug build; Play asset delivery is not used in M1).
- **Rewards:** deterministic. Coins per round are fixed by the help ladder; furniture prices are visible; the child chooses what to buy. No chests, no random drops (memory rule).
- **Local save only.** One JSON string under the PlayerPrefs key `eva.save.v1`; Auto Backup already includes shared preferences.
- **Spikes stay disposable and must not leak:** nothing under `Assets/Spikes` is referenced from production code or the build. The build scene list is `Assets/Scenes/Boot.unity` only.
- **Framework vs game code:** reusable, game-agnostic code changes go to `Packages/com.noadsguy.framework` only when a second game would use it; M1 adds none. Eva-specific code lives under `EvasLearningWorld/Assets/Eva`.
- **Repo rules:** work on a new branch `eva-m1` created from the current `eva-m0` head in place (no worktree: the Unity `Library` is multi-GB); do not commit or push unless the user explicitly asks (every "Commit" step is skipped; report instead). Do not touch `docs/store-assets`, `Keystores/`, or the Sudoku and 2048 projects. Never print secrets. Only one Unity batchmode process at a time; run Unity with `run_in_background` and short bounded waits (cap about 20 minutes); do not edit sources while Unity runs.
- **Device:** Samsung Galaxy S25 Ultra, serial `R3CY30NNA6W`, gesture navigation only. Never send `KEYCODE_BACK`, never tap by guessed coordinates; ask the user to touch the phone; delete screenshots afterwards. The phone must be unlocked and awake during on-device runs. `adb shell pm clear com.noadsguy.evas.dev` resets the save (our own dev app).

## User prerequisites

- **Task 4 needs `GOOGLE_TTS_API_KEY`:** a Google Cloud project with the Text-to-Speech API enabled and an API key, set as a user environment variable. The user creates it; the executor never asks for it in chat. Until it exists, everything else proceeds (missing clips are skipped silently, and the speech bubble shows the text). The M1 acceptance in Task 13 requires voice.
- **Phone connected, unlocked and awake** for Tasks 1, 6, 7, 8, 9, 10, 11, 12 and 13, and the user taps when a task says so.

## File structure

All paths under `EvasLearningWorld/` unless stated.

```
Assets/Eva/Rules/            EvasLearningWorld.Rules.asmdef (noEngineReferences)
  Counting.cs                CountObject, CountRound, CountRoundGenerator
  HelpAndCoins.cs            HelpStep, HelpLadder, CoinPayout
  Furniture.cs               SlotKind, FurnitureItem, FurnitureCatalog, HouseSlot, HouseSlots
  House.cs                   Placement, HouseLayout
  Progress.cs                CharacterLook, BuyResult, PlayerProgress
  Tutorial.cs                TutorialStep, TutorialEvent, TutorialFlow
Assets/Eva/App/              EvasLearningWorld.App.asmdef (existing)
  EvaBootstrap.cs            frame rate, creates EvaGame
  EvaGame.cs                 composition root and shared context (Progress, Save, Voice, Sfx, Navigator, Hud)
  Save/PlayerPrefsStore.cs   IKeyValueStore over PlayerPrefs
  Save/SaveStore.cs          JSON load/save of PlayerProgress
  Ui/EvaUi.cs                MinTap, TapTarget, icon buttons, numerals, sprite loading
  Ui/SafeAreaPanel.cs
  Ui/Hud.cs                  home button, coin counter, speech-bubble button, fps digits
  Ui/PointerHand.cs          Eva's pointing hand (moves, pulses, drags)
  Audio/Sfx.cs               procedural tones (framework ProceduralAudio)
  Audio/Voice.cs             Say/SayAndWait/Duration, Talking flag
  Audio/VoiceLines.cs        key -> English text from Resources/Voice/voice-lines.txt
  Characters/CharacterRig.cs, RigFactory.cs, Palette.cs
  Screens/ScreenBase.cs, Navigator.cs, MapScreen.cs, CreatorScreen.cs, HouseScreen.cs,
          StoreScreen.cs, CountScreen.cs, TutorialGuide.cs
Assets/Eva/Resources/Art/    generated PNGs (tracked, small)
Assets/Eva/Resources/Anim/   Rig.controller and clips (generated, tracked)
Assets/Eva/Resources/Voice/  voice-lines.txt (source of truth) and en/*.mp3
Assets/Editor/               EvaArtImporter.cs, EvaRigAssets.cs (editor tools)
Assets/Eva/Tests/            EvasLearningWorld.Tests.asmdef (existing) + tests per task
art/eva/{icons,objects,world,characters}/*.svg     (repo root)  art sources
tools/build-eva-art.sh       SVG to PNG for all art groups
tools/voice/generate.js, generate.test.js, package.json   voice generator
```

## Child-experience acceptance criteria (the M1 gate, run in Task 13)

Numbers are checked by tests or by the user on the S25. "Adult plays as a non-reader" never replaces a real child; if a 4 to 5-year-old is available, their run is the primary evidence and their hesitations feed M2.

- **A1 No reading:** an automated audit finds no letters in any child-facing screen (only digits) and no interactive element under 240 x 240 units; and the user completes the whole loop with the speech bubble never opened and sound on.
- **A2 Independent first run:** from a cleared save to "furniture bought and placed" with no adult help and no wrong dead ends: at every step Eva says what to do and a pointing hand shows where; the child never has to guess. Count the times the observer had to intervene: target 0, and 1 or more means a named fix.
- **A3 Mistakes are safe and teach:** a wrong answer never punishes (soft wobble, no buzzer, no lost coins). Three mistakes always end with the child doing the correct action themselves after Eva demonstrates, and the round pays the lowest tier.
- **A4 Reward is clear and fair:** the first session (five rounds) always pays at least the cheapest furniture price, even if every round is demonstrated; coins visibly count up; the price of every item is visible as coin icons plus a digit.
- **A5 Feel:** every tap gives an immediate sound and visual response; screens change without a visible stall; the animation on the S25 runs at a measured 110 fps or more in the Count round (digits-only fps counter in the corner of debug builds).
- **A6 Persistence:** after `am force-stop` and relaunch, the look, coins, owned furniture, placements and tutorial step are exactly as before; corrupt save data starts a clean game instead of crashing.
- **A7 Fun (the user's judgement, the actual gate):** would a child come back to this? The user answers yes or lists what to change; the answer decides M2.

## Tasks

Each task ends with edit-mode tests green and, where noted, a build on the phone. Test command (Eva project): `bash tools/run-editmode-tests.sh "C:/Users/schei/mobile-games-framework/EvasLearningWorld"` (never `-quit`). Build: `bash tools/build-eva-debug.sh`. Install and launch: `bash tools/eva-install.sh EvasLearningWorld/Builds/Android/eva-debug.apk com.noadsguy.evas.dev "$TEMP/x.png"`. Compliance: `bash tools/check-apk-compliance.sh EvasLearningWorld/Builds/Android/eva-debug.apk`. Repo-root suite (`bash tools/run-editmode-tests.sh "C:/Users/schei/mobile-games-framework"`, expect 187) runs once in Task 13 only, because M1 changes no framework code.

Task order is chosen so that something is playable on the phone as early as possible: 1 shell, 2 rules, 3 progress and save, 4 voice generation (no Unity, runs in the background of the others), 5 art, 6 characters, 7 to 8 Count the Objects, 9 creator and map flow, 10 house, 11 store, 12 tutorial guidance, 13 acceptance.

---

### Task 1: App shell, navigation and the first device run

Question answered: does the shell (one canvas, safe area, navigator, icon buttons, 120 fps, sound, voice plumbing) work on the phone? Placeholder art only.

**Files:**
- Modify: `.gitignore` (remove the two `TextMesh Pro` ignore lines; keep `Fonts`), `Assets/Eva/App/EvaBootstrap.cs`, `Assets/Eva/App/EvasLearningWorld.App.asmdef`, `Assets/Eva/Tests/EvasLearningWorld.Tests.asmdef`
- Create: `Assets/Eva/Rules/EvasLearningWorld.Rules.asmdef`, `Assets/Eva/App/EvaGame.cs`, `Ui/EvaUi.cs`, `Ui/SafeAreaPanel.cs`, `Ui/Hud.cs`, `Audio/Sfx.cs`, `Audio/Voice.cs`, `Audio/VoiceLines.cs`, `Screens/ScreenBase.cs`, `Screens/Navigator.cs`, `Screens/MapScreen.cs`, placeholder `HouseScreen.cs`, `StoreScreen.cs`, `CountScreen.cs`, `Assets/Eva/Resources/Voice/voice-lines.txt` (empty header for now), `Assets/Eva/Tests/NoReadingAuditTests.cs`, `Assets/Eva/Tests/VoiceLinesTests.cs`

**Interfaces:**
- Produces (used by every later task):
  - `EvaUi.MinTap = 240f`; `EvaUi.Sprite(string name)` loads `Resources/Art/<name>` and, when missing, returns a generated rounded-rect placeholder (so screens work before art exists); `EvaUi.IconButton(Transform parent, string name, Sprite icon, Vector2 anchor, Vector2 position, float size, UnityAction onClick)` returns `Button`, forces size >= `MinTap`, adds `TapTarget`, plays `Sfx.Tap` and a 0.9 scale press feedback; `EvaUi.Numeral(Transform parent, string name, int fontSize)` returns `TextMeshProUGUI` (digits only); `TapTarget : MonoBehaviour` marker (draggables also carry it).
  - `abstract class ScreenBase { RectTransform Root; abstract void Build(EvaGame game); virtual void OnShow(); virtual void OnHide(); }`.
  - `enum ScreenId { Creator, Map, House, School, Store }` and `Navigator.Show(ScreenId)`, `Navigator.Current`.
  - `EvaGame`: `Voice Voice`, `Sfx Sfx`, `Navigator Navigator`, `Hud Hud`, `RectTransform ScreenRoot`. Task 3 adds `PlayerProgress Progress` and `void Commit()`; Task 1 has no game state.
  - `Voice`: `void Say(string key)`, `IEnumerator SayAndWait(string key)`, `float Duration(string key)` (clip length, else `max(1.0, 0.06 * text.Length)` seconds), `bool IsSpeaking`, `string LastKey`, `event Action<bool> SpeakingChanged`. `Say` stops the previous line. Missing clip: no sound, still "speaks" for `Duration`.
  - `VoiceLines.TextFor(string key)` parses `Resources/Voice/voice-lines.txt` (`key<TAB>English text` per line, `#` comments and blank lines ignored; returns the key itself when missing).
  - `Sfx`: `Tap()`, `Right()`, `Retry()`, `Coin()`, `Place()`, `Buy()` using `MobileGamesFramework.UI.ProceduralAudio.GenerateTone` (tap 660 Hz 0.05 s; right two rising tones; retry one low soft tone 220 Hz 0.15 s; coin 1300 Hz 0.06 s; place 440 Hz 0.08 s; buy three rising tones).
  - `Hud`: `SetHomeVisible(bool)`, `SetCoins(int)`, `SetBubbleVisible(bool)`; home icon button top-left (calls `Navigator.Show(ScreenId.Map)`), coin icon plus numeral top-right, speech-bubble icon button bottom-right showing `VoiceLines.TextFor(Voice.LastKey)` in a temporary bubble (legacy `Text` is not allowed; use TMP; the bubble is the only place letters may appear and it is hidden until tapped), and a digits-only fps counter bottom-left in debug builds (`Debug.isDebugBuild`), showing the measured 1 s average as an integer.

- [ ] **Step 1: Assemblies.** Create `EvasLearningWorld.Rules.asmdef` (`"noEngineReferences": true`, no references, `autoReferenced` true). Add `EvasLearningWorld.Rules`, `Unity.TextMeshPro` to the App asmdef references; add `EvasLearningWorld.Rules`, `Unity.TextMeshPro`, `UnityEngine.UI` to the Tests asmdef references. Remove the two `TextMesh Pro` lines from `.gitignore` (leave `Assets/Fonts/` ignored) and check `git status --short -uall EvasLearningWorld | grep -c "TextMesh Pro"` is greater than 0.
- [ ] **Step 2: Bootstrap.** `EvaBootstrap.Register` sets `Application.targetFrameRate = 120; QualitySettings.vSyncCount = 0;` and creates a GameObject with `EvaGame`. Remove the M0 info screen.
- [ ] **Step 3: EvaGame** builds, in this order: canvas via `UiFactory.CreateCanvas(new Vector2(1600, 900), 1f)` (it creates the EventSystem only when none exists), `UiFactory.CreateBackground(...)`, a `SafeAreaPanel` (a stretched `RectTransform` whose anchors follow `Screen.safeArea`, re-applied when `Screen.safeArea` or resolution changes) named `ScreenRoot`, `Sfx`, `Voice` (an `AudioSource` on a child object), `Navigator`, `Hud` (above the screens), then shows `ScreenId.Map`.
- [ ] **Step 4: Screens.** `MapScreen` shows three placeholder building icons (House, School, Store as `EvaUi.IconButton` with placeholder sprites, 300 units, evenly spread inside the 1440 x 900 frame), each calling `Navigator.Show(...)`. The other screens are a coloured background plus a title placeholder sprite; `Hud` shows the home button on every screen except Map.
- [ ] **Step 5: Tests (write first, see them fail, then pass).**
  - `VoiceLinesTests`: parsing ignores comments and blanks, splits on the first tab, returns the key when missing.
  - `NoReadingAuditTests`: builds every screen (`Navigator` with a test `EvaGame`) under a temporary canvas and asserts (a) every `TMP_Text` and every legacy `Text` under it has text matching `^[0-9]*$` except objects under a component named `Bubble`, (b) every `TapTarget` has `rect.width >= 240 && rect.height >= 240`, (c) no `Button` or `IPointerClickHandler` or `IDragHandler` component exists on an object without a `TapTarget`. This test is the automated child-baseline guard; every later screen is added to its list.
- [ ] **Step 6: Run the suite, build, compliance check, install.** Expected: tests green, `OK: no AD_ID`, the app opens on the Map with three big icons.
- [ ] **Step 7: Device check (ask the user to touch the phone).** They tap each building and the home icon. Expected: each opens its placeholder screen, the home icon returns to the map, taps make a sound, the bottom-left number reads about 110 to 120. Delete screenshots.
- [ ] **Step 8: Report** what changed, test counts, and the measured fps number. Do not commit.

---

### Task 2: Rules for counting, help and coins

**Files:**
- Create: `Assets/Eva/Rules/Counting.cs`, `Assets/Eva/Rules/HelpAndCoins.cs`, `Assets/Eva/Tests/CountingTests.cs`, `Assets/Eva/Tests/HelpAndCoinsTests.cs`

**Interfaces:**
- Produces: `CountObject { Apple, Star, Duck, Flower }`; `CountRound { int Quantity; CountObject Object; int[] Choices; }`; `CountRoundGenerator.RoundsPerSession = 5`, `MaxQuantityByRound = {3,3,4,5,5}`, `Create(int roundIndex, System.Random rng, CountObject? previous)`; `CountTally(int quantity)` with `int Counted`, `bool IsComplete`, `bool IsCounted(int objectIndex)` and `bool TryCount(int objectIndex, out int number)` (the one-to-one counting aid: an object is counted at most once; `TryCount` returns true and the running number 1..Quantity only for an object not yet counted, and returns false, with `number` set to the current `Counted`, for an already counted or out-of-range index); `HelpStep { None, Retry, Hint, Demonstrate }`; `HelpLadder { int Mistakes; HelpStep Step; HelpStep RecordMistake(); }`; `CoinPayout.Clean = 3`, `Assisted = 2`, `Demonstrated = 1`, `ForStep(HelpStep)`, `MinSessionPayout`.

- [ ] **Step 1: Write the failing tests.**

```csharp
using System;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class CountingTests
    {
        [Test]
        public void EveryRoundHasValidQuantityAndThreeDistinctAscendingChoices()
        {
            for (var seed = 0; seed < 200; seed++)
            for (var round = 0; round < CountRoundGenerator.RoundsPerSession; round++)
            {
                var r = CountRoundGenerator.Create(round, new Random(seed), null);
                var max = CountRoundGenerator.MaxQuantityByRound[round];
                var pool = Math.Max(3, max);
                Assert.That(r.Quantity, Is.InRange(1, max));
                Assert.That(r.Choices.Length, Is.EqualTo(3));
                Assert.That(r.Choices, Is.Ordered.Ascending);
                Assert.That(r.Choices, Is.Unique);
                Assert.That(r.Choices, Does.Contain(r.Quantity));
                Assert.That(r.Choices, Is.All.InRange(1, pool));
            }
        }

        [Test]
        public void ObjectDiffersFromThePreviousRound()
        {
            for (var seed = 0; seed < 100; seed++)
                foreach (CountObject previous in Enum.GetValues(typeof(CountObject)))
                    Assert.That(CountRoundGenerator.Create(2, new Random(seed), previous).Object, Is.Not.EqualTo(previous));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = CountRoundGenerator.Create(3, new Random(7), null);
            var b = CountRoundGenerator.Create(3, new Random(7), null);
            Assert.That(a.Quantity, Is.EqualTo(b.Quantity));
            Assert.That(a.Object, Is.EqualTo(b.Object));
            Assert.That(a.Choices, Is.EqualTo(b.Choices));
        }

        [Test]
        public void RoundIndexOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CountRoundGenerator.Create(5, new Random(1), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => CountRoundGenerator.Create(-1, new Random(1), null));
        }

        [Test]
        public void TallyCountsEachObjectOnceInTheOrderTheChildTapsThem()
        {
            var tally = new CountTally(3);
            Assert.That(tally.TryCount(2, out var n), Is.True);
            Assert.That(n, Is.EqualTo(1));
            Assert.That(tally.TryCount(0, out n), Is.True);
            Assert.That(n, Is.EqualTo(2));
            Assert.That(tally.IsComplete, Is.False);
            Assert.That(tally.TryCount(1, out n), Is.True);
            Assert.That(n, Is.EqualTo(3));
            Assert.That(tally.IsComplete, Is.True);
        }

        [Test]
        public void TappingAnAlreadyCountedObjectDoesNotAdvanceTheCount()
        {
            var tally = new CountTally(4);
            tally.TryCount(1, out _);
            tally.TryCount(3, out _);
            Assert.That(tally.TryCount(1, out var n), Is.False);
            Assert.That(n, Is.EqualTo(2));
            Assert.That(tally.Counted, Is.EqualTo(2));
            Assert.That(tally.IsCounted(1), Is.True);
            Assert.That(tally.IsCounted(0), Is.False);
        }

        [Test]
        public void OutOfRangeIndexIsIgnored()
        {
            var tally = new CountTally(2);
            Assert.That(tally.TryCount(-1, out _), Is.False);
            Assert.That(tally.TryCount(2, out _), Is.False);
            Assert.That(tally.Counted, Is.EqualTo(0));
        }
    }
}
```

```csharp
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class HelpAndCoinsTests
    {
        [Test]
        public void MistakesClimbTheLadderAndStopAtDemonstrate()
        {
            var ladder = new HelpLadder();
            Assert.That(ladder.Step, Is.EqualTo(HelpStep.None));
            Assert.That(ladder.RecordMistake(), Is.EqualTo(HelpStep.Retry));
            Assert.That(ladder.RecordMistake(), Is.EqualTo(HelpStep.Hint));
            Assert.That(ladder.RecordMistake(), Is.EqualTo(HelpStep.Demonstrate));
            Assert.That(ladder.RecordMistake(), Is.EqualTo(HelpStep.Demonstrate));
            Assert.That(ladder.Mistakes, Is.EqualTo(3));
        }

        [Test]
        public void PayoutFallsWithHelpButNeverReachesZero()
        {
            Assert.That(CoinPayout.ForStep(HelpStep.None), Is.EqualTo(3));
            Assert.That(CoinPayout.ForStep(HelpStep.Retry), Is.EqualTo(2));
            Assert.That(CoinPayout.ForStep(HelpStep.Hint), Is.EqualTo(2));
            Assert.That(CoinPayout.ForStep(HelpStep.Demonstrate), Is.EqualTo(1));
            Assert.That(CoinPayout.MinSessionPayout, Is.EqualTo(CountRoundGenerator.RoundsPerSession));
        }
    }
}
```

- [ ] **Step 2: Run the suite; expected: compile failure (types missing).**
- [ ] **Step 3: Implement.**

```csharp
using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public enum CountObject { Apple, Star, Duck, Flower }

    public sealed class CountRound
    {
        public int Quantity;
        public CountObject Object;
        public int[] Choices;
    }

    public static class CountRoundGenerator
    {
        public const int RoundsPerSession = 5;
        // Quantity ceiling per round: the first rounds stay at 1 to 3 (a 4-year-old), the last reach 5.
        public static readonly int[] MaxQuantityByRound = { 3, 3, 4, 5, 5 };

        public static CountRound Create(int roundIndex, Random rng, CountObject? previous)
        {
            if (roundIndex < 0 || roundIndex >= RoundsPerSession) throw new ArgumentOutOfRangeException(nameof(roundIndex));
            var max = MaxQuantityByRound[roundIndex];
            var pool = Math.Max(3, max);
            var quantity = rng.Next(1, max + 1);
            var choices = new List<int> { quantity };
            while (choices.Count < 3)
            {
                var candidate = rng.Next(1, pool + 1);
                if (!choices.Contains(candidate)) choices.Add(candidate);
            }
            choices.Sort();
            CountObject obj;
            do { obj = (CountObject)rng.Next(0, 4); } while (previous.HasValue && obj == previous.Value);
            return new CountRound { Quantity = quantity, Object = obj, Choices = choices.ToArray() };
        }
    }

    // One-to-one counting aid: each object is counted at most once, so repeated taps on one object
    // can never inflate the spoken number.
    public sealed class CountTally
    {
        private readonly bool[] _counted;

        public CountTally(int quantity) { _counted = new bool[quantity]; }

        public int Counted { get; private set; }
        public bool IsComplete => Counted == _counted.Length;
        public bool IsCounted(int objectIndex) => objectIndex >= 0 && objectIndex < _counted.Length && _counted[objectIndex];

        public bool TryCount(int objectIndex, out int number)
        {
            number = Counted;
            if (objectIndex < 0 || objectIndex >= _counted.Length || _counted[objectIndex]) return false;
            _counted[objectIndex] = true;
            number = ++Counted;
            return true;
        }
    }
}
```

```csharp
namespace EvasLearningWorld.Rules
{
    public enum HelpStep { None = 0, Retry = 1, Hint = 2, Demonstrate = 3 }

    // 1st mistake: gentle retry. 2nd: contextual hint. 3rd: Eva demonstrates and the child does the correct action.
    public sealed class HelpLadder
    {
        public int Mistakes { get; private set; }
        public HelpStep Step => Mistakes >= 3 ? HelpStep.Demonstrate : (HelpStep)Mistakes;

        public HelpStep RecordMistake()
        {
            if (Step != HelpStep.Demonstrate) Mistakes++;
            return Step;
        }
    }

    public static class CoinPayout
    {
        public const int Clean = 3;
        public const int Assisted = 2;
        public const int Demonstrated = 1;
        public static int MinSessionPayout => CountRoundGenerator.RoundsPerSession * Demonstrated;

        public static int ForStep(HelpStep step)
        {
            if (step == HelpStep.None) return Clean;
            return step == HelpStep.Demonstrate ? Demonstrated : Assisted;
        }
    }
}
```

- [ ] **Step 4: Run the suite; expected: all Eva tests green (the previous 76 plus the new ones).** Report the counts.

---

### Task 3: Progress, house, furniture, tutorial rules and the save

**Files:**
- Create: `Assets/Eva/Rules/Furniture.cs`, `House.cs`, `Progress.cs`, `Tutorial.cs`; `Assets/Eva/App/Save/PlayerPrefsStore.cs`, `Save/SaveStore.cs`; tests `FurnitureTests.cs`, `HouseTests.cs`, `ProgressTests.cs`, `TutorialFlowTests.cs`, `SaveStoreTests.cs`
- Modify: `Assets/Eva/App/EvaGame.cs` (own `Progress`, `SaveStore`; `Commit()` saves; load at start), `Assets/Eva/Tests/NoReadingAuditTests.cs` only if its test game needs a fresh `PlayerProgress`.

**Interfaces:**
- Produces:
  - `SlotKind { Seat, Floor, Table, Corner, Bed, Wall }`; `FurnitureItem { string Id; SlotKind Kind; int Price; }`; `FurnitureCatalog.All`, `FurnitureCatalog.StarterId = "sofa"`, `FurnitureCatalog.Find(string id)` (null when unknown), `FurnitureCatalog.CheapestPrice` (over non-starter items only; the starter costs 0 and is not for sale).
  - `HouseSlot { string Id; string Room; SlotKind Kind; }`; `HouseSlots.All` (7 slots), `HouseSlots.Find(id)`.
  - `HouseLayout { List<Placement> Placements; string SlotOf(string itemId); string ItemIn(string slotId); bool CanPlace(string itemId, string slotId, IEnumerable<string> owned); bool TryPlace(string itemId, string slotId, IEnumerable<string> owned); bool Remove(string itemId); }` (`Placement { string ItemId; string SlotId; }`).
  - `CharacterLook { int Head; int Skin; int Shirt; }` with `HeadCount = 4`, `ColorCount = 5`; `BuyResult { Bought, NotEnoughCoins, AlreadyOwned, UnknownItem }`; `PlayerProgress { int Version; bool HasCharacter; CharacterLook Look; int Coins; List<string> Owned; HouseLayout House; TutorialStep Tutorial; bool CountIntroSeen; void AddCoins(int n); BuyResult TryBuy(string itemId); void GrantStarter(); bool Advance(TutorialEvent e); }`.
  - `TutorialStep { CreateCharacter, PlaceStarter, GoToSchool, FirstGame, GoToStore, FirstPurchase, PlacePurchase, Done }`; `TutorialEvent { LookConfirmed, ItemPlaced, EnteredSchool, RoundsFinished, EnteredStore, ItemBought }`; `TutorialFlow.Next(step, event)`.
  - `SaveStore(IKeyValueStore store)`: `PlayerProgress Load()` (missing or unparsable JSON returns `new PlayerProgress()`), `void Save(PlayerProgress p)`; key `eva.save.v1`. `PlayerPrefsStore : IKeyValueStore` (Eva's own copy, `PlayerPrefs.Save()` after each `SetString`).
  - `EvaGame.Progress`, `EvaGame.Commit()`.

Data (exact values):

| Item id | Slot kind | Price |
|---|---|---|
| sofa (starter, free, not for sale) | Seat | 0 |
| rug | Floor | 5 |
| table | Table | 8 |
| lamp | Corner | 6 |
| plant | Corner | 6 |
| bed | Bed | 10 |
| bookshelf | Wall | 9 |

| Slot id | Room | Kind |
|---|---|---|
| living_seat | living | Seat |
| living_floor | living | Floor |
| living_table | living | Table |
| living_corner | living | Corner |
| bedroom_bed | bedroom | Bed |
| bedroom_corner | bedroom | Corner |
| bedroom_wall | bedroom | Wall |

Tutorial transitions (any other event leaves the step unchanged): `CreateCharacter --LookConfirmed--> PlaceStarter`; `PlaceStarter --ItemPlaced--> GoToSchool`; `GoToSchool --EnteredSchool--> FirstGame`; `FirstGame --RoundsFinished--> GoToStore`; `GoToStore --EnteredStore--> FirstPurchase`; `FirstPurchase --ItemBought--> PlacePurchase`; `PlacePurchase --ItemPlaced--> Done`.

- [ ] **Step 1: Write the failing tests** (RED first, then implement, then GREEN). Required tests, written out in full by the implementer in the style of Task 2:
  - `FurnitureTests`: ids unique; every `SlotKind` used by an item has at least one slot; the starter `sofa` has `Price == 0`; `CheapestPrice == 5` (the free starter is excluded); `CoinPayout.MinSessionPayout >= FurnitureCatalog.CheapestPrice` (the first-purchase guarantee, A4); `Find("nope") == null`.
  - `HouseTests`: placing an owned item into a slot of its kind succeeds; wrong kind fails; unowned item fails; occupied slot (different item) fails; lamp and plant can each take one of the two Corner slots; placing an already placed item moves it (old slot becomes empty); `Remove` frees the slot and returns false when not placed; placing the same item in the same slot again returns true.
  - `ProgressTests`: `TryBuy` deducts coins and adds to `Owned` (`Bought`); `NotEnoughCoins` leaves coins and owned untouched; second buy of the same id is `AlreadyOwned`; unknown id is `UnknownItem`; `TryBuy("sofa")` (the starter is never sold) is `UnknownItem` and does not add it; `GrantStarter` adds `sofa` once; `AddCoins` accumulates; `Advance` returns true only when the step changed.
  - `TutorialFlowTests`: the seven transitions above and one ignored event per step.
  - `SaveStoreTests` (App tests, with a `FakeKeyValueStore`): a fully populated `PlayerProgress` (look, coins, owned, two placements, tutorial step `GoToStore`, `CountIntroSeen`) round-trips through `Save` then `Load` with equal values; `Load` on an empty store returns defaults (`HasCharacter == false`, `Tutorial == CreateCharacter`); `Load` on the string `"{not json"` returns defaults and does not throw.
- [ ] **Step 2: Implement** the Rules types exactly per the interfaces and tables above (`[System.Serializable]` classes with public fields so `JsonUtility` can serialise them; `TutorialStep` and `SlotKind` serialise as ints). `SaveStore` uses `JsonUtility`. `PlayerProgress.TryBuy` checks in this order: unknown id or the starter (`UnknownItem`, the starter is not for sale), already owned, not enough coins, then buy.
- [ ] **Step 3: Wire `EvaGame`:** `Progress = new SaveStore(new PlayerPrefsStore()).Load()` at start; `Commit()` saves; `Hud.SetCoins(Progress.Coins)` on start and after every `Commit()`.
- [ ] **Step 4: Run the suite; all green; build the APK to prove the assembly graph compiles for Android (no device step).** Report counts.

---

### Task 4: Voice lines and the generator (no Unity)

**Files:**
- Create: `EvasLearningWorld/Assets/Eva/Resources/Voice/voice-lines.txt`, `tools/voice/generate.js`, `tools/voice/generate.test.js`, `tools/voice/package.json`

The voice-line source of truth is a tab-separated text file, one `key<TAB>English text` per line. These are the complete M1 lines (all screens use these keys; a later task that needs a new line adds it here and re-runs the generator, which skips clips that already exist):

```
# create
create_head	Hi, I'm Eva! Pick the head you like!
create_color	Now pick your colors!
create_done	Wow, that's you! Tap the green check when you're ready!
# map
map_welcome	Hi! Where do you want to go?
map_school	Let's go to school! Tap the school!
map_store	Let's go shopping! Tap the store!
map_house	Let's go home and put it in your house! Tap the house!
# house
house_welcome	This is your house! Here is a sofa for you. Drag it into the house!
house_new	Drag your new furniture into the house!
house_placed	It looks so nice!
# count
count_intro	Let's count things! Watch me first.
count_q_apple	How many apples do you see?
count_q_star	How many stars do you see?
count_q_duck	How many ducks do you see?
count_q_flower	How many flowers do you see?
count_retry	Oops, not quite. Try again!
count_hint	Let's count together!
count_demo	Let's count them together. Tap each one with me!
count_demo_answer	Now tap the number!
count_right_1	Yes! That's right!
count_right_2	Great job!
count_right_3	You did it!
count_done	You did great! Look at all your coins!
count_again	Tap the arrow to play again, or the house to go home!
# numbers
num_1	One.
num_2	Two.
num_3	Three.
num_4	Four.
num_5	Five.
# store
store_welcome	Welcome to the store! Tap what you like!
store_buy_q	Do you want to buy this?
store_bought	Yay! You bought it!
store_not_enough	You need more coins. Let's play at school!
store_owned	You already have this one!
# end of tutorial
tut_done	You did it! Now you can play, shop and decorate any time!
```

**Interfaces:**
- Produces: `node tools/voice/generate.js <voice-lines.txt> <outDir> [--force]`. For each line whose `<outDir>/<key>.mp3` does not exist (or with `--force`), POST to `https://texttospeech.googleapis.com/v1/text:synthesize?key=<GOOGLE_TTS_API_KEY>` with `{"input":{"text":...},"voice":{"languageCode":"en-US","name":"en-US-Chirp3-HD-Leda"},"audioConfig":{"audioEncoding":"MP3"}}`, decode the base64 `audioContent`, write the file. Exports `parseLines(text)` and `generate({lines, outDir, apiKey, fetchImpl, force})` for tests. Never prints the key or the request URL; on a non-2xx response prints the status and the API's error message only and exits non-zero after trying the remaining lines once.

- [ ] **Step 1: Write `voice-lines.txt`** exactly as above (tabs, not spaces; a `voice-lines.txt.meta` is created by Unity later).
- [ ] **Step 2: Failing tests (`node --test`)** with a fake `fetchImpl`: `parseLines` ignores comments and blanks and splits on the first tab; existing files are skipped without a request; `--force` regenerates; the request body carries the voice name and `MP3`; a 403 response is reported without the key text appearing anywhere in the captured output; missing `GOOGLE_TTS_API_KEY` exits with a clear message before any request.
- [ ] **Step 3: Implement `generate.js`** (Node 24, built-in `fetch`, no dependencies) and `package.json` (`"scripts": {"test": "node --test"}`); `cd tools/voice && npm test` passes.
- [ ] **Step 4 (user-gated): first real run.** Only when `GOOGLE_TTS_API_KEY` is set: `node tools/voice/generate.js EvasLearningWorld/Assets/Eva/Resources/Voice/voice-lines.txt EvasLearningWorld/Assets/Eva/Resources/Voice/en`. Expected: one non-empty clip per key in the file. If the API replies 403 or "API not enabled", report exactly that to the user and stop this step (the rest of M1 continues without voice; the speech bubble shows text). Ask the user to listen to three clips by ear (`create_head`, `count_retry`, `count_right_2`), and report if the voice is not Leda-warm. Do not print or store the key. If the key is not set, record "voice generation pending: needs GOOGLE_TTS_API_KEY" and finish the task without it; Task 13 will not pass without it.
- [ ] **Step 5: Confirm size and git:** total clip size (expect well under 5 MB) and that nothing else was added. Report.

---

### Task 5: Art A: icons, count objects, map and School

Art bar: readable and consistent, soft shaded (gradient fills, a soft drop shadow, rounded shapes), friendly and bold. Not final; M2 polishes. Drawn as hand-written SVG in `art/eva/**`, rasterised by `tools/svg2png`.

**Files:**
- Create: `art/eva/icons/*.svg`, `art/eva/objects/*.svg`, `art/eva/world/*.svg`; `tools/build-eva-art.sh`; `Assets/Editor/EvaArtImporter.cs`; generated PNGs under `Assets/Eva/Resources/Art/{icons,objects,world}/`; `Assets/Eva/Tests/ArtTests.cs`

**Interfaces:**
- Produces sprites loadable as `EvaUi.Sprite("icons/coin")` and so on (path relative to `Resources/Art`, no extension). Names and pixel widths:
  - `icons` (256 px wide): `coin`, `check` (green), `cross` (red, soft), `home`, `replay` (arrow), `bubble` (speech bubble), `hand` (Eva's pointing hand, a cartoon glove, pointing up-left), `dot` (a plain filled circle used for answer dots), `tile` (a soft rounded answer-tile background, 300 x 300).
  - `objects` (256 px): `apple`, `star`, `duck`, `flower`.
  - `world` : `map_bg` (1920 x 900, soft landscape with a path), `school_bg` (1920 x 900, a classroom-like calm room), `house_icon`, `school_icon`, `store_icon` (each 512 px, a little building with clear silhouette; the icons must be told apart by shape and colour alone).
- `EvaArtImporter : AssetPostprocessor` sets every texture under `Assets/Eva/Resources/Art/` to Sprite (Single), alpha is transparency, no mipmaps, bilinear, uncompressed for icons and characters, pixels per unit 100.
- `tools/build-eva-art.sh` runs `node tools/svg2png/svg2png.js art/eva/<group> EvasLearningWorld/Assets/Eva/Resources/Art/<group> <width>` per group (icons 256, objects 256, world 512; the two backgrounds are rendered at 1920 by the script with a per-file exception list) and exits non-zero on the first failure.

- [ ] **Step 1: Failing test `ArtTests`:** every sprite name in the list above resolves through `Resources.Load<Sprite>` (so a missing or wrongly imported file fails the test); no sprite is smaller than 64 px on either side.
- [ ] **Step 2: Author the SVGs**, run `bash tools/build-eva-art.sh`, let Unity import them (run the suite once to import and test).
- [ ] **Step 3: Use the real sprites** in `MapScreen` (the three building icons on `map_bg`, 300 units, Eva not yet), `Hud` (coin, home, bubble icons) and the placeholder `CountScreen` background (`school_bg`). Update the audit test lists if needed.
- [ ] **Step 4: Run the suite; build; install; ask the user to look at the phone** and say if anything is unreadable, ugly enough to distract a child, or hard to tell apart (the three buildings, the four objects). Fix once. Delete screenshots. Report.

---

### Task 6: Character rig (Eva and the player) on the phone

Question answered: does the M0 cutout pattern hold on `RectTransform`s, with one shared controller for Eva and the player? Fallback if it does not (after two attempts): keep the same hierarchy as `SpriteRenderer`s under a Screen Space Camera canvas and record the change in the report.

**Files:**
- Create: art `art/eva/characters/*.svg` and PNGs under `Assets/Eva/Resources/Art/characters/`; `Assets/Eva/App/Characters/CharacterRig.cs`, `RigFactory.cs`, `Palette.cs`; `Assets/Editor/EvaRigAssets.cs`; generated `Assets/Eva/Resources/Anim/Rig.controller` and clips; `Assets/Eva/Tests/RigTests.cs`
- Modify: `Screens/MapScreen.cs` (Eva stands on the map), `tools/build-eva-art.sh` (characters group at 512 px)

**Interfaces:**
- Art (all body parts light grey-white so `Image.color` tints them; faces drawn in dark on the heads): player `char_torso`, `char_arm`, `char_leg`, `char_head_0` (girl, hair), `char_head_1` (boy, hair), `char_head_2` (bear), `char_head_3` (rabbit); Eva (a warm orange cat, not tinted) `eva_torso`, `eva_head`, `eva_arm`, `eva_leg`, `eva_tail`.
- Hierarchy (identical for both, child order is draw order, later children in front): `Root(Animator)` > `LegL`, `LegR`, `Tail` (Eva only), `Torso` > `ArmL`, `ArmR`, `Head`. Arms and legs use `RectTransform.pivot = (0.5, 1)`. Torso pivot bottom-centre. Names are exact because the clips bind by path.
- `Palette`: `Color[] Skin` (5 values, light to dark), `Color[] Shirt` (5 bright values), applied as `Skin` to head, arms, legs and `Shirt` to torso. Tints are multiplied over the grey-white art.
- `RigFactory.CreatePlayer(Transform parent, CharacterLook look, float height)` and `RigFactory.CreateEva(Transform parent, float height)` return `CharacterRig`. `CharacterRig`: `void ApplyLook(CharacterLook look)` (player only), `void Wave()`, `void Cheer()`, `void SetTalking(bool talking)`. `Wave` and `Cheer` ignore calls while a Wave or Cheer state is playing or in transition (`IsInTransition(0)`), so mashing cannot queue repeats.
- Clips (generated by `EvaRigAssets.Generate()` with `AnimationClip.SetCurve`; property names for a `RectTransform` hierarchy: `localEulerAnglesRaw.z` for rotation and `m_AnchoredPosition.y` for the bob; the implementer must confirm the names by playing the clips in a test scene and reading values back, and record them): `Idle` (loop, 2 s, torso bob 6 units, gentle head sway 2 degrees), `Talk` (loop, 0.5 s bob and head nod), `Wave` (0.9 s, right arm raises to 150 degrees, swings 3 times at 0.15 s each way and returns to its rest angle), `Cheer` (1.0 s, both arms up and a small torso hop). Controller: default state `Idle`; bool `Talking` (`Idle` to `Talk` and back); triggers `Wave` and `Cheer` from Any State to their states, exit time 1 back to `Idle`. Write Defaults is left on and this is noted; the arm rest angle in Wave's first and last keys equals the arm's rest angle in the hierarchy.
- Player height on screen: 520 units on the Creator, 420 elsewhere; Eva 560.

- [ ] **Step 1: Failing test `RigTests`:** `RigFactory.CreatePlayer` builds exactly the hierarchy and names above; arms have pivot `(0.5, 1)`; `ApplyLook` tints torso with `Palette.Shirt[look.Shirt]` and the other parts with `Palette.Skin[look.Skin]`; every clip binds only to paths that exist in the hierarchy (load the controller, read each clip's bindings, assert each path resolves under a freshly built rig, for both Eva and the player).
- [ ] **Step 2: Author the character SVGs, build art, generate the controller and clips (`EvaRigAssets.Generate()` run once via `-executeMethod`), implement `CharacterRig`, `RigFactory`, `Palette`.**
- [ ] **Step 3: Put Eva on the Map** (right side, 560 units tall, `Idle`). Tapping her plays `Wave`.
- [ ] **Step 4: Run the suite, build, install. Ask the user to look at the phone:** Eva idles smoothly (no jitter), tapping her waves and returns to idle, parts stay attached at every angle (no detached arm), nothing overlaps wrongly. Read the fps digits. If the rig fails on `RectTransform`s after two attempts, apply the fallback and report.
- [ ] **Step 5: Delete screenshots; report** the confirmed property names, any pivot or sorting gotchas.

---

### Task 7: Count the Objects, the core round (playable)

**Files:**
- Modify: `Screens/CountScreen.cs` (replaces the placeholder), `Ui/Hud.cs` (coin counter animation)
- Create: `Assets/Eva/App/Screens/CountLayout.cs`, `Assets/Eva/Tests/CountLayoutTests.cs`

**Interfaces:**
- Consumes: `CountRoundGenerator`, `CountObject`, `HelpLadder`, `CoinPayout`, `EvaGame.Progress`, `Commit()`, `Voice`, `Sfx`, `CharacterRig` (Eva), `EvaUi`.
- Produces: `CountLayout.Positions(int quantity)` returning `Vector2[]` of object centres in canvas units (origin at the screen centre, inside the object field x -620..280, y -40..340), non-overlapping (centres at least 220 apart); exact table (x, y):
  - 1: (-170, 150)
  - 2: (-320, 150), (-20, 150)
  - 3: (-470, 150), (-170, 150), (130, 150)
  - 4: (-470, 270), (-170, 270), (-320, 60), (-20, 60)
  - 5: (-470, 270), (-170, 270), (130, 270), (-320, 60), (-20, 60)

Screen layout (canvas units, origin centre): Eva 560 tall at (520, -120) facing left; object field as above with object sprites 200 wide but hit area 240; three answer tiles `icons/tile` 280 x 280 at (-430, -290), (-140, -290), (150, -290), each showing the numeral (TMP, 140 pt, dark) with that many `icons/dot` dots (60 units) arranged like a die under it; home button and coin counter from the Hud.

Behaviour (a round):
1. Start: pick a `CountRound` (`roundIndex`, `System.Random`, previous object); place objects with a small pop-in; `Voice.Say("count_q_<object>")` while Eva `SetTalking(true)`; answers enabled after the line ends.
2. Tapping an object (optional counting aid, backed by a `CountTally` per round): the first tap on an object not yet counted gives it a soft glow and a small "counted" mark (a soft green tick badge on the object, no letters) and `Voice.Say("num_<k>")`, where k is the tally's running number. Tapping an object that is already counted only gives a tiny bounce and no voice: it never advances or changes the spoken count, and the count never wraps. When all objects are counted nothing further happens (the child still has to pick the answer tile). No effect on scoring.
3. Tapping an answer tile: correct answer means `Sfx.Right()`, Eva `Cheer()`, `Voice.Say("count_right_<1..3 cycling>")` then `num_<Quantity>`, pay `CoinPayout.ForStep(ladder.Step)` coins (`Progress.AddCoins`, `Commit()`, coin counter counts up one coin at a time with `Sfx.Coin()` per coin), then the next round after the speech ends. A wrong tile means `ladder.RecordMistake()`; step `Retry`: the tile wobbles (rotation shake 0.4 s), `Sfx.Retry()`, `Voice.Say("count_retry")`, the child may tap again (a tile already tried stays dimmed). Steps `Hint` and `Demonstrate` are added in Task 8; until then any further wrong tap behaves like `Retry`.
4. After round 5: `Voice.Say("count_done")`, Eva cheers, two icon buttons appear: replay (`icons/replay`, starts a new session) and home (`icons/home`, `Navigator.Show(Map)`), each 260 units, at (-150, -290) and (150, -290); `Voice.Say("count_again")` follows. `Progress.Advance(RoundsFinished)` is called once per finished session.
5. Coins are saved after each round, so quitting mid-session keeps them.

- [ ] **Step 1: Failing test `CountLayoutTests`:** for quantity 1..5 the count of positions equals quantity; all positions are at least 220 apart; every position lies inside the object field.
- [ ] **Step 2: Implement `CountLayout`, `CountScreen` (core round, retry only, end of session).** Add the screen to the audit test list; audit must pass (digits only, tap targets at least 240).
- [ ] **Step 3: Run the suite, build, install. Ask the user to play two full sessions on the phone.** Check: object taps count aloud (text bubble fallback if no voice yet), a wrong tile wobbles softly and can be retried, a right tile pays and Eva cheers, session end shows the two icon buttons, coins survive `am force-stop` plus relaunch (`adb shell am force-stop com.noadsguy.evas.dev` then `adb shell monkey -p com.noadsguy.evas.dev -c android.intent.category.LAUNCHER 1`, then look at the coin digit). Read the fps digits during a round.
- [ ] **Step 4: Delete screenshots; report.**

---

### Task 8: Count the Objects, Eva's help ladder (hint and demonstration)

**Files:**
- Modify: `Screens/CountScreen.cs`
- Create: `Ui/PointerHand.cs`, `Assets/Eva/Tests/PointerHandTests.cs` (only the pure timing helper is tested)

**Interfaces:**
- `PointerHand`: `IEnumerator MoveTo(Vector2 target, float seconds)`, `IEnumerator Tap(float seconds)` (a small press animation), `void Pulse(bool on)`, `void Hide()`; the hand sprite is `icons/hand`, 200 units, non-interactive (`raycastTarget = false`, no `TapTarget`). Pure helper `PointerHand.EaseInOut(float t)` (0 to 1 to 0..1 smoothstep) is unit-tested (0 -> 0, 1 -> 1, 0.5 -> 0.5, monotone).
- Behaviour added to the wrong-answer branch, driven by `HelpLadder`:
  - `Retry` (1st mistake): as in Task 7.
  - `Hint` (2nd mistake): tiles are disabled; `Voice.Say("count_hint")`; the hand goes to each object in order, tapping it, while `num_<k>` plays for each; after the last, it rests on the last object for 0.5 s; then the tiles are enabled again (tiles already tried stay dimmed; the hint never marks the right one).
  - `Demonstrate` (3rd mistake): `Voice.Say("count_demo")`; the child counts the objects with a fresh `CountTally`, guided: the objects not yet counted pulse and the hand points at the next one in reading order; tapping any not-yet-counted object counts it (`num_<k>` from the tally, the "counted" mark appears); tapping an already counted object or an empty place does nothing. When the tally is complete, `Voice.Say("count_demo_answer")`, the correct tile pulses and the hand points at it, the other tiles are disabled, and only the correct tile accepts a tap (which then pays the `Demonstrated` tier). The round cannot complete without the child doing this. Total time is not limited.
  - Intro (first ever session, `!Progress.CountIntroSeen`): before round 1 the hand and Eva run the Hint routine on the first round's objects with `count_intro` instead of `count_hint`, answers disabled during it; then `CountIntroSeen = true`, `Commit()`.
- The presenter implements its own hint and demonstration (spec 4.6); nothing here is generic.

- [ ] **Step 1: Failing test for `EaseInOut`, then implement it.**
- [ ] **Step 2: Implement `PointerHand` and the three help behaviours.** All steps are coroutines that check the screen is still shown (leaving the screen mid-help cancels cleanly, no stuck disabled tiles on return).
- [ ] **Step 3: Extend the audit test** to build the Count screen in each state it can be in (question, hint, demonstration, session end) and re-run it.
- [ ] **Step 4: Run the suite, build, install. Ask the user to test the ladder deliberately:** make three wrong taps in a round. Expected: gentle wobble and "try again"; then Eva and the hand count the objects aloud and the child taps a tile again; on the third, the child taps each object with Eva, then taps the highlighted number, and the round pays 1 coin (check the counter). Then play clean rounds (3 coins). Report what the user observed for each step and any confusion.

---

### Task 9: Character creator and the first-run flow

**Files:**
- Create: `Screens/CreatorScreen.cs`
- Modify: `EvaGame.cs` (initial screen depends on `Progress.HasCharacter`), `Screens/MapScreen.cs` (the player character stands next to Eva)
- Test: extend `NoReadingAuditTests` with the Creator screen

Behaviour: `Progress.HasCharacter == false` shows the Creator, otherwise the Map. Layout: the player preview (520 units, Idle) on the left; on the right three rows, all icon-only, each choice at least 240 units: row 1 four head icons (`char_head_0..3` shown as faces), row 2 five skin swatches (round, `Palette.Skin`), row 3 five shirt swatches (round, `Palette.Shirt`); the selected choice has a bright ring and the preview updates immediately with a small hop (`Cheer` is not used; a scale bounce). A big green `icons/check` (260 units) bottom-right confirms: `Progress.HasCharacter = true`, `Look` saved, `Advance(LookConfirmed)`, `Commit()`, `Navigator.Show(Map)`. Voice: `create_head` on entry, `create_color` after the first head tap, `create_done` after the first colour tap. The 4 + 5 + 5 choices plus the check must fit in the 1440 x 900 frame at 240-unit hit areas; if they do not, swatches use a two-row wrap; overlapping hit areas are not allowed (the audit checks it: no two `TapTarget` rects on one screen overlap by more than 20 units).

- [ ] **Step 1: Extend the audit test** with the overlap rule; implement `CreatorScreen`; build `RigFactory.CreatePlayer` into the Map next to Eva (420 units).
- [ ] **Step 2: Run the suite, build, `adb shell pm clear com.noadsguy.evas.dev`, install.** Ask the user to create a character on the phone: choices respond immediately, the preview matches, confirming lands on the Map with the character visible, and after force-stop plus relaunch the same character appears on the Map (and the Creator does not reappear).
- [ ] **Step 3: Delete screenshots; report.**

---

### Task 10: The house (drag furniture into slots)

**Files:**
- Create: art `art/eva/world/house_bg.svg` (a cutaway of two rooms side by side, living room left and bedroom right, walls, floor, window; 1920 x 900), furniture SVGs `art/eva/objects/{sofa,rug,table,lamp,plant,bed,bookshelf}.svg` (512 px), `Screens/HouseScreen.cs` (replaces the placeholder), `Ui/DragItem.cs`
- Modify: `tools/build-eva-art.sh` (already covers `objects`, `world`), `Assets/Eva/Tests/NoReadingAuditTests.cs`

Layout (canvas units, origin centre): the cutaway fills the screen; slot targets (dashed soft outlines, hit and snap area 260 x 260 each) at fixed positions, the same positions on every device: `living_seat` (-480, -60), `living_floor` (-400, -290), `living_table` (-170, -120), `living_corner` (-620, 120), `bedroom_bed` (440, -100), `bedroom_corner` (200, 100), `bedroom_wall` (620, 150). Owned but unplaced items sit in a tray along the bottom edge (each 240 units, centred, spaced 260 apart, left to right in ownership order). Placed items are drawn in their slots at 240 to 300 units.

Starter: the first time the House opens with nothing owned (`Progress.Owned` empty), it grants the starter (`Progress.GrantStarter()`, `Commit()`), so the sofa appears in the tray (spec 4.1 step 2). Task 12 only adds Eva's guidance on top.

Behaviour: dragging an item (`DragItem`, `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`, with the `TapTarget` marker) shows the outlines of every valid empty slot for its kind (all matching-kind slots, glowing). Dropping within 260 units of a valid empty slot snaps into it (`HouseLayout.TryPlace`, `Sfx.Place()`, `Advance(ItemPlaced)`, `Commit()`, Eva line `house_placed` the first time in a session); dropping elsewhere returns the item to where it came from with a small ease. Placed items can be dragged again to another valid slot or back to the tray (`HouseLayout.Remove`). A slot is never left half-filled and the child cannot get stuck. Entering the House with an unplaced item in the tray: nothing extra (Task 12 adds guidance).

- [ ] **Step 1: Author SVGs, build art, implement `DragItem` and `HouseScreen`; extend the audit test for the House with items in the tray and placed.**
- [ ] **Step 2: Run the suite, build, `adb shell pm clear com.noadsguy.evas.dev`, install.** Ask the user to create a character, open the House on the phone, and drag the sofa into the living-room seat, then out to the tray and back, and try dropping it in the wrong place. Expected: valid slots glow while dragging, the drop snaps, a wrong drop returns the sofa smoothly, and after force-stop plus relaunch the sofa is still placed.
- [ ] **Step 3: Delete screenshots; report** (no commit).

---

### Task 11: The store (buy furniture)

**Files:**
- Create: art `art/eva/world/store_bg.svg` (a shop interior with shelves, 1920 x 900), `Screens/StoreScreen.cs` (replaces the placeholder)
- Modify: `Assets/Eva/Tests/NoReadingAuditTests.cs`

Layout: `store_bg`; the six purchasable items (every catalog item except the free starter) on two shelves, each 240 to 280 units with its price under it as coin icons plus a digit (`icons/coin` 60 units and a TMP numeral); owned items show a green check instead of the price and are dimmed. Coin counter in the Hud.

Behaviour: `Voice.Say("store_welcome")` on entry (first entry per session only; `Advance(EnteredStore)` every entry). Tap an item: the item lifts and grows, Eva says `store_buy_q`, and two icon buttons appear beside it: `icons/check` (buy) and `icons/cross` (cancel), 260 units each. Buy: `Progress.TryBuy(id)`: `Bought` means `Sfx.Buy()`, coins deducted with the coin counter counting down, `Voice.Say("store_bought")`, the item joins the tray (`Commit()`), `Advance(ItemBought)`, then the shelf shows it owned. `NotEnoughCoins`: a soft wobble, `Sfx.Retry()`, `Voice.Say("store_not_enough")`, no state change. `AlreadyOwned`: `Voice.Say("store_owned")`. Cancel closes the pair. No purchase happens without the child tapping the green check. The free starter sofa is not on the shelves.

- [ ] **Step 1: Author the SVG, implement `StoreScreen`, extend the audit test (shelves in every state: affordable, not affordable, owned).**
- [ ] **Step 2: Run the suite, build, install (the save now has coins from Task 7 play).** Ask the user to buy something with earned coins on the phone, try one too expensive, cancel one, then open the House, see the new item in the tray and drag it into its slot; also drag it out and back. Expected: coin digit changes exactly by the price, a too-expensive item is refused softly, the drag snaps and the item stays after force-stop plus relaunch. Report what the user saw (this also completes the on-device drag check for Task 10).

---

### Task 12: Tutorial guidance (Eva shows the way)

**Files:**
- Create: `Screens/TutorialGuide.cs`
- Modify: `Screens/MapScreen.cs`, `HouseScreen.cs`, `StoreScreen.cs`, `CountScreen.cs` (call the guide on `OnShow`), `EvaGame.cs`

`TutorialGuide.Refresh(ScreenId screen)` runs on every screen show and after every state change. It looks only at `Progress.Tutorial`, says the step's line once per entry to the step (Eva talking, `Voice.Say`), and points with `PointerHand`. It never blocks input and never disables anything: every place stays accessible, and the child can ignore Eva. Table:

| Step | Where | Line | Pointing |
|---|---|---|---|
| CreateCharacter | Creator | (Task 9 lines) | none |
| PlaceStarter | House | `house_welcome` (the starter was granted by Task 10) | the hand drags from the sofa to `living_seat`, repeating every 6 s until the child drags |
| PlaceStarter | Map | `map_house` | pulses on the house building |
| GoToSchool | Map | `map_school` | pulses on the school building |
| GoToStore | Map | `map_store` | pulses on the store building |
| FirstPurchase | Store | `store_welcome` (`store_buy_q` is spoken by the item tap in Task 11) | pulses on the cheapest affordable item (`rug`) |
| PlacePurchase | Map | `map_house` | pulses on the house building |
| PlacePurchase | House | `house_new` | drags from the tray item to a valid slot |
| Done (once) | House | `tut_done` | none |

After `Done` the map plays `map_welcome` on the first Map entry per app start, nothing else.

- [ ] **Step 1: Failing tests** for the pure decision: `TutorialGuide.Plan(TutorialStep step, ScreenId screen)` returns `(string voiceKey, GuideTarget target)` per the table (use an enum `GuideTarget { None, HouseBuilding, SchoolBuilding, StoreBuilding, StarterToLivingSeat, TrayToSlot, CheapestItem }`); every table row and a default of `(null, None)` are tested.
- [ ] **Step 2: Implement `Plan` and the guide's coroutines; wire the screens.** Extend the audit test for the guide's hand (non-interactive) and confirm no new `TapTarget`.
- [ ] **Step 3: Run the suite, build, `adb shell pm clear com.noadsguy.evas.dev`, install.** Ask the user to play the whole first run from a cleared save without speaking and to note every moment they were unsure what to do or which way to go: expected the loop completes (character, sofa placed, school, five rounds, store, purchase, placement). Report the notes verbatim.

---

### Task 13: Acceptance, fixes and the M1 summary

**Files:**
- Create: `docs/superpowers/spikes/M1-summary.md`
- Modify: whatever the run below fails

- [ ] **Step 1: Cold verification (background, one at a time):** repo-root suite (expect 187), Eva suite (all green, count reported), `bash tools/build-eva-debug.sh`, `bash tools/check-apk-compliance.sh ...` (expect `OK: no AD_ID`, launchable activity, only the expected permissions), `git status` shows no unexpected large files, and no file under `Assets/Spikes` is in the build scene list.
- [ ] **Step 2: Voice must exist.** Every key in `voice-lines.txt` has a clip in `Resources/Voice/en` (add a test asserting this now if it is not there). If Task 4's first run was never done, stop and ask the user for the API key setup.
- [ ] **Step 3: On-device acceptance run with the user (and a child if one is available).** Clear the save, install, and run A1 to A7 from the acceptance section: the full first run without help, a mistakes test (three wrong taps), a first-session payout check (demonstrate every round: the coins earned must be at least the price of the cheapest item), force-stop and relaunch at three different points (after the creator, after buying, after placing; corrupt-save handling is covered by the automated `SaveStoreTests`), and a fps reading in the Count round (110 or more). Record each criterion as pass, fail or not observed, with what was seen and by whom.
- [ ] **Step 4: Fix every failed criterion that is a bug in this plan's scope** (one focused subagent per failure; anything else goes to the summary as an M2 input).
- [ ] **Step 5: Write `M1-summary.md`:** one line per task with verdict, the acceptance table (A1 to A7), the measured fps, APK size, what the user or child hesitated at, the list of M2 inputs, and the deferrals below. Then ask the user to judge A7 (is it fun? what should change) and whether to start Plan 3 (M2 polish and the first tutorial skip, difficulty ladder, tuning). Ask whether to commit (suggested split: rules and save; art and characters; screens; voice generation script and lines).

---

## Deferred out of M1 (and why)

- **Difficulty ladder and rolling window** (spec 4.7): the slice uses a fixed in-session ramp (`MaxQuantityByRound`). Level-up/down logic is M2.
- **Tutorial skip control, "time for a break", Settings, parent gate, volume controls, parent progress:** not required for the loop; the spec defers them to M2 and later. Clear a save with `pm clear` while testing.
- **Paying in the Store by tapping coins** (spec 4.3): the slice uses a check/cross confirmation; the coin-tapping mechanic belongs with the Store shopping game.
- **Furniture set id and category** in the catalog, more rooms, clothing, pets: data can be extended when a second collection exists.
- **The generic activity engine, content schema and validator, offline AI content generation:** built when the second mode exists.
- **Text for other languages, TMP font work, CJK fonts, voice packs, Play asset delivery:** M4.
- **Real art, final Eva design, music, real sound effects:** M2 onward (the slice uses consistent hand-written SVG art and procedural tones).
- **Battery optimisation, tablet layout check, low-end device check, Auto Backup on a Play install:** M5.
- **Release keystore, package id, store listing:** M6.

## Risks and how the plan handles them

- **Rig on `RectTransform`s is untested (M0 proved `SpriteRenderer`s).** Task 6 is the gate, with a stated fallback after two attempts.
- **Voice generation needs the user's Google Cloud key.** Task 4 is isolated and can be run any time; missing clips do not stop development and the acceptance gate (Task 13) requires them.
- **Layout on other aspect ratios.** All important content lives in a central 1440 x 900 frame plus `SafeAreaPanel`; the real tablet check is M5.
- **Touch feel is only knowable on the phone.** Every UI task ends with a device step where the user taps; the plan never guesses coordinates.
- **The slice grows.** Anything not needed for the loop in the acceptance criteria goes to the deferred list, not into a task.

## Self-review

- **Spec coverage:** creator (4.1.1), house and starter furniture (4.1.2), School and Count the Objects with in-play intro (4.1.3), coin guarantee (4.1.4, A4 and the `MinSessionPayout >= CheapestPrice` test), Store purchase and placement (4.1.5), map with House, School, Store and no locked places (4.2), help ladder that ends with the child acting (4.6), coins per round falling with help but never zero (4.5), voice-first with hidden text (4.9), touch sizes and no timers (4.10), cutout characters (6), 120 fps (6), voice pipeline (7), save and restore (8.5). Skips, gates, settings, ladder are deferred with reasons.
- **Placeholder scan:** every task names files, exact values and checks; the two device-only steps (Tasks 6 to 12) name what to observe. Task 3's tests and Task 6's controller are described precisely rather than as full code, because the implementer needs the values, not a transcription; the rules code for Task 2 is complete.
- **Type consistency:** `HelpStep`, `HelpLadder`, `CoinPayout`, `CountRound`, `CountObject`, `PlayerProgress`, `CharacterLook`, `HouseLayout`, `FurnitureCatalog`, `TutorialStep`, `TutorialEvent`, `TutorialFlow`, `ScreenId`, `Voice`, `Sfx`, `PointerHand`, `EvaUi.MinTap` are used with the same names across tasks. Voice keys used in code appear in Task 4's list (`count_q_<object>` lower-case object names match the enum names `apple`, `star`, `duck`, `flower`; `num_1` to `num_5`; `count_right_1` to `count_right_3`).
