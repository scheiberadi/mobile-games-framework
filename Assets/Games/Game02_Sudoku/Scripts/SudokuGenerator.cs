using System;
using System.Collections.Generic;
using System.Linq;

namespace Game02_Sudoku
{
    public static class SudokuGenerator
    {
        private static readonly Dictionary<Difficulty, int> TargetGivens = new Dictionary<Difficulty, int>
        {
            { Difficulty.Easy, 45 },
            { Difficulty.Medium, 36 },
            { Difficulty.Hard, 30 },
            { Difficulty.Expert, 24 },
        };

        public static SudokuPuzzle Generate(Difficulty difficulty, Random random)
        {
            var empty = SudokuBoardFactory.CreateEmpty();
            SudokuSolver.TrySolve(empty, random, out var solution);

            var board = solution.Clone();
            foreach (var pos in board.AllPositions())
            {
                var cell = board.Get(pos).Value;
                cell.IsGiven = true;
                board.Set(pos, cell);
            }

            var positions = board.AllPositions().ToList();
            positions.Shuffle(random);

            var targetGivens = TargetGivens[difficulty];
            var currentGivens = positions.Count;

            foreach (var pos in positions)
            {
                if (currentGivens <= targetGivens) break;

                var cell = board.Get(pos).Value;
                var previousValue = cell.Value;
                cell.Value = 0;
                cell.IsGiven = false;
                board.Set(pos, cell);

                if (SudokuSolver.CountSolutions(board, 2) == 1)
                {
                    currentGivens--;
                }
                else
                {
                    cell.Value = previousValue;
                    cell.IsGiven = true;
                    board.Set(pos, cell);
                }
            }

            return new SudokuPuzzle { Board = board, Solution = solution };
        }
    }
}
