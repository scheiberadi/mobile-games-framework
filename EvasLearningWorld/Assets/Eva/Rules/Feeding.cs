using System;
using System.Collections.Generic;
using System.Linq;

namespace EvasLearningWorld.Rules
{
    // What each animal likes to eat besides its main food (Animal.Food), for the Feeding game's later levels where a hungry animal wants two
    // or three DIFFERENT foods (an animal never wishes for the same food twice). Animals missing here (lion, tiger, wolf, snow leopard, snake,
    // frog) only ever want their main food; the dolphin has one more.
    public static class FeedingRules
    {
        public static readonly IReadOnlyDictionary<string, string[]> MoreFoods = new Dictionary<string, string[]>
        {
            { "cow", new[] { "hay", "feed" } }, { "sheep", new[] { "hay", "feed" } }, { "horse", new[] { "grass", "feed" } },
            { "zebra", new[] { "hay", "leaves" } }, { "llama", new[] { "hay", "leaves" } }, { "goat", new[] { "leaves", "hay" } },
            { "deer", new[] { "leaves", "nuts" } }, { "rhino", new[] { "leaves", "hay" } }, { "giraffe", new[] { "grass", "hay" } },
            { "elephant", new[] { "grass", "banana" } }, { "beaver", new[] { "grass", "nuts" } }, { "gorilla", new[] { "banana", "insects" } },
            { "chicken", new[] { "insects", "grass" } }, { "duck", new[] { "insects", "grass" } }, { "parrot", new[] { "nuts", "banana" } },
            { "swan", new[] { "grass", "insects" } }, { "squirrel", new[] { "seeds", "insects" } }, { "monkey", new[] { "nuts", "leaves" } },
            { "bear", new[] { "fish", "nuts" } }, { "dog", new[] { "meat", "fish" } }, { "cat", new[] { "fish", "mice" } },
            { "pig", new[] { "nuts", "grass" } }, { "owl", new[] { "insects", "fish" } }, { "fox", new[] { "insects", "nuts" } },
            { "eagle", new[] { "fish", "mice" } }, { "turtle", new[] { "insects", "shrimp" } }, { "fish", new[] { "shrimp", "insects" } },
            { "whale", new[] { "shrimp", "fish" } }, { "dolphin", new[] { "shrimp" } }, { "shark", new[] { "shrimp", "meat" } },
            { "octopus", new[] { "fish", "plankton" } },
        };

        // Per level (1-6): how many different foods an animal wishes for (one portion each), and how many animals one game feeds. The animals come
        // from the first PoolSizeByLevel animals, as in the other Zoo games, and at two or three portions only those that like that many foods.
        public static readonly int[] PortionsByLevel = { 1, 1, 2, 2, 3, 3 };
        public static readonly int[] AnimalsByLevel = { 6, 6, 6, 9, 9, 9 };

        private static int Index(int level) => Math.Max(0, Math.Min(PortionsByLevel.Length - 1, level - DifficultyLadder.MinLevel));

        // The foods an animal can wish for: its main food, then the others it likes.
        public static IReadOnlyList<string> LikedFoods(Animal animal) =>
            MoreFoods.TryGetValue(animal.Id, out var more) ? new[] { animal.Food }.Concat(more).ToArray() : new[] { animal.Food };

        // The foods an animal wishes for at this many portions (the first ones of its liked foods).
        public static IReadOnlyList<string> FoodsOf(Animal animal, int portions) => LikedFoods(animal).Take(portions).ToArray();

        // The animals a game at this level can feed: those that like at least as many foods as the level has portions.
        public static IReadOnlyList<Animal> PoolFor(int level)
        {
            var index = Index(level);
            var portions = PortionsByLevel[index];
            return ZooFarmAnimals.All.Take(Math.Min(ZooFarmRoundGenerator.PoolSizeByLevel[index], ZooFarmAnimals.All.Length))
                .Where(a => LikedFoods(a).Count >= portions).ToList();
        }

        public static int PortionsAt(int level) => PortionsByLevel[Index(level)];
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
        private readonly int _portions;
        private readonly List<Animal> _queue;
        private string _lastSpawn;

        public FeedingGuest[] Guests { get; } = new FeedingGuest[Seats];
        public IReadOnlyList<string> BeltFoods { get; }
        public int Total { get; }

        public FeedingGame(int level, Random rng)
        {
            _rng = rng;
            _portions = FeedingRules.PortionsAt(level);
            var pool = FeedingRules.PoolFor(level);
            _queue = pool.OrderBy(_ => rng.Next()).Take(FeedingRules.AnimalsAt(level)).ToList();
            Total = _queue.Count;
            BeltFoods = pool.SelectMany(a => FeedingRules.FoodsOf(a, _portions)).Distinct().ToList();
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
            var animal = _queue.FirstOrDefault(a => !FeedingRules.FoodsOf(a, _portions).Any(visible.Contains)) ?? _queue[0];
            _queue.Remove(animal);
            var wishes = WishesOf(animal);
            return new FeedingGuest { AnimalId = animal.Id, Wishes = wishes, Fed = new bool[wishes.Length] };
        }

        // One portion of each of the foods the animal wishes for, in a shuffled order.
        private string[] WishesOf(Animal animal) => FeedingRules.FoodsOf(animal, _portions).OrderBy(_ => _rng.Next()).ToArray();
    }
}
