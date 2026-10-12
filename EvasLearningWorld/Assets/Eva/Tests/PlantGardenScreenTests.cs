using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.Tests
{
    // The garden screen: the plants, their thought clouds and the three things the child holds over them. Coroutines (voice, hand, growth,
    // round end) do not advance in Edit Mode, but the garden's logic runs from Tick, which these tests call by hand.
    public class PlantGardenScreenTests
    {
        private GameObject _canvasObject;
        private EvaGame _game;

        [SetUp]
        public void SetUp()
        {
            PlantGardenScreen.UseDevicePlacement = false; // work in picture space
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            ((RectTransform)_canvasObject.transform).sizeDelta = new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight);
            _game = new GameObject("TestEvaGame").AddComponent<EvaGame>();
            _game.Build(_canvasObject.GetComponent<Canvas>(), new FakeKeyValueStore());
        }

        [TearDown]
        public void TearDown()
        {
            PlantGardenScreen.UseDevicePlacement = true;
            if (_game != null) Object.DestroyImmediate(_game.gameObject);
            if (_canvasObject != null) Object.DestroyImmediate(_canvasObject);
        }

        private PlantGardenScreen ShowScreen()
        {
            _game.Navigator.Show(ScreenId.PlantGrowth);
            var screen = _game.Navigator.GetScreen(ScreenId.PlantGrowth) as PlantGardenScreen;
            Assert.IsNotNull(screen, "Plant Growth is registered on its own screen");
            return screen;
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static void Tick(PlantGardenScreen screen, float seconds)
        {
            for (var t = 0f; t < seconds; t += 1f / 60f) screen.Tick(1f / 60f);
        }

        private static string ItemFor(GardenSupply supply) => supply == GardenSupply.Water ? "can" : supply == GardenSupply.Sun ? "sun" : "fertilizer";

        // One dose: the thing is held over plant `index`, then let go.
        private static void Dose(PlantGardenScreen screen, int index, GardenSupply supply)
        {
            var id = ItemFor(supply);
            screen.Grab(id);
            screen.PlaceItem(id, screen.PlantPoint(index));
            Tick(screen, GardenPlant.DoseSeconds + 0.2f);
            screen.Release(id);
            Tick(screen, 0.1f);
        }

        private static GardenSupply[] Asked(GardenPlant plant) => new[] { GardenSupply.Water, GardenSupply.Sun, GardenSupply.Fertilizer }.Where(plant.Asks).ToArray();

        [Test]
        public void TheThreeThingsAreBigTapTargetsClearOfOneAnotherTheBackButtonAndEva()
        {
            var screen = ShowScreen();
            Assert.That(screen.ItemIds.Count, Is.EqualTo(3));
            var rects = screen.ItemIds.Select(id => WorldRect(screen.Item(id).Rect)).ToArray();
            foreach (var rect in rects)
            {
                Assert.That(rect.width, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
                Assert.That(rect.height, Is.GreaterThanOrEqualTo(EvaUi.MinTap - 0.5f));
            }
            for (var i = 0; i < rects.Length; i++)
                for (var j = i + 1; j < rects.Length; j++) Assert.That(rects[i].Overlaps(rects[j]), Is.False, screen.ItemIds[i] + " / " + screen.ItemIds[j]);
        }

        [Test]
        public void TheFirstRoundHasOnePlantAskingForSomething()
        {
            var screen = ShowScreen();
            Assert.That(screen.Garden.Plants.Count, Is.EqualTo(1));
            Assert.That(Asked(screen.Garden.Plants[0]).Length, Is.InRange(1, 3));
        }

        [Test]
        public void HoldingWhatThePlantAsksForOverItGrowsIt()
        {
            var screen = ShowScreen();
            var plant = screen.Garden.Plants[0];
            foreach (var supply in Asked(plant)) Dose(screen, 0, supply);
            Assert.That(plant.Stage, Is.EqualTo(1));
        }

        [Test]
        public void HoldingSomethingElsewhereDoesNothing()
        {
            var screen = ShowScreen();
            var plant = screen.Garden.Plants[0];
            var supply = Asked(plant)[0];
            var id = ItemFor(supply);
            screen.Grab(id);
            screen.PlaceItem(id, new Vector2(600f, 300f));
            Tick(screen, 3f);
            Assert.That(plant.Given(supply), Is.EqualTo(0));
        }

        [Test]
        public void LettingGoSendsTheThingHome()
        {
            var screen = ShowScreen();
            var id = "sun";
            var home = screen.Item(id).Rect.anchoredPosition;
            screen.Grab(id);
            screen.PlaceItem(id, screen.PlantPoint(0));
            Tick(screen, 0.1f);
            screen.Release(id);
            Tick(screen, 1f);
            Assert.That(Vector2.Distance(screen.Item(id).Rect.anchoredPosition, home), Is.LessThan(1f));
        }

        [Test]
        public void ASecondGoIsNeededForEachDose()
        {
            var screen = ShowScreen();
            var plant = screen.Garden.Plants[0];
            var supply = Asked(plant)[0];
            var id = ItemFor(supply);
            screen.Grab(id);
            screen.PlaceItem(id, screen.PlantPoint(0));
            Tick(screen, 5f);
            Assert.That(plant.Given(supply), Is.LessThanOrEqualTo(1), "holding on for ever gives one dose");
        }

        [Test]
        public void ThoughtCloudShowsAnIconForEachThingAskedAndDotsForTheDoses()
        {
            var screen = ShowScreen();
            var plant = screen.Garden.Plants[0];
            var cloud = screen.Root.Find("Plants/Thought0");
            Assert.IsNotNull(cloud);
            Tick(screen, 0.1f);
            var icons = Enumerable.Range(0, 3).Count(s => cloud.Find("Icon" + s).gameObject.activeSelf);
            Assert.That(icons, Is.EqualTo(Asked(plant).Length));
            foreach (var supply in Asked(plant))
            {
                var dots = Enumerable.Range(0, 3).Count(d => cloud.Find("Icon" + (int)supply) != null && cloud.GetComponentsInChildren<Image>(true)
                    .Count(i => i.name == "Dot" + d && i.gameObject.activeSelf) > 0);
                Assert.That(dots, Is.GreaterThan(0));
            }
        }

        [Test]
        public void TheCloudsAndPlantsClearTheBackButtonAndTheCoinCounter()
        {
            var screen = ShowScreen();
            // The plants (at most PlantHalfWidth either side of their middle) stay between the sun and fertilizer on the left and the can on the right.
            foreach (var x in new[] { -350f, 0f, 350f, -190f, 190f })
            {
                Assert.That(x - PlantGardenScreen.PlantHalfWidth, Is.GreaterThan(-530f), "right of the left things, " + x);
                Assert.That(x + PlantGardenScreen.PlantHalfWidth, Is.LessThan(530f), "left of the can, " + x);
            }
        }

        // The thought cloud sits beside the top of its plant and rides up as the plant grows.
        [Test]
        public void TheThoughtCloudRidesUpWithThePlant()
        {
            var screen = ShowScreen();
            var plant = screen.Garden.Plants[0];
            var cloud = (RectTransform)screen.Root.Find("Plants/Thought0");
            var plantRect = (RectTransform)screen.Root.Find("Plants/Plant0");
            Tick(screen, 0.1f);
            Assert.That(cloud.anchoredPosition.y - cloud.rect.height * 0.5f, Is.EqualTo(plantRect.anchoredPosition.y + plantRect.sizeDelta.y).Within(60f), "the cloud stands just over the plant");
            var before = cloud.anchoredPosition.y;
            plantRect.sizeDelta *= 1.5f; // what a growing plant does (the screen swaps in a taller picture)
            Tick(screen, 0.1f);
            Assert.That(cloud.anchoredPosition.y, Is.GreaterThan(before), "a grown plant is taller, so its cloud is higher");
        }

        [Test]
        public void GrabbingSomethingWhileTheHintHandIsOutCancelsTheHint()
        {
            var screen = ShowScreen();
            Tick(screen, 12.5f);
            Assert.That(screen.Hinting, Is.True, "the idle hint started");
            screen.Grab("sun");
            Assert.That(screen.Hinting, Is.False);
        }

        [Test]
        public void ThePicturesAreThere()
        {
            foreach (var name in new[] { "world/garden_bg", "sciencelab/watering_can", "sciencelab/fertilizer_bag", "sciencelab/drop", "world/sun", "zoofarm/bubble" })
                Assert.IsNotNull(Resources.Load<Sprite>("Art/" + name), name);
        }

        [Test]
        public void EveryVoiceLineIsThere()
        {
            var keys = System.IO.File.ReadLines(System.IO.Path.Combine(Application.dataPath, "Eva/Resources/Voice/voice-lines.txt"))
                .Select(l => l.Split('\t')[0]).ToHashSet();
            foreach (var key in new[] { "garden_prompt", "garden_hint", "garden_no_water", "garden_no_sun", "garden_no_fertilizer", "garden_bloom" })
                Assert.That(keys.Contains(key), key);
        }
    }
}
