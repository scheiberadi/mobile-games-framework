using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // Science Lab's Plant Growth: a garden. Each plant (a daisy, a tomato, ...: a PlantKind, each with its own number of growth stages)
    // starts as a seed and grows one stage at a time up to full size. Before each growth the plant asks, in a thought cloud, for one or more of three things: water, sun,
    // fertilizer (in a random mix), and for each of them a number of doses (shown as dots). A dose is one go: the child drags the thing onto
    // the plant and keeps it there a moment (DoseSeconds); the next dose needs the thing taken away and brought back. When everything the
    // plant asked for is given, it grows and asks again. Something it did not ask for just earns a gentle word from Eva (reported once per
    // visit). A round is over when every plant in the garden is in flower. A session is three rounds, each harder than the last: more plants
    // and more doses (Rounds). Nothing is a mistake.
    //
    // Pure logic with no clock of its own: the screen calls Step with the seconds that passed and which supplies are held over the plant.
    public enum GardenSupply { Water, Sun, Fertilizer }

    // A kind of plant: its picture name (Art/sciencelab/plant_<Id>_<stage>) and how many growth stages it has, the seed and the full plant included.
    public readonly struct PlantKind
    {
        public PlantKind(string id, int stages)
        {
            Id = id; Stages = stages;
        }

        public string Id { get; }
        public int Stages { get; }
    }

    public readonly struct GardenRoundSpec
    {
        public GardenRoundSpec(int plants, int minDoses, int maxDoses)
        {
            Plants = plants; MinDoses = minDoses; MaxDoses = maxDoses;
        }

        public int Plants { get; }
        public int MinDoses { get; }
        public int MaxDoses { get; }
    }

    public sealed class GardenPlant
    {
        public const int SupplyCount = 3;
        public const float DoseSeconds = 0.9f;
        public const float UnaskedSeconds = 1.0f;

        private readonly int[] _required = new int[SupplyCount];
        private readonly int[] _given = new int[SupplyCount];
        private readonly float[] _held = new float[SupplyCount];
        private readonly float[] _unasked = new float[SupplyCount];
        private readonly bool[] _locked = new bool[SupplyCount];
        private readonly bool[] _told = new bool[SupplyCount];
        private readonly Random _rng;
        private readonly GardenRoundSpec _spec;

        public GardenPlant(PlantKind kind, GardenRoundSpec spec, Random rng)
        {
            Kind = kind;
            _spec = spec;
            _rng = rng;
            AskForSomething();
        }

        public PlantKind Kind { get; }
        public int Stage { get; private set; }
        public int Stages => Kind.Stages;
        public bool Done => Stage >= Stages - 1;

        // Raised by the last Step.
        public readonly List<GardenSupply> JustDosed = new List<GardenSupply>();
        public readonly List<GardenSupply> JustUnasked = new List<GardenSupply>();
        public bool JustGrew { get; private set; }

        public bool Asks(GardenSupply supply) => !Done && _required[(int)supply] > 0;
        public int Required(GardenSupply supply) => Done ? 0 : _required[(int)supply];
        public int Given(GardenSupply supply) => _given[(int)supply];
        public float DoseProgress(GardenSupply supply) => Math.Min(1f, _held[(int)supply] / DoseSeconds);
        public bool Complete(GardenSupply supply) => !Asks(supply) || _given[(int)supply] >= _required[(int)supply];

        public void Step(float seconds, bool water, bool sun, bool fertilizer)
        {
            JustDosed.Clear();
            JustUnasked.Clear();
            JustGrew = false;
            if (Done) return;
            for (var s = 0; s < SupplyCount; s++)
            {
                var supplied = s == 0 ? water : s == 1 ? sun : fertilizer;
                if (!supplied)
                {
                    _held[s] = 0f;
                    _unasked[s] = 0f;
                    _locked[s] = false;
                    _told[s] = false;
                    continue;
                }
                if (_required[s] == 0)
                {
                    _unasked[s] += seconds;
                    if (!_told[s] && _unasked[s] >= UnaskedSeconds)
                    {
                        _told[s] = true;
                        JustUnasked.Add((GardenSupply)s);
                    }
                    continue;
                }
                if (_locked[s] || _given[s] >= _required[s]) continue;
                _held[s] += seconds;
                if (_held[s] >= DoseSeconds)
                {
                    _given[s]++;
                    _held[s] = 0f;
                    _locked[s] = true;
                    JustDosed.Add((GardenSupply)s);
                }
            }
            for (var s = 0; s < SupplyCount; s++) if (_given[s] < _required[s]) return;
            Stage++;
            JustGrew = true;
            if (!Done) AskForSomething();
            else Array.Clear(_required, 0, SupplyCount);
        }

        // A random non-empty mix of the three, each with its own number of doses.
        private void AskForSomething()
        {
            Array.Clear(_given, 0, SupplyCount);
            Array.Clear(_held, 0, SupplyCount);
            Array.Clear(_locked, 0, SupplyCount);
            var mask = 1 + _rng.Next(7);
            for (var s = 0; s < SupplyCount; s++)
                _required[s] = (mask & (1 << s)) != 0 ? _spec.MinDoses + _rng.Next(_spec.MaxDoses - _spec.MinDoses + 1) : 0;
        }
    }

    public sealed class PlantGarden
    {
        // The kinds of plant there are (each has its pictures). A round shows different kinds side by side while there are enough.
        public static readonly PlantKind[] Kinds =
        {
            new PlantKind("daisy", 5),
            new PlantKind("potato", 4),
            new PlantKind("tomato", 5),
            new PlantKind("cabbage", 4),
            new PlantKind("apple_tree", 5),
        };

        // Each round is harder: more plants, then more doses.
        public static readonly GardenRoundSpec[] Rounds =
        {
            new GardenRoundSpec(1, 1, 1),
            new GardenRoundSpec(2, 1, 2),
            new GardenRoundSpec(3, 2, 3),
        };

        public static int RoundsPerSession => Rounds.Length;

        private readonly List<GardenPlant> _plants = new List<GardenPlant>();

        public PlantGarden(int round, Random rng)
        {
            var spec = Rounds[round];
            var kinds = new List<PlantKind>(Kinds);
            for (var i = kinds.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var swap = kinds[i];
                kinds[i] = kinds[j];
                kinds[j] = swap;
            }
            for (var i = 0; i < spec.Plants; i++) _plants.Add(new GardenPlant(kinds[i % kinds.Count], spec, rng));
        }

        public IReadOnlyList<GardenPlant> Plants => _plants;

        public bool Done
        {
            get
            {
                foreach (var plant in _plants) if (!plant.Done) return false;
                return true;
            }
        }
    }
}
