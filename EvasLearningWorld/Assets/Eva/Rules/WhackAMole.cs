using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // One mole that is currently up.
    public sealed class UpMole
    {
        public int Hole;
        public float SecondsLeft;
    }

    // The real-time rules of Whack-a-Mole (docs/kids-games/arcade-redesign.md): nine holes, moles pop up and hide again, the child
    // taps them. Every mole counts, there are no decoys and nothing is ever lost: a mole that is not whacked just hides. One director
    // per level; the screen plays levels 1-6 in a row, each needing more hits and running faster than the one before. Pure logic with
    // no clock of its own: the screen feeds Tick() the frame time and reports what happened, so the pace is testable.
    public sealed class WhackAMoleDirector
    {
        public const int Holes = 9;
        public const int MinLevel = 1;
        public const int MaxLevel = 6;

        // The whole game pays one coin at the end, however it went (user, 2026-10-02).
        public const int SessionCoins = 1;

        // Per level (index 1-6). Hits grow faster than the pace does, so a level always takes longer than the one before
        // (hits x spawn gap is roughly 10, 14, 17, 22, 25, 28 seconds).
        private static readonly int[] HitsByLevel = { 5, 8, 12, 18, 25, 35 };
        private static readonly int[] MaxUpByLevel = { 1, 2, 2, 3, 4, 5 };
        private static readonly float[] StaySecondsByLevel = { 3.0f, 2.6f, 2.2f, 1.9f, 1.6f, 1.3f };
        private static readonly float[] SpawnGapByLevel = { 2.0f, 1.7f, 1.4f, 1.2f, 1.0f, 0.8f };

        public static int HitsToPass(int level) => HitsByLevel[Index(level)];
        public static int MaxUp(int level) => MaxUpByLevel[Index(level)];
        public static float StaySeconds(int level) => StaySecondsByLevel[Index(level)];
        public static float SpawnGap(int level) => SpawnGapByLevel[Index(level)];

        private static int Index(int level)
        {
            if (level < MinLevel || level > MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            return level - MinLevel;
        }

        private readonly Random _rng;
        private readonly List<UpMole> _up = new List<UpMole>();
        private float _untilNextSpawn;

        public int Level { get; }
        public int Hits { get; private set; }
        public bool LevelDone => Hits >= HitsToPass(Level);
        public IReadOnlyList<UpMole> Up => _up;

        public WhackAMoleDirector(int level, Random rng)
        {
            Index(level);
            Level = level;
            _rng = rng;
            _untilNextSpawn = 0.4f; // the first mole comes almost at once
        }

        // Advances the clock. `spawned` lists moles that just came up, `expired` the ones whose time ran out (they hide on their own,
        // which is never a mistake).
        public void Tick(float seconds, List<UpMole> spawned, List<UpMole> expired)
        {
            for (var i = _up.Count - 1; i >= 0; i--)
            {
                _up[i].SecondsLeft -= seconds;
                if (_up[i].SecondsLeft > 0f) continue;
                expired?.Add(_up[i]);
                _up.RemoveAt(i);
            }
            if (LevelDone) return;

            _untilNextSpawn -= seconds;
            if (_untilNextSpawn > 0f || _up.Count >= MaxUp(Level)) return;
            var mole = Spawn();
            if (mole == null) return;
            spawned?.Add(mole);
            _untilNextSpawn = SpawnGap(Level);
        }

        private UpMole Spawn()
        {
            var free = new List<int>();
            for (var hole = 0; hole < Holes; hole++)
                if (HoleOf(hole) == null) free.Add(hole);
            if (free.Count == 0) return null;
            var mole = new UpMole { Hole = free[_rng.Next(free.Count)], SecondsLeft = StaySeconds(Level) };
            _up.Add(mole);
            return mole;
        }

        private UpMole HoleOf(int hole)
        {
            foreach (var up in _up) if (up.Hole == hole) return up;
            return null;
        }

        // The child taps a hole: a mole that is up is whacked (removed, counts a hit); an empty hole does nothing.
        public bool Whack(int hole)
        {
            var mole = HoleOf(hole);
            if (mole == null) return false;
            _up.Remove(mole);
            Hits++;
            return true;
        }
    }
}
