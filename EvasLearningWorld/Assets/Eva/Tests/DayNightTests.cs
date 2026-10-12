using System;
using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    public class DayNightTests
    {
        [Test]
        public void ARoundHasTwoCardsOfTheAskedTimeAndTwoOfTheOther()
        {
            for (var seed = 0; seed < 60; seed++)
            {
                foreach (TimeOfDay time in Enum.GetValues(typeof(TimeOfDay)))
                {
                    var round = DayNight.CreateRound(time, new System.Random(seed));
                    Assert.That(round.Cards.Length, Is.EqualTo(DayNight.CardsPerRound));
                    Assert.That(round.Cards.Count(c => c.Time == time), Is.EqualTo(DayNight.RightPerRound));
                    Assert.That(round.Cards.Select(c => c.Id).Distinct().Count(), Is.EqualTo(DayNight.CardsPerRound));
                }
            }
        }

        [Test]
        public void ASessionHasThreeRoundsAndBothTimes()
        {
            for (var seed = 0; seed < 60; seed++)
            {
                var session = DayNight.CreateSession(new System.Random(seed));
                Assert.That(session.Length, Is.EqualTo(DayNight.RoundsPerSession));
                Assert.That(session.Select(r => r.Asked).Distinct().Count(), Is.EqualTo(2));
                Assert.That(session[0].Asked, Is.Not.EqualTo(session[1].Asked));
            }
        }

        [Test]
        public void TheRoundIsDoneWhenBothRightCardsArePlacedAndWrongOnesNeverCount()
        {
            var round = DayNight.CreateRound(TimeOfDay.Night, new System.Random(1));
            var right = Enumerable.Range(0, 4).Where(i => round.Cards[i].Time == TimeOfDay.Night).ToArray();
            var wrong = Enumerable.Range(0, 4).Where(i => round.Cards[i].Time == TimeOfDay.Day).ToArray();
            Assert.That(round.Drop(wrong[0]), Is.EqualTo(DayNightDrop.Wrong));
            Assert.That(round.PlacedCount, Is.EqualTo(0));
            Assert.That(round.Drop(right[0]), Is.EqualTo(DayNightDrop.Right));
            Assert.That(round.Drop(right[0]), Is.EqualTo(DayNightDrop.Again));
            Assert.That(round.Done, Is.False);
            Assert.That(round.NextRight(), Is.EqualTo(right[1]));
            Assert.That(round.Drop(right[1]), Is.EqualTo(DayNightDrop.Right));
            Assert.That(round.Done);
            Assert.That(round.NextRight(), Is.EqualTo(-1));
        }

        [Test]
        public void TwoWrongCardsInARowCallForAHintAndARightOneClearsTheCount()
        {
            var round = DayNight.CreateRound(TimeOfDay.Day, new System.Random(2));
            var right = Enumerable.Range(0, 4).First(i => round.Cards[i].Time == TimeOfDay.Day);
            var wrong = Enumerable.Range(0, 4).Where(i => round.Cards[i].Time == TimeOfDay.Night).ToArray();
            round.Drop(wrong[0]);
            Assert.That(round.NeedsHint, Is.False);
            round.Drop(right);
            round.Drop(wrong[0]);
            Assert.That(round.NeedsHint, Is.False, "the right card in between cleared the count");
            round.Drop(wrong[1]);
            Assert.That(round.NeedsHint);
            round.HintGiven();
            Assert.That(round.NeedsHint, Is.False);
            Assert.That(round.TotalWrongs, Is.EqualTo(3));
        }
    }

    // The Day/Night screen: the tray and what a drop on the meadow does. Coroutines (voice, flying, hand) do not advance in Edit Mode, but
    // the rules are set synchronously on a drop.
    public class DayNightScreenTests
    {
        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            DayNightScreen.UseDevicePlacement = false; // work in picture space
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
        }

        [TearDown]
        public void TearDown()
        {
            DayNightScreen.UseDevicePlacement = true;
            if (_game != null) UnityEngine.Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) UnityEngine.Object.DestroyImmediate(_canvasObject);
        }

        private DayNightScreen ShowScreen()
        {
            _game.Navigator.Show(ScreenId.DayNight);
            var screen = _game.Navigator.GetScreen(ScreenId.DayNight) as DayNightScreen;
            Assert.IsNotNull(screen, "Day/Night is registered on its own screen");
            return screen;
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static void Drop(DayNightScreen screen, int card, Vector2 point)
        {
            var drag = screen.CardDrags[card];
            drag.OnBeginDrag(new PointerEventData(null));
            screen.PlaceCard(card, point);
            drag.OnEndDrag(new PointerEventData(null));
        }

        private static void OnMeadow(DayNightScreen screen, int card) => Drop(screen, card, DayNightScreen.MeadowCentre);

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
        public void ARoundStartsInTheAskedTimeWithNothingPlaced()
        {
            var screen = ShowScreen();
            Assert.That(screen.Round, Is.Not.Null);
            Assert.That(screen.ShownTime, Is.EqualTo(screen.Round.Asked));
            Assert.That(screen.Round.PlacedCount, Is.EqualTo(0));
            Assert.That(screen.Round.Done, Is.False);
        }

        [Test]
        public void ACardLetGoOnTheTrayDoesNothingAndARightOneOnTheMeadowIsPlaced()
        {
            var screen = ShowScreen();
            var round = screen.Round;
            var right = Enumerable.Range(0, 4).First(i => round.Cards[i].Time == round.Asked);
            Drop(screen, right, new Vector2(0f, -330f));
            Assert.That(round.PlacedCount, Is.EqualTo(0));
            OnMeadow(screen, right);
            Assert.That(round.PlacedCount, Is.EqualTo(1));
            Assert.That(round.IsPlaced(right));
            Assert.That(round.Done, Is.False);
        }

        [Test]
        public void AWrongCardOnTheMeadowIsCountedButNeverPlaced()
        {
            var screen = ShowScreen();
            var round = screen.Round;
            var wrong = Enumerable.Range(0, 4).First(i => round.Cards[i].Time != round.Asked);
            OnMeadow(screen, wrong);
            Assert.That(round.PlacedCount, Is.EqualTo(0));
            Assert.That(round.TotalWrongs, Is.EqualTo(1));
        }

        [Test]
        public void BothRightCardsFinishTheRound()
        {
            var screen = ShowScreen();
            var round = screen.Round;
            foreach (var i in Enumerable.Range(0, 4).Where(i => round.Cards[i].Time == round.Asked).ToArray()) OnMeadow(screen, i);
            Assert.That(round.Done);
        }
    }
}
