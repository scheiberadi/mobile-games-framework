using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;

namespace EvasLearningWorld.Tests
{
    // Scenery reaches the whole canvas (under a camera cutout too); controls stay inside the safe area.
    public class FullBleedTests
    {
        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            // A 2340 x 1080 phone with a 72 px camera cutout on the left edge.
            FullBleed.ScreenSizeOverride = new Vector2Int(2340, 1080);
            FullBleed.SafeAreaOverride = new Rect(72f, 0f, 2340f - 72f, 1080f);
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            var gameObject = new GameObject("TestEvaGame");
            _game = gameObject.AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
            _game.Progress.HasCharacter = true;
            _game.Progress.Tutorial = TutorialStep.Done;
        }

        [TearDown]
        public void TearDown()
        {
            FullBleed.SafeAreaOverride = null;
            FullBleed.ScreenSizeOverride = null;
            if (_game != null) Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) Object.DestroyImmediate(_canvasObject);
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var c = new Vector3[4];
            rect.GetWorldCorners(c);
            return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
        }

        private Rect Canvas => WorldRect((RectTransform)_canvasObject.transform);
        private Transform Screen(string name) => _canvasObject.transform.Find("ScreenRoot/" + name);

        private static void AssertCovers(Rect background, Rect canvas, string what)
        {
            Assert.That(background.xMin, Is.LessThanOrEqualTo(canvas.xMin + 0.01f), what + " left");
            Assert.That(background.yMin, Is.LessThanOrEqualTo(canvas.yMin + 0.01f), what + " bottom");
            Assert.That(background.xMax, Is.GreaterThanOrEqualTo(canvas.xMax - 0.01f), what + " right");
            Assert.That(background.yMax, Is.GreaterThanOrEqualTo(canvas.yMax - 0.01f), what + " top");
        }

        private static void AssertSame(Rect a, Rect b, string what)
        {
            Assert.That(a.xMin, Is.EqualTo(b.xMin).Within(0.01f), what);
            Assert.That(a.yMin, Is.EqualTo(b.yMin).Within(0.01f), what);
            Assert.That(a.xMax, Is.EqualTo(b.xMax).Within(0.01f), what);
            Assert.That(a.yMax, Is.EqualTo(b.yMax).Within(0.01f), what);
        }

        [Test]
        public void InsetsAreTheDistanceFromTheSafeAreaToTheCanvasEdges()
        {
            var insets = FullBleed.Insets(new Rect(72f, 0f, 2268f, 1080f), 2340f, 1080f, new Vector2(1170f, 540f));
            Assert.That(insets, Is.EqualTo(new Vector4(36f, 0f, 0f, 0f)));
            Assert.That(FullBleed.CentreShift(insets), Is.EqualTo(new Vector2(18f, 0f)));
        }

        [Test]
        public void TheScreenRootLeavesTheCutoutAsideButTheMapWorldReachesEveryCanvasEdge()
        {
            _game.Navigator.Show(ScreenId.Map);
            var root = WorldRect(_game.ScreenRoot);
            Assert.That(root.xMin, Is.GreaterThan(Canvas.xMin + 1f), "the safe area starts right of the cutout");

            var view = (RectTransform)Screen("MapScreen").Find("WorldView");
            AssertSame(WorldRect(view), Canvas, "world view");

            var world = (RectTransform)view.Find("World");
            _game.Map.Pan(new Vector2(100000f, 0f)); // drag right: the view goes to the world's left edge
            var left = WorldRect((RectTransform)world.Find("BackdropLeft"));
            Assert.That(left.xMin, Is.EqualTo(Canvas.xMin).Within(0.01f), "world left edge at the screen edge");
            _game.Map.Pan(new Vector2(-100000f, 0f));
            var right = WorldRect((RectTransform)world.Find("BackdropRight"));
            Assert.That(right.xMax, Is.EqualTo(Canvas.xMax).Within(0.01f), "world right edge at the screen edge");
        }

        [Test]
        public void TheMapControlsStayInsideTheSafeArea()
        {
            _game.Navigator.Show(ScreenId.Map);
            var root = WorldRect(_game.ScreenRoot);
            var gear = WorldRect((RectTransform)Screen("MapScreen").Find("SettingsButton"));
            Assert.That(gear.xMin, Is.GreaterThanOrEqualTo(root.xMin - 0.01f));
            Assert.That(gear.xMin, Is.GreaterThan(Canvas.xMin + 1f));
        }

        [Test]
        public void TheTutorialHandPositionIsRelativeToTheSafeAreaRoot()
        {
            _game.Navigator.Show(ScreenId.Map);
            var school = Places.Find(PlaceId.School).TapBox;
            var shift = FullBleed.CentreShift(FullBleed.CurrentInsets(_canvasObject.transform));
            Assert.That(shift.x, Is.GreaterThan(1f));
            Assert.That(_game.Map.ScreenPositionOf(PlaceId.School).x, Is.EqualTo(school.X - _game.Map.CameraCentre.x - shift.x).Within(0.01f));
        }

        [Test]
        public void EveryPictureBackgroundCoversTheWholeCanvas()
        {
            foreach (var pair in new[] { (ScreenId.Store, "StoreScreen"), (ScreenId.School, "BuildingScreen"), (ScreenId.Count, "CountScreen") })
            {
                _game.Navigator.Show(pair.Item1);
                var background = (RectTransform)Screen(pair.Item2).Find("Background");
                Assert.IsNotNull(background, pair.Item2 + " background");
                AssertSame(WorldRect(background), Canvas, pair.Item2);
            }
        }

        [Test]
        public void TheCountWindowViewFollowsTheBackgroundArtNotTheSafeArea()
        {
            _game.Navigator.Show(ScreenId.Count);
            var background = (RectTransform)Screen("CountScreen").Find("Background");
            var view = (RectTransform)background.Find("WindowView");
            Assert.IsNotNull(view, "the window view is a child of the full-bleed background");
            var bg = WorldRect(background);
            var window = WorldRect(view);
            Assert.That((window.xMin - bg.xMin) / bg.width, Is.EqualTo(66f / 1920f).Within(0.001f));
            Assert.That((window.xMax - bg.xMin) / bg.width, Is.EqualTo(274f / 1920f).Within(0.001f));
            Assert.That((bg.yMax - window.yMax) / bg.height, Is.EqualTo(181f / 900f).Within(0.001f));
            Assert.That((bg.yMax - window.yMin) / bg.height, Is.EqualTo(439f / 900f).Within(0.001f));
        }

        [Test]
        public void SolidColourBackgroundsCoverTheWholeCanvasToo()
        {
            _game.Navigator.Show(ScreenId.Settings);
            AssertCovers(WorldRect((RectTransform)Screen("SettingsScreen").Find("Background")), Canvas, "Settings");
        }
    }
}
