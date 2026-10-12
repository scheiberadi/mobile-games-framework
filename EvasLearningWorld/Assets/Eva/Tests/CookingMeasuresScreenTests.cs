using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    // The Cooking Measures screen: every ingredient of the recipe stands on the counter and goes into the bowl in any order. A pitcher over
    // the cup pours and the cup over the bowl is tipped in (by position, Tick); letting go judges; eggs and sugar dropped on the bowl are in
    // it; a finished step is ticked in the book. Coroutines (voice, hand) do not advance in Edit Mode, but the level and the rules are set
    // synchronously.
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

        // Holds the pitcher of this step over the cup for this long, then lets go.
        private static void Pour(CookingMeasuresScreen screen, int step, float seconds)
        {
            screen.SourceDrag(step).OnBeginDrag(new PointerEventData(null));
            screen.PlaceSource(step, CookingMeasuresScreen.PourPoint);
            for (var t = 0f; t < seconds; t += 0.05f) screen.Tick(0.05f);
            screen.SourceDrag(step).OnEndDrag(new PointerEventData(null));
        }

        // Holds the cup over the bowl for this long, then lets go.
        private static void TipIn(CookingMeasuresScreen screen, float seconds)
        {
            screen.CupDrag.OnBeginDrag(new PointerEventData(null));
            screen.PlaceCup(CookingMeasuresScreen.BowlTipPoint);
            for (var t = 0f; t < seconds; t += 0.05f) screen.Tick(0.05f);
            screen.CupDrag.OnEndDrag(new PointerEventData(null));
        }

        private static void DropInBowl(CookingMeasuresScreen screen, int step)
        {
            screen.SourceDrag(step).OnBeginDrag(new PointerEventData(null));
            screen.PlaceSource(step, CookingMeasuresScreen.BowlPoint);
            screen.SourceDrag(step).OnEndDrag(new PointerEventData(null));
        }

        private static int CupStep(CookingMeasuresScreen screen, int which) =>
            screen.Rounds.Select((r, i) => new { r, i }).Where(x => !x.r.IsAdd).ElementAt(which).i;

        private static int AddStep(CookingMeasuresScreen screen, int which) =>
            screen.Rounds.Select((r, i) => new { r, i }).Where(x => x.r.IsAdd).ElementAt(which).i;

        private static float SecondsFor(CookRound round) => round.Target == CupLevel.Half ? 1.6f : 4f;

        [Test]
        public void EveryStepOfTheRecipeIsInTheBookAndItsIngredientStandsOnTheCounter()
        {
            var screen = ShowScreen();
            Assert.That(screen.Recipe, Is.Not.Null);
            Assert.That(screen.Rounds.Length, Is.EqualTo(screen.Recipe.Steps.Length));
            for (var i = 0; i < screen.Rounds.Length; i++)
            {
                Assert.That(screen.SourceShown(i), "ingredient " + i);
                Assert.That(screen.Ticked(i), Is.False);
                Assert.That(screen.Rounds[i].Done, Is.False);
            }
            Assert.That(screen.InTheBowl, Is.EqualTo(0));
        }

        [Test]
        public void EveryPieceIsAFullSizeTapTargetClearOfTheBackButton()
        {
            var screen = ShowScreen();
            var drags = screen.Root.GetComponentsInChildren<DragItem>(false);
            Assert.That(drags.Length, Is.EqualTo(screen.Rounds.Length + 1), "the ingredients and the cup");
            foreach (var drag in drags)
            {
                var rect = WorldRect(drag.Rect);
                Assert.That(rect.width, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
                Assert.That(rect.height, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
                Assert.That(rect.Overlaps(new Rect(-690f, 175f, 240f, 240f)), Is.False, "under the Back button");
            }
        }

        [Test]
        public void APitcherOverTheCupRaisesTheLevelAndHomeDoesNot()
        {
            var screen = ShowScreen();
            var step = CupStep(screen, 0);
            for (var i = 0; i < 20; i++) screen.Tick(0.05f);
            Assert.That(screen.Rounds[step].Level, Is.EqualTo(0f), "nothing pours while the pitcher is home");
            screen.SourceDrag(step).OnBeginDrag(new PointerEventData(null));
            screen.PlaceSource(step, CookingMeasuresScreen.PourPoint);
            for (var i = 0; i < 20; i++) screen.Tick(0.05f);
            Assert.That(screen.Pouring);
            Assert.That(screen.Rounds[step].Level, Is.GreaterThan(0.1f));
            screen.SourceDrag(step).OnEndDrag(new PointerEventData(null));
        }

        [Test]
        public void TheChildMayStartWithTheSecondCupStepAndTippingTheRightLevelPutsItInTheBowl()
        {
            var screen = ShowScreen();
            var step = CupStep(screen, 1);
            var round = screen.Rounds[step];
            Pour(screen, step, SecondsFor(round));
            Assert.That(screen.CupRound, Is.SameAs(round));
            Assert.That(round.CupReady, "the cup holds " + round.Level + " for " + round.Target);
            Assert.That(screen.InTheBowl, Is.EqualTo(0));
            TipIn(screen, 3f);
            Assert.That(round.InBowl, Is.EqualTo(1));
            Assert.That(screen.InTheBowl, Is.EqualTo(1), "a tiny cup joins the pile in the bowl");
        }

        [Test]
        public void ALittleBitIsAMissAndNothingIsLost()
        {
            var screen = ShowScreen();
            var step = CupStep(screen, 0);
            Pour(screen, step, 0.3f);
            Assert.That(screen.Rounds[step].CupReady, Is.False);
            Assert.That(screen.Rounds[step].Done, Is.False);
            Assert.That(screen.Rounds[step].TotalWrongs, Is.EqualTo(1));
        }

        [Test]
        public void ACupWithOneIngredientDoesNotTakeAnother()
        {
            var screen = ShowScreen();
            var first = CupStep(screen, 0);
            var second = CupStep(screen, 1);
            Pour(screen, first, 0.5f);
            for (var i = 0; i < 20; i++) screen.Tick(0.05f); // the pitcher goes home
            var level = screen.Rounds[first].Level;
            Assert.That(level, Is.GreaterThan(CookingMeasure.EmptyTo));
            Pour(screen, second, 2f);
            Assert.That(screen.Rounds[second].Level, Is.EqualTo(0f), "the cup is busy");
            Assert.That(screen.CupRound, Is.SameAs(screen.Rounds[first]));
            Assert.That(screen.Rounds[first].Level, Is.EqualTo(level).Within(0.001f));
        }

        [Test]
        public void AnEggOrSugarGoesIntoTheBowlAtAnyTimeAndOnlyOverTheBowl()
        {
            var screen = ShowScreen();
            var step = AddStep(screen, 1);
            screen.SourceDrag(step).OnBeginDrag(new PointerEventData(null));
            screen.PlaceSource(step, new Vector2(120f, 300f));
            screen.SourceDrag(step).OnEndDrag(new PointerEventData(null));
            Assert.That(screen.Rounds[step].InBowl, Is.EqualTo(0), "away from the bowl nothing goes in");
            DropInBowl(screen, step);
            Assert.That(screen.Rounds[step].InBowl, Is.EqualTo(1));
            Assert.That(screen.InTheBowl, Is.EqualTo(1));
        }

        [Test]
        public void AFinishedStepIsTickedInTheBookAndItsIngredientIsGone()
        {
            var screen = ShowScreen();
            var step = AddStep(screen, 1);
            for (var i = 0; i < screen.Rounds[step].Count; i++) DropInBowl(screen, step);
            Assert.That(screen.Rounds[step].Done);
            screen.Tick(0.05f);
            Assert.That(screen.Ticked(step));
            Assert.That(screen.SourceShown(step), Is.False);
            for (var i = 0; i < screen.Rounds.Length; i++) if (i != step) Assert.That(screen.Ticked(i), Is.False);
        }

        [Test]
        public void ANewSessionBringsTheCupBackAfterTheDishClearedTheCounter()
        {
            var screen = ShowScreen();
            screen.CupDrag.gameObject.SetActive(false); // what the end of a session does
            _game.Navigator.Show(ScreenId.ScienceLab);
            _game.Navigator.Show(ScreenId.CookingMeasures);
            Assert.That(screen.CupDrag.gameObject.activeSelf);
            for (var i = 0; i < screen.Rounds.Length; i++) Assert.That(screen.SourceShown(i));
        }
    }
}
