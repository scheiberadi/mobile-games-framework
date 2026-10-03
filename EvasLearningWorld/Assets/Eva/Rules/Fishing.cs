using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    public enum FishSize { Small = 0, Medium = 1, Large = 2 }

    // One fish swimming along a row of the lake. X/Y are canvas units (0,0 = middle of the screen, y up); every fish in a row swims the
    // same way. Art faces right; `Dir` -1 means mirrored.
    public sealed class SwimmingFish
    {
        public float X;
        public float Y;
        public float Dir;
        public float Speed;
        public FishSize Size;
        public int Look;
        public int Lane;
        public float Age;
        public bool Hooked; // on the hook, being pulled to the boat

        public float Width => FishingDirector.WidthOf(Size);
        public float Height => FishingDirector.HeightOf(Size);
        public int Points => FishingDirector.PointsOf(Size);
    }

    public enum HookState { Idle, Out, In }

    // The hook on its line: it goes from the rod tip to where the child tapped, then comes back; a fish it touches anywhere on the body
    // is hooked and pulled back with it.
    public sealed class FishingHook
    {
        public HookState State;
        public float X = FishingDirector.RodTipX;
        public float Y = FishingDirector.RodTipY;
        public float TargetX;
        public float TargetY;
        public SwimmingFish Fish;
    }

    // The real-time rules of Fishing (docs/kids-games/arcade-redesign.md): the child and Eva sit in a boat on a lake seen from the side,
    // fish of three sizes swim across in rows, a tap sends the hook there. A fish the hook touches on the way out or on the way back is
    // pulled into the boat and scores 1, 2 or 3 points by its size; a hook that touches nothing just comes back empty. Fish never stop,
    // so the child has to aim a little ahead. Nothing is ever lost. One director per level; the screen plays levels 1-6 in a row like the
    // other Arcade games. Pure logic with no clock of its own: the screen feeds Tick() the frame time.
    public sealed class FishingDirector
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 6;
        public const int Looks = 6;

        // The whole game pays one coin at the end, however it went (user, 2026-10-02).
        public const int SessionCoins = 1;

        // The lake in canvas units: the water starts at the surface; fish enter and leave beyond the sides of any screen.
        public const float SurfaceY = 195f;
        public const float FieldHalfWidth = 1000f;

        // The tip of the child's rod (where the line starts) and the boat the fish jump into.
        public const float RodTipX = 80f;
        public const float RodTipY = 300f;

        public const float HookSpeed = 1000f;
        public const float HookBackSpeed = 1200f;
        public const float HookBackWithFishSpeed = 900f;
        private const float Substep = 10f;

        // Rows from the surface down: shallow rows hold small fish, the deepest big ones. A row's fish all swim the same way at the same speed, so
        // they never run into each other; neighbouring rows go opposite ways.
        public static readonly float[] LaneY = { 105f, -5f, -120f, -235f, -350f };
        private static readonly float[] LaneDir = { 1f, -1f, 1f, -1f, 1f };
        private static readonly float[] LaneSpeed = { 1.15f, 1.0f, 0.9f, 0.85f, 0.75f };
        private static readonly FishSize[][] LaneSizes =
        {
            new[] { FishSize.Small },
            new[] { FishSize.Small, FishSize.Medium },
            new[] { FishSize.Medium },
            new[] { FishSize.Medium },
            new[] { FishSize.Large },
        };
        public static int Lanes => LaneY.Length;

        public static float WidthOf(FishSize size) => size == FishSize.Small ? 120f : size == FishSize.Medium ? 190f : 290f;
        public static float HeightOf(FishSize size) => WidthOf(size) * 0.70f; // the fish pictures are 1.43 times wider than high
        public static int PointsOf(FishSize size) => size == FishSize.Small ? 1 : size == FishSize.Medium ? 2 : 3;

        // Per level (index 1-6). Initial tuning values, to be judged on a device.
        private static readonly int[] PointsByLevel = { 8, 12, 18, 26, 36, 50 };
        private static readonly float[] SpeedByLevel = { 150f, 165f, 180f, 195f, 210f, 230f };
        private static readonly float[] SpawnGapByLevel = { 7f, 6.5f, 6f, 5.5f, 5f, 4.5f };
        private static readonly int[] LaneCapByLevel = { 2, 2, 3, 3, 3, 4 };

        public static int PointsToPass(int level) => PointsByLevel[Index(level)];
        public static float Speed(int level) => SpeedByLevel[Index(level)];
        public static float SpawnGap(int level) => SpawnGapByLevel[Index(level)];
        public static int LaneCap(int level) => LaneCapByLevel[Index(level)];

        private static int Index(int level)
        {
            if (level < MinLevel || level > MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            return level - MinLevel;
        }

        private readonly Random _rng;
        private readonly List<SwimmingFish> _fish = new List<SwimmingFish>();
        private readonly float[] _untilSpawn = new float[LaneY.Length];

        public int Level { get; }
        public int Points { get; private set; }
        public bool LevelDone => Points >= PointsToPass(Level);
        public IReadOnlyList<SwimmingFish> Fish => _fish;
        public FishingHook Hook { get; }

        // `carried` is the fish still swimming when the level before ended and `carriedHook` its hook: the next level takes them over so
        // the change of level is seamless. With nothing carried the lake starts with a few fish already swimming.
        public FishingDirector(int level, Random rng, IEnumerable<SwimmingFish> carried = null, FishingHook carriedHook = null)
        {
            Index(level);
            Level = level;
            _rng = rng;
            Hook = carriedHook ?? new FishingHook();
            if (carried != null) _fish.AddRange(carried);
            for (var lane = 0; lane < Lanes; lane++) _untilSpawn[lane] = SpawnGap(level) * (0.2f + (float)rng.NextDouble() * 0.8f);
            if (carried == null) Prefill();
        }

        // Sends the hook to (x, y) if it is back at the rod; a tap above the water sends it just under the surface.
        public bool Cast(float x, float y)
        {
            if (Hook.State != HookState.Idle || LevelDone) return false;
            Hook.TargetX = x;
            Hook.TargetY = Math.Min(y, SurfaceY - 40f);
            Hook.State = HookState.Out;
            return true;
        }

        // Advances the clock. `spawned` lists fish that just entered, `hooked` fish the hook just caught, `landed` fish that reached the
        // boat (their points are added), `gone` fish that swam out of the other side (never a mistake).
        public void Tick(float seconds, List<SwimmingFish> spawned, List<SwimmingFish> hooked, List<SwimmingFish> landed, List<SwimmingFish> gone)
        {
            for (var i = _fish.Count - 1; i >= 0; i--)
            {
                var fish = _fish[i];
                if (fish.Hooked) continue; // it follows the hook
                fish.Age += seconds;
                fish.X += fish.Dir * fish.Speed * seconds;
                if (Math.Abs(fish.X) > FieldHalfWidth + fish.Width * 0.5f + 20f && fish.X * fish.Dir > 0f)
                {
                    _fish.RemoveAt(i);
                    gone?.Add(fish);
                }
            }

            MoveHook(seconds, hooked, landed);

            if (LevelDone) return;
            for (var lane = 0; lane < Lanes; lane++)
            {
                _untilSpawn[lane] -= seconds;
                if (_untilSpawn[lane] > 0f) continue;
                var next = Spawn(lane, false);
                _untilSpawn[lane] = SpawnGap(Level) * (0.7f + (float)_rng.NextDouble() * 0.6f);
                if (next != null) spawned?.Add(next);
            }
        }

        private void MoveHook(float seconds, List<SwimmingFish> hooked, List<SwimmingFish> landed)
        {
            if (Hook.State == HookState.Idle) return;
            var speed = Hook.State == HookState.Out ? HookSpeed : Hook.Fish != null ? HookBackWithFishSpeed : HookBackSpeed;
            var remaining = speed * seconds;
            while (remaining > 0f && Hook.State != HookState.Idle)
            {
                var goalX = Hook.State == HookState.Out ? Hook.TargetX : RodTipX;
                var goalY = Hook.State == HookState.Out ? Hook.TargetY : RodTipY;
                var dx = goalX - Hook.X;
                var dy = goalY - Hook.Y;
                var distance = (float)Math.Sqrt(dx * dx + dy * dy);
                var step = Math.Min(Math.Min(remaining, Substep), distance);
                if (distance > 0.0001f)
                {
                    Hook.X += dx / distance * step;
                    Hook.Y += dy / distance * step;
                }
                remaining -= Math.Max(step, 0.01f);

                if (Hook.Fish == null)
                {
                    var touched = FishAtHook();
                    if (touched != null)
                    {
                        Hook.Fish = touched;
                        touched.Hooked = true;
                        Hook.State = HookState.In;
                        hooked?.Add(touched);
                        continue;
                    }
                }

                if (step >= distance - 0.0001f)
                {
                    if (Hook.State == HookState.Out) Hook.State = HookState.In;
                    else
                    {
                        Hook.State = HookState.Idle;
                        Hook.X = RodTipX;
                        Hook.Y = RodTipY;
                        if (Hook.Fish != null)
                        {
                            var fish = Hook.Fish;
                            Hook.Fish = null;
                            _fish.Remove(fish);
                            Points += fish.Points;
                            landed?.Add(fish);
                        }
                    }
                }
            }
            if (Hook.Fish != null)
            {
                Hook.Fish.X = Hook.X;
                Hook.Fish.Y = Hook.Y;
            }
        }

        // A fish is touched when the hook is inside an ellipse a little bigger than its body.
        private SwimmingFish FishAtHook()
        {
            SwimmingFish best = null;
            var bestScore = float.MaxValue;
            foreach (var fish in _fish)
            {
                if (fish.Hooked) continue;
                var nx = (Hook.X - fish.X) / (fish.Width * 0.5f + 10f);
                var ny = (Hook.Y - fish.Y) / (fish.Height * 0.5f + 12f);
                var score = nx * nx + ny * ny;
                if (score <= 1f && score < bestScore) { best = fish; bestScore = score; }
            }
            return best;
        }

        // The lake starts with a fish or two in every row.
        private void Prefill()
        {
            for (var lane = 0; lane < Lanes; lane++)
            {
                var count = _rng.Next(1, 3);
                for (var i = 0; i < count; i++) Spawn(lane, true);
            }
        }

        private SwimmingFish Spawn(int lane, bool anywhere)
        {
            var inLane = 0;
            foreach (var other in _fish) if (other.Lane == lane) inLane++;
            if (inLane >= LaneCap(Level)) return null;

            var sizes = LaneSizes[lane];
            var size = sizes[_rng.Next(0, sizes.Length)];
            var dir = LaneDir[lane];
            var width = WidthOf(size);
            var x = anywhere
                ? (float)(_rng.NextDouble() * 2.0 - 1.0) * (FieldHalfWidth - 100f)
                : -dir * (FieldHalfWidth + width * 0.5f + 10f);
            foreach (var other in _fish)
                if (other.Lane == lane && Math.Abs(other.X - x) < (other.Width + width) * 0.5f + 120f) return null; // no room in this row yet

            var fish = new SwimmingFish
            {
                X = x,
                Y = LaneY[lane],
                Dir = dir,
                Speed = Speed(Level) * LaneSpeed[lane],
                Size = size,
                Look = _rng.Next(0, Looks),
                Lane = lane,
            };
            _fish.Add(fish);
            return fish;
        }
    }
}
