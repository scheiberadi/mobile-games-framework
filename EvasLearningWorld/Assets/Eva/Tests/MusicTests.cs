using EvasLearningWorld.App;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    // Every track needs its generated loop (tools/music/generate.js) and every screen the right track.
    public class MusicTests
    {
        [Test]
        public void EveryTrackHasALoopInResources()
        {
            foreach (var name in MusicTracks.Names)
            {
                var clip = Resources.Load<AudioClip>("Music/" + name);
                Assert.IsNotNull(clip, "missing clip: Resources/Music/" + name + ".wav (run node tools/music/generate.js)");
                Assert.That(clip.length, Is.InRange(10f, 40f), name + " should be a loop of a few bars");
                var data = new float[clip.samples];
                clip.GetData(data, 0);
                var sum = 0.0;
                foreach (var v in data) sum += v * v;
                Assert.Greater(System.Math.Sqrt(sum / data.Length), 0.05, name + " is silent (NaN in the generator?)");
            }
        }

        [Test]
        public void EveryTrackAScreenAsksForExists()
        {
            foreach (ScreenId screen in System.Enum.GetValues(typeof(ScreenId)))
            {
                var track = MusicTracks.For(screen);
                if (track != null) Assert.Contains(track, MusicTracks.Names, screen + " asks for an unknown track");
            }
        }

        [Test]
        public void MapArcadeAndBunnyRunHaveTheirOwnMusic()
        {
            Assert.AreEqual("map", MusicTracks.For(ScreenId.Map));
            Assert.AreEqual("arcade", MusicTracks.For(ScreenId.Arcade));
            Assert.AreEqual("bunnyrun", MusicTracks.For(ScreenId.Platformer));
            Assert.AreEqual("fishing", MusicTracks.For(ScreenId.Fishing));
            Assert.IsNull(MusicTracks.For(ScreenId.Settings));
        }

        [Test]
        public void AGameOpenedFromABuildingKeepsTheBuildingsMusic()
        {
            Assert.AreEqual("school", MusicTracks.For(ScreenId.Count, ScreenId.School));
            Assert.AreEqual("zoofarm", MusicTracks.For(ScreenId.ZooFarmHabitat, ScreenId.ZooFarm));
            Assert.AreEqual("whack", MusicTracks.For(ScreenId.WhackAMole, ScreenId.Arcade));
            Assert.IsNull(MusicTracks.For(ScreenId.Count));
            foreach (var building in new[] { ScreenId.School, ScreenId.Playground, ScreenId.ZooFarm, ScreenId.ScienceLab, ScreenId.Workshop, ScreenId.ArtStudio, ScreenId.BrainGym, ScreenId.FriendsPark, ScreenId.StoreActivities })
                Assert.IsNotNull(MusicTracks.For(building), building + " has no music");
        }

        [Test]
        public void ThePlayerRemembersWhatTheScreenAsksFor()
        {
            var go = new GameObject("MusicTest", typeof(MusicPlayer));
            try
            {
                var player = go.GetComponent<MusicPlayer>();
                player.Play("map");
                Assert.AreEqual("map", player.Wanted);
                player.Play(null);
                Assert.IsNull(player.Wanted);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
