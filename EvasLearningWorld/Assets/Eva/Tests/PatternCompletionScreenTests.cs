using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    // Answer-variety step 5: Pattern Completion with the shapes dragged into the blank instead of tapped.
    public class PatternCompletionScreenTests
    {
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

        private Transform ScreenRoot => _canvasObject.transform.Find("ScreenRoot/PatternCompletionScreen");

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [Test]
        public void TheShapesToDragAreBigInFrameAndClearOfEveryOtherTapTarget()
        {
            for (var level = DifficultyLadder.MinLevel; level <= DifficultyLadder.MaxLevel; level++)
            {
                _game.Progress.PatternCompletionLevel = level;
                _game.Navigator.Show(ScreenId.PatternCompletion);
                var items = ScreenRoot.GetComponentsInChildren<DragItem>(false);
                Assert.That(items.Length, Is.InRange(3, 4), "level " + level);

                var targets = new List<RectTransform>();
                foreach (var target in _canvasObject.GetComponentsInChildren<TapTarget>(false))
                    if (target.gameObject.activeInHierarchy && target.name != "HomeButton") targets.Add((RectTransform)target.transform); // the HUD home button deliberately overhangs the top edge (Hud.HomePosition)

                for (var i = 0; i < targets.Count; i++)
                {
                    var a = WorldRect(targets[i]);
                    Assert.That(a.width, Is.GreaterThanOrEqualTo(EvaUi.MinTap), targets[i].name + " width");
                    Assert.That(a.height, Is.GreaterThanOrEqualTo(EvaUi.MinTap), targets[i].name + " height");
                    Assert.That(a.xMin, Is.GreaterThanOrEqualTo(-720f), targets[i].name);
                    Assert.That(a.xMax, Is.LessThanOrEqualTo(720f), targets[i].name);
                    Assert.That(a.yMin, Is.GreaterThanOrEqualTo(-450f), targets[i].name);
                    Assert.That(a.yMax, Is.LessThanOrEqualTo(450f), targets[i].name);
                    for (var j = i + 1; j < targets.Count; j++)
                    {
                        var b = WorldRect(targets[j]);
                        var x = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
                        var y = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
                        if (x <= 0f || y <= 0f) continue;
                        Assert.That(Mathf.Min(x, y), Is.LessThanOrEqualTo(20f), targets[i].name + " overlaps " + targets[j].name);
                    }
                }
            }
        }

        [Test]
        public void TheShapesAreNoLongerButtons()
        {
            _game.Navigator.Show(ScreenId.PatternCompletion);
            Assert.That(ScreenRoot.Find("ChoiceField").GetComponentsInChildren<UnityEngine.UI.Button>(true).Length, Is.EqualTo(0));
        }

        [Test]
        public void TheDragVoiceLinesExist()
        {
            var lines = VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            Assert.That(lines.ContainsKey("pattern_drag_hint"), Is.True);
            Assert.That(lines.ContainsKey("pattern_drag_demo"), Is.True);
        }
    }
}
