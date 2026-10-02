using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    // Answer-variety step 5: What's Missing with the missing shape dragged back into the empty spot instead of tapped.
    public class WhatsMissingScreenTests
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

        private Transform ScreenRoot => _canvasObject.transform.Find("ScreenRoot/WhatsMissingScreen");

        [Test]
        public void TheChoicesAreDragItemsNotButtons()
        {
            _game.Navigator.Show(ScreenId.WhatsMissing);
            var field = ScreenRoot.Find("ChoiceField");
            Assert.That(field.GetComponentsInChildren<UnityEngine.UI.Button>(true).Length, Is.EqualTo(0));
            Assert.That(field.GetComponentsInChildren<DragItem>(true).Length, Is.EqualTo(4));
        }

        [Test]
        public void TheBlankRingExistsAndStartsHidden()
        {
            _game.Navigator.Show(ScreenId.WhatsMissing);
            var ring = ScreenRoot.Find("SetField/BlankRing");
            Assert.That(ring, Is.Not.Null);
            Assert.That(ring.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void TheVoiceLinesTheDragVersionUsesExist()
        {
            var lines = VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            foreach (var key in new[] { "whatsmissing_watch", "whatsmissing_find", "whatsmissing_hint", "pattern_drag_demo" })
                Assert.That(lines.ContainsKey(key), Is.True, key);
        }
    }
}
