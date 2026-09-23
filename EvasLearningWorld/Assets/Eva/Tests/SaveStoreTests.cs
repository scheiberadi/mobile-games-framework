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
    }
}
