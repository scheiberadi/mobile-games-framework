using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    // Zoo and Farm Mother and Habitat as pairing games that run until every pair is made: the game state and the screen.
    public class PairingTests
    {
        private static IEnumerable<(string Name, IReadOnlyList<(string Id, string Key)> Pairs)> Games()
        {
            yield return ("mother", ZooPairs.MotherAndBaby());
            yield return ("habitat", ZooPairs.AnimalAndHabitat());
            yield return ("footprint", ZooPairs.FootprintAndAnimal());
        }

        // Plays a whole game by always making a matching pair, checking the guarantees after every step.
        [Test]
        public void AWholeGameUsesEveryPairOnceAndNeverLeavesTheChildStuck()
        {
            foreach (var game in Games())
            for (var seed = 0; seed < 200; seed++)
            {
                var pairing = new PairingGame(game.Pairs, new System.Random(seed), game.Name == "habitat" ? HabitatRules.IsBelievableButWrong : (System.Func<string, string, bool>)null);
                var used = new List<string>();
                var label = game.Name + " seed " + seed;
                var guard = 0;
                while (!pairing.Finished)
                {
                    Assert.That(guard++, Is.LessThan(game.Pairs.Count + 1), label + " does not end");
                    Assert.That(pairing.TryFindMatch(out var item, out var target), Is.True, label + " is stuck");
                    CheckScreenRules(pairing, label);
                    used.Add(pairing.Items[item].Id);
                    pairing.Resolve(item);
                }
                CollectionAssert.AreEquivalent(game.Pairs.Select(p => p.Id), used, label);
            }
        }

        private static void CheckScreenRules(PairingGame pairing, string label)
        {
            var items = pairing.Items.Where(i => i != null).Select(i => i.Id).ToList();
            var targets = pairing.Targets.Where(t => t != null).ToList();
            Assert.That(items.Distinct().Count(), Is.EqualTo(items.Count), label + " item shown twice");
            Assert.That(targets.Distinct().Count(), Is.EqualTo(targets.Count), label + " target shown twice");
            Assert.That(items.Count, Is.EqualTo(System.Math.Min(PairingGame.Slots, pairing.Remaining)), label + " items on screen");
            if (pairing.Remaining <= PairingGame.Slots)
                foreach (var item in pairing.Items.Where(i => i != null))
                    Assert.That(targets, Does.Contain(item.Key), label + " the end of the game shows every partner");
        }

        [Test]
        public void ATargetStaysUntilNothingBelongsToItAnymore()
        {
            var pairing = new PairingGame(ZooPairs.AnimalAndHabitat(), new System.Random(3));
            for (var step = 0; step < 50 && !pairing.Finished; step++)
            {
                pairing.TryFindMatch(out var item, out var target);
                var key = pairing.Targets[target];
                var othersLeft = pairing.Items.Where((i, s) => i != null && s != item && i.Key == key).Any();
                pairing.Resolve(item);
                if (othersLeft) Assert.That(pairing.Targets, Does.Contain(key), "habitat " + key + " still has animals to take");
            }
        }

        [Test]
        public void HabitatKeepsBelievableButWrongPairsApartWhenItCan()
        {
            var apart = 0;
            var together = 0;
            for (var seed = 0; seed < 200; seed++)
            {
                var pairing = new PairingGame(ZooPairs.AnimalAndHabitat(), new System.Random(seed), HabitatRules.IsBelievableButWrong);
                if (pairing.Items.Where(i => i != null).Any(i => pairing.Targets.Any(t => t != null && t != i.Key && HabitatRules.IsBelievableButWrong(i.Id, t)))) together++;
                else apart++;
            }
            Assert.That(together, Is.LessThan(apart / 10 + 1), "a believable wrong home sits next to an animal in the opening screen");
        }

        [Test]
        public void ReactionsFollowWhatTheAnimalNeeds()
        {
            Assert.That(HabitatRules.ReactionFor("cow", "ocean"), Is.EqualTo(PairReaction.Sink));
            Assert.That(HabitatRules.ReactionFor("cow", "pond"), Is.EqualTo(PairReaction.Sink));
            Assert.That(HabitatRules.ReactionFor("fish", "savanna"), Is.EqualTo(PairReaction.Flop));
            Assert.That(HabitatRules.ReactionFor("duck", "mountain"), Is.EqualTo(PairReaction.Flop));
            Assert.That(HabitatRules.ReactionFor("lion", "forest"), Is.EqualTo(PairReaction.Shiver));
        }

        // Animals with no recorded call of their own (the swimmers play a splash, the others stay quiet).
        private static readonly string[] NoCall = { "fish", "giraffe", "turtle", "shark", "octopus" };

        [Test]
        public void EveryPairHasItsPicturesAndEveryAnimalButTheFishItsSound()
        {
            var lines = VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            foreach (var key in new[] { "pairing_prompt_habitat", "pairing_prompt_mother", "pairing_mother_hint", "pairing_mother_demo", "habitat_drag_hint", "habitat_drag_demo" })
            {
                Assert.That(lines.ContainsKey(key), Is.True, key);
                Assert.That(Resources.Load<AudioClip>("Voice/en/" + key), Is.Not.Null, "clip " + key);
            }
            foreach (var (id, key) in ZooPairs.AnimalAndHabitat())
            {
                Assert.That(Resources.Load<Sprite>("Art/zoofarm/animal_" + id), Is.Not.Null, id);
                Assert.That(Resources.Load<Sprite>("Art/zoofarm/habitat_" + key), Is.Not.Null, key);
                Assert.That(Resources.Load<Sprite>("Art/zoofarm/baby_" + id), Is.Not.Null, "baby " + id);
                Assert.That(Resources.Load<Sprite>("Art/" + ZooFarmAnimals.MotherSprite(id)), Is.Not.Null, "mother " + id);
                if (NoCall.Contains(id)) continue;
                var clip = Resources.Load<AudioClip>("Animals/" + id);
                Assert.That(clip, Is.Not.Null, "sound " + id);
                Assert.That(clip.length, Is.InRange(0.3f, 3f), id);
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

        private PairingScreen ShowScreen(ScreenId id)
        {
            _game.Navigator.Show(id);
            var screen = _game.Navigator.GetScreen(id) as PairingScreen;
            Assert.IsNotNull(screen, id + " is registered on the pairing screen");
            return screen;
        }

        private Transform ScreenRoot => _canvasObject.transform.Find("ScreenRoot/PairingScreen");

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [Test]
        public void TheScreenShowsTargetsAndFourDraggableItemsClearOfTheHomeButtonAndEachOther()
        {
            foreach (var id in new[] { ScreenId.ZooFarmMother, ScreenId.ZooFarmHabitat, ScreenId.ZooFarmFootprint })
            {
                var screen = ShowScreen(id);
                var items = ScreenRoot.GetComponentsInChildren<DragItem>(false);
                Assert.That(items.Length, Is.EqualTo(PairingGame.Slots), id.ToString());
                var targets = ScreenRoot.Find("TargetField").Cast<Transform>().Where(t => t.gameObject.activeSelf).ToList();
                Assert.That(targets.Count, Is.InRange(1, PairingGame.Slots), id.ToString());
                foreach (var item in items)
                {
                    item.Rect.localScale = Vector3.one; // pops in; Edit Mode stops it at its first, small frame
                    var rect = WorldRect(item.Rect);
                    Assert.That(rect.width, Is.GreaterThanOrEqualTo(EvaUi.MinTap));
                    Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(-720f));
                    Assert.That(rect.xMax, Is.LessThanOrEqualTo(720f));
                    Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(-450f));
                }
                for (var i = 0; i < items.Length; i++)
                    for (var j = i + 1; j < items.Length; j++)
                    {
                        var a = WorldRect(items[i].Rect);
                        var b = WorldRect(items[j].Rect);
                        Assert.That(Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin), Is.LessThanOrEqualTo(20f), "items overlap");
                    }
                foreach (var target in targets)
                {
                    var rect = WorldRect((RectTransform)target);
                    Assert.That(rect.yMax, Is.LessThanOrEqualTo(180f), target.name + " top");
                    Assert.That(rect.yMin, Is.GreaterThan(WorldRect(items[0].Rect).yMax - 20f), target.name + " sits above the items");
                    Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(-720f));
                    Assert.That(rect.xMax, Is.LessThanOrEqualTo(720f));
                }
                Assert.That(screen.Game.Remaining, Is.EqualTo(id == ScreenId.ZooFarmFootprint ? ZooPairs.FootprintAndAnimal().Count : ZooPairs.PairsPerGame));
            }
        }

        [Test]
        public void ADropOnAMatchingTargetTakesTheItemAndEmptySpaceDoesNot()
        {
            var screen = ShowScreen(ScreenId.ZooFarmMother);
            Assert.That(screen.Game.TryFindMatch(out var slot, out var target), Is.True);
            var item = ScreenRoot.GetComponentsInChildren<DragItem>(false).First(d => d.name == "Drag_pairitem" + slot);
            item.enabled = true; // the intro line normally enables it

            item.Rect.anchoredPosition = new Vector2(1000f, -300f); // far from every target
            item.OnEndDrag(new PointerEventData(null));
            Assert.That(item.enabled, Is.True, "empty space is not an attempt");

            var targetRect = (RectTransform)ScreenRoot.Find("TargetField/Target" + target);
            item.Rect.anchoredPosition = targetRect.anchoredPosition + new Vector2(20f, -15f);
            item.OnEndDrag(new PointerEventData(null));
            Assert.That(item.enabled, Is.False, "the target takes the item (the match then plays out)");
        }
    }
}
