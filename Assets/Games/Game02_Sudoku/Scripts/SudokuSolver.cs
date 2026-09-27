using System;
using System.Collections.Generic;
using System.Linq;
using MobileGamesFramework.Grid;

namespace Game02_Sudoku
{
    public static class SudokuSolver
    {
        private const int Size = SudokuBoardFactory.Size;
        private const int BoxSize = 3;

        private const int AllDigits = 0x1FF;
        private static readonly byte[] BitCount = BuildBitCountTable();

        public static bool TrySolve(GridCore<SudokuCell> board, Random random, out GridCore<SudokuCell> solution)
        {
            var state = new SolverState(ToArray(board));
            if (state.HasConflictingGivens)
            {
                solution = null;
                return false;
            }

            if (Solve(state, random))
            {
                var working = board.Clone();
                for (var i = 0; i < Size * Size; i++)
                    SetValue(working, new GridPosition(i / Size, i % Size), state.Grid[i]);
                solution = working;
                return true;
            }

            solution = null;
            return false;
        }

        public static int CountSolutions(GridCore<SudokuCell> board, int limit) =>
            CountSolutions(ToArray(board), limit);

        // Flat row-major digit array (0 = empty) - the generator carves puzzles on this directly
        // so it doesn't pay for board clones and per-cell struct copies on every removal attempt.
        internal static int CountSolutions(int[] grid, int limit)
        {
            var state = new SolverState(grid);
            if (state.HasConflictingGivens) return 0;

            var count = 0;
            CountSolutionsRecursive(state, limit, ref count);
            return count;
        }

        internal static int[] ToArray(GridCore<SudokuCell> board)
        {
            var grid = new int[Size * Size];
            for (var i = 0; i < grid.Length; i++)
                grid[i] = board.Get(new GridPosition(i / Size, i % Size)).Value.Value;
            return grid;
        }

        // Bitmask sudoku state: bit (d-1) set in Rows/Cols/Boxes[i] means digit d is used there.
        private sealed class SolverState
        {
            public readonly int[] Grid = new int[Size * Size];
            public readonly int[] Rows = new int[Size];
            public readonly int[] Cols = new int[Size];
            public readonly int[] Boxes = new int[Size];

            // True when two pre-filled digits clash in a row, column or box. Such a board has no
            // solution, but the search would not notice: it only checks the empty cells, so it
            // would burn exponential time proving the impossibility by exhaustion.
            public readonly bool HasConflictingGivens;

            public SolverState(int[] source)
            {
                for (var i = 0; i < Grid.Length; i++)
                {
                    Grid[i] = source[i];
                    if (source[i] == 0) continue;

                    if ((Candidates(i) & (1 << (source[i] - 1))) == 0) HasConflictingGivens = true;
                    Place(i, source[i]);
                }
            }

            public static int BoxIndex(int cell) => (cell / Size / BoxSize) * BoxSize + (cell % Size) / BoxSize;

            public int Candidates(int cell) =>
                ~(Rows[cell / Size] | Cols[cell % Size] | Boxes[BoxIndex(cell)]) & AllDigits;

            public void Place(int cell, int digit)
            {
                var bit = 1 << (digit - 1);
                Grid[cell] = digit;
                Rows[cell / Size] |= bit;
                Cols[cell % Size] |= bit;
                Boxes[BoxIndex(cell)] |= bit;
            }

            public void Clear(int cell, int digit)
            {
                var mask = ~(1 << (digit - 1));
                Grid[cell] = 0;
                Rows[cell / Size] &= mask;
                Cols[cell % Size] &= mask;
                Boxes[BoxIndex(cell)] &= mask;
            }
        }

        private static byte[] BuildBitCountTable()
        {
            var table = new byte[AllDigits + 1];
            for (var i = 1; i < table.Length; i++) table[i] = (byte)(table[i >> 1] + (i & 1));
            return table;
        }

        // Most-constrained-variable (MRV) heuristic: branching on the emptiest-of-candidates
        // cell first prunes dead ends immediately instead of discovering them many moves later,
        // which is what makes naive first-empty-cell backtracking blow up on sparse boards.
        // Returns the empty cell with the fewest candidates (first one on ties), or -1 if the
        // board is full. bestMask is 0 when some empty cell has no candidates (dead end).
        private static int FindMostConstrainedEmptyCell(SolverState state, out int bestMask)
        {
            var best = -1;
            bestMask = 0;
            var bestCount = Size + 1;

            for (var cell = 0; cell < state.Grid.Length; cell++)
            {
                if (state.Grid[cell] != 0) continue;

                var mask = state.Candidates(cell);
                var count = BitCount[mask];
                if (count == 0)
                {
                    bestMask = 0;
                    return cell;
                }

                if (count < bestCount)
                {
                    bestCount = count;
                    best = cell;
                    bestMask = mask;
                    if (count == 1) break;
                }
            }

            return best;
        }

        public static List<GridPosition> FindConflicts(GridCore<SudokuCell> board)
        {
            var conflicts = new HashSet<GridPosition>();

            for (var i = 0; i < Size; i++)
            {
                AddConflictsWithinUnit(board, RowPositions(i), conflicts);
                AddConflictsWithinUnit(board, ColumnPositions(i), conflicts);
            }

            for (var boxRow = 0; boxRow < Size; boxRow += BoxSize)
            for (var boxCol = 0; boxCol < Size; boxCol += BoxSize)
                AddConflictsWithinUnit(board, BoxPositions(boxRow, boxCol), conflicts);

            return conflicts.ToList();
        }

        private static void AddConflictsWithinUnit(GridCore<SudokuCell> board, IEnumerable<GridPosition> unit, HashSet<GridPosition> conflicts)
        {
            var byValue = unit.Where(p => board.Get(p).Value.Value != 0).GroupBy(p => board.Get(p).Value.Value);
            foreach (var group in byValue)
            {
                if (group.Count() <= 1) continue;
                foreach (var pos in group) conflicts.Add(pos);
            }
        }

        private static bool Solve(SolverState state, Random random)
        {
            var cell = FindMostConstrainedEmptyCell(state, out var mask);
            if (cell < 0) return true;

            var candidates = new int[BitCount[mask]];
            var n = 0;
            for (var digit = 1; digit <= Size; digit++)
                if ((mask & (1 << (digit - 1))) != 0) candidates[n++] = digit;
            if (random != null) candidates.Shuffle(random);

            foreach (var candidate in candidates)
            {
                state.Place(cell, candidate);
                if (Solve(state, random)) return true;
                state.Clear(cell, candidate);
            }

            return false;
        }

        private static void CountSolutionsRecursive(SolverState state, int limit, ref int count)
        {
            if (count >= limit) return;

            var cell = FindMostConstrainedEmptyCell(state, out var mask);
            if (cell < 0)
            {
                count++;
                return;
            }

            for (var digit = 1; digit <= Size; digit++)
            {
                if (count >= limit) return;
                if ((mask & (1 << (digit - 1))) == 0) continue;

                state.Place(cell, digit);
                CountSolutionsRecursive(state, limit, ref count);
                state.Clear(cell, digit);
            }
        }

        // Every position sharing a row, column, or 3x3 box with pos (excluding pos itself) -
        // the set of cells a placed digit here can conflict or interact with.
        public static IEnumerable<GridPosition> PeerPositions(GridPosition pos)
        {
            var boxRow = (pos.Row / BoxSize) * BoxSize;
            var boxCol = (pos.Col / BoxSize) * BoxSize;

            return RowPositions(pos.Row).Concat(ColumnPositions(pos.Col)).Concat(BoxPositions(boxRow, boxCol))
                .Where(p => !p.Equals(pos));
        }

        private static void SetValue(GridCore<SudokuCell> board, GridPosition pos, int value)
        {
            var cell = board.Get(pos).Value;
            cell.Value = value;
            board.Set(pos, cell);
        }

        private static IEnumerable<GridPosition> RowPositions(int row) =>
            Enumerable.Range(0, Size).Select(col => new GridPosition(row, col));

        private static IEnumerable<GridPosition> ColumnPositions(int col) =>
            Enumerable.Range(0, Size).Select(row => new GridPosition(row, col));

        private static IEnumerable<GridPosition> BoxPositions(int boxRow, int boxCol)
        {
            for (var row = boxRow; row < boxRow + BoxSize; row++)
            for (var col = boxCol; col < boxCol + BoxSize; col++)
                yield return new GridPosition(row, col);
        }

    }
}
