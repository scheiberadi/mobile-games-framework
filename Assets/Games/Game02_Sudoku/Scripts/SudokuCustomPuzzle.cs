using MobileGamesFramework.Grid;

namespace Game02_Sudoku
{
    public static class SudokuCustomPuzzle
    {
        public static bool TryBuild(GridCore<SudokuCell> board, out SudokuPuzzle puzzle, out string error)
        {
            puzzle = null;

            if (SudokuSolver.FindConflicts(board).Count > 0)
            {
                error = "Board has conflicting numbers.";
                return false;
            }

            var solutionCount = SudokuSolver.CountSolutions(board, 2);
            if (solutionCount == 0)
            {
                error = "No valid solution exists for this puzzle.";
                return false;
            }

            if (solutionCount > 1)
            {
                error = "Puzzle has multiple solutions — add more numbers.";
                return false;
            }

            if (!SudokuSolver.TrySolve(board, null, out var solution))
            {
                error = "No valid solution exists for this puzzle.";
                return false;
            }

            var givenBoard = board.Clone();
            foreach (var pos in givenBoard.AllPositions())
            {
                var cell = givenBoard.Get(pos).Value;
                if (cell.Value == 0) continue;
                cell.IsGiven = true;
                givenBoard.Set(pos, cell);
            }

            puzzle = new SudokuPuzzle { Board = givenBoard, Solution = solution };
            error = null;
            return true;
        }
    }
}
