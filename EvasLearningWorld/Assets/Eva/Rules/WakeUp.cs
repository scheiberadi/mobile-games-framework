using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // Science Lab's Living vs Non-living: "Wake up!". Six things sit asleep on the lab bench. The child moves a star about; held on a living
    // thing for a moment (WakeSeconds) the thing wakes up (it wiggles, then hops into the basket). A thing that is not alive does nothing,
    // however long the star stays on it (after a while that is reported once, so Eva can say so). The round is done when every living thing
    // is in the basket. Nothing is a mistake.
    //
    // Pure logic with no clock of its own: the screen calls Step with the seconds that passed and the star's position. Positions are in
    // "picture space", canvas units.
    public enum WakeState { Asleep, Awake, InBasket }

    public sealed class WakeThing
    {
        public WakeThing(string id, bool alive, float homeX, float homeY)
        {
            Id = id; Alive = alive; HomeX = homeX; HomeY = homeY;
        }

        public string Id { get; }
        public bool Alive { get; }
        public float HomeX { get; }
        public float HomeY { get; }
        public WakeState State { get; internal set; }
        public float Progress => Alive ? Math.Min(1f, Charge / WakeUp.WakeSeconds) : 0f; // 0..1: how close a living thing is to waking
        public bool Dozing => !Alive && Dwell > 0f;                                       // the star rests on a thing that is not alive
        public bool Told { get; internal set; }                                           // the "not alive" report was made on this visit
        internal float Charge;
        internal float Dwell;
    }

    public sealed class WakeUp
    {
        public const int RoundsPerSession = 2;
        public const int ThingsPerRound = 6;
        public const float WakeRange = 120f;
        public const float WakeSeconds = 0.6f;
        public const float NotAliveSeconds = 1.3f;
        private const float ChargeDecay = 2f;

        public static readonly IReadOnlyList<(string Id, bool Alive)> Items = new[]
        {
            ("dog", true), ("stone", false), ("tree", true), ("car", false),
            ("flower", true), ("chair", false), ("goldfish", true), ("cloud", false),
            ("bird", true), ("ball", false), ("ant", true), ("book", false),
            ("cat", true), ("table", false),
        };

        // The ids for every round of a session: each round three living and three not, no thing in two rounds, every order shuffled.
        public static string[][] CreateSession(Random rng)
        {
            var alive = new List<string>();
            var other = new List<string>();
            foreach (var item in Items) (item.Alive ? alive : other).Add(item.Id);
            Shuffle(alive, rng);
            Shuffle(other, rng);
            var perKind = ThingsPerRound / 2;
            var rounds = new string[RoundsPerSession][];
            for (var r = 0; r < RoundsPerSession; r++)
            {
                var round = new List<string>();
                for (var k = 0; k < perKind; k++)
                {
                    round.Add(alive[r * perKind + k]);
                    round.Add(other[r * perKind + k]);
                }
                Shuffle(round, rng);
                rounds[r] = round.ToArray();
            }
            return rounds;
        }

        public static bool IsAlive(string id)
        {
            foreach (var item in Items) if (item.Id == id) return item.Alive;
            throw new ArgumentException("no such thing: " + id);
        }

        private readonly List<WakeThing> _things = new List<WakeThing>();

        public WakeUp(IReadOnlyList<(string Id, float X, float Y)> placed)
        {
            foreach (var p in placed) _things.Add(new WakeThing(p.Id, IsAlive(p.Id), p.X, p.Y));
        }

        public IReadOnlyList<WakeThing> Things => _things;

        // The things that woke / were rested on for NotAliveSeconds without effect, in the last Step.
        public readonly List<int> JustWoke = new List<int>();
        public readonly List<int> JustNotAlive = new List<int>();

        public int InBasketCount { get { var n = 0; foreach (var t in _things) if (t.State == WakeState.InBasket) n++; return n; } }
        public int AliveCount { get { var n = 0; foreach (var t in _things) if (t.Alive) n++; return n; } }
        public bool Done => InBasketCount == AliveCount;

        public void Step(float seconds, float x, float y)
        {
            JustWoke.Clear();
            JustNotAlive.Clear();
            for (var i = 0; i < _things.Count; i++)
            {
                var thing = _things[i];
                if (thing.State != WakeState.Asleep) continue;
                var dx = x - thing.HomeX;
                var dy = y - thing.HomeY;
                var near = Math.Sqrt(dx * dx + dy * dy) < WakeRange;

                if (!thing.Alive)
                {
                    if (!near) { thing.Dwell = 0f; thing.Told = false; continue; }
                    thing.Dwell += seconds;
                    if (!thing.Told && thing.Dwell >= NotAliveSeconds)
                    {
                        thing.Told = true;
                        JustNotAlive.Add(i);
                    }
                    continue;
                }

                if (near) thing.Charge += seconds;
                else thing.Charge = Math.Max(0f, thing.Charge - seconds * ChargeDecay);
                if (thing.Charge >= WakeSeconds)
                {
                    thing.State = WakeState.Awake;
                    JustWoke.Add(i);
                }
            }
        }

        // The awake thing has hopped into the basket.
        public void Collect(int index)
        {
            if (_things[index].State == WakeState.Awake) _things[index].State = WakeState.InBasket;
        }

        private static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var swap = list[i];
                list[i] = list[j];
                list[j] = swap;
            }
        }
    }
}
