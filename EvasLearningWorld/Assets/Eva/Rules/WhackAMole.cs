using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public enum WhackResult { Empty, Target, Decoy }

    // One mole that is currently up.
    public sealed class UpMole
    {
        public int Hole;
        public string MoleId;
        public bool IsTarget;
        public float SecondsLeft;
    }

    // The real-time rules of Whack-a-Mole (docs/kids-games/arcade-redesign.md): nine holes, moles pop up and hide again, the child
    // taps the ones that look like the target. Pure logic with no clock of its own: the screen feeds Tick() the frame time and
    // reports what happened, so the pace and the spawn rules are testable. There is no losing: a mole that is not whacked just
    // hides, a decoy that is tapped stays up and only counts as a wrong whack.
    public sealed class WhackAMoleDirector
    {
        public const int Holes = 9;
        public static readonly string[] MoleIds = { "a", "b", "c", "d", "e", "f" };

        private static readonly int[] MaxUpByLevel = { 1, 2, 2, 3, 4, 5 };
        private static readonly float[] StaySecondsByLevel = { 3.0f, 2.6f, 2.2f, 1.9f, 1.6f, 1.3f };
        private static readonly float[] SpawnGapByLevel = { 2.0f, 1.7f, 1.4f, 1.2f, 1.0f, 0.8f };
        private static readonly float[] DecoyChanceByLevel = { 0f, 0.25f, 0.4f, 0.5f, 0.55f, 0.6f };
        private static readonly int[] HitsPerRoundByLevel = { 4, 4, 5, 5, 6, 6 };

        public static int MaxUp(int level) => MaxUpByLevel[Index(level)];
        public static float StaySeconds(int level) => StaySecondsByLevel[Index(level)];
        public static float SpawnGap(int level) => SpawnGapByLevel[Index(level)];
        public static float DecoyChance(int level) => DecoyChanceByLevel[Index(level)];
        public static int HitsPerRound(int level) => HitsPerRoundByLevel[Index(level)];

        private static int Index(int level)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            return level - DifficultyLadder.MinLevel;
        }

        private readonly Random _rng;
        private readonly List<UpMole> _up = new List<UpMole>();
        private float _untilNextSpawn;

        public int Level { get; }
        public string TargetId { get; }
        public int Hits { get; private set; }
        public int WrongWhacks { get; private set; }
        public bool RoundDone => Hits >= HitsPerRound(Level);
        public IReadOnlyList<UpMole> Up => _up;

        public WhackAMoleDirector(int level, string targetId, Random rng)
        {
            Index(level);
            Level = level;
            TargetId = targetId;
            _rng = rng;
            _untilNextSpawn = 0.4f; // the first mole comes almost at once
        }

        // Advances the clock. `spawned` lists moles that just came up, `expired` the ones whose time ran out (they hide on their own).
        public void Tick(float seconds, List<UpMole> spawned, List<UpMole> expired)
        {
            for (var i = _up.Count - 1; i >= 0; i--)
            {
                _up[i].SecondsLeft -= seconds;
                if (_up[i].SecondsLeft > 0f) continue;
                expired?.Add(_up[i]);
                _up.RemoveAt(i);
            }
            if (RoundDone) return;

            // Decoys never hang around alone: when the last target is gone one is brought up at once (the decoy that was
            // about to hide anyway makes room if the board is full).
            if (_up.Count > 0 && !AnyTargetUp())
            {
                if (_up.Count >= MaxUp(Level))
                {
                    var leaving = _up[0];
                    foreach (var up in _up) if (up.SecondsLeft < leaving.SecondsLeft) leaving = up;
                    _up.Remove(leaving);
                    expired?.Add(leaving);
                }
                var target = Spawn();
                if (target != null) spawned?.Add(target);
            }

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

            // A target is always on the board; otherwise the level's decoy chance decides.
            var decoy = AnyTargetUp() && _rng.NextDouble() < DecoyChance(Level);
            var id = TargetId;
            if (decoy)
            {
                var others = new List<string>(MoleIds);
                others.Remove(TargetId);
                id = others[_rng.Next(others.Count)];
            }
            var mole = new UpMole { Hole = free[_rng.Next(free.Count)], MoleId = id, IsTarget = !decoy, SecondsLeft = StaySeconds(Level) };
            _up.Add(mole);
            return mole;
        }

        private bool AnyTargetUp()
        {
            foreach (var up in _up) if (up.IsTarget) return true;
            return false;
        }

        private UpMole HoleOf(int hole)
        {
            foreach (var up in _up) if (up.Hole == hole) return up;
            return null;
        }

        // The child taps a hole. A target is whacked (removed, counts a hit); a decoy stays and counts a wrong whack.
        public WhackResult Whack(int hole)
        {
            var mole = HoleOf(hole);
            if (mole == null) return WhackResult.Empty;
            if (!mole.IsTarget)
            {
                WrongWhacks++;
                return WhackResult.Decoy;
            }
            _up.Remove(mole);
            Hits++;
            return WhackResult.Target;
        }

        // Which target the next round uses: any mole but the previous one.
        public static string NextTarget(string previous, Random rng)
        {
            string id;
            do id = MoleIds[rng.Next(MoleIds.Length)]; while (id == previous);
            return id;
        }
    }
}
