using MobileGamesFramework.Grid;

namespace Game01_2048
{
    public interface ITileSpawnStrategy
    {
        void Spawn(GridCore<int> grid);
    }
}
