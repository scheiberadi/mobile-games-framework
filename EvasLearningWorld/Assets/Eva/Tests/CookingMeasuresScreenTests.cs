using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    // The Cooking Measures screen: the pitcher pours and the cup tips into the bowl by position (Tick), letting go judges, eggs and sugar
    // dropped on the bowl are in it. Coroutines (voice, hand) do not advance in Edit Mode, but the level and the rules are set synchronously.
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

        // Holds the cup over the bowl for this long, then lets go.
        private static void TipIn(CookingMeasuresScreen screen, float seconds)
        {
            screen.CupDrag.OnBeginDrag(new PointerEventData(null));
            screen.PlaceCup(CookingMeasuresScreen.BowlTipPoint);
            for (var t = 0f; t < seconds; t += 0.05f) screen.Tick(0.05f);
            screen.CupDrag.OnEndDrag(new PointerEventData(null));
        }

        [Test]
        public void ARecipeStartsWithItsFirstCupStepAndTheBowlIsEmpty()
        {
            var screen = ShowScreen();
            Assert.That(screen.Recipe, Is.Not.Null);
            Assert.That(screen.Round.IsAdd, Is.False);
            Assert.That(screen.Round.Step, Is.SameAs(screen.Recipe.Steps[0]));
            Assert.That(screen.Round.Level, Is.EqualTo(0f));
            Assert.That(screen.InTheBowl, Is.EqualTo(0));
        }

        [Test]
        public void ACupStepHasThePitcherAndTheCupToDragAndEachIsAFullSizeTapTargetClearOfTheOther()
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
        public void ThePitcherOverTheCupRaisesTheLevelAndHomeDoesNot()
        {
            var screen = ShowScreen();
            var round = screen.Round;
            for (var i = 0; i < 20; i++) screen.Tick(0.05f);
            Assert.That(round.Level, Is.EqualTo(0f), "nothing pours while the pitcher is home");
            screen.PitcherDrag.OnBeginDrag(new PointerEventData(null));
            screen.PlacePitcher(CookingMeasuresScreen.PourPoint);
            for (var i = 0; i < 20; i++) screen.Tick(0.05f);
            Assert.That(screen.Pouring);
            Assert.That(round.Level, Is.GreaterThan(0.1f));
            screen.PitcherDrag.OnEndDrag(new PointerEventData(null));
        }

        [Test]
        public void TheRightLevelMakesTheCupReadyAndTippingItInPutsItInTheBowl()
        {
            var screen = ShowScreen();
            var round = screen.Round;
            var seconds = round.Target == CupLevel.Half ? 1.6f : 4f;
            Pour(screen, seconds);
            Assert.That(round.CupReady, "the cup holds " + round.Level + " for " + round.Target);
            Assert.That(screen.InTheBowl, Is.EqualTo(0));
            TipIn(screen, 3f);
            Assert.That(screen.Tipping || round.Level == 0f);
            Assert.That(round.InBowl, Is.EqualTo(1));
            Assert.That(screen.InTheBowl, Is.EqualTo(1), "a tiny cup joins the row in front of the bowl");
        }

        [Test]
        public void ALittleBitIsAMissAndTheRoundGoesOn()
        {
            var screen = ShowScreen();
            var round = screen.Round;
            Pour(screen, 0.3f);
            Assert.That(round.CupReady, Is.False);
            Assert.That(round.Done, Is.False);
            Assert.That(round.TotalWrongs, Is.EqualTo(1));
        }

        [Test]
        public void AnAddStepHasOnlyTheEggOrSugarToDragAndItGoesIntoTheBowl()
        {
            var screen = ShowScreen();
            var index = System.Array.FindIndex(screen.Recipe.Steps, st => st.Kind == CookStepKind.Add);
            screen.ShowStep(index);
            Assert.That(screen.Round.IsAdd);
            Assert.That(screen.Root.GetComponentsInChildren<DragItem>(false).Length, Is.EqualTo(1));
            var drag = screen.ItemDrag;
            drag.OnBeginDrag(new PointerEventData(null));
            screen.PlaceItem(new Vector2(120f, 300f));
            drag.OnEndDrag(new PointerEventData(null));
            Assert.That(screen.Round.InBowl, Is.EqualTo(0), "away from the bowl nothing goes in");
            drag.OnBeginDrag(new PointerEventData(null));
            screen.PlaceItem(CookingMeasuresScreen.BowlPoint);
            drag.OnEndDrag(new PointerEventData(null));
            Assert.That(screen.Round.InBowl, Is.EqualTo(1));
            Assert.That(screen.InTheBowl, Is.EqualTo(1));
        }

        [Test]
        public void EveryAddStepFinishesWhenAllItsEggsOrSpoonsAreInTheBowl()
        {
            var screen = ShowScreen();
            var index = System.Array.FindIndex(screen.Recipe.Steps, st => st.Kind == CookStepKind.Add);
            screen.ShowStep(index);
            for (var i = 0; i < screen.Round.Count; i++)
            {
                screen.ItemDrag.OnBeginDrag(new PointerEventData(null));
                screen.PlaceItem(CookingMeasuresScreen.BowlPoint);
                screen.ItemDrag.OnEndDrag(new PointerEventData(null));
            }
            Assert.That(screen.Round.Done);
        }
    }
}
