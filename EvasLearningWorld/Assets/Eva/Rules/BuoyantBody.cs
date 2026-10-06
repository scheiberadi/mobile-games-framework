using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // One object in Sink or Float's tank, the way it behaves in real water. Dropped from the child's finger it falls through the air; on
    // reaching the water it either bobs up and rides the surface (a floater: a spring pulls it to its resting height, which follows the
    // ripples, and it tilts with them) or sinks (a sinker: it falls at its own speed, swaying and turning as it goes, then lands on the
    // floor, bounces a little and lies there). Coordinates are canvas units; Y is the centre of the object. Pure numbers, no Unity types.
    public sealed class BuoyantBody
    {
        public const float AirGravity = 2200f;
        public const float FloatSpring = 110f;     // pulls a floater to its resting height
        public const float FloatDamping = 6.5f;    // a floater bobs two or three times before it calms
        public const float RiseLimit = 900f;       // the most the water lifts a floater from below (it rises slowly, not like a rocket)
        public const float SinkDrag = 4f;
        public const float SettledVelocity = 18f;
        public const float WallMargin = 0.4f;      // how close to a glass wall the centre may come, as a share of the size

        public struct StepResult
        {
            public bool EnteredWater;   // this step the object touched the surface from above
            public float EntrySpeed;
            public bool HitFloor;       // this step a sinker first touched the floor
            public float FloorSpeed;
        }

        private readonly float _phase;
        private float _swayPhase;
        private float _age;
        private bool _touchedFloor;

        public BuoyantBody(bool floats, float size, float submerge, float sinkSpeed, float sway, float x, float y, int seed)
        {
            Floats = floats;
            Size = size;
            Submerge = submerge;
            SinkSpeed = sinkSpeed;
            Sway = sway;
            X = x;
            Y = y;
            _phase = (seed * 2.399f) % 6.2832f;
            _swayPhase = _phase;
            RestAngle = ((seed * 7) % 5 - 2) * 9f; // -18 .. +18 degrees, the way it lies on the floor
        }

        public bool Floats { get; }
        public float Size { get; }
        public float Submerge { get; }   // share of a floater's height under the surface
        public float SinkSpeed { get; }  // a sinker's steady falling speed
        public float Sway { get; }       // how far a sinker drifts sideways as it falls
        public float RestAngle { get; }

        public float X { get; set; }
        public float Y { get; set; }
        public float VX { get; set; }
        public float VY { get; set; }
        public float Angle { get; private set; }
        public bool InWater { get; private set; }
        public bool Settled { get; private set; }
        public bool Sinking => InWater && !Floats && !Settled;

        public StepResult Step(float seconds, WaterSurface water, float surfaceY, float floorY, float halfWidth)
        {
            var result = new StepResult();
            _age += seconds;
            var restY = floorY + Size * 0.42f;
            var waterY = surfaceY + water.HeightAt(X);
            var wall = halfWidth - Size * WallMargin;

            if (!InWater)
            {
                VY -= AirGravity * seconds;
                Y += VY * seconds;
                X += VX * seconds;
                VX *= (float)Math.Exp(-1.5f * seconds);
                Angle += (0f - Angle) * Math.Min(1f, 4f * seconds);
                if (Y - Size * 0.25f <= waterY)
                {
                    InWater = true;
                    result.EnteredWater = true;
                    result.EntrySpeed = Math.Max(0f, -VY);
                    VY *= Floats ? 0.5f : 0.6f;
                    VX *= 0.4f;
                }
            }
            else if (Floats)
            {
                var target = waterY + Size * (0.5f - Submerge);
                var lift = -FloatSpring * (Y - target);
                if (lift > RiseLimit) lift = RiseLimit;
                if (lift < -1500f) lift = -1500f;
                VY += (lift - FloatDamping * VY) * seconds;
                Y += VY * seconds;
                VX *= (float)Math.Exp(-3f * seconds);
                X += VX * seconds;
                var tilt = (float)Math.Atan(water.SlopeAt(X)) * 57.2958f * 1.4f + (float)Math.Sin(_age * 1.7f + _phase) * 2.5f + VY * 0.02f;
                Angle += (tilt - Angle) * Math.Min(1f, 7f * seconds);
                Settled = Math.Abs(VY) < SettledVelocity * 2f && Math.Abs(Y - target) < 6f;
            }
            else if (!Settled)
            {
                VY += (-SinkSpeed * SinkDrag - SinkDrag * VY) * seconds;
                Y += VY * seconds;
                _swayPhase += 2.4f * seconds;
                VX = Sway * (float)Math.Cos(_swayPhase);
                X += VX * seconds;
                Angle += ((float)Math.Sin(_swayPhase) * (8f + Sway * 0.3f) - Angle) * Math.Min(1f, 5f * seconds);
                if (Y <= restY)
                {
                    Y = restY;
                    if (!_touchedFloor)
                    {
                        _touchedFloor = true;
                        result.HitFloor = true;
                        result.FloorSpeed = -VY;
                    }
                    if (-VY > 90f) VY = -VY * 0.22f;
                    else { VY = 0f; VX = 0f; Settled = true; }
                }
            }
            else
            {
                VX = 0f;
                Angle += (RestAngle - Angle) * Math.Min(1f, 6f * seconds);
            }

            if (X < -wall) { X = -wall; VX = Math.Abs(VX); }
            if (X > wall) { X = wall; VX = -Math.Abs(VX); }
            return result;
        }

        // Keeps floaters (and, separately, the sinkers lying on the floor) from piling on one spot: each pair that is too close slides apart sideways.
        public static void Separate(IList<BuoyantBody> bodies, float halfWidth)
        {
            for (var pass = 0; pass < 2; pass++)
            {
                for (var i = 0; i < bodies.Count; i++)
                {
                    for (var j = i + 1; j < bodies.Count; j++)
                    {
                        var a = bodies[i];
                        var b = bodies[j];
                        if (!a.InWater || !b.InWater || a.Floats != b.Floats) continue;
                        if (!a.Floats && !(a.Settled && b.Settled)) continue;
                        var minimum = (a.Size + b.Size) * 0.4f;
                        var dx = b.X - a.X;
                        if (Math.Abs(dx) >= minimum) continue;
                        var direction = dx == 0f ? (j % 2 == 0 ? 1f : -1f) : Math.Sign(dx);
                        var push = (minimum - Math.Abs(dx)) * 0.25f;
                        a.X -= direction * push;
                        b.X += direction * push;
                        var wall = halfWidth - a.Size * WallMargin;
                        a.X = Math.Max(-wall, Math.Min(wall, a.X));
                        b.X = Math.Max(-wall, Math.Min(wall, b.X));
                    }
                }
            }
        }
    }
}
