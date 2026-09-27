using System;

namespace EvasLearningWorld.Rules
{
    public sealed class TangramPiece
    {
        // "tangram/shape_<index>" - a placeholder stand-in for a real tangram piece (triangle, square,
        // parallelogram) until real shape art exists, same as every other placeholder catalogue this session.
        public string SpriteKey;

        public WorldPoint HomePosition;
        public WorldPoint TrayPosition;

        // Relative to the round's own BasePieceWidth/Height - stands in for a real tangram piece's own size
        // (the classic set has 2 large triangles, 1 medium, 2 small, plus a square and a parallelogram, each a
        // different size) until real shape art and rotation-aware placement exist. See the class comment below.
        public float SizeScale;
    }

    public sealed class TangramRound
    {
        public TangramPiece[] Pieces;
        public float BasePieceWidth;
        public float BasePieceHeight;

        // How close a dragged piece's centre must land to its own HomePosition to count as placed.
        public float SnapRadius;
    }

    // Tangram / Puzzle Blocks (Playground, spec 4.1): DRAG & DROP over a ghost silhouette, distinct from
    // Jigsaw's photo reassembly - reuses DragItem plus Jigsaw's own snap-to-region logic with shape pieces
    // instead of cut-photo pieces. Progression: shape count (the classic 7-piece tangram set, building up from
    // 3) and "silhouette complexity" - stood in for here by SizeScale (a set of pieces sized like the classic
    // set's 2 large/1 medium/2 small triangles plus a square and a parallelogram) rather than true tangram
    // geometry, since real shape art and rotation-aware fitting are a separate content/design pass (the plan's
    // own note on Jigsaw's "needs a piece-cutting content pipeline" applies here just as much). The board/tray
    // layout otherwise reuses Jigsaw's own grid-permutation trick unchanged: a fixed 3x3 grid (big enough for
    // the 7-piece ceiling), only the first ShapeCount cells used each round, tray positions a shuffled
    // permutation of the same grid so no scatter/overlap math is needed.
    public static class TangramRoundGenerator
    {
        // Fewer, longer rounds than a tap game, same reasoning as Jigsaw - assembling a silhouette takes longer
        // than one tap.
        public const int RoundsPerSession = 3;

        public const float BoardWidth = 480f, BoardHeight = 480f;
        public static readonly WorldPoint BoardCenter = new WorldPoint(-260f, -60f);

        public const float TrayWidth = 560f, TrayHeight = 100f;
        public static readonly WorldPoint TrayCenter = new WorldPoint(-260f, -360f);

        private const int GridColumns = 3, GridRows = 3;

        // Index i = level (i + 1): 3, 4, 5, 6, 7, 7 pieces - the classic tangram set (7) is the ceiling.
        private static readonly int[] ShapeCountByLevel = { 3, 4, 5, 6, 7, 7 };

        // A placeholder stand-in for the classic set's own varied piece sizes (see the class comment).
        private static readonly float[] SizeScaleCatalog = { 1.3f, 1f, 0.8f, 1.15f, 0.9f, 1.05f, 0.75f };

        public static TangramRound Create(int level, Random rng)
        {
            if (level < DifficultyLadder.MinLevel || level > DifficultyLadder.MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
            var count = ShapeCountByLevel[level - DifficultyLadder.MinLevel];

            var cellWidth = BoardWidth / GridColumns;
            var cellHeight = BoardHeight / GridRows;
            var trayCellWidth = TrayWidth / GridColumns;
            var trayCellHeight = TrayHeight / GridRows;

            var order = new int[count];
            for (var i = 0; i < count; i++) order[i] = i;
            Shuffle(order, rng);

            var pieces = new TangramPiece[count];
            for (var i = 0; i < count; i++)
            {
                var col = i % GridColumns;
                var row = i / GridColumns;
                var trayIndex = order[i];
                var trayCol = trayIndex % GridColumns;
                var trayRow = trayIndex / GridColumns;

                pieces[i] = new TangramPiece
                {
                    SpriteKey = "tangram/shape_" + i,
                    HomePosition = new WorldPoint(
                        BoardCenter.X - BoardWidth / 2f + (col + 0.5f) * cellWidth,
                        BoardCenter.Y + BoardHeight / 2f - (row + 0.5f) * cellHeight),
                    TrayPosition = new WorldPoint(
                        TrayCenter.X - TrayWidth / 2f + (trayCol + 0.5f) * trayCellWidth,
                        TrayCenter.Y + TrayHeight / 2f - (trayRow + 0.5f) * trayCellHeight),
                    SizeScale = SizeScaleCatalog[i % SizeScaleCatalog.Length],
                };
            }

            return new TangramRound
            {
                Pieces = pieces,
                BasePieceWidth = cellWidth,
                BasePieceHeight = cellHeight,
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
