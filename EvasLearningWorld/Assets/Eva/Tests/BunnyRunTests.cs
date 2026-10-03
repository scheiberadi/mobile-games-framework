using System.Linq;
using System.Collections.Generic;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    public class BunnyRunTests
    {
        private const float Step = 1f / 60f;

        // A child who taps at the best moment before every river: the bunny lands in the middle of the far bank's grace.
        private static bool TapsAtTheRiver(BunnyRunDirector director)
        {
            if (director.Jumping || director.State != BunnyState.Running || !director.NextRiver(out var start, out var end)) return false;
            var distance = BunnyRunDirector.JumpDistance(director.Level);
            var width = end - start;
            return director.Scroll >= start - (distance - width) * 0.5f && director.Scroll < start;
        }

        private static BunnyEvents Play(BunnyRunDirector director, float seconds, System.Func<BunnyRunDirector, bool> taps, List<RunCarrot> taken = null)
        {
            var all = BunnyEvents.None;
            for (var t = 0f; t < seconds; t += Step) all |= director.Tick(Step, taps(director), taken);
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
        public void EveryRiverIsEasyToJumpOver()
        {
            // The jump must cover the widest river with at least 0.5 s of slack in when the child may tap.
            for (var level = BunnyRunDirector.MinLevel; level <= BunnyRunDirector.MaxLevel; level++)
            {
                var slackSeconds = (BunnyRunDirector.JumpDistance(level) - BunnyRunDirector.GapMax(level)) / BunnyRunDirector.Speed(level);
                Assert.GreaterOrEqual(slackSeconds, 0.5f, "level " + level);
            }
        }

        [Test]
        public void ARunningBunnyWithNoTapFallsInTheFirstRiverAndIsCarriedBack()
        {
            var director = new BunnyRunDirector(new System.Random(1));
            director.NextRiver(out var start, out var end);
            var events = Play(director, 12f, d => false);
            Assert.IsTrue((events & BunnyEvents.Splashed) != 0, "it fell in");
            Assert.IsTrue((events & BunnyEvents.Rescued) != 0, "and was set back");
            Assert.Less(director.Scroll, start + BunnyRunDirector.EdgeGrace + 60f, "set back before the river, so it can try again");
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
        public void ABunnyThatTapsInTimeNeverFallsAndPassesAllSixLevels()
        {
            var director = new BunnyRunDirector(new System.Random(3));
            var all = BunnyEvents.None;
            var seconds = 0f;
            for (var level = BunnyRunDirector.MinLevel; level <= BunnyRunDirector.MaxLevel; level++)
            {
                director.StartLevel(level);
                while (!director.LevelDone)
                {
                    all |= director.Tick(Step, TapsAtTheRiver(director), null);
                    seconds += Step;
                    Assert.Less(seconds, 900f, "level " + level + " never ended");
                }
            }
            Assert.IsFalse((all & BunnyEvents.Splashed) != 0, "a well timed bunny never falls in");
            Assert.Greater(director.TotalHits, 100);
        }

        [Test]
        public void CarrotsAreNeverTakenBackWhenTheBunnyFallsIn()
        {
            var director = new BunnyRunDirector(new System.Random(4));
            Play(director, 6f, d => false);
            var hits = director.TotalHits;
            Assert.Greater(hits, 0, "the carrots on the first bank were taken");
            var taken = director.Carrots.Count(c => c.Taken);
            Play(director, 30f, d => false);
            Assert.GreaterOrEqual(director.TotalHits, hits);
            Assert.GreaterOrEqual(director.Carrots.Count(c => c.Taken), taken > 0 ? 1 : 0);
        }

        [Test]
        public void TheTrackKeepsGoingAndOnlyKeepsWhatIsNear()
        {
            var director = new BunnyRunDirector(new System.Random(5));
            Play(director, 120f, TapsAtTheRiver);
            Assert.Less(director.Lands.Count, 20);
            Assert.Less(director.Carrots.Count, 120);
            Assert.Greater(director.Lands[director.Lands.Count - 1].End, director.Scroll + 1000f);
        }

        [Test]
        public void TheWholeGameIsOneCoin()
        {
            Assert.AreEqual(1, BunnyRunDirector.SessionCoins);
        }

        [Test]
        public void ThePicturesAndThePromptExist()
        {
            foreach (var name in new[] { "bg", "ground", "bunny_run1", "bunny_run2", "bunny_jump", "bunny_swim", "carrot", "lilypad" })
                Assert.IsNotNull(Resources.Load<Sprite>("Art/platformer/" + name), name);
            var lines = VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            Assert.AreEqual("Hop over the rivers!", lines["platformer_prompt"]);
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
                Assert.IsNotNull(field.Find("Water"));
            }
            finally
            {
                Object.DestroyImmediate(game.gameObject);
                Object.DestroyImmediate(canvasObject);
            }
        }
    }
}
