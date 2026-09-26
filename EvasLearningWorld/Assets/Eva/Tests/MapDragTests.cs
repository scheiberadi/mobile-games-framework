using EvasLearningWorld.App;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    public class MapDragTests
    {
        private GameObject _go;
        private int _pans;
        private int _ends;

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        private MapDrag Make()
        {
            _go = new GameObject("Drag", typeof(RectTransform), typeof(MapDrag));
            var viewport = (RectTransform)_go.transform;
            viewport.sizeDelta = new Vector2(1000f, 500f);
            var drag = _go.GetComponent<MapDrag>();
            _pans = 0;
            _ends = 0;
            drag.Init(viewport, _ => _pans++, () => _ends++);
            return drag;
        }

        private static PointerEventData Event(int id, Vector2 position, Vector2 delta) =>
            new PointerEventData(null) { pointerId = id, position = position, delta = delta };

        [Test]
        public void ATouchWhoseIdIsNotZeroStillPans()
        {
            // With the Input System a touch pointer id grows with every touch, so it is rarely 0.
            var drag = Make();
            drag.OnBeginDrag(Event(7, new Vector2(500f, 250f), Vector2.zero));
            drag.OnDrag(Event(7, new Vector2(520f, 250f), new Vector2(20f, 0f)));
            Assert.AreEqual(1, _pans);
        }

        [Test]
        public void AnotherFingerDuringTheDragIsIgnored()
        {
            var drag = Make();
            drag.OnBeginDrag(Event(7, new Vector2(500f, 250f), Vector2.zero));
            drag.OnDrag(Event(8, new Vector2(520f, 250f), new Vector2(20f, 0f)));
            Assert.AreEqual(0, _pans);
        }

        [Test]
        public void LiftingTheDraggingFingerReportsTheEndOfThePan()
        {
            var drag = Make();
            drag.OnBeginDrag(Event(7, new Vector2(500f, 250f), Vector2.zero));
            drag.OnEndDrag(Event(7, new Vector2(520f, 250f), Vector2.zero));
            Assert.AreEqual(1, _ends);
        }
    }
}
