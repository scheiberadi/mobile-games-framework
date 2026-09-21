using System;
using System.Collections.Generic;
using System.Linq;

namespace MobileGamesFramework.Grid
{
    public class GridCore<TCell> where TCell : struct
    {
        private readonly TCell?[,] _cells;

        public int Width { get; }
        public int Height { get; }

        public GridCore(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new TCell?[height, width];
        }

        public TCell? Get(GridPosition pos) => _cells[pos.Row, pos.Col];

        public void Set(GridPosition pos, TCell? value) => _cells[pos.Row, pos.Col] = value;

        public IEnumerable<GridPosition> AllPositions()
        {
            for (var row = 0; row < Height; row++)
            for (var col = 0; col < Width; col++)
                yield return new GridPosition(row, col);
        }

        public IEnumerable<GridPosition> GetEmptyPositions() => AllPositions().Where(p => Get(p) == null);

        public GridCore<TCell> Clone()
        {
            var clone = new GridCore<TCell>(Width, Height);
            foreach (var pos in AllPositions())
                clone.Set(pos, Get(pos));
            return clone;
        }

        public SlideResult<TCell> SlideAndMerge(Direction direction, IMergeRule<TCell> rule)
        {
            var merged = new List<TCell>();
            var moved = false;
            var lineCount = IsHorizontal(direction) ? Height : Width;

            for (var line = 0; line < lineCount; line++)
            {
                var positions = GetLinePositions(direction, line);
                var values = positions.Select(Get).Where(v => v.HasValue).Select(v => v.Value).ToList();
                var collapsed = CollapseLine(values, rule, merged);

                for (var i = 0; i < positions.Count; i++)
                {
                    TCell? newValue = i < collapsed.Count ? collapsed[i] : (TCell?)null;
                    if (!Equals(Get(positions[i]), newValue))
                        moved = true;
                    Set(positions[i], newValue);
                }
            }

            return new SlideResult<TCell> { Moved = moved, MergedResults = merged };
        }

        private static List<TCell> CollapseLine(List<TCell> values, IMergeRule<TCell> rule, List<TCell> mergedOut)
        {
            var result = new List<TCell>();
            var i = 0;
            while (i < values.Count)
            {
                if (i + 1 < values.Count && rule.CanMerge(values[i], values[i + 1]))
                {
                    var mergedValue = rule.Merge(values[i], values[i + 1]);
                    result.Add(mergedValue);
                    mergedOut.Add(mergedValue);
                    i += 2;
                }
                else
                {
                    result.Add(values[i]);
                    i += 1;
                }
            }
            return result;
        }

        private static bool IsHorizontal(Direction direction) => direction == Direction.Left || direction == Direction.Right;

        private List<GridPosition> GetLinePositions(Direction direction, int line)
        {
            var positions = new List<GridPosition>();
            switch (direction)
            {
                case Direction.Left:
                    for (var col = 0; col < Width; col++) positions.Add(new GridPosition(line, col));
                    break;
                case Direction.Right:
                    for (var col = Width - 1; col >= 0; col--) positions.Add(new GridPosition(line, col));
                    break;
                case Direction.Up:
                    for (var row = 0; row < Height; row++) positions.Add(new GridPosition(row, line));
                    break;
                case Direction.Down:
                    for (var row = Height - 1; row >= 0; row--) positions.Add(new GridPosition(row, line));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
            return positions;
        }
    }
}
