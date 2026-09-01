using System.Linq;
using NUnit.Framework;
using MobileGamesFramework.Grid;
using Game01_2048;

namespace Game01_2048.Tests
{
    public class Game2048GameTests
    {
        private class FixedValueSpawner : ITileSpawnStrategy
        {
            private readonly int _value;
            public FixedValueSpawner(int value) { _value = value; }

            public void Spawn(GridCore<int> grid)
            {
                var empty = grid.GetEmptyPositions().FirstOrDefault();
                if (grid.GetEmptyPositions().Any())
                    grid.Set(empty, _value);
            }
        }

        private class NoSpawn : ITileSpawnStrategy
        {
            public void Spawn(GridCore<int> grid) { }
        }

        [Test]
        public void NewGame_StartsWithTwoTilesScoreZeroPlaying()
        {
            var game = new Game2048Game(new FixedValueSpawner(2), 4, 4);

            game.NewGame();

            Assert.AreEqual(14, game.Grid.GetEmptyPositions().Count());
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(GameState.Playing, game.State);
        }

        [Test]
        public void ApplyMove_MergingTiles_AddsMergedValueToScore()
        {
            var game = new Game2048Game(new NoSpawn(), 2, 1);
            game.Grid.Set(new GridPosition(0, 0), 2);
            game.Grid.Set(new GridPosition(0, 1), 2);

            game.ApplyMove(Direction.Left);

            Assert.AreEqual(4, game.Score);
        }

        [Test]
        public void ApplyMove_ThatChangesBoard_SpawnsANewTile()
        {
            var game = new Game2048Game(new FixedValueSpawner(4), 2, 1);
            game.Grid.Set(new GridPosition(0, 1), 2);

            game.ApplyMove(Direction.Left);

            Assert.AreEqual(0, game.Grid.GetEmptyPositions().Count());
        }

        [Test]
        public void ApplyMove_ThatDoesNothing_DoesNotSpawn()
        {
            var game = new Game2048Game(new FixedValueSpawner(4), 2, 1);
            game.Grid.Set(new GridPosition(0, 0), 2);

            var moved = game.ApplyMove(Direction.Left);

            Assert.IsFalse(moved);
            Assert.AreEqual(1, game.Grid.GetEmptyPositions().Count());
        }

        [Test]
        public void ApplyMove_ReachingWinValue_SetsStateWon()
        {
            var game = new Game2048Game(new FixedValueSpawner(2), 2, 1);
            game.Grid.Set(new GridPosition(0, 0), 1024);
            game.Grid.Set(new GridPosition(0, 1), 1024);

            game.ApplyMove(Direction.Left);

            Assert.AreEqual(GameState.Won, game.State);
        }

        [Test]
        public void ApplyMove_FillingBoardWithNoMovesLeft_SetsStateLost()
        {
            var game = new Game2048Game(new FixedValueSpawner(4), 2, 1);
            game.Grid.Set(new GridPosition(0, 1), 2);

            game.ApplyMove(Direction.Left);

            Assert.AreEqual(GameState.Lost, game.State);
        }

        [Test]
        public void CanUndo_InitiallyFalse()
        {
            var game = new Game2048Game(new NoSpawn(), 2, 1);

            Assert.IsFalse(game.CanUndo);
        }

        [Test]
        public void CanUndo_TrueAfterAMove()
        {
            var game = new Game2048Game(new NoSpawn(), 2, 1);
            game.Grid.Set(new GridPosition(0, 0), 2);
            game.Grid.Set(new GridPosition(0, 1), 2);

            game.ApplyMove(Direction.Left);

            Assert.IsTrue(game.CanUndo);
        }

        [Test]
        public void CanUndo_FalseAfterUndoing()
        {
            var game = new Game2048Game(new NoSpawn(), 2, 1);
            game.Grid.Set(new GridPosition(0, 0), 2);
            game.Grid.Set(new GridPosition(0, 1), 2);
            game.ApplyMove(Direction.Left);

            game.Undo();

            Assert.IsFalse(game.CanUndo);
        }

        [Test]
        public void Undo_WithNoPriorMove_ReturnsFalse()
        {
            var game = new Game2048Game(new NoSpawn(), 2, 1);
            game.Grid.Set(new GridPosition(0, 0), 2);

            Assert.IsFalse(game.Undo());
        }

        [Test]
        public void Undo_AfterAMove_RestoresPreviousGridAndScore()
        {
            var game = new Game2048Game(new NoSpawn(), 2, 1);
            game.Grid.Set(new GridPosition(0, 0), 2);
            game.Grid.Set(new GridPosition(0, 1), 2);
            game.ApplyMove(Direction.Left);

            var undone = game.Undo();

            Assert.IsTrue(undone);
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(2, game.Grid.Get(new GridPosition(0, 0)));
            Assert.AreEqual(2, game.Grid.Get(new GridPosition(0, 1)));
            Assert.AreEqual(GameState.Playing, game.State);
        }

        [Test]
        public void Undo_CalledTwiceInARow_SecondCallReturnsFalse()
        {
            var game = new Game2048Game(new NoSpawn(), 2, 1);
            game.Grid.Set(new GridPosition(0, 0), 2);
            game.Grid.Set(new GridPosition(0, 1), 2);
            game.ApplyMove(Direction.Left);
            game.Undo();

            Assert.IsFalse(game.Undo());
        }

        [Test]
        public void Undo_AfterMoveThatWon_RevertsToPlaying()
        {
            var game = new Game2048Game(new FixedValueSpawner(2), 2, 1);
            game.Grid.Set(new GridPosition(0, 0), 1024);
            game.Grid.Set(new GridPosition(0, 1), 1024);
            game.ApplyMove(Direction.Left);

            var undone = game.Undo();

            Assert.IsTrue(undone);
            Assert.AreEqual(GameState.Playing, game.State);
        }

        [Test]
        public void NewGame_ClearsPreviousUndoSnapshot()
        {
            var game = new Game2048Game(new FixedValueSpawner(2), 2, 1);
            game.Grid.Set(new GridPosition(0, 0), 2);
            game.Grid.Set(new GridPosition(0, 1), 2);
            game.ApplyMove(Direction.Left);

            game.NewGame();

            Assert.IsFalse(game.Undo());
        }

        [Test]
        public void RestoreState_SetsGridScoreAndPlayingState()
        {
            var game = new Game2048Game(new NoSpawn(), 2, 1);
            var grid = new GridCore<int>(2, 1);
            grid.Set(new GridPosition(0, 0), 8);

            game.RestoreState(grid, 40);

            Assert.AreSame(grid, game.Grid);
            Assert.AreEqual(40, game.Score);
            Assert.AreEqual(GameState.Playing, game.State);
        }

        [Test]
        public void ApplyMove_WhenGameAlreadyOver_DoesNothing()
        {
            var game = new Game2048Game(new FixedValueSpawner(4), 2, 1);
            game.Grid.Set(new GridPosition(0, 0), 1024);
            game.Grid.Set(new GridPosition(0, 1), 1024);
            game.ApplyMove(Direction.Left);

            var movedAfterWin = game.ApplyMove(Direction.Right);

            Assert.IsFalse(movedAfterWin);
        }
    }
}
