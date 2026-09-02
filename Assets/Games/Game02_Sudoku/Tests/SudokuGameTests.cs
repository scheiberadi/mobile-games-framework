using NUnit.Framework;
using MobileGamesFramework.Grid;
using Game02_Sudoku;

namespace Game02_Sudoku.Tests
{
    public class SudokuGameTests
    {
        private static SudokuPuzzle SimplePuzzle()
        {
            var solution = SudokuBoardFactory.CreateEmpty();
            SudokuSolver.TrySolve(solution, new System.Random(1), out solution);

            var board = solution.Clone();
            var givenPos = new GridPosition(0, 0);
            var openPos = new GridPosition(0, 1);

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
        public void SetValue_OnEmptyCell_SetsTheValue()
        {
            var game = new SudokuGame(SimplePuzzle());
            var pos = new GridPosition(0, 1);

            var result = game.SetValue(pos, 5);

            Assert.IsTrue(result);
            Assert.AreEqual(5, game.Board.Get(pos).Value.Value);
        }

        [Test]
        public void SetValue_OnGivenCell_Fails()
        {
            var game = new SudokuGame(SimplePuzzle());
            var pos = new GridPosition(0, 0);

            var result = game.SetValue(pos, 9);

            Assert.IsFalse(result);
        }

        [Test]
        public void SetValue_ClearsAnyExistingNotes()
        {
            var game = new SudokuGame(SimplePuzzle());
            var pos = new GridPosition(0, 1);
            game.ToggleNote(pos, 3);

            game.SetValue(pos, 5);

            Assert.AreEqual(0, game.Board.Get(pos).Value.NotesMask);
        }

        [Test]
        public void ToggleNote_OnEmptyCell_SetsTheNoteBit()
        {
            var game = new SudokuGame(SimplePuzzle());
            var pos = new GridPosition(0, 1);

            game.ToggleNote(pos, 4);

            Assert.AreEqual(1 << 3, game.Board.Get(pos).Value.NotesMask);
        }

        [Test]
        public void ToggleNote_Twice_ClearsTheNoteBit()
        {
            var game = new SudokuGame(SimplePuzzle());
            var pos = new GridPosition(0, 1);
            game.ToggleNote(pos, 4);

            game.ToggleNote(pos, 4);

            Assert.AreEqual(0, game.Board.Get(pos).Value.NotesMask);
        }

        [Test]
        public void Erase_ClearsValueAndNotes()
        {
            var game = new SudokuGame(SimplePuzzle());
            var pos = new GridPosition(0, 1);
            game.SetValue(pos, 5);

            var result = game.Erase(pos);

            Assert.IsTrue(result);
            Assert.AreEqual(0, game.Board.Get(pos).Value.Value);
        }

        [Test]
        public void Erase_OnGivenCell_Fails()
        {
            var game = new SudokuGame(SimplePuzzle());

            var result = game.Erase(new GridPosition(0, 0));

            Assert.IsFalse(result);
        }

        [Test]
        public void Undo_WithNoPriorChange_ReturnsFalse()
        {
            var game = new SudokuGame(SimplePuzzle());

            Assert.IsFalse(game.Undo());
        }

        [Test]
        public void Undo_AfterSetValue_RevertsTheValue()
        {
            var game = new SudokuGame(SimplePuzzle());
            var pos = new GridPosition(0, 1);
            game.SetValue(pos, 5);

            var undone = game.Undo();

            Assert.IsTrue(undone);
            Assert.AreEqual(0, game.Board.Get(pos).Value.Value);
        }

        [Test]
        public void Undo_MultipleSteps_RevertsEachInOrder()
        {
            var game = new SudokuGame(SimplePuzzle());
            var pos = new GridPosition(0, 1);
            game.SetValue(pos, 5);
            game.SetValue(pos, 7);

            game.Undo();
            Assert.AreEqual(5, game.Board.Get(pos).Value.Value);

            game.Undo();
            Assert.AreEqual(0, game.Board.Get(pos).Value.Value);
        }

        [Test]
        public void Conflicts_WithConflictingValues_AreReported()
        {
            var game = new SudokuGame(SimplePuzzle());
            var givenValue = game.Board.Get(new GridPosition(0, 0)).Value.Value;

            game.SetValue(new GridPosition(0, 1), givenValue);

            Assert.IsNotEmpty(game.Conflicts);
        }

        [Test]
        public void IsComplete_WhenBoardMatchesSolutionFully_IsTrue()
        {
            var puzzle = SimplePuzzle();
            var game = new SudokuGame(puzzle);

            foreach (var pos in puzzle.Board.AllPositions())
            {
                if (!puzzle.Board.Get(pos).Value.IsGiven)
                    game.SetValue(pos, puzzle.Solution.Get(pos).Value.Value);
            }

            Assert.IsTrue(game.IsComplete);
        }

        [Test]
        public void IsComplete_WithEmptyCellsRemaining_IsFalse()
        {
            var game = new SudokuGame(SimplePuzzle());

            Assert.IsFalse(game.IsComplete);
        }

        [Test]
        public void HintsRemaining_DefaultsToThree()
        {
            var game = new SudokuGame(SimplePuzzle());

            Assert.AreEqual(3, game.HintsRemaining);
        }

        [Test]
        public void FillHint_FillsOneEmptyCellFromSolutionAndDecrementsCount()
        {
            var puzzle = SimplePuzzle();
            var game = new SudokuGame(puzzle);
            var filledBefore = FilledCellCount(game.Board);

            var result = game.FillHint(new System.Random(1));

            Assert.IsTrue(result);
            Assert.AreEqual(2, game.HintsRemaining);
            Assert.AreEqual(filledBefore + 1, FilledCellCount(game.Board));
            Assert.IsEmpty(SudokuSolver.FindConflicts(game.Board));
        }

        [Test]
        public void FillHint_WhenNoHintsRemaining_ReturnsFalse()
        {
            var game = new SudokuGame(SimplePuzzle());
            game.FillHint(new System.Random(1));
            game.FillHint(new System.Random(1));
            game.FillHint(new System.Random(1));

            var result = game.FillHint(new System.Random(1));

            Assert.IsFalse(result);
            Assert.AreEqual(0, game.HintsRemaining);
        }

        [Test]
        public void GrantExtraHint_IncreasesHintsRemaining()
        {
            var game = new SudokuGame(SimplePuzzle());
            game.FillHint(new System.Random(1));
            game.FillHint(new System.Random(1));
            game.FillHint(new System.Random(1));

            game.GrantExtraHint();

            Assert.AreEqual(1, game.HintsRemaining);
            Assert.IsTrue(game.FillHint(new System.Random(1)));
        }

        [Test]
        public void HasUsedAutofill_InitiallyFalse()
        {
            var game = new SudokuGame(SimplePuzzle());

            Assert.IsFalse(game.HasUsedAutofill);
        }

        [Test]
        public void AutofillRemaining_FillsAllEmptyCellsAndSetsFlag()
        {
            var puzzle = SimplePuzzle();
            var game = new SudokuGame(puzzle);

            game.AutofillRemaining();

            Assert.IsTrue(game.HasUsedAutofill);
            Assert.IsTrue(game.IsComplete);
        }

        [Test]
        public void Restore_RebuildsGameWithGivenBoardAndSolution()
        {
            var puzzle = SimplePuzzle();

            var game = SudokuGame.Restore(puzzle.Board, puzzle.Solution, 2, false, false);

            Assert.AreEqual(puzzle.Board.Get(new GridPosition(0, 0)).Value.Value,
                game.Board.Get(new GridPosition(0, 0)).Value.Value);
        }

        [Test]
        public void Restore_SetsHintsRemainingAutofillAndCustomFlag()
        {
            var puzzle = SimplePuzzle();

            var game = SudokuGame.Restore(puzzle.Board, puzzle.Solution, 1, true, true);

            Assert.AreEqual(1, game.HintsRemaining);
            Assert.IsTrue(game.HasUsedAutofill);
            Assert.IsTrue(game.IsCustom);
        }

        [Test]
        public void Restore_FillHint_UsesTheRestoredSolution()
        {
            var puzzle = SimplePuzzle();

            var game = SudokuGame.Restore(puzzle.Board, puzzle.Solution, 3, false, false);
            var result = game.FillHint(new System.Random(1));

            Assert.IsTrue(result);
            Assert.IsEmpty(SudokuSolver.FindConflicts(game.Board));
        }

        [Test]
        public void ClearEntries_RemovesPlayerEnteredValuesAndNotes()
        {
            var game = new SudokuGame(SimplePuzzle());
            var pos = new GridPosition(0, 1);
            game.SetValue(pos, 5);
            game.ToggleNote(new GridPosition(0, 2), 3);

            game.ClearEntries();

            Assert.AreEqual(0, game.Board.Get(pos).Value.Value);
            Assert.AreEqual(0, game.Board.Get(new GridPosition(0, 2)).Value.NotesMask);
        }

        [Test]
        public void ClearEntries_LeavesGivenCellsUntouched()
        {
            var puzzle = SimplePuzzle();
            var game = new SudokuGame(puzzle);
            var givenPos = new GridPosition(0, 0);
            var givenValue = puzzle.Board.Get(givenPos).Value.Value;

            game.ClearEntries();

            Assert.AreEqual(givenValue, game.Board.Get(givenPos).Value.Value);
        }

        [Test]
        public void FindIncorrectEntries_EmptyBoard_ReturnsNoMistakes()
        {
            var game = new SudokuGame(SimplePuzzle());

            Assert.IsEmpty(game.FindIncorrectEntries());
        }

        [Test]
        public void FindIncorrectEntries_CorrectEntry_IsNotReported()
        {
            var puzzle = SimplePuzzle();
            var game = new SudokuGame(puzzle);
            var pos = new GridPosition(0, 1);
            game.SetValue(pos, puzzle.Solution.Get(pos).Value.Value);

            Assert.IsEmpty(game.FindIncorrectEntries());
        }

        [Test]
        public void FindIncorrectEntries_WrongEntry_IsReported()
        {
            var puzzle = SimplePuzzle();
            var game = new SudokuGame(puzzle);
            var pos = new GridPosition(0, 1);
            var correctValue = puzzle.Solution.Get(pos).Value.Value;
            var wrongValue = correctValue == 9 ? 1 : correctValue + 1;
            game.SetValue(pos, wrongValue);

            var mistakes = game.FindIncorrectEntries();

            Assert.AreEqual(1, mistakes.Count);
            Assert.IsTrue(mistakes.Contains(pos));
        }

        private static int FilledCellCount(GridCore<SudokuCell> board)
        {
            var count = 0;
            foreach (var pos in board.AllPositions())
                if (board.Get(pos).Value.Value != 0) count++;
            return count;
        }
    }
}
