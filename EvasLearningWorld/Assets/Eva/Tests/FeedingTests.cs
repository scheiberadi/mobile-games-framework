using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.Tests
{
    // Zoo and Farm Food as the Feeding game: the animals, their wishes, the belt and the screen.
    public class FeedingTests
    {
        [Test]
        public void EverySecondFoodIsARealDifferentFoodWithItsPicture()
        {
            foreach (var pair in FeedingRules.SecondFood)
            {
                var animal = ZooFarmAnimals.All.FirstOrDefault(a => a.Id == pair.Key);
                Assert.That(animal, Is.Not.Null, pair.Key + " is in the roster");
                Assert.That(pair.Value, Is.Not.EqualTo(animal.Food), pair.Key);
                Assert.That(Resources.Load<Sprite>("Art/zoofarm/food_" + pair.Value), Is.Not.Null, "second food of " + pair.Key);
            }
            foreach (var animal in ZooFarmAnimals.All)
                Assert.That(Resources.Load<Sprite>("Art/zoofarm/food_" + animal.Food), Is.Not.Null, "food of " + animal.Id);
        }

        [Test]
        public void EveryLevelHasEnoughAnimalsAndAtTwoKindsEveryOneHasASecondFood()
        {
            Assert.That(FeedingRules.PortionsByLevel.Length, Is.EqualTo(6));
            Assert.That(FeedingRules.KindsByLevel.Length, Is.EqualTo(6));
            Assert.That(FeedingRules.AnimalsByLevel.Length, Is.EqualTo(6));
            for (var level = 1; level <= 6; level++)
            {
                var pool = FeedingRules.PoolFor(level);
                Assert.That(pool.Count, Is.GreaterThanOrEqualTo(FeedingRules.AnimalsAt(level)), "level " + level);
                if (FeedingRules.KindsAt(level) == 2)
                    foreach (var animal in pool) Assert.That(FeedingRules.SecondFood.ContainsKey(animal.Id), Is.True, animal.Id + " at level " + level);
            }
        }

        // Plays whole games the way a child who always finds a wanted food on the belt does, checking the guarantees all along.
        [Test]
        public void AWholeGameFeedsEveryAnimalOnceAndTheBeltAlwaysOffersAWantedFood()
        {
            for (var level = 1; level <= 6; level++)
            for (var seed = 0; seed < 60; seed++)
            {
                var label = "level " + level + " seed " + seed;
                var game = new FeedingGame(level, new System.Random(seed));
                Assert.That(game.Total, Is.EqualTo(FeedingRules.AnimalsAt(level)), label);
                var fed = new List<string>();
                var belt = new List<string>();
                var guard = 0;
                while (!game.Finished)
                {
                    Assert.That(guard++, Is.LessThan(3000), label + " does not end");
                    while (belt.Count < 5)
                    {
                        belt.Add(game.NextBeltFood(belt));
                        var wanted = game.WantedFoods();
                        if (wanted.Count > 0) Assert.That(wanted.Any(belt.Contains), Is.True, label + " belt offers nothing wanted");
                    }

                    var wanting = Enumerable.Range(0, FeedingGame.Seats).Where(s => game.Guests[s] != null && belt.Any(game.Guests[s].Wants)).ToList();
                    var seat = wanting.Count > 0 ? wanting[0] : -1;
                    if (seat < 0) { belt.RemoveAt(0); continue; }

                    var guest = game.Guests[seat];
                    Assert.That(guest.Wishes.Length, Is.EqualTo(FeedingRules.PortionsAt(level)), label);
                    Assert.That(guest.Wishes.Distinct().Count(), Is.LessThanOrEqualTo(FeedingRules.KindsAt(level)), label);
                    if (FeedingRules.KindsAt(level) == 2) Assert.That(guest.Wishes.Distinct().Count(), Is.EqualTo(2), label + " " + guest.AnimalId + " wants two foods");
                    foreach (var wish in guest.Wishes) Assert.That(game.BeltFoods, Does.Contain(wish), label);

                    var food = belt.First(guest.Wants);
                    var result = game.Feed(seat, food, out var index);
                    Assert.That(result, Is.Not.EqualTo(FeedResult.Refused), label);
                    Assert.That(guest.Wishes[index], Is.EqualTo(food), label);
                    belt.Remove(food);
                    if (result == FeedResult.Full)
                    {
                        Assert.That(guest.Fed.All(f => f), Is.True, label);
                        fed.Add(guest.AnimalId);
                        game.Dismiss(seat);
                    }
                }
                Assert.That(fed.Count, Is.EqualTo(game.Total), label);
                Assert.That(fed.Distinct().Count(), Is.EqualTo(fed.Count), label + " an animal came twice");
                foreach (var id in fed) Assert.That(FeedingRules.PoolFor(level).Any(a => a.Id == id), Is.True, label + " " + id + " is not in the level's pool");
            }
        }

        [Test]
        public void AFoodTheAnimalDoesNotWishForIsRefusedAndAFedWishIsNotFedTwice()
        {
            var game = new FeedingGame(3, new System.Random(1)); // two portions of one food
            var guest = game.Guests[0];
            var food = guest.Wishes[0];
            var other = game.BeltFoods.First(f => f != food);
            Assert.That(game.Feed(0, other, out _), Is.EqualTo(FeedResult.Refused));
            Assert.That(game.Feed(0, food, out _), Is.EqualTo(FeedResult.Fed));
            Assert.That(game.Feed(0, food, out _), Is.EqualTo(FeedResult.Full));
            Assert.That(game.Feed(0, food, out _), Is.EqualTo(FeedResult.Refused), "a full animal takes nothing more");
        }

        [Test]
        public void ANewAnimalComesInWhenOneLeavesAndTheOthersStay()
        {
            for (var seed = 0; seed < 100; seed++)
            {
                var game = new FeedingGame(2, new System.Random(seed));
                var before = (FeedingGuest[])game.Guests.Clone();
                game.Feed(1, before[1].Wishes[0], out _);
                game.Dismiss(1);
                Assert.That(game.Guests[0], Is.SameAs(before[0]));
                Assert.That(game.Guests[2], Is.SameAs(before[2]));
                Assert.That(game.Guests[1], Is.Not.SameAs(before[1]));
                Assert.That(game.Guests[1], Is.Not.Null);
            }
        }

        [Test]
        public void TheFirstAnimalsAskForDifferentFoodsWhenTheyCan()
        {
            var overlapping = 0;
            for (var seed = 0; seed < 200; seed++)
            {
                var game = new FeedingGame(4, new System.Random(seed));
                var foods = game.Guests.Select(g => g.Wishes[0]).ToList();
                if (foods.Distinct().Count() < foods.Count) overlapping++;
            }
            Assert.That(overlapping, Is.LessThan(40), "three animals on screen often want the same food");
        }

        [Test]
        public void TheVoiceLinesAndSecondPicturesExist()
        {
            var lines = VoiceLines.Parse(Resources.Load<TextAsset>("Voice/voice-lines").text);
            foreach (var key in new[] { "zoofarm_prompt_feeding", "zoofarm_feeding_hint", "zoofarm_feeding_demo" })
            {
                Assert.That(lines.ContainsKey(key), Is.True, key);
                Assert.That(Resources.Load<AudioClip>("Voice/en/" + key), Is.Not.Null, "clip " + key);
            }
            foreach (var art in new[] { "belt", "belt_slats", "bubble" })
                Assert.That(Resources.Load<Sprite>("Art/zoofarm/" + art), Is.Not.Null, art);
        }

        // ---- The screen ------------------------------------------------------------------------------------

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

        private FeedingScreen ShowScreen()
        {
            _game.Navigator.Show(ScreenId.ZooFarmFood);
            var screen = _game.Navigator.GetScreen(ScreenId.ZooFarmFood) as FeedingScreen;
            Assert.IsNotNull(screen, "Food is registered on the feeding screen");
            return screen;
        }

        private Transform ScreenRoot => _canvasObject.transform.Find("ScreenRoot").Cast<Transform>().First(t => t.name == "FeedingScreen" && t.gameObject.activeSelf);

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [Test]
        public void TheScreenShowsThreeAnimalsWithBubblesClearOfTheButtonsAndFoodsOnTheBelt()
        {
            var screen = ShowScreen();
            var seats = Enumerable.Range(0, 3).Select(i => ScreenRoot.Find("SeatField/Seat" + i)).ToList();
            foreach (var seat in seats)
            {
                Assert.That(seat.gameObject.activeSelf, Is.True, seat.name);
                var bubble = WorldRect((RectTransform)seat.Find("Bubble"));
                Assert.That(bubble.xMin, Is.GreaterThanOrEqualTo(-720f), seat.name);
                Assert.That(bubble.xMax, Is.LessThanOrEqualTo(720f), seat.name);
                Assert.That(bubble.yMax, Is.LessThanOrEqualTo(235f), seat.name + " bubble reaches the Back button's picture");
                Assert.That(WorldRect((RectTransform)seat.Find("Animal")).yMin, Is.GreaterThan(-90f), seat.name + " animal sits above the foods");
            }
            for (var i = 0; i < seats.Count; i++)
                for (var j = i + 1; j < seats.Count; j++)
                    Assert.That(Mathf.Min(WorldRect((RectTransform)seats[i].Find("Bubble")).xMax, WorldRect((RectTransform)seats[j].Find("Bubble")).xMax) -
                        Mathf.Max(WorldRect((RectTransform)seats[i].Find("Bubble")).xMin, WorldRect((RectTransform)seats[j].Find("Bubble")).xMin), Is.LessThan(0f), "bubbles overlap");

            var foods = screen.FoodsOnBelt.ToList();
            Assert.That(foods.Count, Is.InRange(4, 7));
            Assert.That(foods.Any(f => screen.Game.WantedFoods().Contains(f.Food)), Is.True, "the belt starts with a wanted food");
            foreach (var item in ScreenRoot.GetComponentsInChildren<DragItem>(false))
            {
                var rect = WorldRect(item.Rect);
                Assert.That(rect.width, Is.GreaterThanOrEqualTo(200f));
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(-760f));
            }
        }

        [Test]
        public void AnUnwantedFoodDroppedOnAnAnimalIsNotEaten()
        {
            var screen = ShowScreen();
            var game = screen.Game;
            var items = ScreenRoot.GetComponentsInChildren<DragItem>(false).ToList();

            DragItem Find(System.Func<string, bool> food) => items.FirstOrDefault(d => food(FoodOf(screen, d)));

            // an unwanted food on animal 0
            var unwanted = Find(f => !game.WantedFoods().Contains(f));
            if (unwanted != null)
            {
                var before = game.Guests[0].Fed.Count(f => f);
                unwanted.Rect.anchoredPosition = SeatCentre(0);
                unwanted.OnEndDrag(new PointerEventData(null));
                Assert.That(game.Guests[0].Fed.Count(f => f), Is.EqualTo(before), "an unwanted food is not eaten");
            }
        }

        [Test]
        public void AWantedFoodDroppedOnItsAnimalIsEaten()
        {
            var screen = ShowScreen();
            var game = screen.Game;
            var items = ScreenRoot.GetComponentsInChildren<DragItem>(false).ToList();
            var item = items.First(d => game.WantedFoods().Contains(FoodOf(screen, d)));
            var food = FoodOf(screen, item);
            var seat = Enumerable.Range(0, 3).First(s => game.Guests[s].Wants(food));
            item.Rect.anchoredPosition = SeatCentre(seat) + new Vector2(20f, -15f);
            item.OnEndDrag(new PointerEventData(null));
            Assert.That(game.Guests[seat].Fed.Any(f => f), Is.True, "the animal ate it");

            // away from every animal is no attempt
            var other = items.First(d => d != item && d.gameObject.activeSelf);
            other.Rect.anchoredPosition = new Vector2(1000f, -300f);
            other.OnEndDrag(new PointerEventData(null));
            Assert.That(game.Guests.Sum(g => g.Fed.Count(f => f)), Is.EqualTo(1));
        }

        private static string FoodOf(FeedingScreen screen, DragItem item) =>
            screen.FoodsOnBelt.First(f => f.Position == item.Rect.anchoredPosition).Food;

        private static Vector2 SeatCentre(int seat) => new Vector2(new[] { -310f, -10f, 290f }[seat], 20f);
    }
}
