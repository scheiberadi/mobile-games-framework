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

    // Where the tiles of a building's activity list go: a grid of up to four columns, growing downward one row at a
    // time from the top of a scrollable Content rect (see BuildingScreen), so any number of activities fits - a
    // building's own list scrolls instead of being capped. X is centred on the Content rect; Y is the offset down
    // from the Content rect's top edge (0 or negative), for a top-anchored (pivot (0.5,1)) tile.
    public static class TileLayout
    {
        public const float AreaXMin = -600f, AreaXMax = 600f, AreaYMin = -380f, AreaYMax = 170f, Gap = 30f;
        private const int Columns = 4;

        public static TileRect[] Compute(int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            var side = SideFor(count);
            var rows = RowsFor(count);
            var tiles = new TileRect[count];
            for (var i = 0; i < count; i++)
            {
                var row = i / Columns;
                var inRow = row == rows - 1 ? count - row * Columns : Columns;
                var x = (i % Columns - (inRow - 1) / 2f) * (side + Gap);
                var y = -(row * (side + Gap) + side / 2f);
                tiles[i] = new TileRect(x, y, side);
            }
            return tiles;
        }

        // Total height, in the same units as a tile's Y, that a Content rect needs to hold every row.
        public static float ContentHeight(int count) => RowsFor(count) * SideFor(count) + (RowsFor(count) - 1) * Gap;

        private static int RowsFor(int count) => (count + Columns - 1) / Columns;

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
