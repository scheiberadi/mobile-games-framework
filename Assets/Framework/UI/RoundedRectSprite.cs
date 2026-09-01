using System.Collections.Generic;
using UnityEngine;

namespace MobileGamesFramework.UI
{
    public static class RoundedRectSprite
    {
        private const int Size = 32;
        private const int Radius = 10;

        private static Sprite _cached;
        private static readonly Dictionary<(Color, Color), Sprite> GradientCache = new Dictionary<(Color, Color), Sprite>();

        public static Sprite Get()
        {
            if (_cached != null) return _cached;

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };

            for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, IsInsideRoundedRect(x, y) ? 1f : 0f));

            texture.Apply();

            _cached = BuildSprite(texture);
            return _cached;
        }

        public static Sprite GetGradient(Color top, Color bottom)
        {
            var key = (top, bottom);
            if (GradientCache.TryGetValue(key, out var cached)) return cached;

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };

            for (var y = 0; y < Size; y++)
            {
                var color = Color.Lerp(bottom, top, (float)y / (Size - 1));
                for (var x = 0; x < Size; x++)
                {
                    color.a = IsInsideRoundedRect(x, y) ? 1f : 0f;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();

            var sprite = BuildSprite(texture);
            GradientCache[key] = sprite;
            return sprite;
        }

        private static Sprite BuildSprite(Texture2D texture)
        {
            var border = new Vector4(Radius, Radius, Radius, Radius);
            return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        private static bool IsInsideRoundedRect(int x, int y)
        {
            var nearestCornerX = Mathf.Clamp(x, Radius, Size - Radius - 1);
            var nearestCornerY = Mathf.Clamp(y, Radius, Size - Radius - 1);
            var dx = x - nearestCornerX;
            var dy = y - nearestCornerY;
            return dx * dx + dy * dy <= Radius * Radius;
        }
    }
}
