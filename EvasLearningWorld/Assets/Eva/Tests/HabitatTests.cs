using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    // Zoo & Farm Habitat as take-the-animal-home: the round builder, the reactions and a smoke test of the screen.
    public class HabitatTests
    {
        [Test]
        public void ARoundHasTheAnimalsOwnHabitatAmongDistinctChoicesAtEveryLevel()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            for (var seed = 0; seed < 200; seed++)
            {
                var round = HabitatRoundBuilder.Create(level, new System.Random(seed), null);
                var animal = ZooFarmAnimals.All.First(a => a.Id == round.AnimalId);
                var label = "level " + level + " seed " + seed;
                Assert.That(round.HabitatIds.Length, Is.EqualTo(level <= 2 ? 3 : 4), label);
                Assert.That(round.HabitatIds.Distinct().Count(), Is.EqualTo(round.HabitatIds.Length), label);
                Assert.That(round.HabitatIds[round.CorrectIndex], Is.EqualTo(animal.Habitat), label);
            }
        }

        [Test]
        public void ARoundNeverRepeatsTheLastAnimalAndLevelsOutsideTheLadderThrow()
        {
            for (var seed = 0; seed < 100; seed++)
                Assert.That(HabitatRoundBuilder.Create(1, new System.Random(seed), "cow").AnimalId, Is.Not.EqualTo("cow"));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => HabitatRoundBuilder.Create(0, new System.Random(1), null));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => HabitatRoundBuilder.Create(7, new System.Random(1), null));
        }

        // A wrong habitat is always really wrong: no other habitat the animal could believably live in is offered.
        [Test]
        public void NoWrongChoiceIsABelievableHomeForTheAnimal()
        {
            var believable = new[] { ("duck", "farm"), ("duck", "ocean"), ("fish", "pond"), ("elephant", "jungle"), ("eagle", "forest"), ("owl", "mountain") };
            for (var seed = 0; seed < 300; seed++)
            {
                var round = HabitatRoundBuilder.Create(5, new System.Random(seed), null);
                foreach (var (animal, habitat) in believable)
                    if (round.AnimalId == animal) Assert.That(round.HabitatIds, Does.Not.Contain(habitat), animal);
            }
        }

        [Test]
        public void EveryHabitatAnAnimalLivesInIsOfferedAsAChoiceSomewhere()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            for (var seed = 0; seed < 300; seed++)
                foreach (var h in HabitatRoundBuilder.Create(6, new System.Random(seed), null).HabitatIds) seen.Add(h);
            CollectionAssert.AreEquivalent(HabitatRoundBuilder.Habitats, seen);
        }

        [Test]
        public void ReactionsFollowWhatTheAnimalNeeds()
        {
            Assert.That(HabitatRoundBuilder.ReactionFor("cow", "farm"), Is.EqualTo(HabitatReaction.Home));
            Assert.That(HabitatRoundBuilder.ReactionFor("cow", "ocean"), Is.EqualTo(HabitatReaction.Sink));
            Assert.That(HabitatRoundBuilder.ReactionFor("cow", "pond"), Is.EqualTo(HabitatReaction.Sink));
            Assert.That(HabitatRoundBuilder.ReactionFor("fish", "savanna"), Is.EqualTo(HabitatReaction.Flop));
            Assert.That(HabitatRoundBuilder.ReactionFor("duck", "mountain"), Is.EqualTo(HabitatReaction.Flop));
            Assert.That(HabitatRoundBuilder.ReactionFor("lion", "forest"), Is.EqualTo(HabitatReaction.Shiver));
            Assert.That(HabitatRoundBuilder.ReactionFor("fish", "ocean"), Is.EqualTo(HabitatReaction.Home));
        }

        [Test]
        public void EveryHabitatAndReactionHasItsVoiceLineAndPicture()
        {
            var lines = VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            foreach (var key in new[] { "zoofarm_prompt_habitat", "habitat_drag_hint", "habitat_drag_demo" })
            {
                Assert.That(lines.ContainsKey(key), Is.True, key);
                Assert.That(Resources.Load<AudioClip>("Voice/en/" + key), Is.Not.Null, "clip " + key);
            }
            foreach (var habitat in HabitatRoundBuilder.Habitats)
            {
                Assert.That(lines.ContainsKey("habitat_home_" + habitat), Is.True, habitat);
                Assert.That(Resources.Load<AudioClip>("Voice/en/habitat_home_" + habitat), Is.Not.Null, "clip " + habitat);
                Assert.That(Resources.Load<Sprite>("Art/zoofarm/habitat_" + habitat), Is.Not.Null, habitat);
            }
            foreach (var animal in ZooFarmAnimals.All)
                Assert.That(Resources.Load<Sprite>("Art/zoofarm/animal_" + animal.Id), Is.Not.Null, animal.Id);
        }

        // Every animal but the fish has its own real recording (made with ElevenLabs, imported by tools/animals/import.js).
        [Test]
        public void EveryAnimalButTheFishHasItsOwnSoundClip()
        {
            foreach (var animal in ZooFarmAnimals.All.Where(a => a.Id != "fish"))
            {
                var clip = Resources.Load<AudioClip>("Animals/" + animal.Id);
                Assert.That(clip, Is.Not.Null, animal.Id);
                Assert.That(clip.length, Is.InRange(0.3f, 3f), animal.Id);
            }
        }

        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
        }

        [TearDown]
        public void TearDown()
        {
            if (_game != null) Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) Object.DestroyImmediate(_canvasObject);
        }

        private HabitatScreen ShowScreen(int level)
        {
            _game.Progress.ZooFarmHabitatLevel = level;
            _game.Navigator.Show(ScreenId.ZooFarmHabitat);
            var screen = _game.Navigator.GetScreen(ScreenId.ZooFarmHabitat) as HabitatScreen;
            Assert.IsNotNull(screen, "Habitat is registered on the take-the-animal-home screen");
            return screen;
        }

        private Transform ScreenRoot => _canvasObject.transform.Find("ScreenRoot/HabitatScreen");

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [Test]
        public void TheScreenShowsAHabitatPerChoiceAndOneDraggableAnimalClearOfTheHomeButton()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            {
                var screen = ShowScreen(level);
                var cards = ScreenRoot.Find("CardField").Cast<Transform>().Where(c => c.gameObject.activeSelf).ToList();
                Assert.That(cards.Count, Is.EqualTo(screen.CurrentRound.HabitatIds.Length), "level " + level);
                Assert.That(ScreenRoot.GetComponentsInChildren<DragItem>(false).Length, Is.EqualTo(1));
                var animal = ScreenRoot.GetComponentInChildren<DragItem>(false);
                animal.Rect.localScale = Vector3.one; // pops in; Edit Mode stops it at its first, small frame
                var animalRect = WorldRect(animal.Rect);
                Assert.That(animalRect.width, Is.GreaterThanOrEqualTo(EvaUi.MinTap));
                foreach (var card in cards)
                {
                    var rect = WorldRect((RectTransform)card);
                    Assert.That(rect.yMax, Is.LessThanOrEqualTo(180f), card.name + " top");
                    Assert.That(rect.yMin, Is.GreaterThan(animalRect.yMax - 20f), card.name + " sits above the animal");
                    Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(-720f), card.name);
                    Assert.That(rect.xMax, Is.LessThanOrEqualTo(720f), card.name);
                }
            }
        }

        [Test]
        public void DroppingOnAnyHabitatTakesTheAnimalAndEmptySpaceDoesNot()
        {
            var screen = ShowScreen(6);
            var animal = ScreenRoot.GetComponentInChildren<DragItem>(false);
            animal.enabled = true;

            animal.Rect.anchoredPosition = new Vector2(-170f, -245f); // home: empty space, not an attempt
            animal.OnEndDrag(new PointerEventData(null));
            Assert.That(animal.enabled, Is.True);

            var card = (RectTransform)ScreenRoot.Find("CardField/Habitat" + screen.CurrentRound.CorrectIndex);
            animal.Rect.anchoredPosition = card.anchoredPosition + new Vector2(20f, -15f);
            animal.OnEndDrag(new PointerEventData(null));
            Assert.That(animal.enabled, Is.False, "a habitat takes the animal (the reaction then plays)");
        }
    }
}
