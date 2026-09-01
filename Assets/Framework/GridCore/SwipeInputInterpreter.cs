using System;

namespace MobileGamesFramework.Grid
{
    public static class SwipeInputInterpreter
    {
        public static Direction? FromDelta(float dx, float dy, float minDistance = 20f)
        {
            if (Math.Abs(dx) < minDistance && Math.Abs(dy) < minDistance)
                return null;

            if (Math.Abs(dx) > Math.Abs(dy))
                return dx > 0 ? Direction.Right : Direction.Left;

            return dy > 0 ? Direction.Down : Direction.Up;
        }
    }
}
