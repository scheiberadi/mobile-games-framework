using System.Collections.Generic;
using System.Linq;
using MobileGamesFramework.Grid;
using MobileGamesFramework.Undo;

namespace Game02_Sudoku
{
    public class SudokuGame
    {
        private readonly GridCore<SudokuCell> _solution;
        private readonly UndoStack<GridCore<SudokuCell>> _undoStack = new UndoStack<GridCore<SudokuCell>>();

        public GridCore<SudokuCell> Board { get; private set; }
        public bool CanUndo => _undoStack.CanUndo;
        public List<GridPosition> Conflicts => SudokuSolver.FindConflicts(Board);
        public bool IsComplete => Board.AllPositions().All(p => Board.Get(p).Value.Value != 0) && Conflicts.Count == 0;

        public SudokuGame(SudokuPuzzle puzzle)
        {
            Board = puzzle.Board;
            _solution = puzzle.Solution;
        }

        public bool SetValue(GridPosition pos, int value)
        {
            var cell = Board.Get(pos).Value;
            if (cell.IsGiven) return false;

            _undoStack.Push(Board.Clone());
            cell.Value = value;
            cell.NotesMask = 0;
            Board.Set(pos, cell);
            return true;
        }

        public bool ToggleNote(GridPosition pos, int number)
        {
            var cell = Board.Get(pos).Value;
            if (cell.IsGiven) return false;

            _undoStack.Push(Board.Clone());
            cell.NotesMask ^= 1 << (number - 1);
            Board.Set(pos, cell);
            return true;
        }

        public bool Erase(GridPosition pos)
        {
            var cell = Board.Get(pos).Value;
            if (cell.IsGiven) return false;
            if (cell.Value == 0 && cell.NotesMask == 0) return false;

            _undoStack.Push(Board.Clone());
            cell.Value = 0;
            cell.NotesMask = 0;
            Board.Set(pos, cell);
            return true;
        }

        public bool Undo()
        {
            if (!_undoStack.TryPop(out var previous)) return false;
            Board = previous;
            return true;
        }
    }
}
