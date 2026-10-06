using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    // What each animal likes to eat besides its main food (Animal.Food), for the Feeding game's later levels where a hungry animal
    // wants two different foods. Animals missing here (lion, tiger, wolf, snow leopard, snake, frog) only ever want their main food.
    public static class FeedingRules
    {
        public static readonly IReadOnlyDictionary<string, string> SecondFood = new Dictionary<string, string>
        {
            { "cow", "hay" }, { "sheep", "hay" }, { "zebra", "hay" }, { "llama", "hay" }, { "horse", "grass" },
            { "goat", "leaves" }, { "deer", "leaves" }, { "rhino", "leaves" }, { "giraffe", "grass" },
            { "elephant", "grass" }, { "beaver", "grass" }, { "gorilla", "banana" },
            { "chicken", "insects" }, { "duck", "insects" }, { "parrot", "nuts" }, { "swan", "grass" },
            { "squirrel", "seeds" }, { "monkey", "nuts" }, { "bear", "fish" }, { "dog", "meat" }, { "cat", "fish" },
            { "pig", "nuts" }, { "owl", "insects" }, { "fox", "insects" }, { "eagle", "fish" }, { "turtle", "insects" },
            { "fish", "shrimp" }, { "whale", "shrimp" }, { "dolphin", "shrimp" }, { "shark", "shrimp" }, { "octopus", "fish" },
        };

        // Per level (1-6): how many portions an animal eats before it is full, how many different foods those portions are,
        // and how many animals one game feeds. The animals come from the first PoolSizeByLevel animals, as in the other Zoo games.
        public static readonly int[] PortionsByLevel = { 1, 1, 2, 2, 2, 3 };
        public static readonly int[] KindsByLevel = { 1, 1, 1, 1, 2, 2 };
        public static readonly int[] AnimalsByLevel = { 6, 6, 6, 9, 9, 9 };

        private static int Index(int level) => Math.Max(0, Math.Min(PortionsByLevel.Length - 1, level - DifficultyLadder.MinLevel));

        // The foods an animal wants at this many kinds: its main food, plus the second one when two kinds are asked for and it has one.
        public static IReadOnlyList<string> FoodsOf(Animal animal, int kinds)
        {
            if (kinds >= 2 && SecondFood.TryGetValue(animal.Id, out var second)) return new[] { animal.Food, second };
            return new[] { animal.Food };
        }

        // The animals a game at this level can feed; at two kinds only those that have a second food.
        public static IReadOnlyList<Animal> PoolFor(int level)
        {
            var index = Index(level);
            var pool = ZooFarmAnimals.All.Take(Math.Min(ZooFarmRoundGenerator.PoolSizeByLevel[index], ZooFarmAnimals.All.Length));
            return (KindsByLevel[index] >= 2 ? pool.Where(a => SecondFood.ContainsKey(a.Id)) : pool).ToList();
        }

        public static int PortionsAt(int level) => PortionsByLevel[Index(level)];
        public static int KindsAt(int level) => KindsByLevel[Index(level)];
        public static int AnimalsAt(int level) => AnimalsByLevel[Index(level)];
    }

    // One hungry animal on screen: the foods it wishes for (shown in its thought bubble, one picture per portion) and which of them it has got.
    public sealed class FeedingGuest
    {
        public string AnimalId;
        public string[] Wishes;
        public bool[] Fed;

        public bool IsFull => Fed.All(f => f);
        public bool Wants(string food) { for (var i = 0; i < Wishes.Length; i++) if (!Fed[i] && Wishes[i] == food) return true; return false; }
    }

    public enum FeedResult { Refused, Fed, Full }

    // The Feeding game's state (Zoo & Farm Food): three hungry animals sit in a row; a belt carries foods past, and the child drags a food
    // to the animal that wishes for it. An animal that has all its wishes fed is full and leaves (Dismiss), and the next animal takes its
    // seat. The game ends when every animal of the game has been fed.
    //
    // Rules the screen can rely on:
    //  - every animal on screen has at least one wish left (a full one is only on screen until it is dismissed);
    //  - NextBeltFood keeps at least two different wished-for foods (fewer when fewer are wished for) on the belt, so the child never waits;
    //  - a new animal is preferably one whose wishes share no food with the animals already on screen.
    public sealed class FeedingGame
    {
        public const int Seats = 3;
        private const int WantedKindsOnBelt = 2;

        private readonly Random _rng;
        private readonly int _portions, _kinds;
        private readonly List<Animal> _queue;
        private string _lastSpawn;

        public FeedingGuest[] Guests { get; } = new FeedingGuest[Seats];
        public IReadOnlyList<string> BeltFoods { get; }
        public int Total { get; }

        public FeedingGame(int level, Random rng)
        {
            _rng = rng;
            _portions = FeedingRules.PortionsAt(level);
            _kinds = FeedingRules.KindsAt(level);
            var pool = FeedingRules.PoolFor(level);
            _queue = pool.OrderBy(_ => rng.Next()).Take(FeedingRules.AnimalsAt(level)).ToList();
            Total = _queue.Count;
            BeltFoods = pool.SelectMany(a => FeedingRules.FoodsOf(a, _kinds)).Distinct().ToList();
            for (var seat = 0; seat < Seats; seat++) Guests[seat] = NextGuest();
        }

        public int Remaining => _queue.Count + Guests.Count(g => g != null);
        public bool Finished => Remaining == 0;
        public int Done => Total - Remaining;

        // The foods some animal on screen still wishes for.
        public IReadOnlyCollection<string> WantedFoods() =>
            Guests.Where(g => g != null).SelectMany(g => g.Wishes.Where((w, i) => !g.Fed[i])).Distinct().ToList();

        // The child gives `food` to the animal in `seat`: it eats it when it wishes for it (Full when that was its last wish).
        public FeedResult Feed(int seat, string food, out int wishIndex)
        {
            wishIndex = -1;
            var guest = Guests[seat];
            if (guest == null || guest.IsFull) return FeedResult.Refused;
            for (var i = 0; i < guest.Wishes.Length; i++)
                if (!guest.Fed[i] && guest.Wishes[i] == food)
                {
                    guest.Fed[i] = true;
                    wishIndex = i;
                    return guest.IsFull ? FeedResult.Full : FeedResult.Fed;
                }
            return FeedResult.Refused;
        }

        // A full animal leaves and the next one (or nobody, at the end) takes the seat.
        public void Dismiss(int seat)
        {
            if (Guests[seat] == null || !Guests[seat].IsFull) throw new InvalidOperationException("seat " + seat + " is not full");
            Guests[seat] = null;
            Guests[seat] = NextGuest();
        }

        // The next food to put on the belt, given what is on it now.
        public string NextBeltFood(IReadOnlyCollection<string> onBelt)
        {
            var wanted = WantedFoods();
            var wantedOnBelt = wanted.Count(f => onBelt.Contains(f));
            string food;
            if (wanted.Count > 0 && wantedOnBelt < Math.Min(WantedKindsOnBelt, wanted.Count))
            {
                var missing = wanted.Where(f => !onBelt.Contains(f)).ToList();
                food = missing[_rng.Next(missing.Count)];
            }
            else
            {
                var others = BeltFoods.Where(f => f != _lastSpawn).ToList();
                if (others.Count == 0) others = BeltFoods.ToList();
                food = others[_rng.Next(others.Count)];
            }
            _lastSpawn = food;
            return food;
        }

        // Some food on the belt and the animal that wishes for it (a hint or demonstration helps with it), or false when none is there.
        public bool TryFindFeed(IReadOnlyList<string> onBelt, out int beltIndex, out int seat)
        {
            var options = new List<(int, int)>();
            for (var i = 0; i < onBelt.Count; i++)
                for (var s = 0; s < Seats; s++)
                    if (Guests[s] != null && Guests[s].Wants(onBelt[i])) options.Add((i, s));
            if (options.Count == 0) { beltIndex = seat = -1; return false; }
            (beltIndex, seat) = options[_rng.Next(options.Count)];
            return true;
        }

        private FeedingGuest NextGuest()
        {
            if (_queue.Count == 0) return null;
            var visible = new HashSet<string>(Guests.Where(g => g != null).SelectMany(g => g.Wishes));
            var animal = _queue.FirstOrDefault(a => !FeedingRules.FoodsOf(a, _kinds).Any(visible.Contains)) ?? _queue[0];
            _queue.Remove(animal);
            var wishes = WishesOf(animal);
            return new FeedingGuest { AnimalId = animal.Id, Wishes = wishes, Fed = new bool[wishes.Length] };
        }

        // One food: every portion is it. Two foods: each at least once, the rest either, in a shuffled order.
        private string[] WishesOf(Animal animal)
        {
            var foods = FeedingRules.FoodsOf(animal, _kinds);
            var wishes = new List<string>();
            if (foods.Count == 1) for (var i = 0; i < _portions; i++) wishes.Add(foods[0]);
            else
            {
                wishes.AddRange(foods);
                while (wishes.Count < _portions) wishes.Add(foods[_rng.Next(foods.Count)]);
            }
            return wishes.OrderBy(_ => _rng.Next()).ToArray();
        }
    }
}
