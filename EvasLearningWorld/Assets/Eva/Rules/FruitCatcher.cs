using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // One piece of fruit that is falling. `X` is where it falls (canvas units, 0 = middle of the screen), `Look` which fruit (0-5).
    public sealed class FallingFruit
    {
        public float X;
        public int Look;
        public float Age;
        public float FallSeconds;
        public float Progress => Age / FallSeconds; // 0 above the top of the screen, 1 when it has fallen past the basket
    }

    // The real-time rules of Fruit Catcher (docs/kids-games/arcade-redesign.md): fruit falls, the child slides a basket left and right
    // to catch it. Every fruit counts and nothing is ever lost: a fruit that is not caught just falls away. One director per level;
    // the screen plays levels 1-6 in a row like the other Arcade games. Pure logic with no clock of its own: the screen feeds Tick()
    // the frame time and where the basket is.
    public sealed class FruitCatcherDirector
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 6;
        public const int Looks = 6;

        // The whole game pays one coin at the end, however it went (user, 2026-10-02).
        public const int SessionCoins = 1;

        // Fruit is caught when it passes the basket's mouth (this share of its fall) within this distance of the basket's middle.
        public const float CatchAt = 0.82f;
        public const float CatchHalfWidth = 175f;

        // Fruit falls in this range across the whole screen, never two close together near the top.
        public const float SpawnRange = 620f;
        private const float MinSpacing = 170f;
        private const float SpacingZone = 0.3f;

        // Per level (index 1-6). Initial tuning values, to be judged on a device.
        private static readonly int[] HitsByLevel = { 10, 15, 22, 30, 40, 50 };
        private static readonly int[] MaxUpByLevel = { 3, 4, 5, 6, 8, 10 };
        private static readonly float[] FallSecondsByLevel = { 4.5f, 4.1f, 3.8f, 3.5f, 3.2f, 3.0f };
        private static readonly float[] SpawnGapByLevel = { 1.0f, 0.8f, 0.65f, 0.5f, 0.38f, 0.28f };

        public static int HitsToPass(int level) => HitsByLevel[Index(level)];
        public static int MaxUp(int level) => MaxUpByLevel[Index(level)];
        public static float FallSeconds(int level) => FallSecondsByLevel[Index(level)];
        public static float SpawnGap(int level) => SpawnGapByLevel[Index(level)];

        private static int Index(int level)
        {
            if (level < MinLevel || level > MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            return level - MinLevel;
        }

        private readonly Random _rng;
        private readonly List<FallingFruit> _up = new List<FallingFruit>();
        private float _untilNextSpawn;

        public int Level { get; }
        public int Hits { get; private set; }
        public bool LevelDone => Hits >= HitsToPass(Level);
        public IReadOnlyList<FallingFruit> Up => _up;

        // `carried` is the fruit still falling when the level before ended: the next level takes it over so the change of level is
        // seamless (it keeps falling at the speed it started with).
        public FruitCatcherDirector(int level, Random rng, IEnumerable<FallingFruit> carried = null)
        {
            Index(level);
            Level = level;
            _rng = rng;
            if (carried != null) _up.AddRange(carried);
            _untilNextSpawn = carried == null ? 0.4f : 0.1f; // the first fruit comes almost at once
        }

        // Advances the clock. `spawned` lists fruit that just started falling, `caught` what the basket caught this tick, `gone`
        // what fell past it (never a mistake).
        public void Tick(float seconds, float basketX, List<FallingFruit> spawned, List<FallingFruit> caught, List<FallingFruit> gone)
        {
            for (var i = _up.Count - 1; i >= 0; i--)
            {
                var fruit = _up[i];
                var before = fruit.Progress;
                fruit.Age += seconds;
                if (before < CatchAt && fruit.Progress >= CatchAt && Math.Abs(fruit.X - basketX) <= CatchHalfWidth)
                {
                    _up.RemoveAt(i);
                    Hits++;
                    caught?.Add(fruit);
                }
                else if (fruit.Progress >= 1f)
                {
                    _up.RemoveAt(i);
                    gone?.Add(fruit);
                }
            }
            if (LevelDone) return;

            _untilNextSpawn -= seconds;
            if (_untilNextSpawn > 0f || _up.Count >= MaxUp(Level)) return;
            var next = Spawn();
            if (next == null) return; // no room near the top yet: try again next tick
            spawned?.Add(next);
            _untilNextSpawn = SpawnGap(Level);
        }

        private FallingFruit Spawn()
        {
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var x = (float)(_rng.NextDouble() * 2.0 - 1.0) * SpawnRange;
                if (!RoomAt(x)) continue;
                var fruit = new FallingFruit { X = x, Look = _rng.Next(0, Looks), FallSeconds = FallSeconds(Level) };
                _up.Add(fruit);
                return fruit;
            }
            return null;
        }

        private bool RoomAt(float x)
        {
            foreach (var up in _up) if (up.Progress < SpacingZone && Math.Abs(up.X - x) < MinSpacing) return false;
            return true;
        }
    }
}
