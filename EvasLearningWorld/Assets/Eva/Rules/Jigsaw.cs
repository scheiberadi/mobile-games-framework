using System;

namespace EvasLearningWorld.Rules
{
    public sealed class JigsawPiece
    {
        // One tile of the picture cut for this round's grid: "jigsaw/piece_<row>_<col>" for the 5x5 grid and
        // "jigsaw/grid<cols>x<rows>_<row>_<col>" for the others (tools/art-import/cut-jigsaw.js), so every level
        // shows the whole picture and not just a corner of the 5x5 cut.
        public string SpriteKey;

        // Where this piece belongs in the assembled board.
        public WorldPoint HomePosition;

        // Where it starts, scattered in the tray.
        public WorldPoint TrayPosition;
    }

    public sealed class JigsawRound
    {
        public JigsawPiece[] Pieces;
        public int Columns;
        public int Rows;
        public float PieceWidth;
        public float PieceHeight;

        // How close a dragged piece's centre must land to its own HomePosition to count as placed.
        public float SnapRadius;
    }

    // Jigsaw (Playground, spec 4.1): DRAG & DROP, reusing DragItem (App/Ui) with a new "snap when close to its
    // own correct region" check the screen applies - this generator only lays out the board and tray, it knows
    // nothing about dragging itself. Progression: piece count (the plan's own "4 -> 6 -> 9 -> 16 -> 25+" ladder).
    // The tray is a second grid, the same shape and size as the board, holding every piece under a shuffled index - so
    // "where a piece starts" and "where it belongs" are always two different grid slots without needing any
    // scatter/overlap math.
    public static class JigsawRoundGenerator
    {
        // Fewer, longer rounds than a tap game - fully assembling a picture takes longer than one tap.
        public const int RoundsPerSession = 3;

        public const float BoardWidth = 560f, BoardHeight = 560f;
        public static readonly WorldPoint BoardCenter = new WorldPoint(-260f, -60f);

        // Same size as the board, to its right and above the Corner companion pair (top at y -240), so a tray piece is shown
        // at its full size and never lands on the board or runs off the frame.
        public const float TrayWidth = 560f, TrayHeight = 560f;
        public static readonly WorldPoint TrayCenter = new WorldPoint(395f, 60f);

        // Index i = level (i + 1): 4, 6, 9, 16, 25 pieces; level 6 repeats level 5's grid - 25 is the ceiling
        // this placeholder grid comfortably supports without real piece art to judge legibility against.
        private static readonly (int Columns, int Rows)[] GridByLevel = { (2, 2), (2, 3), (3, 3), (4, 4), (5, 5), (5, 5) };

        public static JigsawRound Create(int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var (columns, rows) = GridByLevel[level - DifficultyLadder.MinLevel];
            var count = columns * rows;

            var cellWidth = BoardWidth / columns;
            var cellHeight = BoardHeight / rows;
            var trayCellWidth = TrayWidth / columns;
            var trayCellHeight = TrayHeight / rows;

            var order = new int[count];
            for (var i = 0; i < count; i++) order[i] = i;
            Shuffle(order, rng);

            var pieces = new JigsawPiece[count];
            for (var i = 0; i < count; i++)
            {
                var col = i % columns;
                var row = i / columns;
                var trayIndex = order[i];
                var trayCol = trayIndex % columns;
                var trayRow = trayIndex / columns;

                pieces[i] = new JigsawPiece
                {
                    SpriteKey = SpriteKeyFor(columns, rows, row, col),
                    HomePosition = new WorldPoint(
                        BoardCenter.X - BoardWidth / 2f + (col + 0.5f) * cellWidth,
                        BoardCenter.Y + BoardHeight / 2f - (row + 0.5f) * cellHeight),
                    TrayPosition = new WorldPoint(
                        TrayCenter.X - TrayWidth / 2f + (trayCol + 0.5f) * trayCellWidth,
                        TrayCenter.Y + TrayHeight / 2f - (trayRow + 0.5f) * trayCellHeight),
                };
            }

            return new JigsawRound
            {
                Pieces = pieces,
                Columns = columns,
                Rows = rows,
                PieceWidth = cellWidth,
                PieceHeight = cellHeight,
                SnapRadius = Math.Min(cellWidth, cellHeight) * 0.4f,
            };
        }

        private static string SpriteKeyFor(int columns, int rows, int row, int col) =>
            columns == 5 && rows == 5
                ? "jigsaw/piece_" + row + "_" + col
                : "jigsaw/grid" + columns + "x" + rows + "_" + row + "_" + col;

        private static void Shuffle(int[] values, Random rng)
        {
            for (var i = values.Length - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }
    }
}
