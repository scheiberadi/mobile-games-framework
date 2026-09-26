using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    [Serializable]
    public sealed class CharacterLook
    {
        public const int HeadCount = 4;
        public const int ColorCount = 5;

        public int Head;
        public int Skin;
        public int Shirt;
    }

    public enum BuyResult { Bought, NotEnoughCoins, AlreadyOwned, UnknownItem }

    // Everything that is saved between sessions.
    [Serializable]
    public sealed class PlayerProgress
    {
        public int Version = 1;
        public bool HasCharacter;
        public CharacterLook Look = new CharacterLook();
        public int Coins;
        public List<string> Owned = new List<string>();
        public HouseLayout House = new HouseLayout();
        public TutorialStep Tutorial = TutorialStep.CreateCharacter;
        public bool CountIntroSeen;
        // The place the child last visited (a PlaceId name): the characters stand there when the Map opens.
        public string LastPlace = "House";
        // The parent settings switches. All on by default, so a save from before they existed loads as all on.
        public bool MusicEnabled = true;
        public bool SfxEnabled = true;
        public bool VoiceEnabled = true;

        // Difficulty ladder (spec 4.3): current level (1-4, starts at 1) and its rolling outcome buffer
        // (true = clean, false = demonstrated; see DifficultyLadder in Rules/Counting.cs), persisted so the
        // ladder survives across sessions and app restarts instead of resetting every relaunch.
        public int DifficultyLevel = DifficultyLadder.MinLevel;
        public List<bool> DifficultyBuffer = new List<bool>();

        // Number Hunt's own difficulty ladder, independent of Counting's above (same DifficultyLadder class, own state).
        public int NumberHuntLevel = DifficultyLadder.MinLevel;
        public List<bool> NumberHuntBuffer = new List<bool>();

        // Pattern Completion's own difficulty ladder (Playground), independent of the others above.
        public int PatternCompletionLevel = DifficultyLadder.MinLevel;
        public List<bool> PatternCompletionBuffer = new List<bool>();

        // Odd One Out's own difficulty ladder (Playground), independent of the others above.
        public int OddOneOutLevel = DifficultyLadder.MinLevel;
        public List<bool> OddOneOutBuffer = new List<bool>();

        // What's Missing?'s own difficulty ladder (Playground), independent of the others above.
        public int WhatsMissingLevel = DifficultyLadder.MinLevel;
        public List<bool> WhatsMissingBuffer = new List<bool>();

        // Which Doesn't Make Sense?'s own difficulty ladder (Playground), independent of the others above.
        public int WhichDoesntMakeSenseLevel = DifficultyLadder.MinLevel;
        public List<bool> WhichDoesntMakeSenseBuffer = new List<bool>();

        // Item to Shadow's own difficulty ladder (Playground), independent of the others above.
        public int ItemToShadowLevel = DifficultyLadder.MinLevel;
        public List<bool> ItemToShadowBuffer = new List<bool>();

        // Finger Maze's own difficulty ladder (Playground), independent of the others above.
        public int FingerMazeLevel = DifficultyLadder.MinLevel;
        public List<bool> FingerMazeBuffer = new List<bool>();

        // Follow Numbers in Order's own difficulty ladder (Playground), independent of the others above.
        public int FollowNumbersLevel = DifficultyLadder.MinLevel;
        public List<bool> FollowNumbersBuffer = new List<bool>();

        // Follow Letters in Order's own difficulty ladder (Playground), independent of the others above.
        public int FollowLettersLevel = DifficultyLadder.MinLevel;
        public List<bool> FollowLettersBuffer = new List<bool>();

        // Shortest Path's own difficulty ladder (Playground), independent of the others above. Built but not yet
        // registered in Activities.cs - see full-catalogue-plan.md's TileLayout.MaxTiles note.
        public int ShortestPathLevel = DifficultyLadder.MinLevel;
        public List<bool> ShortestPathBuffer = new List<bool>();

        // Avoid Obstacles' own difficulty ladder (Playground), independent of the others above. Also built but
        // not yet registered in Activities.cs, same open ceiling.
        public int AvoidObstaclesLevel = DifficultyLadder.MinLevel;
        public List<bool> AvoidObstaclesBuffer = new List<bool>();

        // Collect Everything's own difficulty ladder (Playground), independent of the others above. Also built
        // but not yet registered in Activities.cs, same open ceiling.
        public int CollectEverythingLevel = DifficultyLadder.MinLevel;
        public List<bool> CollectEverythingBuffer = new List<bool>();

        // Rotate the Piece's own difficulty ladder (Playground), independent of the others above. Also built
        // but not yet registered in Activities.cs, same open ceiling.
        public int RotateThePieceLevel = DifficultyLadder.MinLevel;
        public List<bool> RotateThePieceBuffer = new List<bool>();

        // Jigsaw's own difficulty ladder (Playground), independent of the others above. Also built but not yet
        // registered in Activities.cs, same open ceiling.
        public int JigsawLevel = DifficultyLadder.MinLevel;
        public List<bool> JigsawBuffer = new List<bool>();

        // Tangram / Puzzle Blocks' own difficulty ladder (Playground), independent of the others above. Also
        // built but not yet registered in Activities.cs, same open ceiling. Playground's last game (14/14).
        public int TangramLevel = DifficultyLadder.MinLevel;
        public List<bool> TangramBuffer = new List<bool>();

        public void AddCoins(int n) => Coins += n;

        public BuyResult TryBuy(string itemId)
        {
            var item = FurnitureCatalog.Find(itemId);
            if (item == null || item.Id == FurnitureCatalog.StarterId) return BuyResult.UnknownItem;
            if (Owned.Contains(itemId)) return BuyResult.AlreadyOwned;
            if (Coins < item.Price) return BuyResult.NotEnoughCoins;
            Coins -= item.Price;
            Owned.Add(itemId);
            return BuyResult.Bought;
        }

        public void GrantStarter()
        {
            if (!Owned.Contains(FurnitureCatalog.StarterId)) Owned.Add(FurnitureCatalog.StarterId);
        }

        // True only when the tutorial step actually changed.
        public bool Advance(TutorialEvent e)
        {
            var next = TutorialFlow.Next(Tutorial, e);
            if (next == Tutorial) return false;
            Tutorial = next;
            return true;
        }
    }
}
