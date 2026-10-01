using System;
using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    // Answer-variety Prototype A (Item to Shadow as drag-to-target): the pure pairing/placement rules, the art
    // naming regression, and a build/layout/placement smoke test of the new screen.
    // Written in a cloud container with no Unity: none of these has been run yet.
    public class DragToTargetTests
    {
        // --- DropGeometry --------------------------------------------------------------------------------------

        private static readonly WorldPoint[] Centres = { new WorldPoint(0f, 0f), new WorldPoint(100f, 0f), new WorldPoint(0f, 100f) };

        [Test]
        public void NearestWithinRadiusPicksTheClosestInRangeCentre()
        {
            Assert.That(DropGeometry.NearestWithinRadius(10f, 5f, Centres, 50f), Is.EqualTo(0));
            Assert.That(DropGeometry.NearestWithinRadius(90f, 5f, Centres, 50f), Is.EqualTo(1));
            Assert.That(DropGeometry.NearestWithinRadius(5f, 95f, Centres, 50f), Is.EqualTo(2));
        }

        [Test]
        public void NearestWithinRadiusIsMinusOneWhenNothingIsInRange()
        {
            Assert.That(DropGeometry.NearestWithinRadius(500f, 500f, Centres, 50f), Is.EqualTo(-1));
            Assert.That(DropGeometry.NearestWithinRadius(0f, 0f, new WorldPoint[0], 50f), Is.EqualTo(-1));
        }

        [Test]
        public void NearestWithinRadiusIsInclusiveAndTiesGoToTheLowerIndex()
        {
            Assert.That(DropGeometry.NearestWithinRadius(50f, 0f, Centres, 50f), Is.EqualTo(0), "50 from centre 0 and 50 from centre 1: lower index, and the radius itself is in range");
            Assert.That(DropGeometry.NearestWithinRadius(51f, 0f, new[] { new WorldPoint(0f, 0f) }, 50f), Is.EqualTo(-1));
        }

        // --- Adapter from Item to Shadow -----------------------------------------------------------------------

        [Test]
        public void EveryItemHasExactlyOneTargetWithTheSameKey()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var rng = new Random(seed);
                var source = ItemToShadowRoundGenerator.Create(level, rng);
                var round = DragToTargetRoundBuilder.FromItemToShadow(source, rng);

                Assert.That(round.ItemKeys.Length, Is.EqualTo(source.Choices.Length));
                Assert.That(round.TargetKeys, Is.EqualTo(source.Choices), "shadows keep the generator's own order");
                Assert.That(round.ItemKeys.OrderBy(k => k), Is.EqualTo(round.TargetKeys.OrderBy(k => k)), "same set of keys");
                for (var i = 0; i < round.ItemKeys.Length; i++)
                    Assert.That(round.TargetKeys[round.TargetIndexOf(i)], Is.EqualTo(round.ItemKeys[i]));
            }
        }

        [Test]
        public void NoObjectSitsInTheSamePositionAsItsOwnShadow()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var rng = new Random(seed);
                var round = DragToTargetRoundBuilder.FromItemToShadow(ItemToShadowRoundGenerator.Create(level, rng), rng);
                for (var i = 0; i < round.ItemKeys.Length; i++)
                    Assert.That(round.TargetIndexOf(i), Is.Not.EqualTo(i), "level " + level + " seed " + seed + " item " + i);
            }
        }

        [Test]
        public void TheHintItemIsTheGeneratorsTargetAndRoundsAreDeterministicPerSeed()
        {
            var rngA = new Random(11);
            var sourceA = ItemToShadowRoundGenerator.Create(4, rngA);
            var a = DragToTargetRoundBuilder.FromItemToShadow(sourceA, rngA);
            Assert.That(a.ItemKeys[a.HintItemIndex], Is.EqualTo(sourceA.TargetKey));

            var rngB = new Random(11);
            var b = DragToTargetRoundBuilder.FromItemToShadow(ItemToShadowRoundGenerator.Create(4, rngB), rngB);
            Assert.That(b.ItemKeys, Is.EqualTo(a.ItemKeys));
            Assert.That(b.TargetKeys, Is.EqualTo(a.TargetKeys));
        }

        // --- Art naming regression -----------------------------------------------------------------------------

        // The tap screen once asked for "itemtoshadow/<key>_object" while the art is "itemtoshadow/<key>.png", so the
        // object showed a placeholder. Both screens now take their keys from ItemToShadowRoundGenerator; this loads
        // every one of them from the real Resources folder.
        [Test]
        public void EveryItemToShadowObjectAndSilhouetteSpriteResolves()
        {
            var keys = ItemToShadowRoundGenerator.AllKeys;
            Assert.That(keys.Count, Is.EqualTo(16));
            foreach (var key in keys)
            {
                Assert.IsNotNull(Resources.Load<Sprite>("Art/" + ItemToShadowRoundGenerator.ObjectSpriteKey(key)), "missing object sprite for " + key);
                Assert.IsNotNull(Resources.Load<Sprite>("Art/" + ItemToShadowRoundGenerator.SilhouetteSpriteKey(key)), "missing silhouette sprite for " + key);
            }
        }

        [Test]
        public void SpriteKeysUseTheNamesTheArtFilesHave()
        {
            Assert.That(ItemToShadowRoundGenerator.ObjectSpriteKey("apple"), Is.EqualTo("itemtoshadow/apple"));
            Assert.That(ItemToShadowRoundGenerator.SilhouetteSpriteKey("apple"), Is.EqualTo("itemtoshadow/apple_silhouette"));
        }

        // --- The new screen: layout and placement smoke ---------------------------------------------------------

        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
        }

        [TearDown]
        public void TearDown()
        {
            if (_game != null) UnityEngine.Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) UnityEngine.Object.DestroyImmediate(_canvasObject);
        }

        private DragToTargetScreen ShowScreen()
        {
            _game.Navigator.Show(ScreenId.ItemToShadow);
            var screen = _game.Navigator.GetScreen(ScreenId.ItemToShadow) as DragToTargetScreen;
            Assert.IsNotNull(screen, "ItemToShadow is registered on the drag screen (EvaGame.ItemToShadowUsesDrag must be true for this test)");
            return screen;
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private List<DragItem> ActiveItems()
        {
            var root = _canvasObject.transform.Find("ScreenRoot/DragToTargetScreen");
            return root.GetComponentsInChildren<DragItem>(false).ToList();
        }

        [Test]
        public void TheDragScreenBuildsWithBigInFrameNonOverlappingHandles()
        {
            ShowScreen();
            var items = ActiveItems();
            Assert.That(items.Count, Is.InRange(3, 4));

            var targets = new List<RectTransform>();
            foreach (var target in _canvasObject.GetComponentsInChildren<TapTarget>(false))
                if (target.gameObject.activeInHierarchy) targets.Add((RectTransform)target.transform);

            for (var i = 0; i < targets.Count; i++)
            {
                var a = WorldRect(targets[i]);
                Assert.That(a.width, Is.GreaterThanOrEqualTo(EvaUi.MinTap), targets[i].name + " width");
                Assert.That(a.height, Is.GreaterThanOrEqualTo(EvaUi.MinTap), targets[i].name + " height");
                Assert.That(a.xMin, Is.GreaterThanOrEqualTo(-720f), targets[i].name);
                Assert.That(a.xMax, Is.LessThanOrEqualTo(720f), targets[i].name);
                Assert.That(a.yMin, Is.GreaterThanOrEqualTo(-450f), targets[i].name);
                Assert.That(a.yMax, Is.LessThanOrEqualTo(450f), targets[i].name);
                for (var j = i + 1; j < targets.Count; j++)
                {
                    var b = WorldRect(targets[j]);
                    var x = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
                    var y = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
                    if (x <= 0f || y <= 0f) continue;
                    Assert.That(Mathf.Min(x, y), Is.LessThanOrEqualTo(20f), targets[i].name + " overlaps " + targets[j].name);
                }
            }
        }

        // Drops all but the last item on their own shadows by calling the real DragItem end-drag path, and checks
        // each one is accepted (disabled, no longer draggable). The last drop is left out so the test does not
        // run the round-complete reward flow (coins, voice) in Edit Mode.
        [Test]
        public void DroppingItemsOnTheirOwnShadowsPlacesThem()
        {
            var screen = ShowScreen();
            var round = screen.CurrentRound;
            Assert.IsNotNull(round, "a round is shown as soon as the screen is");

            var root = _canvasObject.transform.Find("ScreenRoot/DragToTargetScreen");
            var items = ActiveItems();
            Assert.That(items.Count, Is.EqualTo(round.ItemKeys.Length));

            for (var i = 0; i < items.Count - 1; i++)
            {
                var targetRect = (RectTransform)root.Find("TargetField/Target" + round.TargetIndexOf(i));
                items[i].enabled = true; // the intro line normally enables them
                items[i].Rect.anchoredPosition = targetRect.anchoredPosition + new Vector2(20f, -15f); // near, not exact
                items[i].OnEndDrag(new PointerEventData(null));
                Assert.That(items[i].enabled, Is.False, "item " + i + " should be placed (and no longer draggable)");
            }
        }
    }
}
