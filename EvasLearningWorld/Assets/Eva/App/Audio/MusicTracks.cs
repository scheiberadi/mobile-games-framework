namespace EvasLearningWorld.App
{
    // Which background loop (Resources/Music/<name>, made by tools/music/generate.js) plays on which screen; null is silence.
    // Every Arcade game has its own track; the Arcade menu has the hall's.
    public static class MusicTracks
    {
        // Every track that has a file in Resources/Music (checked by MusicTests).
        public static readonly string[] Names = { "map", "arcade", "bunnyrun", "whack", "balloon", "fruitcatcher", "fishing", "spaceshooter", "treasure" };

        public static string For(ScreenId screen)
        {
            switch (screen)
            {
                case ScreenId.Map: return "map";
                case ScreenId.Arcade: return "arcade";
                case ScreenId.Platformer: return "bunnyrun";
                case ScreenId.WhackAMole: return "whack";
                case ScreenId.BalloonPopping: return "balloon";
                case ScreenId.FruitCatcher: return "fruitcatcher";
                case ScreenId.Fishing: return "fishing";
                case ScreenId.SpaceShooter: return "spaceshooter";
                case ScreenId.TreasureHunt: return "treasure";
                default: return null;
            }
        }
    }
}
