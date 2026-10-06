using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // The water of Sink or Float's tank, drawn from a WaterSurface every frame so it really moves: the top edge follows the ripples and
    // the slow swell, a bright foam line runs along it, and soft shafts of light sway down through it. Two of these sit in the tank,
    // one behind the objects (the deep body of water) and one in front (a thin clear tint, the foam and the light), so a sunk object
    // is seen THROUGH the water while one floating on top stays clear. The rect is the tank's inside; x is the surface's x, y is
    // measured from the rect's centre.
    public sealed class WaterGraphic : MaskableGraphic
    {
        public enum Layer { Back, Front }

        public Layer Kind = Layer.Back;
        public WaterSurface Surface;
        public float RestY;          // where the resting surface is, from the rect's centre
        public float Phase;          // shifts this layer's swell in time, so the two layers do not move as one
        public float Excite;         // 0..1: brightens the surface while an object is held over the tank

        private static readonly Color BackTop = new Color(0.36f, 0.74f, 0.93f, 0.62f);
        private static readonly Color BackBottom = new Color(0.10f, 0.40f, 0.70f, 0.86f);
        private static readonly Color FrontTop = new Color(0.60f, 0.90f, 1f, 0.16f);
        private static readonly Color FrontBottom = new Color(0.12f, 0.45f, 0.78f, 0.20f);

        private const int Rays = 5;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Surface == null) return;

            var bottom = -rectTransform.rect.height * 0.5f;
            var columns = Surface.Columns;
            var top = Kind == Layer.Back ? BackTop : FrontTop;
            var deep = Kind == Layer.Back ? BackBottom : FrontBottom;
            top.a = Mathf.Min(1f, top.a + 0.08f * Excite);

            // The body of the water: three rows of vertices per column, so the colour deepens smoothly with depth.
            const int rows = 3;
            for (var i = 0; i < columns; i++)
            {
                var x = Surface.ColumnX(i);
                var surfaceY = RestY + Surface.HeightAt(x, Phase);
                var span = surfaceY - bottom;
                for (var r = 0; r < rows; r++)
                {
                    var t = r / (float)(rows - 1);                     // 0 at the surface, 1 at the floor
                    var depth = Mathf.Pow(t, 0.7f);
                    var color = Color.Lerp(top, deep, depth);
                    vh.AddVert(new Vector3(x, surfaceY - span * t, 0f), color, Vector2.zero);
                }
            }
            for (var i = 0; i < columns - 1; i++)
            {
                for (var r = 0; r < rows - 1; r++)
                {
                    var a = i * rows + r;
                    var b = (i + 1) * rows + r;
                    vh.AddTriangle(a, b, a + 1);
                    vh.AddTriangle(b, b + 1, a + 1);
                }
            }

            if (Kind != Layer.Front) return;
            AddFoam(vh, columns);
            AddLightShafts(vh, bottom);
        }

        // A bright line along the surface with a soft fade just under it.
        private void AddFoam(VertexHelper vh, int columns)
        {
            var line = new Color(1f, 1f, 1f, 0.78f + 0.2f * Excite);
            var fade = new Color(0.85f, 0.97f, 1f, 0.34f);
            var clear = new Color(0.85f, 0.97f, 1f, 0f);
            var thickness = 5f + 3f * Excite;
            var start = vh.currentVertCount;
            for (var i = 0; i < columns; i++)
            {
                var x = Surface.ColumnX(i);
                var y = RestY + Surface.HeightAt(x, Phase);
                vh.AddVert(new Vector3(x, y + thickness * 0.5f, 0f), line, Vector2.zero);
                vh.AddVert(new Vector3(x, y - thickness * 0.5f, 0f), fade, Vector2.zero);
                vh.AddVert(new Vector3(x, y - thickness * 0.5f - 24f, 0f), clear, Vector2.zero);
            }
            for (var i = 0; i < columns - 1; i++)
            {
                for (var r = 0; r < 2; r++)
                {
                    var a = start + i * 3 + r;
                    var b = start + (i + 1) * 3 + r;
                    vh.AddTriangle(a, b, a + 1);
                    vh.AddTriangle(b, b + 1, a + 1);
                }
            }
        }

        // A few slanted shafts of light that start at the surface, widen a little as they go and fade out, swaying slowly.
        private void AddLightShafts(VertexHelper vh, float bottom)
        {
            var width = Surface.Width;
            var half = width * 0.5f; // the shafts stay inside the glass
            for (var k = 0; k < Rays; k++)
            {
                var sway = Mathf.Sin(Surface.Time * 0.35f + k * 1.9f);
                var centre = -width * 0.5f + width * (k + 0.5f) / Rays + sway * 34f;
                var topY = RestY + Surface.HeightAt(centre, Phase) - 3f;
                var length = (topY - bottom) * (0.62f + 0.14f * Mathf.Sin(Surface.Time * 0.5f + k));
                var lean = 70f;
                var halfTop = 20f + 6f * Mathf.Sin(Surface.Time * 0.7f + k * 0.8f);
                var halfEnd = 46f;
                var bright = 0.15f + 0.05f * Mathf.Sin(Surface.Time * 0.9f + k * 2.1f);
                var start = vh.currentVertCount;
                var strong = new Color(1f, 1f, 1f, bright);
                var none = new Color(1f, 1f, 1f, 0f);
                vh.AddVert(new Vector3(Mathf.Clamp(centre - halfTop, -half, half), topY, 0f), strong, Vector2.zero);
                vh.AddVert(new Vector3(Mathf.Clamp(centre + halfTop, -half, half), topY, 0f), strong, Vector2.zero);
                vh.AddVert(new Vector3(Mathf.Clamp(centre + lean + halfEnd, -half, half), topY - length, 0f), none, Vector2.zero);
                vh.AddVert(new Vector3(Mathf.Clamp(centre + lean - halfEnd, -half, half), topY - length, 0f), none, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
        }

        private void LateUpdate()
        {
            if (Surface != null) SetVerticesDirty();
        }
    }
}
