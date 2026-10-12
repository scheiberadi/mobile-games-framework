using System;
using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    public class SeasonsTests
    {
        [Test]
        public void ASessionAsksThreeDifferentSeasonsAndNeverStartsOnTheOneTheMeadowShows()
        {
            for (var seed = 0; seed < 60; seed++)
            {
                var session = Seasons.CreateSession(new System.Random(seed));
                Assert.That(session.Length, Is.EqualTo(Seasons.RoundsPerSession));
                Assert.That(session.Distinct().Count(), Is.EqualTo(session.Length));
                Assert.That(session[0], Is.Not.EqualTo(Seasons.Start));
            }
        }

        [Test]
        public void TheAskedSeasonFinishesTheRound()
        {
            var round = new SeasonRound(Season.Spring, Season.Summer);
            Assert.That(round.Drop(Season.Spring), Is.True);
            Assert.That(round.Done);
            Assert.That(round.Shown, Is.EqualTo(Season.Spring));
        }

        [Test]
        public void AnyOtherSeasonIsShownAndCountedAsWrong()
        {
            var round = new SeasonRound(Season.Spring, Season.Summer);
            Assert.That(round.Drop(Season.Winter), Is.False);
            Assert.That(round.Shown, Is.EqualTo(Season.Winter));
            Assert.That(round.Done, Is.False);
            Assert.That(round.Wrongs, Is.EqualTo(1));
            Assert.That(round.NeedsHint, Is.False);
            round.Drop(Season.Autumn);
            Assert.That(round.NeedsHint);
            round.HintGiven();
            Assert.That(round.NeedsHint, Is.False);
            Assert.That(round.TotalWrongs, Is.EqualTo(2));
        }

        [Test]
        public void DroppingTheSeasonAlreadyShownChangesNothing()
        {
            var round = new SeasonRound(Season.Spring, Season.Summer);
            Assert.That(round.Drop(Season.Summer), Is.False);
            Assert.That(round.Wrongs, Is.EqualTo(0));
        }
    }

    // The Seasons screen: the tray and what a drop on the meadow does. Coroutines (voice, hand) do not advance in Edit Mode, but the
    // rules and the shown season are set synchronously on a drop.
    public class SeasonsScreenTests
    {
        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            SeasonsScreen.UseDevicePlacement = false; // work in picture space
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
        }

        [TearDown]
        public void TearDown()
        {
            SeasonsScreen.UseDevicePlacement = true;
            if (_game != null) UnityEngine.Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) UnityEngine.Object.DestroyImmediate(_canvasObject);
        }

        private SeasonsScreen ShowScreen()
        {
            _game.Navigator.Show(ScreenId.Seasons);
            var screen = _game.Navigator.GetScreen(ScreenId.Seasons) as SeasonsScreen;
            Assert.IsNotNull(screen, "Seasons is registered on its own screen");
            return screen;
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static void Drop(SeasonsScreen screen, Season season, Vector2 point)
        {
            var drag = screen.CardDrags[(int)season];
            drag.OnBeginDrag(new PointerEventData(null));
            screen.PlaceCard((int)season, point);
            drag.OnEndDrag(new PointerEventData(null));
        }

        private static void OnMeadow(SeasonsScreen screen, Season season) => Drop(screen, season, SeasonsScreen.MeadowCentre);

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
        public void ASessionStartsInSummerAndAsksAnotherSeason()
        {
            var screen = ShowScreen();
            Assert.That(screen.ShownSeason, Is.EqualTo(Seasons.Start));
            Assert.That(screen.Round.Asked, Is.Not.EqualTo(Seasons.Start));
            Assert.That(screen.Round.Done, Is.False);
            Assert.That(screen.PetalsFalling || screen.LeavesFalling || screen.SnowFalling, Is.False);
        }

        [Test]
        public void ACardLetGoOnTheTrayDoesNothingAndOneLetGoOnTheMeadowShowsItsSeason()
        {
            var screen = ShowScreen();
            var asked = screen.Round.Asked;
            var other = Enum.GetValues(typeof(Season)).Cast<Season>().First(s => s != asked && s != Seasons.Start);
            Drop(screen, other, new Vector2(0f, -330f));
            Assert.That(screen.ShownSeason, Is.EqualTo(Seasons.Start));
            OnMeadow(screen, other);
            Assert.That(screen.ShownSeason, Is.EqualTo(other));
            Assert.That(screen.Round.Wrongs, Is.EqualTo(1));
            Assert.That(screen.Round.Done, Is.False);
        }

        [Test]
        public void TheAskedSeasonFinishesTheRound()
        {
            var screen = ShowScreen();
            var asked = screen.Round.Asked;
            OnMeadow(screen, asked);
            Assert.That(screen.Round.Done);
            Assert.That(screen.ShownSeason, Is.EqualTo(asked));
        }

        [Test]
        public void EachSeasonBringsItsOwnFallingBits()
        {
            var screen = ShowScreen();
            OnMeadow(screen, Season.Spring);
            Assert.That(screen.PetalsFalling);
            OnMeadow(screen, Season.Autumn);
            Assert.That(screen.LeavesFalling);
            OnMeadow(screen, Season.Winter);
            Assert.That(screen.SnowFalling);
            OnMeadow(screen, Season.Summer);
            Assert.That(screen.PetalsFalling || screen.LeavesFalling || screen.SnowFalling, Is.False);
        }
    }
}
