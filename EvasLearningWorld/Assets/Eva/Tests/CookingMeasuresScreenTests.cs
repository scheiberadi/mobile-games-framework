using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    // The Cooking Measures screen: the pitcher pours and the cup tips out by position (Tick), and letting go judges. Coroutines (voice, hand)
    // do not advance in Edit Mode, but the level and the rules are set synchronously.
    public class CookingMeasuresScreenTests
    {
        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            CookingMeasuresScreen.UseDevicePlacement = false; // work in picture space
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
        }

        [TearDown]
        public void TearDown()
        {
            CookingMeasuresScreen.UseDevicePlacement = true;
            if (_game != null) UnityEngine.Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) UnityEngine.Object.DestroyImmediate(_canvasObject);
        }

        private CookingMeasuresScreen ShowScreen()
        {
            _game.Navigator.Show(ScreenId.CookingMeasures);
            var screen = _game.Navigator.GetScreen(ScreenId.CookingMeasures) as CookingMeasuresScreen;
            Assert.IsNotNull(screen, "Cooking Measures is registered on its own screen");
            return screen;
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        // Holds the pitcher over the cup for this long, then lets go.
        private static void Pour(CookingMeasuresScreen screen, float seconds)
        {
            screen.PitcherDrag.OnBeginDrag(new PointerEventData(null));
            screen.PlacePitcher(CookingMeasuresScreen.PourPoint);
            for (var t = 0f; t < seconds; t += 0.05f) screen.Tick(0.05f);
            screen.PitcherDrag.OnEndDrag(new PointerEventData(null));
        }

        private static void TipOut(CookingMeasuresScreen screen, float seconds)
        {
            screen.CupDrag.OnBeginDrag(new PointerEventData(null));
            screen.PlaceCup(CookingMeasuresScreen.TipPoint);
            for (var t = 0f; t < seconds; t += 0.05f) screen.Tick(0.05f);
            screen.CupDrag.OnEndDrag(new PointerEventData(null));
        }

        [Test]
        public void TwoPiecesAreTheOnlyThingsToDragAndEachIsAFullSizeTapTargetClearOfTheOther()
        {
            var screen = ShowScreen();
            var drags = screen.Root.GetComponentsInChildren<DragItem>(false);
            Assert.That(drags.Length, Is.EqualTo(2));
            var rects = drags.Select(d => WorldRect(d.Rect)).ToArray();
            foreach (var rect in rects)
            {
                Assert.That(rect.width, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
                Assert.That(rect.height, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
                Assert.That(rect.Overlaps(new Rect(-690f, 175f, 240f, 240f)), Is.False, "under the Back button");
            }
            Assert.That(rects[0].Overlaps(rects[1]), Is.False);
        }

        [Test]
        public void ARoundStartsWithTheCupAtTheStartLevelOfItsRecipe()
        {
            var screen = ShowScreen();
            var round = screen.Round;
            Assert.That(round.Level, Is.EqualTo(round.Target == CupLevel.Empty ? 1f : 0f));
            Assert.That(round.Done, Is.False);
        }

        [Test]
        public void ThePitcherOverTheCupRaisesTheLevelAndHomeDoesNot()
        {
            var screen = ShowScreen();
            var round = screen.Round;
            var start = round.Level;
            for (var i = 0; i < 20; i++) screen.Tick(0.05f);
            Assert.That(round.Level, Is.EqualTo(start), "nothing pours while the pitcher is home");
            screen.PitcherDrag.OnBeginDrag(new PointerEventData(null));
            screen.PlacePitcher(CookingMeasuresScreen.PourPoint);
            for (var i = 0; i < 20; i++) screen.Tick(0.05f);
            Assert.That(screen.Pouring);
            Assert.That(round.Level, Is.GreaterThan(start + 0.1f).Or.EqualTo(1f));
            screen.PitcherDrag.OnEndDrag(new PointerEventData(null));
        }

        [Test]
        public void TheCupAwayFromHomeIsTippedOut()
        {
            var screen = ShowScreen();
            var round = screen.Round;
            Pour(screen, 1f);
            Assert.That(round.Level, Is.GreaterThan(0.2f));
            var before = round.Level;
            screen.CupDrag.OnBeginDrag(new PointerEventData(null));
            screen.PlaceCup(CookingMeasuresScreen.TipPoint);
            for (var i = 0; i < 10; i++) screen.Tick(0.05f);
            Assert.That(screen.Tipping);
            Assert.That(round.Level, Is.LessThan(before));
            screen.CupDrag.OnEndDrag(new PointerEventData(null));
        }

        [Test]
        public void LettingGoOfAFullCupFinishesAFullRound()
        {
            var screen = ShowScreen();
            var round = screen.Round;
            if (round.Target == CupLevel.Empty) TipOut(screen, 3f);
            else Pour(screen, round.Target == CupLevel.Full ? 4f : 1.6f);
            Assert.That(round.Done, "the level " + round.Level + " matches " + round.Target);
        }

        [Test]
        public void ALittleBitIsAMissAndTheRoundGoesOn()
        {
            var screen = ShowScreen();
            var round = screen.Round;
            if (round.Target == CupLevel.Empty) TipOut(screen, 0.3f);
            else Pour(screen, 0.3f);
            Assert.That(round.Done, Is.False);
            Assert.That(round.TotalWrongs, Is.EqualTo(1));
        }
    }
}
