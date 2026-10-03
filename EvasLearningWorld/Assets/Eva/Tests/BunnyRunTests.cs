using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    public class BunnyRunTests
    {
        private const float Step = 1f / 60f;

        // A child who taps at the best moment: the bunny is at the top of its jump over the obstacle.
        private static bool TapsAtTheObstacle(BunnyRunDirector director)
        {
            if (director.Jumping || director.State != BunnyState.Running) return false;
            var next = director.NextObstacle();
            if (next == null) return false;
            var launch = next.X - BunnyRunDirector.JumpDistance(director.Level) * 0.5f;
            return director.Scroll >= launch && director.Scroll < next.X - next.HalfWidth - BunnyRunDirector.BunnyHalfWidth;
        }

        private static BunnyEvents Play(BunnyRunDirector director, float seconds, System.Func<BunnyRunDirector, bool> taps)
        {
            var all = BunnyEvents.None;
            for (var t = 0f; t < seconds; t += Step) all |= director.Tick(Step, taps(director), null);
            return all;
        }

        [Test]
        public void SixLevelsGetFasterAndNeedMoreCarrots()
        {
            for (var level = BunnyRunDirector.MinLevel + 1; level <= BunnyRunDirector.MaxLevel; level++)
            {
                Assert.Greater(BunnyRunDirector.Speed(level), BunnyRunDirector.Speed(level - 1));
                Assert.Greater(BunnyRunDirector.HitsToPass(level), BunnyRunDirector.HitsToPass(level - 1));
            }
        }

        [Test]
        public void EveryObstacleIsEasyToJumpOver()
        {
            // At the best moment the bunny stays above even the tallest obstacle for long enough that the child has at least half a second
            // of slack in when to tap (the obstacle and the bunny both have width).
            for (var level = BunnyRunDirector.MinLevel; level <= BunnyRunDirector.MaxLevel; level++)
            for (var kind = 0; kind < BunnyRunDirector.ObstacleKinds; kind++)
            {
                var passing = 2f * (BunnyRunDirector.ObstacleHalfWidth(kind) + BunnyRunDirector.BunnyHalfWidth) / BunnyRunDirector.Speed(level);
                Assert.GreaterOrEqual(BunnyRunDirector.SecondsAbove(kind) - passing, 0.5f, "level " + level + " kind " + kind);
            }
        }

        [Test]
        public void ARunningBunnyWithNoTapBumpsTheFirstObstacleAndIsSlidBack()
        {
            var director = new BunnyRunDirector(new System.Random(1));
            var first = director.NextObstacle();
            var events = Play(director, 12f, d => false);
            Assert.IsTrue((events & BunnyEvents.Bumped) != 0, "it ran into it");
            Assert.IsTrue((events & BunnyEvents.Recovered) != 0, "and was back on its feet");
            Assert.Less(director.Scroll, first.X, "set back before the obstacle, so it can try again");
        }

        [Test]
        public void ATapJumpsOnceAndATapInTheAirDoesNothing()
        {
            var director = new BunnyRunDirector(new System.Random(2));
            Assert.IsTrue((director.Tick(Step, true, null) & BunnyEvents.Jumped) != 0);
            Assert.IsTrue(director.Jumping);
            var again = BunnyEvents.None;
            for (var i = 0; i < 10; i++) again |= director.Tick(Step, true, null);
            Assert.IsFalse((again & BunnyEvents.Jumped) != 0, "no double jump");
            Assert.Greater(director.JumpHeight, 0f);
        }

        [Test]
        public void ABunnyThatTapsInTimeNeverBumpsAndPassesAllSixLevels()
        {
            var director = new BunnyRunDirector(new System.Random(3));
            var all = BunnyEvents.None;
            var seconds = 0f;
            for (var level = BunnyRunDirector.MinLevel; level <= BunnyRunDirector.MaxLevel; level++)
            {
                director.StartLevel(level);
                while (!director.LevelDone)
                {
                    all |= director.Tick(Step, TapsAtTheObstacle(director), null);
                    seconds += Step;
                    Assert.Less(seconds, 1200f, "level " + level + " never ended");
                }
            }
            Assert.IsFalse((all & BunnyEvents.Bumped) != 0, "a well timed bunny never bumps");
            Assert.Greater(director.TotalHits, 200);
        }

        [Test]
        public void ABunnyThatBumpsKeepsEveryCarrotItTook()
        {
            var director = new BunnyRunDirector(new System.Random(4));
            Play(director, 3f, d => false);
            var hits = director.TotalHits;
            Assert.Greater(hits, 0, "the carrots before the first obstacle were taken");
            Play(director, 30f, d => false);
            Assert.GreaterOrEqual(director.TotalHits, hits);
        }

        [Test]
        public void TheCarrotsOverAnObstacleAreOnTheWayOfTheBestJump()
        {
            var director = new BunnyRunDirector(new System.Random(5));
            var taken = 0;
            var all = new List<RunCarrot>();
            // Jump at the best moment over the first obstacle and count what is picked up in the air over it.
            for (var t = 0f; t < 10f && director.NextObstacle() != null && director.Scroll < director.Obstacles[0].X + 300f; t += Step)
            {
                all.Clear();
                director.Tick(Step, TapsAtTheObstacle(director), all);
                taken += all.Count(c => c.Y > BunnyRunDirector.BunnyCenter + 50f);
            }
            Assert.GreaterOrEqual(taken, 2, "at least two of the three");
        }

        [Test]
        public void EveryThirdObstacleHasAGoldenStarWorthTwo()
        {
            var director = new BunnyRunDirector(new System.Random(6));
            var stars = director.Carrots.Where(c => c.Star).ToList();
            Assert.GreaterOrEqual(stars.Count, 1);
            Assert.IsTrue(stars.All(s => s.Value == 2));
        }

        [Test]
        public void TheTrailKeepsGoingAndOnlyKeepsWhatIsNear()
        {
            var director = new BunnyRunDirector(new System.Random(7));
            Play(director, 120f, TapsAtTheObstacle);
            Assert.Less(director.Obstacles.Count, 12);
            Assert.Less(director.Carrots.Count, 120);
            Assert.Greater(director.Obstacles[director.Obstacles.Count - 1].X, director.Scroll + 1000f);
        }

        [Test]
        public void ANewLevelRebuildsTheTrailAheadForItsOwnWorld()
        {
            var director = new BunnyRunDirector(new System.Random(8));
            Play(director, 6f, TapsAtTheObstacle);
            var scroll = director.Scroll;
            director.StartLevel(2);
            Assert.AreEqual(0, director.Hits);
            Assert.Greater(director.Obstacles[director.Obstacles.Count - 1].X, scroll + 2000f, "and extended again");
        }

        [Test]
        public void TheWholeGameIsOneCoin()
        {
            Assert.AreEqual(1, BunnyRunDirector.SessionCoins);
        }

        [Test]
        public void ThePicturesAndThePromptExist()
        {
            var names = new List<string> { "carrot", "star", "friend_bird", "friend_butterfly", "friend_bee", "w1_bg", "w1_ground", "w1_obstacle1", "w1_obstacle2", "w1_obstacle3" };
            names.AddRange(new[] { "crouch", "push", "tuck", "fall", "land", "stumble", "cheer", "sit" }.Select(p => "bunny_" + p));
            foreach (var name in names) Assert.IsNotNull(Resources.Load<Sprite>("Art/platformer/" + name), name);
            var lines = VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            Assert.AreEqual("Jump and get the carrots!", lines["platformer_prompt"]);
            Assert.IsNotNull(Resources.Load<AudioClip>("Voice/en/platformer_prompt"));
        }

        [Test]
        public void TheScreenHasAFullScreenTapPadAndTheBunny()
        {
            var canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            var game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            game.Build(canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
            try
            {
                game.Navigator.Show(ScreenId.Platformer);
                var field = canvasObject.transform.Find("ScreenRoot/PlatformerScreen/Field");
                Assert.IsNotNull(field.Find("Pad"));
                Assert.IsNotNull(field.Find("Bunny"));
                Assert.IsNotNull(field.Find("Ground0"));
            }
            finally
            {
                Object.DestroyImmediate(game.gameObject);
                Object.DestroyImmediate(canvasObject);
            }
        }
    }
}
