using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.Tests
{
    // Inside a game the Hud shows Back (to the building's game list) next to Home (to the Map).
    public class NavigatorBackTests
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
            if (_game != null) Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) Object.DestroyImmediate(_canvasObject);
        }

        private Button HudButton(string name) => _canvasObject.transform.Find("HudRoot/" + name).GetComponent<Button>();

        [Test]
        public void AGameOpenedFromAGameListGoesBackToThatList()
        {
            Assert.That(Navigator.BackTargetFor(ScreenId.Count, ScreenId.School), Is.EqualTo(ScreenId.School));
            Assert.That(Navigator.BackTargetFor(ScreenId.Sorting, ScreenId.BrainGym), Is.EqualTo(ScreenId.BrainGym));
            Assert.That(Navigator.BackTargetFor(ScreenId.DressForOccasion, ScreenId.StoreActivities), Is.EqualTo(ScreenId.StoreActivities));
        }

        [Test]
        public void MenusTheMapAndGamesOpenedFromTheMapHaveNoBack()
        {
            Assert.IsNull(Navigator.BackTargetFor(ScreenId.School, ScreenId.Map));
            Assert.IsNull(Navigator.BackTargetFor(ScreenId.BrainGym, ScreenId.Sorting));
            Assert.IsNull(Navigator.BackTargetFor(ScreenId.Map, ScreenId.School));
            Assert.IsNull(Navigator.BackTargetFor(ScreenId.Settings, ScreenId.Map));
            Assert.IsNull(Navigator.BackTargetFor(ScreenId.Count, null));
        }

        [Test]
        public void InAGameOnlyBackShowsAndLeadsToTheListWhileHomeOnTheListLeadsToTheMap()
        {
            _game.Navigator.Show(ScreenId.Map);
            _game.Navigator.Show(ScreenId.School);
            Assert.IsFalse(HudButton("BackButton").gameObject.activeSelf, "a menu has no Back, Home does it");
            Assert.IsTrue(HudButton("HomeButton").gameObject.activeSelf);

            _game.Navigator.Show(ScreenId.Count);
            Assert.That(_game.Navigator.BackTarget, Is.EqualTo(ScreenId.School));
            Assert.IsTrue(HudButton("BackButton").gameObject.activeSelf);
            Assert.IsFalse(HudButton("HomeButton").gameObject.activeSelf, "inside a game only Back shows, so wild tapping cannot leave the building");
            Assert.That(((RectTransform)HudButton("BackButton").transform).anchoredPosition, Is.EqualTo(Hud.HomePosition), "Back takes Home's corner");

            _game.Navigator.Show(ScreenId.Count); // "play again" keeps the list it came from
            Assert.That(_game.Navigator.BackTarget, Is.EqualTo(ScreenId.School));

            HudButton("BackButton").onClick.Invoke();
            Assert.That(_game.Navigator.Current, Is.EqualTo(ScreenId.School));
            Assert.IsFalse(HudButton("BackButton").gameObject.activeSelf);

            Assert.IsTrue(HudButton("HomeButton").gameObject.activeSelf, "the list has Home");
            HudButton("HomeButton").onClick.Invoke();
            Assert.That(_game.Navigator.Current, Is.EqualTo(ScreenId.Map));
        }

        [Test]
        public void BackAndHomeBothHaveFullTapAreasAndBackSitsInHomesCornerInAGame()
        {
            var home = (RectTransform)HudButton("HomeButton").transform;
            var back = (RectTransform)HudButton("BackButton").transform;
            Assert.That(back.rect.width, Is.GreaterThanOrEqualTo(EvaUi.MinTap));
            Assert.That(back.rect.height, Is.GreaterThanOrEqualTo(EvaUi.MinTap));
            Assert.That(home.rect.width, Is.GreaterThanOrEqualTo(EvaUi.MinTap));
            _game.Navigator.Show(ScreenId.Map);
            _game.Navigator.Show(ScreenId.School);
            _game.Navigator.Show(ScreenId.Count);
            Assert.That(back.anchoredPosition, Is.EqualTo(home.anchoredPosition), "same corner, Home is hidden there");
        }
    }
}
