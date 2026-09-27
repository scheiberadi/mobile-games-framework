using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static EvasLearningWorld.Tests.CountScreenTestSupport;

namespace EvasLearningWorld.Tests
{
    // The automated child-baseline guard: the child is 4 to 5 and cannot read, so the only text on any
    // screen is digits, every tappable thing is big, and nothing is interactive without the TapTarget marker.
    // Every later screen is added to Screens below.
    public class NoReadingAuditTests
    {
        private static readonly ScreenId[] Screens = { ScreenId.Creator, ScreenId.Map, ScreenId.House, ScreenId.School, ScreenId.Store, ScreenId.Count, ScreenId.NumberHunt, ScreenId.LetterHunt, ScreenId.Addition, ScreenId.Subtraction, ScreenId.WhichHasMore, ScreenId.OneMoreOneLess, ScreenId.NumberOrdering, ScreenId.MissingNumber, ScreenId.NumberLine, ScreenId.Multiplication, ScreenId.UppercaseToLowercase, ScreenId.BeginningSound, ScreenId.Rhyming, ScreenId.WordToImage, ScreenId.ImageToWord, ScreenId.LetterToSound, ScreenId.ParentGate, ScreenId.Settings, ScreenId.Playground, ScreenId.PatternCompletion, ScreenId.OddOneOut, ScreenId.WhatsMissing, ScreenId.WhichDoesntMakeSense, ScreenId.ItemToShadow, ScreenId.FingerMaze, ScreenId.FollowNumbersInOrder, ScreenId.FollowLettersInOrder, ScreenId.ShortestPath, ScreenId.AvoidObstacles, ScreenId.CollectEverything, ScreenId.RotateThePiece, ScreenId.Jigsaw, ScreenId.Tangram };
        // Adult-facing screens (spec "Settings"): normal readable text is allowed there; every other audit still applies.
        private static bool IsAdultScreen(Transform t) => Inside(t, "ParentGateScreen") || Inside(t, "SettingsScreen");
        private static bool Inside(Transform t, string screenName)
        {
            for (var p = t; p != null; p = p.parent) if (p.name == screenName) return true;
            return false;
        }
        private static readonly Regex DigitsOnly = new Regex("^[0-9]*$");

        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            // A Screen Space - Overlay canvas is normally auto-sized to Screen.width/height, but that resize
            // only happens on an actual render, which never occurs in a headless -nographics test run; left
            // alone, the canvas RectTransform stays at Unity's default 100x100 and every Hud corner button
            // (anchored a few dozen units in from a corner) ends up misleadingly close to screen centre instead
            // of near the real corner. Sizing it explicitly to the 1440 x 900 frame every screen is designed
            // against (see the plan's Global Constraints) makes the audit see the same geometry a real device
            // would - required for CreatorScreenTapTargetsDoNotOverlapByMoreThan20Units below to mean anything.
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            var gameObject = new GameObject("TestEvaGame");
            _game = gameObject.AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
        }

        [TearDown]
        public void TearDown()
        {
            if (_game != null) Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) Object.DestroyImmediate(_canvasObject);
        }

        private void ShowEveryScreen()
        {
            foreach (var id in Screens)
            {
                _game.Navigator.Show(id);
                Assert.AreEqual(id, _game.Navigator.Current);
            }
        }

        [Test]
        public void EveryScreenCanBeShownAndHomeIsHiddenOnlyOnTheMap()
        {
            foreach (var id in Screens)
            {
                _game.Navigator.Show(id);
                Assert.AreEqual(id, _game.Navigator.Current);
                var home = _canvasObject.transform.Find("HudRoot/HomeButton");
                Assert.IsNotNull(home, "the Hud has a home button");
                Assert.AreEqual(id != ScreenId.Map, home.gameObject.activeSelf, "home visible on " + id);
            }
        }

        [Test]
        public void OnlyDigitsAreShownExceptInTheSpeechBubble()
        {
            ShowEveryScreen();
            var texts = _canvasObject.GetComponentsInChildren<TMP_Text>(true);
            Assert.Greater(texts.Length, 0, "the Hud has numerals, so the audit must see some text");
            foreach (var text in texts)
            {
                if (text.GetComponentInParent<Bubble>(true) != null) continue;
                if (IsAdultScreen(text.transform)) continue;
                Assert.IsTrue(DigitsOnly.IsMatch(text.text), Path(text.transform) + " shows \"" + text.text + "\"");
            }
            foreach (var text in _canvasObject.GetComponentsInChildren<Text>(true))
                if (!IsAdultScreen(text.transform)) Assert.IsTrue(DigitsOnly.IsMatch(text.text), "legacy Text " + Path(text.transform) + " shows \"" + text.text + "\"");
        }

        // Count's object slots are a counting aid at 130-200 units (CountLayout); the child answers via the tiles.
        private static bool IsCountObjectSlot(Transform target) => target.parent != null && target.parent.name == "ObjectField";

        // Jigsaw's (and later Tangram's) puzzle pieces are necessarily smaller than 240 units at higher piece
        // counts (25 pieces can't each keep a 240-unit tap target on a board sized for a phone screen) - the same
        // "many small items are the assembly itself, not a standalone button" rationale as Count's ObjectField
        // above, so it reuses the same exemption mechanism rather than a separate one-off rule. Flagged to Adrian
        // same as every other audit/UX tension this session - see full-catalogue-plan.md.
        private static bool IsPuzzlePieceSlot(Transform target) => target.parent != null && target.parent.name == "PuzzlePieceField";

        [Test]
        public void EveryTapTargetIsAtLeast240UnitsSquare()
        {
            ShowEveryScreen();
            var targets = _canvasObject.GetComponentsInChildren<TapTarget>(true);
            Assert.Greater(targets.Length, 0);
            foreach (var target in targets)
            {
                if (IsCountObjectSlot(target.transform) || IsPuzzlePieceSlot(target.transform)) continue;
                var rect = ((RectTransform)target.transform).rect;
                Assert.GreaterOrEqual(rect.width, EvaUi.MinTap, Path(target.transform) + " width");
                Assert.GreaterOrEqual(rect.height, EvaUi.MinTap, Path(target.transform) + " height");
            }
        }

        [Test]
        public void NothingInteractiveExistsWithoutATapTarget()
        {
            ShowEveryScreen();
            var offenders = new List<string>();
            foreach (var behaviour in _canvasObject.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null) continue;
                var interactive = behaviour is Button || behaviour is IPointerClickHandler || behaviour is IDragHandler;
                if (interactive && behaviour.GetComponent<TapTarget>() == null)
                    offenders.Add(Path(behaviour.transform) + " (" + behaviour.GetType().Name + ")");
            }
            Assert.IsEmpty(offenders, "interactive objects without a TapTarget: " + string.Join(", ", offenders));
        }

        // Introduced by Task 9 and scoped to the Creator screen only, per an earlier ruling: other screens'
        // fixed-position layouts (House's furniture slots, Count's object positions) are exempt. Creator is
        // shown on its own, not via ShowEveryScreen, so the scan below sees only its own TapTargets plus the
        // always-present Hud (Home, and the Bubble button unless CreatorScreen has told the Hud to hide it) -
        // exactly the set of things that can actually be on screen together. Only active-and-visible targets
        // are compared: a hidden button (e.g. Home on the Map) cannot be mistapped, so it cannot "overlap"
        // anything in any way that matters here.
        [Test]
        public void CreatorScreenTapTargetsDoNotOverlapByMoreThan20Units()
        {
            _game.Navigator.Show(ScreenId.Creator);
            var targets = new List<RectTransform>();
            foreach (var target in _canvasObject.GetComponentsInChildren<TapTarget>(true))
                if (target.gameObject.activeInHierarchy) targets.Add((RectTransform)target.transform);

            Assert.Greater(targets.Count, 0);
            for (var i = 0; i < targets.Count; i++)
            for (var j = i + 1; j < targets.Count; j++)
            {
                var a = WorldRect(targets[i]);
                var b = WorldRect(targets[j]);
                var xOverlap = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
                var yOverlap = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
                if (xOverlap <= 0f || yOverlap <= 0f) continue; // disjoint on at least one axis: no overlap at all
                var amount = Mathf.Min(xOverlap, yOverlap);
                Assert.LessOrEqual(amount, 20f,
                    Path(targets[i]) + " overlaps " + Path(targets[j]) + " by " + amount + " units");
            }
        }

        // Levels 3-4 show five and levels 5-6 six answer tiles: each at least 190 (under MinTap by design), inside the frame, overlapping no other visible
        // tile or Hud button (home, bubble) by more than 20 units, and sitting below Eva's feet (anchor y -120).
        [Test]
        public void CountScreenFiveAnswerTilesFitWithoutCrowdingTheHudOrEva() => AssertAnswerTilesFit(3, 5);

        [Test]
        public void CountScreenSixAnswerTilesFitWithoutCrowdingTheHudOrEva() => AssertAnswerTilesFit(5, 6);

        private void AssertAnswerTilesFit(int level, int expectedCount)
        {
            _game.Progress.DifficultyLevel = level;
            _game.Navigator.Show(ScreenId.Count);
            var tiles = new List<RectTransform>();
            foreach (Transform tile in _canvasObject.transform.Find("ScreenRoot/CountScreen/AnswerField"))
                if (tile.gameObject.activeSelf) tiles.Add((RectTransform)tile);
            Assert.That(tiles.Count, Is.EqualTo(expectedCount));

            var others = new List<RectTransform>(tiles);
            foreach (var target in _canvasObject.GetComponentsInChildren<TapTarget>(false))
                if (target.transform.parent == _canvasObject.transform.Find("HudRoot")) others.Add((RectTransform)target.transform);

            var eva = _canvasObject.transform.Find("ScreenRoot/CountScreen/EvaAnchor");
            for (var i = 0; i < tiles.Count; i++)
            {
                var a = WorldRect(tiles[i]);
                Assert.GreaterOrEqual(a.width, 190f, "tile " + i + " width"); // 5-6 tiles deliberately under MinTap (user: tiles too big)
                Assert.GreaterOrEqual(a.height, 190f, "tile " + i + " height");
                Assert.That(a.xMin, Is.GreaterThanOrEqualTo(-720f), "tile " + i + " left");
                Assert.That(a.xMax, Is.LessThanOrEqualTo(720f), "tile " + i + " right");
                Assert.That(a.yMin, Is.GreaterThanOrEqualTo(-450f), "tile " + i + " bottom");
                Assert.That(a.yMax, Is.LessThanOrEqualTo(eva.position.y), "tile " + i + " must sit below Eva's feet");
                foreach (var other in others)
                {
                    if (other == tiles[i]) continue;
                    var b = WorldRect(other);
                    var xOverlap = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
                    var yOverlap = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
                    if (xOverlap <= 0f || yOverlap <= 0f) continue;
                    Assert.LessOrEqual(Mathf.Min(xOverlap, yOverlap), 20f, Path(tiles[i]) + " overlaps " + Path(other));
                }
            }
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        // Regression test for the Tasks 9-11 review finding: the Creator screen's Confirm button and shirt
        // row (at positions ShirtRowY=-220, CheckPosition=(580, -220)) used to fall below the real on-device
        // frame floor at y=-450 because an earlier draft assumed a taller nominal frame. Every tap target's
        // full rect (not just its centre point) must stay inside y ∈ [-450, 450], the actual bounds a
        // height-matched canvas produces on the real device.
        [Test]
        public void CreatorScreenTapTargetsDoNotFallBelowTheRealFrameFloor()
        {
            _game.Navigator.Show(ScreenId.Creator);
            var targets = new List<RectTransform>();
            foreach (var target in _canvasObject.GetComponentsInChildren<TapTarget>(true))
                if (target.gameObject.activeInHierarchy) targets.Add((RectTransform)target.transform);

            Assert.Greater(targets.Count, 0);
            const float frameFloor = -450f;
            for (var i = 0; i < targets.Count; i++)
            {
                var rect = WorldRect(targets[i]);
                Assert.GreaterOrEqual(rect.yMin, frameFloor,
                    Path(targets[i]) + " bottom edge " + rect.yMin + " is below the real " + frameFloor + " frame floor");
            }
        }

        [Test]
        public void MapBuildingsNavigateAndTappingPlaysNoErrors()
        {
            _game.Navigator.Show(ScreenId.Map);
            var map = _game.ScreenRoot.Find("MapScreen");
            Assert.IsNotNull(map);
            var expected = new Dictionary<string, ScreenId>
            {
                { "WorldView/World/Place_House", ScreenId.House }, { "WorldView/World/Place_School", ScreenId.School }, { "WorldView/World/Place_Store", ScreenId.Store }
            };
            foreach (var pair in expected)
            {
                _game.Navigator.Show(ScreenId.Map);
                map.Find(pair.Key).GetComponent<Button>().onClick.Invoke();
                _game.Map.Advance(10f); // finish the walk
                Assert.AreEqual(pair.Value, _game.Navigator.Current);
                _canvasObject.transform.Find("HudRoot/HomeButton").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(ScreenId.Map, _game.Navigator.Current);
            }
        }

        // The House screen (Task 10): ShowEveryScreen already exercises the empty-house state (Progress.Owned
        // starts empty, so opening House grants and shows just the starter sofa in the tray), but not a mix of
        // a placed item and a still-unplaced one, which is the shape DragItem/HouseScreen actually vary their
        // layout for. Owned/placed here before the screen is ever shown, same as the Store-purchase case this
        // guards against, since HouseScreen.OnShow only grants the starter when Owned is still empty.
        [Test]
        public void HouseScreenPassesTheAuditWithItemsInTheTrayAndPlaced()
        {
            _game.Progress.Owned.Add("sofa");
            _game.Progress.Owned.Add("rug");
            Assert.IsTrue(_game.Progress.House.TryPlace("sofa", "living_seat", _game.Progress.Owned));

            _game.Progress.Tutorial = TutorialStep.PlaceStarter; // opens zoomed into the living room (tray and drag items only exist in room view)
            _game.Navigator.Show(ScreenId.House);
            Assert.AreEqual(ScreenId.House, _game.Navigator.Current);
            AssertNoReadingInvariants();

            var dragTargets = _canvasObject.GetComponentsInChildren<DragItem>(true);
            Assert.AreEqual(2, dragTargets.Length, "one placed item, one tray item");
        }

        // The Store screen (Task 11): 6 coins afford the three cheapest items (rug 5, lamp 6, plant 6) but not
        // the two priciest (table 8, bed 10); bookshelf (9) is already owned, dimmed with its check badge
        // instead of a price. All three shelf states - affordable, not affordable, owned - appear together in
        // this one pass, which the plain empty-Owned/zero-coin pass ShowEveryScreen already exercises does not.
        [Test]
        public void StoreScreenPassesTheAuditWithAffordableUnaffordableAndOwnedItems()
        {
            _game.Progress.AddCoins(6);
            _game.Progress.Owned.Add("bookshelf");

            _game.Navigator.Show(ScreenId.Store);
            Assert.AreEqual(ScreenId.Store, _game.Navigator.Current);
            AssertNoReadingInvariants();

            var store = _game.ScreenRoot.Find("StoreScreen");
            Assert.IsNotNull(store, "the Store screen builds its root");
            var shelf = store.Find("Shelf");
            Assert.IsNotNull(shelf, "the Store screen has a Shelf layer");

            Assert.IsTrue(shelf.Find("OwnedBadge_bookshelf").gameObject.activeSelf, "an owned item shows its check badge");
            Assert.IsFalse(shelf.Find("Price_bookshelf").gameObject.activeSelf, "an owned item hides its price");
            Assert.IsFalse(shelf.Find("OwnedBadge_rug").gameObject.activeSelf, "an unowned item hides its check badge");
            Assert.IsTrue(shelf.Find("Price_rug").gameObject.activeSelf, "an affordable item still shows its price");
            Assert.IsTrue(shelf.Find("Price_table").gameObject.activeSelf, "an unaffordable item still shows its price");
        }

        // Regression coverage for the Task 11 review finding: two of the six shelf items (the top-left and
        // bottom-right slots) used to overlap the Hud's fixed Home/Bubble buttons by 30 units, stealing part
        // of their tap targets, because StoreScreen's layout math assumed a 1600-unit-wide design floor
        // instead of the real 1440-unit one. Store is shown on its own, not via ShowEveryScreen, so the scan
        // below sees only its own TapTargets plus the always-present Hud (Home, and Bubble since
        // Store != Creator) - exactly the set of things that can actually be on screen together. Same pattern
        // as CreatorScreenTapTargetsDoNotOverlapByMoreThan20Units above.
        [Test]
        public void StoreScreenTapTargetsDoNotOverlapByMoreThan20Units()
        {
            _game.Navigator.Show(ScreenId.Store);
            var targets = new List<RectTransform>();
            foreach (var target in _canvasObject.GetComponentsInChildren<TapTarget>(true))
                if (target.gameObject.activeInHierarchy) targets.Add((RectTransform)target.transform);

            Assert.Greater(targets.Count, 0);
            for (var i = 0; i < targets.Count; i++)
            for (var j = i + 1; j < targets.Count; j++)
            {
                var a = WorldRect(targets[i]);
                var b = WorldRect(targets[j]);
                var xOverlap = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
                var yOverlap = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
                if (xOverlap <= 0f || yOverlap <= 0f) continue; // disjoint on at least one axis: no overlap at all
                var amount = Mathf.Min(xOverlap, yOverlap);
                Assert.LessOrEqual(amount, 20f,
                    Path(targets[i]) + " overlaps " + Path(targets[j]) + " by " + amount + " units");
            }
        }

        // Regression test for House tap targets (draggable furniture items): placed items and tray items must
        // not overlap each other or the Hud by more than 20 units. House is shown on its own, not via
        // ShowEveryScreen, so the scan below sees only its own TapTargets plus the always-present Hud (Home,
        // and Bubble since House != Creator). Same pattern as CreatorScreenTapTargetsDoNotOverlapByMoreThan20Units
        // and StoreScreenTapTargetsDoNotOverlapByMoreThan20Units above.
        [Test]
        public void HouseScreenTapTargetsDoNotOverlapByMoreThan20Units()
        {
            _game.Progress.Owned.Add("sofa");
            _game.Progress.Owned.Add("rug");
            Assert.IsTrue(_game.Progress.House.TryPlace("sofa", "living_seat", _game.Progress.Owned));

            _game.Progress.Tutorial = TutorialStep.PlaceStarter; // room view: tray and placed items are the tap targets
            _game.Navigator.Show(ScreenId.House);
            var targets = new List<RectTransform>();
            foreach (var target in _canvasObject.GetComponentsInChildren<TapTarget>(true))
            {
                if (!target.gameObject.activeInHierarchy) continue;
                // The room panels stay in the hierarchy but are not tappable in room view.
                if (target.TryGetComponent<UnityEngine.UI.Button>(out var button) && !button.interactable) continue;
                targets.Add((RectTransform)target.transform);
            }

            Assert.Greater(targets.Count, 0);
            for (var i = 0; i < targets.Count; i++)
            for (var j = i + 1; j < targets.Count; j++)
            {
                var a = WorldRect(targets[i]);
                var b = WorldRect(targets[j]);
                var xOverlap = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
                var yOverlap = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
                if (xOverlap <= 0f || yOverlap <= 0f) continue; // disjoint on at least one axis: no overlap at all
                var amount = Mathf.Min(xOverlap, yOverlap);
                Assert.LessOrEqual(amount, 20f,
                    Path(targets[i]) + " overlaps " + Path(targets[j]) + " by " + amount + " units");
            }
        }

        // The Count screen (Task 8): Eva's help ladder adds three more states beyond the plain question the
        // other audit tests already exercise via ShowEveryScreen - Hint, Demonstrate and the end-of-session
        // panel. Every one of them must still pass the same two invariants: only digits outside the speech
        // bubble, and every TapTarget at least MinTap square. A UnityTest because reaching Hint/Demonstrate/end
        // requires the screen's own coroutines (real WaitForSeconds, real voice-clip lengths) to run.
        [UnityTest]
        public IEnumerator CountScreenPassesTheAuditInQuestionHintDemonstrationAndSessionEndStates()
        {
            const float timeout = 20f;
            _game.Progress.CountIntroSeen = true; // isolate this audit from the separate once-ever intro state
            _game.Navigator.Show(ScreenId.Count);

            yield return WaitUntil(() => AnyTileInteractable(_canvasObject.transform), timeout, "question phase to start");
            AssertNoReadingInvariants();

            var canvas = _canvasObject.transform;
            var wrong = WrongTileIndices(canvas);
            Assert.That(wrong.Length, Is.EqualTo(2));

            TapTile(canvas, wrong[0]); // 1st mistake: Retry
            yield return Tick();
            TapTile(canvas, wrong[1]); // 2nd mistake: Hint begins
            yield return Tick();
            AssertNoReadingInvariants(); // hint state: tiles disabled, hand animating

            yield return WaitUntil(() => TileInteractable(canvas, wrong[0]), timeout, "hint to finish");
            TapTile(canvas, wrong[0]); // 3rd mistake (re-enabled tried tile): Demonstrate begins
            yield return Tick();
            AssertNoReadingInvariants(); // demonstration state: guided counting in progress

            var quantity = ObjectCount(canvas);
            for (var i = 0; i < quantity; i++)
            {
                TapObject(canvas, i);
                yield return Tick();
            }
            yield return WaitUntil(() => AnyTileInteractable(canvas), timeout, "the correct tile to be highlighted");
            AssertNoReadingInvariants(); // demonstration answer step: correct tile highlighted, others disabled
            TapTile(canvas, CorrectTileIndex(canvas));

            // Finish the remaining rounds cleanly to reach the end-of-session state.
            for (var round = 1; round < CountRoundGenerator.RoundsPerSession; round++)
                yield return PlayRoundCorrectly(canvas, timeout);

            yield return WaitUntil(() => SessionEndVisible(canvas), timeout, "the end-of-session panel");
            AssertNoReadingInvariants(); // session end state
        }

        // The same two checks as OnlyDigitsAreShownExceptInTheSpeechBubble and EveryTapTargetIsAtLeast240UnitsSquare,
        // factored out so the Count-screen-state test above can re-run them at each state without re-showing
        // every screen (ShowEveryScreen would tear down the very Count-screen state under test).
        private void AssertNoReadingInvariants()
        {
            foreach (var text in _canvasObject.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.GetComponentInParent<Bubble>(true) != null) continue;
                if (IsAdultScreen(text.transform)) continue;
                Assert.IsTrue(DigitsOnly.IsMatch(text.text), Path(text.transform) + " shows \"" + text.text + "\"");
            }
            foreach (var text in _canvasObject.GetComponentsInChildren<Text>(true))
                if (!IsAdultScreen(text.transform)) Assert.IsTrue(DigitsOnly.IsMatch(text.text), "legacy Text " + Path(text.transform) + " shows \"" + text.text + "\"");

            foreach (var target in _canvasObject.GetComponentsInChildren<TapTarget>(true))
            {
                if (IsCountObjectSlot(target.transform) || IsPuzzlePieceSlot(target.transform)) continue;
                var rect = ((RectTransform)target.transform).rect;
                Assert.GreaterOrEqual(rect.width, EvaUi.MinTap, Path(target.transform) + " width");
                Assert.GreaterOrEqual(rect.height, EvaUi.MinTap, Path(target.transform) + " height");
            }
        }

        // Task 12: the tutorial guide's own pointer hand (separate from the Count screen's - see TutorialGuide,
        // built once under its own GuideRoot layer for the whole session) must never become an accidental tap
        // target, on top of every screen the plain audit above already covers via ShowEveryScreen. It has no
        // TapTarget (PointerHand's own Image never carries one, matching its class comment that it is never
        // meant to receive taps) and its raycastTarget is off, so it can never steal or block a tap on
        // whatever real button or draggable it happens to be sitting on top of.
        [Test]
        public void TheTutorialGuideHandIsNeverATapTargetOnAnyScreen()
        {
            ShowEveryScreen();
            var hand = _canvasObject.transform.Find("GuideRoot/PointerHand");
            Assert.IsNotNull(hand, "the tutorial guide builds its hand under GuideRoot");
            Assert.IsNull(hand.GetComponent<TapTarget>(), "the guide's hand must never be tappable");
            var image = hand.GetComponent<Image>();
            Assert.IsNotNull(image);
            Assert.IsFalse(image.raycastTarget, "the guide's hand must never block a tap underneath it");
        }

        private static string Path(Transform transform)
        {
            var path = transform.name;
            while (transform.parent != null) { transform = transform.parent; path = transform.name + "/" + path; }
            return path;
        }
    }
}
