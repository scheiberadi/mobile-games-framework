using EvasLearningWorld.App;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    // Off the phone every vibration is a no-op: nothing may throw, switched on or off.
    public class HapticsTests
    {
        [TearDown]
        public void TearDown() => Haptics.Enabled = true;

        [Test]
        public void EveryPatternIsHarmlessOffTheDevice()
        {
            foreach (var on in new[] { true, false })
            {
                Haptics.Enabled = on;
                Assert.DoesNotThrow(() =>
                {
                    Haptics.Tick();
                    Haptics.Tap();
                    Haptics.Thud();
                    Haptics.Thud(5f);
                    Haptics.Buzz(-1f);
                    Haptics.Wrong();
                    Haptics.Win();
                });
            }
        }

        [Test]
        public void TheSoundEffectsSwitchAlsoSilencesTheVibrations()
        {
            var store = new FakeKeyValueStore();
            var canvas = new UnityEngine.GameObject("TestCanvas", typeof(UnityEngine.Canvas));
            var game = new UnityEngine.GameObject("TestEvaGame").AddComponent<EvaGame>();
            game.Build(canvas.GetComponent<UnityEngine.Canvas>(), store);
            game.Progress.SfxEnabled = false;
            game.ApplyAudioSettings();
            Assert.That(Haptics.Enabled, Is.False);
            game.Progress.SfxEnabled = true;
            game.ApplyAudioSettings();
            Assert.That(Haptics.Enabled, Is.True);
            UnityEngine.Object.DestroyImmediate(game.gameObject);
            UnityEngine.Object.DestroyImmediate(canvas);
        }
    }
}
