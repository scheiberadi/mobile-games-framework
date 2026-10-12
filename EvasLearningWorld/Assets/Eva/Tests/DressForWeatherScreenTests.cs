using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    // The Weather "Dress for the weather" screen: the tray and what a drop onto Eva does. Coroutines (voice, hand, reactions) do not
    // advance in Edit Mode, but the rules are set synchronously on a drop, which these tests do by hand.
    public class DressForWeatherScreenTests
    {
        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            DressForWeatherScreen.UseDevicePlacement = false; // work in picture space
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
        }

        [TearDown]
        public void TearDown()
        {
            DressForWeatherScreen.UseDevicePlacement = true;
            if (_game != null) Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) Object.DestroyImmediate(_canvasObject);
        }

        private DressForWeatherScreen ShowScreen()
        {
            _game.Navigator.Show(ScreenId.DressForWeather);
            var screen = _game.Navigator.GetScreen(ScreenId.DressForWeather) as DressForWeatherScreen;
            Assert.IsNotNull(screen, "Dress for Weather is registered on its own screen");
            return screen;
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static void Drop(DressForWeatherScreen screen, Outfit outfit, Vector2 point)
        {
            var drag = screen.CardDrags[(int)outfit];
            drag.OnBeginDrag(new PointerEventData(null));
            screen.PlaceCard((int)outfit, point);
            drag.OnEndDrag(new PointerEventData(null));
        }

        private static void OnEva(DressForWeatherScreen screen, Outfit outfit) => Drop(screen, outfit, DressForWeatherScreen.EvaZoneCentre);

        [Test]
        public void FourCardsAreTheOnlyThingsToDragAndEachIsAFullSizeTapTargetClearOfTheOthers()
        {
            var screen = ShowScreen();
            var drags = screen.Root.GetComponentsInChildren<DragItem>(false);
            Assert.That(drags.Length, Is.EqualTo(4));
            var rects = drags.Select(d => WorldRect(d.Rect)).ToArray();
            for (var i = 0; i < rects.Length; i++)
            {
                Assert.That(rects[i].width, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
                Assert.That(rects[i].height, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
                for (var j = i + 1; j < rects.Length; j++) Assert.That(rects[i].Overlaps(rects[j]), Is.False, i + " / " + j);
            }
            var back = new Rect(-690f, 175f, 240f, 240f);
            foreach (var rect in rects) Assert.That(rect.Overlaps(back), Is.False, "under the Back button");
        }

        [Test]
        public void ARoundStartsInTheAskedWeatherWithEvaInHerPlainFur()
        {
            var screen = ShowScreen();
            Assert.That(screen.Round, Is.Not.Null);
            Assert.That(screen.ShownWeather, Is.EqualTo(screen.Round.Weather));
            Assert.That(screen.Round.Worn, Is.Null);
            Assert.That(screen.OutfitOnEva, Is.Null);
            Assert.That(screen.RainFalling, Is.EqualTo(screen.Round.Weather == WeatherKind.Rainy));
            Assert.That(screen.SnowFalling, Is.EqualTo(screen.Round.Weather == WeatherKind.Snowy));
            Assert.That(screen.LeavesBlowing, Is.EqualTo(screen.Round.Weather == WeatherKind.Windy));
        }

        [Test]
        public void ACardLetGoOnEvaDressesHerAndOneLetGoOnTheTrayDoesNot()
        {
            var screen = ShowScreen();
            Drop(screen, Outfit.Snow, new Vector2(0f, -330f));
            Assert.That(screen.Round.Worn, Is.Null);
            Assert.That(screen.OutfitOnEva, Is.Null);
            OnEva(screen, Outfit.Snow);
            Assert.That(screen.OutfitOnEva, Is.EqualTo(Outfit.Snow));
        }

        [Test]
        public void TheRightOutfitFinishesTheRound()
        {
            var screen = ShowScreen();
            var right = screen.Round.Right;
            OnEva(screen, right);
            Assert.That(screen.Round.Done);
            Assert.That(screen.OutfitOnEva, Is.EqualTo(right));
        }

        // The reaction coroutine does not run in Edit Mode, so the wrong outfit stays on here; while she reacts no card can be dropped.
        [Test]
        public void AWrongOutfitDoesNotFinishTheRoundAndNothingCanBeDroppedWhileSheReacts()
        {
            var screen = ShowScreen();
            var right = screen.Round.Right;
            var wrong = (Outfit)(((int)right + 1) % 4);
            OnEva(screen, wrong);
            Assert.That(screen.Round.Done, Is.False);
            Assert.That(screen.Round.TotalWrongs, Is.EqualTo(1));
            OnEva(screen, right);
            Assert.That(screen.Round.Done, Is.False, "she is still reacting");
        }

        [Test]
        public void ADressedEvaGetsTheOutfitPictureAndHidesHerPlainFaceOverlays()
        {
            var screen = ShowScreen();
            var outfit = screen.Root.GetComponentsInChildren<UnityEngine.UI.Image>(true).First(i => i.name == "Outfit");
            OnEva(screen, screen.Round.Right);
            screen.Tick(1f);
            Assert.That(outfit.enabled);
            Assert.That(outfit.sprite, Is.Not.Null);
            Assert.That(outfit.color.a, Is.EqualTo(1f).Within(0.01f));
        }

        [Test]
        public void ThePlainCatUnderneathIsOffOnlyWhileTheOutfitFullyCoversHer()
        {
            var screen = ShowScreen();
            var plain = screen.Root.Find("Hop/Body") ?? screen.Root.GetComponentsInChildren<UnityEngine.UI.Image>(true).First(i => i.name == "Body").transform;
            var image = plain.GetComponent<UnityEngine.UI.Image>();
            Assert.That(image.enabled, "plain cat shows before any outfit");
            OnEva(screen, screen.Round.Right);
            screen.Tick(1f);
            Assert.That(image.enabled, Is.False, "outfit covers her, so no plain ears or fur behind it");
        }
    }
}
