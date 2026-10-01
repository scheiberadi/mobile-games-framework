using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    // Answer-variety Prototype B (Sorting as drop-sort): build, layout and drop smoke tests of DropSortScreen.
    // Coroutines do not advance in Edit Mode, so these check what happens before a coroutine's first pause: the
    // item being accepted (no longer draggable) or refused (still draggable).
    public class DropSortScreenTests
    {
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
            if (_game != null) Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) Object.DestroyImmediate(_canvasObject);
        }

        private DropSortScreen ShowScreen(int level = 1)
        {
            _game.Progress.SortingLevel = level;
            _game.Navigator.Show(ScreenId.Sorting);
            var screen = _game.Navigator.GetScreen(ScreenId.Sorting) as DropSortScreen;
            Assert.IsNotNull(screen, "Sorting is registered on the drop-sort screen (EvaGame.SortingUsesDrop must be true for this test)");
            return screen;
        }

        private Transform ScreenRoot => _canvasObject.transform.Find("ScreenRoot/DropSortScreen");

        private DragItem Item => ScreenRoot.GetComponentInChildren<DragItem>(false);

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [Test]
        public void TheScreenBuildsABinPerCategoryAndOneDraggableItemAtEveryLevel()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            {
                var screen = ShowScreen(level);
                var round = screen.CurrentRound;
                Assert.IsNotNull(round, "a round is shown as soon as the screen is");
                var bins = ScreenRoot.Find("BinField").Cast<Transform>().Count(b => b.gameObject.activeSelf);
                Assert.That(bins, Is.EqualTo(round.BinCategories.Length), "level " + level);
                Assert.That(ScreenRoot.GetComponentsInChildren<DragItem>(false).Length, Is.EqualTo(1), "only the current item is draggable");
            }
        }

        [Test]
        public void TheDraggableItemIsBigInFrameAndClearOfEveryOtherTapTarget()
        {
            ShowScreen(6);
            Item.Rect.localScale = Vector3.one; // the item pops in; Edit Mode stops it at its first, small frame
            var targets = new List<RectTransform>();
            foreach (var target in _canvasObject.GetComponentsInChildren<TapTarget>(false))
                if (target.gameObject.activeInHierarchy && target.name != "HomeButton") targets.Add((RectTransform)target.transform); // the HUD home button deliberately overhangs the top edge (Hud.HomePosition)

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

        // The bins sit above the Hud's Home button's reach (y 175) and clear of the item and the waiting row.
        [Test]
        public void BinsClearTheHomeButtonAndTheBelt()
        {
            ShowScreen(6);
            var binField = ScreenRoot.Find("BinField");
            foreach (Transform bin in binField)
            {
                if (!bin.gameObject.activeSelf) continue;
                var rect = WorldRect((RectTransform)bin);
                Assert.That(rect.yMax, Is.LessThanOrEqualTo(180f), bin.name + " top");
                Assert.That(rect.yMin, Is.GreaterThan(Item.Rect.anchoredPosition.y + 120f), bin.name + " must sit above the item");
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(-720f), bin.name);
                Assert.That(rect.xMax, Is.LessThanOrEqualTo(720f), bin.name);
            }
        }

        [Test]
        public void DroppingTheItemOnItsOwnBinAcceptsItAndANearbyWrongBinRefusesIt()
        {
            var screen = ShowScreen(6);
            var round = screen.CurrentRound;
            var item = Item;
            item.enabled = true; // the intro line normally enables it

            var wrongBin = Enumerable.Range(0, round.BinCategories.Length).First(b => b != round.BinIndexOf(0));
            var wrongRect = (RectTransform)ScreenRoot.Find("BinField/Bin" + wrongBin);
            item.Rect.anchoredPosition = wrongRect.anchoredPosition + new Vector2(20f, -15f);
            item.OnEndDrag(new PointerEventData(null));
            Assert.That(item.enabled, Is.True, "a wrong bin refuses the item, which stays draggable");
            Assert.That(screen.CurrentItemIndex, Is.EqualTo(0));

            var rightRect = (RectTransform)ScreenRoot.Find("BinField/Bin" + round.BinIndexOf(0));
            item.Rect.anchoredPosition = rightRect.anchoredPosition + new Vector2(20f, -15f); // near, not exact
            item.OnEndDrag(new PointerEventData(null));
            Assert.That(item.enabled, Is.False, "the right bin takes the item, which is then no longer draggable");
        }

        [Test]
        public void DroppingInEmptySpaceIsNotAnAttemptAndKeepsTheItemDraggable()
        {
            var screen = ShowScreen(1);
            var item = Item;
            item.enabled = true;
            item.Rect.anchoredPosition = new Vector2(-200f, -215f);
            item.OnEndDrag(new PointerEventData(null));
            Assert.That(item.enabled, Is.True);
            Assert.That(screen.CurrentItemIndex, Is.EqualTo(0));
        }
    }
}
