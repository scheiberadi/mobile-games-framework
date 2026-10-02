using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    public class BalloonPoppingTests
    {
        private static void Run(BalloonPoppingDirector director, float seconds, List<UpBalloon> spawned = null, List<UpBalloon> escaped = null)
        {
            for (var t = 0f; t < seconds; t += 0.05f) director.Tick(0.05f, spawned, escaped);
        }

        [Test]
        public void ThePaceGetsHarderAndEveryLevelNeedsMoreBalloonsAtOnceAndMoreHits()
        {
            for (var level = BalloonPoppingDirector.MinLevel + 1; level <= BalloonPoppingDirector.MaxLevel; level++)
            {
                Assert.Greater(BalloonPoppingDirector.MaxUp(level), BalloonPoppingDirector.MaxUp(level - 1));
                Assert.Less(BalloonPoppingDirector.RiseSeconds(level), BalloonPoppingDirector.RiseSeconds(level - 1));
                Assert.Less(BalloonPoppingDirector.SpawnGap(level), BalloonPoppingDirector.SpawnGap(level - 1));
                Assert.Greater(BalloonPoppingDirector.HitsToPass(level), BalloonPoppingDirector.HitsToPass(level - 1));
            }
            Assert.AreEqual(5f, BalloonPoppingDirector.RiseSeconds(1), "a balloon takes 5 s to cross the screen at the start");
            Assert.LessOrEqual(BalloonPoppingDirector.RiseSeconds(6), 3.5f);
            Assert.GreaterOrEqual(BalloonPoppingDirector.MaxUp(1), 4, "several balloons from the first level");
            {
            }
        }

        [Test]
        public void NeverMoreBalloonsUpThanTheLevelAllowsAndNeverTwoOverlappingInALane()
        {
            for (var level = BalloonPoppingDirector.MinLevel; level <= BalloonPoppingDirector.MaxLevel; level++)
            {
                var director = new BalloonPoppingDirector(level, new System.Random(level));
                for (var i = 0; i < 1200; i++)
                {
                    director.Tick(0.05f, null, null);
                    Assert.LessOrEqual(director.Up.Count, BalloonPoppingDirector.MaxUp(level), "level " + level);
                    foreach (var lane in director.Up.GroupBy(b => b.Lane))
                    {
                        var progress = lane.Select(b => b.Progress).OrderBy(p => p).ToList();
                        for (var k = 1; k < progress.Count; k++) Assert.GreaterOrEqual(progress[k] - progress[k - 1], 0.27f, "balloons too close in lane " + lane.Key);
                    }
                    Assert.IsTrue(director.Up.All(b => b.Look >= 0 && b.Look < BalloonPoppingDirector.Looks && b.Lane >= 0 && b.Lane < BalloonPoppingDirector.Lanes));
                }
            }
        }

        [Test]
        public void TheScreenFillsUpToTheLevelsLimitWhenTheChildIsSlow()
        {
            for (var level = BalloonPoppingDirector.MinLevel; level <= BalloonPoppingDirector.MaxLevel; level++)
            {
                var director = new BalloonPoppingDirector(level, new System.Random(level));
                var most = 0;
                for (var i = 0; i < 1200; i++)
                {
                    director.Tick(0.05f, null, null);
                    most = Mathf.Max(most, director.Up.Count);
                }
                Assert.GreaterOrEqual(most, BalloonPoppingDirector.MaxUp(level) - 1, "level " + level);
            }
        }

        [Test]
        public void ABalloonThatIsNotPoppedFloatsOffTheTopAndThatIsNeverAMistake()
        {
            var director = new BalloonPoppingDirector(1, new System.Random(3));
            var spawned = new List<UpBalloon>();
            var escaped = new List<UpBalloon>();
            Run(director, 30f, spawned, escaped);
            Assert.GreaterOrEqual(escaped.Count, 1);
            Assert.AreEqual(0, director.Hits, "an unpopped balloon is simply gone, nothing is lost");
            Assert.IsFalse(director.LevelDone);
            Assert.IsTrue(escaped.All(b => spawned.Contains(b) && b.Progress >= 1f));
        }

        [Test]
        public void PoppingABalloonCountsAHitAndAnUnknownBalloonDoesNothing()
        {
            var director = new BalloonPoppingDirector(6, new System.Random(4));
            for (var i = 0; i < 400 && director.Up.Count == 0; i++) director.Tick(0.05f, null, null);
            var balloon = director.Up.First();
            Assert.IsTrue(director.Pop(balloon));
            Assert.AreEqual(1, director.Hits);
            Assert.IsFalse(director.Up.Contains(balloon));
            Assert.IsFalse(director.Pop(balloon), "a balloon pops only once");
            Assert.IsFalse(director.Pop(null));
            Assert.AreEqual(1, director.Hits);
        }

        [Test]
        public void ALevelIsDoneAfterItsNumberOfHitsAndNoMoreBalloonsComeThen()
        {
            for (var level = BalloonPoppingDirector.MinLevel; level <= BalloonPoppingDirector.MaxLevel; level++)
            {
                var director = new BalloonPoppingDirector(level, new System.Random(5 + level));
                var spawned = new List<UpBalloon>();
                var ticks = 0;
                while (!director.LevelDone && ticks++ < 20000)
                {
                    director.Tick(0.05f, spawned, null);
                    foreach (var balloon in director.Up.ToList()) director.Pop(balloon);
                }
                Assert.AreEqual(BalloonPoppingDirector.HitsToPass(level), director.Hits, "level " + level);
                spawned.Clear();
                Run(director, 10f, spawned);
                Assert.AreEqual(0, spawned.Count);
            }
        }

        [Test]
        public void TheWholeGameIsOneCoin()
        {
            Assert.AreEqual(1, BalloonPoppingDirector.SessionCoins);
        }

        [Test]
        public void TheBalloonPicturesAndThePromptExist()
        {
            foreach (var id in new[] { "a", "b", "c", "d", "e", "f" })
                Assert.IsNotNull(Resources.Load<Sprite>("Art/arcade/bal_" + id), "bal_" + id);
            Assert.IsNotNull(Resources.Load<Sprite>("Art/arcade/prop_pop"));
            var lines = VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            Assert.AreEqual("Pop the balloons!", lines["balloonpop_find"]);
            Assert.IsNotNull(Resources.Load<AudioClip>("Voice/en/balloonpop_find"));
        }

        [Test]
        public void TheScreenHasBigBalloonsToTap()
        {
            var canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            var game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            game.Build(canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
            try
            {
                game.Navigator.Show(ScreenId.BalloonPopping);
                var field = canvasObject.transform.Find("ScreenRoot/BalloonPoppingScreen/Field");
                var balloons = field.GetComponentsInChildren<TapTarget>(true);
                Assert.GreaterOrEqual(balloons.Length, BalloonPoppingDirector.MaxUp(BalloonPoppingDirector.MaxLevel));
                foreach (var balloon in balloons)
                {
                    var rect = (RectTransform)balloon.transform;
                    Assert.GreaterOrEqual(rect.rect.width, EvaUi.MinTap);
                    Assert.GreaterOrEqual(rect.rect.height, EvaUi.MinTap);
                }
            }
            finally
            {
                Object.DestroyImmediate(game.gameObject);
                Object.DestroyImmediate(canvasObject);
            }
        }
    }
}
