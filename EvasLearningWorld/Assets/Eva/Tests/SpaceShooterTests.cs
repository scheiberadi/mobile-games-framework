using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    public class SpaceShooterTests
    {
        private const float FarAway = 5000f; // a ship nowhere near any shape (its stars fly off the screen)

        private static void Run(SpaceShooterDirector director, float seconds, List<SpaceShape> spawned = null, List<SpaceShape> gone = null)
        {
            for (var t = 0f; t < seconds; t += 0.05f) director.Tick(0.05f, FarAway, null, spawned, null, gone, null);
        }

        // The ship goes under the lowest shape, so its stars hit it.
        private static void TickUnderAShape(SpaceShooterDirector director, List<SpaceShape> spawned = null, List<SpaceShape> popped = null)
        {
            var lowest = director.Up.OrderByDescending(s => s.Progress).FirstOrDefault();
            director.Tick(0.05f, lowest != null ? lowest.X : 0f, null, spawned, popped, null, null);
        }

        [Test]
        public void ThePaceGetsHarderAndEveryLevelNeedsMoreShapesAtOnceAndMorePops()
        {
            for (var level = SpaceShooterDirector.MinLevel + 1; level <= SpaceShooterDirector.MaxLevel; level++)
            {
                Assert.Greater(SpaceShooterDirector.MaxUp(level), SpaceShooterDirector.MaxUp(level - 1));
                Assert.Less(SpaceShooterDirector.FallSeconds(level), SpaceShooterDirector.FallSeconds(level - 1));
                Assert.Less(SpaceShooterDirector.SpawnGap(level), SpaceShooterDirector.SpawnGap(level - 1));
                Assert.Greater(SpaceShooterDirector.HitsToPass(level), SpaceShooterDirector.HitsToPass(level - 1));
            }
        }

        [Test]
        public void NeverMoreShapesUpThanTheLevelAllowsAndNeverTwoCloseTogetherNearTheTop()
        {
            for (var level = SpaceShooterDirector.MinLevel; level <= SpaceShooterDirector.MaxLevel; level++)
            {
                var director = new SpaceShooterDirector(level, new System.Random(level));
                for (var i = 0; i < 1200; i++)
                {
                    director.Tick(0.05f, FarAway, null, null, null, null, null);
                    Assert.LessOrEqual(director.Up.Count, SpaceShooterDirector.MaxUp(level), "level " + level);
                    Assert.IsTrue(director.Up.All(s => s.Look >= 0 && s.Look < SpaceShooterDirector.Looks && Mathf.Abs(s.BaseX) <= SpaceShooterDirector.SpawnRange));
                }
            }
        }

        [Test]
        public void TheSkyFillsUpWithShapesWhenTheChildPopsNothing()
        {
            for (var level = SpaceShooterDirector.MinLevel; level <= SpaceShooterDirector.MaxLevel; level++)
            {
                var director = new SpaceShooterDirector(level, new System.Random(level));
                var most = 0;
                for (var i = 0; i < 1200; i++)
                {
                    director.Tick(0.05f, FarAway, null, null, null, null, null);
                    most = Mathf.Max(most, director.Up.Count);
                }
                Assert.GreaterOrEqual(most, SpaceShooterDirector.MaxUp(level) - 1, "level " + level);
            }
        }

        [Test]
        public void AShapeThatIsNotHitDriftsAwayAndThatIsNeverAMistake()
        {
            var director = new SpaceShooterDirector(1, new System.Random(3));
            var spawned = new List<SpaceShape>();
            var gone = new List<SpaceShape>();
            Run(director, 40f, spawned, gone);
            Assert.GreaterOrEqual(gone.Count, 1);
            Assert.AreEqual(0, director.Hits, "an unhit shape is simply gone, nothing is lost");
            Assert.IsFalse(director.LevelDone);
            Assert.IsTrue(gone.All(s => spawned.Contains(s) && s.Progress >= 1f));
        }

        [Test]
        public void TheShipFiresByItselfAndTheStarsLeaveTheScreenAtTheTop()
        {
            var director = new SpaceShooterDirector(1, new System.Random(2));
            var fired = new List<SpaceShot>();
            var spent = new List<SpaceShot>();
            for (var i = 0; i < 100; i++) director.Tick(0.05f, 123f, fired, null, null, null, spent);
            Assert.GreaterOrEqual(fired.Count, 8, "about one star every few tenths of a second");
            Assert.IsTrue(fired.All(s => Mathf.Approximately(s.X, 123f)), "a star leaves from where the ship is");
            Assert.GreaterOrEqual(spent.Count, fired.Count - 3);
            Assert.LessOrEqual(director.Shots.Count, 3);
        }

        [Test]
        public void AShipUnderAShapePopsItAndOneToTheSideDoesNot()
        {
            var under = new SpaceShooterDirector(1, new System.Random(4));
            var popped = new List<SpaceShape>();
            for (var i = 0; i < 3000 && under.Hits < 3; i++) TickUnderAShape(under, null, popped);
            Assert.GreaterOrEqual(under.Hits, 3);
            Assert.AreEqual(under.Hits, popped.Count);

            var beside = new SpaceShooterDirector(1, new System.Random(4));
            Run(beside, 30f);
            Assert.AreEqual(0, beside.Hits);
        }

        [Test]
        public void ALevelIsDoneAfterItsNumberOfPopsAndNothingNewComesThen()
        {
            for (var level = SpaceShooterDirector.MinLevel; level <= SpaceShooterDirector.MaxLevel; level++)
            {
                var director = new SpaceShooterDirector(level, new System.Random(5 + level));
                var spawned = new List<SpaceShape>();
                var fired = new List<SpaceShot>();
                var ticks = 0;
                while (!director.LevelDone && ticks++ < 60000)
                {
                    var lowest = director.Up.OrderByDescending(s => s.Progress).FirstOrDefault();
                    director.Tick(0.05f, lowest != null ? lowest.X : 0f, fired, spawned, null, null, null);
                }
                Assert.IsTrue(director.LevelDone, "level " + level);
                Assert.GreaterOrEqual(director.Hits, SpaceShooterDirector.HitsToPass(level));
                spawned.Clear();
                fired.Clear();
                director.Tick(0.05f, 0f, fired, spawned, null, null, null);
                Run(director, 10f, spawned);
                Assert.AreEqual(0, spawned.Count);
                Assert.AreEqual(0, fired.Count, "no new stars once the level is done");
            }
        }

        [Test]
        public void ShapesAndStarsStillOnScreenAtTheEndOfALevelGoOnIntoTheNextOne()
        {
            var first = new SpaceShooterDirector(1, new System.Random(9));
            for (var i = 0; i < 400 && (first.Up.Count < 2 || first.Shots.Count < 1); i++) first.Tick(0.05f, FarAway, null, null, null, null, null);
            var carried = first.Up.ToList();
            var carriedShots = first.Shots.ToList();
            Assert.GreaterOrEqual(carried.Count, 2);
            Assert.GreaterOrEqual(carriedShots.Count, 1);
            var second = new SpaceShooterDirector(2, new System.Random(10), carried, carriedShots);
            Assert.AreEqual(carried.Count, second.Up.Count);
            Assert.AreEqual(carriedShots.Count, second.Shots.Count);
            Assert.AreEqual(0, second.Hits, "the new level counts its own pops");
            var before = carried[0].Progress;
            second.Tick(0.1f, FarAway, null, null, null, null, null);
            Assert.Greater(carried[0].Progress, before, "carried shapes keep drifting");
        }

        [Test]
        public void TheWholeGameIsOneCoin()
        {
            Assert.AreEqual(1, SpaceShooterDirector.SessionCoins);
        }

        [Test]
        public void TheShapePicturesTheRocketTheSkyArtAndThePromptExist()
        {
            foreach (var name in new[] { "circle", "diamond", "heart", "square", "star", "triangle" })
                Assert.IsNotNull(Resources.Load<Sprite>("Art/arcade/shapecard_" + name), "shapecard_" + name);
            Assert.IsNotNull(Resources.Load<Sprite>("Art/arcade/ship_star"));
            Assert.IsNotNull(Resources.Load<Sprite>("Art/arcade/prop_pop"));
            foreach (var name in new[] { "space_star", "space_moon", "space_saturn" })
                Assert.IsNotNull(Resources.Load<Sprite>("Art/sciencelab/" + name), name);
            var lines = VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            Assert.AreEqual("Blast the shapes!", lines["spaceshooter_find"]);
            Assert.IsNotNull(Resources.Load<AudioClip>("Voice/en/spaceshooter_find"));
        }

        [Test]
        public void TheScreenHasAFullScreenTouchPadAndAShip()
        {
            var canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            var game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            game.Build(canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
            try
            {
                game.Navigator.Show(ScreenId.SpaceShooter);
                var field = canvasObject.transform.Find("ScreenRoot/SpaceShooterScreen/Field");
                Assert.IsNotNull(field.Find("Pad"));
                Assert.IsNotNull(field.Find("Ship"));
                Assert.GreaterOrEqual(field.childCount, 2 + SpaceShooterDirector.MaxUp(SpaceShooterDirector.MaxLevel));
            }
            finally
            {
                Object.DestroyImmediate(game.gameObject);
                Object.DestroyImmediate(canvasObject);
            }
        }
    }
}
