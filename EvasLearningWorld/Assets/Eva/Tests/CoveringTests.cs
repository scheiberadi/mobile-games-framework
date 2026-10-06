using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.Tests
{
    // Zoo and Farm Covering as "which animals have feathers?": the rounds, the guess and the screen.
    public class CoveringTests
    {
        private static Animal AnimalOf(string id) => ZooFarmAnimals.All.First(a => a.Id == id);

        [Test]
        public void EveryRoundHasExactlyTheWantedAnimalsWithTheTextureAndNoWoolAnimals()
        {
            for (var level = 1; level <= 6; level++)
            for (var seed = 0; seed < 150; seed++)
            {
                var label = "level " + level + " seed " + seed;
                var round = CoveringRules.Create(level, new System.Random(seed), null);
                Assert.That(round.AnimalIds.Length, Is.EqualTo(CoveringRules.GridSizeByLevel[level - 1]), label);
                Assert.That(round.AnimalIds.Length, Is.LessThanOrEqualTo(CoveringRules.MaxGrid), label);
                Assert.That(round.AnimalIds.Distinct().Count(), Is.EqualTo(round.AnimalIds.Length), label + " an animal twice");
                Assert.That(round.IsCorrect.Count(c => c), Is.EqualTo(round.Wanted), label);
                Assert.That(round.Wanted, Is.InRange(2, CoveringRules.WantedByLevel[level - 1]), label);
                for (var i = 0; i < round.AnimalIds.Length; i++)
                {
                    var animal = AnimalOf(round.AnimalIds[i]);
                    Assert.That(CoveringRules.InGame(animal), Is.True, label + " " + animal.Id + " has wool");
                    Assert.That(animal.Covering == round.Texture, Is.EqualTo(round.IsCorrect[i]), label + " " + animal.Id + " for " + round.Texture);
                }
                Assert.That(CoveringRules.Textures.Take(CoveringRules.TextureCountByLevel[level - 1]), Does.Contain(round.Texture), label);
            }
        }

        [Test]
        public void ScalesAreOnlyAskedForAsManyAsThereAreAndTheTextureChangesFromRoundToRound()
        {
            var seenScales = false;
            for (var seed = 0; seed < 300; seed++)
            {
                var round = CoveringRules.Create(6, new System.Random(seed), "fur");
                Assert.That(round.Texture, Is.Not.EqualTo("fur"), "the previous texture is not asked twice in a row");
                if (round.Texture == "scales") { seenScales = true; Assert.That(round.Wanted, Is.InRange(2, 3)); }
            }
            Assert.That(seenScales, Is.True);
            for (var seed = 0; seed < 100; seed++)
                Assert.That(new[] { "fur", "feathers" }, Does.Contain(CoveringRules.Create(1, new System.Random(seed), null).Texture), "levels 1-2 ask about fur and feathers");
        }

        [Test]
        public void ThePicksAreCheckedWhenWantedAnimalsArePickedAndTakenBackByTappingAgain()
        {
            var round = CoveringRules.Create(3, new System.Random(1), null);
            var guess = new CoveringGuess(round, new System.Random(1));
            var right = Enumerable.Range(0, round.AnimalIds.Length).Where(i => round.IsCorrect[i]).ToList();
            var wrong = Enumerable.Range(0, round.AnimalIds.Length).Where(i => !round.IsCorrect[i]).ToList();
            Assert.That(right.Count, Is.EqualTo(2));

            Assert.That(guess.Toggle(right[0]), Is.EqualTo(Verdict.Pending));
            Assert.That(guess.Toggle(right[0]), Is.EqualTo(Verdict.Pending), "tapping again takes it back");
            Assert.That(guess.SelectedCount, Is.EqualTo(0));
            guess.Toggle(right[0]);
            Assert.That(guess.Toggle(wrong[0]), Is.EqualTo(Verdict.Wrong));
            Assert.That(guess.Toggle(wrong[0]), Is.EqualTo(Verdict.Pending));
            Assert.That(guess.Toggle(right[1]), Is.EqualTo(Verdict.Right));
            Assert.That(guess.IsSolved, Is.True);
        }

        [Test]
        public void TheGameStepsInMoreEachTimeAndThirdTimePicksTheRightAnimals()
        {
            var round = CoveringRules.Create(4, new System.Random(5), null);
            var guess = new CoveringGuess(round, new System.Random(5));
            var wrong = Enumerable.Range(0, round.AnimalIds.Length).First(i => !round.IsCorrect[i]);
            guess.Toggle(wrong);

            Assert.That(guess.Escalate().Level, Is.EqualTo(1));
            var second = guess.Escalate();
            Assert.That(second.Level, Is.EqualTo(2));
            Assert.That(second.WrongIndex, Is.EqualTo(wrong), "it says what is wrong");
            Assert.That(guess.Selected[wrong], Is.False, "and takes it back");
            var third = guess.Escalate();
            Assert.That(third.Level, Is.EqualTo(3));
            Assert.That(guess.IsSolved, Is.True);
            Assert.That(guess.SolvedByGame, Is.True);
            Assert.That(guess.Notifications, Is.EqualTo(3));
        }

        [Test]
        public void WithNothingWrongTheGamePointsAtAnAnimalStillToFindSecondTime()
        {
            var round = CoveringRules.Create(2, new System.Random(2), null);
            var guess = new CoveringGuess(round, new System.Random(2));
            guess.Escalate();
            var second = guess.Escalate();
            Assert.That(second.WrongIndex, Is.EqualTo(-1));
            Assert.That(second.HintIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(round.IsCorrect[second.HintIndex], Is.True);
            Assert.That(guess.Selected[second.HintIndex], Is.False);
        }

        [Test]
        public void EveryQuestionAndNudgeIsSpokenAndHasItsPicture()
        {
            var lines = VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            var keys = new System.Collections.Generic.List<string> { "covering_check", "covering_help", "zoofarm_food_hint" };
            foreach (var texture in CoveringRules.Textures)
            {
                keys.Add(CoveringRules.NotKey(texture));
                Assert.That(Resources.Load<Sprite>("Art/zoofarm/covering_" + texture), Is.Not.Null, texture);
            }
            for (var level = 1; level <= 6; level++)
            for (var seed = 0; seed < 100; seed++) keys.Add(CoveringRules.QuestionKey(CoveringRules.Create(level, new System.Random(seed), null)));
            foreach (var key in keys.Distinct())
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

        private GuessCoveringScreen ShowScreen()
        {
            _game.Navigator.Show(ScreenId.ZooFarmCovering);
            var screen = _game.Navigator.GetScreen(ScreenId.ZooFarmCovering) as GuessCoveringScreen;
            Assert.IsNotNull(screen, "Covering is registered on the guess-the-covering screen");
            return screen;
        }

        private Transform ScreenRoot => _canvasObject.transform.Find("ScreenRoot").Cast<Transform>().First(t => t.name == "GuessCoveringScreen" && t.gameObject.activeSelf);

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [Test]
        public void TheGridSitsClearOfTheButtonsAndTheBadgeAndEveryAnimalIsABigTapTile()
        {
            var screen = ShowScreen();
            var cells = ScreenRoot.Find("CellField").Cast<Transform>().Where(t => t.name.StartsWith("Animal") && t.gameObject.activeSelf).Select(t => (RectTransform)t).ToList();
            Assert.That(cells.Count, Is.EqualTo(screen.Round.AnimalIds.Length));
            foreach (var cell in cells)
            {
                cell.localScale = Vector3.one; // pops in; Edit Mode stops it at its first, small frame
                var rect = WorldRect(cell);
                Assert.That(rect.width, Is.GreaterThanOrEqualTo(EvaUi.MinTap), cell.name);
                Assert.That(rect.height, Is.GreaterThanOrEqualTo(EvaUi.MinTap), cell.name);
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(-720f), cell.name);
                Assert.That(rect.yMax, Is.LessThanOrEqualTo(175f), cell.name + " reaches the Home and Back buttons");
            }
            var badge = WorldRect((RectTransform)ScreenRoot.Find("BadgeField/BadgePlate"));
            foreach (var cell in cells) Assert.That(WorldRect(cell).xMax, Is.LessThanOrEqualTo(badge.xMin + 1f), cell.name + " is under the texture");
            Assert.That(badge.xMax, Is.LessThanOrEqualTo(720f));
        }

        [Test]
        public void TappingAnAnimalPicksItAndTheRightPicksFinishTheRound()
        {
            var screen = ShowScreen();
            var cells = ScreenRoot.Find("CellField").Cast<Transform>().Where(t => t.name.StartsWith("Animal")).ToList();
            var cell = cells[0].GetComponent<Button>();
            cell.interactable = true;
            cell.onClick.Invoke();
            Assert.That(screen.Guess.Selected[0], Is.True, "the tapped animal is picked");
            cell.onClick.Invoke();
            Assert.That(screen.Guess.Selected[0], Is.False, "tapping again takes it back");
        }
    }
}
