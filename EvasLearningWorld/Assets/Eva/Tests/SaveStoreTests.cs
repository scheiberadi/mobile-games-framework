using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;

namespace EvasLearningWorld.Tests
{
    public class SaveStoreTests
    {
        [Test]
        public void AFullyPopulatedProgressRoundTrips()
        {
            var fake = new FakeKeyValueStore();
            var saved = new PlayerProgress
            {
                HasCharacter = true,
                Look = new CharacterLook { Head = 2, Skin = 4, Shirt = 1 },
                Tutorial = TutorialStep.GoToStore,
                CountIntroSeen = true,
            };
            saved.AddCoins(9);
            saved.GrantStarter();
            saved.Owned.Add("rug");
            saved.House.TryPlace("sofa", "living_seat", saved.Owned);
            saved.House.TryPlace("rug", "living_floor", saved.Owned);

            new SaveStore(fake).Save(saved);
            var loaded = new SaveStore(fake).Load();

            Assert.That(loaded.HasCharacter, Is.True);
            Assert.That(loaded.Look.Head, Is.EqualTo(2));
            Assert.That(loaded.Look.Skin, Is.EqualTo(4));
            Assert.That(loaded.Look.Shirt, Is.EqualTo(1));
            Assert.That(loaded.Coins, Is.EqualTo(9));
            Assert.That(loaded.Owned, Is.EqualTo(new[] { "sofa", "rug" }));
            Assert.That(loaded.House.Placements.Count, Is.EqualTo(2));
            Assert.That(loaded.House.SlotOf("sofa"), Is.EqualTo("living_seat"));
            Assert.That(loaded.House.ItemIn("living_floor"), Is.EqualTo("rug"));
            Assert.That(loaded.Tutorial, Is.EqualTo(TutorialStep.GoToStore));
            Assert.That(loaded.CountIntroSeen, Is.True);
        }

        [Test]
        public void DifficultyLevelClampsOnLoadAndTheBufferRoundTrips()
        {
            var fake = new FakeKeyValueStore();
            var saved = new PlayerProgress { DifficultyLevel = 99, DifficultyBuffer = new System.Collections.Generic.List<bool> { true, false, true } };
            new SaveStore(fake).Save(saved);
            var loaded = new SaveStore(fake).Load();
            Assert.That(loaded.DifficultyLevel, Is.EqualTo(DifficultyLadder.MaxLevel));
            Assert.That(loaded.DifficultyBuffer, Is.EqualTo(new[] { true, false, true }));

            saved.DifficultyLevel = -4;
            new SaveStore(fake).Save(saved);
            Assert.That(new SaveStore(fake).Load().DifficultyLevel, Is.EqualTo(DifficultyLadder.MinLevel));
        }

        [Test]
        public void AnEmptyStoreLoadsDefaults()
        {
            var loaded = new SaveStore(new FakeKeyValueStore()).Load();
            Assert.That(loaded.HasCharacter, Is.False);
            Assert.That(loaded.Tutorial, Is.EqualTo(TutorialStep.CreateCharacter));
            Assert.That(loaded.Coins, Is.EqualTo(0));
        }

        [Test]
        public void UnparsableJsonLoadsDefaultsWithoutThrowing()
        {
            var fake = new FakeKeyValueStore();
            fake.SetString("eva.save.v1", "{not json");
            PlayerProgress loaded = null;
            Assert.DoesNotThrow(() => loaded = new SaveStore(fake).Load());
            Assert.That(loaded.HasCharacter, Is.False);
            Assert.That(loaded.Tutorial, Is.EqualTo(TutorialStep.CreateCharacter));
        }

        [Test]
        public void SaveUsesTheDocumentedKey()
        {
            var fake = new FakeKeyValueStore();
            new SaveStore(fake).Save(new PlayerProgress());
            Assert.That(fake.Values.ContainsKey("eva.save.v1"), Is.True);
        }

        [Test]
        public void OldBedroomPlacementsLoadAsKidsAndUnknownSlotsAreDropped()
        {
            var fake = new FakeKeyValueStore();
            var saved = new PlayerProgress();
            saved.Owned.AddRange(new[] { "bed", "lamp", "rug" });
            saved.House.Placements.Add(new Placement { ItemId = "bed", SlotId = "bedroom_bed" });
            saved.House.Placements.Add(new Placement { ItemId = "lamp", SlotId = "bedroom_corner" });
            saved.House.Placements.Add(new Placement { ItemId = "rug", SlotId = "no_such_slot" });
            new SaveStore(fake).Save(saved);

            var loaded = new SaveStore(fake).Load();

            Assert.That(loaded.House.SlotOf("bed"), Is.EqualTo("kids_bed"));
            Assert.That(loaded.House.SlotOf("chest"), Is.EqualTo("kids_corner"), "the old lamp is now the toy chest");
            Assert.That(loaded.Owned, Does.Contain("chest"));
            Assert.That(loaded.Owned, Does.Not.Contain("lamp"));
            Assert.That(loaded.House.SlotOf("rug"), Is.Null);
            Assert.That(loaded.House.Placements.Count, Is.EqualTo(2));
        }

        [Test]
        public void LastPlaceAndVoiceVolumeSurviveASaveAndLoad()
        {
            var fake = new FakeKeyValueStore();
            new SaveStore(fake).Save(new PlayerProgress { LastPlace = "Store", VoiceVolumeStep = 0 });
            var loaded = new SaveStore(fake).Load();
            Assert.That(loaded.LastPlace, Is.EqualTo("Store"));
            Assert.That(loaded.VoiceVolumeStep, Is.EqualTo(0));
        }

        [Test]
        public void AnUnknownLastPlaceOrAnOutOfRangeVolumeIsNormalisedOnLoad()
        {
            var fake = new FakeKeyValueStore();
            new SaveStore(fake).Save(new PlayerProgress { LastPlace = "Volcano", VoiceVolumeStep = 9 });
            var loaded = new SaveStore(fake).Load();
            Assert.That(loaded.LastPlace, Is.EqualTo("House"));
            Assert.That(loaded.VoiceVolumeStep, Is.EqualTo(VoiceSettings.Steps - 1));
        }

        [Test]
        public void ANewProgressStartsAtTheHouseWithFullVoiceVolume()
        {
            var progress = new PlayerProgress();
            Assert.That(progress.LastPlace, Is.EqualTo("House"));
            Assert.That(progress.VoiceVolumeStep, Is.EqualTo(VoiceSettings.DefaultStep));
        }
    }
}
