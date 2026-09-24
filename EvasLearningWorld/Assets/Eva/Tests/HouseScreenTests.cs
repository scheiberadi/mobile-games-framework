using System.Collections;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static EvasLearningWorld.Tests.CountScreenTestSupport;

namespace EvasLearningWorld.Tests
{
    public class HouseScreenTests
    {
        private const float Timeout = 10f;
        private GameObject _canvasObject;
        private EvaGame _game;
        private Transform _canvas;

        [SetUp]
        public void SetUp()
        {
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            var gameObject = new GameObject("TestEvaGame");
            _game = gameObject.AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
            _game.Progress.Tutorial = TutorialStep.Done; // no tutorial zoom-in: House opens on the overview
            _canvas = _canvasObject.transform;
        }

        [TearDown]
        public void TearDown()
        {
            if (_game != null) Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) Object.DestroyImmediate(_canvasObject);
        }

        private Transform House => _canvas.Find("ScreenRoot/HouseScreen");

        [Test]
        public void OverviewShowsEightTappableRoomsAndNoTrayOrNavigation()
        {
            _game.Navigator.Show(ScreenId.House);
            foreach (var room in HouseRooms.All)
            {
                var button = House.Find("World/Room_" + room.Id + "/Panel").GetComponent<Button>();
                Assert.IsTrue(button.interactable, room.Id);
            }
            Assert.That(House.Find("Items").childCount, Is.EqualTo(0), "no draggable items in the overview");
            Assert.That(House.Find("Slots").childCount, Is.EqualTo(0));
            Assert.IsFalse(House.Find("OverviewButton").gameObject.activeSelf);
            foreach (var n in new[] { "NavLeft", "NavRight", "NavUp", "NavDown" })
                Assert.IsFalse(House.Find(n).gameObject.activeSelf, n);
        }

        [UnityTest]
        public IEnumerator TappingARoomZoomsInAndShowsExactlyItsNeighbourArrows()
        {
            _game.Navigator.Show(ScreenId.House);
            House.Find("World/Room_kids/Panel").GetComponent<Button>().onClick.Invoke();
            yield return WaitUntil(() => House.Find("OverviewButton").gameObject.activeSelf, Timeout, "room view");

            Assert.That(House.Find("World").localScale.x, Is.EqualTo(1f).Within(1e-4f));
            Assert.IsTrue(House.Find("NavLeft").gameObject.activeSelf);
            Assert.IsTrue(House.Find("NavRight").gameObject.activeSelf);
            Assert.IsTrue(House.Find("NavUp").gameObject.activeSelf);
            Assert.IsTrue(House.Find("NavDown").gameObject.activeSelf);
            foreach (var slot in HouseSlots.InRoom("kids"))
                Assert.IsNotNull(House.Find("Slots/Slot_" + slot.Id), slot.Id);
            Assert.IsNull(House.Find("Slots/Slot_living_seat"), "only the current room's slots exist");
        }

        [UnityTest]
        public IEnumerator ArrowsMoveToTheNeighbourAndTheOverviewButtonGoesBack()
        {
            _game.Navigator.Show(ScreenId.House);
            House.Find("World/Room_living/Panel").GetComponent<Button>().onClick.Invoke();
            yield return WaitUntil(() => House.Find("OverviewButton").gameObject.activeSelf, Timeout, "living room view");
            Assert.IsFalse(House.Find("NavLeft").gameObject.activeSelf, "living has no left neighbour");
            Assert.IsFalse(House.Find("NavDown").gameObject.activeSelf, "living has no room below");

            House.Find("NavRight").GetComponent<Button>().onClick.Invoke();
            yield return WaitUntil(() => House.Find("Slots/Slot_dining_table") != null, Timeout, "dining room view");

            House.Find("OverviewButton").GetComponent<Button>().onClick.Invoke();
            yield return WaitUntil(() => House.Find("World/Room_dining/Panel").GetComponent<Button>().interactable, Timeout, "overview");
            Assert.That(House.Find("World").localScale.x, Is.LessThan(0.5f));
            Assert.IsFalse(House.Find("OverviewButton").gameObject.activeSelf);
        }

        [Test]
        public void PlacedFurnitureAppearsInTheOverviewOnItsRoomsPanel()
        {
            _game.Progress.Owned.Add("lamp");
            _game.Progress.House.TryPlace("lamp", "kids_corner", _game.Progress.Owned);
            _game.Navigator.Show(ScreenId.House);
            Assert.That(House.Find("World/Room_kids/StaticItems").childCount, Is.EqualTo(1));
            Assert.That(House.Find("World/Room_living/StaticItems").childCount, Is.EqualTo(0));
        }

        [Test]
        public void WhileThePlaceStarterTutorialIsActiveTheHouseOpensZoomedIntoTheLivingRoom()
        {
            _game.Progress.Tutorial = TutorialStep.PlaceStarter;
            _game.Navigator.Show(ScreenId.House);
            Assert.That(House.Find("World").localScale.x, Is.EqualTo(1f).Within(1e-4f));
            Assert.IsNotNull(House.Find("Slots/Slot_living_seat"));
            Assert.IsNotNull(House.Find("Items/Drag_sofa"));
        }
    }
}
