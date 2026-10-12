using System.Linq;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class PlantGardenTests
    {
        private static GardenPlant Plant(GardenRoundSpec spec, int seed = 1) => new GardenPlant(PlantGarden.Kinds[0], spec, new System.Random(seed));

        private static GardenSupply[] Asked(GardenPlant plant) => new[] { GardenSupply.Water, GardenSupply.Sun, GardenSupply.Fertilizer }.Where(plant.Asks).ToArray();

        private static void Hold(GardenPlant plant, bool water, bool sun, bool fert, float seconds)
        {
            for (var t = 0f; t < seconds; t += 1f / 60f) plant.Step(1f / 60f, water, sun, fert);
        }

        private static void Give(GardenPlant plant, GardenSupply supply, float seconds)
            => Hold(plant, supply == GardenSupply.Water, supply == GardenSupply.Sun, supply == GardenSupply.Fertilizer, seconds);

        // Give everything the plant asks for (each dose: held, then taken away).
        private static void FeedFully(GardenPlant plant)
        {
            var stage = plant.Stage;
            for (var guard = 0; plant.Stage == stage && guard < 50; guard++)
            {
                foreach (var supply in Asked(plant).Where(s => !plant.Complete(s)).ToArray())
                {
                    Give(plant, supply, GardenPlant.DoseSeconds + 0.1f);
                    Hold(plant, false, false, false, 0.1f);
                }
            }
        }

        [Test]
        public void AFreshPlantIsASeedAskingForSomething()
        {
            var plant = Plant(new GardenRoundSpec(1, 1, 1));
            Assert.That(plant.Stage, Is.EqualTo(0));
            Assert.That(Asked(plant).Length, Is.InRange(1, 3));
            foreach (var supply in Asked(plant)) Assert.That(plant.Required(supply), Is.EqualTo(1));
        }

        [Test]
        public void TheDoseCountsStayInsideTheRoundsRange()
        {
            var spec = new GardenRoundSpec(1, 2, 3);
            for (var seed = 0; seed < 40; seed++)
            {
                var plant = Plant(spec, seed);
                foreach (var supply in Asked(plant)) Assert.That(plant.Required(supply), Is.InRange(2, 3));
            }
        }

        [Test]
        public void EveryMixOfTheThreeThingsTurnsUpOverTime()
        {
            var seen = new System.Collections.Generic.HashSet<int>();
            for (var seed = 0; seed < 200; seed++)
            {
                var plant = Plant(new GardenRoundSpec(1, 1, 1), seed);
                seen.Add(Asked(plant).Aggregate(0, (mask, s) => mask | 1 << (int)s));
            }
            Assert.That(seen.Count, Is.EqualTo(7));
        }

        [Test]
        public void GivingTheThingsAskedForGrowsThePlantOneStage()
        {
            var plant = Plant(new GardenRoundSpec(1, 1, 1));
            FeedFully(plant);
            Assert.That(plant.Stage, Is.EqualTo(1));
        }

        [Test]
        public void AShortTouchIsNotADose()
        {
            var plant = Plant(new GardenRoundSpec(1, 1, 1));
            var supply = Asked(plant)[0];
            Give(plant, supply, GardenPlant.DoseSeconds * 0.4f);
            Hold(plant, false, false, false, 0.2f);
            Give(plant, supply, GardenPlant.DoseSeconds * 0.4f);
            Assert.That(plant.Given(supply), Is.EqualTo(0), "held in two short goes");
        }

        [Test]
        public void ManyDosesNeedTheThingTakenAwayBetweenThem()
        {
            GardenPlant plant = null;
            for (var seed = 0; plant == null; seed++)
            {
                var candidate = Plant(new GardenRoundSpec(1, 2, 2), seed);
                if (Asked(candidate).Length == 1) plant = candidate;
            }
            var supply = Asked(plant)[0];
            Give(plant, supply, 5f);
            Assert.That(plant.Given(supply), Is.EqualTo(1), "holding on does not give a second dose");
            Hold(plant, false, false, false, 0.1f);
            Give(plant, supply, GardenPlant.DoseSeconds + 0.1f);
            Assert.That(plant.Stage, Is.EqualTo(1));
        }

        [Test]
        public void ATwoDoseThingIsStillOwedAfterOneDose()
        {
            GardenPlant plant = null;
            for (var seed = 0; plant == null; seed++)
            {
                var candidate = Plant(new GardenRoundSpec(1, 2, 2), seed);
                if (Asked(candidate).Length == 1) plant = candidate;
            }
            var supply = Asked(plant)[0];
            Give(plant, supply, GardenPlant.DoseSeconds + 0.1f);
            Assert.That(plant.Stage, Is.EqualTo(0));
            Assert.That(plant.Complete(supply), Is.False);
        }

        [Test]
        public void SomethingNotAskedForNeverGrowsItButIsReportedOnce()
        {
            GardenPlant plant = null;
            for (var seed = 0; plant == null; seed++)
            {
                var candidate = Plant(new GardenRoundSpec(1, 1, 1), seed);
                if (!candidate.Asks(GardenSupply.Water)) plant = candidate;
            }
            var told = 0;
            for (var t = 0f; t < 4f; t += 1f / 60f)
            {
                plant.Step(1f / 60f, true, false, false);
                told += plant.JustUnasked.Count;
            }
            Assert.That(told, Is.EqualTo(1));
            Assert.That(plant.Stage, Is.EqualTo(0));
        }

        [Test]
        public void ARoundOfDifferentGardenSizesHasAllPlantsFeedableToFlower()
        {
            for (var round = 0; round < PlantGarden.RoundsPerSession; round++)
            {
                var garden = new PlantGarden(round, new System.Random(round));
                Assert.That(garden.Plants.Count, Is.EqualTo(PlantGarden.Rounds[round].Plants));
                Assert.That(garden.Plants.Select(p => p.Kind.Id).Distinct().Count(), Is.EqualTo(System.Math.Min(garden.Plants.Count, PlantGarden.Kinds.Length)), "different kinds side by side");
                foreach (var plant in garden.Plants)
                    for (var guard = 0; !plant.Done && guard < 100; guard++) FeedFully(plant);
                Assert.That(garden.Done, Is.True);
                Assert.That(garden.Plants.All(p => p.Stage == p.Stages - 1));
            }
        }

        [Test]
        public void EveryKindOfPlantHasAPictureForEachOfItsStages()
        {
            foreach (var kind in PlantGarden.Kinds)
            {
                Assert.That(kind.Stages, Is.GreaterThanOrEqualTo(3), kind.Id);
                for (var stage = 0; stage < kind.Stages; stage++)
                    Assert.IsNotNull(UnityEngine.Resources.Load<UnityEngine.Sprite>("Art/sciencelab/plant_" + kind.Id + "_" + stage), kind.Id + " " + stage);
            }
        }

        [Test]
        public void TheSessionGetsHarderEachRound()
        {
            Assert.That(PlantGarden.RoundsPerSession, Is.EqualTo(3));
            for (var i = 1; i < PlantGarden.Rounds.Length; i++)
            {
                var easier = PlantGarden.Rounds[i - 1];
                var harder = PlantGarden.Rounds[i];
                Assert.That(harder.Plants >= easier.Plants && harder.MaxDoses >= easier.MaxDoses && (harder.Plants > easier.Plants || harder.MaxDoses > easier.MaxDoses));
            }
        }
    }
}
