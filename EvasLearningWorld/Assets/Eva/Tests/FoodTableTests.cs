using System;
using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class FoodTableTests
    {
        private static int Healthy(FoodTable table) => Enumerable.Range(0, FoodTable.Slots).Count(i => FoodTable.IsHealthy(table.FoodAt(i)));

        private static void AssertTable(FoodTable table, string when)
        {
            var foods = Enumerable.Range(0, FoodTable.Slots).Select(table.FoodAt).ToArray();
            Assert.That(foods.Distinct().Count(), Is.EqualTo(FoodTable.Slots), "no food twice " + when);
            Assert.That(Healthy(table), Is.GreaterThanOrEqualTo(FoodTable.MinHealthyOnTable), "healthy " + when);
            Assert.That(FoodTable.Slots - Healthy(table), Is.GreaterThanOrEqualTo(FoodTable.MinUnhealthyOnTable), "unhealthy " + when);
        }

        [Test]
        public void FourteenFoodsSevenHealthyAndSevenNot()
        {
            Assert.AreEqual(14, FoodTable.Foods.Count);
            Assert.AreEqual(7, FoodTable.Foods.Count(f => f.Healthy));
        }

        [Test]
        public void TheTableAlwaysHasTwoHealthyOneUnhealthyAndNoRepeats()
        {
            for (var seed = 0; seed < 40; seed++)
            {
                var table = new FoodTable(new Random(seed));
                AssertTable(table, "at the start");
                for (var step = 0; step < 60; step++)
                {
                    // Feed what is there: mostly healthy, sometimes an unhealthy one.
                    var wantHealthy = step % 5 != 4;
                    var slot = Enumerable.Range(0, FoodTable.Slots).First(i => FoodTable.IsHealthy(table.FoodAt(i)) == wantHealthy);
                    table.Feed(slot);
                    AssertTable(table, "after step " + step + " seed " + seed);
                }
            }
        }

        [Test]
        public void AHealthyFoodLightsAStarAndIsReplacedAnUnhealthyOneDimsOneAndStaysPut()
        {
            var table = new FoodTable(new Random(1));
            var good = Enumerable.Range(0, 4).First(i => FoodTable.IsHealthy(table.FoodAt(i)));
            var bad = Enumerable.Range(0, 4).First(i => !FoodTable.IsHealthy(table.FoodAt(i)));
            var goodBefore = table.FoodAt(good);
            var badBefore = table.FoodAt(bad);
            Assert.AreEqual(FoodResult.Unhealthy, table.Feed(bad));
            Assert.AreEqual(0, table.Stars, "never below none");
            Assert.AreEqual(badBefore, table.FoodAt(bad));
            Assert.AreEqual(FoodResult.Healthy, table.Feed(good));
            Assert.AreEqual(1, table.Stars);
            Assert.AreNotEqual(goodBefore, table.FoodAt(good));
            Assert.AreEqual(FoodResult.Unhealthy, table.Feed(bad));
            Assert.AreEqual(0, table.Stars);
        }

        [Test]
        public void FiveHealthyFoodsFinishTheRound()
        {
            var table = new FoodTable(new Random(2));
            for (var i = 0; i < FoodTable.StarsNeeded; i++)
            {
                Assert.IsFalse(table.Done);
                table.Feed(table.HealthySlot());
            }
            Assert.IsTrue(table.Done);
            Assert.AreEqual(FoodTable.StarsNeeded, table.Stars);
        }

        [Test]
        public void TwoUnhealthyInARowCallForAHintAndAHealthyOneClearsTheCount()
        {
            var table = new FoodTable(new Random(3));
            var bad = Enumerable.Range(0, 4).First(i => !FoodTable.IsHealthy(table.FoodAt(i)));
            table.Feed(bad);
            Assert.IsFalse(table.NeedsHint);
            table.Feed(bad);
            Assert.IsTrue(table.NeedsHint);
            table.HintGiven();
            Assert.IsFalse(table.NeedsHint);
            table.Feed(bad);
            table.Feed(table.HealthySlot());
            table.Feed(bad);
            Assert.IsFalse(table.NeedsHint);
        }
    }
}
