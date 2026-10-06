using System;
using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    public class JetpackFlightTests
    {
        private static JetpackDirector Started()
        {
            var director = new JetpackDirector(new System.Random(7));
            director.Hovering = false;
            return director;
        }

        private static void Run(JetpackDirector director, float seconds, bool holding, List<JetpackPillar> passed = null, List<JetpackPillar> bumped = null)
        {
            for (var t = 0f; t < seconds; t += 1f / 60f) director.Tick(1f / 60f, holding, passed, bumped);
        }

        // Steers by hand: holds while below the middle of the next gap, lets go while above it.
        private static bool Steer(JetpackDirector director)
        {
            JetpackPillar next = null;
            foreach (var pillar in director.Pillars) if (!pillar.Passed) { next = pillar; break; }
            return next != null && director.CatY < next.GapY;
        }

        [Test]
        public void EvaHoversAndNothingMovesUntilTheFirstTouch()
        {
            var director = new JetpackDirector(new System.Random(1));
            Run(director, 3f, false);
            Assert.That(director.CatY, Is.EqualTo(0f));
            Assert.That(director.Pillars.Count, Is.EqualTo(0));
            Assert.That(director.TotalPassed, Is.EqualTo(0));
        }

        [Test]
        public void AFingerHeldLiftsHerAndLettingGoLetsHerSink()
        {
            var director = Started();
            Run(director, 0.3f, true);
            var up = director.CatY;
            Assert.That(up, Is.GreaterThan(30f), "she rises while a finger is down");
            Run(director, 0.5f, false);
            Assert.That(director.CatY, Is.LessThan(up), "and sinks when it is let go");
        }

        [Test]
        public void SheNeverLeavesTheSkyEvenWhenHeldOrLetGoForLong()
        {
            var director = Started();
            Run(director, 1.2f, true); // short enough that the first pillar is still far away
            Assert.That(director.CatY, Is.EqualTo(JetpackDirector.CeilingY));
            Run(director, 2.6f, false);
            Assert.That(director.CatY, Is.EqualTo(JetpackDirector.FloorY));
        }

        [Test]
        public void PillarsComeInFromTheRightAndEveryGapIsReachable()
        {
            var director = Started();
            for (var t = 0f; t < 8f; t += 1f / 60f) director.Tick(1f / 60f, Steer(director), null, null);
            Assert.That(director.Pillars.Count, Is.GreaterThan(1));
            var previous = float.NaN;
            foreach (var pillar in director.Pillars)
            {
                Assert.That(Mathf.Abs(pillar.GapY), Is.LessThanOrEqualTo(170.01f));
                if (!float.IsNaN(previous)) Assert.That(Mathf.Abs(previous - pillar.X), Is.EqualTo(JetpackDirector.Spacing(director.Level)).Within(JetpackDirector.Speed(director.Level) / 60f + 0.01f));
                previous = pillar.X;
            }
        }

        [Test]
        public void ATouchedPillarHoldsHerInsideTheGapAndStillCountsAsPassed()
        {
            var director = Started();
            var passed = new List<JetpackPillar>();
            var bumped = new List<JetpackPillar>();
            // Held the whole time she flies into the ceiling: at some pillar the gap is lower than the ceiling.
            for (var t = 0f; t < 40f; t += 1f / 60f)
            {
                director.Tick(1f / 60f, true, passed, bumped);
                foreach (var pillar in director.Pillars)
                {
                    if (Mathf.Abs(pillar.X - JetpackDirector.CatX) >= JetpackDirector.PillarHalfWidth + JetpackDirector.CatHalfLength) continue;
                    Assert.That(director.CatY, Is.InRange(pillar.GapY - pillar.GapHeight * 0.5f, pillar.GapY + pillar.GapHeight * 0.5f), "never inside a pillar");
                }
            }
            Assert.That(bumped.Count, Is.GreaterThan(0), "a pillar was touched");
            Assert.That(passed.Count, Is.GreaterThan(0), "and it counted as passed anyway");
            foreach (var pillar in bumped) Assert.That(pillar.Bumped, Is.True);
        }

        [Test]
        public void AClearCrossingIsNotABump()
        {
            var director = Started();
            var passed = new List<JetpackPillar>();
            var bumped = new List<JetpackPillar>();
            for (var t = 0f; t < 25f; t += 1f / 60f) director.Tick(1f / 60f, Steer(director), passed, bumped);
            Assert.That(passed.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(bumped.Count, Is.EqualTo(0));
        }

        [Test]
        public void ALevelIsDoneAfterItsPillarsAndTheNextLevelKeepsTheOnesOnScreen()
        {
            var director = Started();
            var guard = 0;
            while (!director.LevelDone && guard++ < 60 * 120) director.Tick(1f / 60f, Steer(director), null, null);
            Assert.That(director.LevelDone);
            Assert.That(director.PassedInLevel, Is.EqualTo(JetpackDirector.PillarsToPass(1)));
            var onScreen = director.Pillars.Count;
            director.SetLevel(2);
            Assert.That(director.PassedInLevel, Is.EqualTo(0));
            Assert.That(director.Pillars.Count, Is.EqualTo(onScreen), "the pillars already out carry on");
            Assert.That(director.TotalPassed, Is.EqualTo(JetpackDirector.PillarsToPass(1)));
        }

        [Test]
        public void EveryLevelIsFasterNarrowerAndLongerThanTheOneBefore()
        {
            for (var level = JetpackDirector.MinLevel + 1; level <= JetpackDirector.MaxLevel; level++)
            {
                Assert.That(JetpackDirector.Speed(level), Is.GreaterThan(JetpackDirector.Speed(level - 1)));
                Assert.That(JetpackDirector.GapHeight(level), Is.LessThan(JetpackDirector.GapHeight(level - 1)));
                Assert.That(JetpackDirector.PillarsToPass(level), Is.GreaterThan(JetpackDirector.PillarsToPass(level - 1)));
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => JetpackDirector.Speed(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => JetpackDirector.Speed(JetpackDirector.MaxLevel + 1));
        }

        [Test]
        public void TheWholeGameLastsAboutAMinuteAndAHalfAndPaysOneCoin()
        {
            var seconds = 0f;
            for (var level = JetpackDirector.MinLevel; level <= JetpackDirector.MaxLevel; level++)
                seconds += JetpackDirector.PillarsToPass(level) * JetpackDirector.Spacing(level) / JetpackDirector.Speed(level);
            Assert.That(seconds, Is.InRange(45f, 120f));
            Assert.That(JetpackDirector.SessionCoins, Is.EqualTo(1));
        }

        [Test]
        public void PillarsAppearPastTheEdgeOfAWidePhoneScreen()
        {
            Assert.That(JetpackDirector.SpawnX - JetpackDirector.PillarHalfWidth, Is.GreaterThan(1050f));
        }
    }

    public class JetpackCatScreenTests
    {
        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
        }

        [TearDown]
        public void TearDown()
        {
            if (_game != null) UnityEngine.Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) UnityEngine.Object.DestroyImmediate(_canvasObject);
        }

        [Test]
        public void TheGameIsInTheArcadeMenuAndOpensOnItsOwnScreen()
        {
            var entry = Activities.For(BuildingId.Arcade).First(a => a.Id == "jetpack_cat");
            _game.Navigator.Show((ScreenId)Enum.Parse(typeof(ScreenId), entry.ScreenKey));
            var screen = _game.Navigator.GetScreen(ScreenId.JetpackCat) as JetpackCatScreen;
            Assert.IsNotNull(screen);
            Assert.IsNotNull(screen.Director);
            Assert.That(screen.Director.Hovering, Is.True, "she waits for the first touch");
        }

        [Test]
        public void ItsArtAndSoundsExist()
        {
            foreach (var sprite in new[] { "arcade/jetpack_cat", "arcade/jetpack_flame", "arcade/jetpack_pillar", "world/jetpack_bg", "activities/jetpack_cat" })
                Assert.IsNotNull(Resources.Load<Sprite>("Art/" + sprite), sprite);
            foreach (var clip in new[] { "Sfx/jetpack", "Sfx/jetpack_on", "Voice/en/jetpack_find", "Voice/en/activity_jetpack_cat" })
                Assert.IsNotNull(Resources.Load<AudioClip>(clip), clip);
        }
    }
}
