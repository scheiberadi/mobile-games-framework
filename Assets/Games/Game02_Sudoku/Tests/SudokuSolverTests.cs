using System.Linq;
using NUnit.Framework;
using MobileGamesFramework.Grid;
using Game02_Sudoku;

namespace Game02_Sudoku.Tests
{
    public class SudokuSolverTests
    {
        [Test]
        public void TrySolve_EmptyBoard_ProducesCompleteConflictFreeSolution()
        {
            var board = SudokuBoardFactory.CreateEmpty();

            var solved = SudokuSolver.TrySolve(board, new System.Random(1), out var solution);

            Assert.IsTrue(solved);
            Assert.IsTrue(solution.AllPositions().All(p => solution.Get(p).Value.Value != 0));
            Assert.IsEmpty(SudokuSolver.FindConflicts(solution));
        }

        [Test]
        public void TrySolve_BoardWithNoValidSolution_ReturnsFalse()
        {
            var board = SudokuBoardFactory.CreateEmpty();
            SetValue(board, 0, 0, 5);
            SetValue(board, 0, 1, 5);

            var solved = SudokuSolver.TrySolve(board, new System.Random(1), out _);

            Assert.IsFalse(solved);
        }

        [Test]
        public void CountSolutions_FullySolvedBoard_ReturnsOne()
        {
            var board = SudokuBoardFactory.CreateEmpty();
            SudokuSolver.TrySolve(board, new System.Random(1), out var solved);

            var count = SudokuSolver.CountSolutions(solved, 2);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void CountSolutions_EmptyBoard_HasMoreThanOneSolution()
        {
            var board = SudokuBoardFactory.CreateEmpty();

            var count = SudokuSolver.CountSolutions(board, 2);

            Assert.AreEqual(2, count);
        }

        [Test]
        public void FindConflicts_ValidSolvedBoard_ReturnsEmpty()
        {
            var board = SudokuBoardFactory.CreateEmpty();
            SudokuSolver.TrySolve(board, new System.Random(1), out var solved);

            Assert.IsEmpty(SudokuSolver.FindConflicts(solved));
        }

        [Test]
        public void FindConflicts_DuplicateInRow_ReturnsBothPositions()
        {
            var board = SudokuBoardFactory.CreateEmpty();
            SetValue(board, 0, 0, 7);
            SetValue(board, 0, 4, 7);

            var conflicts = SudokuSolver.FindConflicts(board);

            CollectionAssert.Contains(conflicts, new GridPosition(0, 0));
            CollectionAssert.Contains(conflicts, new GridPosition(0, 4));
        }

        [Test]
        public void FindConflicts_DuplicateInBox_ReturnsBothPositions()
        {
            var board = SudokuBoardFactory.CreateEmpty();
            SetValue(board, 0, 0, 3);
            SetValue(board, 2, 2, 3);

            var conflicts = SudokuSolver.FindConflicts(board);

            CollectionAssert.Contains(conflicts, new GridPosition(0, 0));
            CollectionAssert.Contains(conflicts, new GridPosition(2, 2));
        }

        private static void SetValue(GridCore<SudokuCell> board, int row, int col, int value)
        {
            var cell = board.Get(new GridPosition(row, col)).Value;
            cell.Value = value;
            board.Set(new GridPosition(row, col), cell);
        }
    }
}
