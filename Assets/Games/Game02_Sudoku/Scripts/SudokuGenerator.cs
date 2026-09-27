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

            // Carve on a flat digit array: each removal needs a uniqueness check, and doing those
            // on GridCore clones was the bulk of the generation time.
            var grid = SudokuSolver.ToArray(solution);
            var order = Enumerable.Range(0, grid.Length).ToList();
            order.Shuffle(random);

            var targetGivens = TargetGivens[difficulty];
            var currentGivens = grid.Length;

            foreach (var index in order)
            {
                if (currentGivens <= targetGivens) break;

                var previousValue = grid[index];
                grid[index] = 0;

                if (SudokuSolver.CountSolutions(grid, 2) == 1) currentGivens--;
                else grid[index] = previousValue;
            }

            var board = solution.Clone();
            for (var i = 0; i < grid.Length; i++)
            {
                var pos = new MobileGamesFramework.Grid.GridPosition(i / SudokuBoardFactory.Size, i % SudokuBoardFactory.Size);
                var cell = board.Get(pos).Value;
                cell.Value = grid[i];
                cell.IsGiven = grid[i] != 0;
                board.Set(pos, cell);
            }

            return new SudokuPuzzle { Board = board, Solution = solution };
        }
    }
}
