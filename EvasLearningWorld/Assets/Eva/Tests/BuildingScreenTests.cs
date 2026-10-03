using System;
using System.Collections.Generic;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.Tests
{
    public class BuildingScreenTests
    {
        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            var gameObject = new GameObject("TestEvaGame");
            _game = gameObject.AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
            _game.Progress.Tutorial = TutorialStep.Done;
        }

        [TearDown]
        public void TearDown()
        {
            if (_game != null) UnityEngine.Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) UnityEngine.Object.DestroyImmediate(_canvasObject);
        }

        private Transform SchoolScreen => _canvasObject.transform.Find("ScreenRoot/BuildingScreen");

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
        }

        private List<RectTransform> Tiles()
        {
            var tiles = new List<RectTransform>();
            foreach (var activity in Activities.For(BuildingId.School))
                tiles.Add((RectTransform)SchoolScreen.Find("Menu/Content/Tile_" + activity.Id));
            return tiles;
        }

        [Test]
        public void SchoolShowsOneTileForEachActivityAndEveryScreenKeyIsARegisteredScreen()
        {
            _game.Navigator.Show(ScreenId.School);
            Assert.That(Tiles().Count, Is.EqualTo(Activities.For(BuildingId.School).Count));
            foreach (var activity in Activities.For(BuildingId.School))
            {
                var id = (ScreenId)Enum.Parse(typeof(ScreenId), activity.ScreenKey);
                _game.Navigator.Show(id);
                Assert.AreEqual(id, _game.Navigator.Current, activity.Id);
            }
        }

        [Test]
        public void TilesFollowTheLayoutAndAreAtLeastMinTapAndDoNotOverlap()
        {
            _game.Navigator.Show(ScreenId.School);
            var tiles = Tiles();
            var layout = TileLayout.Compute(Activities.For(BuildingId.School).Count);
            for (var i = 0; i < tiles.Count; i++)
            {
                Assert.That(tiles[i].rect.width, Is.GreaterThanOrEqualTo(EvaUi.MinTap));
                Assert.That(tiles[i].rect.height, Is.GreaterThanOrEqualTo(EvaUi.MinTap));
                Assert.That(tiles[i].anchoredPosition, Is.EqualTo(new Vector2(layout[i].X, layout[i].YMax)));
                for (var j = i + 1; j < tiles.Count; j++)
                    Assert.IsFalse(WorldRect(tiles[i]).Overlaps(WorldRect(tiles[j])), "tiles " + i + " and " + j + " overlap");
            }
        }

        // The building's tile list scrolls (BuildingScreen.AddScrollingMenu), so tiles past the first page sit
        // outside the screen's own world rect by design - only the Menu's own clipped viewport (what's ever
        // actually visible or tappable, since its RectMask2D blocks both rendering and raycasts outside it) needs
        // to stay inside the safe area and clear of the Hud.
        [Test]
        public void TheMenuStaysInsideTheSafeAreaAndClearOfTheHomeButtonAndCoinCounter()
        {
            _game.Navigator.Show(ScreenId.School);
            var root = WorldRect((RectTransform)SchoolScreen);
            var hud = _canvasObject.transform.Find("HudRoot");
            var blocked = new List<Rect>
            {
                WorldRect((RectTransform)hud.Find("HomeButton")),
                WorldRect((RectTransform)hud.Find("CoinIcon")),
                WorldRect((RectTransform)hud.Find("CoinCount")),
            };
            var menu = WorldRect((RectTransform)SchoolScreen.Find("Menu"));
            Assert.That(menu.xMin, Is.GreaterThanOrEqualTo(root.xMin));
            Assert.That(menu.xMax, Is.LessThanOrEqualTo(root.xMax));
            Assert.That(menu.yMin, Is.GreaterThanOrEqualTo(root.yMin));
            Assert.That(menu.yMax, Is.LessThanOrEqualTo(root.yMax));
            foreach (var other in blocked) Assert.IsFalse(menu.Overlaps(other), "Menu touches a Hud control");
        }

        // The first two rows of every building's list are fully visible without scrolling (the lowest row used to be cut off by half a
        // tile because the tiles hang from the top of the content and were placed by their centre).
        [Test]
        public void TheFirstTwoRowsOfEveryBuildingsListFitInsideTheMenuViewport()
        {
            foreach (BuildingId building in Enum.GetValues(typeof(BuildingId)))
            {
                var activities = Activities.For(building);
                if (activities.Count == 0) continue;
                _game.Navigator.Show((ScreenId)Enum.Parse(typeof(ScreenId), building.ToString()));
                RectTransform menu = null;
                foreach (Transform screen in _canvasObject.transform.Find("ScreenRoot"))
                {
                    var candidate = screen.Find("Menu");
                    if (screen.gameObject.activeInHierarchy && candidate != null) menu = (RectTransform)candidate;
                }
                Assert.IsNotNull(menu, building.ToString());
                var viewport = WorldRect(menu);
                for (var i = 0; i < Math.Min(8, activities.Count); i++)
                {
                    var tile = WorldRect((RectTransform)menu.Find("Content/Tile_" + activities[i].Id));
                    Assert.That(tile.xMin, Is.GreaterThanOrEqualTo(viewport.xMin - 0.5f), building + " tile " + i);
                    Assert.That(tile.xMax, Is.LessThanOrEqualTo(viewport.xMax + 0.5f), building + " tile " + i);
                    Assert.That(tile.yMin, Is.GreaterThanOrEqualTo(viewport.yMin - 0.5f), building + " tile " + i + " is cut off at the bottom");
                    Assert.That(tile.yMax, Is.LessThanOrEqualTo(viewport.yMax + 0.5f), building + " tile " + i);
                }
            }
        }

        [Test]
        public void TappingATileStartsTheVoiceLineAndOpensTheActivityImmediately()
        {
            _game.Navigator.Show(ScreenId.School);
            var said = new List<string>();
            _game.Voice.Said += key => said.Add(key);

            SchoolScreen.Find("Menu/Content/Tile_count").GetComponent<Button>().onClick.Invoke();

            Assert.AreEqual(ScreenId.Count, _game.Navigator.Current, "the activity opens in the same call");
            Assert.That(said.Count, Is.GreaterThan(0));
            Assert.AreEqual("activity_count", said[0], "the tile's line is the first thing Eva says");
        }

        [Test]
        public void OpeningTheSchoolListAdvancesTheTutorialFromGoToSchoolToFirstGame()
        {
            _game.Progress.Tutorial = TutorialStep.GoToSchool;
            _game.Navigator.Show(ScreenId.School);
            Assert.AreEqual(TutorialStep.FirstGame, _game.Progress.Tutorial);
        }

        [Test]
        public void OpeningCountDoesNotAdvanceTheTutorial()
        {
            _game.Progress.Tutorial = TutorialStep.GoToSchool;
            _game.Navigator.Show(ScreenId.Count);
            Assert.AreEqual(TutorialStep.GoToSchool, _game.Progress.Tutorial);
        }
    }
}
