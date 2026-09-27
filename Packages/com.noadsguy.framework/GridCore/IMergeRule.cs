namespace MobileGamesFramework.Grid
{
    public interface IMergeRule<TCell> where TCell : struct
    {
        bool CanMerge(TCell a, TCell b);
        TCell Merge(TCell a, TCell b);
    }
}
