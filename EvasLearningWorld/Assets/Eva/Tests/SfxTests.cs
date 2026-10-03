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
                Assert.Less(clip.length, loop ? 3f : 1.5f, name + " is too long for an effect");
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
