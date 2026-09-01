using System;
using System.Linq;
using MobileGamesFramework.Grid;

namespace Game01_2048
{
    public class Game2048SpawnStrategy : ITileSpawnStrategy
    {
        private readonly Random _random;

        public Game2048SpawnStrategy(Random random)
        {
            _random = random;
        }

        public void Spawn(GridCore<int> grid)
        {
            var empties = grid.GetEmptyPositions().ToList();
            if (empties.Count == 0)
                return;

            var position = empties[_random.Next(empties.Count)];
            var value = _random.NextDouble() < 0.9 ? 2 : 4;
            grid.Set(position, value);
        }
    }
}
