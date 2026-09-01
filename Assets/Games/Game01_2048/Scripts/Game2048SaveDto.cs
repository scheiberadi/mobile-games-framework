using System;
using MobileGamesFramework.Grid;

namespace Game01_2048
{
    [Serializable]
    public class Game2048SaveDto
    {
        public int width;
        public int height;
        public int score;
        public int[] cells;

        public static Game2048SaveDto From(Game2048Game game)
        {
            var grid = game.Grid;
            var cells = new int[grid.Width * grid.Height];
            var i = 0;
            foreach (var pos in grid.AllPositions())
                cells[i++] = grid.Get(pos) ?? -1;

            return new Game2048SaveDto { width = grid.Width, height = grid.Height, score = game.Score, cells = cells };
        }

        public Game2048Game ToGame(ITileSpawnStrategy spawner)
        {
            var grid = new GridCore<int>(width, height);
            var i = 0;
            foreach (var pos in grid.AllPositions())
            {
                var value = cells[i++];
                if (value >= 0) grid.Set(pos, value);
            }

            var game = new Game2048Game(spawner, width, height);
            game.RestoreState(grid, score);
            return game;
        }
    }
}
