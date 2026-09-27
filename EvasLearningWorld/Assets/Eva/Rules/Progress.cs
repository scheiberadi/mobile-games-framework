using System;
using System.Collections.Generic;

namespace EvasLearningWorld.Rules
{
    // CharacterLook now lives in Rules/Character.cs (M5 grew it well past a Progress.cs-sized fit).

    public enum BuyResult { Bought, NotEnoughCoins, AlreadyOwned, UnknownItem }

    // Everything that is saved between sessions.
    [Serializable]
    public sealed class PlayerProgress
    {
        // 2 = the M5 character model (Gender/Face/wardrobe-item-ids); 1 = the old Head/Skin/Shirt tint model.
        // A fresh PlayerProgress is already on the current model, so it must start at the CURRENT version, not
        // a hard-coded 1 - see SaveStore.Load()'s migration, which only fires for JSON that really did save a
        // lower Version. If this default is ever bumped again for a future save-breaking change, bump it here
        // to the new current version, not by editing the "< 2" check below.
        public int Version = 2;
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

        // Letter Hunt's own difficulty ladder (School), independent of the others above.
        public int LetterHuntLevel = DifficultyLadder.MinLevel;
        public List<bool> LetterHuntBuffer = new List<bool>();

        // Addition's own difficulty ladder (School), independent of the others above.
        public int AdditionLevel = DifficultyLadder.MinLevel;
        public List<bool> AdditionBuffer = new List<bool>();

        // Subtraction's own difficulty ladder (School), independent of the others above.
        public int SubtractionLevel = DifficultyLadder.MinLevel;
        public List<bool> SubtractionBuffer = new List<bool>();

        // Which Has More?'s own difficulty ladder (School), independent of the others above.
        public int WhichHasMoreLevel = DifficultyLadder.MinLevel;
        public List<bool> WhichHasMoreBuffer = new List<bool>();

        // One More / One Less's own difficulty ladder (School), independent of the others above.
        public int OneMoreOneLessLevel = DifficultyLadder.MinLevel;
        public List<bool> OneMoreOneLessBuffer = new List<bool>();

        // Number Ordering's own difficulty ladder (School), independent of the others above.
        public int NumberOrderingLevel = DifficultyLadder.MinLevel;
        public List<bool> NumberOrderingBuffer = new List<bool>();

        // Missing Number's own difficulty ladder (School), independent of the others above.
        public int MissingNumberLevel = DifficultyLadder.MinLevel;
        public List<bool> MissingNumberBuffer = new List<bool>();

        // Number Line's own difficulty ladder (School), independent of the others above.
        public int NumberLineLevel = DifficultyLadder.MinLevel;
        public List<bool> NumberLineBuffer = new List<bool>();

        // Multiplication's own difficulty ladder (School), independent of the others above. School's last
        // Mathematics game (see docs/kids-games/full-catalogue-plan.md's M4.2 order).
        public int MultiplicationLevel = DifficultyLadder.MinLevel;
        public List<bool> MultiplicationBuffer = new List<bool>();

        // Uppercase to Lowercase's own difficulty ladder (School), independent of the others above. First game
        // of School's Literacy cluster beyond Letter Hunt.
        public int UppercaseToLowercaseLevel = DifficultyLadder.MinLevel;
        public List<bool> UppercaseToLowercaseBuffer = new List<bool>();

        // Beginning Sound's own difficulty ladder (School), independent of the others above.
        public int BeginningSoundLevel = DifficultyLadder.MinLevel;
        public List<bool> BeginningSoundBuffer = new List<bool>();

        // Rhyming's own difficulty ladder (School), independent of the others above.
        public int RhymingLevel = DifficultyLadder.MinLevel;
        public List<bool> RhymingBuffer = new List<bool>();

        // Word to Image's own difficulty ladder (School), independent of the others above.
        public int WordToImageLevel = DifficultyLadder.MinLevel;
        public List<bool> WordToImageBuffer = new List<bool>();

        // Image to Word's own difficulty ladder (School), independent of the others above.
        public int ImageToWordLevel = DifficultyLadder.MinLevel;
        public List<bool> ImageToWordBuffer = new List<bool>();

        // Letter to Sound's own difficulty ladder (School), independent of the others above.
        public int LetterToSoundLevel = DifficultyLadder.MinLevel;
        public List<bool> LetterToSoundBuffer = new List<bool>();

        // Missing Letter's own difficulty ladder (School), independent of the others above.
        public int MissingLetterLevel = DifficultyLadder.MinLevel;
        public List<bool> MissingLetterBuffer = new List<bool>();

        // Build a Word's own difficulty ladder (School), independent of the others above.
        public int BuildAWordLevel = DifficultyLadder.MinLevel;
        public List<bool> BuildAWordBuffer = new List<bool>();

        // Scrambled Word's own difficulty ladder (School), independent of the others above.
        public int ScrambledWordLevel = DifficultyLadder.MinLevel;
        public List<bool> ScrambledWordBuffer = new List<bool>();

        // Simple Sentence Builder's own difficulty ladder (School), independent of the others above - closes
        // out the Literacy cluster.
        public int SentenceBuilderLevel = DifficultyLadder.MinLevel;
        public List<bool> SentenceBuilderBuffer = new List<bool>();

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

        // Shortest Path's own difficulty ladder (Playground), independent of the others above.
        public int ShortestPathLevel = DifficultyLadder.MinLevel;
        public List<bool> ShortestPathBuffer = new List<bool>();

        // Avoid Obstacles' own difficulty ladder (Playground), independent of the others above.
        public int AvoidObstaclesLevel = DifficultyLadder.MinLevel;
        public List<bool> AvoidObstaclesBuffer = new List<bool>();

        // Collect Everything's own difficulty ladder (Playground), independent of the others above.
        public int CollectEverythingLevel = DifficultyLadder.MinLevel;
        public List<bool> CollectEverythingBuffer = new List<bool>();

        // Rotate the Piece's own difficulty ladder (Playground), independent of the others above.
        public int RotateThePieceLevel = DifficultyLadder.MinLevel;
        public List<bool> RotateThePieceBuffer = new List<bool>();

        // Jigsaw's own difficulty ladder (Playground), independent of the others above.
        public int JigsawLevel = DifficultyLadder.MinLevel;
        public List<bool> JigsawBuffer = new List<bool>();

        // Tangram / Puzzle Blocks' own difficulty ladder (Playground), independent of the others above.
        // Playground's last game (14/14).
        public int TangramLevel = DifficultyLadder.MinLevel;
        public List<bool> TangramBuffer = new List<bool>();

        // Shopping's own difficulty ladder (Store, M4.3) - unlike every other ladder above, level doubles as
        // ShoppingMode (see ShoppingRoundGenerator's own class comment), but persists the same way.
        public int ShoppingLevel = DifficultyLadder.MinLevel;
        public List<bool> ShoppingBuffer = new List<bool>();

        // Dress the Character's own difficulty ladder (Store, M4.3 dressing cluster), independent of the others above.
        public int DressTheCharacterLevel = DifficultyLadder.MinLevel;
        public List<bool> DressTheCharacterBuffer = new List<bool>();

        // Dress for the Occasion's own difficulty ladder (Store, M4.3 dressing cluster) - level doubles as
        // Occasion (see DressForOccasionRoundGenerator's own class comment), same shape as Shopping's ladder.
        public int DressForOccasionLevel = DifficultyLadder.MinLevel;
        public List<bool> DressForOccasionBuffer = new List<bool>();

        // Pack a Suitcase's own difficulty ladder (Store, M4.3 dressing cluster) - level doubles as Trip
        // (reusing Occasion, see PackASuitcaseRoundGenerator's own class comment), same shape as Shopping's and
        // Dress for the Occasion's ladders. Closes out M4.3.
        public int PackASuitcaseLevel = DifficultyLadder.MinLevel;
        public List<bool> PackASuitcaseBuffer = new List<bool>();

        // Zoo & Farm's ten animal games (M4.4), each its own difficulty ladder, independent of the others
        // above - same shape as every ladder in this file even though all ten share one MatchScreen presenter
        // and one MatchRoundBuilder (see Rules/ZooFarm.cs).
        public int ZooFarmHabitatLevel = DifficultyLadder.MinLevel;
        public List<bool> ZooFarmHabitatBuffer = new List<bool>();

        public int ZooFarmMotherLevel = DifficultyLadder.MinLevel;
        public List<bool> ZooFarmMotherBuffer = new List<bool>();

        public int ZooFarmFoodLevel = DifficultyLadder.MinLevel;
        public List<bool> ZooFarmFoodBuffer = new List<bool>();

        public int ZooFarmSoundLevel = DifficultyLadder.MinLevel;
        public List<bool> ZooFarmSoundBuffer = new List<bool>();

        public int ZooFarmFootprintLevel = DifficultyLadder.MinLevel;
        public List<bool> ZooFarmFootprintBuffer = new List<bool>();

        public int ZooFarmCoveringLevel = DifficultyLadder.MinLevel;
        public List<bool> ZooFarmCoveringBuffer = new List<bool>();

        public int ZooFarmBabiesLevel = DifficultyLadder.MinLevel;
        public List<bool> ZooFarmBabiesBuffer = new List<bool>();

        public int DomesticVsWildLevel = DifficultyLadder.MinLevel;
        public List<bool> DomesticVsWildBuffer = new List<bool>();

        public int LandSeaAirLevel = DifficultyLadder.MinLevel;
        public List<bool> LandSeaAirBuffer = new List<bool>();

        // Animal Classification's own ladder - level also selects which bucket split is active (2, then 3,
        // then 4 compound buckets; see ZooFarmRoundGenerator.ClassificationConfig), same "level doubles as
        // mode" shape as Shopping's/Dress for the Occasion's ladders.
        public int AnimalClassificationLevel = DifficultyLadder.MinLevel;
        public List<bool> AnimalClassificationBuffer = new List<bool>();

        // Geography's own ladder (Zoo & Farm's eleventh game) - level also selects Flag/Continent/Landmark
        // mode (see GeographyRoundGenerator), closing out M4.4.
        public int GeographyLevel = DifficultyLadder.MinLevel;
        public List<bool> GeographyBuffer = new List<bool>();

        // Science Lab's thirteen games (M4.5), each its own difficulty ladder - same shape as every ladder in
        // this file even though twelve of the thirteen share one MatchScreen presenter (see Rules/ScienceLab.cs)
        // and the thirteenth (Plant Growth) shares the new SequenceScreen presenter (see Rules/PlantGrowth.cs).
        public int SinkOrFloatLevel = DifficultyLadder.MinLevel;
        public List<bool> SinkOrFloatBuffer = new List<bool>();

        public int MagnetLevel = DifficultyLadder.MinLevel;
        public List<bool> MagnetBuffer = new List<bool>();

        public int LivingVsNonLivingLevel = DifficultyLadder.MinLevel;
        public List<bool> LivingVsNonLivingBuffer = new List<bool>();

        public int PlantGrowthLevel = DifficultyLadder.MinLevel;
        public List<bool> PlantGrowthBuffer = new List<bool>();

        public int HumanSensesLevel = DifficultyLadder.MinLevel;
        public List<bool> HumanSensesBuffer = new List<bool>();

        public int HealthyVsUnhealthyLevel = DifficultyLadder.MinLevel;
        public List<bool> HealthyVsUnhealthyBuffer = new List<bool>();

        public int WeatherLevel = DifficultyLadder.MinLevel;
        public List<bool> WeatherBuffer = new List<bool>();

        public int DressForWeatherLevel = DifficultyLadder.MinLevel;
        public List<bool> DressForWeatherBuffer = new List<bool>();

        public int CauseAndEffectLevel = DifficultyLadder.MinLevel;
        public List<bool> CauseAndEffectBuffer = new List<bool>();

        public int CookingMeasuresLevel = DifficultyLadder.MinLevel;
        public List<bool> CookingMeasuresBuffer = new List<bool>();

        public int SeasonsLevel = DifficultyLadder.MinLevel;
        public List<bool> SeasonsBuffer = new List<bool>();

        public int DayNightLevel = DifficultyLadder.MinLevel;
        public List<bool> DayNightBuffer = new List<bool>();

        public int SpaceLevel = DifficultyLadder.MinLevel;
        public List<bool> SpaceBuffer = new List<bool>();

        // Workshop's ten games (M4.6), each its own difficulty ladder - same shape as every ladder in this
        // file even though seven of the ten share one AssemblyScreen presenter (see Rules/Workshop.cs) and the
        // other three share the existing MatchScreen presenter.
        public int BuildACarLevel = DifficultyLadder.MinLevel;
        public List<bool> BuildACarBuffer = new List<bool>();

        public int BuildARocketLevel = DifficultyLadder.MinLevel;
        public List<bool> BuildARocketBuffer = new List<bool>();

        public int BuildAHouseLevel = DifficultyLadder.MinLevel;
        public List<bool> BuildAHouseBuffer = new List<bool>();

        public int BuildABoatLevel = DifficultyLadder.MinLevel;
        public List<bool> BuildABoatBuffer = new List<bool>();

        public int BuildARobotLevel = DifficultyLadder.MinLevel;
        public List<bool> BuildARobotBuffer = new List<bool>();

        public int BridgeBuildingLevel = DifficultyLadder.MinLevel;
        public List<bool> BridgeBuildingBuffer = new List<bool>();

        public int SimplePhysicsLevel = DifficultyLadder.MinLevel;
        public List<bool> SimplePhysicsBuffer = new List<bool>();

        public int ToolSelectionLevel = DifficultyLadder.MinLevel;
        public List<bool> ToolSelectionBuffer = new List<bool>();

        public int BalanceLevel = DifficultyLadder.MinLevel;
        public List<bool> BalanceBuffer = new List<bool>();

        public int HelpTheCharacterLevel = DifficultyLadder.MinLevel;
        public List<bool> HelpTheCharacterBuffer = new List<bool>();

        // Art Studio's nine round-based games (M4.7); Free Drawing (the tenth) has no round/level shape at all
        // - see Rules/ArtStudio.cs's class comment - so it gets no ladder here.
        public int TraceShapesLevel = DifficultyLadder.MinLevel;
        public List<bool> TraceShapesBuffer = new List<bool>();

        public int TraceLettersLevel = DifficultyLadder.MinLevel;
        public List<bool> TraceLettersBuffer = new List<bool>();

        public int TraceNumbersLevel = DifficultyLadder.MinLevel;
        public List<bool> TraceNumbersBuffer = new List<bool>();

        public int ColorByNumberLevel = DifficultyLadder.MinLevel;
        public List<bool> ColorByNumberBuffer = new List<bool>();

        public int ColorByInstructionLevel = DifficultyLadder.MinLevel;
        public List<bool> ColorByInstructionBuffer = new List<bool>();

        public int FinishTheDrawingLevel = DifficultyLadder.MinLevel;
        public List<bool> FinishTheDrawingBuffer = new List<bool>();

        public int DrawWhatYouHearLevel = DifficultyLadder.MinLevel;
        public List<bool> DrawWhatYouHearBuffer = new List<bool>();

        public int GuidedDrawingLevel = DifficultyLadder.MinLevel;
        public List<bool> GuidedDrawingBuffer = new List<bool>();

        public int DrawingChallengesLevel = DifficultyLadder.MinLevel;
        public List<bool> DrawingChallengesBuffer = new List<bool>();

        // Brain Gym's 21 games (M4.8), all round-based.
        public int ClassicMemoryLevel = DifficultyLadder.MinLevel;
        public List<bool> ClassicMemoryBuffer = new List<bool>();

        public int RememberTheSequenceLevel = DifficultyLadder.MinLevel;
        public List<bool> RememberTheSequenceBuffer = new List<bool>();

        public int SimonSaysLevel = DifficultyLadder.MinLevel;
        public List<bool> SimonSaysBuffer = new List<bool>();

        public int WhatsDisappearedLevel = DifficultyLadder.MinLevel;
        public List<bool> WhatsDisappearedBuffer = new List<bool>();

        public int RememberTheLocationLevel = DifficultyLadder.MinLevel;
        public List<bool> RememberTheLocationBuffer = new List<bool>();

        public int SameOrDifferentLevel = DifficultyLadder.MinLevel;
        public List<bool> SameOrDifferentBuffer = new List<bool>();

        public int MatchRotationLevel = DifficultyLadder.MinLevel;
        public List<bool> MatchRotationBuffer = new List<bool>();

        public int WhichIsBiggerLevel = DifficultyLadder.MinLevel;
        public List<bool> WhichIsBiggerBuffer = new List<bool>();

        public int CompleteThePictureLevel = DifficultyLadder.MinLevel;
        public List<bool> CompleteThePictureBuffer = new List<bool>();

        public int FindTheDifferencesLevel = DifficultyLadder.MinLevel;
        public List<bool> FindTheDifferencesBuffer = new List<bool>();

        public int SpotTheObjectLevel = DifficultyLadder.MinLevel;
        public List<bool> SpotTheObjectBuffer = new List<bool>();

        public int FollowThePathLevel = DifficultyLadder.MinLevel;
        public List<bool> FollowThePathBuffer = new List<bool>();

        public int WhatsBehindLevel = DifficultyLadder.MinLevel;
        public List<bool> WhatsBehindBuffer = new List<bool>();

        public int PerspectiveLevel = DifficultyLadder.MinLevel;
        public List<bool> PerspectiveBuffer = new List<bool>();

        public int CopyTheConstructionLevel = DifficultyLadder.MinLevel;
        public List<bool> CopyTheConstructionBuffer = new List<bool>();

        public int FindTheMissingPieceLevel = DifficultyLadder.MinLevel;
        public List<bool> FindTheMissingPieceBuffer = new List<bool>();

        public int SortingLevel = DifficultyLadder.MinLevel;
        public List<bool> SortingBuffer = new List<bool>();

        public int RecyclingLevel = DifficultyLadder.MinLevel;
        public List<bool> RecyclingBuffer = new List<bool>();

        public int MatchItemToCategoryLevel = DifficultyLadder.MinLevel;
        public List<bool> MatchItemToCategoryBuffer = new List<bool>();

        public int SortLaundryChoresLevel = DifficultyLadder.MinLevel;
        public List<bool> SortLaundryChoresBuffer = new List<bool>();

        public int BrainGymSequenceOrderingLevel = DifficultyLadder.MinLevel;
        public List<bool> BrainGymSequenceOrderingBuffer = new List<bool>();

        public int EmotionMatchingLevel = DifficultyLadder.MinLevel;
        public List<bool> EmotionMatchingBuffer = new List<bool>();

        public int FacialExpressionGameLevel = DifficultyLadder.MinLevel;
        public List<bool> FacialExpressionGameBuffer = new List<bool>();

        public int WhatWouldYouDoLevel = DifficultyLadder.MinLevel;
        public List<bool> WhatWouldYouDoBuffer = new List<bool>();

        public int EmpathyLevel = DifficultyLadder.MinLevel;
        public List<bool> EmpathyBuffer = new List<bool>();

        public int SocialSituationsLevel = DifficultyLadder.MinLevel;
        public List<bool> SocialSituationsBuffer = new List<bool>();

        public int ListenAndChooseLevel = DifficultyLadder.MinLevel;
        public List<bool> ListenAndChooseBuffer = new List<bool>();

        public int ListenForDetailsLevel = DifficultyLadder.MinLevel;
        public List<bool> ListenForDetailsBuffer = new List<bool>();

        public int Follow1InstructionLevel = DifficultyLadder.MinLevel;
        public List<bool> Follow1InstructionBuffer = new List<bool>();

        public int Follow2InstructionsLevel = DifficultyLadder.MinLevel;
        public List<bool> Follow2InstructionsBuffer = new List<bool>();

        public int Follow3InstructionsLevel = DifficultyLadder.MinLevel;
        public List<bool> Follow3InstructionsBuffer = new List<bool>();

        public int RoadSafetyLevel = DifficultyLadder.MinLevel;
        public List<bool> RoadSafetyBuffer = new List<bool>();

        public int SafetyScenariosLevel = DifficultyLadder.MinLevel;
        public List<bool> SafetyScenariosBuffer = new List<bool>();

        public int BalloonPoppingLevel = DifficultyLadder.MinLevel;
        public List<bool> BalloonPoppingBuffer = new List<bool>();

        public int WhackAMoleLevel = DifficultyLadder.MinLevel;
        public List<bool> WhackAMoleBuffer = new List<bool>();

        public int FishingLevel = DifficultyLadder.MinLevel;
        public List<bool> FishingBuffer = new List<bool>();

        public int SpaceShooterLevel = DifficultyLadder.MinLevel;
        public List<bool> SpaceShooterBuffer = new List<bool>();

        public int FruitCatcherLevel = DifficultyLadder.MinLevel;
        public List<bool> FruitCatcherBuffer = new List<bool>();

        public int TreasureHuntLevel = DifficultyLadder.MinLevel;
        public List<bool> TreasureHuntBuffer = new List<bool>();

        public int PlatformerLevel = DifficultyLadder.MinLevel;
        public List<bool> PlatformerBuffer = new List<bool>();

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
