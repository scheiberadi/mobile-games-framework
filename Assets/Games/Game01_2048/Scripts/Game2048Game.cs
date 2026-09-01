using System.Linq;
using MobileGamesFramework.Grid;

namespace Game01_2048
{
    public class Game2048Game
    {
        private const int WinValue = 2048;
        private static readonly Direction[] AllDirections = { Direction.Up, Direction.Down, Direction.Left, Direction.Right };

        private readonly ITileSpawnStrategy _spawner;
        private readonly Game2048MergeRule _mergeRule = new Game2048MergeRule();

        public GridCore<int> Grid { get; private set; }
        public int Score { get; private set; }
        public GameState State { get; private set; }

        public Game2048Game(ITileSpawnStrategy spawner, int width = 4, int height = 4)
        {
            _spawner = spawner;
            Grid = new GridCore<int>(width, height);
        }

        public void NewGame()
        {
            Grid = new GridCore<int>(Grid.Width, Grid.Height);
            Score = 0;
            State = GameState.Playing;
            _spawner.Spawn(Grid);
            _spawner.Spawn(Grid);
        }

        public bool ApplyMove(Direction direction)
        {
            if (State != GameState.Playing)
                return false;

            var result = Grid.SlideAndMerge(direction, _mergeRule);
            if (!result.Moved)
                return false;

            Score += result.MergedResults.Sum();
            _spawner.Spawn(Grid);
            UpdateState();
            return true;
        }

        private void UpdateState()
        {
            if (Grid.AllPositions().Any(p => Grid.Get(p) == WinValue))
            {
                State = GameState.Won;
                return;
            }

            if (!HasAnyValidMove())
                State = GameState.Lost;
        }

        private bool HasAnyValidMove()
        {
            return AllDirections.Any(direction => Grid.Clone().SlideAndMerge(direction, _mergeRule).Moved);
        }
    }
}
