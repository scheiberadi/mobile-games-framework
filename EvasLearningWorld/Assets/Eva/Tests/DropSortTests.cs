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

        // Zoo & Farm and Science Lab sorting games on the same presenter. Eva's line after a drop is keyed by the bin
        // (<prefix><category>), so a two-bucket game gets two clips, not one per item.
        private static System.Collections.Generic.IEnumerable<(string Name, System.Func<int, System.Collections.Generic.IReadOnlyList<(string Id, string Category)>> Catalogue, string ItemSprites, string BinSprites, string VoicePrefix, string PromptKey)> BuildingSorts()
        {
            yield return ("domesticvswild", l => ZooFarmRoundGenerator.DropSortCatalogue(ZooFarmGameKind.DomesticVsWild, l), "zoofarm/animal_", "zoofarm/bucket_", "zoofarm_ds_", "zoofarm_prompt_domestic_wild");
            yield return ("landseaair", l => ZooFarmRoundGenerator.DropSortCatalogue(ZooFarmGameKind.LandSeaAir, l), "zoofarm/animal_", "zoofarm/bucket_", "zoofarm_ds_", "zoofarm_prompt_land_sea_air");
            yield return ("classification", l => ZooFarmRoundGenerator.DropSortCatalogue(ZooFarmGameKind.Classification, l), "zoofarm/animal_", "zoofarm/bucket_", "zoofarm_ds_", "zoofarm_prompt_classification");
            yield return ("livingvsnonliving", l => ScienceLabRoundGenerator.DropSortCatalogue(ScienceLabGameKind.LivingVsNonLiving), "sciencelab/object_", "sciencelab/bucket_", "sciencelab_ds_", "sciencelab_prompt_livingvsnonliving");
            yield return ("seasons", l => ScienceLabRoundGenerator.DropSortCatalogue(ScienceLabGameKind.Seasons), "sciencelab/activity_", "sciencelab/season_", "sciencelab_ds_", "sciencelab_prompt_seasons");
            yield return ("daynight", l => ScienceLabRoundGenerator.DropSortCatalogue(ScienceLabGameKind.DayNight), "sciencelab/activity_", "sciencelab/daynight_", "sciencelab_ds_", "sciencelab_prompt_daynight");
        }

        [Test]
        public void ZooAndScienceSortRoundsAreSoundAtEveryLevel()
        {
            foreach (var game in BuildingSorts())
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            {
                var catalogue = game.Catalogue(level);
                var categoryCount = catalogue.Select(c => c.Category).Distinct().Count();
                for (var seed = 0; seed < 100; seed++)
                {
                    var round = DropSortRoundBuilder.Create(catalogue, level, new System.Random(seed));
                    var label = game.Name + " level " + level + " seed " + seed;
                    Assert.That(round.BinCategories.Length, Is.EqualTo(System.Math.Min(DropSortRoundBuilder.BinCountByLevel[level - 1], categoryCount)), label);
                    Assert.That(round.ItemIds.Length, Is.InRange(round.BinCategories.Length, DropSortRoundBuilder.ItemCountByLevel[level - 1]), label);
                    Assert.That(round.ItemIds.Distinct().Count(), Is.EqualTo(round.ItemIds.Length), label);
                    for (var i = 0; i < round.ItemIds.Length; i++)
                        Assert.That(round.BinCategories[round.BinIndexOf(i)], Is.EqualTo(catalogue.First(c => c.Id == round.ItemIds[i]).Category), label);
                    foreach (var category in round.BinCategories)
                        Assert.That(round.ItemCategories.Count(c => c == category), Is.GreaterThanOrEqualTo(1), "empty bin " + category + " " + label);
                }
            }
        }

        [Test]
        public void ZooAndScienceSortGamesHaveTheirPicturesAndVoiceLines()
        {
            var lines = EvasLearningWorld.App.VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            foreach (var game in BuildingSorts())
            {
                Assert.That(lines.ContainsKey(game.PromptKey), Is.True, game.PromptKey);
                for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
                    foreach (var (id, category) in game.Catalogue(level))
                    {
                        Assert.That(lines.ContainsKey(game.VoicePrefix + category), Is.True, game.VoicePrefix + category);
                        Assert.That(Resources.Load<AudioClip>("Voice/en/" + game.VoicePrefix + category), Is.Not.Null, "clip " + game.VoicePrefix + category);
                        Assert.That(Resources.Load<Sprite>("Art/" + game.ItemSprites + id), Is.Not.Null, game.ItemSprites + id);
                        Assert.That(Resources.Load<Sprite>("Art/" + game.BinSprites + category), Is.Not.Null, game.BinSprites + category);
                    }
            }
        }

        [Test]
        public void EveryBinStartsWithItsOwnKindOfResidents()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
                for (var seed = 0; seed < 30; seed++)
                {
                    var catalogue = ZooFarmRoundGenerator.DropSortCatalogue(ZooFarmGameKind.DomesticVsWild, level);
                    var round = DropSortRoundBuilder.Create(catalogue, level, new System.Random(seed));
                    Assert.That(round.Residents.Length, Is.EqualTo(round.BinCategories.Length));
                    for (var b = 0; b < round.BinCategories.Length; b++)
                    {
                        Assert.That(round.Residents[b].Length, Is.EqualTo(DropSortRoundBuilder.ResidentsPerBin), "level " + level + " seed " + seed);
                        foreach (var id in round.Residents[b])
                        {
                            Assert.That(catalogue.First(c => c.Id == id).Category, Is.EqualTo(round.BinCategories[b]));
                        }
                    }
                }
        }

        [Test]
        public void DomesticVsWildKeepsSeaAnimalsOutOfThePastureAndPutsTheFarmFirst()
        {
            var sea = ZooFarmAnimals.WaterOnly.ToArray();
            Assert.That(sea.Length, Is.GreaterThan(0));
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            {
                var catalogue = ZooFarmRoundGenerator.DropSortCatalogue(ZooFarmGameKind.DomesticVsWild, level);
                Assert.That(catalogue.Any(c => sea.Contains(c.Id)), Is.False, "level " + level);
                Assert.That(catalogue.Any(c => c.Id == "frog"), Is.True, "the frog stays");
                for (var seed = 0; seed < 30; seed++)
                {
                    var round = DropSortRoundBuilder.Create(catalogue, level, new System.Random(seed)).WithBinOrder("domestic", "wild");
                    Assert.That(round.BinCategories[0], Is.EqualTo("domestic"));
                    foreach (var id in round.Residents[0]) Assert.That(catalogue.First(c => c.Id == id).Category, Is.EqualTo("domestic"));
                    for (var i = 0; i < round.ItemIds.Length; i++)
                        Assert.That(round.BinIndexOf(i), Is.EqualTo(round.ItemCategories[i] == "domestic" ? 0 : 1));
                }
            }
        }

        [Test]
        public void LandSeaAirAlwaysHasAllThreeZonesInTheirOrderAndFiveResidentsEach()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            {
                var catalogue = ZooFarmRoundGenerator.DropSortCatalogue(ZooFarmGameKind.LandSeaAir, level);
                for (var seed = 0; seed < 30; seed++)
                {
                    var round = DropSortRoundBuilder.Create(catalogue, level, new System.Random(seed), 3).WithBinOrder("air", "land", "sea");
                    Assert.That(round.BinCategories, Is.EqualTo(new[] { "air", "land", "sea" }), "level " + level + " seed " + seed);
                    for (var b = 0; b < 3; b++)
                    {
                        Assert.That(round.Residents[b].Length, Is.EqualTo(DropSortRoundBuilder.ResidentsPerBin));
                        foreach (var id in round.Residents[b]) Assert.That(catalogue.First(c => c.Id == id).Category, Is.EqualTo(round.BinCategories[b]));
                    }
                    for (var i = 0; i < round.ItemIds.Length; i++)
                        Assert.That(round.BinIndexOf(i), Is.EqualTo(System.Array.IndexOf(round.BinCategories, round.ItemCategories[i])));
                    Assert.That(round.ItemCategories.Distinct().Count(), Is.EqualTo(3), "every zone gets at least one animal");
                }
            }
        }
    }
}
