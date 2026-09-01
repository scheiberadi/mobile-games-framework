using MobileGamesFramework.Grid;

namespace Game02_Sudoku
{
    public static class SudokuBoardFactory
    {
        public const int Size = 9;

        public static GridCore<SudokuCell> CreateEmpty()
        {
            var board = new GridCore<SudokuCell>(Size, Size);
            foreach (var pos in board.AllPositions())
                board.Set(pos, new SudokuCell());
            return board;
        }
    }
}
