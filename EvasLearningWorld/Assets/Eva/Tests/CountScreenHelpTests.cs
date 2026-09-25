using System.Collections;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static EvasLearningWorld.Tests.CountScreenTestSupport;

namespace EvasLearningWorld.Tests
{
    // Eva's help ladder on the Count screen (Task 8): Retry (Task 7, unchanged) still climbs to Hint on the
    // 2nd mistake and Demonstrate on the 3rd. These are UnityTests (not plain [Test]s) because the ladder's
    // coroutines use real WaitForSeconds and real voice-clip lengths, which only advance across real editor
    // frames - see CountScreenTestSupport.WaitUntil.
    public class CountScreenHelpTests
    {
        private const float Timeout = 20f;

        private GameObject _canvasObject;
        private EvaGame _game;
        private Transform _canvas;

        [SetUp]
        public void SetUp()
        {
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            var gameObject = new GameObject("TestEvaGame");
            _game = gameObject.AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
            // Most of these tests are about the mistake ladder, not the once-ever intro; skip it so a round's
            // question phase starts immediately. FirstSessionRunsIntroBeforeTheQuestion below leaves it unset.
            _game.Progress.CountIntroSeen = true;
            _canvas = _canvasObject.transform;
        }

        [TearDown]
        public void TearDown()
        {
            if (_game != null) Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) Object.DestroyImmediate(_canvasObject);
        }

        // Levels 5-6 mix distractor objects into the field. Tapping any of them is never a mistake (no tile is
        // disabled or dimmed), and the correct tile, computed from the asked objects only, settles the round.
        [UnityTest]
        public IEnumerator AtLevelFiveDistractorTapsAreNeverMistakesAndTheCorrectTileSettlesTheRound()
        {
            _game.Progress.DifficultyLevel = 5;
            _game.Navigator.Show(ScreenId.Count);
            yield return WaitUntil(() => AnyTileInteractable(_canvas), Timeout, "question phase to start");
            Assert.That(TileCount(_canvas), Is.EqualTo(6));

            var correct = CorrectTileIndex(_canvas); // taps every slot, distractors included
            yield return Tick();
            for (var i = 0; i < TileCount(_canvas); i++)
            {
                Assert.IsTrue(TileInteractable(_canvas, i), "tile " + i + " stays tappable after object taps");
                Assert.That(TileColor(_canvas, i), Is.EqualTo(Color.white), "tile " + i + " is not dimmed");
            }

            var coinsBefore = _game.Progress.Coins;
            TapTile(_canvas, correct);
            yield return WaitUntil(() => _game.Progress.Coins > coinsBefore, Timeout, "the correct answer to pay out");
        }

        // The load-bearing behaviour the controller specifically ruled on: with only 3 tiles and 2 wrong ones,
        // the 3rd mistake (Demonstrate) is structurally unreachable unless the Hint step brings a previously
        // tried, still-dimmed wrong tile back into play.
        [UnityTest]
        public IEnumerator HintReEnablesAPreviouslyTriedWrongTileSoDemonstrateIsReachable()
        {
            _game.Navigator.Show(ScreenId.Count);
            yield return WaitUntil(() => AnyTileInteractable(_canvas), Timeout, "question phase to start");

            var wrong = WrongTileIndices(_canvas);
            Assert.That(wrong.Length, Is.EqualTo(2));
            var original = TileColor(_canvas, wrong[0]);

            TapTile(_canvas, wrong[0]); // 1st mistake: Retry
            yield return Tick();
            Assert.IsFalse(TileInteractable(_canvas, wrong[0]), "the tried tile is disabled immediately");

            TapTile(_canvas, wrong[1]); // 2nd mistake: Hint begins
            yield return Tick();
            Assert.IsFalse(TileInteractable(_canvas, 0), "every tile is disabled while the hand counts");
            Assert.IsFalse(TileInteractable(_canvas, 1), "every tile is disabled while the hand counts");
            Assert.IsFalse(TileInteractable(_canvas, 2), "every tile is disabled while the hand counts");

            yield return WaitUntil(() => TileInteractable(_canvas, wrong[0]), Timeout, "the tried tile to be re-enabled after the hint");

            // The core assertion: a previously wrong, tried tile is tappable again...
            Assert.IsTrue(TileInteractable(_canvas, wrong[0]));
            Assert.IsTrue(TileInteractable(_canvas, wrong[1]));
            // ...but the hint never marks the right one, and tried tiles stay visually dimmed.
            Assert.IsTrue(TileInteractable(_canvas, CorrectTileIndex(_canvas)));
            Assert.That(TileColor(_canvas, wrong[0]), Is.Not.EqualTo(original), "the tried tile stays dimmed");
        }

        // Tapping the re-enabled wrong tile a second time is the 3rd mistake and must reach Demonstrate: the
        // child then finishes counting a fresh tally and taps the highlighted tile, paying the Demonstrated tier.
        [UnityTest]
        public IEnumerator ThreeMistakesReachDemonstrateAndPayTheDemonstratedTierOnCompletion()
        {
            _game.Navigator.Show(ScreenId.Count);
            yield return WaitUntil(() => AnyTileInteractable(_canvas), Timeout, "question phase to start");

            var quantity = ObjectCount(_canvas);
            var wrong = WrongTileIndices(_canvas);
            var coinsBefore = _game.Progress.Coins;

            TapTile(_canvas, wrong[0]); // 1st mistake: Retry
            yield return Tick();
            TapTile(_canvas, wrong[1]); // 2nd mistake: Hint
            yield return Tick();
            yield return WaitUntil(() => TileInteractable(_canvas, wrong[0]), Timeout, "hint to finish");

            TapTile(_canvas, wrong[0]); // 3rd mistake (tried tile, now re-enabled): Demonstrate
            yield return Tick();
            Assert.IsFalse(TileInteractable(_canvas, 0), "tiles are disabled while the child counts");
            Assert.IsFalse(TileInteractable(_canvas, 1), "tiles are disabled while the child counts");
            Assert.IsFalse(TileInteractable(_canvas, 2), "tiles are disabled while the child counts");

            // Count every object in reading order, as the guided demo allows (any not-yet-counted order works).
            for (var i = 0; i < quantity; i++)
            {
                TapObject(_canvas, i);
                yield return Tick();
            }

            yield return WaitUntil(() => AnyTileInteractable(_canvas), Timeout, "the correct tile to be highlighted and enabled");
            var correct = CorrectTileIndex(_canvas);
            Assert.IsTrue(TileInteractable(_canvas, correct), "only the correct tile accepts a tap");
            for (var i = 0; i < 3; i++)
                if (i != correct) Assert.IsFalse(TileInteractable(_canvas, i), "the other tiles are disabled during the answer step");

            TapTile(_canvas, correct);
            yield return WaitUntil(() => _game.Progress.Coins == coinsBefore + CoinPayout.Demonstrated, Timeout,
                "the round to pay the Demonstrated tier (" + CoinPayout.Demonstrated + " coin)");
        }

        // Navigator deactivates the previous screen's Root on every switch, which halts every coroutine the
        // screen's Runner started (see the Runner comment on CountScreen) - this must hold for the new
        // Hint/Demonstrate coroutines exactly as it already did for Task 7's round flow: leaving mid-help must
        // not leave any tile stuck disabled when the child comes back.
        [UnityTest]
        public IEnumerator LeavingMidHintAndReturningStartsAFreshWorkingRound()
        {
            _game.Navigator.Show(ScreenId.Count);
            yield return WaitUntil(() => AnyTileInteractable(_canvas), Timeout, "question phase to start");

            var wrong = WrongTileIndices(_canvas);
            TapTile(_canvas, wrong[0]); // 1st mistake: Retry
            yield return Tick();
            TapTile(_canvas, wrong[1]); // 2nd mistake: Hint begins (tiles disabled, hand animating)
            yield return Tick();
            Assert.IsFalse(AnyTileInteractable(_canvas), "hint is in progress: no tile is interactable yet");

            // Leave mid-hint, then come back. A new session always starts on OnShow (see StartNewSession).
            _game.Navigator.Show(ScreenId.Map);
            yield return Tick();
            _game.Navigator.Show(ScreenId.Count);

            yield return WaitUntil(() => AnyTileInteractable(_canvas), Timeout, "the fresh round's question phase to start");
            // The stale Hint coroutine must never fire again: give it more than enough real time to have
            // finished on its own, then confirm the fresh round's tiles are still in a normal, working state
            // (all three untried and interactable) rather than being clobbered by a leftover callback.
            yield return new WaitForSeconds(3f);
            for (var i = 0; i < 3; i++)
                Assert.IsTrue(TileInteractable(_canvas, i), "tile " + i + " should be interactable in the fresh round, not stuck");
        }

        // First ever session (Progress.CountIntroSeen starts false with a fresh save): before round 1's
        // question, Eva and the hand run the Hint choreography once, unprompted, with answers disabled
        // throughout, then CountIntroSeen is committed so it never repeats.
        [UnityTest]
        public IEnumerator FirstSessionRunsIntroBeforeTheQuestionAndSetsCountIntroSeen()
        {
            // Undo SetUp's default opt-out: this is the one test that specifically exercises the once-ever intro.
            _game.Progress.CountIntroSeen = false;
            Assert.IsFalse(_game.Progress.CountIntroSeen, "fresh save: intro not seen yet");
            _game.Navigator.Show(ScreenId.Count);

            // The intro plays before any tile is ever enabled this session.
            Assert.IsFalse(AnyTileInteractable(_canvas), "answers are disabled during the intro");

            yield return WaitUntil(() => _game.Progress.CountIntroSeen, Timeout, "CountIntroSeen to be set once the intro finishes");
            yield return WaitUntil(() => AnyTileInteractable(_canvas), Timeout, "the question phase to start after the intro");
        }

        // The session-end Home button returns to the activity list; during the tutorial's GoToStore step it goes
        // to the Map instead so the first-run Store hint still fires.
        [UnityTest]
        public IEnumerator SessionHomeButtonOpensSchoolListOutsideTheTutorialStep()
        {
            _game.Progress.Tutorial = TutorialStep.Done;
            _game.Navigator.Show(ScreenId.Count);
            yield return Tick();
            Screen(_canvas).Find("EndPanel/SessionHomeButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(_game.Navigator.Current, Is.EqualTo(ScreenId.School));
        }

        [UnityTest]
        public IEnumerator SessionHomeButtonOpensMapAtGoToStore()
        {
            _game.Progress.Tutorial = TutorialStep.GoToStore;
            _game.Navigator.Show(ScreenId.Count);
            yield return Tick();
            Screen(_canvas).Find("EndPanel/SessionHomeButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(_game.Navigator.Current, Is.EqualTo(ScreenId.Map));
        }
    }
}
