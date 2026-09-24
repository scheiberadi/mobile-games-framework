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

        // Difficulty ladder (spec 4.3): current level (1-4, starts at 1) and its rolling outcome buffer
        // (true = clean, false = demonstrated; see DifficultyLadder in Rules/Counting.cs), persisted so the
        // ladder survives across sessions and app restarts instead of resetting every relaunch.
        public int DifficultyLevel = DifficultyLadder.MinLevel;
        public List<bool> DifficultyBuffer = new List<bool>();

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
