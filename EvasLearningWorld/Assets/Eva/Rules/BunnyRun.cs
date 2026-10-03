using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // A block of ground the bunny can run on. Between two blocks lies a river.
    public sealed class RunLand
    {
        public float Start;
        public float End;
    }

    // A carrot in the world. `X` is the distance along the track, `Y` the height of its middle above the ground.
    public sealed class RunCarrot
    {
        public float X;
        public float Y;
        public bool Taken;
    }

    public enum BunnyState { Running, Swimming }

    [Flags]
    public enum BunnyEvents
    {
        None = 0,
        Jumped = 1,
        Landed = 2,
        Splashed = 4, // fell into a river
        Rescued = 8, // set back on the bank, running again
    }

    // The real-time rules of Bunny Run, Arcade's Platformer (docs/kids-games/arcade-redesign.md): the bunny runs on the spot while the track
    // scrolls under it, a tap makes it jump over the rivers, carrots are collected by touching them. Nothing is ever lost: a bunny that falls
    // in swims a moment, a lily pad carries it back to the bank before the river, and the carrots it already took stay taken. One director
    // is the whole game: the screen calls StartLevel(1..6) in turn, the track just goes on. Pure logic with no clock of its own: the screen
    // feeds Tick() the frame time and whether the child tapped.
    public sealed class BunnyRunDirector
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 6;

        // The whole game pays one coin at the end, however it went (user, 2026-10-02).
        public const int SessionCoins = 1;

        public const float JumpSeconds = 1.3f;
        public const float JumpApex = 240f; // how high the feet get
        public const float BunnyCenter = 100f; // the bunny's middle above its feet
        public const float EdgeGrace = 15f; // the feet may overhang a bank by this much and still stand
        public const float CoyoteSeconds = 0.15f; // a tap this soon after running off a bank still jumps
        public const float SwimSeconds = 0.7f; // splashing about, then the lily pad carries the bunny back
        public const float CarrySeconds = 0.9f;
        public const float BackFromEdge = 170f; // where the lily pad puts the bunny down, before the river

        private const float CarrotReachX = 75f;
        private const float CarrotReachY = 115f;
        private const float Ahead = 2600f; // the track is built this far in front of the bunny
        private const float Behind = 2200f; // and forgotten this far behind it
        private const float FirstEdge = 1100f; // the first river is this far away

        // Per level (index 1-6). Initial tuning values, to be judged on a device.
        private static readonly int[] HitsByLevel = { 18, 24, 30, 38, 46, 56 };
        private static readonly float[] SpeedByLevel = { 230f, 250f, 270f, 300f, 330f, 360f };
        private static readonly float[] GapMinByLevel = { 120f, 130f, 140f, 150f, 160f, 170f };
        private static readonly float[] GapMaxByLevel = { 150f, 165f, 180f, 195f, 210f, 225f };
        private static readonly float[] LandMinByLevel = { 650f, 620f, 580f, 550f, 520f, 500f };
        private static readonly float[] LandMaxByLevel = { 900f, 850f, 800f, 750f, 700f, 650f };
        private const float CarrotSpacing = 230f;

        public static int HitsToPass(int level) => HitsByLevel[Index(level)];
        public static float Speed(int level) => SpeedByLevel[Index(level)];
        public static float JumpDistance(int level) => Speed(level) * JumpSeconds;
        public static float GapMin(int level) => GapMinByLevel[Index(level)];
        public static float GapMax(int level) => GapMaxByLevel[Index(level)];

        private static int Index(int level)
        {
            if (level < MinLevel || level > MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            return level - MinLevel;
        }

        private readonly Random _rng;
        private readonly List<RunLand> _lands = new List<RunLand>();
        private readonly List<RunCarrot> _carrots = new List<RunCarrot>();
        private float _jumpTime;
        private float _offBank; // seconds spent past the edge of a bank without having jumped
        private float _swimTime;
        private float _carryFrom;
        private float _carryTo;

        public int Level { get; private set; } = MinLevel;
        public int Hits { get; private set; }
        public int TotalHits { get; private set; }
        public float Scroll { get; private set; } // where the bunny is along the track
        public BunnyState State { get; private set; } = BunnyState.Running;
        public bool Jumping { get; private set; }
        public IReadOnlyList<RunLand> Lands => _lands;
        public IReadOnlyList<RunCarrot> Carrots => _carrots;

        public bool LevelDone => Hits >= HitsToPass(Level);

        // 0 while splashing about, then 0 -> 1 while the lily pad carries the bunny back.
        public float CarryProgress => State == BunnyState.Swimming && _swimTime > SwimSeconds ? Math.Min(1f, (_swimTime - SwimSeconds) / CarrySeconds) : 0f;

        public float JumpProgress => Jumping ? _jumpTime / JumpSeconds : 0f;

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
            _lands.Add(new RunLand { Start = -3000f, End = FirstEdge });
            AddCarrotsOnLand(_lands[0], 300f);
            Extend();
        }

        // A new level starts counting its own carrots; the track and the bunny go on as they are.
        public void StartLevel(int level)
        {
            Index(level);
            Level = level;
            Hits = 0;
        }

        public bool OnLand(float x)
        {
            foreach (var land in _lands) if (x >= land.Start - EdgeGrace && x <= land.End + EdgeGrace) return true;
            return false;
        }

        // The first river still ahead of the bunny (null when none is built yet): a pair of its two banks.
        public bool NextRiver(out float start, out float end)
        {
            start = end = 0f;
            for (var i = 0; i < _lands.Count - 1; i++)
            {
                if (_lands[i].End <= Scroll - EdgeGrace) continue;
                start = _lands[i].End;
                end = _lands[i + 1].Start;
                return true;
            }
            return false;
        }

        // Advances the clock. `taken` lists the carrots touched this tick.
        public BunnyEvents Tick(float seconds, bool tap, List<RunCarrot> taken)
        {
            var events = BunnyEvents.None;
            if (State == BunnyState.Swimming)
            {
                _swimTime += seconds;
                if (_swimTime > SwimSeconds) Scroll = _carryFrom + (_carryTo - _carryFrom) * Ease(CarryProgress);
                if (_swimTime >= SwimSeconds + CarrySeconds)
                {
                    State = BunnyState.Running;
                    Scroll = _carryTo;
                    _offBank = 0f;
                    events |= BunnyEvents.Rescued;
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
                    if (!OnLand(Scroll)) return events | Splash();
                }
            }
            else
            {
                if (OnLand(Scroll)) _offBank = 0f;
                else _offBank += seconds;
                if (tap && _offBank <= CoyoteSeconds)
                {
                    Jumping = true;
                    _jumpTime = 0f;
                    events |= BunnyEvents.Jumped;
                }
                else if (_offBank > CoyoteSeconds) return events | Splash();
            }

            TouchCarrots(taken);
            Extend();
            return events;
        }

        private BunnyEvents Splash()
        {
            State = BunnyState.Swimming;
            Jumping = false;
            _swimTime = 0f;
            _carryFrom = Scroll;
            // Back on the bank before the river the bunny fell into.
            RunLand bank = null;
            foreach (var land in _lands) if (land.Start <= Scroll && (bank == null || land.Start > bank.Start)) bank = land;
            _carryTo = bank != null ? Math.Max(bank.Start + 60f, bank.End - BackFromEdge) : Scroll;
            return BunnyEvents.Splashed;
        }

        private static float Ease(float k) => k * k * (3f - 2f * k);

        private void TouchCarrots(List<RunCarrot> taken)
        {
            var centre = JumpHeight + BunnyCenter;
            foreach (var carrot in _carrots)
            {
                if (carrot.Taken || Math.Abs(carrot.X - Scroll) > CarrotReachX || Math.Abs(carrot.Y - centre) > CarrotReachY) continue;
                carrot.Taken = true;
                Hits++;
                TotalHits++;
                taken?.Add(carrot);
            }
        }

        // Builds the track ahead of the bunny with the current level's rivers, and forgets what is far behind.
        private void Extend()
        {
            while (_lands[_lands.Count - 1].End < Scroll + Ahead)
            {
                var last = _lands[_lands.Count - 1];
                var gap = Between(GapMin(Level), GapMax(Level));
                var land = new RunLand { Start = last.End + gap, End = last.End + gap + Between(LandMinByLevel[Index(Level)], LandMaxByLevel[Index(Level)]) };
                _lands.Add(land);
                AddCarrotsOverGap(last.End, land.Start);
                AddCarrotsOnLand(land, 260f);
            }
            while (_lands.Count > 3 && _lands[1].End < Scroll - Behind) _lands.RemoveAt(0);
            _carrots.RemoveAll(c => c.X < Scroll - Behind);
        }

        private float Between(float low, float high) => low + (float)_rng.NextDouble() * (high - low);

        private void AddCarrotsOnLand(RunLand land, float from)
        {
            for (var x = land.Start + from; x < land.End - 220f; x += CarrotSpacing)
                _carrots.Add(new RunCarrot { X = x, Y = BunnyCenter });
        }

        // An arc of three over the river, where a jumping bunny passes.
        private void AddCarrotsOverGap(float gapStart, float gapEnd)
        {
            var middle = (gapStart + gapEnd) * 0.5f;
            _carrots.Add(new RunCarrot { X = middle - 70f, Y = 170f });
            _carrots.Add(new RunCarrot { X = middle, Y = 230f });
            _carrots.Add(new RunCarrot { X = middle + 70f, Y = 170f });
        }
    }
}
