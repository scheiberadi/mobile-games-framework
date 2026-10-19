using EvasLearningWorld.App;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    // Every named effect needs its generated clip (tools/sfx/generate.js), otherwise Sfx quietly falls back to a plain beep.
    public class SfxTests
    {
        [Test]
        public void EveryEffectHasAClipInResources()
        {
            foreach (var name in Sfx.Names)
            {
                var clip = Resources.Load<AudioClip>("Sfx/" + name);
                Assert.IsNotNull(clip, "missing clip: Resources/Sfx/" + name + ".wav (run node tools/sfx/generate.js)");
                Assert.Greater(clip.length, 0.05f, name + " is too short");
                // The fishing reel and the detector tone are loops (a real recording of the reel is 1.9 s).
                var loop = name == "reelout" || name == "reelin" || name == "dethot";
                // The weather sounds (rain, storm, wind...) last a few seconds on purpose.
                var weather = name.StartsWith("weather_");
                // The ambient weather loops (Dress for weather) are just under 3 s.
                loop |= name.StartsWith("ambient_");
                // The Workshop toys moving (the car drives off and back, the rocket counts down and launches...) last a few seconds on purpose.
                var toy = name == "car_go" || name == "boat_go" || name == "rocket_rumble" || name == "rocket_launch" || name == "robot_dance" || name == "door_open" || name == "car_cross";
                Assert.Less(clip.length, weather || toy ? 4f : loop ? 3f : 1.5f, name + " is too long for an effect");
            }
        }

        [Test]
        public void EveryEffectPlaysWithoutThrowing()
        {
            var go = new GameObject("SfxTest", typeof(Sfx));
            try
            {
                var sfx = go.GetComponent<Sfx>();
                sfx.Tap(); sfx.Pick(); sfx.Drop(); sfx.Place(); sfx.Right(); sfx.Retry(); sfx.Coin();
                sfx.Buy(); sfx.Win(); sfx.Hint(); sfx.Pop();
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
