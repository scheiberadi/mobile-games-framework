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
        private readonly int _undoCreditsPerGame;

        private GridCore<int> _previousGrid;
        private int _previousScore;
        private bool _hasUndoSnapshot;

        public GridCore<int> Grid { get; private set; }
        public int Score { get; private set; }
        public GameState State { get; private set; }
        public int UndoCredits { get; private set; }
        public bool CanUndo => _hasUndoSnapshot && UndoCredits > 0;

        public Game2048Game(ITileSpawnStrategy spawner, int width = 4, int height = 4, int undoCreditsPerGame = 3)
        {
            _spawner = spawner;
            _undoCreditsPerGame = undoCreditsPerGame;
            Grid = new GridCore<int>(width, height);
            UndoCredits = undoCreditsPerGame;
        }

        public void RestoreState(GridCore<int> grid, int score)
        {
            Grid = grid;
            Score = score;
            State = GameState.Playing;
            _hasUndoSnapshot = false;
            UndoCredits = _undoCreditsPerGame;
        }

        public void NewGame()
        {
            Grid = new GridCore<int>(Grid.Width, Grid.Height);
            Score = 0;
            State = GameState.Playing;
            _hasUndoSnapshot = false;
            UndoCredits = _undoCreditsPerGame;
            _spawner.Spawn(Grid);
            _spawner.Spawn(Grid);
        }

        public void GrantExtraUndo()
        {
            UndoCredits++;
        }

        public bool ApplyMove(Direction direction)
        {
            if (State != GameState.Playing)
                return false;

            var gridBeforeMove = Grid.Clone();
            var scoreBeforeMove = Score;

            var result = Grid.SlideAndMerge(direction, _mergeRule);
            if (!result.Moved)
                return false;

            _previousGrid = gridBeforeMove;
            _previousScore = scoreBeforeMove;
            _hasUndoSnapshot = true;

            Score += result.MergedResults.Sum();
            _spawner.Spawn(Grid);
            UpdateState();
            return true;
        }

        public bool Undo()
        {
            if (!CanUndo)
                return false;

            Grid = _previousGrid;
            Score = _previousScore;
            State = GameState.Playing;
            _hasUndoSnapshot = false;
            UndoCredits--;
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
