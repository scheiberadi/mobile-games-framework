using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    // Tapping a building makes Eva say its line. A place with no line in voice-lines.txt walks off in silence (only the tap
    // effect is heard), and VoiceCompletenessTests cannot notice because it only checks keys that are already in the file.
    public class PlaceVoiceTests
    {
        [Test]
        public void EveryPlaceHasASpokenLineAndClip()
        {
            var lines = VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            foreach (var place in Places.All)
            {
                Assert.IsFalse(string.IsNullOrEmpty(place.VoiceKey), place.Id + " has no voice key");
                Assert.IsTrue(lines.ContainsKey(place.VoiceKey), place.Id + ": '" + place.VoiceKey + "' is not in voice-lines.txt");
                Assert.IsNotNull(Resources.Load<AudioClip>("Voice/en/" + place.VoiceKey), place.Id + ": missing clip Voice/en/" + place.VoiceKey);
            }
        }
    }
}
