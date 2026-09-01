using UnityEngine;

namespace MobileGamesFramework.UI
{
    public static class RoundedRectSprite
    {
        private const int Size = 32;
        private const int Radius = 10;

        private static Sprite _cached;

        public static Sprite Get()
        {
            if (_cached != null) return _cached;

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };

            for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, IsInsideRoundedRect(x, y) ? 1f : 0f));

            texture.Apply();

            var border = new Vector4(Radius, Radius, Radius, Radius);
            _cached = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            return _cached;
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
