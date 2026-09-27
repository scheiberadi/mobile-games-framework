using System.Collections.Generic;
using UnityEngine;

namespace MobileGamesFramework.UI
{
    public static class GradientSprite
    {
        private const int Size = 64;

        private static readonly Dictionary<(Color, Color), Sprite> Cache = new Dictionary<(Color, Color), Sprite>();

        public static Sprite Get(Color top, Color bottom)
        {
            var key = (top, bottom);
            if (Cache.TryGetValue(key, out var cached)) return cached;

            var texture = new Texture2D(1, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < Size; y++)
                texture.SetPixel(0, y, Color.Lerp(bottom, top, (float)y / (Size - 1)));
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, Size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            Cache[key] = sprite;
            return sprite;
        }
    }
}
