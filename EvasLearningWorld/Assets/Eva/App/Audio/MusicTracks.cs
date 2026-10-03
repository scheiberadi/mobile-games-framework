namespace EvasLearningWorld.App
{
    // Which background loop (Resources/Music/<name>, made by tools/music/generate.js) plays on which screen; null is silence.
    // The map, every building and every Arcade game has its own. A game opened from a building's game list has no track of its own
    // and keeps the building's, so walking from the list into a game and back never restarts the music.
    public static class MusicTracks
    {
        // Every track that has a file in Resources/Music (checked by MusicTests).
        public static readonly string[] Names =
        {
            "map", "arcade", "bunnyrun", "whack", "balloon", "fruitcatcher", "fishing", "spaceshooter", "treasure",
            "school", "playground", "zoofarm", "sciencelab", "workshop", "artstudio", "braingym", "friendspark", "store",
        };

        // backTarget is the game list the screen was opened from (Navigator.BackTarget), or null.
        public static string For(ScreenId screen, ScreenId? backTarget = null)
        {
            var own = Own(screen);
            if (own != null || !backTarget.HasValue) return own;
            return Own(backTarget.Value);
        }

        private static string Own(ScreenId screen)
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
                case ScreenId.School: return "school";
                case ScreenId.Playground: return "playground";
                case ScreenId.ZooFarm: return "zoofarm";
                case ScreenId.ScienceLab: return "sciencelab";
                case ScreenId.Workshop: return "workshop";
                case ScreenId.ArtStudio: return "artstudio";
                case ScreenId.BrainGym: return "braingym";
                case ScreenId.FriendsPark: return "friendspark";
                case ScreenId.Store:
                case ScreenId.StoreActivities:
                case ScreenId.Shopping:
                    return "store";
                default: return null;
            }
        }
    }
}
