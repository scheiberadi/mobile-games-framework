using System.Collections.Generic;

namespace MobileGamesFramework.Grid
{
    public class SlideResult<TCell> where TCell : struct
    {
        public bool Moved;
        public IReadOnlyList<TCell> MergedResults;
    }
}
