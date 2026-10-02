using System.Collections.Generic;
using System.Text;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.Tests
{
    // M5 Task 6 spike checks for the shared companion pairing (docs/superpowers/spikes/character-everywhere.md).
    public class CompanionPairTests
    {
        [Test]
        public void StandardLayoutsStayInsideTheFrameAndTheTwoCharactersDoNotOverlap()
        {
            foreach (var layout in CompanionLayout.All)
            {
                var box = layout.Footprint;
                Assert.That(layout.PlayerX, Is.InRange(-720f, 720f), layout.Name);
                Assert.That(layout.EvaX, Is.InRange(-720f, 720f), layout.Name);
                Assert.That(box.YMin, Is.GreaterThanOrEqualTo(-450f), layout.Name);
                Assert.That(box.YMax, Is.LessThanOrEqualTo(450f), layout.Name);
                Assert.That(layout.PlayerFootprint.OverlapWith(layout.EvaFootprint), Is.EqualTo(0f), layout.Name + " player vs Eva");
                Assert.That(layout.PlayerX, Is.LessThan(layout.EvaX), layout.Name + ": player left, Eva right");
            }
        }

        [Test]
        public void SideIsLargerThanCorner()
        {
            Assert.That(CompanionLayout.Side.PlayerHeight, Is.GreaterThan(CompanionLayout.Corner.PlayerHeight));
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

        [Test]
        public void PairIsPurelyDecorativeAndSwallowsNoTaps()
        {
            _game.Navigator.Show(ScreenId.FreeDrawing);
            var pair = CompanionPair.Create(_game.ScreenRoot, _game, CompanionLayout.Corner);
            Assert.That(pair.Root.GetComponentsInChildren<TapTarget>(true), Is.Empty);
            foreach (var graphic in pair.Root.GetComponentsInChildren<Graphic>(true))
                Assert.That(graphic.raycastTarget, Is.False, graphic.name);
            Object.DestroyImmediate(pair.Root.gameObject);
        }

        // Rollout check (M5 Task 7): every screen that stands on the shared pair, at its first view, against
        // every visible TapTarget and DragItem. Prints one line per screen that conflicts, then fails if any does.
        [Test]
        public void NoScreenWithAPairHasATapTargetOrDragItemUnderIt()
        {
            var report = new StringBuilder();
            var withPair = 0;
            foreach (ScreenId id in System.Enum.GetValues(typeof(ScreenId)))
            {
                if (_game.Navigator.GetScreen(id) == null) continue;
                _game.Navigator.Show(id);
                var marker = _game.Navigator.GetScreen(id).Root.GetComponentInChildren<CompanionPairMarker>(false);
                if (marker == null) continue;
                withPair++;
                AppendConflicts(report, id.ToString(), id);
            }
            Assert.That(withPair, Is.GreaterThan(40), "the pair should be on the generic presenters");
            Assert.That(report.ToString(), Is.Empty, "pair footprint conflicts:" + (char)10 + report);
        }

        // Count's answer row grows from three tiles to six with the level, and it is the tightest screen.
        [Test]
        public void CountKeepsClearOfThePairAtEveryDifficultyLevel()
        {
            var report = new StringBuilder();
            for (var level = 1; level <= 6; level++)
            {
                _game.Progress.DifficultyLevel = level;
                _game.Navigator.Show(ScreenId.Count);
                AppendConflicts(report, "Count level " + level, ScreenId.Count);
            }
            Assert.That(report.ToString(), Is.Empty, "pair footprint conflicts:" + (char)10 + report);
        }

        private void AppendConflicts(StringBuilder report, string label, ScreenId id)
        {
            var screenRoot = _game.Navigator.GetScreen(id).Root;
            var pair = screenRoot.GetComponentInChildren<CompanionPairMarker>(false).Pair;
            var footprint = pair.Layout.Footprint;
            var conflicts = new List<string>();
            var handles = new List<Component>();
            handles.AddRange(screenRoot.GetComponentsInChildren<TapTarget>(false));
            handles.AddRange(screenRoot.GetComponentsInChildren<DragItem>(false));
            foreach (var target in handles)
            {
                if (!target.gameObject.activeInHierarchy) continue;
                var corners = new Vector3[4];
                ((RectTransform)target.transform).GetWorldCorners(corners);
                var overlap = footprint.OverlapWith(new FootprintBox(corners[0].x, corners[0].y, corners[2].x, corners[2].y));
                if (overlap > MaxOverlap) conflicts.Add(target.name + " " + overlap.ToString("0"));
            }
            if (conflicts.Count > 0)
                report.Append("  ").Append(label).Append(" (").Append(pair.Layout.Name).Append("): ").Append(string.Join(", ", conflicts)).Append((char)10);
        }

        // The footprint is a deliberately generous box (see CompanionLayout), so a graze under this is not a conflict.
        private const float MaxOverlap = 20f;
    }
}
