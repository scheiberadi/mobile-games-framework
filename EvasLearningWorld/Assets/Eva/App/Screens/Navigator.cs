using System.Collections.Generic;
using UnityEngine;

namespace EvasLearningWorld.App
{
    public enum ScreenId { Creator, Map, House, School, Store, StoreActivities, Shopping, DressTheCharacter, DressForOccasion, PackASuitcase, Count, NumberHunt,LetterHunt, Addition, Subtraction, WhichHasMore, OneMoreOneLess, NumberOrdering, MissingNumber, NumberLine, Multiplication, UppercaseToLowercase, BeginningSound, Rhyming, WordToImage, ImageToWord, LetterToSound, MissingLetter, BuildAWord, ScrambledWord, SentenceBuilder, ParentGate, Settings, Playground, PatternCompletion, OddOneOut, WhatsMissing, WhichDoesntMakeSense, ItemToShadow, FingerMaze, FollowNumbersInOrder, FollowLettersInOrder, ShortestPath, AvoidObstacles, CollectEverything, RotateThePiece, Jigsaw, Tangram, ZooFarm, ZooFarmHabitat, ZooFarmMother, ZooFarmFood, ZooFarmFootprint, ZooFarmCovering, ZooFarmSound, DomesticVsWild, LandSeaAir, AnimalBabies, AnimalClassification, Geography, ScienceLab, SinkOrFloat, Magnet, LivingVsNonLiving, PlantGrowth, HumanSenses, HealthyVsUnhealthy, Weather, DressForWeather, CauseAndEffect, CookingMeasures, Seasons, DayNight, Space, Workshop, BuildACar, BuildARocket, BuildAHouse, BuildABoat, BuildARobot, BridgeBuilding, SimplePhysics, ToolSelection, Balance, HelpTheCharacter, ArtStudio, TraceShapes, TraceLetters, TraceNumbers, ColorByNumber, ColorByInstruction, FinishTheDrawing, DrawWhatYouHear, GuidedDrawing, DrawingChallenges, FreeDrawing, BrainGym, ClassicMemory, RememberTheSequence, SimonSays, WhatsDisappeared, RememberTheLocation, SameOrDifferent, MatchRotation, WhichIsBigger, CompleteThePicture, FindTheDifferences, SpotTheObject, FollowThePath, WhatsBehind, Perspective, CopyTheConstruction, FindTheMissingPiece, Sorting, Recycling, MatchItemToCategory, SortLaundryChores, BrainGymSequenceOrdering, FriendsPark, EmotionMatching, FacialExpressionGame, WhatWouldYouDo, Empathy, SocialSituations, ListenAndChoose, ListenForDetails, Follow1Instruction, Follow2Instructions, Follow3Instructions, RoadSafety, SafetyScenarios, Arcade, BalloonPopping, WhackAMole, Fishing, SpaceShooter, FruitCatcher, TreasureHunt, Platformer }

    // Owns the screens and shows exactly one at a time under EvaGame.ScreenRoot. A screen is built the first time it is shown.
    public sealed class Navigator
    {
        private readonly EvaGame _game;
        private readonly Dictionary<ScreenId, ScreenBase> _screens = new Dictionary<ScreenId, ScreenBase>();

        public Navigator(EvaGame game) => _game = game;

        // Null until the first Show.
        public ScreenId? Current { get; private set; }

        // The screen shown before the current one (not updated when the same screen is shown again, e.g. "play again").
        private ScreenId? _previous;

        // The building's game list the current game was opened from; null on every other screen. The Hud's Back button
        // goes here, while Home always goes to the Map.
        public ScreenId? BackTarget { get; private set; }

        // The building menus that list a building's games.
        public static bool IsGameList(ScreenId id) =>
            id == ScreenId.School || id == ScreenId.Playground || id == ScreenId.ZooFarm || id == ScreenId.ScienceLab
            || id == ScreenId.Workshop || id == ScreenId.ArtStudio || id == ScreenId.BrainGym || id == ScreenId.FriendsPark
            || id == ScreenId.Arcade || id == ScreenId.StoreActivities;

        // Back is offered on a screen that is not itself a menu and was opened from a game list.
        public static ScreenId? BackTargetFor(ScreenId current, ScreenId? previous) =>
            previous.HasValue && IsGameList(previous.Value) && current != ScreenId.Map && !IsGameList(current) ? previous : null;

        public void Register(ScreenId id, ScreenBase screen) => _screens[id] = screen;

        // The registered screen object, or null (used by tests that drive a screen directly).
        public ScreenBase GetScreen(ScreenId id) => _screens.TryGetValue(id, out var screen) ? screen : null;

        public void Show(ScreenId id)
        {
            if (!_screens.TryGetValue(id, out var next))
            {
                Debug.LogError("Navigator: no screen registered for " + id);
                return;
            }

            if (Current.HasValue && Current.Value != id)
            {
                _previous = Current;
                // Leaving a screen silences it: Eva's line, an animal's call, a splash or a running loop never carry into the next one.
                // (The background music is its own player and goes on.)
                if (_game.Voice != null) _game.Voice.Stop();
                if (_game.Sfx != null) _game.Sfx.StopAll();
                var previous = _screens[Current.Value];
                previous.OnHide();
                previous.Root.gameObject.SetActive(false);
            }

            if (next.Root == null)
            {
                var rootObject = new GameObject(next.GetType().Name, typeof(RectTransform));
                rootObject.transform.SetParent(_game.ScreenRoot, false);
                var rect = (RectTransform)rootObject.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                next.Attach(rect);
                next.Build(_game);
            }

            next.Root.gameObject.SetActive(true);
            Current = id;
            BackTarget = BackTargetFor(id, _previous);
            if (_game.Hud != null)
            {
                _game.Hud.SetHomeVisible(id != ScreenId.Map && !BackTarget.HasValue);
                _game.Hud.SetBackVisible(BackTarget.HasValue);
                _game.Hud.SetNumeralsLight(id == ScreenId.SpaceShooter);
                _game.Hud.SetBubbleButtonVisible(id != ScreenId.Creator);
                _game.Hud.SetFpsVisible(id != ScreenId.ParentGate && id != ScreenId.Settings);
            }
            _game.Music?.Play(MusicTracks.For(id, BackTarget));
            next.OnShow();
            foreach (var marker in next.Root.GetComponentsInChildren<CompanionPairMarker>(true)) marker.Pair.Refresh(_game.Progress.Look);
        }
    }
}
