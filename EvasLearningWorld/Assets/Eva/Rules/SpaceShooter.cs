using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // One shape drifting down the sky. `Look` is which shape (0-5), `BaseX` the middle of its slow sideways sway.
    public sealed class SpaceShape
    {
        public float BaseX;
        public float Phase;
        public int Look;
        public float Age;
        public float FallSeconds;

        public float X => BaseX + (float)Math.Sin(Age * 1.3f + Phase) * 40f;
        public float Progress => Age / FallSeconds; // 0 above the top of the screen, 1 when it has drifted past the bottom
        public float Y => SpaceShooterDirector.ShapeStartY + (SpaceShooterDirector.ShapeEndY - SpaceShooterDirector.ShapeStartY) * Progress;
    }

    // One star the ship has fired; it flies straight up from where the ship was.
    public sealed class SpaceShot
    {
        public float X;
        public float Y;
    }

    // The real-time rules of Space Shooter, calm version for age 4-5 (docs/kids-games/arcade-redesign.md): the child slides the ship left
    // and right, it fires by itself, and a shape a shot touches pops. Every shape counts and nothing is ever lost: a shape that is not
    // hit just drifts away. One director per level; the screen plays levels 1-6 in a row like the other Arcade games. Pure logic with no
    // clock of its own: the screen feeds Tick() the frame time and where the ship is.
    public sealed class SpaceShooterDirector
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 6;
        public const int Looks = 6;

        // The whole game pays one coin at the end, however it went (user, 2026-10-02).
        public const int SessionCoins = 1;

        // Shapes drift from above the top to below the bottom; the ship sits near the bottom.
        public const float ShapeStartY = 500f;
        public const float ShapeEndY = -440f;
        public const float ShipY = -290f;
        public const float ShotSpeed = 1100f;
        public const float ShotTop = 520f;

        // A shot pops a shape whose middle is within this distance of it.
        public const float HitHalfWidth = 105f;
        public const float HitHalfHeight = 105f;

        // Shapes appear across the whole screen, never two close together near the top.
        public const float SpawnRange = 620f;
        private const float MinSpacing = 190f;
        private const float SpacingZone = 0.3f;

        // Per level (index 1-6). Initial tuning values, to be judged on a device.
        private static readonly int[] HitsByLevel = { 8, 12, 18, 26, 36, 50 };
        private static readonly int[] MaxUpByLevel = { 2, 3, 4, 5, 6, 8 };
        private static readonly float[] FallSecondsByLevel = { 7f, 6.3f, 5.6f, 5f, 4.5f, 4f };
        private static readonly float[] SpawnGapByLevel = { 1.3f, 1.0f, 0.8f, 0.65f, 0.5f, 0.4f };
        private static readonly float[] ShotGapByLevel = { 0.45f, 0.42f, 0.4f, 0.37f, 0.34f, 0.3f };

        public static int HitsToPass(int level) => HitsByLevel[Index(level)];
        public static int MaxUp(int level) => MaxUpByLevel[Index(level)];
        public static float FallSeconds(int level) => FallSecondsByLevel[Index(level)];
        public static float SpawnGap(int level) => SpawnGapByLevel[Index(level)];
        public static float ShotGap(int level) => ShotGapByLevel[Index(level)];

        private static int Index(int level)
        {
            if (level < MinLevel || level > MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            return level - MinLevel;
        }

        private readonly Random _rng;
        private readonly List<SpaceShape> _up = new List<SpaceShape>();
        private readonly List<SpaceShot> _shots = new List<SpaceShot>();
        private float _untilNextSpawn;
        private float _untilNextShot;

        public int Level { get; }
        public int Hits { get; private set; }
        public bool LevelDone => Hits >= HitsToPass(Level);
        public IReadOnlyList<SpaceShape> Up => _up;
        public IReadOnlyList<SpaceShot> Shots => _shots;

        // `carried` is the shapes still drifting when the level before ended, `carriedShots` the shots still flying: the next level takes
        // them over so the change of level is seamless.
        public SpaceShooterDirector(int level, Random rng, IEnumerable<SpaceShape> carried = null, IEnumerable<SpaceShot> carriedShots = null)
        {
            Index(level);
            Level = level;
            _rng = rng;
            if (carried != null) _up.AddRange(carried);
            if (carriedShots != null) _shots.AddRange(carriedShots);
            _untilNextSpawn = carried == null ? 0.4f : 0.1f; // the first shape comes almost at once
            _untilNextShot = 0.2f;
        }

        // Advances the clock. `fired` lists shots that just left the ship, `spawned` shapes that just appeared, `popped` what a shot hit
        // this tick, `gone` what drifted past the bottom (never a mistake), `spent` shots that flew off the top or hit something.
        public void Tick(float seconds, float shipX, List<SpaceShot> fired, List<SpaceShape> spawned, List<SpaceShape> popped,
            List<SpaceShape> gone, List<SpaceShot> spent)
        {
            for (var i = _up.Count - 1; i >= 0; i--)
            {
                var shape = _up[i];
                shape.Age += seconds;
                if (shape.Progress >= 1f)
                {
                    _up.RemoveAt(i);
                    gone?.Add(shape);
                }
            }

            for (var i = _shots.Count - 1; i >= 0; i--)
            {
                var shot = _shots[i];
                shot.Y += ShotSpeed * seconds;
                var hit = HitBy(shot);
                if (hit != null)
                {
                    _up.Remove(hit);
                    _shots.RemoveAt(i);
                    Hits++;
                    popped?.Add(hit);
                    spent?.Add(shot);
                }
                else if (shot.Y > ShotTop)
                {
                    _shots.RemoveAt(i);
                    spent?.Add(shot);
                }
            }

            if (LevelDone) return;

            _untilNextShot -= seconds;
            if (_untilNextShot <= 0f)
            {
                var shot = new SpaceShot { X = shipX, Y = ShipY + 80f };
                _shots.Add(shot);
                fired?.Add(shot);
                _untilNextShot = ShotGap(Level);
            }

            _untilNextSpawn -= seconds;
            if (_untilNextSpawn > 0f || _up.Count >= MaxUp(Level)) return;
            var next = Spawn();
            if (next == null) return; // no room near the top yet: try again next tick
            spawned?.Add(next);
            _untilNextSpawn = SpawnGap(Level);
        }

        private SpaceShape HitBy(SpaceShot shot)
        {
            SpaceShape best = null;
            var bestDistance = float.MaxValue;
            foreach (var shape in _up)
            {
                var dx = Math.Abs(shape.X - shot.X);
                var dy = Math.Abs(shape.Y - shot.Y);
                if (dx > HitHalfWidth || dy > HitHalfHeight) continue;
                var distance = dx + dy;
                if (distance < bestDistance) { best = shape; bestDistance = distance; }
            }
            return best;
        }

        private SpaceShape Spawn()
        {
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var x = (float)(_rng.NextDouble() * 2.0 - 1.0) * SpawnRange;
                if (!RoomAt(x)) continue;
                var shape = new SpaceShape
                {
                    BaseX = x,
                    Phase = (float)_rng.NextDouble() * 6.28f,
                    Look = _rng.Next(0, Looks),
                    FallSeconds = FallSeconds(Level),
                };
                _up.Add(shape);
                return shape;
            }
            return null;
        }

        private bool RoomAt(float x)
        {
            foreach (var up in _up) if (up.Progress < SpacingZone && Math.Abs(up.BaseX - x) < MinSpacing) return false;
            return true;
        }
    }
}
