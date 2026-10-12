using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // Science Lab's Human Senses: "What do I use?". Three things sit on the table (a rainbow, a bell, a lemon...). The child drags one of
    // the five organs (eye, ear, nose, tongue, hand) onto a thing. The right organ is used: the thing is looked at, listened to, smelled,
    // tasted or felt, and it is done. A wrong organ is only "not that one" (the organ goes back). A round is done when all three things
    // are used; two wrong tries in a row call for a hint.
    //
    // Pure logic: no clock, no positions (the screen decides what "dropped on a thing" means).
    public enum Sense { Sight, Hearing, Smell, Taste, Touch }

    public enum SenseTry { Right, Wrong, AlreadyUsed }

    public static class Senses
    {
        public const int RoundsPerSession = 3;
        public const int ThingsPerRound = 3;
        public const int WrongsForHint = 2;

        // Two things for each sense.
        public static readonly IReadOnlyList<(string Id, Sense Sense)> Things = new[]
        {
            ("rainbow", Sense.Sight), ("butterfly", Sense.Sight),
            ("bell", Sense.Hearing), ("drum", Sense.Hearing),
            ("roses", Sense.Smell), ("skunk", Sense.Smell),
            ("lemon", Sense.Taste), ("strawberry", Sense.Taste),
            ("bunny", Sense.Touch), ("ice", Sense.Touch),
        };

        public static Sense SenseOf(string id)
        {
            foreach (var thing in Things) if (thing.Id == id) return thing.Sense;
            throw new ArgumentException("no such thing: " + id);
        }

        // The ids of every round of a session: each round three things for three different senses, no thing in two rounds. Each round takes the
        // senses with most things left (ties by chance), so the last round always still has three senses to choose from.
        public static string[][] CreateSession(Random rng)
        {
            var left = new Dictionary<Sense, List<string>>();
            foreach (var thing in Things)
            {
                if (!left.ContainsKey(thing.Sense)) left[thing.Sense] = new List<string>();
                left[thing.Sense].Add(thing.Id);
            }
            var rounds = new string[RoundsPerSession][];
            for (var r = 0; r < RoundsPerSession; r++)
            {
                var senses = new List<Sense>(left.Keys);
                Shuffle(senses, rng);
                senses.Sort((x, y) => left[y].Count.CompareTo(left[x].Count)); // stable enough: the shuffle above decides ties
                var round = new List<string>();
                for (var i = 0; i < ThingsPerRound; i++)
                {
                    var pool = left[senses[i]];
                    var pick = pool[rng.Next(pool.Count)];
                    pool.Remove(pick);
                    round.Add(pick);
                }
                Shuffle(round, rng);
                rounds[r] = round.ToArray();
            }
            return rounds;
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

    // One round: the things on the table and which are used up.
    public sealed class SensesRound
    {
        private readonly string[] _ids;
        private readonly bool[] _used;

        public SensesRound(IReadOnlyList<string> ids)
        {
            _ids = new string[ids.Count];
            for (var i = 0; i < ids.Count; i++) _ids[i] = ids[i];
            _used = new bool[ids.Count];
        }

        public int Count => _ids.Length;
        public string IdOf(int index) => _ids[index];
        public Sense SenseOf(int index) => Senses.SenseOf(_ids[index]);
        public bool IsUsed(int index) => _used[index];
        public int Wrongs { get; private set; }            // wrong tries in a row since the last right one
        public int TotalWrongs { get; private set; }
        public bool NeedsHint => Wrongs >= Senses.WrongsForHint;
        public bool Done { get { foreach (var used in _used) if (!used) return false; return true; } }

        // The first thing not used yet (what a hint points at), or -1.
        public int NextUnused()
        {
            for (var i = 0; i < _used.Length; i++) if (!_used[i]) return i;
            return -1;
        }

        public SenseTry Try(int index, Sense organ)
        {
            if (_used[index]) return SenseTry.AlreadyUsed;
            if (SenseOf(index) != organ)
            {
                Wrongs++;
                TotalWrongs++;
                return SenseTry.Wrong;
            }
            _used[index] = true;
            Wrongs = 0;
            return SenseTry.Right;
        }

        // The hint was shown: the count starts again.
        public void HintGiven() => Wrongs = 0;
    }
}
