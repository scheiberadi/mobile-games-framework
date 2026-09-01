using MobileGamesFramework.Grid;

namespace Game01_2048
{
    public class Game2048MergeRule : IMergeRule<int>
    {
        public bool CanMerge(int a, int b) => a == b;

        public int Merge(int a, int b) => a + b;
    }
}
