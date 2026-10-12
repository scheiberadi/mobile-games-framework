using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    public class CauseEffectTests
    {
        [Test]
        public void ARoundHasTheRightToolAndTwoOthersAllDifferent()
        {
            for (var seed = 0; seed < 60; seed++)
            {
                foreach (var pair in CauseEffect.Pairs)
                {
                    var round = CauseEffect.CreateRound(pair, new System.Random(seed));
                    Assert.That(round.Tools.Length, Is.EqualTo(CauseEffect.ToolsPerRound));
                    Assert.That(round.Tools.Distinct().Count(), Is.EqualTo(CauseEffect.ToolsPerRound));
                    Assert.That(round.Tools.Count(t => t == pair.Tool), Is.EqualTo(1));
                    Assert.That(round.Tools[round.RightTool], Is.EqualTo(pair.Tool));
                }
            }
        }

        [Test]
        public void ASessionHasThreeDifferentPairs()
        {
            for (var seed = 0; seed < 60; seed++)
            {
                var session = CauseEffect.CreateSession(new System.Random(seed));
                Assert.That(session.Length, Is.EqualTo(CauseEffect.RoundsPerSession));
                Assert.That(session.Select(r => r.Pair.Id).Distinct().Count(), Is.EqualTo(CauseEffect.RoundsPerSession));
            }
        }

        [Test]
        public void TheRightToolFinishesTheRoundAndAWrongOneNeverDoes()
        {
            var round = CauseEffect.CreateRound(CauseEffect.Pairs[0], new System.Random(1));
            var wrong = Enumerable.Range(0, 3).First(i => i != round.RightTool);
            Assert.That(round.Drop(wrong), Is.EqualTo(CauseDrop.Wrong));
            Assert.That(round.Done, Is.False);
            Assert.That(round.Drop(round.RightTool), Is.EqualTo(CauseDrop.Right));
            Assert.That(round.Done);
            Assert.That(round.Drop(round.RightTool), Is.EqualTo(CauseDrop.Again));
        }

        [Test]
        public void TwoWrongToolsInARowCallForAHint()
        {
            var round = CauseEffect.CreateRound(CauseEffect.Pairs[1], new System.Random(2));
            var wrong = Enumerable.Range(0, 3).Where(i => i != round.RightTool).ToArray();
            round.Drop(wrong[0]);
            Assert.That(round.NeedsHint, Is.False);
            round.Drop(wrong[1]);
            Assert.That(round.NeedsHint);
            round.HintGiven();
            Assert.That(round.NeedsHint, Is.False);
            Assert.That(round.TotalWrongs, Is.EqualTo(2));
        }

        [Test]
        public void EveryPairAndToolHasItsArtAndVoiceLines()
        {
            foreach (var pair in CauseEffect.Pairs)
            {
                Assert.IsNotNull(Resources.Load<Sprite>("Art/sciencelab/causebefore_" + pair.Id), pair.Id + " before");
                Assert.IsNotNull(Resources.Load<Sprite>("Art/sciencelab/causeafter_" + pair.Id), pair.Id + " after");
                Assert.IsNotNull(Resources.Load<Sprite>("Art/sciencelab/causetool_" + pair.Tool), pair.Tool + " tool");
                foreach (var key in new[] { "ce_ask_" + pair.Id, "ce_say_" + pair.Id, "ce_bad_" + pair.Tool })
                    Assert.IsNotNull(Resources.Load<AudioClip>("Voice/en/" + key), key);
            }
        }
    }

    // The Cause and Effect screen: the tray and what a drop on the thing does. Coroutines (voice, flying, hand) do not advance in Edit Mode,
    // but the rules are set synchronously on a drop.
    public class CauseEffectScreenTests
    {
        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            CauseEffectScreen.UseDevicePlacement = false; // work in picture space
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
        }

        [TearDown]
        public void TearDown()
        {
            CauseEffectScreen.UseDevicePlacement = true;
            if (_game != null) UnityEngine.Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) UnityEngine.Object.DestroyImmediate(_canvasObject);
        }

        private CauseEffectScreen ShowScreen()
        {
            _game.Navigator.Show(ScreenId.CauseAndEffect);
            var screen = _game.Navigator.GetScreen(ScreenId.CauseAndEffect) as CauseEffectScreen;
            Assert.IsNotNull(screen, "Cause and Effect is registered on its own screen");
            return screen;
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static void Drop(CauseEffectScreen screen, int tool, Vector2 point)
        {
            var drag = screen.ToolDrags[tool];
            drag.OnBeginDrag(new PointerEventData(null));
            screen.PlaceTool(tool, point);
            drag.OnEndDrag(new PointerEventData(null));
        }

        [Test]
        public void ThreeToolsAreTheOnlyThingsToDragAndEachIsAFullSizeTapTargetClearOfTheOthers()
        {
            var screen = ShowScreen();
            var drags = screen.Root.GetComponentsInChildren<DragItem>(false);
            Assert.That(drags.Length, Is.EqualTo(3));
            var rects = drags.Select(d => WorldRect(d.Rect)).ToArray();
            for (var i = 0; i < rects.Length; i++)
            {
                Assert.That(rects[i].width, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
                Assert.That(rects[i].height, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
                for (var j = i + 1; j < rects.Length; j++) Assert.That(rects[i].Overlaps(rects[j]), Is.False, i + " / " + j);
            }
            var back = new Rect(-690f, 175f, 240f, 240f);
            foreach (var rect in rects) Assert.That(rect.Overlaps(back), Is.False, "under the Back button");
        }

        [Test]
        public void ARoundStartsWithNothingChanged()
        {
            var screen = ShowScreen();
            Assert.That(screen.Round, Is.Not.Null);
            Assert.That(screen.Round.Done, Is.False);
            Assert.That(screen.Changed, Is.False);
        }

        [Test]
        public void AToolLetGoInTheTrayDoesNothingAndTheRightOneOnTheThingFinishesTheRound()
        {
            var screen = ShowScreen();
            var round = screen.Round;
            Drop(screen, round.RightTool, new Vector2(0f, -330f));
            Assert.That(round.Done, Is.False);
            Drop(screen, round.RightTool, CauseEffectScreen.ThingPoint);
            Assert.That(round.Done);
        }

        [Test]
        public void AWrongToolOnTheThingIsCountedButNeverChangesIt()
        {
            var screen = ShowScreen();
            var round = screen.Round;
            var wrong = Enumerable.Range(0, 3).First(i => i != round.RightTool);
            Drop(screen, wrong, CauseEffectScreen.ThingPoint);
            Assert.That(round.Done, Is.False);
            Assert.That(round.TotalWrongs, Is.EqualTo(1));
        }
    }
}
