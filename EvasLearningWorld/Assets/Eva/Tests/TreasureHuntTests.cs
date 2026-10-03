using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    public class TreasureHuntTests
    {
        private const float Step = 1f / 60f;

        private static HiddenItem FirstTreasure(TreasureHuntDirector director) => director.Items.First(i => !i.Junk && !i.Found);

        // Puts the detector `distance` away from the item (to the left of it), far from every other item as far as the sand allows.
        private static void StandAt(TreasureHuntDirector director, HiddenItem item, float distance) =>
            director.MoveDetector(item.X - distance, item.Y);

        private static HuntEvents Wait(TreasureHuntDirector director, float seconds)
        {
            var all = HuntEvents.None;
            for (var t = 0f; t < seconds; t += Step) all |= director.Tick(Step, false);
            return all;
        }

        // A child who slides the detector straight to the treasure, waits for the X, and taps until it is out.
        private static void FindNextTreasure(TreasureHuntDirector director)
        {
            var item = FirstTreasure(director);
            director.MoveDetector(item.X, item.Y);
            var events = Wait(director, TreasuresHold());
            Assert.IsTrue((events & HuntEvents.Marked) != 0, "marked");
            var revealed = false;
            for (var i = 0; i < 20 && !revealed; i++) revealed = (director.Tick(Step, true) & HuntEvents.Revealed) != 0;
            Assert.IsTrue(revealed, "dug out");
        }

        private static float TreasuresHold() => TreasureHuntDirector.HoldSeconds + 0.2f;

        [Test]
        public void SixLevelsGetHarder()
        {
            for (var level = TreasureHuntDirector.MinLevel + 1; level <= TreasureHuntDirector.MaxLevel; level++)
            {
                Assert.Less(TreasureHuntDirector.HotRadius(level), TreasureHuntDirector.HotRadius(level - 1), "hot zone shrinks");
                Assert.LessOrEqual(TreasureHuntDirector.FarRadius(level), TreasureHuntDirector.FarRadius(level - 1), "less far to hear");
                Assert.GreaterOrEqual(TreasureHuntDirector.TreasuresToFind(level), TreasureHuntDirector.TreasuresToFind(level - 1));
                Assert.GreaterOrEqual(TreasureHuntDirector.DigsNeeded(level), TreasureHuntDirector.DigsNeeded(level - 1));
                Assert.GreaterOrEqual(TreasureHuntDirector.JunkCount(level), TreasureHuntDirector.JunkCount(level - 1));
            }
            Assert.IsTrue(TreasureHuntDirector.ShowsGlow(1));
            Assert.IsFalse(TreasureHuntDirector.ShowsGlow(3), "from level 3 only the sound helps");
            Assert.AreEqual(0, TreasureHuntDirector.JunkCount(1));
            Assert.Greater(TreasureHuntDirector.JunkCount(6), 0);
        }

        [Test]
        public void EverythingIsBuriedInTheSandAndApart()
        {
            for (var seed = 0; seed < 100; seed++)
            for (var level = TreasureHuntDirector.MinLevel; level <= TreasureHuntDirector.MaxLevel; level++)
            {
                var director = new TreasureHuntDirector(new System.Random(seed));
                director.StartLevel(level);
                Assert.AreEqual(TreasureHuntDirector.TreasuresToFind(level) + TreasureHuntDirector.JunkCount(level), director.Items.Count);
                foreach (var a in director.Items)
                {
                    Assert.That(a.X, Is.InRange(TreasureHuntDirector.MinX, TreasureHuntDirector.MaxX));
                    Assert.That(a.Y, Is.InRange(TreasureHuntDirector.MinY, TreasureHuntDirector.MaxY));
                    foreach (var b in director.Items)
                        if (a != b) Assert.GreaterOrEqual(Vector2.Distance(new Vector2(a.X, a.Y), new Vector2(b.X, b.Y)), 2f * TreasureHuntDirector.HotRadius(1) + 20f, "far enough apart that their zones never touch");
                }
            }
        }

        [Test]
        public void EachLevelStartsWithTheDetectorFarFromEverythingBuried()
        {
            for (var seed = 0; seed < 100; seed++)
            for (var level = TreasureHuntDirector.MinLevel; level <= TreasureHuntDirector.MaxLevel; level++)
            {
                var director = new TreasureHuntDirector(new System.Random(seed));
                director.StartLevel(level);
                foreach (var item in director.Items)
                    Assert.Greater(Vector2.Distance(new Vector2(director.DetectorX, director.DetectorY), new Vector2(item.X, item.Y)),
                        TreasureHuntDirector.HotRadius(level) + 150f, "level " + level + " seed " + seed);
                Assert.AreEqual(DetectorSignal.Silent, director.Signal);
            }
        }

        [Test]
        public void TheDetectorIsSilentFarAwayBeepsFasterWhenCloserAndIsContinuousOverIt()
        {
            var director = new TreasureHuntDirector(new System.Random(3));
            var item = FirstTreasure(director);
            var hot = TreasureHuntDirector.HotRadius(1);
            var far = TreasureHuntDirector.FarRadius(1);

            StandAt(director, item, far + 50f);
            director.Tick(Step, false);
            Assert.AreEqual(DetectorSignal.Silent, director.Signal);

            StandAt(director, item, far - 5f);
            director.Tick(Step, false);
            Assert.AreEqual(DetectorSignal.Beeping, director.Signal);
            var slow = director.BeepInterval;

            StandAt(director, item, (hot + far) / 2f);
            director.Tick(Step, false);
            var middle = director.BeepInterval;

            StandAt(director, item, hot + 5f);
            director.Tick(Step, false);
            var fast = director.BeepInterval;

            Assert.Greater(slow, middle);
            Assert.Greater(middle, fast);
            Assert.Less(fast, 0.25f);

            StandAt(director, item, 0f);
            director.Tick(Step, false);
            Assert.AreEqual(DetectorSignal.Continuous, director.Signal);
            Assert.AreEqual(1f, director.Closeness, 0.001f);
        }

        [Test]
        public void BeepsComeAtTheInterval()
        {
            var director = new TreasureHuntDirector(new System.Random(4));
            var item = FirstTreasure(director);
            StandAt(director, item, (TreasureHuntDirector.HotRadius(1) + TreasureHuntDirector.FarRadius(1)) / 2f);
            director.Tick(Step, false);
            var interval = director.BeepInterval;
            var beeps = 0;
            for (var t = 0f; t < 6f; t += Step)
                if ((director.Tick(Step, false) & HuntEvents.Beep) != 0) beeps++;
            Assert.AreEqual(6f / interval, beeps, 1.5f);
        }

        [Test]
        public void TheSpotIsMarkedOnlyAfterStayingOverItForAMoment()
        {
            var director = new TreasureHuntDirector(new System.Random(5));
            var item = FirstTreasure(director);

            director.MoveDetector(item.X, item.Y);
            Assert.IsTrue((Wait(director, 0.3f) & HuntEvents.Marked) == 0, "passing over it is not enough");
            StandAt(director, item, TreasureHuntDirector.HotRadius(1) + 80f);
            Wait(director, 0.2f);
            director.MoveDetector(item.X, item.Y);
            Assert.IsTrue((Wait(director, 0.3f) & HuntEvents.Marked) == 0, "and leaving starts the wait over");
            Assert.IsTrue((Wait(director, 0.5f) & HuntEvents.Marked) != 0, "staying marks it");
            Assert.AreEqual(HuntPhase.Marked, director.Phase);
            Assert.AreSame(item, director.Marked);
        }

        [Test]
        public void EveryTapDigsAndTheLastOneBringsTheTreasureOut()
        {
            var director = new TreasureHuntDirector(new System.Random(6));
            director.StartLevel(3);
            var treasure = FirstTreasure(director);
            Assert.IsTrue((director.Tick(Step, true) & HuntEvents.Dug) == 0, "a tap on the sand does nothing yet");

            director.MoveDetector(treasure.X, treasure.Y);
            Wait(director, TreasuresHold());
            Assert.AreEqual(HuntPhase.Marked, director.Phase);

            var needed = TreasureHuntDirector.DigsNeeded(3);
            for (var i = 1; i < needed; i++)
            {
                var events = director.Tick(Step, true);
                Assert.IsTrue((events & HuntEvents.Dug) != 0);
                Assert.IsTrue((events & HuntEvents.Revealed) == 0, "tap " + i);
                Assert.AreEqual(i, director.Digs);
            }
            var last = director.Tick(Step, true);
            Assert.IsTrue((last & HuntEvents.Revealed) != 0);
            Assert.AreSame(treasure, director.LastRevealed);
            Assert.IsTrue(treasure.Found);
            Assert.AreEqual(1, director.Found);
            Assert.AreEqual(HuntPhase.Searching, director.Phase);
        }

        [Test]
        public void JunkDigsOutInTwoTapsAndDoesNotCount()
        {
            var director = new TreasureHuntDirector(new System.Random(7));
            director.StartLevel(4);
            var junk = director.Items.First(i => i.Junk);
            director.MoveDetector(junk.X, junk.Y);
            Wait(director, TreasuresHold());
            Assert.AreEqual(HuntPhase.Marked, director.Phase);

            Assert.IsTrue((director.Tick(Step, true) & HuntEvents.Revealed) == 0);
            Assert.IsTrue((director.Tick(Step, true) & HuntEvents.Revealed) != 0);
            Assert.IsTrue(director.LastRevealed.Junk);
            Assert.AreEqual(0, director.Found);
            Assert.AreEqual(0, director.TotalFound);
            Assert.IsFalse(director.LevelDone);
        }

        [Test]
        public void AFoundThingStopsBeeping()
        {
            var director = new TreasureHuntDirector(new System.Random(8));
            var item = FirstTreasure(director);
            FindNextTreasure(director);
            Assert.IsTrue(item.Found);
            director.MoveDetector(item.X, item.Y);
            director.Tick(Step, false);
            Assert.AreEqual(DetectorSignal.Silent, director.Signal);
        }

        [Test]
        public void ABotFindsEveryTreasureOfAllSixLevels()
        {
            var director = new TreasureHuntDirector(new System.Random(9));
            var total = 0;
            for (var level = TreasureHuntDirector.MinLevel; level <= TreasureHuntDirector.MaxLevel; level++)
            {
                director.StartLevel(level);
                Assert.IsFalse(director.LevelDone);
                while (!director.LevelDone) FindNextTreasure(director);
                Assert.AreEqual(TreasureHuntDirector.TreasuresToFind(level), director.Found);
                total += director.Found;
            }
            Assert.AreEqual(12, total);
            Assert.AreEqual(12, director.TotalFound);
        }

        [Test]
        public void TheWholeGameIsOneCoin()
        {
            Assert.AreEqual(1, TreasureHuntDirector.SessionCoins);
        }

        [Test]
        public void ThePicturesAndTheVoiceExist()
        {
            foreach (var name in new[] { "bg", "detector", "chest", "crown", "gem", "coins", "ring", "key", "can", "boot", "bottle", "mark", "shovel", "hole" })
                Assert.IsNotNull(Resources.Load<Sprite>("Art/treasure/" + name), name);
            var lines = VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            foreach (var key in new[] { "treasure_prompt", "treasure_dig", "treasure_found", "treasure_junk" })
            {
                Assert.IsTrue(lines.ContainsKey(key), key);
                Assert.IsNotNull(Resources.Load<AudioClip>("Voice/en/" + key), key);
            }
            for (var n = 1; n <= 8; n++) Assert.IsNotNull(Resources.Load<AudioClip>("Voice/en/num_" + n), "num_" + n);
        }

        [Test]
        public void TheWorldsOfBunnyRunHaveTheirPictures()
        {
            for (var world = 1; world <= 6; world++)
                foreach (var name in new[] { "bg", "ground", "obstacle1", "obstacle2", "obstacle3" })
                    Assert.IsNotNull(Resources.Load<Sprite>("Art/platformer/w" + world + "_" + name), "w" + world + "_" + name);
        }

        [Test]
        public void TheScreenHasAFullScreenPadAndTheDetector()
        {
            var canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            var game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            game.Build(canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
            try
            {
                game.Navigator.Show(ScreenId.TreasureHunt);
                var field = canvasObject.transform.Find("ScreenRoot/TreasureHuntScreen/Field");
                Assert.IsNotNull(field.Find("Pad"));
                Assert.IsNotNull(field.Find("Detector"));
                Assert.IsNotNull(field.Find("Mark"));
                Assert.IsNotNull(field.Find("Hole"));
            }
            finally
            {
                Object.DestroyImmediate(game.gameObject);
                Object.DestroyImmediate(canvasObject);
            }
        }
    }
}
