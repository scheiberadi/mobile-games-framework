using System.Linq;
using NUnit.Framework;
using Game02_Sudoku;

namespace Game02_Sudoku.Tests
{
    public class SudokuGeneratorTests
    {
        [Test]
        public void Generate_ProducesAUniquelySolvableBoard()
        {
            var puzzle = SudokuGenerator.Generate(Difficulty.Easy, new System.Random(1));

            Assert.AreEqual(1, SudokuSolver.CountSolutions(puzzle.Board, 2));
        }

        [Test]
        public void Generate_GivenCellsHaveNoConflicts()
        {
            var puzzle = SudokuGenerator.Generate(Difficulty.Medium, new System.Random(1));

            Assert.IsEmpty(SudokuSolver.FindConflicts(puzzle.Board));
        }

        [Test]
        public void Generate_SolutionIsAFullyConflictFreeBoard()
        {
            var puzzle = SudokuGenerator.Generate(Difficulty.Hard, new System.Random(1));

            Assert.IsTrue(puzzle.Solution.AllPositions().All(p => puzzle.Solution.Get(p).Value.Value != 0));
            Assert.IsEmpty(SudokuSolver.FindConflicts(puzzle.Solution));
        }

        [Test]
        public void Generate_GivenCellsMatchTheSolution()
        {
            var puzzle = SudokuGenerator.Generate(Difficulty.Expert, new System.Random(1));

            foreach (var pos in puzzle.Board.AllPositions())
            {
                var cell = puzzle.Board.Get(pos).Value;
                if (cell.IsGiven)
                    Assert.AreEqual(puzzle.Solution.Get(pos).Value.Value, cell.Value);
            }
        }

        [Test]
        public void Generate_HarderDifficulty_HasFewerGivens()
        {
            var easy = SudokuGenerator.Generate(Difficulty.Easy, new System.Random(1));
            var expert = SudokuGenerator.Generate(Difficulty.Expert, new System.Random(1));

            var easyGivens = easy.Board.AllPositions().Count(p => easy.Board.Get(p).Value.IsGiven);
            var expertGivens = expert.Board.AllPositions().Count(p => expert.Board.Get(p).Value.IsGiven);

            Assert.Greater(easyGivens, expertGivens);
        }
    }
}
