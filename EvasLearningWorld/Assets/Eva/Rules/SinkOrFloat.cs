using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // One thing on Sink or Float's shelves and how it behaves in water. Submerge (floaters): the share of its height under the surface
    // when it floats - a leaf lies on the water, a sponge sits low. SinkSpeed (sinkers): canvas units per second as it falls - a coin
    // flutters down, a hammer drops like a stone. Sway (sinkers): how far it drifts sideways while falling.
    public readonly struct SinkOrFloatItem
    {
        public SinkOrFloatItem(string id, bool floats, float submerge, float sinkSpeed, float sway)
        {
            Id = id; Floats = floats; Submerge = submerge; SinkSpeed = sinkSpeed; Sway = sway;
        }

        public string Id { get; }
        public bool Floats { get; }
        public float Submerge { get; }
        public float SinkSpeed { get; }
        public float Sway { get; }
    }

    // A session: two rounds. Each round puts six things on the shelves (three that float, three that sink, in a shuffled order); the
    // child drags them into the tank in any order and watches what the water does with each. The twelve things are split between
    // the two rounds, so a session shows every one of them once.
    public static class SinkOrFloat
    {
        public const int RoundsPerSession = 2;
        public const int ObjectsPerRound = 6;

        public static readonly IReadOnlyList<SinkOrFloatItem> Items = new[]
        {
            new SinkOrFloatItem("rock", false, 0f, 380f, 8f),
            new SinkOrFloatItem("leaf", true, 0.12f, 0f, 0f),
            new SinkOrFloatItem("key", false, 0f, 230f, 55f),
            new SinkOrFloatItem("balloon", true, 0.22f, 0f, 0f),
            new SinkOrFloatItem("coin", false, 0f, 200f, 75f),
            new SinkOrFloatItem("cork", true, 0.35f, 0f, 0f),
            new SinkOrFloatItem("spoon", false, 0f, 250f, 50f),
            new SinkOrFloatItem("sponge", true, 0.5f, 0f, 0f),
            new SinkOrFloatItem("marble", false, 0f, 330f, 12f),
            new SinkOrFloatItem("rubber_duck", true, 0.4f, 0f, 0f),
            new SinkOrFloatItem("hammer", false, 0f, 430f, 8f),
            new SinkOrFloatItem("apple", true, 0.45f, 0f, 0f),
        };

        public static SinkOrFloatItem Find(string id)
        {
            foreach (var item in Items) if (item.Id == id) return item;
            throw new ArgumentException("no such object: " + id);
        }

        // The ids for every round of a session: each round three floaters and three sinkers, no thing in two rounds, every order shuffled.
        public static string[][] CreateSession(Random rng)
        {
            var floaters = new List<string>();
            var sinkers = new List<string>();
            foreach (var item in Items) (item.Floats ? floaters : sinkers).Add(item.Id);
            Shuffle(floaters, rng);
            Shuffle(sinkers, rng);
            var perKind = ObjectsPerRound / 2;
            var rounds = new string[RoundsPerSession][];
            for (var r = 0; r < RoundsPerSession; r++)
            {
                var round = new List<string>();
                for (var k = 0; k < perKind; k++)
                {
                    round.Add(floaters[r * perKind + k]);
                    round.Add(sinkers[r * perKind + k]);
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
}
