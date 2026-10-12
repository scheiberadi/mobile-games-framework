using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    // Healthy vs Unhealthy's "Feed Eva" screen: the table, Eva, what a drop does. Coroutines (voice, hand, eating) do not advance in Edit
    // Mode, but the rules run synchronously on a drop, which these tests do by hand.
    public class FeedEvaScreenTests
    {
        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            FeedEvaScreen.UseDevicePlacement = false; // work in picture space
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
        }

        [TearDown]
        public void TearDown()
        {
            FeedEvaScreen.UseDevicePlacement = true;
            if (_game != null) Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) Object.DestroyImmediate(_canvasObject);
        }

        private FeedEvaScreen ShowScreen()
        {
            _game.Navigator.Show(ScreenId.HealthyVsUnhealthy);
            var screen = _game.Navigator.GetScreen(ScreenId.HealthyVsUnhealthy) as FeedEvaScreen;
            Assert.IsNotNull(screen, "Healthy vs Unhealthy is registered on its own screen");
            return screen;
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        // The food in `slot` is dragged to `point` (picture space) and let go.
        private static void Drop(FeedEvaScreen screen, int slot, Vector2 point)
        {
            var drag = screen.FoodDrags[slot];
            drag.OnBeginDrag(new PointerEventData(null));
            screen.PlaceFood(slot, point);
            drag.OnEndDrag(new PointerEventData(null));
        }

        [Test]
        public void FourFoodsAreTheOnlyThingsToDragAndEachIsAFullSizeTapTarget()
        {
            var screen = ShowScreen();
            var drags = screen.Root.GetComponentsInChildren<DragItem>(false);
            Assert.That(drags.Length, Is.EqualTo(FoodTable.Slots));
            var rects = drags.Select(d => WorldRect(d.Rect)).ToArray();
            for (var i = 0; i < rects.Length; i++)
            {
                Assert.That(rects[i].width, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
                Assert.That(rects[i].height, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
                for (var j = i + 1; j < rects.Length; j++) Assert.That(rects[i].Overlaps(rects[j]), Is.False, i + " / " + j);
                Assert.That(rects[i].Overlaps(new Rect(-690f, 175f, 240f, 240f)), Is.False, "under the Back button");
            }
        }

        [Test]
        public void TheTableStartsWithTwoHealthyAndAtLeastOneUnhealthyFood()
        {
            var screen = ShowScreen();
            var healthy = Enumerable.Range(0, FoodTable.Slots).Count(i => FoodTable.IsHealthy(screen.Table.FoodAt(i)));
            Assert.That(healthy, Is.InRange(FoodTable.MinHealthyOnTable, FoodTable.Slots - FoodTable.MinUnhealthyOnTable));
        }

        [Test]
        public void AHealthyFoodLetGoAtEvasMouthLightsAStar()
        {
            var screen = ShowScreen();
            Drop(screen, screen.Table.HealthySlot(), FeedEvaScreen.EvaMouth);
            Assert.That(screen.Table.Stars, Is.EqualTo(1));
        }

        [Test]
        public void AnUnhealthyFoodLetGoAtEvasMouthIsRefusedAndNeverLightsAStar()
        {
            var screen = ShowScreen();
            var bad = Enumerable.Range(0, FoodTable.Slots).First(i => !FoodTable.IsHealthy(screen.Table.FoodAt(i)));
            Drop(screen, bad, FeedEvaScreen.EvaMouth);
            Assert.That(screen.Table.Stars, Is.EqualTo(0));
            Assert.That(screen.Table.TotalUnhealthy, Is.EqualTo(1));
        }

        [Test]
        public void AFoodLetGoFarFromEvaGoesHomeAndCountsForNothing()
        {
            var screen = ShowScreen();
            Drop(screen, 0, new Vector2(-300f, -300f));
            Assert.That(screen.Table.Stars, Is.EqualTo(0));
            Assert.That(screen.Table.TotalUnhealthy, Is.EqualTo(0));
        }

        [Test]
        public void EvaStaysClearOfTheTableAndTheFoods()
        {
            var screen = ShowScreen();
            var eva = WorldRect(screen.EvaRoot);
            foreach (var drag in screen.FoodDrags) Assert.That(WorldRect(drag.Rect).Overlaps(eva), Is.False);
        }
    }
}
