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
            var look = new CharacterLook { Gender = Gender.Girl, Face = 6, Skin = 4, HairStyle = 2, HairColor = 3, EyeColor = 1 };
            look.SetDress("dress_a");
            look.Shoes = "shoes_a";
            look.Glasses = "glasses_a";
            var saved = new PlayerProgress
            {
                HasCharacter = true,
                Look = look,
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
            Assert.That(loaded.Look.Gender, Is.EqualTo(Gender.Girl));
            Assert.That(loaded.Look.Face, Is.EqualTo(6));
            Assert.That(loaded.Look.Skin, Is.EqualTo(4));
            Assert.That(loaded.Look.HairStyle, Is.EqualTo(2));
            Assert.That(loaded.Look.HairColor, Is.EqualTo(3));
            Assert.That(loaded.Look.EyeColor, Is.EqualTo(1));
            Assert.That(loaded.Look.Dress, Is.EqualTo("dress_a"));
            Assert.That(loaded.Look.Top, Is.Null, "Dress must still exclude Top after a save/load round trip");
            Assert.That(loaded.Look.Bottom, Is.Null, "Dress must still exclude Bottom after a save/load round trip");
            Assert.That(loaded.Look.Shoes, Is.EqualTo("shoes_a"));
            Assert.That(loaded.Look.Glasses, Is.EqualTo("glasses_a"));
            Assert.That(loaded.Coins, Is.EqualTo(9));
            Assert.That(loaded.Owned, Is.EqualTo(new[] { "sofa", "rug" }));
            Assert.That(loaded.House.Placements.Count, Is.EqualTo(2));
            Assert.That(loaded.House.SlotOf("sofa"), Is.EqualTo("living_seat"));
            Assert.That(loaded.House.ItemIn("living_floor"), Is.EqualTo("rug"));
            Assert.That(loaded.Tutorial, Is.EqualTo(TutorialStep.GoToStore));
            Assert.That(loaded.CountIntroSeen, Is.True);
        }

        // The required fixture test for M5's save-breaking change (docs/superpowers/specs/2026-09-27-
        // character-system-design.md's "Save compatibility", Task 1's "invalidate" decision): a real M1-era
        // save - Version 1, the old 3-field Look, no Gender/Face/wardrobe fields at all - must load with Look
        // deliberately reset and HasCharacter forced back to false, never with the old Head/Skin/Shirt values
        // silently zero-defaulted into the new fields as if that were a real character.
        [Test]
        public void AnOldHeadSkinShirtSaveIsInvalidatedNotSilentlyMigrated()
        {
            var fake = new FakeKeyValueStore();
            fake.SetString("eva.save.v1",
                "{\"Version\":1,\"HasCharacter\":true,\"Look\":{\"Head\":2,\"Skin\":3,\"Shirt\":1},\"Coins\":12}");

            var loaded = new SaveStore(fake).Load();

            Assert.That(loaded.Version, Is.EqualTo(2), "the save must be bumped onto the current version on load");
            Assert.That(loaded.HasCharacter, Is.False, "an invalidated Look must send the child back through Creator");
            Assert.That(loaded.Look, Is.Not.Null);
            Assert.That(loaded.Look.Gender, Is.EqualTo(Gender.Boy), "a fresh CharacterLook's default, not a guess at the old character");
            Assert.That(loaded.Look.Face, Is.EqualTo(0));
            Assert.That(loaded.Look.Skin, Is.EqualTo(0), "the old Skin=3 is deliberately NOT carried across - see the class comment in SaveStore.Load()");
            Assert.That(loaded.Look.Top, Is.Null);
            Assert.That(loaded.Look.Dress, Is.Null);
            Assert.That(loaded.Coins, Is.EqualTo(12), "only Look/HasCharacter are touched by the migration - the rest of the save is untouched");
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
        public void NumberHuntLevelClampsOnLoadAndTheBufferRoundTrips()
        {
            var fake = new FakeKeyValueStore();
            var saved = new PlayerProgress { NumberHuntLevel = 99, NumberHuntBuffer = new System.Collections.Generic.List<bool> { false, true, true } };
            new SaveStore(fake).Save(saved);
            var loaded = new SaveStore(fake).Load();
            Assert.That(loaded.NumberHuntLevel, Is.EqualTo(DifficultyLadder.MaxLevel));
            Assert.That(loaded.NumberHuntBuffer, Is.EqualTo(new[] { false, true, true }));

            saved.NumberHuntLevel = -4;
            new SaveStore(fake).Save(saved);
            Assert.That(new SaveStore(fake).Load().NumberHuntLevel, Is.EqualTo(DifficultyLadder.MinLevel));
        }

        [Test]
        public void ASaveFromBeforeNumberHuntExistedLoadsWithADefaultLevelAndAnEmptyBuffer()
        {
            var fake = new FakeKeyValueStore();
            fake.SetString("eva.save.v1", "{\"Version\":1,\"Coins\":5,\"DifficultyLevel\":3}");
            var loaded = new SaveStore(fake).Load();
            Assert.That(loaded.Coins, Is.EqualTo(5));
            Assert.That(loaded.DifficultyLevel, Is.EqualTo(3));
            Assert.That(loaded.NumberHuntLevel, Is.EqualTo(DifficultyLadder.MinLevel));
            Assert.That(loaded.NumberHuntBuffer, Is.Not.Null);
            Assert.That(loaded.NumberHuntBuffer, Is.Empty);
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
        public void LastPlaceAndTheAudioSwitchesSurviveASaveAndLoad()
        {
            var fake = new FakeKeyValueStore();
            new SaveStore(fake).Save(new PlayerProgress { LastPlace = "Store", MusicEnabled = false, SfxEnabled = false, VoiceEnabled = false });
            var loaded = new SaveStore(fake).Load();
            Assert.That(loaded.LastPlace, Is.EqualTo("Store"));
            Assert.That(loaded.MusicEnabled, Is.False);
            Assert.That(loaded.SfxEnabled, Is.False);
            Assert.That(loaded.VoiceEnabled, Is.False);
        }

        [Test]
        public void AnUnknownLastPlaceIsNormalisedOnLoad()
        {
            var fake = new FakeKeyValueStore();
            new SaveStore(fake).Save(new PlayerProgress { LastPlace = "Volcano" });
            Assert.That(new SaveStore(fake).Load().LastPlace, Is.EqualTo("House"));
        }

        [Test]
        public void ANewProgressStartsAtTheHouseWithEveryAudioSwitchOn()
        {
            var progress = new PlayerProgress();
            Assert.That(progress.LastPlace, Is.EqualTo("House"));
            Assert.That(progress.MusicEnabled && progress.SfxEnabled && progress.VoiceEnabled, Is.True);
        }

        [Test]
        public void ASaveFromBeforeTheSwitchesExistedLoadsWithEverythingOn()
        {
            var fake = new FakeKeyValueStore();
            fake.SetString("eva.save.v1", "{\"Version\":1,\"Coins\":5,\"VoiceVolumeStep\":0}");
            var loaded = new SaveStore(fake).Load();
            Assert.That(loaded.Coins, Is.EqualTo(5));
            Assert.That(loaded.MusicEnabled && loaded.SfxEnabled && loaded.VoiceEnabled, Is.True);
        }
    }
}
