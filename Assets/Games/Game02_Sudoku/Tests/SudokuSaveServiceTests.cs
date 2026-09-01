using System.Collections.Generic;
using NUnit.Framework;
using MobileGamesFramework.Grid;
using MobileGamesFramework.Persistence;
using Game02_Sudoku;

namespace Game02_Sudoku.Tests
{
    public class SudokuSaveServiceTests
    {
        private class FakeKeyValueStore : IKeyValueStore
        {
            private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

            public string GetString(string key, string defaultValue) =>
                _values.TryGetValue(key, out var value) ? value : defaultValue;

            public void SetString(string key, string value) => _values[key] = value;
        }

        private static SudokuPuzzle SimplePuzzle()
        {
            var solution = SudokuBoardFactory.CreateEmpty();
            SudokuSolver.TrySolve(solution, new System.Random(1), out solution);

            var board = solution.Clone();
            var givenPos = new GridPosition(0, 0);

            foreach (var pos in board.AllPositions())
            {
                var cell = board.Get(pos).Value;
                cell.IsGiven = pos.Equals(givenPos);
                if (!cell.IsGiven) cell.Value = 0;
                board.Set(pos, cell);
            }

            return new SudokuPuzzle { Board = board, Solution = solution };
        }

        [Test]
        public void HasSave_Initially_ReturnsFalse()
        {
            var saveService = new SudokuSaveService(new FakeKeyValueStore());

            Assert.IsFalse(saveService.HasSave());
        }

        [Test]
        public void HasSave_AfterSave_ReturnsTrue()
        {
            var saveService = new SudokuSaveService(new FakeKeyValueStore());
            var game = new SudokuGame(SimplePuzzle());

            saveService.Save(game, Difficulty.Hard, 42f);

            Assert.IsTrue(saveService.HasSave());
        }

        [Test]
        public void ClearSave_RemovesSave()
        {
            var saveService = new SudokuSaveService(new FakeKeyValueStore());
            var game = new SudokuGame(SimplePuzzle());
            saveService.Save(game, Difficulty.Hard, 42f);

            saveService.ClearSave();

            Assert.IsFalse(saveService.HasSave());
        }

        [Test]
        public void TryLoad_WithNoSave_ReturnsFalse()
        {
            var saveService = new SudokuSaveService(new FakeKeyValueStore());

            var loaded = saveService.TryLoad(out var game, out _, out _);

            Assert.IsFalse(loaded);
            Assert.IsNull(game);
        }

        [Test]
        public void Save_ThenTryLoad_RestoresBoardDifficultyAndElapsedTime()
        {
            var puzzle = SimplePuzzle();
            var game = new SudokuGame(puzzle);
            var openPos = new GridPosition(0, 1);
            game.SetValue(openPos, puzzle.Solution.Get(openPos).Value.Value);
            var saveService = new SudokuSaveService(new FakeKeyValueStore());

            saveService.Save(game, Difficulty.Expert, 77.5f);
            var loaded = saveService.TryLoad(out var restored, out var difficulty, out var elapsedSeconds);

            Assert.IsTrue(loaded);
            Assert.AreEqual(Difficulty.Expert, difficulty);
            Assert.AreEqual(77.5f, elapsedSeconds);
            Assert.AreEqual(game.Board.Get(openPos).Value.Value, restored.Board.Get(openPos).Value.Value);
            Assert.AreEqual(game.Board.Get(new GridPosition(0, 0)).Value.Value, restored.Board.Get(new GridPosition(0, 0)).Value.Value);
        }

        [Test]
        public void Save_ThenTryLoad_RestoresHintsAutofillAndCustomFlag()
        {
            var puzzle = SimplePuzzle();
            var game = new SudokuGame(puzzle);
            game.FillHint(new System.Random(1));
            game.IsCustom = true;
            var saveService = new SudokuSaveService(new FakeKeyValueStore());

            saveService.Save(game, Difficulty.Medium, 10f);
            saveService.TryLoad(out var restored, out _, out _);

            Assert.AreEqual(game.HintsRemaining, restored.HintsRemaining);
            Assert.IsTrue(restored.IsCustom);
        }

        [Test]
        public void Save_ThenTryLoad_RestoresSolutionForFurtherHints()
        {
            var puzzle = SimplePuzzle();
            var game = new SudokuGame(puzzle);
            var saveService = new SudokuSaveService(new FakeKeyValueStore());
            saveService.Save(game, Difficulty.Medium, 10f);

            saveService.TryLoad(out var restored, out _, out _);
            restored.FillHint(new System.Random(1));

            Assert.IsEmpty(SudokuSolver.FindConflicts(restored.Board));
        }
    }
}
