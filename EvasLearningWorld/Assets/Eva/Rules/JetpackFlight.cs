using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // One pair of cloud pillars with the gap Eva flies through. `X` is the middle of the pillars (canvas units), `GapY` the middle of the gap.
    public sealed class JetpackPillar
    {
        public float X;
        public float GapY;
        public float GapHeight;
        public bool Passed;
        public bool Bumped; // Eva touched a pillar while crossing it
    }

    // The real-time rules of Jetpack Cat (Arcade, a Flappy-Bird-like game): Eva flies sideways with a jetpack, the finger held on the screen
    // lifts her and letting go lets her sink, and she flies through the gaps between cloud pillars. Nothing is ever lost: a pillar she
    // touches only holds her inside the gap while she crosses it (the screen shows a bump and no star), the pillar still counts as passed.
    // The screen plays levels 1-6 in a row like the other Arcade games. Pure logic with no clock of its own: the screen feeds Tick()
    // the frame time and whether the finger is down.
    public sealed class JetpackDirector
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 6;

        // The whole game pays one coin at the end, however it went.
        public const int SessionCoins = 1;

        public const float CatX = -300f;
        public const float CatHalfHeight = 48f;  // how far her body reaches above and below her middle for touching a pillar
        public const float CatHalfLength = 70f;  // and how far along (nose and tail are left out: it should feel forgiving)
        public const float PillarHalfWidth = 62f;
        public const float CeilingY = 300f;
        public const float FloorY = -330f;
        public const float RiseSpeed = 360f;     // units per second while the finger is down
        public const float FallSpeed = 320f;     // and while it is up
        private const float Acceleration = 2400f; // how fast the vertical speed follows, so she glides instead of jerking

        // Pillars come in from beyond the right edge of any phone (canvas half width is about 1000) and leave beyond the left one.
        public const float SpawnX = 1120f;
        public const float DespawnX = -1120f;
        private const float FirstPillarX = 760f;
        private const float MaxGapStep = 200f;     // how far a gap's middle may be from the one before: always reachable
        private const float GapRange = 100f;       // the gap's middle stays within this of the screen's middle: pillars from the top and from the bottom stay about as long, varying a little

        // Per level (index 1-6). Initial tuning values, to be judged on a device.
        private static readonly int[] PillarsByLevel = { 3, 4, 5, 6, 7, 8 };
        private static readonly float[] SpeedByLevel = { 230f, 245f, 260f, 275f, 290f, 305f };
        private static readonly float[] GapByLevel = { 500f, 470f, 440f, 420f, 400f, 380f };
        private static readonly float[] SpacingByLevel = { 700f, 700f, 690f, 680f, 670f, 660f };

        public static int PillarsToPass(int level) => PillarsByLevel[Index(level)];
        public static float Speed(int level) => SpeedByLevel[Index(level)];
        public static float GapHeight(int level) => GapByLevel[Index(level)];
        public static float Spacing(int level) => SpacingByLevel[Index(level)];

        private static int Index(int level)
        {
            if (level < MinLevel || level > MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            return level - MinLevel;
        }

        private readonly Random _rng;
        private readonly List<JetpackPillar> _pillars = new List<JetpackPillar>();
        private float _lastGapY;

        public int Level { get; private set; }
        public int PassedInLevel { get; private set; }
        public int TotalPassed { get; private set; }
        public float CatY { get; private set; }
        public float VelocityY { get; private set; }
        // True until the child first touches the screen: Eva hovers and nothing moves, so a voice line or a slow start costs nothing.
        public bool Hovering { get; set; } = true;
        public bool LevelDone => PassedInLevel >= PillarsToPass(Level);
        public IReadOnlyList<JetpackPillar> Pillars => _pillars;

        public JetpackDirector(Random rng)
        {
            _rng = rng;
            SetLevel(MinLevel);
        }

        // The next level: the pillars already on screen stay and keep going, so the change of level is seamless.
        public void SetLevel(int level)
        {
            Index(level);
            Level = level;
            PassedInLevel = 0;
        }

        // `passed` lists the pillars Eva just finished crossing (check Bumped), `bumped` the ones she just touched.
        public void Tick(float seconds, bool holding, List<JetpackPillar> passed, List<JetpackPillar> bumped)
        {
            if (Hovering) { VelocityY = 0f; return; }
            var target = holding ? RiseSpeed : -FallSpeed;
            VelocityY = Move(VelocityY, target, Acceleration * seconds);
            CatY += VelocityY * seconds;
            if (CatY > CeilingY) { CatY = CeilingY; if (VelocityY > 0f) VelocityY = 0f; }
            if (CatY < FloorY) { CatY = FloorY; if (VelocityY < 0f) VelocityY = 0f; }

            var step = Speed(Level) * seconds;
            for (var i = _pillars.Count - 1; i >= 0; i--)
            {
                var pillar = _pillars[i];
                pillar.X -= step;
                if (Math.Abs(pillar.X - CatX) < PillarHalfWidth + CatHalfLength) Hold(pillar, bumped);
                if (!pillar.Passed && pillar.X + PillarHalfWidth < CatX - CatHalfLength)
                {
                    pillar.Passed = true;
                    PassedInLevel++;
                    TotalPassed++;
                    passed?.Add(pillar);
                }
                if (pillar.X < DespawnX) _pillars.RemoveAt(i);
            }

            if (LevelDone) return;
            if (_pillars.Count == 0) _pillars.Add(NewPillar(FirstPillarX));
            else if (_pillars[_pillars.Count - 1].X <= SpawnX - Spacing(Level)) _pillars.Add(NewPillar(SpawnX));
        }

        // While Eva is level with a pillar she cannot leave its gap: she slides along the edge instead (a bump, never a loss).
        private void Hold(JetpackPillar pillar, List<JetpackPillar> bumped)
        {
            var low = pillar.GapY - pillar.GapHeight * 0.5f + CatHalfHeight;
            var high = pillar.GapY + pillar.GapHeight * 0.5f - CatHalfHeight;
            if (CatY >= low && CatY <= high) return;
            CatY = CatY < low ? low : high;
            VelocityY = 0f;
            if (pillar.Bumped) return;
            pillar.Bumped = true;
            bumped?.Add(pillar);
        }

        private JetpackPillar NewPillar(float x)
        {
            // Anywhere in the allowed band, but never farther from the gap before than she can climb or sink in time.
            var wanted = ((float)_rng.NextDouble() * 2f - 1f) * GapRange;
            var gapY = Math.Max(_lastGapY - MaxGapStep, Math.Min(_lastGapY + MaxGapStep, wanted));
            _lastGapY = gapY;
            return new JetpackPillar { X = x, GapY = gapY, GapHeight = GapHeight(Level) };
        }

        private static float Move(float from, float to, float maxDelta)
        {
            if (Math.Abs(to - from) <= maxDelta) return to;
            return from + Math.Sign(to - from) * maxDelta;
        }
    }
}
