using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    // The Weather "Make the weather" screen: the tray and what a drop does to the village. Coroutines (voice, hand) do not advance in
    // Edit Mode, but the rules and the village targets are set synchronously on a drop, which these tests do by hand.
    public class MakeWeatherScreenTests
    {
        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            MakeWeatherScreen.UseDevicePlacement = false; // work in picture space
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
        }

        [TearDown]
        public void TearDown()
        {
            MakeWeatherScreen.UseDevicePlacement = true;
            if (_game != null) Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) Object.DestroyImmediate(_canvasObject);
        }

        private MakeWeatherScreen ShowScreen()
        {
            _game.Navigator.Show(ScreenId.Weather);
            var screen = _game.Navigator.GetScreen(ScreenId.Weather) as MakeWeatherScreen;
            Assert.IsNotNull(screen, "Weather is registered on its own screen");
            return screen;
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        // The piece is dragged into the sky and let go.
        private static void Drop(MakeWeatherScreen screen, SkyPiece piece)
        {
            var drag = screen.PieceDrags[(int)piece];
            drag.OnBeginDrag(new PointerEventData(null));
            screen.PlacePiece((int)piece, MakeWeatherScreen.SkyPoint);
            drag.OnEndDrag(new PointerEventData(null));
        }

        [Test]
        public void SixPiecesAreTheOnlyThingsToDragAndEachIsAFullSizeTapTargetClearOfTheOthers()
        {
            var screen = ShowScreen();
            var drags = screen.Root.GetComponentsInChildren<DragItem>(false);
            Assert.That(drags.Length, Is.EqualTo(6));
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
        public void ARoundStartsOnAPlainDay()
        {
            var screen = ShowScreen();
            Assert.That(screen.Round, Is.Not.Null);
            Assert.That(screen.Round.Made, Is.Null);
            Assert.That(screen.ShownWeather, Is.EqualTo(WeatherKind.Sunny));
        }

        [Test]
        public void ThePiecesLetGoInTheSkyChangeTheVillage()
        {
            var screen = ShowScreen();
            Drop(screen, SkyPiece.RainCloud);
            Assert.That(screen.RainFalling);
            Assert.That(screen.ShownWeather, Is.EqualTo(WeatherKind.Rainy));
            Drop(screen, SkyPiece.Snowflake);
            Assert.That(screen.SnowFalling);
            Assert.That(screen.RainFalling, Is.False);
            Assert.That(screen.ShownWeather, Is.EqualTo(WeatherKind.Snowy));
            Drop(screen, SkyPiece.Sun);
            Assert.That(screen.SnowFalling, Is.False);
            Assert.That(screen.ShownWeather, Is.EqualTo(WeatherKind.Sunny));
            screen.Tick(2f);
        }

        [Test]
        public void APieceLetGoOnTheTrayIsNotDropped()
        {
            var screen = ShowScreen();
            var drag = screen.PieceDrags[0];
            drag.OnBeginDrag(new PointerEventData(null));
            screen.PlacePiece(0, new Vector2(0f, -330f));
            drag.OnEndDrag(new PointerEventData(null));
            Assert.That(screen.Round.Made, Is.Null);
            Assert.That(screen.Round.TotalWrongs, Is.EqualTo(0));
        }

        [Test]
        public void TheRightPieceFinishesTheRoundAndAWrongOneDoesNot()
        {
            var screen = ShowScreen();
            var right = MakeWeather.PieceFor(screen.Round.Asked);
            var wrong = (SkyPiece)(((int)right + 1) % 6);
            Drop(screen, wrong);
            Assert.That(screen.Round.Done, Is.False);
            Assert.That(screen.Round.TotalWrongs, Is.EqualTo(1));
            Drop(screen, right);
            Assert.That(screen.Round.Done);
        }

        [Test]
        public void TwoWrongPiecesInARowStartTheHintWhichStartsTheCountAgain()
        {
            var screen = ShowScreen();
            var wrong = (SkyPiece)(((int)MakeWeather.PieceFor(screen.Round.Asked) + 1) % 6);
            Drop(screen, wrong);
            Drop(screen, wrong);
            Assert.That(screen.Round.Wrongs, Is.EqualTo(0), "the hint was started and the count began again");
        }
    }
}
