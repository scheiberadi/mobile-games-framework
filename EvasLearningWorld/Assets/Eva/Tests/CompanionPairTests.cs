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
                Assert.That(box.XMin, Is.GreaterThanOrEqualTo(-720f), layout.Name);
                Assert.That(box.XMax, Is.LessThanOrEqualTo(720f), layout.Name);
                Assert.That(box.YMin, Is.GreaterThanOrEqualTo(-450f), layout.Name);
                Assert.That(box.YMax, Is.LessThanOrEqualTo(450f), layout.Name);
                Assert.That(layout.PlayerFootprint.OverlapWith(layout.EvaFootprint), Is.EqualTo(0f), layout.Name + " player vs Eva");
                Assert.That(layout.PlayerX, Is.LessThan(layout.EvaX), layout.Name + ": player left, Eva right");
            }
        }

        [Test]
        public void OpenIsLargerThanCorner()
        {
            Assert.That(CompanionLayout.Open.PlayerHeight, Is.GreaterThan(CompanionLayout.Corner.PlayerHeight));
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

        // The spike's measurement: for each of the three test screens and each standard layout, which visible
        // TapTargets the pairing's footprint would sit on. Logs one line per (screen, layout); it reports, it
        // does not fail, because "does the default conflict with Count at level 6" is a finding for the spike
        // doc, not a bug. Run it in the Unity Test Runner and read the console.
        [Test]
        public void ReportFootprintConflictsOnTheThreeSpikeScreens()
        {
            var report = new StringBuilder("Companion pairing footprint conflicts (overlap in units, 0 = clear):\n");
            Measure(report, "Count (level 6, six tiles)", () => { _game.Progress.DifficultyLevel = 5; _game.Navigator.Show(ScreenId.Count); });
            Measure(report, "Jigsaw", () => _game.Navigator.Show(ScreenId.Jigsaw));
            Measure(report, "FreeDrawing", () => _game.Navigator.Show(ScreenId.FreeDrawing));
            Debug.Log(report.ToString());
            Assert.Pass();
        }

        private void Measure(StringBuilder report, string label, System.Action show)
        {
            foreach (var layout in CompanionLayout.All)
            {
                show();
                var conflicts = new List<string>();
                var footprint = layout.Footprint;
                foreach (var target in _canvasObject.GetComponentsInChildren<TapTarget>(false))
                {
                    if (!target.gameObject.activeInHierarchy) continue;
                    var rect = (RectTransform)target.transform;
                    var corners = new Vector3[4];
                    rect.GetWorldCorners(corners);
                    var box = new FootprintBox(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
                    var overlap = footprint.OverlapWith(box);
                    if (overlap > 0f) conflicts.Add(target.name + " " + overlap.ToString("0"));
                }
                report.Append("  ").Append(label).Append(" / ").Append(layout.Name).Append(": ")
                    .Append(conflicts.Count == 0 ? "clear" : string.Join(", ", conflicts)).Append('\n');
            }
        }
    }
}
