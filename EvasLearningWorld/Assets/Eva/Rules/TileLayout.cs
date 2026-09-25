using System;

namespace EvasLearningWorld.Rules
{
    // A tile's centre and side in canvas units (centre origin, y up).
    public readonly struct TileRect
    {
        public TileRect(float x, float y, float side) { X = x; Y = y; Side = side; }
        public float X { get; }
        public float Y { get; }
        public float Side { get; }
        public float XMin => X - Side / 2f;
        public float XMax => X + Side / 2f;
        public float YMin => Y - Side / 2f;
        public float YMax => Y + Side / 2f;
    }

    // Where the tiles of a building's activity list go. One activity gets one large tile; more get a grid of up to
    // four columns and two rows, centred, inside the area that stays clear of the Hud Home button (top-left) and the
    // coin counter (top-right).
    public static class TileLayout
    {
        public const float AreaXMin = -600f, AreaXMax = 600f, AreaYMin = -380f, AreaYMax = 170f, Gap = 30f;
        public const int MaxTiles = 8;

        public static TileRect[] Compute(int count)
        {
            if (count < 1 || count > MaxTiles) throw new ArgumentOutOfRangeException(nameof(count));
            var side = SideFor(count);
            var rows = count <= 4 ? 1 : 2;
            var centreY = (AreaYMin + AreaYMax) / 2f;
            var tiles = new TileRect[count];
            for (var i = 0; i < count; i++)
            {
                var row = i / 4;
                var inRow = row == 0 ? Math.Min(count, 4) : count - 4;
                var x = (i % 4 - (inRow - 1) / 2f) * (side + Gap);
                var y = centreY + ((rows - 1) / 2f - row) * (side + Gap);
                tiles[i] = new TileRect(x, y, side);
            }
            return tiles;
        }

        private static float SideFor(int count)
        {
            switch (count)
            {
                case 1: return 480f;
                case 2: return 440f;
                case 3: return 380f;
                case 4: return 270f;
                default: return 250f;
            }
        }
    }
}
