using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    // Follow 1/2/3 Instructions: Eva says which actions to do, the board also holds decoy tiles.
    public class FollowInstructionsTests
    {
        [Test]
        public void TheBoardHoldsTheSpokenTargetsPlusDecoysAndNeverFewerThanThreeTiles()
        {
            for (var count = 1; count <= 3; count++)
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 20; seed++)
            {
                var round = FollowInstructionsRoundGenerator.Create(count, level, new System.Random(seed));
                Assert.AreEqual(count, round.TargetOrder.Length);
                Assert.GreaterOrEqual(round.Choices.Length, 3, "one tile alone is no choice");
                Assert.AreEqual(round.Choices.Length, round.Choices.Distinct().Count());
                foreach (var target in round.TargetOrder) CollectionAssert.Contains(round.Choices, target);
                Assert.AreEqual(count, round.SpokenKeys.Length);
                for (var i = 0; i < count; i++)
                    Assert.AreEqual(FollowInstructionsRoundGenerator.InstructionVoicePrefix + round.TargetOrder[i], round.SpokenKeys[i]);
            }
        }

        [Test]
        public void TheTargetsVaryFromRoundToRound()
        {
            var first = Enumerable.Range(0, 30).Select(seed => FollowInstructionsRoundGenerator.Create(1, 1, new System.Random(seed)).TargetOrder[0]).Distinct().Count();
            Assert.Greater(first, 2, "the one instruction used to be the same action every round");
        }

        [Test]
        public void EveryActionHasASpokenInstructionLineAndClip()
        {
            var lines = EvasLearningWorld.App.VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            foreach (var action in FollowInstructionsRoundGenerator.AllActions)
            {
                var key = FollowInstructionsRoundGenerator.InstructionVoicePrefix + action;
                Assert.IsTrue(lines.ContainsKey(key), key);
                Assert.IsNotNull(Resources.Load<AudioClip>("Voice/en/" + key), "missing clip " + key);
            }
        }
    }
}
