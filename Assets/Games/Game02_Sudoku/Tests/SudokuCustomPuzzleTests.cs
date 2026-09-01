using System.Linq;
using NUnit.Framework;
using MobileGamesFramework.Grid;
using Game02_Sudoku;

namespace Game02_Sudoku.Tests
{
    public class SudokuCustomPuzzleTests
    {
        [Test]
        public void TryBuild_BoardWithConflict_Fails()
        {
            var board = SudokuBoardFactory.CreateEmpty();
            Set(board, 0, 0, 5);
            Set(board, 0, 1, 5);

            var success = SudokuCustomPuzzle.TryBuild(board, out var puzzle, out var error);

            Assert.IsFalse(success);
            Assert.IsNull(puzzle);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void TryBuild_EmptyBoard_FailsAsNotUnique()
        {
            var board = SudokuBoardFactory.CreateEmpty();

            var success = SudokuCustomPuzzle.TryBuild(board, out var puzzle, out var error);

            Assert.IsFalse(success);
            Assert.IsNull(puzzle);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void TryBuild_ValidUniqueBoard_Succeeds()
        {
            var generated = SudokuGenerator.Generate(Difficulty.Easy, new System.Random(1));

            var success = SudokuCustomPuzzle.TryBuild(generated.Board, out var puzzle, out var error);

            Assert.IsTrue(success);
            Assert.IsNotNull(puzzle);
            Assert.IsNull(error);
        }

        [Test]
        public void TryBuild_ValidUniqueBoard_FilledCellsBecomeGivenAndMatchSolution()
        {
            var generated = SudokuGenerator.Generate(Difficulty.Easy, new System.Random(1));

            SudokuCustomPuzzle.TryBuild(generated.Board, out var puzzle, out _);

            foreach (var pos in generated.Board.AllPositions())
            {
                var sourceCell = generated.Board.Get(pos).Value;
                var builtCell = puzzle.Board.Get(pos).Value;

                if (sourceCell.Value != 0)
                {
                    Assert.IsTrue(builtCell.IsGiven);
                    Assert.AreEqual(sourceCell.Value, builtCell.Value);
                }
                else
                {
                    Assert.IsFalse(builtCell.IsGiven);
                }
            }
        }

        [Test]
        public void TryBuild_ValidUniqueBoard_SolutionIsFullyConflictFree()
        {
            var generated = SudokuGenerator.Generate(Difficulty.Easy, new System.Random(1));

            SudokuCustomPuzzle.TryBuild(generated.Board, out var puzzle, out _);

            Assert.IsTrue(puzzle.Solution.AllPositions().All(p => puzzle.Solution.Get(p).Value.Value != 0));
            Assert.IsEmpty(SudokuSolver.FindConflicts(puzzle.Solution));
        }

        private static void Set(GridCore<SudokuCell> board, int row, int col, int value)
        {
            var pos = new GridPosition(row, col);
            var cell = board.Get(pos).Value;
            cell.Value = value;
            board.Set(pos, cell);
        }
    }
}
