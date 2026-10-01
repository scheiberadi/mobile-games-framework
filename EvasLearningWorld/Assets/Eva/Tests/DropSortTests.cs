using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    // Answer-variety Prototype B (Sorting as drop-sort): the round builder's invariants.
    public class DropSortTests
    {
        [Test]
        public void EveryCategoryHasAtLeastTwoItemsAndNoItemIsListedTwice()
        {
            var catalogue = DropSortRoundBuilder.Catalogue;
            Assert.That(catalogue.Select(c => c.Id).Distinct().Count(), Is.EqualTo(catalogue.Count));
            foreach (var category in DropSortRoundBuilder.Categories)
                Assert.That(catalogue.Count(c => c.Category == category), Is.GreaterThanOrEqualTo(2), category);
        }

        [Test]
        public void ItemAndBinCountsFollowTheLevelTableAndStayWithinTheLimits()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = DropSortRoundBuilder.Create(level, new System.Random(seed));
                Assert.That(round.ItemIds.Length, Is.EqualTo(DropSortRoundBuilder.ItemCountByLevel[level - 1]), "level " + level);
                Assert.That(round.BinCategories.Length, Is.EqualTo(DropSortRoundBuilder.BinCountByLevel[level - 1]), "level " + level);
                Assert.That(round.ItemIds.Length, Is.LessThanOrEqualTo(DropSortRoundBuilder.MaxItems));
                Assert.That(round.BinCategories.Length, Is.LessThanOrEqualTo(DropSortRoundBuilder.MaxBins));
                Assert.That(round.ItemIds.Distinct().Count(), Is.EqualTo(round.ItemIds.Length), "no item twice, level " + level + " seed " + seed);
                Assert.That(round.BinCategories.Distinct().Count(), Is.EqualTo(round.BinCategories.Length));
            }
        }

        [Test]
        public void EveryItemBelongsToAShownBinAndEveryShownBinReceivesAnItem()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = DropSortRoundBuilder.Create(level, new System.Random(seed));
                for (var i = 0; i < round.ItemIds.Length; i++)
                {
                    var bin = round.BinIndexOf(i);
                    Assert.That(bin, Is.InRange(0, round.BinCategories.Length - 1), round.ItemIds[i]);
                    var expected = DropSortRoundBuilder.Catalogue.First(c => c.Id == round.ItemIds[i]).Category;
                    Assert.That(round.BinCategories[bin], Is.EqualTo(expected));
                }
                foreach (var category in round.BinCategories)
                    Assert.That(round.ItemCategories.Count(c => c == category), Is.GreaterThanOrEqualTo(1), "empty bin " + category);
            }
        }

        [Test]
        public void EveryShownBinGetsTwoOrMoreItemsWhenTheRoundHasRoomForIt()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            {
                var items = DropSortRoundBuilder.ItemCountByLevel[level - 1];
                var bins = DropSortRoundBuilder.BinCountByLevel[level - 1];
                if (items < bins * 2) continue;
                for (var seed = 0; seed < 100; seed++)
                {
                    var round = DropSortRoundBuilder.Create(level, new System.Random(seed));
                    foreach (var category in round.BinCategories)
                        Assert.That(round.ItemCategories.Count(c => c == category), Is.GreaterThanOrEqualTo(2), "level " + level + " " + category);
                }
            }
        }

        [Test]
        public void ARoundIsDeterministicPerSeedAndLevelsOutsideTheLadderThrow()
        {
            var a = DropSortRoundBuilder.Create(4, new System.Random(5));
            var b = DropSortRoundBuilder.Create(4, new System.Random(5));
            Assert.That(b.ItemIds, Is.EqualTo(a.ItemIds));
            Assert.That(b.BinCategories, Is.EqualTo(a.BinCategories));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => DropSortRoundBuilder.Create(0, new System.Random(1)));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => DropSortRoundBuilder.Create(7, new System.Random(1)));
        }

        // Eva says what each item is when it is sorted (braingym_category_<id>); every catalogue item needs its line.
        [Test]
        public void EveryCatalogueItemHasItsSortedVoiceLine()
        {
            var lines = EvasLearningWorld.App.VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            foreach (var (id, _) in DropSortRoundBuilder.Catalogue)
                Assert.That(lines.ContainsKey("braingym_category_" + id), Is.True, id);
            Assert.That(lines.ContainsKey("sorting_drag_hint"), Is.True);
            Assert.That(lines.ContainsKey("sorting_drag_demo"), Is.True);
        }
    }
}
