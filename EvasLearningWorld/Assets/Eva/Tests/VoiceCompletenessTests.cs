using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    // Every key in voice-lines.txt must have a matching recorded clip, so Eva never falls silent on a real line.
    // A missing clip is not a crash (Voice.Say still "speaks" for a text-based duration), so nothing else would catch it.
    public class VoiceCompletenessTests
    {
        private static Dictionary<string, string> LoadLines()
        {
            var asset = Resources.Load<TextAsset>("Voice/voice-lines");
            Assert.IsNotNull(asset, "missing Resources/Voice/voice-lines.txt");
            return EvasLearningWorld.App.VoiceLines.Parse(asset.text);
        }

        [Test]
        public void EveryVoiceLineKeyHasAnEnglishClip()
        {
            var lines = LoadLines();
            Assert.Greater(lines.Count, 0, "voice-lines.txt parsed to zero keys");
            foreach (var key in lines.Keys)
                Assert.IsNotNull(Resources.Load<AudioClip>("Voice/en/" + key), "missing clip: Voice/en/" + key + ".mp3");
        }
    }
}
