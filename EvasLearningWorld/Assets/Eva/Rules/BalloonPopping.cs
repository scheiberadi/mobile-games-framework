using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // One balloon that is rising. `Look` is its colour (0-5); `Lane` is one of the vertical lanes it floats up in.
    public sealed class UpBalloon
    {
        public int Lane;
        public int Look;
        public float Age;
        public float RiseSeconds;
        public float Progress => Age / RiseSeconds; // 0 at the bottom of the screen, 1 when it has floated off the top
    }

    // The real-time rules of Balloon Popping (docs/kids-games/arcade-redesign.md): balloons rise slowly, the child pops them. Every
    // balloon counts and nothing is ever lost: a balloon that is not popped just floats off the top. One director per level; the
    // screen plays levels 1-6 in a row like Whack-a-Mole. Pure logic with no clock of its own: the screen feeds Tick() the frame time.
    public sealed class BalloonPoppingDirector
    {
        public const int Lanes = 6;
        public const int MinLevel = 1;
        public const int MaxLevel = 6;
        public const int Looks = 6;

        // The whole game pays one coin at the end, however it went (user, 2026-10-02).
        public const int SessionCoins = 1;

        // A new balloon only starts in a lane whose last balloon has already risen this far, so two never overlap.
        private const float LaneClearance = 0.28f;

        // Per level (index 1-6). Many balloons at once, more of them on every level. Initial tuning values, to be judged on a device.
        private static readonly int[] HitsByLevel = { 20, 30, 45, 65, 90, 120 };
        private static readonly int[] MaxUpByLevel = { 4, 6, 8, 10, 12, 14 };
        private static readonly float[] RiseSecondsByLevel = { 5f, 4.6f, 4.2f, 3.9f, 3.6f, 3.3f };
        private static readonly float[] SpawnGapByLevel = { 1.0f, 0.7f, 0.5f, 0.38f, 0.26f, 0.2f };

        public static int HitsToPass(int level) => HitsByLevel[Index(level)];
        public static int MaxUp(int level) => MaxUpByLevel[Index(level)];
        public static float RiseSeconds(int level) => RiseSecondsByLevel[Index(level)];
        public static float SpawnGap(int level) => SpawnGapByLevel[Index(level)];

        private static int Index(int level)
        {
            if (level < MinLevel || level > MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            return level - MinLevel;
        }

        private readonly Random _rng;
        private readonly List<UpBalloon> _up = new List<UpBalloon>();
        private float _untilNextSpawn;

        public int Level { get; }
        public int Hits { get; private set; }
        public bool LevelDone => Hits >= HitsToPass(Level);
        public IReadOnlyList<UpBalloon> Up => _up;

        public BalloonPoppingDirector(int level, Random rng)
        {
            Index(level);
            Level = level;
            _rng = rng;
            _untilNextSpawn = 0.4f; // the first balloon comes almost at once
        }

        // Advances the clock. `spawned` lists balloons that just started rising, `escaped` the ones that floated off the top
        // (never a mistake).
        public void Tick(float seconds, List<UpBalloon> spawned, List<UpBalloon> escaped)
        {
            for (var i = _up.Count - 1; i >= 0; i--)
            {
                _up[i].Age += seconds;
                if (_up[i].Progress < 1f) continue;
                escaped?.Add(_up[i]);
                _up.RemoveAt(i);
            }
            if (LevelDone) return;

            _untilNextSpawn -= seconds;
            if (_untilNextSpawn > 0f || _up.Count >= MaxUp(Level)) return;
            var balloon = Spawn();
            if (balloon == null) return; // no lane is clear yet: try again next tick
            spawned?.Add(balloon);
            _untilNextSpawn = SpawnGap(Level);
        }

        private UpBalloon Spawn()
        {
            var free = new List<int>();
            for (var lane = 0; lane < Lanes; lane++)
                if (LaneIsClear(lane)) free.Add(lane);
            if (free.Count == 0) return null;
            var balloon = new UpBalloon
            {
                Lane = free[_rng.Next(free.Count)],
                Look = _rng.Next(0, Looks),
                RiseSeconds = RiseSeconds(Level),
            };
            _up.Add(balloon);
            return balloon;
        }

        private bool LaneIsClear(int lane)
        {
            foreach (var up in _up) if (up.Lane == lane && up.Progress < LaneClearance) return false;
            return true;
        }

        // The child taps a balloon: it pops (removed, counts a hit).
        public bool Pop(UpBalloon balloon)
        {
            if (balloon == null || !_up.Remove(balloon)) return false;
            Hits++;
            return true;
        }
    }
}
