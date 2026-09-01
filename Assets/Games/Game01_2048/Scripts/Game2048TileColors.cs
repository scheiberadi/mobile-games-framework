using System.Collections.Generic;
using UnityEngine;

namespace Game01_2048
{
    public static class Game2048TileColors
    {
        private static readonly Dictionary<int, Color> Colors = new Dictionary<int, Color>
        {
            { 2, new Color(0.93f, 0.89f, 0.85f) },
            { 4, new Color(0.93f, 0.88f, 0.78f) },
            { 8, new Color(0.95f, 0.69f, 0.47f) },
            { 16, new Color(0.96f, 0.58f, 0.39f) },
            { 32, new Color(0.96f, 0.49f, 0.37f) },
            { 64, new Color(0.96f, 0.37f, 0.23f) },
            { 128, new Color(0.93f, 0.81f, 0.45f) },
            { 256, new Color(0.93f, 0.80f, 0.38f) },
            { 512, new Color(0.93f, 0.78f, 0.31f) },
            { 1024, new Color(0.93f, 0.77f, 0.25f) },
            { 2048, new Color(0.93f, 0.76f, 0.18f) },
        };

        private static readonly Color Empty = new Color(0.80f, 0.75f, 0.71f);
        private static readonly Color Overflow = new Color(0.24f, 0.22f, 0.20f);

        public static Color ForValue(int? value)
        {
            if (value == null) return Empty;
            return Colors.TryGetValue(value.Value, out var color) ? color : Overflow;
        }
    }
}
