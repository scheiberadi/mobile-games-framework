using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    // Sink or Float's screen: the shelves, the tank, dropping things in. Coroutines (voice, hand, round end) do not advance in Edit
    // Mode, but the tank's physics runs from Tick, which these tests call by hand.
    public class SinkOrFloatScreenTests
    {
        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            SinkOrFloatScreen.UseDevicePlacement = false; // work in picture space
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
        }

        [TearDown]
        public void TearDown()
        {
            SinkOrFloatScreen.UseDevicePlacement = true;
            if (_game != null) Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) Object.DestroyImmediate(_canvasObject);
        }

        private SinkOrFloatScreen ShowScreen()
        {
            _game.Navigator.Show(ScreenId.SinkOrFloat);
            var screen = _game.Navigator.GetScreen(ScreenId.SinkOrFloat) as SinkOrFloatScreen;
            Assert.IsNotNull(screen, "Sink or Float is registered on its own screen");
            return screen;
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static void Tick(SinkOrFloatScreen screen, float seconds)
        {
            for (var t = 0f; t < seconds; t += 1f / 60f) screen.Tick(1f / 60f);
        }

        [Test]
        public void SixThingsStandOnTheShelvesEachABigTapTargetClearOfTheOthers()
        {
            var screen = ShowScreen();
            var items = screen.Root.GetComponentsInChildren<DragItem>(false);
            Assert.That(items.Length, Is.EqualTo(SinkOrFloatScreen.Cells));
            Assert.That(screen.CurrentIds.Length, Is.EqualTo(SinkOrFloatScreen.Cells));
            foreach (var item in items) item.Rect.localScale = Vector3.one; // they pop in; Edit Mode stops them at the first, small frame
            foreach (var item in items)
            {
                Assert.That(WorldRect(item.Rect).width, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f), item.name);
                Assert.That(WorldRect(item.Rect).height, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f), item.name);
            }
            // The tap areas of neighbours may touch a little (the planks are only about 200 apart), the pictures never do.
            var art = items.Select(i => WorldRect((RectTransform)i.transform.Find("Art"))).ToArray();
            for (var i = 0; i < items.Length; i++)
                for (var j = i + 1; j < items.Length; j++)
                    Assert.That(art[i].Overlaps(art[j]), Is.False, items[i].name + " / " + items[j].name);
        }

        [Test]
        public void TheShelvesClearTheBackButtonAndTheTank()
        {
            ShowScreen();
            for (var cell = 0; cell < SinkOrFloatScreen.Cells; cell++)
            {
                var box = new Rect(SinkOrFloatScreen.CellPosition(cell) - Vector2.one * SinkOrFloatScreen.ShelfArt * 0.5f, Vector2.one * SinkOrFloatScreen.ShelfArt);
                Assert.That(box.xMin, Is.GreaterThanOrEqualTo(-452f), "right of the Back button's reach (x -690..-450), cell " + cell);
                Assert.That(box.xMax, Is.LessThanOrEqualTo(SinkOrFloatScreen.TankCentre.x - SinkOrFloatScreen.TankWidth * 0.5f), "left of the tank, cell " + cell);
            }
            var tank = new Rect(SinkOrFloatScreen.TankCentre.x - SinkOrFloatScreen.TankWidth * 0.5f, SinkOrFloatScreen.TankCentre.y - SinkOrFloatScreen.WaterDepth,
                SinkOrFloatScreen.TankWidth, SinkOrFloatScreen.WaterDepth + SinkOrFloatScreen.HeadRoom);
            Assert.That(tank.yMax, Is.LessThanOrEqualTo(300f), "under the coin counter (y 300..450)");
            Assert.That(tank.xMax, Is.LessThanOrEqualTo(720f));
            var pair = CompanionLayout.Corner.Footprint;
            Assert.That(tank.yMin, Is.GreaterThanOrEqualTo(pair.YMax - 40f), "the pair in the corner stands in front of the table, not in the water");
        }

        [Test]
        public void ADropOutsideTheTankGoesBackToItsShelf()
        {
            var screen = ShowScreen();
            var item = screen.Root.GetComponentsInChildren<DragItem>(false)[0];
            item.Rect.anchoredPosition = new Vector2(-600f, -300f);
            item.OnEndDrag(new PointerEventData(null));
            Assert.That(screen.PlacedCount, Is.EqualTo(0));
            Assert.That(screen.IsOnShelf(0), Is.True);
            Assert.That(item.gameObject.activeSelf, Is.True);
        }

        // The idle hint hand must never swallow a child's drag: touching another thing cancels the hint and the drop works.
        [Test]
        public void ADragStartedWhileTheHintHandIsOutCancelsTheHintAndTheDropStillWorks()
        {
            var screen = ShowScreen();
            Tick(screen, 14.5f); // the idle hint starts on the first thing on the shelf
            var items = screen.Root.GetComponentsInChildren<DragItem>(false);
            var other = items[3];
            other.OnBeginDrag(new PointerEventData(null));
            other.Rect.anchoredPosition = SinkOrFloatScreen.TankCentre + new Vector2(0f, 120f);
            other.OnEndDrag(new PointerEventData(null));
            Assert.That(screen.PlacedCount, Is.EqualTo(1), "the drop went into the tank");
            Assert.That(screen.IsOnShelf(3), Is.False);
            Assert.That(items[0].enabled, Is.True, "the hinted thing is back and draggable");
        }

        [Test]
        public void ADropOverTheTankLeavesTheShelfAndPutsTheThingInTheWater()
        {
            var screen = ShowScreen();
            var item = screen.Root.GetComponentsInChildren<DragItem>(false)[0];
            item.Rect.anchoredPosition = SinkOrFloatScreen.TankCentre + new Vector2(0f, 120f);
            item.OnEndDrag(new PointerEventData(null));
            Assert.That(screen.PlacedCount, Is.EqualTo(1));
            Assert.That(screen.IsOnShelf(0), Is.False);
            Assert.That(item.gameObject.activeSelf, Is.False, "the shelf cell is empty");
            Assert.That(screen.Bodies[0].InWater, Is.False, "it has not hit the water yet");
        }

        [Test]
        public void EveryThingDoesWhatItDoesInRealWater()
        {
            var screen = ShowScreen();
            for (var cell = 0; cell < SinkOrFloatScreen.Cells; cell++)
                screen.Release(cell, SinkOrFloatScreen.TankCentre + new Vector2(-250f + cell * 100f, 150f));
            Assert.That(screen.PlacedCount, Is.EqualTo(SinkOrFloatScreen.Cells));

            Tick(screen, 12f);

            for (var cell = 0; cell < SinkOrFloatScreen.Cells; cell++)
            {
                var item = SinkOrFloat.Find(screen.CurrentIds[cell]);
                var body = screen.Bodies[cell];
                Assert.That(body.InWater, Is.True, item.Id);
                Assert.That(body.Floats, Is.EqualTo(item.Floats), item.Id);
                Assert.That(body.Settled, Is.True, item.Id);
                if (item.Floats) Assert.That(body.Y, Is.GreaterThan(-40f), item.Id + " floats at the surface");
                else Assert.That(body.Y, Is.LessThan(SinkOrFloatScreen.FloorY + SinkOrFloatScreen.BodySize), item.Id + " lies on the floor");
            }
        }

        [Test]
        public void AThingCannotBeDroppedTwice()
        {
            var screen = ShowScreen();
            screen.Release(2, SinkOrFloatScreen.TankCentre);
            screen.Release(2, SinkOrFloatScreen.TankCentre);
            Assert.That(screen.PlacedCount, Is.EqualTo(1));
        }

        [Test]
        public void ADropPastAWallIsClampedInsideTheTank()
        {
            var screen = ShowScreen();
            screen.Release(0, SinkOrFloatScreen.TankCentre + new Vector2(-500f, 0f)); // far past the wall: it is clamped inside
            Assert.That(Mathf.Abs(screen.Bodies[0].X), Is.LessThanOrEqualTo(SinkOrFloatScreen.TankWidth * 0.5f));
        }

        [Test]
        public void ARippleRunsThroughTheWaterWhenSomethingLandsInIt()
        {
            var screen = ShowScreen();
            screen.Release(0, SinkOrFloatScreen.TankCentre + new Vector2(0f, 100f));
            Tick(screen, 0.4f);
            Assert.That(screen.Surface.RippleEnergy, Is.GreaterThan(0.5f));
        }

        [Test]
        public void TheScreenKeepsEveryTapTargetInsideTheSafeFrame()
        {
            var screen = ShowScreen();
            foreach (var target in screen.Root.GetComponentsInChildren<TapTarget>(false))
            {
                if (target.name == "HomeButton") continue;
                var rect = WorldRect((RectTransform)target.transform);
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(-720f), target.name);
                Assert.That(rect.xMax, Is.LessThanOrEqualTo(720f), target.name);
            }
            Assert.That(screen.Root.GetComponentsInChildren<WaterGraphic>(false).Length, Is.EqualTo(2), "a layer of water behind the things and one in front");
        }
    }
}
