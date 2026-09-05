using System.Collections.Generic;
using UnityEngine;

namespace MobileGamesFramework.UI
{
    // A small procedural gear/cog glyph, drawn the same way IconGenerator draws the
    // app icons - a per-pixel radius/angle check rather than an imported image asset.
    public static class GearIconSprite
    {
        private const int Size = 64;
        private const float OuterRadius = 26f;
        private const float BodyRadius = 19f;
        private const float HoleRadius = 7f;
        private const int ToothCount = 8;
        private const float ToothHalfWidthDegrees = 18f;

        private static readonly Dictionary<Color, Sprite> Cache = new Dictionary<Color, Sprite>();

        public static Sprite Get(Color color)
        {
            if (Cache.TryGetValue(color, out var cached)) return cached;

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var center = new Vector2(Size / 2f, Size / 2f);

            for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
            {
                var offset = new Vector2(x + 0.5f, y + 0.5f) - center;
                var dist = offset.magnitude;
                var filled = dist <= HoleRadius
                    ? false
                    : dist <= BodyRadius
                        ? true
                        : dist <= OuterRadius && IsOnTooth(offset);
                texture.SetPixel(x, y, filled ? color : Color.clear);
            }

            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            Cache[color] = sprite;
            return sprite;
        }

        private static bool IsOnTooth(Vector2 offset)
        {
            var angleDegrees = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            if (angleDegrees < 0) angleDegrees += 360f;

            var step = 360f / ToothCount;
            var withinStep = angleDegrees % step;
            var distanceFromToothCenter = Mathf.Min(withinStep, step - withinStep);
            return distanceFromToothCenter <= ToothHalfWidthDegrees;
        }
    }
}
