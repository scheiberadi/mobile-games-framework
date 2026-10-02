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
        public void ThePaceGetsHarderWithEveryLevel()
        {
            for (var level = DifficultyLadder.MinLevel + 1; level <= DifficultyLadder.MaxLevel; level++)
            {
                Assert.GreaterOrEqual(WhackAMoleDirector.MaxUp(level), WhackAMoleDirector.MaxUp(level - 1));
                Assert.Less(WhackAMoleDirector.StaySeconds(level), WhackAMoleDirector.StaySeconds(level - 1));
                Assert.Less(WhackAMoleDirector.SpawnGap(level), WhackAMoleDirector.SpawnGap(level - 1));
                Assert.GreaterOrEqual(WhackAMoleDirector.DecoyChance(level), WhackAMoleDirector.DecoyChance(level - 1));
            }
            Assert.AreEqual(1, WhackAMoleDirector.MaxUp(1), "level 1: one mole at a time");
            Assert.That(WhackAMoleDirector.MaxUp(6), Is.InRange(4, 5), "level 6: four or five at once");
        }

        [Test]
        public void NeverMoreMolesUpThanTheLevelAllowsAndNeverTwoInOneHole()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            {
                var director = new WhackAMoleDirector(level, "a", new System.Random(level));
                for (var i = 0; i < 400; i++)
                {
                    director.Tick(0.05f, null, null);
                    Assert.LessOrEqual(director.Up.Count, WhackAMoleDirector.MaxUp(level), "level " + level);
                    Assert.AreEqual(director.Up.Count, director.Up.Select(m => m.Hole).Distinct().Count());
                }
            }
        }

        [Test]
        public void LevelOneHasNoDecoysAndALaterLevelAlwaysKeepsATargetOnTheBoard()
        {
            var one = new WhackAMoleDirector(1, "c", new System.Random(1));
            var spawned = new List<UpMole>();
            Run(one, 40f, spawned);
            Assert.IsTrue(spawned.Count > 5 && spawned.All(m => m.IsTarget && m.MoleId == "c"));

            var six = new WhackAMoleDirector(6, "c", new System.Random(2));
            for (var i = 0; i < 600; i++)
            {
                six.Tick(0.05f, null, null);
                if (six.Up.Count > 0) Assert.IsTrue(six.Up.Any(m => m.IsTarget), "a target mole is always up when any mole is");
            }
        }

        [Test]
        public void AMoleHidesByItselfAfterItsStayTime()
        {
            var director = new WhackAMoleDirector(1, "a", new System.Random(3));
            var spawned = new List<UpMole>();
            var expired = new List<UpMole>();
            Run(director, 12f, spawned, expired);
            Assert.GreaterOrEqual(expired.Count, 1);
            Assert.AreEqual(0, director.Hits, "an unwhacked mole is simply gone, nothing is lost");
        }

        [Test]
        public void WhackingATargetCountsAHitADecoyOnlyAWrongWhackAndAnEmptyHoleNothing()
        {
            var director = new WhackAMoleDirector(6, "a", new System.Random(4));
            for (var i = 0; i < 400 && !(director.Up.Any(m => m.IsTarget) && director.Up.Any(m => !m.IsTarget)); i++) director.Tick(0.05f, null, null);
            var target = director.Up.First(m => m.IsTarget);
            var decoy = director.Up.FirstOrDefault(m => !m.IsTarget);
            if (decoy != null)
            {
                Assert.AreEqual(WhackResult.Decoy, director.Whack(decoy.Hole));
                Assert.AreEqual(1, director.WrongWhacks);
                Assert.IsTrue(director.Up.Contains(decoy), "a decoy that is tapped stays up");
            }
            Assert.AreEqual(WhackResult.Target, director.Whack(target.Hole));
            Assert.AreEqual(1, director.Hits);
            Assert.IsFalse(director.Up.Contains(target));
            var empty = Enumerable.Range(0, WhackAMoleDirector.Holes).First(h => director.Up.All(m => m.Hole != h));
            Assert.AreEqual(WhackResult.Empty, director.Whack(empty));
        }

        [Test]
        public void TheRoundIsDoneAfterTheLevelsNumberOfHitsAndNoMoreMolesComeThen()
        {
            var director = new WhackAMoleDirector(2, "b", new System.Random(5));
            var spawned = new List<UpMole>();
            while (!director.RoundDone)
            {
                director.Tick(0.05f, spawned, null);
                foreach (var mole in director.Up.ToList()) if (mole.IsTarget) director.Whack(mole.Hole);
            }
            Assert.AreEqual(WhackAMoleDirector.HitsPerRound(2), director.Hits);
            spawned.Clear();
            Run(director, 10f, spawned);
            Assert.AreEqual(0, spawned.Count);
        }

        [Test]
        public void TheNextTargetIsNeverThePreviousOne()
        {
            var rng = new System.Random(6);
            var previous = "a";
            for (var i = 0; i < 100; i++)
            {
                var next = WhackAMoleDirector.NextTarget(previous, rng);
                Assert.AreNotEqual(previous, next);
                previous = next;
            }
        }

        [Test]
        public void EveryMoleHasItsArtAndTheNewPromptHasAVoiceLine()
        {
            foreach (var id in WhackAMoleDirector.MoleIds)
            {
                Assert.IsNotNull(Resources.Load<Sprite>("Art/arcade/mole_" + id), "mole_" + id);
                Assert.IsNotNull(Resources.Load<Sprite>("Art/arcade/molecard_" + id), "molecard_" + id);
            }
            var lines = VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            Assert.IsTrue(lines.ContainsKey("whackamole_find"));
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
