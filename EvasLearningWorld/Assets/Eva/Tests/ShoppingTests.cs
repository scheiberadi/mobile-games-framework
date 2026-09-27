using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class ShoppingTests
    {
        [Test]
        public void LevelOutOfRangeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ShoppingRoundGenerator.Create(0, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => ShoppingRoundGenerator.Create(7, new Random(1)));
        }

        [Test]
        public void SameSeedGivesSameRoundAtEveryLevel()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            {
                var a = ShoppingRoundGenerator.Create(level, new Random(7));
                var b = ShoppingRoundGenerator.Create(level, new Random(7));
                Assert.That(a.Mode, Is.EqualTo(b.Mode), "level " + level);
                Assert.That(a.Denomination, Is.EqualTo(b.Denomination), "level " + level);
                Assert.That(a.Price, Is.EqualTo(b.Price), "level " + level);
                Assert.That(a.CorrectIndex, Is.EqualTo(b.CorrectIndex), "level " + level);
            }
        }

        [Test]
        public void RoundsPerSessionMatchesTheSharedFiveRoundWindow()
        {
            Assert.That(ShoppingRoundGenerator.RoundsPerSession, Is.EqualTo(5));
        }

        // Level == mode, a deliberate departure from every other game this session - see ShoppingRoundGenerator's
        // own class comment for why.
        [Test]
        public void EachLevelProducesItsOwnMode()
        {
            var expected = new[]
            {
                ShoppingMode.Recognize, ShoppingMode.ExactPayment, ShoppingMode.Addition,
                ShoppingMode.Change, ShoppingMode.ComparePrices, ShoppingMode.Budget,
            };
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 20; seed++)
            {
                var round = ShoppingRoundGenerator.Create(level, new Random(seed));
                Assert.That(round.Mode, Is.EqualTo(expected[level - 1]), "level " + level + " seed " + seed);
            }
        }

        [Test]
        public void RecognizeChoicesContainTheDenominationExactlyOnceAndAreDistinct()
        {
            for (var seed = 0; seed < 300; seed++)
            {
                var round = ShoppingRoundGenerator.Create(1, new Random(seed));
                Assert.That(round.Choices.Count(c => c == round.Denomination), Is.EqualTo(1), "seed " + seed);
                Assert.That(round.Choices.Distinct().Count(), Is.EqualTo(round.Choices.Length), "seed " + seed);
                Assert.That(round.CorrectIndex, Is.EqualTo(Array.IndexOf(round.Choices, round.Denomination)), "seed " + seed);
            }
        }

        // The classic same-digit-different-magnitude mistake (1/10, 2/20, 5/50) is guaranteed among the choices
        // whenever the partner is itself a valid denomination - it always is, since MagnitudePartner only pairs
        // values that are both in Denominations.
        [Test]
        public void RecognizeGuaranteesTheMagnitudeConfusablePartner()
        {
            var partners = new System.Collections.Generic.Dictionary<int, int>
            {
                { 1, 10 }, { 10, 1 }, { 2, 20 }, { 20, 2 }, { 5, 50 }, { 50, 5 },
            };
            for (var seed = 0; seed < 300; seed++)
            {
                var round = ShoppingRoundGenerator.Create(1, new Random(seed));
                if (!partners.TryGetValue(round.Denomination, out var partner)) continue;
                Assert.IsTrue(round.Choices.Contains(partner), "seed " + seed + " denomination " + round.Denomination);
            }
        }

        [Test]
        public void ExactPaymentChoicesContainThePriceExactlyOnceAndAreDistinct()
        {
            for (var seed = 0; seed < 300; seed++)
            {
                var round = ShoppingRoundGenerator.Create(2, new Random(seed));
                Assert.That(round.Choices.Count(c => c == round.Price), Is.EqualTo(1), "seed " + seed);
                Assert.That(round.Choices.Distinct().Count(), Is.EqualTo(round.Choices.Length), "seed " + seed);
                Assert.That(round.CorrectIndex, Is.EqualTo(Array.IndexOf(round.Choices, round.Price)), "seed " + seed);
            }
        }

        [Test]
        public void AdditionComboChoicesHaveDistinctSumsAndTheCorrectComboSumsToThePrice()
        {
            for (var seed = 0; seed < 300; seed++)
            {
                var round = ShoppingRoundGenerator.Create(3, new Random(seed));
                var sums = round.ComboChoices.Select(combo => combo[0] + combo[1]).ToArray();
                Assert.That(sums.Distinct().Count(), Is.EqualTo(sums.Length), "seed " + seed);
                Assert.That(sums[round.CorrectIndex], Is.EqualTo(round.Price), "seed " + seed);
                Assert.That(sums.Count(s => s == round.Price), Is.EqualTo(1), "seed " + seed);
            }
        }

        [Test]
        public void ChangePaidIsAlwaysMoreThanPriceAndChoicesContainTheChangeWithOffByOneGuarantee()
        {
            for (var seed = 0; seed < 300; seed++)
            {
                var round = ShoppingRoundGenerator.Create(4, new Random(seed));
                Assert.Greater(round.Paid, round.Price, "seed " + seed);
                var change = round.Paid - round.Price;
                Assert.That(round.Choices.Count(c => c == change), Is.EqualTo(1), "seed " + seed);
                Assert.That(round.Choices.Distinct().Count(), Is.EqualTo(round.Choices.Length), "seed " + seed);
                Assert.That(round.CorrectIndex, Is.EqualTo(Array.IndexOf(round.Choices, change)), "seed " + seed);
                Assert.IsTrue(round.Choices.Contains(change - 1) || round.Choices.Contains(change + 1),
                    "seed " + seed + " missing an off-by-one distractor around " + change);
            }
        }

        [Test]
        public void ComparePricesCorrectIndexIsTheStrictlyCheaperItemAndPricesAreDistinct()
        {
            for (var seed = 0; seed < 300; seed++)
            {
                var round = ShoppingRoundGenerator.Create(5, new Random(seed));
                Assert.AreNotEqual(round.Item, round.Item2, "seed " + seed);
                Assert.AreNotEqual(round.Price, round.Price2, "seed " + seed);
                var cheaperIndex = round.Price < round.Price2 ? 0 : 1;
                Assert.That(round.CorrectIndex, Is.EqualTo(cheaperIndex), "seed " + seed);
            }
        }

        [Test]
        public void BudgetHasExactlyOneAffordableItemAndDistinctPrices()
        {
            for (var seed = 0; seed < 300; seed++)
            {
                var round = ShoppingRoundGenerator.Create(6, new Random(seed));
                Assert.That(round.Items.Length, Is.EqualTo(3), "seed " + seed);
                Assert.That(round.Prices.Length, Is.EqualTo(3), "seed " + seed);
                Assert.That(round.Prices.Distinct().Count(), Is.EqualTo(3), "seed " + seed);
                Assert.That(round.Items.Distinct().Count(), Is.EqualTo(3), "seed " + seed);
                var affordable = round.Prices.Count(p => p <= round.Budget);
                Assert.That(affordable, Is.EqualTo(1), "seed " + seed);
                Assert.That(round.Prices[round.CorrectIndex], Is.LessThanOrEqualTo(round.Budget), "seed " + seed);
            }
        }

        [Test]
        public void IsNoteMatchesTheDocumentedTwentyAndUpBoundary()
        {
            Assert.IsFalse(ShoppingRoundGenerator.IsNote(1));
            Assert.IsFalse(ShoppingRoundGenerator.IsNote(10));
            Assert.IsTrue(ShoppingRoundGenerator.IsNote(20));
            Assert.IsTrue(ShoppingRoundGenerator.IsNote(100));
        }
    }
}
