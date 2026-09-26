using System.Collections.Generic;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.Tests
{
    public class MapScreenTests
    {
        private GameObject _canvasObject;
        private EvaGame _game;
        private FakeKeyValueStore _store;

        [SetUp]
        public void SetUp()
        {
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _store = new FakeKeyValueStore();
            var gameObject = new GameObject("TestEvaGame");
            _game = gameObject.AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), _store);
            _game.Progress.HasCharacter = true;
            _game.Progress.Tutorial = TutorialStep.Done;
        }

        [TearDown]
        public void TearDown()
        {
            if (_game != null) Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) Object.DestroyImmediate(_canvasObject);
        }

        private Transform Map => _canvasObject.transform.Find("ScreenRoot/MapScreen");
        private Button Place(PlaceId id) => Map.Find("WorldView/World/Place_" + id).GetComponent<Button>();
        private void ShowMap() => _game.Navigator.Show(ScreenId.Map);

        // While the first-run tutorial runs the view stays on the first view (centred on the origin).
        private void ShowMapAtFirstView()
        {
            _game.Progress.Tutorial = TutorialStep.PlaceStarter;
            ShowMap();
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [Test]
        public void EveryPlaceHasABuildingButtonAtItsTapBoxWithItsSize()
        {
            ShowMap();
            foreach (var place in Places.All)
            {
                var rect = (RectTransform)Place(place.Id).transform;
                Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(place.TapBox.X, place.TapBox.Y)), place.Id + " position");
                Assert.That(rect.rect.width, Is.EqualTo(place.TapBox.Width), place.Id + " width");
                Assert.That(rect.rect.height, Is.EqualTo(place.TapBox.Height), place.Id + " height");
                Assert.That(rect.rect.width, Is.GreaterThanOrEqualTo(EvaUi.MinTap));
                Assert.That(rect.rect.height, Is.GreaterThanOrEqualTo(EvaUi.MinTap));
            }
        }

        [Test]
        public void ThePlaceButtonsAreTheOnlyWaysToOpenPlacesAndDoNotOverlapEachOther()
        {
            ShowMap();
            var rects = new List<Rect>();
            foreach (var place in Places.All) rects.Add(WorldRect((RectTransform)Place(place.Id).transform));
            for (var i = 0; i < rects.Count; i++)
            for (var j = i + 1; j < rects.Count; j++) Assert.IsFalse(rects[i].Overlaps(rects[j]), i + " overlaps " + j);
        }

        [Test]
        public void FirstLaunchShowsTheFirstViewWithTheCharactersAtTheHouse()
        {
            ShowMapAtFirstView();
            var map = _game.Map;
            Assert.That(map.At, Is.EqualTo(PlaceId.House));
            Assert.That(map.CameraCentre, Is.EqualTo(Vector2.zero));
            var zone = (RectTransform)Map.Find("WorldView/World/WaveZone");
            var spot = Places.Find(PlaceId.House).StandingSpot;
            Assert.That(zone.anchoredPosition, Is.EqualTo(new Vector2(spot.X, spot.Y)));
            Assert.That(zone.rect.width, Is.GreaterThanOrEqualTo(EvaUi.MinTap));
            Assert.That(zone.rect.height, Is.GreaterThanOrEqualTo(EvaUi.MinTap));
        }

        [Test]
        public void TheWorldContainerFollowsTheCameraAndPanningIsClampedToTheWorld()
        {
            ShowMapAtFirstView();
            _game.Progress.Tutorial = TutorialStep.Done;
            var world = (RectTransform)Map.Find("WorldView/World");
            Assert.That(world.anchoredPosition, Is.EqualTo(Vector2.zero));
            _game.Map.Pan(new Vector2(-300f, 0f)); // dragging the world left moves the view right
            Assert.That(_game.Map.CameraCentre, Is.EqualTo(new Vector2(300f, 0f)));
            Assert.That(world.anchoredPosition, Is.EqualTo(new Vector2(-300f, 0f)));
            _game.Map.Pan(new Vector2(-5000f, 4000f));
            Assert.That(_game.Map.CameraCentre, Is.EqualTo(new Vector2(720f, -225f)));
            Assert.That(world.anchoredPosition, Is.EqualTo(new Vector2(-720f, 225f)));
        }

        [Test]
        public void TappingAPlaceStartsTheVoiceLineAndTheWalkInTheSameCallAndOpensNothingYet()
        {
            ShowMap();
            var said = new List<string>();
            _game.Voice.Said += key => said.Add(key);
            Place(PlaceId.School).onClick.Invoke();
            Assert.That(said, Is.EqualTo(new List<string> { "place_school" }));
            Assert.IsTrue(_game.Map.IsWalking);
            Assert.That(_game.Navigator.Current, Is.EqualTo(ScreenId.Map));
        }

        [Test]
        public void TheWalkTakesAtMostTwoAndAHalfSecondsThenOpensThePlaceAndSavesIt()
        {
            ShowMap();
            Place(PlaceId.Store).onClick.Invoke();
            var elapsed = 0f;
            while (_game.Navigator.Current == ScreenId.Map && elapsed < 10f)
            {
                _game.Map.Advance(1f / 60f);
                elapsed += 1f / 60f;
            }
            Assert.That(_game.Navigator.Current, Is.EqualTo(ScreenId.Store));
            Assert.That(elapsed, Is.LessThanOrEqualTo(MapPath.MaxSeconds + 0.05f));
            Assert.That(_game.Progress.LastPlace, Is.EqualTo("Store"));
            Assert.IsFalse(_game.Map.IsWalking);
        }

        [Test]
        public void TheCharactersAndTheViewFollowTheRouteWhileWalking()
        {
            ShowMap();
            Place(PlaceId.Store).onClick.Invoke();
            var duration = MapPath.Duration(MapPath.Route(PlaceId.House, PlaceId.Store));
            _game.Map.Advance(duration / 2f);
            var midpoint = MapPath.PositionAt(MapPath.Route(PlaceId.House, PlaceId.Store), 0.5f);
            var zone = (RectTransform)Map.Find("WorldView/World/WaveZone");
            Assert.That(zone.anchoredPosition.x, Is.EqualTo(midpoint.X).Within(0.5f));
            Assert.That(zone.anchoredPosition.y, Is.EqualTo(midpoint.Y).Within(0.5f));
            var expected = MapCamera.Clamp(midpoint);
            Assert.That(_game.Map.CameraCentre.x, Is.EqualTo(expected.X).Within(0.5f));
            Assert.That(_game.Map.CameraCentre.y, Is.EqualTo(expected.Y).Within(0.5f));
        }

        [Test]
        public void TheDragThresholdIsWideEnoughForAChildsTap()
        {
            var eventSystem = new GameObject("TestEventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            try
            {
                Object.DestroyImmediate(_game.gameObject);
                Object.DestroyImmediate(_canvasObject);
                SetUpAgainOnTheSameStore();
                Assert.GreaterOrEqual(eventSystem.GetComponent<UnityEngine.EventSystems.EventSystem>().pixelDragThreshold, 10);
            }
            finally
            {
                Object.DestroyImmediate(eventSystem);
            }
        }

        [Test]
        public void TheWaveZoneSitsBelowEveryPlaceButton()
        {
            ShowMap();
            var zone = Map.Find("WorldView/World/WaveZone").GetSiblingIndex();
            foreach (var place in Places.All)
                Assert.Less(zone, Place(place.Id).transform.GetSiblingIndex(), place.Id.ToString());
        }

        [Test]
        public void DraggingMovesTheViewEvenWhileTheTutorialRuns()
        {
            ShowMapAtFirstView();
            _game.Map.Pan(new Vector2(-300f, 0f));
            Assert.That(_game.Map.CameraCentre.x, Is.GreaterThan(1f));
        }

        [Test]
        public void TheCameraDoesNotJumpWhenAWalkStarts()
        {
            ShowMap();
            _game.Map.Pan(new Vector2(-400f, 0f));
            var start = _game.Map.CameraCentre;
            Place(PlaceId.Store).onClick.Invoke();
            var target = MapCamera.Clamp(Places.Find(PlaceId.Store).StandingSpot);
            var total = Vector2.Distance(start, new Vector2(target.X, target.Y));
            _game.Map.Advance(1f / 60f);
            Assert.Greater(total, 100f);
            Assert.That(Vector2.Distance(start, _game.Map.CameraCentre), Is.LessThan(total * 0.1f));
        }

        [Test]
        public void TapsAndDragsAreIgnoredWhileWalking()
        {
            ShowMap();
            Place(PlaceId.School).onClick.Invoke();
            var said = new List<string>();
            _game.Voice.Said += key => said.Add(key);
            Place(PlaceId.Store).onClick.Invoke();
            Assert.That(said, Is.Empty, "a second tap must not start another line");
            var before = _game.Map.CameraCentre;
            _game.Map.Pan(new Vector2(200f, 100f));
            Assert.That(_game.Map.CameraCentre, Is.EqualTo(before));
            _game.Map.Advance(10f);
            Assert.That(_game.Navigator.Current, Is.EqualTo(ScreenId.School), "the first tap wins");
        }

        [Test]
        public void TappingThePlaceTheCharactersAreAlreadyAtOpensItAtOnceWithoutWalking()
        {
            ShowMap();
            Place(PlaceId.House).onClick.Invoke();
            Assert.That(_game.Navigator.Current, Is.EqualTo(ScreenId.House));
            Assert.IsFalse(_game.Map.IsWalking);
        }

        [Test]
        public void AfterAVisitTheCharactersStandAtThatPlaceAndTheViewIsCentredThere()
        {
            ShowMap();
            Place(PlaceId.School).onClick.Invoke();
            _game.Map.Advance(10f);
            _game.Navigator.Show(ScreenId.Map);
            Assert.That(_game.Map.At, Is.EqualTo(PlaceId.School));
            var spot = Places.Find(PlaceId.School).StandingSpot;
            var zone = (RectTransform)Map.Find("WorldView/World/WaveZone");
            Assert.That(zone.anchoredPosition, Is.EqualTo(new Vector2(spot.X, spot.Y)));
            var expected = MapCamera.Clamp(spot);
            Assert.That(_game.Map.CameraCentre, Is.EqualTo(new Vector2(expected.X, expected.Y)));
        }

        [Test]
        public void TheLastPlaceComesBackFromTheSave()
        {
            _game.Progress.LastPlace = "Store";
            _game.Commit();
            Object.DestroyImmediate(_game.gameObject);
            Object.DestroyImmediate(_canvasObject);
            SetUpAgainOnTheSameStore();
            ShowMap();
            Assert.That(_game.Map.At, Is.EqualTo(PlaceId.Store));
        }

        private void SetUpAgainOnTheSameStore()
        {
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), _store);
            _game.Progress.HasCharacter = true;
            _game.Progress.Tutorial = TutorialStep.Done;
        }

        [Test]
        public void AfterTheTutorialTheViewCentresOnTheCharactersStandingSpot()
        {
            ShowMap();
            var spot = Places.Find(PlaceId.House).StandingSpot;
            var expected = MapCamera.Clamp(spot);
            Assert.That(_game.Map.CameraCentre, Is.EqualTo(new Vector2(expected.X, expected.Y)));
        }

        // While the first-run tutorial runs, the view stays on the first view so the hand always finds School and Store.
        [Test]
        public void DuringTheTutorialTheViewStaysOnTheFirstViewEvenAfterAVisit()
        {
            _game.Progress.Tutorial = TutorialStep.GoToSchool;
            _game.Progress.LastPlace = "School";
            ShowMap();
            Assert.That(_game.Map.CameraCentre, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void TheTutorialHandTargetsAreTheLiveScreenPositionsOfTheBuildings()
        {
            ShowMapAtFirstView();
            var school = Places.Find(PlaceId.School).TapBox;
            Assert.That(_game.Map.ScreenPositionOf(PlaceId.School), Is.EqualTo(new Vector2(school.X, school.Y)));
            _game.Progress.Tutorial = TutorialStep.Done;
            _game.Map.Pan(new Vector2(-100f, 0f));
            Assert.That(_game.Map.ScreenPositionOf(PlaceId.School), Is.EqualTo(new Vector2(school.X - 100f, school.Y)));
        }

        [Test]
        public void TheSettingsGearIsAtTheTopLeftWithAFullSizeTapArea()
        {
            ShowMap();
            var gear = (RectTransform)Map.Find("SettingsButton");
            Assert.IsNotNull(gear);
            Assert.That(gear.rect.width, Is.GreaterThanOrEqualTo(EvaUi.MinTap));
            Assert.That(gear.rect.height, Is.GreaterThanOrEqualTo(EvaUi.MinTap));
            var zone = Places.SettingsZone;
            var rect = WorldRect(gear);
            Assert.That(rect.xMin, Is.EqualTo(zone.XMin).Within(1f));
            Assert.That(rect.yMax, Is.EqualTo(zone.YMax).Within(1f));
            // Exactly the Hud Home button: same anchor, position and size.
            var home = (RectTransform)_game.Hud.transform.Find("HomeButton");
            Assert.That(gear.anchorMin, Is.EqualTo(home.anchorMin));
            Assert.That(gear.anchoredPosition, Is.EqualTo(home.anchoredPosition));
            Assert.That(gear.sizeDelta, Is.EqualTo(home.sizeDelta));
            Assert.That(WorldRect(gear).center.x, Is.EqualTo(WorldRect(home).center.x).Within(0.5f));
            Assert.That(WorldRect(gear).center.y, Is.EqualTo(WorldRect(home).center.y).Within(0.5f));
        }

        [Test]
        public void LeavingTheMapMidWalkCancelsTheWalk()
        {
            ShowMap();
            Place(PlaceId.Store).onClick.Invoke();
            _game.Navigator.Show(ScreenId.House);
            Assert.IsFalse(_game.Map.IsWalking);
        }
    }
}
