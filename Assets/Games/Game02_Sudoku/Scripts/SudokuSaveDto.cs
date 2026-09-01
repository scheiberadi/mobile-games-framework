using System;
using MobileGamesFramework.Grid;

namespace Game02_Sudoku
{
    [Serializable]
    public class SudokuSaveDto
    {
        public int[] values;
        public int[] solutionValues;
        public bool[] isGiven;
        public int[] notesMask;
        public string difficulty;
        public int hintsRemaining;
        public bool hasUsedAutofill;
        public bool isCustom;
        public float elapsedSeconds;

        public static SudokuSaveDto From(SudokuGame game, Difficulty difficulty, float elapsedSeconds)
        {
            var board = game.Board;
            var solution = game.Solution;
            var count = SudokuBoardFactory.Size * SudokuBoardFactory.Size;

            var dto = new SudokuSaveDto
            {
                values = new int[count],
                solutionValues = new int[count],
                isGiven = new bool[count],
                notesMask = new int[count],
                difficulty = difficulty.ToString(),
                hintsRemaining = game.HintsRemaining,
                hasUsedAutofill = game.HasUsedAutofill,
                isCustom = game.IsCustom,
                elapsedSeconds = elapsedSeconds
            };

            var i = 0;
            foreach (var pos in board.AllPositions())
            {
                var cell = board.Get(pos).Value;
                dto.values[i] = cell.Value;
                dto.isGiven[i] = cell.IsGiven;
                dto.notesMask[i] = cell.NotesMask;
                dto.solutionValues[i] = solution.Get(pos).Value.Value;
                i++;
            }

            return dto;
        }

        public SudokuGame ToGame(out Difficulty difficulty, out float elapsedSeconds)
        {
            var board = SudokuBoardFactory.CreateEmpty();
            var solution = SudokuBoardFactory.CreateEmpty();

            var i = 0;
            foreach (var pos in board.AllPositions())
            {
                var cell = new SudokuCell { Value = values[i], IsGiven = isGiven[i], NotesMask = notesMask[i] };
                board.Set(pos, cell);
                solution.Set(pos, new SudokuCell { Value = solutionValues[i] });
                i++;
            }

            difficulty = (Difficulty)Enum.Parse(typeof(Difficulty), this.difficulty);
            elapsedSeconds = this.elapsedSeconds;
            return SudokuGame.Restore(board, solution, hintsRemaining, hasUsedAutofill, isCustom);
        }
    }
}
