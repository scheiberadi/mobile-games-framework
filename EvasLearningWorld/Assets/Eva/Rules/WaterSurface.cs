using System;

namespace EvasLearningWorld.Rules
{
    // The top edge of the water in Sink or Float's tank: a row of columns that move up and down like a stretched sheet (a 1-D wave
    // equation), so something dropped in sends a ring of ripples out to both glass walls, they bounce back and die away. On top of the
    // ripples a slow ambient swell keeps the surface alive when nobody touches it. Heights are canvas units above the resting level;
    // x runs from -Width/2 to Width/2 across the tank. Pure numbers: the picture is drawn from it (App/Ui/WaterGraphic) and the floating
    // objects ride it (BuoyantBody), so what the child sees and what the objects do always agree.
    public sealed class WaterSurface
    {
        public const float WaveSpeed = 260f;   // canvas units per second a ripple travels
        public const float Tension = 28f;      // pulls every column back to rest
        public const float Damping = 2.0f;     // ripples fade out in a few seconds
        public const float TimeStep = 1f / 120f;
        public const float AmbientAmplitude = 4.5f;

        private readonly float[] _height;
        private readonly float[] _velocity;
        private float _spare;

        public WaterSurface(int columns, float width)
        {
            if (columns < 3) throw new ArgumentOutOfRangeException(nameof(columns));
            _height = new float[columns];
            _velocity = new float[columns];
            Width = width;
        }

        public int Columns => _height.Length;
        public float Width { get; }
        public float ColumnWidth => Width / (_height.Length - 1);
        public float Time { get; private set; }

        public float ColumnX(int column) => -Width * 0.5f + column * ColumnWidth;
        public float Ripple(int column) => _height[column];

        // The slow swell: two sine waves running opposite ways. `phase` shifts it in time (the back layer of water uses another phase).
        public static float Ambient(float x, float time) =>
            AmbientAmplitude * (Sin(0.011f * x + 1.6f * time) + 0.6f * Sin(-0.019f * x + 2.5f * time + 1.3f)) / 1.6f;

        // Surface height at x: the ripples (linear between columns) plus the ambient swell.
        public float HeightAt(float x, float phase = 0f)
        {
            var position = (x + Width * 0.5f) / ColumnWidth;
            if (position <= 0f) position = 0f;
            var last = _height.Length - 1;
            if (position >= last) position = last;
            var lower = (int)position;
            var upper = Math.Min(lower + 1, last);
            var fraction = position - lower;
            var ripple = _height[lower] + (_height[upper] - _height[lower]) * fraction;
            return ripple + Ambient(x, Time + phase);
        }

        // How steeply the surface tilts at x (rise over run), for tilting what floats on it.
        public float SlopeAt(float x)
        {
            const float reach = 12f;
            return (HeightAt(x + reach) - HeightAt(x - reach)) / (2f * reach);
        }

        // Pushes the water up (positive) or down at x, falling off smoothly over `radius`. Dropping something in is a downward push.
        public void Disturb(float x, float velocity, float radius = 34f)
        {
            for (var i = 0; i < _velocity.Length; i++)
            {
                var distance = (ColumnX(i) - x) / radius;
                if (distance > 3f || distance < -3f) continue;
                _velocity[i] += velocity * (float)Math.Exp(-distance * distance);
            }
        }

        public void Advance(float seconds)
        {
            _spare += Math.Min(seconds, 0.1f);
            while (_spare >= TimeStep)
            {
                Substep();
                _spare -= TimeStep;
                Time += TimeStep;
            }
        }

        // Sum of squared ripple heights, a measure of how much the water is still moving.
        public float RippleEnergy
        {
            get
            {
                var sum = 0f;
                foreach (var h in _height) sum += h * h;
                return sum;
            }
        }

        private void Substep()
        {
            var last = _height.Length - 1;
            var spacing = ColumnWidth;
            var stiffness = WaveSpeed * WaveSpeed / (spacing * spacing);
            for (var i = 0; i <= last; i++)
            {
                var left = _height[i == 0 ? 0 : i - 1];   // the glass wall reflects: the missing neighbour is the column itself
                var right = _height[i == last ? last : i + 1];
                var acceleration = stiffness * (left + right - 2f * _height[i]) - Tension * _height[i] - Damping * _velocity[i];
                _velocity[i] += acceleration * TimeStep;
            }
            for (var i = 0; i <= last; i++) _height[i] += _velocity[i] * TimeStep;
        }

        private static float Sin(float value) => (float)Math.Sin(value);
    }
}
