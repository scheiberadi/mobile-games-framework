using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    public class WhackAMoleTests
    {
        private static void Run(WhackAMoleDirector director, float seconds, List<UpMole> spawned = null, List<UpMole> expired = null)
        {
            for (var t = 0f; t < seconds; t += 0.05f) director.Tick(0.05f, spawned, expired);
        }

        [Test]
        public void ThePaceGetsHarderAndEveryLevelNeedsMoreHitsAndTakesLongerThanTheLast()
        {
            for (var level = WhackAMoleDirector.MinLevel + 1; level <= WhackAMoleDirector.MaxLevel; level++)
            {
                Assert.GreaterOrEqual(WhackAMoleDirector.MaxUp(level), WhackAMoleDirector.MaxUp(level - 1));
                Assert.Less(WhackAMoleDirector.StaySeconds(level), WhackAMoleDirector.StaySeconds(level - 1));
                Assert.Less(WhackAMoleDirector.SpawnGap(level), WhackAMoleDirector.SpawnGap(level - 1));
                Assert.Greater(WhackAMoleDirector.HitsToPass(level), WhackAMoleDirector.HitsToPass(level - 1));
                // Roughly how long a level lasts if the child keeps up: a faster level must still not be over sooner.
                Assert.Greater(WhackAMoleDirector.HitsToPass(level) * WhackAMoleDirector.SpawnGap(level),
                    WhackAMoleDirector.HitsToPass(level - 1) * WhackAMoleDirector.SpawnGap(level - 1), "level " + level);
            }
            Assert.AreEqual(1, WhackAMoleDirector.MaxUp(1), "level 1: one mole at a time");
            Assert.That(WhackAMoleDirector.MaxUp(6), Is.InRange(4, 5), "level 6: four or five at once");
        }

        [Test]
        public void NeverMoreMolesUpThanTheLevelAllowsAndNeverTwoInOneHole()
        {
            for (var level = WhackAMoleDirector.MinLevel; level <= WhackAMoleDirector.MaxLevel; level++)
            {
                var director = new WhackAMoleDirector(level, new System.Random(level));
                for (var i = 0; i < 400; i++)
                {
                    director.Tick(0.05f, null, null);
                    Assert.LessOrEqual(director.Up.Count, WhackAMoleDirector.MaxUp(level), "level " + level);
                    Assert.AreEqual(director.Up.Count, director.Up.Select(m => m.Hole).Distinct().Count());
                }
            }
        }

        [Test]
        public void TheBoardReallyFillsUpToTheLevelsLimitWhenTheChildIsSlow()
        {
            for (var level = WhackAMoleDirector.MinLevel; level <= WhackAMoleDirector.MaxLevel; level++)
            {
                var director = new WhackAMoleDirector(level, new System.Random(level));
                var most = 0;
                for (var i = 0; i < 1200; i++)
                {
                    director.Tick(0.05f, null, null);
                    most = Mathf.Max(most, director.Up.Count);
                }
                Assert.GreaterOrEqual(most, WhackAMoleDirector.MaxUp(level) - 1, "level " + level);
                if (level <= 5) Assert.AreEqual(WhackAMoleDirector.MaxUp(level), most, "level " + level);
            }
        }

        [Test]
        public void AMoleHidesByItselfAfterItsStayTimeAndThatIsNeverAMistake()
        {
            var director = new WhackAMoleDirector(1, new System.Random(3));
            var spawned = new List<UpMole>();
            var expired = new List<UpMole>();
            Run(director, 12f, spawned, expired);
            Assert.GreaterOrEqual(expired.Count, 1);
            Assert.AreEqual(0, director.Hits, "an unwhacked mole is simply gone, nothing is lost");
            Assert.IsFalse(director.LevelDone);
            Assert.IsTrue(expired.All(m => spawned.Contains(m)));
        }

        [Test]
        public void WhackingAMoleCountsAHitAndAnEmptyHoleDoesNothing()
        {
            var director = new WhackAMoleDirector(6, new System.Random(4));
            for (var i = 0; i < 400 && director.Up.Count == 0; i++) director.Tick(0.05f, null, null);
            var mole = director.Up.First();
            Assert.IsTrue(director.Whack(mole.Hole));
            Assert.AreEqual(1, director.Hits);
            Assert.IsFalse(director.Up.Contains(mole));
            var empty = Enumerable.Range(0, WhackAMoleDirector.Holes).First(h => director.Up.All(m => m.Hole != h));
            Assert.IsFalse(director.Whack(empty));
            Assert.AreEqual(1, director.Hits);
        }

        [Test]
        public void ALevelIsDoneAfterItsNumberOfHitsAndNoMoreMolesComeThen()
        {
            for (var level = WhackAMoleDirector.MinLevel; level <= WhackAMoleDirector.MaxLevel; level++)
            {
                var director = new WhackAMoleDirector(level, new System.Random(5 + level));
                var spawned = new List<UpMole>();
                var ticks = 0;
                while (!director.LevelDone && ticks++ < 20000)
                {
                    director.Tick(0.05f, spawned, null);
                    foreach (var mole in director.Up.ToList()) director.Whack(mole.Hole);
                }
                Assert.AreEqual(WhackAMoleDirector.HitsToPass(level), director.Hits, "level " + level);
                spawned.Clear();
                Run(director, 10f, spawned);
                Assert.AreEqual(0, spawned.Count);
            }
        }

        [Test]
        public void TheWholeGameIsOneCoin()
        {
            Assert.AreEqual(1, WhackAMoleDirector.SessionCoins);
        }

        [Test]
        public void TheMolePicturesTheLevelUpSoundAndTheVoiceLineExist()
        {
            Assert.IsNotNull(Resources.Load<Sprite>("Art/arcade/wam_hole"), "wam_hole");
            Assert.IsNotNull(Resources.Load<Sprite>("Art/arcade/wam_mole"), "wam_mole");
            Assert.IsNotNull(Resources.Load<Sprite>("Art/arcade/prop_sparkle"), "prop_sparkle");
            Assert.Contains("levelup", Sfx.Names);
            var lines = VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            Assert.AreEqual("Whack the moles!", lines["whackamole_find"]);
            Assert.IsNotNull(Resources.Load<AudioClip>("Voice/en/whackamole_find"));
        }

        [Test]
        public void TheScreenShowsNineBigHolesThatDoNotOverlapEachOther()
        {
            var canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            var game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            game.Build(canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
            try
            {
                game.Navigator.Show(ScreenId.WhackAMole);
                var field = canvasObject.transform.Find("ScreenRoot/WhackAMoleScreen/Field");
                var holes = field.GetComponentsInChildren<TapTarget>(false);
                Assert.AreEqual(9, holes.Length);
                var rects = holes.Select(h => (RectTransform)h.transform).ToArray();
                foreach (var rect in rects)
                {
                    Assert.GreaterOrEqual(rect.rect.width, EvaUi.MinTap);
                    Assert.GreaterOrEqual(rect.rect.height, EvaUi.MinTap);
                }
                for (var i = 0; i < rects.Length; i++)
                for (var j = i + 1; j < rects.Length; j++)
                {
                    var dx = Mathf.Abs(rects[i].anchoredPosition.x - rects[j].anchoredPosition.x);
                    var dy = Mathf.Abs(rects[i].anchoredPosition.y - rects[j].anchoredPosition.y);
                    Assert.IsTrue(dx >= EvaUi.MinTap - 1f || dy >= EvaUi.MinTap - 1f, rects[i].name + " overlaps " + rects[j].name);
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
