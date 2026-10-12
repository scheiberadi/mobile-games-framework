using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    // Living vs Non-living's "Wake up!" screen: the bench, the basket, the star and what wakes. Coroutines (voice, hand, round end) do not
    // advance in Edit Mode, but the bench's logic runs from Tick, which these tests call by hand.
    public class WakeUpScreenTests
    {
        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            WakeUpScreen.UseDevicePlacement = false; // work in picture space
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
        }

        [TearDown]
        public void TearDown()
        {
            WakeUpScreen.UseDevicePlacement = true;
            if (_game != null) Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) Object.DestroyImmediate(_canvasObject);
        }

        private WakeUpScreen ShowScreen()
        {
            _game.Navigator.Show(ScreenId.LivingVsNonLiving);
            var screen = _game.Navigator.GetScreen(ScreenId.LivingVsNonLiving) as WakeUpScreen;
            Assert.IsNotNull(screen, "Living vs Non-living is registered on its own screen");
            return screen;
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static void Tick(WakeUpScreen screen, float seconds)
        {
            for (var t = 0f; t < seconds; t += 1f / 60f) screen.Tick(1f / 60f);
        }

        private static void Grab(WakeUpScreen screen) => screen.Star.OnBeginDrag(new PointerEventData(null));

        // The star is held on thing `index` for a moment.
        private static void WakeThing(WakeUpScreen screen, int index)
        {
            Grab(screen);
            screen.PlaceStar(WakeUpScreen.ThingPosition(index));
            Tick(screen, 0.8f);
        }

        private static int[] Indices(WakeUpScreen screen, bool alive)
            => Enumerable.Range(0, screen.CurrentIds.Length).Where(i => WakeUp.IsAlive(screen.CurrentIds[i]) == alive).ToArray();

        [Test]
        public void TheStarIsOneBigTapTargetAndTheOnlyThingToDrag()
        {
            var screen = ShowScreen();
            var items = screen.Root.GetComponentsInChildren<DragItem>(false);
            Assert.That(items.Length, Is.EqualTo(1));
            Assert.That(WorldRect(screen.Star.Rect).width, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
            Assert.That(WorldRect(screen.Star.Rect).height, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
        }

        [Test]
        public void SixThingsSitOnTheBenchClearOfOneAnotherTheBackButtonAndTheBasket()
        {
            var screen = ShowScreen();
            Assert.That(screen.Table.Things.Count, Is.EqualTo(WakeUp.ThingsPerRound));
            Assert.That(Indices(screen, true).Length, Is.EqualTo(3));
            var boxes = Enumerable.Range(0, 6).Select(i => new Rect(WakeUpScreen.ThingPosition(i) - Vector2.one * WakeUpScreen.ThingSize * 0.5f,
                Vector2.one * WakeUpScreen.ThingSize)).ToArray();
            for (var i = 0; i < 6; i++)
            {
                Assert.That(boxes[i].yMax, Is.LessThanOrEqualTo(170f), "under the Back button, thing " + i);
                for (var j = i + 1; j < 6; j++) Assert.That(boxes[i].Overlaps(boxes[j]), Is.False, i + " / " + j);
            }
            var basket = new Rect(700f - WakeUpScreen.BasketWidth * 0.5f, -250f, WakeUpScreen.BasketWidth, 250f);
            foreach (var box in boxes) Assert.That(box.xMax, Is.LessThanOrEqualTo(basket.xMin), "left of the basket");
        }

        [Test]
        public void NothingWakesBeforeTheStarIsFirstTouched()
        {
            var screen = ShowScreen();
            Tick(screen, 2f);
            foreach (var thing in screen.Table.Things) Assert.That(thing.State, Is.EqualTo(WakeState.Asleep));
        }

        [Test]
        public void TheStarHeldOnALivingThingWakesIt()
        {
            var screen = ShowScreen();
            var index = Indices(screen, true)[0];
            WakeThing(screen, index);
            Assert.That(screen.Table.Things[index].State, Is.Not.EqualTo(WakeState.Asleep));
        }

        [Test]
        public void TheStarHeldOnAThingThatIsNotAliveLeavesItAsleep()
        {
            var screen = ShowScreen();
            var index = Indices(screen, false)[0];
            WakeThing(screen, index);
            Tick(screen, 3f);
            Assert.That(screen.Table.Things[index].State, Is.EqualTo(WakeState.Asleep));
            Assert.That(screen.Table.InBasketCount, Is.EqualTo(0));
        }

        [Test]
        public void WakingAllThreeLivingThingsSendsThemToTheBasketAndFinishesTheRound()
        {
            var screen = ShowScreen();
            foreach (var index in Indices(screen, true)) WakeThing(screen, index);
            Tick(screen, 3f);
            Assert.That(screen.Table.InBasketCount, Is.EqualTo(3));
            Assert.That(screen.Table.Done);
            Assert.That(screen.Root.Find("Things/Thing" + Indices(screen, true)[0]).gameObject.activeSelf, Is.False, "what hopped in is gone from the bench");
            foreach (var index in Indices(screen, false)) Assert.That(screen.Root.Find("Things/Thing" + index).gameObject.activeSelf, "the sleepers stay");
        }

        [Test]
        public void TheStarCannotBeDraggedOffTheScreenOrIntoTheBench()
        {
            var screen = ShowScreen();
            Grab(screen);
            screen.Star.Rect.anchoredPosition = new Vector2(5000f, -5000f);
            Tick(screen, 0.1f);
            var position = screen.Star.Rect.anchoredPosition;
            Assert.That(position.x, Is.LessThanOrEqualTo(800f));
            Assert.That(position.y, Is.GreaterThanOrEqualTo(-300f));
        }

        // The idle hint hand must never swallow a child's drag: grabbing the star cancels the hint.
        [Test]
        public void GrabbingTheStarWhileTheHintHandIsOutCancelsTheHint()
        {
            var screen = ShowScreen();
            Tick(screen, 12.5f);
            Assert.That(screen.Hinting, Is.True, "the idle hint started");
            Grab(screen);
            Assert.That(screen.Hinting, Is.False);
        }

        [Test]
        public void ThePicturesAreThere()
        {
            Assert.IsNotNull(Resources.Load<Sprite>("Art/world/sciencelab_bg"));
            Assert.IsNotNull(Resources.Load<Sprite>("Art/sciencelab/feather"));
            Assert.IsNotNull(Resources.Load<Sprite>("Art/sciencelab/basket_plain"));
        }

        [Test]
        public void EveryThingHasItsVoiceLine()
        {
            var keys = System.IO.File.ReadLines(System.IO.Path.Combine(Application.dataPath, "Eva/Resources/Voice/voice-lines.txt"))
                .Select(l => l.Split('\t')[0]).ToHashSet();
            foreach (var item in WakeUp.Items) Assert.That(keys.Contains((item.Alive ? "wake_say_" : "wake_no_") + item.Id), item.Id);
            Assert.That(keys.Contains("wake_prompt") && keys.Contains("wake_hint"));
        }
    }
}
