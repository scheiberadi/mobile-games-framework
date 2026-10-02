using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // One fish in the pond. X/Y are canvas units (0,0 = middle of the screen); `Dir` is +1 swimming right, -1 left, `Look` which fish (0-5).
    public sealed class SwimmingFish
    {
        public float X;
        public float BaseY;
        public float Phase;
        public float Dir;
        public float Speed;
        public int Look;
        public float Age;
        public float LifeSeconds;

        // A gentle up and down wobble while it swims.
        public float Y => BaseY + (float)Math.Sin(Age * 2.2f + Phase) * 22f;

        // 0 just surfaced, 1 about to dive away.
        public float Progress => Age / LifeSeconds;
    }

    // The real-time rules of Fishing (docs/kids-games/arcade-redesign.md): fish swim to and fro in the pond, the child drags a hook with a
    // finger and a fish the hook touches is caught. Every fish counts, nothing is ever lost: a fish that is not caught just dives away.
    // One director per level; the screen plays levels 1-6 in a row like the other Arcade games. Pure logic with no clock of its own:
    // the screen feeds Tick() the frame time and where the hook is.
    public sealed class FishingDirector
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 6;
        public const int Looks = 6;

        // The whole game pays one coin at the end, however it went (user, 2026-10-02).
        public const int SessionCoins = 1;

        // The pond in canvas units: fish stay inside it.
        public const float PondLeft = -400f;
        public const float PondRight = 420f;
        public const float PondBottom = -280f;
        public const float PondTop = 150f;

        // A fish is caught when the hook's tip is this close to its middle.
        public const float CatchRadius = 120f;
        private const float MinSpacing = 240f;

        // Per level (index 1-6). Initial tuning values, to be judged on a device.
        private static readonly int[] HitsByLevel = { 5, 8, 12, 18, 25, 35 };
        private static readonly int[] MaxUpByLevel = { 2, 3, 3, 4, 5, 6 };
        private static readonly float[] LifeSecondsByLevel = { 9f, 8f, 7f, 6.2f, 5.5f, 5f };
        private static readonly float[] SpeedByLevel = { 70f, 90f, 110f, 135f, 160f, 190f };
        private static readonly float[] SpawnGapByLevel = { 1.4f, 1.1f, 0.9f, 0.75f, 0.6f, 0.5f };

        public static int HitsToPass(int level) => HitsByLevel[Index(level)];
        public static int MaxUp(int level) => MaxUpByLevel[Index(level)];
        public static float LifeSeconds(int level) => LifeSecondsByLevel[Index(level)];
        public static float Speed(int level) => SpeedByLevel[Index(level)];
        public static float SpawnGap(int level) => SpawnGapByLevel[Index(level)];

        private static int Index(int level)
        {
            if (level < MinLevel || level > MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            return level - MinLevel;
        }

        private readonly Random _rng;
        private readonly List<SwimmingFish> _up = new List<SwimmingFish>();
        private float _untilNextSpawn;

        public int Level { get; }
        public int Hits { get; private set; }
        public bool LevelDone => Hits >= HitsToPass(Level);
        public IReadOnlyList<SwimmingFish> Up => _up;

        // `carried` is the fish still swimming when the level before ended: the next level takes it over so the change of level is
        // seamless (it keeps swimming at the speed it started with).
        public FishingDirector(int level, Random rng, IEnumerable<SwimmingFish> carried = null)
        {
            Index(level);
            Level = level;
            _rng = rng;
            if (carried != null) _up.AddRange(carried);
            _untilNextSpawn = carried == null ? 0.4f : 0.1f; // the first fish comes almost at once
        }

        // Advances the clock. `hookActive` is true while a finger holds the hook in the water at (hookX, hookY). `spawned` lists fish
        // that just surfaced, `caught` what the hook caught this tick, `gone` what dived away on its own (never a mistake).
        public void Tick(float seconds, bool hookActive, float hookX, float hookY,
            List<SwimmingFish> spawned, List<SwimmingFish> caught, List<SwimmingFish> gone)
        {
            for (var i = _up.Count - 1; i >= 0; i--)
            {
                var fish = _up[i];
                fish.Age += seconds;
                fish.X += fish.Dir * fish.Speed * seconds;
                if (fish.X > PondRight) { fish.X = PondRight; fish.Dir = -1f; }
                else if (fish.X < PondLeft) { fish.X = PondLeft; fish.Dir = 1f; }

                if (hookActive && Distance(fish.X, fish.Y, hookX, hookY) <= CatchRadius)
                {
                    _up.RemoveAt(i);
                    Hits++;
                    caught?.Add(fish);
                }
                else if (fish.Progress >= 1f)
                {
                    _up.RemoveAt(i);
                    gone?.Add(fish);
                }
            }
            if (LevelDone) return;

            _untilNextSpawn -= seconds;
            if (_untilNextSpawn > 0f || _up.Count >= MaxUp(Level)) return;
            var next = Spawn();
            if (next == null) return; // no room in the pond yet: try again next tick
            spawned?.Add(next);
            _untilNextSpawn = SpawnGap(Level);
        }

        private static float Distance(float ax, float ay, float bx, float by)
        {
            var dx = ax - bx;
            var dy = ay - by;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private SwimmingFish Spawn()
        {
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var x = PondLeft + (float)_rng.NextDouble() * (PondRight - PondLeft);
                var y = PondBottom + (float)_rng.NextDouble() * (PondTop - PondBottom);
                if (!RoomAt(x, y)) continue;
                var fish = new SwimmingFish
                {
                    X = x,
                    BaseY = y,
                    Phase = (float)_rng.NextDouble() * 6.28f,
                    Dir = _rng.Next(0, 2) == 0 ? -1f : 1f,
                    Speed = Speed(Level) * (0.8f + 0.4f * (float)_rng.NextDouble()),
                    Look = _rng.Next(0, Looks),
                    LifeSeconds = LifeSeconds(Level),
                };
                _up.Add(fish);
                return fish;
            }
            return null;
        }

        private bool RoomAt(float x, float y)
        {
            foreach (var up in _up) if (Distance(up.X, up.Y, x, y) < MinSpacing) return false;
            return true;
        }
    }
}
