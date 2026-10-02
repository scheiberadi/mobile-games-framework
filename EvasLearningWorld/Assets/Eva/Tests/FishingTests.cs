using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    public class FishingTests
    {
        private const float FarAway = 5000f; // a hook nowhere near any fish

        private static void Run(FishingDirector director, float seconds, List<SwimmingFish> spawned = null, List<SwimmingFish> gone = null)
        {
            for (var t = 0f; t < seconds; t += 0.05f) director.Tick(0.05f, true, FarAway, FarAway, spawned, null, gone);
        }

        // Puts the hook on the fish that has been in the pond longest.
        private static void TickOnAFish(FishingDirector director, List<SwimmingFish> spawned = null, List<SwimmingFish> caught = null)
        {
            var oldest = director.Up.OrderByDescending(f => f.Age).FirstOrDefault();
            if (oldest == null) director.Tick(0.05f, false, 0f, 0f, spawned, caught, null);
            else director.Tick(0.05f, true, oldest.X, oldest.Y, spawned, caught, null);
        }

        [Test]
        public void ThePaceGetsHarderAndEveryLevelNeedsMoreFishAtOnceAndMoreCatches()
        {
            for (var level = FishingDirector.MinLevel + 1; level <= FishingDirector.MaxLevel; level++)
            {
                Assert.Greater(FishingDirector.MaxUp(level), FishingDirector.MaxUp(level - 1) - 1);
                Assert.Greater(FishingDirector.Speed(level), FishingDirector.Speed(level - 1));
                Assert.Less(FishingDirector.LifeSeconds(level), FishingDirector.LifeSeconds(level - 1));
                Assert.Less(FishingDirector.SpawnGap(level), FishingDirector.SpawnGap(level - 1));
                Assert.Greater(FishingDirector.HitsToPass(level), FishingDirector.HitsToPass(level - 1));
            }
            Assert.Greater(FishingDirector.MaxUp(FishingDirector.MaxLevel), FishingDirector.MaxUp(FishingDirector.MinLevel));
        }

        [Test]
        public void NeverMoreFishUpThanTheLevelAllowsAndAlwaysInsideThePond()
        {
            for (var level = FishingDirector.MinLevel; level <= FishingDirector.MaxLevel; level++)
            {
                var director = new FishingDirector(level, new System.Random(level));
                for (var i = 0; i < 1200; i++)
                {
                    director.Tick(0.05f, false, 0f, 0f, null, null, null);
                    Assert.LessOrEqual(director.Up.Count, FishingDirector.MaxUp(level), "level " + level);
                    Assert.IsTrue(director.Up.All(f =>
                        f.Look >= 0 && f.Look < FishingDirector.Looks
                        && f.X >= FishingDirector.PondLeft && f.X <= FishingDirector.PondRight
                        && f.BaseY >= FishingDirector.PondBottom && f.BaseY <= FishingDirector.PondTop));
                }
            }
        }

        [Test]
        public void ThePondFillsUpWithFishWhenTheChildCatchesNothing()
        {
            for (var level = FishingDirector.MinLevel; level <= FishingDirector.MaxLevel; level++)
            {
                var director = new FishingDirector(level, new System.Random(level));
                var most = 0;
                for (var i = 0; i < 1200; i++)
                {
                    director.Tick(0.05f, false, 0f, 0f, null, null, null);
                    most = Mathf.Max(most, director.Up.Count);
                }
                Assert.GreaterOrEqual(most, FishingDirector.MaxUp(level) - 1, "level " + level);
            }
        }

        [Test]
        public void AFishThatIsNotCaughtDivesAwayAndThatIsNeverAMistake()
        {
            var director = new FishingDirector(1, new System.Random(3));
            var spawned = new List<SwimmingFish>();
            var gone = new List<SwimmingFish>();
            Run(director, 40f, spawned, gone);
            Assert.GreaterOrEqual(gone.Count, 1);
            Assert.AreEqual(0, director.Hits, "an uncaught fish is simply gone, nothing is lost");
            Assert.IsFalse(director.LevelDone);
            Assert.IsTrue(gone.All(f => spawned.Contains(f) && f.Progress >= 1f));
        }

        [Test]
        public void AHookOnAFishCatchesItAndAHookInEmptyWaterOrOutOfTheWaterDoesNot()
        {
            var on = new FishingDirector(1, new System.Random(4));
            var caught = new List<SwimmingFish>();
            for (var i = 0; i < 2000 && on.Hits < 3; i++) TickOnAFish(on, null, caught);
            Assert.GreaterOrEqual(on.Hits, 3);
            Assert.AreEqual(on.Hits, caught.Count);

            var beside = new FishingDirector(1, new System.Random(4));
            Run(beside, 30f);
            Assert.AreEqual(0, beside.Hits);

            var lifted = new FishingDirector(1, new System.Random(4));
            for (var i = 0; i < 600; i++)
            {
                var oldest = lifted.Up.OrderByDescending(f => f.Age).FirstOrDefault();
                lifted.Tick(0.05f, false, oldest != null ? oldest.X : 0f, oldest != null ? oldest.Y : 0f, null, null, null);
            }
            Assert.AreEqual(0, lifted.Hits, "a hook that is not in the water catches nothing");
        }

        [Test]
        public void ALevelIsDoneAfterItsNumberOfCatchesAndNoMoreFishComesThen()
        {
            for (var level = FishingDirector.MinLevel; level <= FishingDirector.MaxLevel; level++)
            {
                var director = new FishingDirector(level, new System.Random(5 + level));
                var spawned = new List<SwimmingFish>();
                var ticks = 0;
                while (!director.LevelDone && ticks++ < 40000) TickOnAFish(director, spawned);
                Assert.IsTrue(director.LevelDone, "level " + level);
                Assert.GreaterOrEqual(director.Hits, FishingDirector.HitsToPass(level));
                spawned.Clear();
                Run(director, 10f, spawned);
                Assert.AreEqual(0, spawned.Count);
            }
        }

        [Test]
        public void FishStillSwimmingAtTheEndOfALevelGoOnIntoTheNextOne()
        {
            var first = new FishingDirector(1, new System.Random(9));
            for (var i = 0; i < 400 && first.Up.Count < 2; i++) first.Tick(0.05f, false, 0f, 0f, null, null, null);
            var carried = first.Up.ToList();
            Assert.GreaterOrEqual(carried.Count, 2);
            var second = new FishingDirector(2, new System.Random(10), carried);
            Assert.AreEqual(carried.Count, second.Up.Count);
            Assert.AreEqual(0, second.Hits, "the new level counts its own catches");
            var before = carried[0].Age;
            second.Tick(0.1f, false, 0f, 0f, null, null, null);
            Assert.Greater(carried[0].Age, before, "carried fish keep swimming");
        }

        [Test]
        public void TheWholeGameIsOneCoin()
        {
            Assert.AreEqual(1, FishingDirector.SessionCoins);
        }

        [Test]
        public void TheFishPicturesThePondTheHookAndThePromptExist()
        {
            foreach (var name in new[] { "red", "orange", "yellow", "green", "blue", "purple" })
                Assert.IsNotNull(Resources.Load<Sprite>("Art/arcade/fish_" + name), "fish_" + name);
            Assert.IsNotNull(Resources.Load<Sprite>("Art/arcade/prop_hook"));
            Assert.IsNotNull(Resources.Load<Sprite>("Art/world/pond"));
            Assert.IsNotNull(Resources.Load<Sprite>("Art/world/map_bg"));
            var lines = VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            Assert.AreEqual("Catch the fish!", lines["fishing_find"]);
            Assert.IsNotNull(Resources.Load<AudioClip>("Voice/en/fishing_find"));
        }

        [Test]
        public void TheScreenHasAFullScreenTouchPadAPondAndAHook()
        {
            var canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            var game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            game.Build(canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
            try
            {
                game.Navigator.Show(ScreenId.Fishing);
                var field = canvasObject.transform.Find("ScreenRoot/FishingScreen/Field");
                Assert.IsNotNull(field.Find("Pad"));
                Assert.IsNotNull(field.Find("Pond"));
                Assert.IsNotNull(field.Find("Hook"));
                Assert.GreaterOrEqual(field.childCount, 3 + FishingDirector.MaxUp(FishingDirector.MaxLevel));
            }
            finally
            {
                Object.DestroyImmediate(game.gameObject);
                Object.DestroyImmediate(canvasObject);
            }
        }
    }
}
