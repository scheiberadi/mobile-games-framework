using System;

namespace EvasLearningWorld.Rules
{
    public sealed class JigsawPiece
    {
        // Stands in for one tile of a real sliced photo ("jigsaw/piece_<row>_<col>") until a real piece-cutting
        // content pipeline exists (source image sliced into N pieces) - the plan's own note that this is a
        // separate prerequisite, not something buildable in a Unity-less session anyway. Every piece still has
        // its own distinct sprite key, same as every other placeholder catalogue this session.
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
    // The tray is a second grid, the same shape as the board, holding every piece under a shuffled index - so
    // "where a piece starts" and "where it belongs" are always two different grid slots without needing any
    // scatter/overlap math.
    public static class JigsawRoundGenerator
    {
        // Fewer, longer rounds than a tap game - fully assembling a picture takes longer than one tap.
        public const int RoundsPerSession = 3;

        public const float BoardWidth = 560f, BoardHeight = 560f;
        public static readonly WorldPoint BoardCenter = new WorldPoint(-260f, -60f);

        public const float TrayWidth = 560f, TrayHeight = 220f;
        public static readonly WorldPoint TrayCenter = new WorldPoint(-260f, -330f);

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
                    SpriteKey = "jigsaw/piece_" + row + "_" + col,
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
