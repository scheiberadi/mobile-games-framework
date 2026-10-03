namespace EvasLearningWorld.App
{
    // Which background loop (Resources/Music/<name>, made by tools/music/generate.js) plays on which screen; null is silence.
    // Every Arcade game is meant to get its own track; until they are written they share the arcade hall.
    public static class MusicTracks
    {
        // Every track that has a file in Resources/Music (checked by MusicTests).
        public static readonly string[] Names = { "map", "arcade", "bunnyrun" };

        public static string For(ScreenId screen)
        {
            switch (screen)
            {
                case ScreenId.Map: return "map";
                case ScreenId.Platformer: return "bunnyrun";
                case ScreenId.Arcade:
                case ScreenId.BalloonPopping:
                case ScreenId.WhackAMole:
                case ScreenId.Fishing:
                case ScreenId.SpaceShooter:
                case ScreenId.FruitCatcher:
                case ScreenId.TreasureHunt:
                    return "arcade";
                default: return null;
            }
        }
    }
}
