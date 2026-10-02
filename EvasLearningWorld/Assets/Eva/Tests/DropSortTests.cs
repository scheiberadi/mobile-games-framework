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

        // Step 6 rollout: Recycling, Match Item to Category and Sort Laundry/Chores use the same builder with their own catalogues.
        private static readonly (string Name, System.Collections.Generic.IReadOnlyList<(string Id, string Category)> Catalogue, string ItemSprites, string BinSprites, string VoicePrefix)[] Rollout =
        {
            ("recycling", DropSortRoundBuilder.RecyclingCatalogue, "waste_", "bin_", "braingym_bin_"),
            ("matchitemtocategory", DropSortRoundBuilder.ItemToCategoryCatalogue, "catitem_", "categorylabel_", "braingym_categorylabel_"),
            ("sortlaundrychores", DropSortRoundBuilder.ChoresCatalogue, "choreitem_", "room_", "braingym_room_"),
        };

        [Test]
        public void RolloutRoundsNeverShowAnEmptyBinOrRepeatAnItemAndNeverAskForMoreThanTheLevelTable()
        {
            foreach (var game in Rollout)
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var round = DropSortRoundBuilder.Create(game.Catalogue, level, new System.Random(seed));
                var label = game.Name + " level " + level + " seed " + seed;
                Assert.That(round.BinCategories.Length, Is.EqualTo(DropSortRoundBuilder.BinCountByLevel[level - 1]), label);
                Assert.That(round.ItemIds.Length, Is.InRange(round.BinCategories.Length, DropSortRoundBuilder.ItemCountByLevel[level - 1]), label);
                Assert.That(round.ItemIds.Distinct().Count(), Is.EqualTo(round.ItemIds.Length), label);
                for (var i = 0; i < round.ItemIds.Length; i++)
                {
                    var expected = game.Catalogue.First(c => c.Id == round.ItemIds[i]).Category;
                    Assert.That(round.BinCategories[round.BinIndexOf(i)], Is.EqualTo(expected), label);
                }
                foreach (var category in round.BinCategories)
                    Assert.That(round.ItemCategories.Count(c => c == category), Is.GreaterThanOrEqualTo(1), "empty bin " + category + " " + label);
            }
        }

        [Test]
        public void RolloutGamesHaveTheirPicturesAndSortedVoiceLines()
        {
            var lines = EvasLearningWorld.App.VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            foreach (var game in Rollout)
            {
                Assert.That(lines.ContainsKey("braingym_prompt_" + game.Name), Is.True, game.Name);
                foreach (var (id, category) in game.Catalogue)
                {
                    Assert.That(lines.ContainsKey(game.VoicePrefix + id), Is.True, game.VoicePrefix + id);
                    Assert.That(Resources.Load<Sprite>("Art/braingym/" + game.ItemSprites + id), Is.Not.Null, game.ItemSprites + id);
                    Assert.That(Resources.Load<Sprite>("Art/braingym/" + game.BinSprites + category), Is.Not.Null, game.BinSprites + category);
                }
            }
        }
    }
}
