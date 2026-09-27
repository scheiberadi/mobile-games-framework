using System;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class CountingTests
    {
        private static int MaxFor(int level) => new[] { 3, 5, 10, 20, 10, 15 }[level - 1];
        private static int ChoiceCountFor(int level) => level <= 2 ? 3 : level <= 4 ? 5 : 6;

        [Test]
        public void EveryRoundHasAValidQuantityAndChoicesWithinTheLevelsRange()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var r = CountRoundGenerator.Create(level, new Random(seed), null);
                var max = MaxFor(level);
                Assert.That(r.Quantity, Is.InRange(1, max));
                Assert.That(r.Choices.Length, Is.EqualTo(ChoiceCountFor(level)));
                Assert.That(r.Choices, Is.Unique);
                Assert.That(r.Choices, Does.Contain(r.Quantity));
                Assert.That(r.Choices, Is.All.InRange(1, max));
            }
        }

        [Test]
        public void ObjectDiffersFromThePreviousRound()
        {
            for (var seed = 0; seed < 100; seed++)
                foreach (CountObject previous in Enum.GetValues(typeof(CountObject)))
                    Assert.That(CountRoundGenerator.Create(2, new Random(seed), previous).Object, Is.Not.EqualTo(previous));
        }

        [Test]
        public void SameSeedGivesSameRound()
        {
            var a = CountRoundGenerator.Create(3, new Random(7), null);
            var b = CountRoundGenerator.Create(3, new Random(7), null);
            Assert.That(a.Quantity, Is.EqualTo(b.Quantity));
            Assert.That(a.Object, Is.EqualTo(b.Object));
            Assert.That(a.Choices, Is.EqualTo(b.Choices));
        }

        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CountRoundGenerator.Create(0, new Random(1), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => CountRoundGenerator.Create(7, new Random(1), null));
        }

        [Test]
        public void AnswerChoicesAlwaysHaveTheLevelsExactCountWithNoDuplicates()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 300; seed++)
            {
                var round = CountRoundGenerator.Create(level, new Random(seed), null);
                Assert.That(round.Choices.Length, Is.EqualTo(ChoiceCountFor(level)));
                Assert.That(round.Choices, Is.Unique);
                Assert.That(round.Choices, Does.Contain(round.Quantity));
            }
        }

        [Test]
        public void TallyCountsEachObjectOnceInTheOrderTheChildTapsThem()
        {
            var tally = new CountTally(3);
            Assert.That(tally.TryCount(2, out var n), Is.True);
            Assert.That(n, Is.EqualTo(1));
            Assert.That(tally.TryCount(0, out n), Is.True);
            Assert.That(n, Is.EqualTo(2));
            Assert.That(tally.IsComplete, Is.False);
            Assert.That(tally.TryCount(1, out n), Is.True);
            Assert.That(n, Is.EqualTo(3));
            Assert.That(tally.IsComplete, Is.True);
        }

        [Test]
        public void TappingAnAlreadyCountedObjectDoesNotAdvanceTheCount()
        {
            var tally = new CountTally(4);
            tally.TryCount(1, out _);
            tally.TryCount(3, out _);
            Assert.That(tally.TryCount(1, out var n), Is.False);
            Assert.That(n, Is.EqualTo(2));
            Assert.That(tally.Counted, Is.EqualTo(2));
            Assert.That(tally.IsCounted(1), Is.True);
            Assert.That(tally.IsCounted(0), Is.False);
        }

        [Test]
        public void OutOfRangeIndexIsIgnored()
        {
            var tally = new CountTally(2);
            Assert.That(tally.TryCount(-1, out _), Is.False);
            Assert.That(tally.TryCount(2, out _), Is.False);
            Assert.That(tally.Counted, Is.EqualTo(0));
        }

        // --- DifficultyLadder (spec 4.3) --------------------------------------------------------------------

        [Test]
        public void FourOrMoreCleanRoundsOfFiveLevelsUpAtRoundFive()
        {
            var buffer = new List<bool>();
            var level = 2;
            bool[] outcomes = { true, true, true, true, false }; // 4 clean, 1 demonstrated
            foreach (var clean in outcomes) level = DifficultyLadder.RecordRound(buffer, level, clean);
            Assert.That(level, Is.EqualTo(3));
            Assert.That(buffer, Is.Empty); // real change clears the buffer
        }

        [Test]
        public void ThreeOrMoreDemonstratedRoundsOfFiveLevelsDownAtRoundFive()
        {
            var buffer = new List<bool>();
            var level = 2;
            bool[] outcomes = { false, false, false, true, true }; // 3 demonstrated, 2 clean
            foreach (var clean in outcomes) level = DifficultyLadder.RecordRound(buffer, level, clean);
            Assert.That(level, Is.EqualTo(1));
            Assert.That(buffer, Is.Empty);
        }

        [Test]
        public void RoundSixEvaluatesRoundsTwoThroughSixNotRoundsOneThroughFiveAgain()
        {
            var buffer = new List<bool>();
            var level = 3;
            // Rounds 1-5: 3 clean, 2 demonstrated - the one 5-round split that fires neither threshold, so
            // round 5 leaves the level unchanged and the buffer full.
            bool[] firstFive = { true, true, true, false, false };
            foreach (var clean in firstFive) level = DifficultyLadder.RecordRound(buffer, level, clean);
            Assert.That(level, Is.EqualTo(3));
            Assert.That(buffer.Count, Is.EqualTo(5));

            // Round 6 is demonstrated. If the window correctly slides (drops round 1's clean, keeps rounds
            // 2-5, adds round 6), the latest 5 is clean,clean,demonstrated,demonstrated,demonstrated - 2 clean,
            // 3 demonstrated - which fires level-down. This is only decisive because round 1 dropped out: had
            // it (wrongly) stayed in the window instead of round 5, the composition would still read 3
            // clean/2 demonstrated and not fire.
            level = DifficultyLadder.RecordRound(buffer, level, false);
            Assert.That(level, Is.EqualTo(2));
        }

        [Test]
        public void BufferClearsOnARealLevelChangeAndNeedsFiveFreshRoundsBeforeTheNextEvaluation()
        {
            var buffer = new List<bool>();
            var level = 1;
            for (var i = 0; i < 5; i++) level = DifficultyLadder.RecordRound(buffer, level, true); // 5/5 clean
            Assert.That(level, Is.EqualTo(2));
            Assert.That(buffer, Is.Empty);

            // The next 4 rounds, even all clean, must not evaluate yet (buffer isn't full).
            for (var i = 0; i < 4; i++)
            {
                level = DifficultyLadder.RecordRound(buffer, level, true);
                Assert.That(level, Is.EqualTo(2));
            }
            Assert.That(buffer.Count, Is.EqualTo(4));

            // The 5th fresh round completes the window and evaluates.
            level = DifficultyLadder.RecordRound(buffer, level, true);
            Assert.That(level, Is.EqualTo(3));
        }

        [Test]
        public void LevelClampedAtSixDoesNotExceedTheMaximum()
        {
            var buffer = new List<bool>();
            var level = 6;
            for (var i = 0; i < 5; i++) level = DifficultyLadder.RecordRound(buffer, level, true); // 5/5 clean
            Assert.That(level, Is.EqualTo(6));
            Assert.That(DifficultyLadder.MaxLevel, Is.EqualTo(6));
        }

        [Test]
        public void LevelFourClimbsToFiveThenSixOneStepAtATime()
        {
            var buffer = new List<bool>();
            var level = 4;
            for (var i = 0; i < 5; i++) level = DifficultyLadder.RecordRound(buffer, level, true);
            Assert.That(level, Is.EqualTo(5));
            Assert.That(buffer, Is.Empty);
            for (var i = 0; i < 5; i++) level = DifficultyLadder.RecordRound(buffer, level, true);
            Assert.That(level, Is.EqualTo(6));
        }

        [Test]
        public void LevelsFiveAndSixHaveSixChoicesAndTheirQuantityMax()
        {
            for (var seed = 0; seed < 300; seed++)
            {
                var five = CountRoundGenerator.Create(5, new Random(seed), null);
                var six = CountRoundGenerator.Create(6, new Random(seed), null);
                Assert.That(five.Choices.Length, Is.EqualTo(6));
                Assert.That(six.Choices.Length, Is.EqualTo(6));
                Assert.That(five.Quantity, Is.InRange(1, 10));
                Assert.That(six.Quantity, Is.InRange(1, 15));
            }
        }

        [Test]
        public void DistractorsAppearOnlyAtLevelsFiveAndSixWithinBoundsNeverTheTargetTypeAndTotalAtMostTwenty()
        {
            var sawTwoTypes = false;
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 500; seed++)
            {
                var r = CountRoundGenerator.Create(level, new Random(seed), null);
                if (level < 5) { Assert.That(r.Distractors, Is.Empty); continue; }
                Assert.That(r.Distractors.Length, Is.InRange(level == 5 ? 2 : 3, level == 5 ? 6 : 8));
                Assert.That(r.Distractors, Is.All.Not.EqualTo(r.Object));
                Assert.That(r.TotalItems, Is.EqualTo(r.Quantity + r.Distractors.Length));
                Assert.That(r.TotalItems, Is.LessThanOrEqualTo(20));
                var kinds = new HashSet<CountObject>(r.Distractors);
                Assert.That(kinds.Count, Is.LessThanOrEqualTo(level == 5 ? 1 : 2));
                if (kinds.Count == 2) sawTwoTypes = true;
            }
            Assert.IsTrue(sawTwoTypes);
        }

        [Test]
        public void MixedRoundsAreDeterministicPerSeed()
        {
            for (var level = 5; level <= 6; level++)
            {
                var a = CountRoundGenerator.Create(level, new Random(11), null);
                var b = CountRoundGenerator.Create(level, new Random(11), null);
                Assert.That(a.Distractors, Is.EqualTo(b.Distractors));
                Assert.That(a.AssignSlots(new Random(3)), Is.EqualTo(b.AssignSlots(new Random(3))));
            }
        }

        [Test]
        public void SlotsHoldExactlyQuantityTargetsAndTheDistractorsAndOnlyTargetsGetCountingIndexes()
        {
            for (var seed = 0; seed < 300; seed++)
            {
                var r = CountRoundGenerator.Create(6, new Random(seed), null);
                var slots = r.AssignSlots(new Random(seed));
                var order = r.TargetIndexBySlot(slots);
                Assert.That(slots.Length, Is.EqualTo(r.TotalItems));
                var expected = 0;
                for (var i = 0; i < slots.Length; i++)
                {
                    if (slots[i] == r.Object) Assert.That(order[i], Is.EqualTo(expected++));
                    else Assert.That(order[i], Is.EqualTo(-1));
                }
                Assert.That(expected, Is.EqualTo(r.Quantity));
                // A tally sized to the target quantity is complete after exactly the target slots, never more.
                var tally = new CountTally(r.Quantity);
                foreach (var idx in order) if (idx >= 0) tally.TryCount(idx, out _);
                Assert.IsTrue(tally.IsComplete);
                Assert.IsFalse(tally.TryCount(-1, out _)); // a distractor (index -1) never counts
                Assert.That(tally.Counted, Is.EqualTo(r.Quantity));
            }
        }

        [Test]
        public void LevelClampedAtOneDoesNotDropBelowTheMinimum()
        {
            var buffer = new List<bool>();
            var level = 1;
            for (var i = 0; i < 5; i++) level = DifficultyLadder.RecordRound(buffer, level, false); // 5/5 demonstrated
            Assert.That(level, Is.EqualTo(1));
        }

        [Test]
        public void LevelMovesByExactlyOneStepEvenForAFiveOfFiveExtremeWindow()
        {
            var buffer = new List<bool>();
            var level = 2;
            for (var i = 0; i < 5; i++) level = DifficultyLadder.RecordRound(buffer, level, true); // 5/5 clean
            Assert.That(level, Is.EqualTo(3)); // moved by exactly 1, not straight to the top
        }

        [Test]
        public void LevelOneWithTheLevelDownConditionMetStaysClampedAndTheBufferKeepsRolling()
        {
            var buffer = new List<bool>();
            var level = 1;
            bool[] firstFive = { false, false, false, true, true }; // 3 demonstrated -> would fire level-down
            foreach (var clean in firstFive) level = DifficultyLadder.RecordRound(buffer, level, clean);
            Assert.That(level, Is.EqualTo(1)); // clamped: treated as no-change
            Assert.That(buffer, Is.EqualTo(firstFive)); // not cleared - a clamp is not a real change

            // The very next round evaluates rounds 2-6, not a fresh 1-5: dropping round 1 (demonstrated) and
            // adding round 6 (clean) leaves demonstrated,demonstrated,true,true,true = 2 demonstrated, 3 clean,
            // which is not decisive either - proving the buffer rolled instead of resetting.
            level = DifficultyLadder.RecordRound(buffer, level, true);
            Assert.That(level, Is.EqualTo(1));
            Assert.That(buffer, Is.EqualTo(new List<bool> { false, false, true, true, true }));
        }

        [Test]
        public void LevelSixWithTheLevelUpConditionMetStaysClampedAndTheBufferKeepsRolling()
        {
            var buffer = new List<bool>();
            var level = 6;
            bool[] firstFive = { true, true, true, true, false }; // 4 clean -> would fire level-up
            foreach (var clean in firstFive) level = DifficultyLadder.RecordRound(buffer, level, clean);
            Assert.That(level, Is.EqualTo(6)); // clamped: treated as no-change
            Assert.That(buffer, Is.EqualTo(firstFive));

            // Rounds 2-6: drop round 1 (clean), add round 6 (demonstrated) -> true,true,true,false,false =
            // 3 clean, 2 demonstrated - not decisive, proving the buffer rolled instead of resetting.
            level = DifficultyLadder.RecordRound(buffer, level, false);
            Assert.That(level, Is.EqualTo(6));
            Assert.That(buffer, Is.EqualTo(new List<bool> { true, true, true, false, false }));
        }

        [Test]
        public void AtMostOneOfLevelUpOrLevelDownFiresForEveryPossibleFiveRoundCleanDemonstratedSplit()
        {
            for (var cleanCount = 0; cleanCount <= 5; cleanCount++)
            {
                var buffer = new List<bool>();
                var level = 2; // interior level: level-up (-> 3) and level-down (-> 1) are both reachable and distinct
                for (var i = 0; i < 5; i++) level = DifficultyLadder.RecordRound(buffer, level, i < cleanCount);

                var demonstratedCount = 5 - cleanCount;
                var leveledUp = level == 3;
                var leveledDown = level == 1;
                Assert.That(leveledUp && leveledDown, Is.False, $"cleanCount={cleanCount} fired both level-up and level-down");

                if (cleanCount >= 4) Assert.That(level, Is.EqualTo(3), $"cleanCount={cleanCount} should level up");
                else if (demonstratedCount >= 3) Assert.That(level, Is.EqualTo(1), $"cleanCount={cleanCount} should level down");
                else Assert.That(level, Is.EqualTo(2), $"cleanCount={cleanCount} should not change");
            }
        }
    }
}
