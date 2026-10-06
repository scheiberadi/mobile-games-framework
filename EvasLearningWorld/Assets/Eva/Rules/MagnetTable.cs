using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // Science Lab's Magnet table. Six things lie on a table: some a magnet pulls, some it does not. The child holds a horseshoe magnet and
    // moves it about. Near a magnetic thing the thing wiggles and leans toward the magnet (Pull, stronger the nearer); held there for a
    // moment (StickSeconds) it jumps up and sticks to the magnet's tips. A thing the magnet does not pull never moves, however long the
    // magnet sits on it (after a while it is reported once, so Eva can say so). Stuck things ride along with the magnet; letting go over the bucket
    // drops them in. The round is done when every magnetic thing is in the bucket.
    //
    // Pure logic with no clock of its own: the screen calls Step with the seconds that passed and the magnet's pole point (the middle of the
    // two tips). All positions are in the same plane, "picture space", in canvas units.
    public enum MagnetThingState { OnTable, Stuck, InBucket }

    public sealed class MagnetThing
    {
        public MagnetThing(string id, bool magnetic, float homeX, float homeY)
        {
            Id = id; Magnetic = magnetic; HomeX = homeX; HomeY = homeY; Slot = -1;
        }

        public string Id { get; }
        public bool Magnetic { get; }
        public float HomeX { get; }
        public float HomeY { get; }
        public MagnetThingState State { get; internal set; }
        public int Slot { get; internal set; }   // where it hangs on the magnet (0..MaxStuck-1) while Stuck
        public float Pull { get; internal set; } // 0..1: how hard the magnet pulls it now (a magnetic thing lying on the table)
        internal float Charge;                   // seconds the magnet has been right on it
        internal float Dwell;                    // seconds the magnet has been right on a thing it does not pull
        internal bool Told;                      // the "it does not stick" report was made on this visit
    }

    public sealed class MagnetTable
    {
        public const int RoundsPerSession = 2;
        public const int ThingsPerRound = 6;
        public const int MaxStuck = 3;
        public const float PullRange = 330f;     // the wiggle starts this far from the thing
        public const float StickRange = 150f;    // right on it: charging
        public const float StickSeconds = 0.2f;  // how long it has to stay right on it before it jumps up
        public const float NoPullSeconds = 1.3f; // how long on a thing that is not pulled before that is said
        private const float PullEase = 7f;       // how fast Pull follows the distance
        private const float ChargeDecay = 2f;

        public static readonly IReadOnlyList<(string Id, bool Magnetic)> Items = new[]
        {
            ("nail", true), ("pencil", false), ("paperclip", true), ("leaf2", false),
            ("scissors", true), ("button", false), ("fork", true), ("plastic_cup", false),
            ("bottle_cap", true), ("wooden_block", false), ("screw", true), ("cotton_ball", false),
        };

        // The ids for every round of a session: each round three magnetic and three not, no thing in two rounds, every order shuffled.
        public static string[][] CreateSession(Random rng)
        {
            var pulled = new List<string>();
            var other = new List<string>();
            foreach (var item in Items) (item.Magnetic ? pulled : other).Add(item.Id);
            Shuffle(pulled, rng);
            Shuffle(other, rng);
            var perKind = ThingsPerRound / 2;
            var rounds = new string[RoundsPerSession][];
            for (var r = 0; r < RoundsPerSession; r++)
            {
                var round = new List<string>();
                for (var k = 0; k < perKind; k++)
                {
                    round.Add(pulled[r * perKind + k]);
                    round.Add(other[r * perKind + k]);
                }
                Shuffle(round, rng);
                rounds[r] = round.ToArray();
            }
            return rounds;
        }

        public static bool IsMagnetic(string id)
        {
            foreach (var item in Items) if (item.Id == id) return item.Magnetic;
            throw new ArgumentException("no such thing: " + id);
        }

        private readonly List<MagnetThing> _things = new List<MagnetThing>();

        public MagnetTable(IReadOnlyList<(string Id, float X, float Y)> placed)
        {
            foreach (var p in placed) _things.Add(new MagnetThing(p.Id, IsMagnetic(p.Id), p.X, p.Y));
        }

        public IReadOnlyList<MagnetThing> Things => _things;

        // The things that jumped up onto the magnet / were held on for NoPullSeconds without effect, in the last Step.
        public readonly List<int> JustStuck = new List<int>();
        public readonly List<int> JustNotPulled = new List<int>();

        public int StuckCount { get { var n = 0; foreach (var t in _things) if (t.State == MagnetThingState.Stuck) n++; return n; } }
        public int InBucketCount { get { var n = 0; foreach (var t in _things) if (t.State == MagnetThingState.InBucket) n++; return n; } }
        public int MagneticCount { get { var n = 0; foreach (var t in _things) if (t.Magnetic) n++; return n; } }
        public bool Done => InBucketCount == MagneticCount;

        public void Step(float seconds, float poleX, float poleY)
        {
            JustStuck.Clear();
            JustNotPulled.Clear();
            for (var i = 0; i < _things.Count; i++)
            {
                var thing = _things[i];
                if (thing.State != MagnetThingState.OnTable) continue;
                var dx = poleX - thing.HomeX;
                var dy = poleY - thing.HomeY;
                var distance = (float)Math.Sqrt(dx * dx + dy * dy);
                var near = distance < StickRange;

                if (!thing.Magnetic)
                {
                    if (!near) { thing.Dwell = 0f; thing.Told = false; continue; }
                    thing.Dwell += seconds;
                    if (!thing.Told && thing.Dwell >= NoPullSeconds)
                    {
                        thing.Told = true;
                        JustNotPulled.Add(i);
                    }
                    continue;
                }

                var target = distance >= PullRange ? 0f : Math.Min(1f, 1f - (distance - StickRange) / (PullRange - StickRange));
                thing.Pull = MoveTowards(thing.Pull, target, seconds * PullEase);
                if (near) thing.Charge += seconds;
                else thing.Charge = Math.Max(0f, thing.Charge - seconds * ChargeDecay);

                if (thing.Charge >= StickSeconds && StuckCount < MaxStuck)
                {
                    thing.State = MagnetThingState.Stuck;
                    thing.Slot = FreeSlot();
                    thing.Pull = 0f;
                    thing.Charge = 0f;
                    JustStuck.Add(i);
                }
            }
        }

        // The magnet is let go over the bucket: everything stuck to it falls in. Returns how many fell.
        public int DropStuck()
        {
            var dropped = 0;
            foreach (var thing in _things)
            {
                if (thing.State != MagnetThingState.Stuck) continue;
                thing.State = MagnetThingState.InBucket;
                thing.Slot = -1;
                dropped++;
            }
            return dropped;
        }

        private int FreeSlot()
        {
            for (var slot = 0; slot < MaxStuck; slot++)
            {
                var taken = false;
                foreach (var t in _things) if (t.State == MagnetThingState.Stuck && t.Slot == slot) taken = true;
                if (!taken) return slot;
            }
            return 0;
        }

        private static float MoveTowards(float value, float target, float maxDelta)
            => Math.Abs(target - value) <= maxDelta ? target : value + Math.Sign(target - value) * maxDelta;

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
