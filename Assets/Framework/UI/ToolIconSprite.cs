using UnityEngine;

namespace MobileGamesFramework.UI
{
    // Small flat-icon glyphs (pencil, eraser) for tool buttons, drawn procedurally at
    // runtime the same way RoundedRectSprite/GradientSprite generate their textures -
    // no image assets to import or license, and it stays crisp at any button size.
    public static class ToolIconSprite
    {
        private const int Size = 64;

        private static Sprite _pencil;
        private static Sprite _eraser;

        public static Sprite GetPencil()
        {
            if (_pencil != null) return _pencil;

            var texture = NewTransparentTexture();
            var tip = new Vector2(14f, 14f);
            var dir = new Vector2(0.70710678f, 0.70710678f); // long axis, tip -> cap
            var perp = new Vector2(-0.70710678f, 0.70710678f);

            const float graphiteEnd = 4f;
            const float woodEnd = 14f;
            const float halfWidth = 6f;
            const float shaftEnd = 40f;
            const float ferruleEnd = 43f;
            const float capEnd = 50.9f;
            const float outline = 1.4f;

            var graphite = new Color(0.22f, 0.22f, 0.22f);
            var wood = new Color(0.91f, 0.79f, 0.54f);
            var yellow = new Color(0.97f, 0.81f, 0.35f);
            var ferrule = new Color(0.79f, 0.80f, 0.82f);
            var eraserCap = new Color(0.91f, 0.59f, 0.64f);
            var outlineColor = new Color(0.23f, 0.16f, 0.10f);

            for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f) - tip;
                var t = Vector2.Dot(p, dir);
                var s = Vector2.Dot(p, perp);

                if (t < 0f || t > capEnd) continue;

                var halfWidthAtT = t < woodEnd ? halfWidth * (t / woodEnd) : halfWidth;
                var absS = Mathf.Abs(s);
                if (absS > halfWidthAtT + outline) continue;

                Color color;
                if (absS > halfWidthAtT) color = outlineColor;
                else if (t < graphiteEnd) color = graphite;
                else if (t < woodEnd) color = wood;
                else if (t < shaftEnd) color = yellow;
                else if (t < ferruleEnd) color = ferrule;
                else color = eraserCap;

                texture.SetPixel(x, y, color);
            }

            texture.Apply();
            _pencil = BuildSprite(texture);
            return _pencil;
        }

        public static Sprite GetEraser()
        {
            if (_eraser != null) return _eraser;

            var texture = NewTransparentTexture();
            const float x0 = 18f, x1 = 46f, y0 = 10f, y1 = 50f;
            const float radius = 4f;
            const float chamfer = 8f;
            const float splitY = 36f;
            const float outline = 1.4f;

            var pink = new Color(0.89f, 0.63f, 0.68f);
            var cream = new Color(0.96f, 0.95f, 0.93f);
            var outlineColor = new Color(0.46f, 0.24f, 0.29f);
            var highlight = new Color(1f, 0.86f, 0.89f, 0.9f);

            for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
            {
                var px = x + 0.5f;
                var py = y + 0.5f;

                if (!IsInsideEraser(px, py, x0, x1, y0, y1, radius, chamfer, 0f)) continue;

                Color color;
                if (!IsInsideEraser(px, py, x0, x1, y0, y1, radius, chamfer, outline))
                    color = outlineColor;
                else if (px >= 22f && px <= 34f && py >= 44f && py <= 47f)
                    color = highlight;
                else
                    color = py >= splitY ? pink : cream;

                texture.SetPixel(x, y, color);
            }

            texture.Apply();
            _eraser = BuildSprite(texture);
            return _eraser;
        }

        // inset shrinks the boundary uniformly - used to carve the outline band by testing
        // "inside the full shape" vs "inside the shape minus outline thickness".
        private static bool IsInsideEraser(float x, float y, float x0, float x1, float y0, float y1,
            float radius, float chamfer, float inset)
        {
            var ix0 = x0 + inset;
            var ix1 = x1 - inset;
            var iy0 = y0 + inset;
            var iy1 = y1 - inset;
            if (x < ix0 || x > ix1 || y < iy0 || y > iy1) return false;

            var r = Mathf.Max(0f, radius - inset);
            if (x < ix0 + r && y < iy0 + r)
            {
                var dx = x - (ix0 + r);
                var dy = y - (iy0 + r);
                return dx * dx + dy * dy <= r * r;
            }
            if (x > ix1 - r && y < iy0 + r)
            {
                var dx = x - (ix1 - r);
                var dy = y - (iy0 + r);
                return dx * dx + dy * dy <= r * r;
            }

            var c = Mathf.Max(0f, chamfer - inset * 1.41421356f);
            if (x > ix1 - c && y > iy1 - c && (x - (ix1 - c)) + (y - (iy1 - c)) > c) return false;

            return true;
        }

        private static Texture2D NewTransparentTexture()
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
                texture.SetPixel(x, y, Color.clear);
            return texture;
        }

        private static Sprite BuildSprite(Texture2D texture) =>
            Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
    }
}
