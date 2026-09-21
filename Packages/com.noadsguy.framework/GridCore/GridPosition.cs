namespace MobileGamesFramework.Grid
{
    public readonly struct GridPosition
    {
        public readonly int Row;
        public readonly int Col;

        public GridPosition(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public override bool Equals(object obj) => obj is GridPosition other && other.Row == Row && other.Col == Col;

        public override int GetHashCode() => (Row * 397) ^ Col;
    }
}
