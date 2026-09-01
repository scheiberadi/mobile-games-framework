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

        public static bool TrySolve(GridCore<SudokuCell> board, Random random, out GridCore<SudokuCell> solution)
        {
            var working = board.Clone();
            if (Solve(working, random))
            {
                solution = working;
                return true;
            }

            solution = null;
            return false;
        }

        public static int CountSolutions(GridCore<SudokuCell> board, int limit)
        {
            var working = board.Clone();
            var count = 0;
            CountSolutionsRecursive(working, limit, ref count);
            return count;
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

        private static bool Solve(GridCore<SudokuCell> board, Random random)
        {
            var empty = FindMostConstrainedEmptyCell(board);
            if (empty == null) return true;

            var candidates = Enumerable.Range(1, Size).Where(v => CanPlace(board, empty.Value, v)).ToList();
            if (random != null) candidates.Shuffle(random);

            foreach (var candidate in candidates)
            {
                SetValue(board, empty.Value, candidate);
                if (Solve(board, random)) return true;
                SetValue(board, empty.Value, 0);
            }

            return false;
        }

        private static void CountSolutionsRecursive(GridCore<SudokuCell> board, int limit, ref int count)
        {
            if (count >= limit) return;

            var empty = FindMostConstrainedEmptyCell(board);
            if (empty == null)
            {
                count++;
                return;
            }

            for (var candidate = 1; candidate <= Size; candidate++)
            {
                if (count >= limit) return;
                if (!CanPlace(board, empty.Value, candidate)) continue;

                SetValue(board, empty.Value, candidate);
                CountSolutionsRecursive(board, limit, ref count);
                SetValue(board, empty.Value, 0);
            }
        }

        // Most-constrained-variable (MRV) heuristic: branching on the emptiest-of-candidates
        // cell first prunes dead ends immediately instead of discovering them many moves later,
        // which is what makes naive first-empty-cell backtracking blow up on sparse boards.
        private static GridPosition? FindMostConstrainedEmptyCell(GridCore<SudokuCell> board)
        {
            GridPosition? best = null;
            var bestCandidateCount = Size + 1;

            foreach (var pos in board.AllPositions())
            {
                if (board.Get(pos).Value.Value != 0) continue;

                var candidateCount = Enumerable.Range(1, Size).Count(v => CanPlace(board, pos, v));
                if (candidateCount == 0) return pos;

                if (candidateCount < bestCandidateCount)
                {
                    bestCandidateCount = candidateCount;
                    best = pos;
                    if (bestCandidateCount == 1) break;
                }
            }

            return best;
        }

        private static bool CanPlace(GridCore<SudokuCell> board, GridPosition pos, int value)
        {
            var boxRow = (pos.Row / BoxSize) * BoxSize;
            var boxCol = (pos.Col / BoxSize) * BoxSize;

            return RowPositions(pos.Row).Concat(ColumnPositions(pos.Col)).Concat(BoxPositions(boxRow, boxCol))
                .All(p => board.Get(p).Value.Value != value);
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
