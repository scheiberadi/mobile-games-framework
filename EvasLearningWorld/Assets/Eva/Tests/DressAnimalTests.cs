using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    // Zoo and Farm Covering as "dress the animal": the animals with a trunk shadow, the rounds and the screen.
    public class DressAnimalTests
    {
        [Test]
        public void EveryShadowedAnimalHasItsPicturesAndTheShadowStaysOnTheAnimal()
        {
            Assert.That(TrunkShadows.Centres.Count, Is.GreaterThanOrEqualTo(20));
            foreach (var pair in TrunkShadows.Centres)
            {
                var animal = ZooFarmAnimals.All.FirstOrDefault(a => a.Id == pair.Key);
                Assert.That(animal, Is.Not.Null, pair.Key + " is in the roster");
                Assert.That(Resources.Load<Sprite>("Art/zoofarm/animal_" + pair.Key), Is.Not.Null, "animal " + pair.Key);
                var shadow = Resources.Load<Sprite>("Art/zoofarm/shadow_" + pair.Key);
                Assert.That(shadow, Is.Not.Null, "shadow " + pair.Key);
                Assert.That(shadow.rect.size, Is.EqualTo(Resources.Load<Sprite>("Art/zoofarm/animal_" + pair.Key).rect.size), "the shadow lies exactly over the animal " + pair.Key);
                Assert.That(Resources.Load<Sprite>("Art/zoofarm/covering_" + animal.Covering), Is.Not.Null, "covering of " + pair.Key);
                Assert.That(pair.Value.X, Is.InRange(0.2f, 0.8f), pair.Key);
                Assert.That(pair.Value.Y, Is.InRange(0.3f, 0.9f), pair.Key);
            }
        }

        [Test]
        public void EveryRoundHasTheAnimalsOwnCoveringAmongDistinctChoices()
        {
            for (var level = 1; level <= 6; level++)
            for (var seed = 0; seed < 100; seed++)
            {
                var label = "level " + level + " seed " + seed;
                var round = CoveringRules.Create(level, new System.Random(seed), null);
                Assert.That(TrunkShadows.Centres.ContainsKey(round.TargetId), Is.True, label + " " + round.TargetId + " has no shadow");
                var animal = ZooFarmAnimals.All.First(a => a.Id == round.TargetId);
                Assert.That(round.ChoiceSprites[round.CorrectIndex], Is.EqualTo("zoofarm/covering_" + animal.Covering), label);
                Assert.That(round.ChoiceSprites.Distinct().Count(), Is.EqualTo(round.ChoiceSprites.Length), label + " a covering twice");
                Assert.That(round.ChoiceSprites.Length, Is.LessThanOrEqualTo(CoveringRules.ChoiceCountByLevel[level - 1]), label);
            }
        }

        [Test]
        public void HigherLevelsOfferMoreCoveringsOnceTheirAnimalsAreIn()
        {
            var most = 0;
            for (var seed = 0; seed < 200; seed++) most = System.Math.Max(most, CoveringRules.Create(6, new System.Random(seed), null).ChoiceSprites.Length);
            Assert.That(most, Is.EqualTo(5));
            for (var seed = 0; seed < 100; seed++) Assert.That(CoveringRules.Create(1, new System.Random(seed), null).ChoiceSprites.Length, Is.EqualTo(3));
        }

        [Test]
        public void TheDragLinesExist()
        {
            var lines = VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            foreach (var key in new[] { "covering_drag_hint", "covering_drag_demo", "zoofarm_prompt_covering" })
            {
                Assert.That(lines.ContainsKey(key), Is.True, key);
                Assert.That(Resources.Load<AudioClip>("Voice/en/" + key), Is.Not.Null, "clip " + key);
            }
        }

        // ---- The screen ------------------------------------------------------------------------------------

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

        private DressAnimalScreen ShowScreen()
        {
            _game.Navigator.Show(ScreenId.ZooFarmCovering);
            var screen = _game.Navigator.GetScreen(ScreenId.ZooFarmCovering) as DressAnimalScreen;
            Assert.IsNotNull(screen, "Covering is registered on the dress-the-animal screen");
            return screen;
        }

        private Transform ScreenRoot => _canvasObject.transform.Find("ScreenRoot").Cast<Transform>().First(t => t.name == "DressAnimalScreen" && t.gameObject.activeSelf);

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [Test]
        public void TheScreenShowsTheShadowedAnimalAndTheCoveringsInsideTheCanvas()
        {
            var screen = ShowScreen();
            Assert.That(screen.CurrentRound, Is.Not.Null);
            Assert.That(screen.ShadowShowing, Is.True);
            var animal = WorldRect((RectTransform)ScreenRoot.Find("AnimalField/Animal"));
            Assert.That(animal.xMin, Is.GreaterThanOrEqualTo(-720f));
            Assert.That(animal.xMax, Is.LessThanOrEqualTo(720f));
            var items = ScreenRoot.GetComponentsInChildren<DragItem>(false);
            Assert.That(items.Length, Is.EqualTo(screen.CurrentRound.ChoiceSprites.Length));
            foreach (var item in items)
            {
                item.Rect.localScale = Vector3.one; // pops in; Edit Mode stops it at its first, small frame
                var rect = WorldRect(item.Rect);
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(-720f));
                Assert.That(rect.xMax, Is.LessThanOrEqualTo(720f));
                Assert.That(rect.yMax, Is.LessThan(animal.yMin + 30f), "the coverings sit below the animal");
            }
        }

        [Test]
        public void AWrongCoveringDroppedOnTheAnimalDoesNotLiftTheShadowAndEmptySpaceIsNoAttempt()
        {
            var screen = ShowScreen();
            var items = ScreenRoot.GetComponentsInChildren<DragItem>(false).ToList();
            var wrong = items.First(d => d.name != "Drag_covering" + screen.CurrentRound.CorrectIndex);
            wrong.enabled = true;
            wrong.Rect.anchoredPosition = new Vector2(1000f, -300f);
            wrong.OnEndDrag(new PointerEventData(null));
            Assert.That(wrong.enabled, Is.True, "empty space is not an attempt");
            wrong.Rect.anchoredPosition = new Vector2(-140f, 100f);
            wrong.OnEndDrag(new PointerEventData(null));
            Assert.That(screen.ShadowShowing, Is.True, "a wrong covering leaves the shadow");
        }

        [Test]
        public void TheRightCoveringDroppedOnTheAnimalIsTaken()
        {
            var screen = ShowScreen();
            var right = ScreenRoot.GetComponentsInChildren<DragItem>(false).First(d => d.name == "Drag_covering" + screen.CurrentRound.CorrectIndex);
            right.enabled = true;
            right.Rect.anchoredPosition = new Vector2(-120f, 120f);
            right.OnEndDrag(new PointerEventData(null));
            Assert.That(right.enabled, Is.False, "the animal takes the covering (the reveal then plays out)");
        }
    }
}
