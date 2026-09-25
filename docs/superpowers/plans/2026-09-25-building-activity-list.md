# Building Activity List Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tapping School on the Map opens a reusable "building activity list" (big picture tiles, tap = play) instead of jumping straight into the Count game, so more games and other buildings can reuse the same screen.

**Architecture:** A pure Rules catalogue (`Activities`) plus a pure tile-layout function (`TileLayout`) feed one generic `BuildingScreen(BuildingId)` in the App layer. `ScreenId.School` maps to `BuildingScreen(School)`; the Count game gets its own `ScreenId.Count`. The tutorial step `GoToSchool` now advances when the list opens and `FirstGame` points at the first tile.

**Tech Stack:** Unity 6000.5.10f1, C#, uGUI, NUnit edit-mode tests (`EvasLearningWorld/Assets/Eva/Tests`), node + sharp for one art script.

**Spec:** `docs/superpowers/specs/2026-09-25-building-activity-list-design.md` (read it first; the plan implements it).

## Global Constraints

- Every child-facing tap target is at least `EvaUi.MinTap` = 240 canvas units; tiles never overlap; no text except digits on screens (no-reading audit).
- Design frame 1440 x 900 canvas units, centre origin; screens live under a safe-area `Root` (`ScreenRoot/<TypeName>`).
- Tile content area: x -600..600, y -380..170 (clear of the Hud Home button top-left and the coin counter top-right).
- Rules assembly (`Assets/Eva/Rules`) is engine-free (`noEngineReferences`): no `UnityEngine` types there.
- No activity progress, no locks, no "coming soon" tiles, no categories, no swipe; at most 8 activities per building.
- Tapping a tile starts the Eva voice line and opens the activity in the same call; never wait for the line.
- Never touch `ProjectSettings/AndroidResolverDependencies.xml` (an unrelated Sudoku change). Only `git add` explicit paths.
- Commit messages end with `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>`.
- Test runner (bounded, about 2 min): `cd C:/Users/schei/mobile-games-framework && source tools/env.sh && timeout 580 bash tools/run-editmode-tests.sh "C:/Users/schei/mobile-games-framework/EvasLearningWorld" 2>&1 | tail -1 | cut -c1-150`. Failures: parse `EvasLearningWorld/Logs/editmode-results.xml`; compile errors: `grep -h "error CS" EvasLearningWorld/Logs/*.log | sort -u` (a stale `Assets\Spikes\Pad\PadSpike.cs(51,18)` CS0029 line is always there, ignore). Never run two Unity instances, never edit sources while Unity runs. Baseline: 251 tests pass.

New C# test files need a `.meta` (Unity generates it on the first run; commit it with the file).

---

## File structure

- Create `EvasLearningWorld/Assets/Eva/Rules/TileLayout.cs`: tile rectangles for N tiles (pure).
- Create `EvasLearningWorld/Assets/Eva/Rules/Activities.cs`: `BuildingId`, `Activity`, the catalogue (pure).
- Create `EvasLearningWorld/Assets/Eva/App/Screens/BuildingScreen.cs`: the generic list screen.
- Modify `App/Screens/Navigator.cs` (add `ScreenId.Count`), `App/EvaGame.cs` (registrations), `App/Screens/CountScreen.cs` (OnShow and session-home), `App/Screens/TutorialGuide.cs` (row + target).
- Modify `Resources/Voice/voice-lines.txt` (two lines); create `Resources/Art/activities/count.png` and `Resources/Art/world/school_list_bg.png` (the user's art, imported by `tools/art-import/import-activity-art.js`).
- Tests: create `Tests/TileLayoutTests.cs`, `Tests/ActivitiesTests.cs`, `Tests/BuildingScreenTests.cs`; modify `Tests/CountScreenHelpTests.cs`, `Tests/NoReadingAuditTests.cs`, `Tests/TutorialGuideTests.cs`, `Tests/ArtTests.cs`.

---

### Task 1: Rules: `TileLayout` and `Activities`

**Files:**
- Create: `EvasLearningWorld/Assets/Eva/Rules/TileLayout.cs`, `EvasLearningWorld/Assets/Eva/Rules/Activities.cs`
- Test: `EvasLearningWorld/Assets/Eva/Tests/TileLayoutTests.cs`, `EvasLearningWorld/Assets/Eva/Tests/ActivitiesTests.cs`

**Interfaces:**
- Produces:
  - `readonly struct TileRect { float X, Y, Side; float XMin, XMax, YMin, YMax }` (X, Y = centre).
  - `static class TileLayout { const float AreaXMin=-600f, AreaXMax=600f, AreaYMin=-380f, AreaYMax=170f, Gap=30f; static TileRect[] Compute(int count) }` (count 1..8, else `ArgumentOutOfRangeException`; row-major, top row first, last row centred).
  - `enum BuildingId { School }`; `sealed class Activity { string Id; BuildingId Building; string ScreenKey; string IconSprite; string VoiceKey }`; `static class Activities { IReadOnlyList<Activity> For(BuildingId); IReadOnlyList<Activity> Filter(BuildingId, IReadOnlyList<Activity> source) }`.

- [ ] **Step 1: Write the failing tests**

`Tests/TileLayoutTests.cs`:

```csharp
using System;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class TileLayoutTests
    {
        [TestCase(1, 480f)]
        [TestCase(2, 440f)]
        [TestCase(3, 380f)]
        [TestCase(4, 270f)]
        [TestCase(5, 250f)]
        [TestCase(8, 250f)]
        public void TilesHaveTheDocumentedSide(int count, float side)
        {
            foreach (var tile in TileLayout.Compute(count)) Assert.That(tile.Side, Is.EqualTo(side));
        }

        [Test]
        public void EveryCountFromOneToEightFitsTheAreaWithoutOverlapAndAtLeastMinTap()
        {
            for (var count = 1; count <= 8; count++)
            {
                var tiles = TileLayout.Compute(count);
                Assert.That(tiles.Length, Is.EqualTo(count));
                for (var i = 0; i < count; i++)
                {
                    Assert.That(tiles[i].Side, Is.GreaterThanOrEqualTo(240f), "side, count " + count);
                    Assert.That(tiles[i].XMin, Is.GreaterThanOrEqualTo(TileLayout.AreaXMin), "xmin, count " + count);
                    Assert.That(tiles[i].XMax, Is.LessThanOrEqualTo(TileLayout.AreaXMax), "xmax, count " + count);
                    Assert.That(tiles[i].YMin, Is.GreaterThanOrEqualTo(TileLayout.AreaYMin), "ymin, count " + count);
                    Assert.That(tiles[i].YMax, Is.LessThanOrEqualTo(TileLayout.AreaYMax), "ymax, count " + count);
                    for (var j = i + 1; j < count; j++)
                    {
                        var apart = tiles[i].XMax <= tiles[j].XMin || tiles[j].XMax <= tiles[i].XMin
                            || tiles[i].YMax <= tiles[j].YMin || tiles[j].YMax <= tiles[i].YMin;
                        Assert.IsTrue(apart, "tiles " + i + " and " + j + " overlap, count " + count);
                    }
                }
            }
        }

        [Test]
        public void ASingleTileIsCentredInTheAreaAndTheLargest()
        {
            var tile = TileLayout.Compute(1)[0];
            Assert.That(tile.X, Is.EqualTo(0f).Within(0.01f));
            Assert.That(tile.Y, Is.EqualTo((TileLayout.AreaYMin + TileLayout.AreaYMax) / 2f).Within(0.01f));
            Assert.That(tile.Side, Is.EqualTo(480f));
        }

        [Test]
        public void TilesRunLeftToRightThenTopToBottomAndTheLastRowIsCentred()
        {
            var five = TileLayout.Compute(5);
            Assert.That(five[0].X, Is.LessThan(five[1].X));
            Assert.That(five[4].Y, Is.LessThan(five[0].Y), "second row is below the first");
            Assert.That(five[4].X, Is.EqualTo(0f).Within(0.01f), "a single tile in the last row is centred");
        }

        [TestCase(0)]
        [TestCase(9)]
        public void UnsupportedCountsThrow(int count) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => TileLayout.Compute(count));
    }
}
```

`Tests/ActivitiesTests.cs`:

```csharp
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class ActivitiesTests
    {
        [Test]
        public void SchoolHasCountFirstAndEveryEntryIsComplete()
        {
            var list = Activities.For(BuildingId.School);
            Assert.That(list.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(list[0].Id, Is.EqualTo("count"));
            var ids = new HashSet<string>();
            foreach (var activity in list)
            {
                Assert.IsTrue(ids.Add(activity.Id), "duplicate id " + activity.Id);
                Assert.That(activity.Building, Is.EqualTo(BuildingId.School));
                Assert.IsNotEmpty(activity.ScreenKey);
                Assert.IsNotEmpty(activity.IconSprite);
                Assert.IsNotEmpty(activity.VoiceKey);
            }
            Assert.That(list.Count, Is.LessThanOrEqualTo(8));
        }

        [Test]
        public void ForReturnsTheSameOrderEveryTime()
        {
            var first = Activities.For(BuildingId.School);
            var second = Activities.For(BuildingId.School);
            Assert.That(second.Count, Is.EqualTo(first.Count));
            for (var i = 0; i < first.Count; i++) Assert.That(second[i].Id, Is.EqualTo(first[i].Id));
        }

        // The catalogue holds one School entry today, so the ordering rule is proven on a synthetic list: entries
        // keep their declared order and only the asked building's entries come back.
        [Test]
        public void FilterKeepsTheDeclaredOrder()
        {
            var source = new List<Activity>
            {
                new Activity("c", BuildingId.School, "Count", "a/c", "v_c"),
                new Activity("a", BuildingId.School, "Count", "a/a", "v_a"),
                new Activity("b", BuildingId.School, "Count", "a/b", "v_b"),
            };
            var result = Activities.Filter(BuildingId.School, source);
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result[0].Id, Is.EqualTo("c"));
            Assert.That(result[1].Id, Is.EqualTo("a"));
            Assert.That(result[2].Id, Is.EqualTo("b"));
        }
    }
}
```

- [ ] **Step 2: Run the suite to verify it fails**

Run the test runner command from Global Constraints. Expected: compile errors `TileLayout`/`Activities`/`BuildingId` do not exist.

- [ ] **Step 3: Write `Rules/TileLayout.cs`**

```csharp
using System;

namespace EvasLearningWorld.Rules
{
    // A tile's centre and side in canvas units (centre origin, y up).
    public readonly struct TileRect
    {
        public TileRect(float x, float y, float side) { X = x; Y = y; Side = side; }
        public float X { get; }
        public float Y { get; }
        public float Side { get; }
        public float XMin => X - Side / 2f;
        public float XMax => X + Side / 2f;
        public float YMin => Y - Side / 2f;
        public float YMax => Y + Side / 2f;
    }

    // Where the tiles of a building's activity list go. One activity gets one large tile; more get a grid of up to
    // four columns and two rows, centred, inside the area that stays clear of the Hud Home button (top-left) and the
    // coin counter (top-right).
    public static class TileLayout
    {
        public const float AreaXMin = -600f, AreaXMax = 600f, AreaYMin = -380f, AreaYMax = 170f, Gap = 30f;
        public const int MaxTiles = 8;

        public static TileRect[] Compute(int count)
        {
            if (count < 1 || count > MaxTiles) throw new ArgumentOutOfRangeException(nameof(count));
            var side = SideFor(count);
            var rows = count <= 4 ? 1 : 2;
            var centreY = (AreaYMin + AreaYMax) / 2f;
            var tiles = new TileRect[count];
            for (var i = 0; i < count; i++)
            {
                var row = i / 4;
                var inRow = row == 0 ? Math.Min(count, 4) : count - 4;
                var x = (i % 4 - (inRow - 1) / 2f) * (side + Gap);
                var y = centreY + ((rows - 1) / 2f - row) * (side + Gap);
                tiles[i] = new TileRect(x, y, side);
            }
            return tiles;
        }

        private static float SideFor(int count)
        {
            switch (count)
            {
                case 1: return 480f;
                case 2: return 440f;
                case 3: return 380f;
                case 4: return 270f;
                default: return 250f;
            }
        }
    }
}
```

- [ ] **Step 4: Write `Rules/Activities.cs`**

```csharp
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public enum BuildingId { School }

    // One thing a child can play in a building. ScreenKey is the name of the App layer's ScreenId (a string so this
    // assembly stays engine- and App-free).
    public sealed class Activity
    {
        public Activity(string id, BuildingId building, string screenKey, string iconSprite, string voiceKey)
        {
            Id = id;
            Building = building;
            ScreenKey = screenKey;
            IconSprite = iconSprite;
            VoiceKey = voiceKey;
        }

        public string Id { get; }
        public BuildingId Building { get; }
        public string ScreenKey { get; }
        public string IconSprite { get; }
        public string VoiceKey { get; }
    }

    // The activities of each building, in the fixed order they are shown. No locks and no progress: every entry that
    // exists is playable.
    public static class Activities
    {
        private static readonly Activity[] All =
        {
            new Activity("count", BuildingId.School, "Count", "activities/count", "activity_count"),
        };

        public static IReadOnlyList<Activity> For(BuildingId building) => Filter(building, All);

        public static IReadOnlyList<Activity> Filter(BuildingId building, IReadOnlyList<Activity> source)
        {
            var result = new List<Activity>();
            foreach (var activity in source)
                if (activity.Building == building) result.Add(activity);
            return result;
        }
    }
}
```

- [ ] **Step 5: Run the suite, verify pass**

Expected: all tests pass (251 + the new ones).

- [ ] **Step 6: Commit**

```bash
git add EvasLearningWorld/Assets/Eva/Rules/TileLayout.cs EvasLearningWorld/Assets/Eva/Rules/TileLayout.cs.meta EvasLearningWorld/Assets/Eva/Rules/Activities.cs EvasLearningWorld/Assets/Eva/Rules/Activities.cs.meta EvasLearningWorld/Assets/Eva/Tests/TileLayoutTests.cs EvasLearningWorld/Assets/Eva/Tests/TileLayoutTests.cs.meta EvasLearningWorld/Assets/Eva/Tests/ActivitiesTests.cs EvasLearningWorld/Assets/Eva/Tests/ActivitiesTests.cs.meta
git commit -m "feat: activity catalogue and tile layout (rules)"
```

---

### Task 2: `BuildingScreen`, `ScreenId.Count`, wiring, voice lines

**Files:**
- Create: `App/Screens/BuildingScreen.cs`, `Tests/BuildingScreenTests.cs`
- Modify: `App/Screens/Navigator.cs:6`, `App/EvaGame.cs:44`, `App/Screens/CountScreen.cs` (OnShow around line 113-122; SessionHomeButton around line 638), `Resources/Voice/voice-lines.txt`, `Tests/CountScreenHelpTests.cs` (six `ScreenId.School` -> `ScreenId.Count` at lines 49, 72, 104, 148, 161, 181), `Tests/NoReadingAuditTests.cs` (lines 21, 162, 369)

**Interfaces:**
- Consumes: `Activities.For(BuildingId)`, `TileLayout.Compute(int)`, `TileRect`, `EvaUi.IconButton(Transform, string, Sprite, Vector2 anchor, Vector2 position, float size, UnityAction)`, `EvaUi.Sprite(string)`, `_game.Voice.Say(string)`, `_game.Navigator.Show(ScreenId)`.
- Produces: `enum ScreenId { Creator, Map, House, School, Store, Count }`; `sealed class BuildingScreen : ScreenBase { BuildingScreen(BuildingId building); static Vector2 TilePosition(BuildingId building, int index) }`. Tile GameObjects are named `Tile_<activity id>` directly under the screen root (`ScreenRoot/BuildingScreen/Tile_count`).

- [ ] **Step 1: Write the failing tests**

`Tests/BuildingScreenTests.cs` (the SetUp mirrors `NoReadingAuditTests`, including the explicit 1440 x 900 canvas size so world-space rectangles mean what they do on a device):

```csharp
using System;
using System.Collections.Generic;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.Tests
{
    public class BuildingScreenTests
    {
        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            var gameObject = new GameObject("TestEvaGame");
            _game = gameObject.AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
            _game.Progress.Tutorial = TutorialStep.Done;
        }

        [TearDown]
        public void TearDown()
        {
            if (_game != null) UnityEngine.Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) UnityEngine.Object.DestroyImmediate(_canvasObject);
        }

        private Transform SchoolScreen => _canvasObject.transform.Find("ScreenRoot/BuildingScreen");

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
        }

        private List<RectTransform> Tiles()
        {
            var tiles = new List<RectTransform>();
            foreach (var activity in Activities.For(BuildingId.School))
                tiles.Add((RectTransform)SchoolScreen.Find("Tile_" + activity.Id));
            return tiles;
        }

        [Test]
        public void SchoolShowsOneTileForEachActivityAndEveryScreenKeyIsARegisteredScreen()
        {
            _game.Navigator.Show(ScreenId.School);
            Assert.That(Tiles().Count, Is.EqualTo(Activities.For(BuildingId.School).Count));
            foreach (var activity in Activities.For(BuildingId.School))
            {
                var id = (ScreenId)Enum.Parse(typeof(ScreenId), activity.ScreenKey);
                _game.Navigator.Show(id);
                Assert.AreEqual(id, _game.Navigator.Current, activity.Id);
            }
        }

        [Test]
        public void TilesFollowTheLayoutAndAreAtLeastMinTapAndDoNotOverlap()
        {
            _game.Navigator.Show(ScreenId.School);
            var tiles = Tiles();
            for (var i = 0; i < tiles.Count; i++)
            {
                Assert.That(tiles[i].rect.width, Is.GreaterThanOrEqualTo(EvaUi.MinTap));
                Assert.That(tiles[i].rect.height, Is.GreaterThanOrEqualTo(EvaUi.MinTap));
                Assert.That(tiles[i].anchoredPosition, Is.EqualTo(BuildingScreen.TilePosition(BuildingId.School, i)));
                for (var j = i + 1; j < tiles.Count; j++)
                    Assert.IsFalse(WorldRect(tiles[i]).Overlaps(WorldRect(tiles[j])), "tiles " + i + " and " + j + " overlap");
            }
        }

        [Test]
        public void TilesStayInsideTheSafeAreaAndClearOfTheHomeButtonAndCoinCounter()
        {
            _game.Navigator.Show(ScreenId.School);
            var root = WorldRect((RectTransform)SchoolScreen);
            var hud = _canvasObject.transform.Find("HudRoot");
            var blocked = new List<Rect>
            {
                WorldRect((RectTransform)hud.Find("HomeButton")),
                WorldRect((RectTransform)hud.Find("CoinIcon")),
                WorldRect((RectTransform)hud.Find("CoinCount")),
            };
            foreach (var tile in Tiles())
            {
                var rect = WorldRect(tile);
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(root.xMin));
                Assert.That(rect.xMax, Is.LessThanOrEqualTo(root.xMax));
                Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(root.yMin));
                Assert.That(rect.yMax, Is.LessThanOrEqualTo(root.yMax));
                foreach (var other in blocked) Assert.IsFalse(rect.Overlaps(other), tile.name + " touches a Hud control");
            }
        }

        [Test]
        public void TappingATileStartsTheVoiceLineAndOpensTheActivityImmediately()
        {
            _game.Navigator.Show(ScreenId.School);
            string keyAtFirstSpeech = null;
            _game.Voice.SpeakingChanged += speaking =>
            {
                if (speaking && keyAtFirstSpeech == null) keyAtFirstSpeech = _game.Voice.LastKey;
            };

            SchoolScreen.Find("Tile_count").GetComponent<Button>().onClick.Invoke();

            Assert.AreEqual(ScreenId.Count, _game.Navigator.Current, "the activity opens in the same call");
            Assert.AreEqual("activity_count", keyAtFirstSpeech, "the tile's line is the first thing Eva says");
        }

        [Test]
        public void OpeningTheSchoolListAdvancesTheTutorialFromGoToSchoolToFirstGame()
        {
            _game.Progress.Tutorial = TutorialStep.GoToSchool;
            _game.Navigator.Show(ScreenId.School);
            Assert.AreEqual(TutorialStep.FirstGame, _game.Progress.Tutorial);
        }

        [Test]
        public void OpeningCountDoesNotAdvanceTheTutorial()
        {
            _game.Progress.Tutorial = TutorialStep.GoToSchool;
            _game.Navigator.Show(ScreenId.Count);
            Assert.AreEqual(TutorialStep.GoToSchool, _game.Progress.Tutorial);
        }
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Expected: compile errors (`BuildingScreen`, `ScreenId.Count`).

- [ ] **Step 3: Add `ScreenId.Count`** in `App/Screens/Navigator.cs` line 6:

```csharp
    public enum ScreenId { Creator, Map, House, School, Store, Count }
```

- [ ] **Step 4: Create `App/Screens/BuildingScreen.cs`**

```csharp
using System;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // The inside of a building: its backdrop and one big picture tile per activity (Activities.For). Tap = play: the
    // tile's Eva line starts and the activity opens in the same call, so the child never waits for the voice.
    // Deliberately plain: no progress, no locks, no categories.
    public sealed class BuildingScreen : ScreenBase
    {
        private readonly BuildingId _building;
        private EvaGame _game;

        public BuildingScreen(BuildingId building) => _building = building;

        // Where tile `index` of a building's list is, in the canvas units of every screen's Root (also used by
        // TutorialGuide to point the hand at a tile).
        public static Vector2 TilePosition(BuildingId building, int index)
        {
            var tile = TileLayout.Compute(Activities.For(building).Count)[index];
            return new Vector2(tile.X, tile.Y);
        }

        public override void Build(EvaGame game)
        {
            _game = game;
            AddBackdrop(BackdropFor(_building));
            var activities = Activities.For(_building);
            var tiles = TileLayout.Compute(activities.Count);
            for (var i = 0; i < activities.Count; i++)
            {
                var activity = activities[i];
                var button = EvaUi.IconButton(Root, "Tile_" + activity.Id, EvaUi.Sprite(activity.IconSprite),
                    new Vector2(0.5f, 0.5f), new Vector2(tiles[i].X, tiles[i].Y), tiles[i].Side, () => Open(activity));
                ((RectTransform)button.transform).sizeDelta = new Vector2(tiles[i].Side, tiles[i].Side);
            }
        }

        public override void OnShow()
        {
            if (_building == BuildingId.School)
            {
                _game.Progress.Advance(TutorialEvent.EnteredSchool);
                _game.Commit();
            }
            _game.TutorialGuide.Refresh(ScreenId.School);
        }

        private void Open(Activity activity)
        {
            _game.Voice.Say(activity.VoiceKey);
            _game.Navigator.Show((ScreenId)Enum.Parse(typeof(ScreenId), activity.ScreenKey));
        }

        private static string BackdropFor(BuildingId building)
        {
            switch (building)
            {
                default: return "world/school_list_bg";
            }
        }

        // Fills exactly the safe area (see CountScreen.AddSchoolBackground for why a real picture cannot use the huge
        // solid-colour offsets).
        private void AddBackdrop(string sprite)
        {
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = background.GetComponent<Image>();
            image.sprite = EvaUi.Sprite(sprite);
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
    }
}
```

`OnShow` calls `Refresh(ScreenId.School)` for the School list; when a second building arrives, this line becomes a per-building `ScreenId` field (not needed now).

- [ ] **Step 5: Wire registrations** in `App/EvaGame.cs` (replace line 44):

```csharp
            Navigator.Register(ScreenId.School, new BuildingScreen(BuildingId.School));
            Navigator.Register(ScreenId.Count, new CountScreen());
```

- [ ] **Step 6: Change `CountScreen`.** In `OnShow` remove the `EnteredSchool` advance and the commit, and refresh the guide for `ScreenId.Count`:

```csharp
        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.Count);
            StartNewSession();
        }
```

Delete the now-wrong comment block above it that explains the `EnteredSchool` advance (it moved to `BuildingScreen`) and leave one line: `// A new session starts every time the child opens the counting game (including a replay tap).` The session-end Home button returns to the School list:

```csharp
                EndButtonPositions[1], () => _game.Navigator.Show(ScreenId.School));
```

- [ ] **Step 7: Voice lines.** Append to `Resources/Voice/voice-lines.txt` (tab between key and text, same style as the file):

```
activity_count	Let's count!
school_welcome	Let's play a game! Tap the counting game!
```

Check the file's existing section comments and put both lines under a `# school` comment. A missing audio clip is silent (Voice.Say handles it), so no audio is needed to pass tests.

- [ ] **Step 8: Update the existing tests that meant "the Count screen".** In `Tests/CountScreenHelpTests.cs` replace each `_game.Navigator.Show(ScreenId.School);` with `_game.Navigator.Show(ScreenId.Count);` (six places), in `Tests/NoReadingAuditTests.cs` the same at lines 162 and 369, and change line 21 to:

```csharp
        private static readonly ScreenId[] Screens = { ScreenId.Creator, ScreenId.Map, ScreenId.House, ScreenId.School, ScreenId.Store, ScreenId.Count };
```

Leave the Map-button test at line 232 (`SchoolButton -> ScreenId.School`) as it is: it is still true.

- [ ] **Step 9: Run the suite, verify pass**

Expected: everything passes, including the new `BuildingScreenTests`. If `TappingATileStartsTheVoiceLine...` fails because the first `speaking == true` key is not `activity_count`, something spoke before the tile line (the Hud or the School list): fix that source, do not weaken the assertion.

- [ ] **Step 10: Commit**

```bash
git add EvasLearningWorld/Assets/Eva/App/Screens/BuildingScreen.cs EvasLearningWorld/Assets/Eva/App/Screens/BuildingScreen.cs.meta EvasLearningWorld/Assets/Eva/App/Screens/Navigator.cs EvasLearningWorld/Assets/Eva/App/EvaGame.cs EvasLearningWorld/Assets/Eva/App/Screens/CountScreen.cs EvasLearningWorld/Assets/Eva/Resources/Voice/voice-lines.txt EvasLearningWorld/Assets/Eva/Tests/BuildingScreenTests.cs EvasLearningWorld/Assets/Eva/Tests/BuildingScreenTests.cs.meta EvasLearningWorld/Assets/Eva/Tests/CountScreenHelpTests.cs EvasLearningWorld/Assets/Eva/Tests/NoReadingAuditTests.cs
git commit -m "feat: reusable building activity list; School opens it, Count is its own screen"
```

---

### Task 3: Tutorial: `FirstGame` points at the Count tile

**Files:**
- Modify: `App/Screens/TutorialGuide.cs` (`GuideTarget` enum line 10, `Plan` around line 56-66, `StartPointing` around line 108-116), `Tests/TutorialGuideTests.cs` (lines 20-30 table, 37-53 default-null list)

**Interfaces:**
- Consumes: `BuildingScreen.TilePosition(BuildingId, int)`.
- Produces: `GuideTarget.CountTile`; `Plan(TutorialStep.FirstGame, ScreenId.School) == ("school_welcome", GuideTarget.CountTile)`.

- [ ] **Step 1: Update the tests first.** In `PlanMatchesEveryRowOfTheGuidanceTable` add the row:

```csharp
        [TestCase(TutorialStep.FirstGame, ScreenId.School, "school_welcome", GuideTarget.CountTile)]
```

In the default-null list remove `[TestCase(TutorialStep.FirstGame, ScreenId.School)]` and add `[TestCase(TutorialStep.FirstGame, ScreenId.Count)]`, `[TestCase(TutorialStep.GoToSchool, ScreenId.School)]` (the step has already advanced by the time the list is visible, so no row) and `[TestCase(TutorialStep.PlacePurchase, ScreenId.Count)]`. Also update the comment above that list so it no longer says FirstGame has no row.

- [ ] **Step 2: Run to verify it fails** (compile error: `GuideTarget.CountTile`).

- [ ] **Step 3: Implement.** In `TutorialGuide.cs`:

```csharp
    public enum GuideTarget { None, HouseBuilding, SchoolBuilding, StoreBuilding, CountTile, StarterToLivingSeat, TrayToSlot, CheapestItem }
```

In `Plan`, after the `GoToSchool` row:

```csharp
            if (step == TutorialStep.FirstGame && screen == ScreenId.School) return ("school_welcome", GuideTarget.CountTile);
```

Update the doc comment above `Plan` (it says FirstGame on every screen defaults to nothing): FirstGame has a row on the School list only. In `StartPointing` add:

```csharp
                case GuideTarget.CountTile: _pointRoutine = _runner.StartCoroutine(PulseAt(BuildingScreen.TilePosition(BuildingId.School, 0))); break;
```

(add `using EvasLearningWorld.Rules;` if the file lacks it).

- [ ] **Step 4: Run the suite, verify pass**

- [ ] **Step 5: Commit**

```bash
git add EvasLearningWorld/Assets/Eva/App/Screens/TutorialGuide.cs EvasLearningWorld/Assets/Eva/Tests/TutorialGuideTests.cs
git commit -m "feat: tutorial hand points at the Count tile on the School list"
```

---

### Task 4: Import the user's art, phone check

**Files:**
- Create: `tools/art-import/import-activity-art.js`, `EvasLearningWorld/Assets/Eva/Resources/Art/world/school_list_bg.png`, `EvasLearningWorld/Assets/Eva/Resources/Art/activities/count.png` (+ `.meta` files after Unity imports them)
- Modify: `EvasLearningWorld/Assets/Eva/Tests/ArtTests.cs` (sprite manifest, line 18-20 area), `BuildingScreen.BackdropFor` (returns `world/school_list_bg`)

**Interfaces:**
- Consumes: the user's pictures `C:\Users\schei\Downloads\School_background.png` (1536 x 1024, RGB, empty classroom, list backdrop only) and `C:\Users\schei\Downloads\counting_tile.png` (1278 x 1230, RGB, classroom scene with three apples, three stars and blocks). sharp (already in `tools/art-import/node_modules`; run `npm install` there if missing).
- Produces: sprite `world/school_list_bg` (the backdrop of the activity list only; the Count game keeps `world/school_bg`) and sprite `activities/count` (1024 x 1024, centre-cropped square, rounded corners radius about 120 with transparent outside, thin brown outline `#7a4a1e`).

- [ ] **Step 1: Add both sprites to the art manifest test.** In `Tests/ArtTests.cs` add `"world/school_list_bg"` and `"activities/count"` to the list of required sprites. Run the suite: expect the art test to fail on the missing files.

- [ ] **Step 2: Write `tools/art-import/import-activity-art.js`:** it copies the backdrop as is into `Art/world/school_list_bg.png`, and builds the tile: centre-crop to a square (side = the shorter edge), resize to 1024, mask with a rounded-rectangle SVG (`dest-in` blend), then composite a rounded-rectangle outline SVG on top.

```js
const path = require('path');
const fs = require('fs');
const sharp = require('sharp');

const downloads = 'C:/Users/schei/Downloads';
const root = path.resolve(__dirname, '../../EvasLearningWorld/Assets/Eva/Resources/Art');
const size = 1024;
const radius = 120;

async function backdrop() {
  fs.copyFileSync(path.join(downloads, 'School_background.png'), path.join(root, 'world/school_list_bg.png'));
}

async function countTile() {
  const src = path.join(downloads, 'counting_tile.png');
  const meta = await sharp(src).metadata();
  const side = Math.min(meta.width, meta.height);
  const square = await sharp(src)
    .extract({ left: Math.floor((meta.width - side) / 2), top: Math.floor((meta.height - side) / 2), width: side, height: side })
    .resize(size, size)
    .ensureAlpha()
    .toBuffer();
  const mask = Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}"><rect width="${size}" height="${size}" rx="${radius}" fill="#fff"/></svg>`);
  const outline = Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}"><rect x="5" y="5" width="${size - 10}" height="${size - 10}" rx="${radius - 5}" fill="none" stroke="#7a4a1e" stroke-width="10"/></svg>`);
  await sharp(square)
    .composite([{ input: mask, blend: 'dest-in' }, { input: outline, blend: 'over' }])
    .png()
    .toFile(path.join(root, 'activities/count.png'));
}

(async () => {
  fs.mkdirSync(path.join(root, 'activities'), { recursive: true });
  await backdrop();
  await countTile();
  console.log('school_list_bg.png and activities/count.png written');
})();
```

- [ ] **Step 3: Run it and look at both pictures**

```bash
cd C:/Users/schei/mobile-games-framework/tools/art-import && node import-activity-art.js
```

Open both PNGs with the Read tool. The tile must show the scene inside rounded corners with a thin brown outline, nothing important cropped, transparent corners. Adjust the crop if the apples or stars are cut.

- [ ] **Step 4: Make sure `BuildingScreen.BackdropFor(BuildingId.School)` returns `"world/school_list_bg"`.** Run the suite, verify pass (the art test finds both sprites; Unity creates the `.meta` files).

- [ ] **Step 5: Commit**

```bash
git add tools/art-import/import-activity-art.js EvasLearningWorld/Assets/Eva/Resources/Art/world/school_list_bg.png EvasLearningWorld/Assets/Eva/Resources/Art/world/school_list_bg.png.meta EvasLearningWorld/Assets/Eva/Resources/Art/activities EvasLearningWorld/Assets/Eva/Tests/ArtTests.cs EvasLearningWorld/Assets/Eva/App/Screens/BuildingScreen.cs
git commit -m "art: School list backdrop and Count tile picture from the user's art"
```

- [ ] **Step 6: Build, install, and let the user judge on the phone**

```bash
cd C:/Users/schei/mobile-games-framework && source tools/env.sh && timeout 560 bash tools/build-eva-debug.sh 2>&1 | grep BUILD_RESULT
timeout 100 bash tools/eva-install.sh EvasLearningWorld/Builds/Android/eva-debug.apk com.noadsguy.evas.dev "$TEMP/x.png"
```

Ask the user to tap School and say "shot"; take a screenshot (`source tools/env.sh; MSYS_NO_PATHCONV=1 "$ADB" -s R3CY30NNA6W exec-out screencap -p > "$TEMP/x.png"`, view with Read; never script taps). Review points from the spec: is the single Count tile large and prominent, does it read as "counting", does the list feel like the school rather than a menu, does the Home button stay clear of it, does the tutorial hand point at the tile (reset the save or use a fresh install to see the first-run tutorial).

---

## Self-review against the spec

- Data (`Activities`, fixed order, `Filter` for the order test), `TileLayout` sizes 480/440/380/270/250, content area: Task 1.
- `BuildingScreen`, per-building backdrop, immediate tap with voice, `OnShow` tutorial advance, Hud Home unchanged: Task 2.
- `ScreenId.Count`, Count session-home back to the School list, audit `Screens` list extended: Task 2.
- Tutorial `FirstGame` row and `CountTile` target; `GoToSchool` advance moved: Tasks 2 and 3.
- Real art from the user (list backdrop `world/school_list_bg`, tile `activities/count`); voice lines `activity_count` and `school_welcome`: Tasks 2 and 4.
- Tests: catalogue order and completeness, layout bounds/overlap/min tap, tiles inside the safe area and clear of Home and coin counter (stricter than the 20-unit rule), tap starts the voice first and navigates at once, tutorial advance and plan rows: Tasks 1-3. The existing 251 tests keep passing (updated where they meant the Count screen).
- Out of scope respected: no locks, progress, categories, swipe, Store change.
