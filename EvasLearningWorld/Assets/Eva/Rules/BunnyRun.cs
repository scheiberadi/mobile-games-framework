using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // Something lying on the trail that the bunny has to jump over. `Kind` 0-2 is low / medium / tall.
    public sealed class RunObstacle
    {
        public float X;
        public int Kind;
        public float Height;
        public float HalfWidth;
    }

    // A carrot (or golden star) in the world. `X` is the distance along the trail, `Y` the height of its middle above the ground.
    public sealed class RunCarrot
    {
        public float X;
        public float Y;
        public int Value = 1; // a golden star is worth 2
        public bool Star;
        public bool Taken;
    }

    public enum BunnyState { Running, Stumbling }

    [Flags]
    public enum BunnyEvents
    {
        None = 0,
        Jumped = 1,
        Landed = 2,
        Bumped = 4, // ran into an obstacle
        Recovered = 8, // back on its feet and running again
    }

    // The real-time rules of Bunny Run, Arcade's Platformer (docs/kids-games/arcade-redesign.md): the bunny hops along a trail while it scrolls
    // past, a tap makes it jump over the obstacles lying on it, carrots and golden stars are collected by touching them. Nothing is ever
    // lost: a bunny that bumps an obstacle stumbles, slides back to a run-up before it and the carrots it already took stay taken. One
    // director is the whole game: the screen calls StartLevel(1..6) in turn (each level is a world with its own scenery), the trail just goes
    // on. Pure logic with no clock of its own: the screen feeds Tick() the frame time and whether the child tapped.
    public sealed class BunnyRunDirector
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 6;

        // The whole game pays one coin at the end, however it went (user, 2026-10-02).
        public const int SessionCoins = 1;

        public const float JumpSeconds = 1.5f;
        public const float JumpApex = 300f; // how high the feet get
        public const float BunnyCenter = 100f; // the bunny's middle above its feet
        public const float BunnyHalfWidth = 25f; // the part of the bunny that can bump into something
        public const float StumbleSeconds = 0.9f;
        public const float RunUpSeconds = 1.0f; // after a bump the bunny is slid back to this much running before the obstacle

        // Low / medium / tall. The hit boxes are narrower than the pictures.
        private static readonly float[] ObstacleHeights = { 70f, 95f, 120f };
        private static readonly float[] ObstacleHalfWidths = { 42f, 48f, 40f };

        private const float CarrotReachX = 75f;
        private const float CarrotReachY = 115f;
        private const float ArcSpread = 110f; // the three carrots over an obstacle are this far apart
        private const float Ahead = 2600f; // the trail is built this far in front of the bunny
        private const float Behind = 1500f; // and forgotten this far behind it
        private const float FirstObstacle = 1000f;
        private const float WorldChangeAhead = 800f; // a new level's scenery starts this far in front of the bunny
        private const float CarrotSpacing = 230f;

        // Per level (index 1-6). Initial tuning values, to be judged on a device.
        private static readonly int[] HitsByLevel = { 18, 24, 30, 38, 46, 56 };
        private static readonly float[] SpeedByLevel = { 230f, 250f, 270f, 300f, 330f, 360f };
        private static readonly float[] SpacingMinByLevel = { 950f, 900f, 850f, 800f, 750f, 700f };
        private static readonly float[] SpacingMaxByLevel = { 1250f, 1200f, 1150f, 1100f, 1050f, 1000f };

        public static int HitsToPass(int level) => HitsByLevel[Index(level)];
        public static float Speed(int level) => SpeedByLevel[Index(level)];
        public static float JumpDistance(int level) => Speed(level) * JumpSeconds;
        public static float ObstacleHeight(int kind) => ObstacleHeights[kind];
        public static float ObstacleHalfWidth(int kind) => ObstacleHalfWidths[kind];
        public static int ObstacleKinds => ObstacleHeights.Length;

        // How long the bunny stays above an obstacle of this kind, in seconds, jumping at the best moment (the child's slack to tap is that
        // minus the time the bunny's width needs to pass).
        public static float SecondsAbove(int kind) => JumpSeconds * (float)Math.Sqrt(1f - ObstacleHeights[kind] / JumpApex);

        private static int Index(int level)
        {
            if (level < MinLevel || level > MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            return level - MinLevel;
        }

        private readonly Random _rng;
        private readonly List<RunObstacle> _obstacles = new List<RunObstacle>();
        private readonly List<RunCarrot> _carrots = new List<RunCarrot>();
        private float _edge; // where the last obstacle is
        private int _built; // obstacles built so far, to put a star over every third
        private float _jumpTime;
        private float _stumbleTime;
        private float _stumbleFrom;
        private float _stumbleTo;

        public int Level { get; private set; } = MinLevel;
        public int Hits { get; private set; }
        public int TotalHits { get; private set; }
        public float Scroll { get; private set; } // where the bunny is along the trail
        public BunnyState State { get; private set; } = BunnyState.Running;
        public bool Jumping { get; private set; }
        public IReadOnlyList<RunObstacle> Obstacles => _obstacles;
        public IReadOnlyList<RunCarrot> Carrots => _carrots;

        public bool LevelDone => Hits >= HitsToPass(Level);
        public float JumpProgress => Jumping ? _jumpTime / JumpSeconds : 0f;
        public float StumbleProgress => State == BunnyState.Stumbling ? Math.Min(1f, _stumbleTime / StumbleSeconds) : 0f;

        public float JumpHeight
        {
            get
            {
                if (!Jumping) return 0f;
                var u = _jumpTime / JumpSeconds;
                return 4f * JumpApex * u * (1f - u);
            }
        }

        public BunnyRunDirector(Random rng)
        {
            _rng = rng;
            _edge = Scroll;
            AddGroundCarrots(260f, FirstObstacle - 380f);
            AddObstacle(FirstObstacle);
            Extend();
        }

        // A new level (a new world) starts counting its own carrots. The trail goes on, but what was built far ahead for the old scenery
        // is rebuilt for the new one.
        public void StartLevel(int level)
        {
            Index(level);
            Level = level;
            Hits = 0;
            var limit = Scroll + WorldChangeAhead;
            _obstacles.RemoveAll(o => o.X > limit);
            _carrots.RemoveAll(c => c.X > limit);
            _edge = Math.Max(Scroll, _obstacles.Count > 0 ? _obstacles[_obstacles.Count - 1].X : Scroll);
            Extend();
        }

        // The first obstacle the bunny has not passed yet (null when none).
        public RunObstacle NextObstacle()
        {
            foreach (var o in _obstacles) if (o.X + o.HalfWidth + BunnyHalfWidth > Scroll) return o;
            return null;
        }

        // Advances the clock. `taken` lists the carrots touched this tick.
        public BunnyEvents Tick(float seconds, bool tap, List<RunCarrot> taken)
        {
            var events = BunnyEvents.None;
            if (State == BunnyState.Stumbling)
            {
                _stumbleTime += seconds;
                Scroll = _stumbleFrom + (_stumbleTo - _stumbleFrom) * Ease(StumbleProgress);
                if (_stumbleTime >= StumbleSeconds)
                {
                    State = BunnyState.Running;
                    Scroll = _stumbleTo;
                    events |= BunnyEvents.Recovered;
                }
                return events;
            }

            Scroll += Speed(Level) * seconds;

            if (Jumping)
            {
                _jumpTime += seconds;
                if (_jumpTime >= JumpSeconds)
                {
                    Jumping = false;
                    _jumpTime = 0f;
                    events |= BunnyEvents.Landed;
                }
            }
            else if (tap)
            {
                Jumping = true;
                _jumpTime = 0f;
                events |= BunnyEvents.Jumped;
            }

            var bumped = BumpedInto();
            if (bumped != null) return events | Stumble(bumped);

            TouchCarrots(taken);
            Extend();
            return events;
        }

        private RunObstacle BumpedInto()
        {
            var height = JumpHeight;
            foreach (var o in _obstacles)
                if (Math.Abs(o.X - Scroll) < o.HalfWidth + BunnyHalfWidth && height < o.Height) return o;
            return null;
        }

        private BunnyEvents Stumble(RunObstacle obstacle)
        {
            State = BunnyState.Stumbling;
            Jumping = false;
            _jumpTime = 0f;
            _stumbleTime = 0f;
            _stumbleFrom = Scroll;
            _stumbleTo = Math.Min(Scroll, obstacle.X - obstacle.HalfWidth - BunnyHalfWidth - Speed(Level) * RunUpSeconds);
            return BunnyEvents.Bumped;
        }

        private static float Ease(float k) => k * k * (3f - 2f * k);

        private void TouchCarrots(List<RunCarrot> taken)
        {
            var centre = JumpHeight + BunnyCenter;
            foreach (var carrot in _carrots)
            {
                if (carrot.Taken || Math.Abs(carrot.X - Scroll) > CarrotReachX || Math.Abs(carrot.Y - centre) > CarrotReachY) continue;
                carrot.Taken = true;
                Hits += carrot.Value;
                TotalHits += carrot.Value;
                taken?.Add(carrot);
            }
        }

        // Builds the trail ahead of the bunny with the current level's spacing, and forgets what is far behind.
        private void Extend()
        {
            while (_edge < Scroll + Ahead)
            {
                var index = Index(Level);
                var x = _edge + SpacingMinByLevel[index] + (float)_rng.NextDouble() * (SpacingMaxByLevel[index] - SpacingMinByLevel[index]);
                AddGroundCarrots(_edge + 260f, x - 380f);
                AddObstacle(x);
            }
            _obstacles.RemoveAll(o => o.X < Scroll - Behind);
            _carrots.RemoveAll(c => c.X < Scroll - Behind);
        }

        private void AddObstacle(float x)
        {
            var kind = _rng.Next(0, ObstacleKinds);
            _obstacles.Add(new RunObstacle { X = x, Kind = kind, Height = ObstacleHeights[kind], HalfWidth = ObstacleHalfWidths[kind] });
            _edge = x;
            _built++;
            // Three carrots (the middle one a golden star every third time) on the path of the best jump over it.
            var distance = JumpDistance(Level);
            for (var i = -1; i <= 1; i++)
            {
                var u = 0.5f + i * ArcSpread / distance;
                var star = i == 0 && _built % 3 == 0;
                _carrots.Add(new RunCarrot
                {
                    X = x + i * ArcSpread,
                    Y = BunnyCenter + 4f * JumpApex * u * (1f - u),
                    Star = star,
                    Value = star ? 2 : 1,
                });
            }
        }

        private void AddGroundCarrots(float from, float to)
        {
            for (var x = from; x < to; x += CarrotSpacing) _carrots.Add(new RunCarrot { X = x, Y = BunnyCenter });
        }
    }
}
