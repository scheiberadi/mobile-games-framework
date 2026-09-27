using MobileGamesFramework.Grid;

namespace Game02_Sudoku
{
    public static class SudokuCustomPuzzle
    {
        public static bool TryBuild(GridCore<SudokuCell> board, out SudokuPuzzle puzzle, out SudokuCustomPuzzleError error)
        {
            puzzle = null;

            if (SudokuSolver.FindConflicts(board).Count > 0)
            {
                error = SudokuCustomPuzzleError.ConflictingNumbers;
                return false;
            }

            var solutionCount = SudokuSolver.CountSolutions(board, 2);
            if (solutionCount == 0)
            {
                error = SudokuCustomPuzzleError.NoSolution;
                return false;
            }

            if (solutionCount > 1)
            {
                error = SudokuCustomPuzzleError.MultipleSolutions;
                return false;
            }

            if (!SudokuSolver.TrySolve(board, null, out var solution))
            {
                error = SudokuCustomPuzzleError.NoSolution;
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
            error = SudokuCustomPuzzleError.None;
            return true;
        }
    }
}
