using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    // The Magnet table's screen: the table, the tub, the magnet and what sticks to it. Coroutines (voice, hand, round end) do not advance
    // in Edit Mode, but the table's logic runs from Tick, which these tests call by hand.
    public class MagnetTableScreenTests
    {
        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            MagnetTableScreen.UseDevicePlacement = false; // work in picture space
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
        }

        [TearDown]
        public void TearDown()
        {
            MagnetTableScreen.UseDevicePlacement = true;
            if (_game != null) Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) Object.DestroyImmediate(_canvasObject);
        }

        private MagnetTableScreen ShowScreen()
        {
            _game.Navigator.Show(ScreenId.Magnet);
            var screen = _game.Navigator.GetScreen(ScreenId.Magnet) as MagnetTableScreen;
            Assert.IsNotNull(screen, "Magnet is registered on its own screen");
            return screen;
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static void Tick(MagnetTableScreen screen, float seconds)
        {
            for (var t = 0f; t < seconds; t += 1f / 60f) screen.Tick(1f / 60f);
        }

        private static void Grab(MagnetTableScreen screen) => screen.Magnet.OnBeginDrag(new PointerEventData(null));

        private static void LetGo(MagnetTableScreen screen) => screen.Magnet.OnEndDrag(new PointerEventData(null));

        // The magnet is held over thing `index` for a moment.
        private static void StickThing(MagnetTableScreen screen, int index)
        {
            Grab(screen);
            screen.PlaceMagnetPole(MagnetTableScreen.ThingPosition(index));
            Tick(screen, 0.5f);
        }

        private static int[] Indices(MagnetTableScreen screen, bool magnetic)
            => Enumerable.Range(0, screen.CurrentIds.Length).Where(i => MagnetTable.IsMagnetic(screen.CurrentIds[i]) == magnetic).ToArray();

        [Test]
        public void TheMagnetIsOneBigTapTargetAndTheOnlyThingToDrag()
        {
            var screen = ShowScreen();
            var items = screen.Root.GetComponentsInChildren<DragItem>(false);
            Assert.That(items.Length, Is.EqualTo(1));
            Assert.That(WorldRect(screen.Magnet.Rect).width, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
            Assert.That(WorldRect(screen.Magnet.Rect).height, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
        }

        [Test]
        public void SixThingsLieOnTheTableClearOfOneAnotherTheBackButtonAndTheTub()
        {
            var screen = ShowScreen();
            Assert.That(screen.Table.Things.Count, Is.EqualTo(MagnetTable.ThingsPerRound));
            Assert.That(Indices(screen, true).Length, Is.EqualTo(3));
            var boxes = Enumerable.Range(0, 6).Select(i => new Rect(MagnetTableScreen.ThingPosition(i) - Vector2.one * MagnetTableScreen.ThingSize * 0.5f,
                Vector2.one * MagnetTableScreen.ThingSize)).ToArray();
            for (var i = 0; i < 6; i++)
            {
                Assert.That(boxes[i].yMax, Is.LessThanOrEqualTo(170f), "under the Back button, thing " + i);
                for (var j = i + 1; j < 6; j++) Assert.That(boxes[i].Overlaps(boxes[j]), Is.False, i + " / " + j);
            }
            var tub = new Rect(690f - MagnetTableScreen.TubWidth * 0.5f, -120f, MagnetTableScreen.TubWidth, 170f);
            foreach (var box in boxes) Assert.That(box.xMax, Is.LessThanOrEqualTo(tub.xMin), "left of the tub");
            Assert.That(tub.yMax, Is.LessThanOrEqualTo(300f), "under the coin counter (y 300..450)");
            Assert.That(tub.yMin, Is.GreaterThanOrEqualTo(CompanionLayout.Corner.Footprint.YMax - 140f), "the pair in the corner stands in front of the table");
        }

        [Test]
        public void NothingWigglesBeforeTheMagnetIsFirstTouched()
        {
            var screen = ShowScreen();
            Tick(screen, 2f);
            foreach (var thing in screen.Table.Things) Assert.That(thing.Pull, Is.EqualTo(0f));
        }

        [Test]
        public void AMagnetHeldOverAMagneticThingPicksItUp()
        {
            var screen = ShowScreen();
            var index = Indices(screen, true)[0];
            StickThing(screen, index);
            Assert.That(screen.Table.Things[index].State, Is.EqualTo(MagnetThingState.Stuck));
            Assert.That(screen.Table.StuckCount, Is.EqualTo(1));
        }

        [Test]
        public void AMagnetHeldOverAThingItDoesNotPullLeavesItAlone()
        {
            var screen = ShowScreen();
            var index = Indices(screen, false)[0];
            StickThing(screen, index);
            Tick(screen, 3f);
            Assert.That(screen.Table.Things[index].State, Is.EqualTo(MagnetThingState.OnTable));
            Assert.That(screen.Table.StuckCount, Is.EqualTo(0));
        }

        [Test]
        public void AStuckThingTravelsWithTheMagnet()
        {
            var screen = ShowScreen();
            var index = Indices(screen, true)[0];
            StickThing(screen, index);
            Tick(screen, 0.5f); // the jump is over
            var thing = screen.Root.Find("Things/Thing" + index) as RectTransform;
            Assert.IsNotNull(thing);
            var before = thing.anchoredPosition;
            screen.PlaceMagnetPole(screen.Pole + new Vector2(150f, 60f));
            Tick(screen, 0.1f);
            Assert.That(thing.anchoredPosition.x - before.x, Is.EqualTo(150f).Within(2f));
            Assert.That(thing.anchoredPosition.y - before.y, Is.EqualTo(60f).Within(2f));
        }

        [Test]
        public void LettingGoAwayFromTheTubKeepsWhatIsStuck()
        {
            var screen = ShowScreen();
            StickThing(screen, Indices(screen, true)[0]);
            LetGo(screen);
            Tick(screen, 1f);
            Assert.That(screen.Table.StuckCount, Is.EqualTo(1));
            Assert.That(screen.Table.InBucketCount, Is.EqualTo(0));
        }

        [Test]
        public void LettingGoOverTheTubDropsEverythingStuckInAndAllThreeFinishTheRound()
        {
            var screen = ShowScreen();
            foreach (var index in Indices(screen, true)) StickThing(screen, index);
            Assert.That(screen.Table.StuckCount, Is.EqualTo(3));
            Assert.That(screen.Table.Done, Is.False);
            screen.PlaceMagnetPole(new Vector2(690f, 120f));
            LetGo(screen);
            Assert.That(screen.Table.StuckCount, Is.EqualTo(0));
            Assert.That(screen.Table.InBucketCount, Is.EqualTo(3));
            Assert.That(screen.Table.Done, Is.True);
            Tick(screen, 1f);
            Assert.That(screen.Root.Find("Things/Thing" + Indices(screen, true)[0]).gameObject.activeSelf, Is.False, "what fell in is gone from the table");
        }

        [Test]
        public void TheMagnetCannotBeDraggedOffTheScreenOrIntoTheTable()
        {
            var screen = ShowScreen();
            Grab(screen);
            screen.Magnet.Rect.anchoredPosition = new Vector2(5000f, -5000f);
            Tick(screen, 0.1f);
            var position = screen.Magnet.Rect.anchoredPosition;
            Assert.That(position.x, Is.LessThanOrEqualTo(800f));
            Assert.That(position.y, Is.GreaterThanOrEqualTo(-100f));
        }

        // The idle hint hand must never swallow a child's drag: grabbing the magnet cancels the hint.
        [Test]
        public void GrabbingTheMagnetWhileTheHintHandIsOutCancelsTheHint()
        {
            var screen = ShowScreen();
            Tick(screen, 12.5f);
            Assert.That(screen.Hinting, Is.True, "the idle hint started");
            Grab(screen);
            Assert.That(screen.Hinting, Is.False);
        }

        [Test]
        public void ThePicturesAndSoundsAreThere()
        {
            Assert.IsNotNull(Resources.Load<Sprite>("Art/world/magnet_bg"));
            Assert.IsNotNull(Resources.Load<Sprite>("Art/sciencelab/horseshoe_magnet"));
            Assert.IsNotNull(Resources.Load<Sprite>("Art/sciencelab/bucket_magnetic"));
            Assert.IsNotNull(Resources.Load<AudioClip>("Sfx/clink"));
        }
    }
}
