using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    public class FruitCatcherTests
    {
        private const float FarAway = 5000f; // a basket nowhere near any fruit

        private static void Run(FruitCatcherDirector director, float seconds, List<FallingFruit> spawned = null, List<FallingFruit> gone = null)
        {
            for (var t = 0f; t < seconds; t += 0.05f) director.Tick(0.05f, FarAway, spawned, null, gone);
        }

        [Test]
        public void ThePaceGetsHarderAndEveryLevelNeedsMoreFruitAtOnceAndMoreCatches()
        {
            for (var level = FruitCatcherDirector.MinLevel + 1; level <= FruitCatcherDirector.MaxLevel; level++)
            {
                Assert.Greater(FruitCatcherDirector.MaxUp(level), FruitCatcherDirector.MaxUp(level - 1));
                Assert.Less(FruitCatcherDirector.FallSeconds(level), FruitCatcherDirector.FallSeconds(level - 1));
                Assert.Less(FruitCatcherDirector.SpawnGap(level), FruitCatcherDirector.SpawnGap(level - 1));
                Assert.Greater(FruitCatcherDirector.HitsToPass(level), FruitCatcherDirector.HitsToPass(level - 1));
            }
        }

        [Test]
        public void NeverMoreFruitUpThanTheLevelAllowsAndNeverTwoCloseTogetherNearTheTop()
        {
            for (var level = FruitCatcherDirector.MinLevel; level <= FruitCatcherDirector.MaxLevel; level++)
            {
                var director = new FruitCatcherDirector(level, new System.Random(level));
                for (var i = 0; i < 1200; i++)
                {
                    director.Tick(0.05f, FarAway, null, null, null);
                    Assert.LessOrEqual(director.Up.Count, FruitCatcherDirector.MaxUp(level), "level " + level);
                    Assert.IsTrue(director.Up.All(f => f.Look >= 0 && f.Look < FruitCatcherDirector.Looks && Mathf.Abs(f.X) <= FruitCatcherDirector.SpawnRange));
                }
            }
        }

        [Test]
        public void TheScreenFillsUpWithFruitWhenTheChildCatchesNothing()
        {
            for (var level = FruitCatcherDirector.MinLevel; level <= FruitCatcherDirector.MaxLevel; level++)
            {
                var director = new FruitCatcherDirector(level, new System.Random(level));
                var most = 0;
                for (var i = 0; i < 1200; i++)
                {
                    director.Tick(0.05f, FarAway, null, null, null);
                    most = Mathf.Max(most, director.Up.Count);
                }
                Assert.GreaterOrEqual(most, FruitCatcherDirector.MaxUp(level) - 1, "level " + level);
            }
        }

        [Test]
        public void FruitThatIsNotCaughtFallsAwayAndThatIsNeverAMistake()
        {
            var director = new FruitCatcherDirector(1, new System.Random(3));
            var spawned = new List<FallingFruit>();
            var gone = new List<FallingFruit>();
            Run(director, 30f, spawned, gone);
            Assert.GreaterOrEqual(gone.Count, 1);
            Assert.AreEqual(0, director.Hits, "an uncaught fruit is simply gone, nothing is lost");
            Assert.IsFalse(director.LevelDone);
            Assert.IsTrue(gone.All(f => spawned.Contains(f) && f.Progress >= 1f));
        }

        [Test]
        public void ABasketUnderTheFruitCatchesItAndOneToTheSideDoesNot()
        {
            var under = new FruitCatcherDirector(1, new System.Random(4));
            var caught = new List<FallingFruit>();
            for (var i = 0; i < 2000 && under.Hits < 3; i++)
            {
                var lowest = under.Up.OrderByDescending(f => f.Progress).FirstOrDefault();
                under.Tick(0.05f, lowest != null ? lowest.X : 0f, null, caught, null);
            }
            Assert.GreaterOrEqual(under.Hits, 3);
            Assert.AreEqual(under.Hits, caught.Count);
            Assert.IsTrue(caught.All(f => f.Progress >= FruitCatcherDirector.CatchAt));

            var beside = new FruitCatcherDirector(1, new System.Random(4));
            Run(beside, 20f);
            Assert.AreEqual(0, beside.Hits);
        }

        [Test]
        public void ALevelIsDoneAfterItsNumberOfCatchesAndNoMoreFruitComesThen()
        {
            for (var level = FruitCatcherDirector.MinLevel; level <= FruitCatcherDirector.MaxLevel; level++)
            {
                var director = new FruitCatcherDirector(level, new System.Random(5 + level));
                var spawned = new List<FallingFruit>();
                var ticks = 0;
                while (!director.LevelDone && ticks++ < 40000)
                {
                    var lowest = director.Up.OrderByDescending(f => f.Progress).FirstOrDefault();
                    director.Tick(0.05f, lowest != null ? lowest.X : 0f, spawned, null, null);
                }
                Assert.IsTrue(director.LevelDone, "level " + level);
                Assert.GreaterOrEqual(director.Hits, FruitCatcherDirector.HitsToPass(level));
                spawned.Clear();
                Run(director, 10f, spawned);
                Assert.AreEqual(0, spawned.Count);
            }
        }

        [Test]
        public void FruitStillFallingAtTheEndOfALevelGoesOnIntoTheNextOne()
        {
            var first = new FruitCatcherDirector(1, new System.Random(9));
            for (var i = 0; i < 400 && first.Up.Count < 3; i++) first.Tick(0.05f, FarAway, null, null, null);
            var carried = first.Up.ToList();
            Assert.GreaterOrEqual(carried.Count, 3);
            var second = new FruitCatcherDirector(2, new System.Random(10), carried);
            Assert.AreEqual(carried.Count, second.Up.Count);
            Assert.AreEqual(0, second.Hits, "the new level counts its own catches");
            var before = carried[0].Progress;
            second.Tick(0.1f, FarAway, null, null, null);
            Assert.Greater(carried[0].Progress, before, "carried fruit keeps falling");
        }

        [Test]
        public void TheWholeGameIsOneCoin()
        {
            Assert.AreEqual(1, FruitCatcherDirector.SessionCoins);
        }

        [Test]
        public void TheFruitPicturesTheBasketAndThePromptExist()
        {
            foreach (var name in new[] { "apple", "banana", "grape", "orange", "pear", "plum" })
                Assert.IsNotNull(Resources.Load<Sprite>("Art/arcade/fruit_" + name), "fruit_" + name);
            Assert.IsNotNull(Resources.Load<Sprite>("Art/arcade/prop_basket"));
            var lines = VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            Assert.AreEqual("Catch the fruit!", lines["fruitcatch_find"]);
            Assert.IsNotNull(Resources.Load<AudioClip>("Voice/en/fruitcatch_find"));
        }

        [Test]
        public void TheScreenHasAFullScreenTouchPadAndABasket()
        {
            var canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            var game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            game.Build(canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
            try
            {
                game.Navigator.Show(ScreenId.FruitCatcher);
                var field = canvasObject.transform.Find("ScreenRoot/FruitCatcherScreen/Field");
                Assert.IsNotNull(field.Find("Pad"));
                Assert.IsNotNull(field.Find("Basket"));
                Assert.GreaterOrEqual(field.childCount, 2 + FruitCatcherDirector.MaxUp(FruitCatcherDirector.MaxLevel));
            }
            finally
            {
                Object.DestroyImmediate(game.gameObject);
                Object.DestroyImmediate(canvasObject);
            }
        }
    }
}
