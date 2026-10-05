using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    // The 37-animal Zoo & Farm roster: about the same number of animals in every habitat, every animal fully drawn,
    // and a game of the pairing kind that shows every habitat a fair share.
    public class ZooFarmRosterTests
    {
        [Test]
        public void EveryHabitatHasAboutTheSameNumberOfAnimals()
        {
            var counts = ZooFarmAnimals.All.GroupBy(a => a.Habitat).ToDictionary(g => g.Key, g => g.Count());
            Assert.That(counts.Count, Is.EqualTo(7));
            foreach (var pair in counts) Assert.That(pair.Value, Is.InRange(5, 7), pair.Key);
        }

        [Test]
        public void AnimalIdsAreUniqueAndEveryAnimalHasItsPictures()
        {
            Assert.That(ZooFarmAnimals.All.Select(a => a.Id).Distinct().Count(), Is.EqualTo(ZooFarmAnimals.All.Length));
            foreach (var a in ZooFarmAnimals.All)
            {
                Assert.That(Resources.Load<Sprite>("Art/zoofarm/animal_" + a.Id), Is.Not.Null, "animal " + a.Id);
                Assert.That(Resources.Load<Sprite>("Art/zoofarm/baby_" + a.Id), Is.Not.Null, "baby " + a.Id);
                Assert.That(Resources.Load<Sprite>("Art/" + ZooFarmAnimals.MotherSprite(a.Id)), Is.Not.Null, "mother " + a.Id);
                Assert.That(Resources.Load<Sprite>("Art/zoofarm/food_" + a.Food), Is.Not.Null, "food " + a.Food);
                Assert.That(Resources.Load<Sprite>("Art/zoofarm/covering_" + a.Covering), Is.Not.Null, "covering " + a.Covering);
                if (a.Footprint != null) Assert.That(Resources.Load<Sprite>("Art/zoofarm/footprint_" + a.Footprint), Is.Not.Null, "footprint " + a.Footprint);
                Assert.That(Resources.Load<AudioClip>("Voice/en/zoofarm_animal_" + a.Id), Is.Not.Null, "name " + a.Id);
                if (a.SpokenSound) Assert.That(Resources.Load<AudioClip>("Voice/en/zoofarm_sound_" + a.Id), Is.Not.Null, "spoken sound " + a.Id);
            }
        }

        [Test]
        public void EveryPrefixTheLevelsTakeShowsBothDomesticAndWildAndEveryRealm()
        {
            foreach (var size in new[] { 8, 12, 16, 22, 30, 37 })
            {
                var pool = ZooFarmAnimals.All.Take(size).ToArray();
                Assert.That(pool.Any(a => a.Domestic) && pool.Any(a => !a.Domestic), Is.True, "domestic/wild in " + size);
                Assert.That(pool.Select(a => a.RealmOf).Distinct().Count(), Is.EqualTo(3), "realms in " + size);
            }
        }

        [Test]
        public void AGameOfTwelveGivesEveryHabitatAShareAndKeepsEveryAnimalOnce()
        {
            for (var seed = 0; seed < 100; seed++)
            {
                var picked = PairingSession.Pick(ZooPairs.AnimalAndHabitat(), ZooPairs.PairsPerGame, new System.Random(seed));
                Assert.That(picked.Count, Is.EqualTo(ZooPairs.PairsPerGame));
                Assert.That(picked.Select(p => p.Id).Distinct().Count(), Is.EqualTo(picked.Count), "seed " + seed);
                var perHabitat = picked.GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.Count());
                Assert.That(perHabitat.Count, Is.EqualTo(7), "seed " + seed);
                foreach (var count in perHabitat.Values) Assert.That(count, Is.InRange(1, 2), "seed " + seed);
            }
        }

        [Test]
        public void OnlyAnimalsWithAUniqueRecognisablePrintAreInTheFootprintGame()
        {
            var ids = ZooFarmAnimals.All.Where(a => a.Footprint != null).Select(a => a.Id).OrderBy(i => i).ToArray();
            Assert.That(ids, Is.EqualTo(new[] { "cat", "chicken", "cow", "duck", "elephant", "frog", "horse" }));
        }

        [Test]
        public void TheSoundGameOnlyUsesAnimalsEvaCanSpeak()
        {
            for (var seed = 0; seed < 50; seed++)
            {
                var round = ZooFarmRoundGenerator.Create(ZooFarmGameKind.Sound, 6, new System.Random(seed), null);
                Assert.That(ZooFarmAnimals.All.First(a => a.Id == round.TargetId).SpokenSound, Is.True, "seed " + seed);
            }
        }
    }
}
