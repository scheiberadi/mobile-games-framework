using System.Collections;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static EvasLearningWorld.Tests.CountScreenTestSupport;

namespace EvasLearningWorld.Tests
{
    // TutorialGuide.Plan is the pure decision half of Task 12's guidance: given the current tutorial step and
    // which screen is showing, it returns the line Eva says (once per entry) and where the hand points, per
    // the brief's table. The behavioural half - Refresh's once-per-entry bookkeeping and the coroutines that
    // actually move PointerHand - was previously untested; it is covered further down (review fix round) by
    // the Refresh* tests and AdvancingTheStepStopsTheDragLoopSoTheHandDoesNotKeepPointing.
    public class TutorialGuideTests
    {
        [TestCase(TutorialStep.CreateCharacter, ScreenId.Creator, null, GuideTarget.None)] // Task 9 owns this screen's lines
        [TestCase(TutorialStep.PlaceStarter, ScreenId.House, "house_welcome", GuideTarget.StarterToLivingSeat)]
        [TestCase(TutorialStep.PlaceStarter, ScreenId.Map, "map_house", GuideTarget.HouseBuilding)]
        [TestCase(TutorialStep.GoToSchool, ScreenId.Map, "map_school", GuideTarget.SchoolBuilding)]
        [TestCase(TutorialStep.GoToStore, ScreenId.Map, "map_store", GuideTarget.StoreBuilding)]
        [TestCase(TutorialStep.FirstPurchase, ScreenId.Store, "store_welcome", GuideTarget.CheapestItem)]
        [TestCase(TutorialStep.PlacePurchase, ScreenId.Map, "map_house", GuideTarget.HouseBuilding)]
        [TestCase(TutorialStep.PlacePurchase, ScreenId.House, "house_new", GuideTarget.TrayToSlot)]
        [TestCase(TutorialStep.Done, ScreenId.House, "tut_done", GuideTarget.None)]
        public void PlanMatchesEveryRowOfTheGuidanceTable(TutorialStep step, ScreenId screen, string voiceKey, GuideTarget target)
        {
            var plan = TutorialGuide.Plan(step, screen);
            Assert.AreEqual(voiceKey, plan.voiceKey);
            Assert.AreEqual(target, plan.target);
        }

        // Every (step, screen) pair the table does not list defaults to no line and no pointing: FirstGame on
        // any screen (Tasks 7/8 already own the Count screen's own help ladder, and the brief explicitly omits
        // this step from the table), plus a sample of steps shown on a screen their table row does not cover.
        [TestCase(TutorialStep.FirstGame, ScreenId.School)]
        [TestCase(TutorialStep.FirstGame, ScreenId.Map)]
        [TestCase(TutorialStep.FirstGame, ScreenId.House)]
        [TestCase(TutorialStep.FirstGame, ScreenId.Store)]
        [TestCase(TutorialStep.FirstGame, ScreenId.Creator)]
        [TestCase(TutorialStep.CreateCharacter, ScreenId.Map)]
        [TestCase(TutorialStep.CreateCharacter, ScreenId.House)]
        [TestCase(TutorialStep.GoToSchool, ScreenId.House)]
        [TestCase(TutorialStep.GoToSchool, ScreenId.Store)]
        [TestCase(TutorialStep.GoToStore, ScreenId.House)]
        [TestCase(TutorialStep.FirstPurchase, ScreenId.Map)]
        [TestCase(TutorialStep.FirstPurchase, ScreenId.House)]
        [TestCase(TutorialStep.PlacePurchase, ScreenId.Store)]
        [TestCase(TutorialStep.PlacePurchase, ScreenId.School)]
        [TestCase(TutorialStep.Done, ScreenId.Map)]
        [TestCase(TutorialStep.Done, ScreenId.Store)]
        [TestCase(TutorialStep.Done, ScreenId.School)]
        [TestCase(TutorialStep.Done, ScreenId.Creator)]
        public void EverythingElseDefaultsToNoLineAndNoTarget(TutorialStep step, ScreenId screen)
        {
            var plan = TutorialGuide.Plan(step, screen);
            Assert.IsNull(plan.voiceKey);
            Assert.AreEqual(GuideTarget.None, plan.target);
        }

        // --- Refresh behaviour (review fix round: the Critical dedup fix and the Important coverage gap) ----

        private const float Timeout = 20f;

        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            var gameObject = new GameObject("TestEvaGame");
            _game = gameObject.AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
        }

        [TearDown]
        public void TearDown()
        {
            if (_game != null) Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) Object.DestroyImmediate(_canvasObject);
        }

        // The Critical fix: Refresh used to dedup on the (step, screen) pair, not on whether the line had
        // already been said at all this step, so PlaceStarter's House -> Map -> House (without placing the
        // sofa) re-spoke house_welcome a second time on the return to House, because the Map visit in between
        // both changed _lastScreen and overwrote Voice.LastKey to "map_house". Re-entering the same screen
        // mid-step must never re-speak its line, but a genuine step advance must always speak its new line once.
        [Test]
        public void RefreshDedupsPerStepNotPerScreen()
        {
            _game.Progress.Tutorial = TutorialStep.PlaceStarter;

            _game.Navigator.Show(ScreenId.House); // HouseScreen.OnShow -> Refresh(House): speaks house_welcome
            Assert.AreEqual("house_welcome", _game.Voice.LastKey);

            _game.Navigator.Show(ScreenId.Map); // MapScreen.OnShow -> Refresh(Map), same step: speaks map_house
            Assert.AreEqual("map_house", _game.Voice.LastKey);

            _game.Navigator.Show(ScreenId.House); // back to House mid-step, sofa still unplaced
            Assert.AreEqual("map_house", _game.Voice.LastKey,
                "house_welcome must not be re-spoken when re-entering House without the step having advanced");

            // A real step advance (e.g. the child places the sofa) always speaks the new step's line once.
            _game.Progress.Tutorial = TutorialStep.GoToSchool;
            _game.TutorialGuide.Refresh(ScreenId.Map);
            Assert.AreEqual("map_school", _game.Voice.LastKey, "a genuine step advance must speak its new line");
        }

        // Re-entering the exact same (step, screen) pair a second time (not just bouncing to another screen
        // and back) must also stay silent - the plain repeat-Refresh case the dedup has always had to handle.
        [Test]
        public void RefreshDoesNotRespeakOnASecondShowOfTheSameScreenWithTheStepUnchanged()
        {
            _game.Progress.Tutorial = TutorialStep.GoToSchool;
            _game.Navigator.Show(ScreenId.Map);
            Assert.AreEqual("map_school", _game.Voice.LastKey);

            // Move Voice.LastKey away from map_school via an unrelated line (standing in for whatever else
            // might speak between two Map visits), then repeat the exact same Refresh call as above.
            _game.Voice.Say("house_welcome");
            _game.TutorialGuide.Refresh(ScreenId.Map); // same step, same screen as the first call
            Assert.AreEqual("house_welcome", _game.Voice.LastKey,
                "re-showing the same (step, screen) must not re-speak its already-said line");
        }

        // The Important gap: no coverage existed for the drag loop actually stopping when the tutorial step
        // moves on (e.g. the child places the item mid-demonstration). StartPointing always stops the previous
        // coroutine before starting a new one, but that was never exercised end-to-end against a real,
        // running DragLoop coroutine. Interrupting mid-loop and then pumping real time well past a full
        // CycleSeconds (6s) cycle confirms no stale coroutine survives to reactivate the hand later.
        [UnityTest]
        public IEnumerator AdvancingTheStepStopsTheDragLoopSoTheHandDoesNotKeepPointing()
        {
            _game.Progress.Tutorial = TutorialStep.PlaceStarter;
            _game.Navigator.Show(ScreenId.House); // starts DragLoop("sofa", "living_seat")

            var hand = _canvasObject.transform.Find("GuideRoot/PointerHand");
            Assert.IsNotNull(hand, "the tutorial guide builds its hand under GuideRoot");
            yield return WaitUntil(() => hand.gameObject.activeSelf, Timeout, "the drag-loop hand to become visible");

            // The child places the sofa mid-demonstration: this is what actually happens in HouseScreen -
            // Progress.Advance(ItemPlaced) moves the step on, then OnItemEndDrag calls Refresh again.
            _game.Progress.Tutorial = TutorialStep.GoToSchool;
            _game.TutorialGuide.Refresh(ScreenId.House); // GoToSchool/House has no row: (null, None)

            Assert.IsFalse(hand.gameObject.activeSelf, "StartPointing(None) must hide the hand immediately");

            // Force the player loop forward for well over one full drag-loop cycle: a stale coroutine that
            // was not actually stopped would reactivate the hand on its next MoveTo within that window.
            var start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < 6.5f)
            {
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
                Assert.IsFalse(hand.gameObject.activeSelf, "no stale DragLoop coroutine should reactivate the hand");
            }
        }
    }
}
