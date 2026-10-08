using System;
using EvasLearningWorld.Rules;
using MobileGamesFramework.Persistence;
using MobileGamesFramework.UI;
using UnityEngine;

namespace EvasLearningWorld.App
{
    // The root of the app: builds the canvas, the screen area, audio, navigation and the Hud, then opens the Map.
    public sealed class EvaGame : MonoBehaviour
    {
        // Answer-variety Prototype A: Item to Shadow as drag-to-target (true) or the original tap screen (false).
        // A multi-pair round is 3-4 drags, so a session is 3 rounds instead of the tap version's 5.
        private static readonly bool ItemToShadowUsesDrag = true;
        private const int ItemToShadowDragRoundsPerSession = 3;

        // Answer-variety Prototype B: Sorting as drop-sort (true) or the original tap screen (false).
        private static readonly bool SortingUsesDrop = true;

        public Voice Voice { get; private set; }
        public Sfx Sfx { get; private set; }
        public MusicPlayer Music { get; private set; }
        public Navigator Navigator { get; private set; }
        public MapScreen Map { get; private set; }
        public Hud Hud { get; private set; }
        public RectTransform ScreenRoot { get; private set; }
        public PlayerProgress Progress { get; private set; }
        public TutorialGuide TutorialGuide { get; private set; }

        private SaveStore _save;
        private GameObject _ownedCanvas;

        // The canvas this game created itself (null when one was passed in), so a restart can remove it.
        public GameObject OwnedCanvas => _ownedCanvas;

        // Raised after the save was erased; EvaBootstrap rebuilds the whole game in response.
        public event Action StartOverRequested;

        public void StartOver()
        {
            _save.Save(new PlayerProgress());
            StartOverRequested?.Invoke();
        }

        // Pass an existing canvas to build under it (tests); by default this creates the app canvas and its EventSystem.
        // Pass a store to keep the save away from PlayerPrefs (tests); by default the save lives in PlayerPrefs.
        public void Build(Canvas canvas = null, IKeyValueStore store = null)
        {
            _save = new SaveStore(store ?? new PlayerPrefsStore());
            Progress = _save.Load();

            if (canvas == null)
            {
                canvas = UiFactory.CreateCanvas(new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight), 1f);
                _ownedCanvas = canvas.gameObject;
            }
            // The default 10 px drag threshold is about half a millimetre on a phone: a child's tap would count as a drag
            // and cancel the click. About 2.5 mm of finger wobble is still a tap.
            var eventSystem = UnityEngine.EventSystems.EventSystem.current != null
                ? UnityEngine.EventSystems.EventSystem.current
                : FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem != null) eventSystem.pixelDragThreshold = DragThreshold(Screen.dpi);
            UiFactory.CreateBackground(canvas.transform, new Color(0.75f, 0.91f, 1f), new Color(0.91f, 0.97f, 0.88f));

            ScreenRoot = CreateSafeAreaPanel(canvas.transform, "ScreenRoot");

            Sfx = new GameObject("Sfx", typeof(Sfx)).GetComponent<Sfx>();
            Sfx.transform.SetParent(transform, false);
            EvaUi.Sfx = Sfx;

            Voice = new GameObject("Voice", typeof(Voice)).GetComponent<Voice>();
            Voice.transform.SetParent(transform, false);
            Music = new GameObject("Music", typeof(MusicPlayer)).GetComponent<MusicPlayer>();
            Music.transform.SetParent(transform, false);
            Music.Voice = Voice;
            ApplyAudioSettings();

            Navigator = new Navigator(this);
            Navigator.Register(ScreenId.Creator, new CreatorScreen());
            Map = new MapScreen();
            Navigator.Register(ScreenId.Map, Map);
            Navigator.Register(ScreenId.House, new HouseScreen());
            Navigator.Register(ScreenId.School, new BuildingScreen(BuildingId.School));
            Navigator.Register(ScreenId.Count, new CountScreen());
            Navigator.Register(ScreenId.NumberHunt, new NumberHuntScreen());
            Navigator.Register(ScreenId.LetterHunt, new LetterHuntScreen());
            Navigator.Register(ScreenId.Addition, new AdditionScreen());
            Navigator.Register(ScreenId.Subtraction, new SubtractionScreen());
            Navigator.Register(ScreenId.WhichHasMore, new WhichHasMoreScreen());
            Navigator.Register(ScreenId.OneMoreOneLess, new OneMoreOneLessScreen());
            Navigator.Register(ScreenId.NumberOrdering, new NumberOrderingScreen());
            Navigator.Register(ScreenId.MissingNumber, new MissingNumberScreen());
            Navigator.Register(ScreenId.NumberLine, new NumberLineScreen());
            Navigator.Register(ScreenId.Multiplication, new MultiplicationScreen());
            Navigator.Register(ScreenId.UppercaseToLowercase, new UppercaseToLowercaseScreen());
            Navigator.Register(ScreenId.BeginningSound, new BeginningSoundScreen());
            Navigator.Register(ScreenId.Rhyming, new RhymingScreen());
            Navigator.Register(ScreenId.WordToImage, new WordToImageScreen());
            Navigator.Register(ScreenId.ImageToWord, new ImageToWordScreen());
            Navigator.Register(ScreenId.LetterToSound, new LetterToSoundScreen());
            Navigator.Register(ScreenId.MissingLetter, new MissingLetterScreen());
            Navigator.Register(ScreenId.BuildAWord, new BuildAWordScreen());
            Navigator.Register(ScreenId.ScrambledWord, new ScrambledWordScreen());
            Navigator.Register(ScreenId.SentenceBuilder, new SentenceBuilderScreen());
            Navigator.Register(ScreenId.Store, new StoreScreen());
            Navigator.Register(ScreenId.StoreActivities, new StoreActivitiesScreen());
            Navigator.Register(ScreenId.Shopping, new ShoppingScreen());
            Navigator.Register(ScreenId.DressTheCharacter, new DressTheCharacterScreen());
            Navigator.Register(ScreenId.DressForOccasion, new DressForOccasionScreen());
            Navigator.Register(ScreenId.PackASuitcase, new PackASuitcaseScreen());
            Navigator.Register(ScreenId.ParentGate, new ParentGateScreen());
            Navigator.Register(ScreenId.Settings, new SettingsScreen());
            Navigator.Register(ScreenId.Playground, new BuildingScreen(BuildingId.Playground));
            Navigator.Register(ScreenId.PatternCompletion, new PatternCompletionScreen());
            Navigator.Register(ScreenId.OddOneOut, new OddOneOutScreen());
            Navigator.Register(ScreenId.WhatsMissing, new WhatsMissingScreen());
            Navigator.Register(ScreenId.WhichDoesntMakeSense, new WhichDoesntMakeSenseScreen());
            // Answer-variety Prototype A (docs/kids-games/answer-variety-prototypes.md): Item to Shadow as drag-to-target.
            // The original tap screen stays in the project so the two can be compared on the device; set
            // ItemToShadowUsesDrag to false to register it again.
            if (ItemToShadowUsesDrag)
                Navigator.Register(ScreenId.ItemToShadow, new DragToTargetScreen(ScreenId.ItemToShadow, ScreenId.Playground, "world/playground_bg",
                    (level, rng) => DragToTargetRoundBuilder.FromItemToShadow(ItemToShadowRoundGenerator.Create(level, rng), rng),
                    p => p.ItemToShadowLevel, (p, v) => p.ItemToShadowLevel = v, p => p.ItemToShadowBuffer,
                    ItemToShadowDragRoundsPerSession, ItemToShadowRoundGenerator.SpritePrefix, ItemToShadowRoundGenerator.SilhouetteSuffix,
                    "itemtoshadow_drag", "itemtoshadow_drag_hint", "itemtoshadow_drag_demo"));
            else
                Navigator.Register(ScreenId.ItemToShadow, new ItemToShadowScreen());
            Navigator.Register(ScreenId.FingerMaze, new FingerMazeScreen());
            Navigator.Register(ScreenId.FollowNumbersInOrder, new FollowNumbersInOrderScreen());
            Navigator.Register(ScreenId.FollowLettersInOrder, new FollowLettersInOrderScreen());
            Navigator.Register(ScreenId.ShortestPath, new ShortestPathScreen());
            Navigator.Register(ScreenId.AvoidObstacles, new AvoidObstaclesScreen());
            Navigator.Register(ScreenId.CollectEverything, new CollectEverythingScreen());
            Navigator.Register(ScreenId.RotateThePiece, new RotateThePieceScreen());
            Navigator.Register(ScreenId.Jigsaw, new JigsawScreen());
            Navigator.Register(ScreenId.Tangram, new TangramScreen());
            Navigator.Register(ScreenId.ZooFarm, new BuildingScreen(BuildingId.ZooFarm));
            // Answer-variety step 6: Habitat is "take each animal to its home", played until every animal is home (PairingScreen).
            Navigator.Register(ScreenId.ZooFarmHabitat, new PairingScreen(ScreenId.ZooFarmHabitat, ScreenId.ZooFarm, "world/zoofarm_bg", new PairingConfig
            {
                Pairs = ZooPairs.AnimalAndHabitat(), PairsPerGame = ZooPairs.PairsPerGame,
                ItemSpritePrefix = "zoofarm/animal_",
                TargetSpritePrefix = "zoofarm/habitat_",
                PromptKey = "pairing_prompt_habitat", HintKey = "habitat_drag_hint", DemoKey = "habitat_drag_demo",
                Confusable = HabitatRules.IsBelievableButWrong,
                WrongReaction = HabitatRules.ReactionFor,
            }));
            // Mother: take each baby to its mother, played until every baby is with its mother (PairingScreen).
            Navigator.Register(ScreenId.ZooFarmMother, new PairingScreen(ScreenId.ZooFarmMother, ScreenId.ZooFarm, "world/zoofarm_bg", new PairingConfig
            {
                Pairs = ZooPairs.MotherAndBaby(),
                ItemSpritePrefix = "zoofarm/baby_",
                TargetSprite = ZooFarmAnimals.MotherSprite,
                PairsPerGame = ZooPairs.PairsPerGame,
                PromptKey = "pairing_prompt_mother", HintKey = "pairing_mother_hint", DemoKey = "pairing_mother_demo",
                WrongReaction = (_, __) => PairReaction.Refuse,
            }));
            // Food: feed the hungry animals from a conveyor belt of foods, six levels of portions / kinds / animals (FeedingScreen).
            Navigator.Register(ScreenId.ZooFarmFood, new FeedingScreen(ScreenId.ZooFarmFood, ScreenId.ZooFarm, "world/zoofarm_bg",
                p => p.ZooFarmFoodLevel, (p, v) => p.ZooFarmFoodLevel = v, p => p.ZooFarmFoodBuffer,
                "zoofarm_prompt_feeding", "zoofarm_feeding_hint", "zoofarm_feeding_demo"));
            // Footprint: take each footprint to the animal that left it, played until every footprint has its animal (PairingScreen).
            Navigator.Register(ScreenId.ZooFarmFootprint, new PairingScreen(ScreenId.ZooFarmFootprint, ScreenId.ZooFarm, "world/zoofarm_bg", new PairingConfig
            {
                Pairs = ZooPairs.FootprintAndAnimal(), Slots = 3,
                ItemSpritePrefix = "zoofarm/footprint_",
                TargetSpritePrefix = "zoofarm/animal_",
                PromptKey = "pairing_prompt_footprint", HintKey = "pairing_footprint_hint", DemoKey = "pairing_footprint_demo",
                WrongReaction = (_, __) => PairReaction.Refuse,
            }));
            // Covering: which animals have feathers / fur / skin / scales? Tap them in a grid (GuessCoveringScreen).
            Navigator.Register(ScreenId.ZooFarmCovering, new GuessCoveringScreen(ScreenId.ZooFarmCovering, ScreenId.ZooFarm, "world/zoofarm_bg",
                p => p.ZooFarmCoveringLevel, (p, v) => p.ZooFarmCoveringLevel = v, p => p.ZooFarmCoveringBuffer));
            Navigator.Register(ScreenId.ZooFarmSound, new MatchScreen(ScreenId.ZooFarmSound, ScreenId.ZooFarm, "world/zoofarm_bg",
                (level, rng, prev) => ZooFarmRoundGenerator.Create(ZooFarmGameKind.Sound, level, rng, prev),
                p => p.ZooFarmSoundLevel, (p, v) => p.ZooFarmSoundLevel = v, p => p.ZooFarmSoundBuffer,
                ZooFarmRoundGenerator.RoundsPerSession, "zoofarm_sound_hint", "zoofarm_sound_demo"));
            Navigator.Register(ScreenId.DomesticVsWild, new DropSortScreen(ScreenId.DomesticVsWild, ScreenId.ZooFarm, "world/domestic_wild_bg",
                (level, rng) => DropSortRoundBuilder.Create(ZooFarmRoundGenerator.DropSortCatalogue(ZooFarmGameKind.DomesticVsWild, level), level, rng).WithBinOrder("domestic", "wild"),
                p => p.DomesticVsWildLevel, (p, v) => p.DomesticVsWildLevel = v, p => p.DomesticVsWildBuffer,
                DropSortRoundBuilder.RoundsPerSession, "zoofarm/animal_", "zoofarm/bucket_",
                "zoofarm_prompt_domestic_wild", "sorting_drag_hint", "sorting_drag_demo", "zoofarm_ds_", true, ResidentsLayout.DomesticVsWild));
            Navigator.Register(ScreenId.LandSeaAir, new DropSortScreen(ScreenId.LandSeaAir, ScreenId.ZooFarm, "world/land_sea_air_bg",
                (level, rng) => DropSortRoundBuilder.Create(ZooFarmRoundGenerator.DropSortCatalogue(ZooFarmGameKind.LandSeaAir, level), level, rng, 3, DropSortRoundBuilder.FewResidentsPerBin).WithBinOrder("air", "land", "sea"),
                p => p.LandSeaAirLevel, (p, v) => p.LandSeaAirLevel = v, p => p.LandSeaAirBuffer,
                DropSortRoundBuilder.RoundsPerSession, "zoofarm/animal_", "zoofarm/bucket_",
                "zoofarm_prompt_land_sea_air", "sorting_drag_hint", "sorting_drag_demo", "zoofarm_ds_", true, ResidentsLayout.LandSeaAir));
            Navigator.Register(ScreenId.AnimalBabies, new MatchScreen(ScreenId.AnimalBabies, ScreenId.ZooFarm, "world/zoofarm_bg",
                (level, rng, prev) => ZooFarmRoundGenerator.Create(ZooFarmGameKind.Babies, level, rng, prev),
                p => p.ZooFarmBabiesLevel, (p, v) => p.ZooFarmBabiesLevel = v, p => p.ZooFarmBabiesBuffer,
                ZooFarmRoundGenerator.RoundsPerSession, "animalbabies_hint", "animalbabies_demo"));
            Navigator.Register(ScreenId.AnimalClassification, new DropSortScreen(ScreenId.AnimalClassification, ScreenId.ZooFarm, "world/zoofarm_bg",
                (level, rng) => DropSortRoundBuilder.Create(ZooFarmRoundGenerator.DropSortCatalogue(ZooFarmGameKind.Classification, level), level, rng),
                p => p.AnimalClassificationLevel, (p, v) => p.AnimalClassificationLevel = v, p => p.AnimalClassificationBuffer,
                DropSortRoundBuilder.RoundsPerSession, "zoofarm/animal_", "zoofarm/bucket_",
                "zoofarm_prompt_classification", "sorting_drag_hint", "sorting_drag_demo", "zoofarm_ds_", true));
            Navigator.Register(ScreenId.Geography, new MatchScreen(ScreenId.Geography, ScreenId.ZooFarm, "world/zoofarm_bg",
                (level, rng, prev) => GeographyRoundGenerator.Create(level, rng, prev),
                p => p.GeographyLevel, (p, v) => p.GeographyLevel = v, p => p.GeographyBuffer,
                GeographyRoundGenerator.RoundsPerSession, "geography_hint", "geography_demo"));
            Navigator.Register(ScreenId.ScienceLab, new BuildingScreen(BuildingId.ScienceLab));
            Navigator.Register(ScreenId.SinkOrFloat, new SinkOrFloatScreen(ScreenId.SinkOrFloat, ScreenId.ScienceLab, "world/sink_float_bg"));
            Navigator.Register(ScreenId.Magnet, new MagnetTableScreen(ScreenId.Magnet, ScreenId.ScienceLab, "world/magnet_bg"));
            Navigator.Register(ScreenId.LivingVsNonLiving, new DropSortScreen(ScreenId.LivingVsNonLiving, ScreenId.ScienceLab, "world/sciencelab_bg",
                (level, rng) => DropSortRoundBuilder.Create(ScienceLabRoundGenerator.DropSortCatalogue(ScienceLabGameKind.LivingVsNonLiving), level, rng),
                p => p.LivingVsNonLivingLevel, (p, v) => p.LivingVsNonLivingLevel = v, p => p.LivingVsNonLivingBuffer,
                DropSortRoundBuilder.RoundsPerSession, "sciencelab/object_", "sciencelab/bucket_",
                "sciencelab_prompt_livingvsnonliving", "sorting_drag_hint", "sorting_drag_demo", "sciencelab_ds_", true));
            Navigator.Register(ScreenId.PlantGrowth, new SequenceScreen(ScreenId.PlantGrowth, ScreenId.ScienceLab, "world/sciencelab_bg", "sciencelab/stage_",
                (level, rng) => PlantGrowthRoundGenerator.Create(level, rng),
                p => p.PlantGrowthLevel, (p, v) => p.PlantGrowthLevel = v, p => p.PlantGrowthBuffer,
                PlantGrowthRoundGenerator.RoundsPerSession, "plantgrowth_prompt", "plantgrowth_hint", "plantgrowth_demo"));
            Navigator.Register(ScreenId.HumanSenses, new MatchScreen(ScreenId.HumanSenses, ScreenId.ScienceLab, "world/sciencelab_bg",
                (level, rng, prev) => ScienceLabRoundGenerator.Create(ScienceLabGameKind.HumanSenses, level, rng, prev),
                p => p.HumanSensesLevel, (p, v) => p.HumanSensesLevel = v, p => p.HumanSensesBuffer,
                ScienceLabRoundGenerator.RoundsPerSession, "humansenses_hint", "humansenses_demo"));
            Navigator.Register(ScreenId.HealthyVsUnhealthy, new MatchScreen(ScreenId.HealthyVsUnhealthy, ScreenId.ScienceLab, "world/sciencelab_bg",
                (level, rng, prev) => ScienceLabRoundGenerator.Create(ScienceLabGameKind.HealthyVsUnhealthy, level, rng, prev),
                p => p.HealthyVsUnhealthyLevel, (p, v) => p.HealthyVsUnhealthyLevel = v, p => p.HealthyVsUnhealthyBuffer,
                ScienceLabRoundGenerator.RoundsPerSession, "healthyvsunhealthy_hint", "healthyvsunhealthy_demo"));
            Navigator.Register(ScreenId.Weather, new MatchScreen(ScreenId.Weather, ScreenId.ScienceLab, "world/sciencelab_bg",
                (level, rng, prev) => ScienceLabRoundGenerator.Create(ScienceLabGameKind.Weather, level, rng, prev),
                p => p.WeatherLevel, (p, v) => p.WeatherLevel = v, p => p.WeatherBuffer,
                ScienceLabRoundGenerator.RoundsPerSession, "weather_hint", "weather_demo"));
            Navigator.Register(ScreenId.DressForWeather, new MatchScreen(ScreenId.DressForWeather, ScreenId.ScienceLab, "world/sciencelab_bg",
                (level, rng, prev) => ScienceLabRoundGenerator.Create(ScienceLabGameKind.DressForWeather, level, rng, prev),
                p => p.DressForWeatherLevel, (p, v) => p.DressForWeatherLevel = v, p => p.DressForWeatherBuffer,
                ScienceLabRoundGenerator.RoundsPerSession, "dressforweather_hint", "dressforweather_demo"));
            Navigator.Register(ScreenId.CauseAndEffect, new MatchScreen(ScreenId.CauseAndEffect, ScreenId.ScienceLab, "world/sciencelab_bg",
                (level, rng, prev) => ScienceLabRoundGenerator.Create(ScienceLabGameKind.CauseAndEffect, level, rng, prev),
                p => p.CauseAndEffectLevel, (p, v) => p.CauseAndEffectLevel = v, p => p.CauseAndEffectBuffer,
                ScienceLabRoundGenerator.RoundsPerSession, "causeandeffect_hint", "causeandeffect_demo"));
            Navigator.Register(ScreenId.CookingMeasures, new MatchScreen(ScreenId.CookingMeasures, ScreenId.ScienceLab, "world/sciencelab_bg",
                (level, rng, prev) => ScienceLabRoundGenerator.Create(ScienceLabGameKind.CookingMeasures, level, rng, prev),
                p => p.CookingMeasuresLevel, (p, v) => p.CookingMeasuresLevel = v, p => p.CookingMeasuresBuffer,
                ScienceLabRoundGenerator.RoundsPerSession, "cookingmeasures_hint", "cookingmeasures_demo"));
            Navigator.Register(ScreenId.Seasons, new DropSortScreen(ScreenId.Seasons, ScreenId.ScienceLab, "world/sciencelab_bg",
                (level, rng) => DropSortRoundBuilder.Create(ScienceLabRoundGenerator.DropSortCatalogue(ScienceLabGameKind.Seasons), level, rng),
                p => p.SeasonsLevel, (p, v) => p.SeasonsLevel = v, p => p.SeasonsBuffer,
                DropSortRoundBuilder.RoundsPerSession, "sciencelab/activity_", "sciencelab/season_",
                "sciencelab_prompt_seasons", "sorting_drag_hint", "sorting_drag_demo", "sciencelab_ds_", true));
            Navigator.Register(ScreenId.DayNight, new DropSortScreen(ScreenId.DayNight, ScreenId.ScienceLab, "world/sciencelab_bg",
                (level, rng) => DropSortRoundBuilder.Create(ScienceLabRoundGenerator.DropSortCatalogue(ScienceLabGameKind.DayNight), level, rng),
                p => p.DayNightLevel, (p, v) => p.DayNightLevel = v, p => p.DayNightBuffer,
                DropSortRoundBuilder.RoundsPerSession, "sciencelab/activity_", "sciencelab/daynight_",
                "sciencelab_prompt_daynight", "sorting_drag_hint", "sorting_drag_demo", "sciencelab_ds_", true));
            Navigator.Register(ScreenId.Space, new MatchScreen(ScreenId.Space, ScreenId.ScienceLab, "world/sciencelab_bg",
                (level, rng, prev) => ScienceLabRoundGenerator.Create(ScienceLabGameKind.Space, level, rng, prev),
                p => p.SpaceLevel, (p, v) => p.SpaceLevel = v, p => p.SpaceBuffer,
                ScienceLabRoundGenerator.RoundsPerSession, "space_hint", "space_demo"));
            Navigator.Register(ScreenId.Workshop, new BuildingScreen(BuildingId.Workshop));
            Navigator.Register(ScreenId.BuildACar, new AssemblyScreen(ScreenId.BuildACar, ScreenId.Workshop, "world/workshop_bg", WorkshopBuildKind.Car,
                p => p.BuildACarLevel, (p, v) => p.BuildACarLevel = v, p => p.BuildACarBuffer, "buildacar_hint", "buildacar_demo"));
            Navigator.Register(ScreenId.BuildARocket, new AssemblyScreen(ScreenId.BuildARocket, ScreenId.Workshop, "world/workshop_bg", WorkshopBuildKind.Rocket,
                p => p.BuildARocketLevel, (p, v) => p.BuildARocketLevel = v, p => p.BuildARocketBuffer, "buildarocket_hint", "buildarocket_demo"));
            Navigator.Register(ScreenId.BuildAHouse, new AssemblyScreen(ScreenId.BuildAHouse, ScreenId.Workshop, "world/workshop_bg", WorkshopBuildKind.House,
                p => p.BuildAHouseLevel, (p, v) => p.BuildAHouseLevel = v, p => p.BuildAHouseBuffer, "buildahouse_hint", "buildahouse_demo"));
            Navigator.Register(ScreenId.BuildABoat, new AssemblyScreen(ScreenId.BuildABoat, ScreenId.Workshop, "world/workshop_bg", WorkshopBuildKind.Boat,
                p => p.BuildABoatLevel, (p, v) => p.BuildABoatLevel = v, p => p.BuildABoatBuffer, "buildaboat_hint", "buildaboat_demo"));
            Navigator.Register(ScreenId.BuildARobot, new AssemblyScreen(ScreenId.BuildARobot, ScreenId.Workshop, "world/workshop_bg", WorkshopBuildKind.Robot,
                p => p.BuildARobotLevel, (p, v) => p.BuildARobotLevel = v, p => p.BuildARobotBuffer, "buildarobot_hint", "buildarobot_demo"));
            Navigator.Register(ScreenId.BridgeBuilding, new AssemblyScreen(ScreenId.BridgeBuilding, ScreenId.Workshop, "world/workshop_bg", WorkshopBuildKind.Bridge,
                p => p.BridgeBuildingLevel, (p, v) => p.BridgeBuildingLevel = v, p => p.BridgeBuildingBuffer, "bridgebuilding_hint", "bridgebuilding_demo"));
            Navigator.Register(ScreenId.SimplePhysics, new AssemblyScreen(ScreenId.SimplePhysics, ScreenId.Workshop, "world/workshop_bg", WorkshopBuildKind.SimplePhysics,
                p => p.SimplePhysicsLevel, (p, v) => p.SimplePhysicsLevel = v, p => p.SimplePhysicsBuffer, "simplephysics_hint", "simplephysics_demo"));
            Navigator.Register(ScreenId.ToolSelection, new MatchScreen(ScreenId.ToolSelection, ScreenId.Workshop, "world/workshop_bg",
                (level, rng, prev) => WorkshopMatchRoundGenerator.Create(WorkshopMatchGameKind.ToolSelection, level, rng, prev),
                p => p.ToolSelectionLevel, (p, v) => p.ToolSelectionLevel = v, p => p.ToolSelectionBuffer,
                WorkshopMatchRoundGenerator.RoundsPerSession, "toolselection_hint", "toolselection_demo"));
            Navigator.Register(ScreenId.Balance, new MatchScreen(ScreenId.Balance, ScreenId.Workshop, "world/workshop_bg",
                (level, rng, prev) => WorkshopMatchRoundGenerator.Create(WorkshopMatchGameKind.Balance, level, rng, prev),
                p => p.BalanceLevel, (p, v) => p.BalanceLevel = v, p => p.BalanceBuffer,
                WorkshopMatchRoundGenerator.RoundsPerSession, "balance_hint", "balance_demo"));
            Navigator.Register(ScreenId.HelpTheCharacter, new MatchScreen(ScreenId.HelpTheCharacter, ScreenId.Workshop, "world/workshop_bg",
                (level, rng, prev) => WorkshopMatchRoundGenerator.Create(WorkshopMatchGameKind.HelpTheCharacter, level, rng, prev),
                p => p.HelpTheCharacterLevel, (p, v) => p.HelpTheCharacterLevel = v, p => p.HelpTheCharacterBuffer,
                WorkshopMatchRoundGenerator.RoundsPerSession, "helpthecharacter_hint", "helpthecharacter_demo"));

            Navigator.Register(ScreenId.ArtStudio, new BuildingScreen(BuildingId.ArtStudio));
            Navigator.Register(ScreenId.TraceShapes, new TraceScreen(ScreenId.TraceShapes, ScreenId.ArtStudio, "world/artstudio_bg", TraceGameKind.Shapes,
                p => p.TraceShapesLevel, (p, v) => p.TraceShapesLevel = v, p => p.TraceShapesBuffer, "traceshapes_hint", "traceshapes_demo"));
            Navigator.Register(ScreenId.TraceLetters, new TraceScreen(ScreenId.TraceLetters, ScreenId.ArtStudio, "world/artstudio_bg", TraceGameKind.Letters,
                p => p.TraceLettersLevel, (p, v) => p.TraceLettersLevel = v, p => p.TraceLettersBuffer, "traceletters_hint", "traceletters_demo"));
            Navigator.Register(ScreenId.TraceNumbers, new TraceScreen(ScreenId.TraceNumbers, ScreenId.ArtStudio, "world/artstudio_bg", TraceGameKind.Numbers,
                p => p.TraceNumbersLevel, (p, v) => p.TraceNumbersLevel = v, p => p.TraceNumbersBuffer, "tracenumbers_hint", "tracenumbers_demo"));
            Navigator.Register(ScreenId.ColorByNumber, new MatchScreen(ScreenId.ColorByNumber, ScreenId.ArtStudio, "world/artstudio_bg",
                (level, rng, prev) => ArtStudioMatchRoundGenerator.Create(ArtStudioMatchGameKind.ColorByNumber, level, rng, prev),
                p => p.ColorByNumberLevel, (p, v) => p.ColorByNumberLevel = v, p => p.ColorByNumberBuffer,
                ArtStudioMatchRoundGenerator.RoundsPerSession, "colorbynumber_hint", "colorbynumber_demo"));
            Navigator.Register(ScreenId.ColorByInstruction, new MatchScreen(ScreenId.ColorByInstruction, ScreenId.ArtStudio, "world/artstudio_bg",
                (level, rng, prev) => ArtStudioMatchRoundGenerator.Create(ArtStudioMatchGameKind.ColorByInstruction, level, rng, prev),
                p => p.ColorByInstructionLevel, (p, v) => p.ColorByInstructionLevel = v, p => p.ColorByInstructionBuffer,
                ArtStudioMatchRoundGenerator.RoundsPerSession, "colorbyinstruction_hint", "colorbyinstruction_demo"));
            Navigator.Register(ScreenId.FinishTheDrawing, new MatchScreen(ScreenId.FinishTheDrawing, ScreenId.ArtStudio, "world/artstudio_bg",
                (level, rng, prev) => ArtStudioMatchRoundGenerator.Create(ArtStudioMatchGameKind.FinishTheDrawing, level, rng, prev),
                p => p.FinishTheDrawingLevel, (p, v) => p.FinishTheDrawingLevel = v, p => p.FinishTheDrawingBuffer,
                ArtStudioMatchRoundGenerator.RoundsPerSession, "finishthedrawing_hint", "finishthedrawing_demo"));
            Navigator.Register(ScreenId.DrawWhatYouHear, new MatchScreen(ScreenId.DrawWhatYouHear, ScreenId.ArtStudio, "world/artstudio_bg",
                (level, rng, prev) => ArtStudioMatchRoundGenerator.Create(ArtStudioMatchGameKind.DrawWhatYouHear, level, rng, prev),
                p => p.DrawWhatYouHearLevel, (p, v) => p.DrawWhatYouHearLevel = v, p => p.DrawWhatYouHearBuffer,
                ArtStudioMatchRoundGenerator.RoundsPerSession, "drawwhatyouhear_hint", "drawwhatyouhear_demo"));
            Navigator.Register(ScreenId.GuidedDrawing, new SequenceScreen(ScreenId.GuidedDrawing, ScreenId.ArtStudio, "world/artstudio_bg", "artstudio/step_",
                (level, rng) => ArtStudioSequenceRoundGenerator.Create(ArtStudioSequenceGameKind.GuidedDrawing, level, rng),
                p => p.GuidedDrawingLevel, (p, v) => p.GuidedDrawingLevel = v, p => p.GuidedDrawingBuffer,
                ArtStudioSequenceRoundGenerator.RoundsPerSession, "guideddrawing_prompt", "guideddrawing_hint", "guideddrawing_demo"));
            Navigator.Register(ScreenId.DrawingChallenges, new SequenceScreen(ScreenId.DrawingChallenges, ScreenId.ArtStudio, "world/artstudio_bg", "artstudio/challenge_step_",
                (level, rng) => ArtStudioSequenceRoundGenerator.Create(ArtStudioSequenceGameKind.DrawingChallenges, level, rng),
                p => p.DrawingChallengesLevel, (p, v) => p.DrawingChallengesLevel = v, p => p.DrawingChallengesBuffer,
                ArtStudioSequenceRoundGenerator.RoundsPerSession, "drawingchallenges_prompt", "drawingchallenges_hint", "drawingchallenges_demo"));
            Navigator.Register(ScreenId.FreeDrawing, new FreeDrawingScreen());

            Navigator.Register(ScreenId.BrainGym, new BuildingScreen(BuildingId.BrainGym));
            Navigator.Register(ScreenId.ClassicMemory, new MemoryBoardScreen(ScreenId.ClassicMemory, ScreenId.BrainGym, "world/braingym_bg",
                (level, rng) => MemoryBoardRoundGenerator.Create(level, rng),
                p => p.ClassicMemoryLevel, (p, v) => p.ClassicMemoryLevel = v, p => p.ClassicMemoryBuffer,
                MemoryBoardRoundGenerator.RoundsPerSession, "classicmemory_prompt", "classicmemory_hint", "classicmemory_demo"));
            Navigator.Register(ScreenId.RememberTheSequence, new SequenceRecallScreen(ScreenId.RememberTheSequence, ScreenId.BrainGym, "world/braingym_bg",
                SequenceRecallRoundGenerator.RememberTheSequenceTileSpritePrefix,
                (level, rng) => SequenceRecallRoundGenerator.Create(SequenceRecallGameKind.RememberTheSequence, level, rng),
                p => p.RememberTheSequenceLevel, (p, v) => p.RememberTheSequenceLevel = v, p => p.RememberTheSequenceBuffer,
                SequenceRecallRoundGenerator.RoundsPerSession, "rememberthesequence_prompt", "rememberthesequence_hint", "rememberthesequence_demo"));
            Navigator.Register(ScreenId.SimonSays, new SequenceRecallScreen(ScreenId.SimonSays, ScreenId.BrainGym, "world/braingym_bg",
                SequenceRecallRoundGenerator.SimonSaysTileSpritePrefix,
                (level, rng) => SequenceRecallRoundGenerator.Create(SequenceRecallGameKind.SimonSays, level, rng),
                p => p.SimonSaysLevel, (p, v) => p.SimonSaysLevel = v, p => p.SimonSaysBuffer,
                SequenceRecallRoundGenerator.RoundsPerSession, "simonsays_prompt", "simonsays_hint", "simonsays_demo"));
            Navigator.Register(ScreenId.WhatsDisappeared, new MatchScreen(ScreenId.WhatsDisappeared, ScreenId.BrainGym, "world/braingym_bg",
                (level, rng, prev) => BrainGymMatchRoundGenerator.Create(BrainGymMatchGameKind.WhatsDisappeared, level, rng, prev),
                p => p.WhatsDisappearedLevel, (p, v) => p.WhatsDisappearedLevel = v, p => p.WhatsDisappearedBuffer,
                BrainGymMatchRoundGenerator.RoundsPerSession, "whatsdisappeared_hint", "whatsdisappeared_demo"));
            Navigator.Register(ScreenId.RememberTheLocation, new MatchScreen(ScreenId.RememberTheLocation, ScreenId.BrainGym, "world/braingym_bg",
                (level, rng, prev) => BrainGymMatchRoundGenerator.Create(BrainGymMatchGameKind.RememberTheLocation, level, rng, prev),
                p => p.RememberTheLocationLevel, (p, v) => p.RememberTheLocationLevel = v, p => p.RememberTheLocationBuffer,
                BrainGymMatchRoundGenerator.RoundsPerSession, "rememberthelocation_hint", "rememberthelocation_demo"));
            Navigator.Register(ScreenId.SameOrDifferent, new MatchScreen(ScreenId.SameOrDifferent, ScreenId.BrainGym, "world/braingym_bg",
                (level, rng, prev) => BrainGymMatchRoundGenerator.Create(BrainGymMatchGameKind.SameOrDifferent, level, rng, prev),
                p => p.SameOrDifferentLevel, (p, v) => p.SameOrDifferentLevel = v, p => p.SameOrDifferentBuffer,
                BrainGymMatchRoundGenerator.RoundsPerSession, "sameordifferent_hint", "sameordifferent_demo"));
            Navigator.Register(ScreenId.MatchRotation, new MatchScreen(ScreenId.MatchRotation, ScreenId.BrainGym, "world/braingym_bg",
                (level, rng, prev) => BrainGymMatchRoundGenerator.Create(BrainGymMatchGameKind.MatchRotation, level, rng, prev),
                p => p.MatchRotationLevel, (p, v) => p.MatchRotationLevel = v, p => p.MatchRotationBuffer,
                BrainGymMatchRoundGenerator.RoundsPerSession, "matchrotation_hint", "matchrotation_demo"));
            Navigator.Register(ScreenId.WhichIsBigger, new MatchScreen(ScreenId.WhichIsBigger, ScreenId.BrainGym, "world/braingym_bg",
                (level, rng, prev) => BrainGymMatchRoundGenerator.Create(BrainGymMatchGameKind.WhichIsBigger, level, rng, prev),
                p => p.WhichIsBiggerLevel, (p, v) => p.WhichIsBiggerLevel = v, p => p.WhichIsBiggerBuffer,
                BrainGymMatchRoundGenerator.RoundsPerSession, "whichisbigger_hint", "whichisbigger_demo"));
            Navigator.Register(ScreenId.CompleteThePicture, new MatchScreen(ScreenId.CompleteThePicture, ScreenId.BrainGym, "world/braingym_bg",
                (level, rng, prev) => BrainGymMatchRoundGenerator.Create(BrainGymMatchGameKind.CompleteThePicture, level, rng, prev),
                p => p.CompleteThePictureLevel, (p, v) => p.CompleteThePictureLevel = v, p => p.CompleteThePictureBuffer,
                BrainGymMatchRoundGenerator.RoundsPerSession, "completethepicture_hint", "completethepicture_demo"));
            Navigator.Register(ScreenId.FindTheDifferences, new MatchScreen(ScreenId.FindTheDifferences, ScreenId.BrainGym, "world/braingym_bg",
                (level, rng, prev) => BrainGymMatchRoundGenerator.Create(BrainGymMatchGameKind.FindTheDifferences, level, rng, prev),
                p => p.FindTheDifferencesLevel, (p, v) => p.FindTheDifferencesLevel = v, p => p.FindTheDifferencesBuffer,
                BrainGymMatchRoundGenerator.RoundsPerSession, "findthedifferences_hint", "findthedifferences_demo"));
            Navigator.Register(ScreenId.SpotTheObject, new MatchScreen(ScreenId.SpotTheObject, ScreenId.BrainGym, "world/braingym_bg",
                (level, rng, prev) => BrainGymMatchRoundGenerator.Create(BrainGymMatchGameKind.SpotTheObject, level, rng, prev),
                p => p.SpotTheObjectLevel, (p, v) => p.SpotTheObjectLevel = v, p => p.SpotTheObjectBuffer,
                BrainGymMatchRoundGenerator.RoundsPerSession, "spottheobject_hint", "spottheobject_demo"));
            Navigator.Register(ScreenId.FollowThePath, new MatchScreen(ScreenId.FollowThePath, ScreenId.BrainGym, "world/braingym_bg",
                (level, rng, prev) => BrainGymMatchRoundGenerator.Create(BrainGymMatchGameKind.FollowThePath, level, rng, prev),
                p => p.FollowThePathLevel, (p, v) => p.FollowThePathLevel = v, p => p.FollowThePathBuffer,
                BrainGymMatchRoundGenerator.RoundsPerSession, "followthepath_hint", "followthepath_demo"));
            Navigator.Register(ScreenId.WhatsBehind, new MatchScreen(ScreenId.WhatsBehind, ScreenId.BrainGym, "world/braingym_bg",
                (level, rng, prev) => BrainGymMatchRoundGenerator.Create(BrainGymMatchGameKind.WhatsBehind, level, rng, prev),
                p => p.WhatsBehindLevel, (p, v) => p.WhatsBehindLevel = v, p => p.WhatsBehindBuffer,
                BrainGymMatchRoundGenerator.RoundsPerSession, "whatsbehind_hint", "whatsbehind_demo"));
            Navigator.Register(ScreenId.Perspective, new MatchScreen(ScreenId.Perspective, ScreenId.BrainGym, "world/braingym_bg",
                (level, rng, prev) => BrainGymMatchRoundGenerator.Create(BrainGymMatchGameKind.Perspective, level, rng, prev),
                p => p.PerspectiveLevel, (p, v) => p.PerspectiveLevel = v, p => p.PerspectiveBuffer,
                BrainGymMatchRoundGenerator.RoundsPerSession, "perspective_hint", "perspective_demo"));
            Navigator.Register(ScreenId.CopyTheConstruction, new MatchScreen(ScreenId.CopyTheConstruction, ScreenId.BrainGym, "world/braingym_bg",
                (level, rng, prev) => BrainGymMatchRoundGenerator.Create(BrainGymMatchGameKind.CopyTheConstruction, level, rng, prev),
                p => p.CopyTheConstructionLevel, (p, v) => p.CopyTheConstructionLevel = v, p => p.CopyTheConstructionBuffer,
                BrainGymMatchRoundGenerator.RoundsPerSession, "copytheconstruction_hint", "copytheconstruction_demo"));
            Navigator.Register(ScreenId.FindTheMissingPiece, new MatchScreen(ScreenId.FindTheMissingPiece, ScreenId.BrainGym, "world/braingym_bg",
                (level, rng, prev) => BrainGymMatchRoundGenerator.Create(BrainGymMatchGameKind.FindTheMissingPiece, level, rng, prev),
                p => p.FindTheMissingPieceLevel, (p, v) => p.FindTheMissingPieceLevel = v, p => p.FindTheMissingPieceBuffer,
                BrainGymMatchRoundGenerator.RoundsPerSession, "findthemissingpiece_hint", "findthemissingpiece_demo"));
            // Answer-variety Prototype B (docs/kids-games/answer-variety-prototypes.md): Sorting as drop-sort. The tap
            // version stays registered behind SortingUsesDrop so the two can be compared on the device.
            if (SortingUsesDrop)
                Navigator.Register(ScreenId.Sorting, new DropSortScreen(ScreenId.Sorting, ScreenId.BrainGym, "world/braingym_bg",
                    DropSortRoundBuilder.Create,
                    p => p.SortingLevel, (p, v) => p.SortingLevel = v, p => p.SortingBuffer,
                    DropSortRoundBuilder.RoundsPerSession, "braingym/sortitem_", "braingym/category_",
                    "braingym_prompt_sorting", "sorting_drag_hint", "sorting_drag_demo", "braingym_category_"));
            else
                Navigator.Register(ScreenId.Sorting, new MatchScreen(ScreenId.Sorting, ScreenId.BrainGym, "world/braingym_bg",
                    (level, rng, prev) => BrainGymMatchRoundGenerator.Create(BrainGymMatchGameKind.Sorting, level, rng, prev),
                    p => p.SortingLevel, (p, v) => p.SortingLevel = v, p => p.SortingBuffer,
                    BrainGymMatchRoundGenerator.RoundsPerSession, "sorting_hint", "sorting_demo"));
            // Step 6 of the answer-variety plan: the other three Brain Gym sorting games on the drop-sort presenter.
            // They reuse Sorting's drag hint and demonstration lines.
            Navigator.Register(ScreenId.Recycling, new DropSortScreen(ScreenId.Recycling, ScreenId.BrainGym, "world/braingym_bg",
                (level, rng) => DropSortRoundBuilder.Create(DropSortRoundBuilder.RecyclingCatalogue, level, rng),
                p => p.RecyclingLevel, (p, v) => p.RecyclingLevel = v, p => p.RecyclingBuffer,
                DropSortRoundBuilder.RoundsPerSession, "braingym/waste_", "braingym/bin_",
                "braingym_prompt_recycling", "sorting_drag_hint", "sorting_drag_demo", "braingym_bin_"));
            Navigator.Register(ScreenId.MatchItemToCategory, new DropSortScreen(ScreenId.MatchItemToCategory, ScreenId.BrainGym, "world/braingym_bg",
                (level, rng) => DropSortRoundBuilder.Create(DropSortRoundBuilder.ItemToCategoryCatalogue, level, rng),
                p => p.MatchItemToCategoryLevel, (p, v) => p.MatchItemToCategoryLevel = v, p => p.MatchItemToCategoryBuffer,
                DropSortRoundBuilder.RoundsPerSession, "braingym/catitem_", "braingym/categorylabel_",
                "braingym_prompt_matchitemtocategory", "sorting_drag_hint", "sorting_drag_demo", "braingym_categorylabel_"));
            Navigator.Register(ScreenId.SortLaundryChores, new DropSortScreen(ScreenId.SortLaundryChores, ScreenId.BrainGym, "world/braingym_bg",
                (level, rng) => DropSortRoundBuilder.Create(DropSortRoundBuilder.ChoresCatalogue, level, rng),
                p => p.SortLaundryChoresLevel, (p, v) => p.SortLaundryChoresLevel = v, p => p.SortLaundryChoresBuffer,
                DropSortRoundBuilder.RoundsPerSession, "braingym/choreitem_", "braingym/room_",
                "braingym_prompt_sortlaundrychores", "sorting_drag_hint", "sorting_drag_demo", "braingym_room_"));
            Navigator.Register(ScreenId.BrainGymSequenceOrdering, new SequenceScreen(ScreenId.BrainGymSequenceOrdering, ScreenId.BrainGym, "world/braingym_bg",
                BrainGymSequenceOrderingRoundGenerator.TileSpritePrefix,
                (level, rng) => BrainGymSequenceOrderingRoundGenerator.Create(level, rng),
                p => p.BrainGymSequenceOrderingLevel, (p, v) => p.BrainGymSequenceOrderingLevel = v, p => p.BrainGymSequenceOrderingBuffer,
                BrainGymSequenceOrderingRoundGenerator.RoundsPerSession, "sequenceorderingbg_prompt", "sequenceorderingbg_hint", "sequenceorderingbg_demo"));

            Navigator.Register(ScreenId.FriendsPark, new BuildingScreen(BuildingId.FriendsPark));
            Navigator.Register(ScreenId.EmotionMatching, new MatchScreen(ScreenId.EmotionMatching, ScreenId.FriendsPark, "world/friendspark_bg",
                (level, rng, prev) => FriendsParkMatchRoundGenerator.Create(FriendsParkMatchGameKind.EmotionMatching, level, rng, prev),
                p => p.EmotionMatchingLevel, (p, v) => p.EmotionMatchingLevel = v, p => p.EmotionMatchingBuffer,
                FriendsParkMatchRoundGenerator.RoundsPerSession, "emotionmatching_hint", "emotionmatching_demo"));
            Navigator.Register(ScreenId.FacialExpressionGame, new MatchScreen(ScreenId.FacialExpressionGame, ScreenId.FriendsPark, "world/friendspark_bg",
                (level, rng, prev) => FriendsParkMatchRoundGenerator.Create(FriendsParkMatchGameKind.FacialExpressionGame, level, rng, prev),
                p => p.FacialExpressionGameLevel, (p, v) => p.FacialExpressionGameLevel = v, p => p.FacialExpressionGameBuffer,
                FriendsParkMatchRoundGenerator.RoundsPerSession, "facialexpression_hint", "facialexpression_demo"));
            Navigator.Register(ScreenId.WhatWouldYouDo, new MatchScreen(ScreenId.WhatWouldYouDo, ScreenId.FriendsPark, "world/friendspark_bg",
                (level, rng, prev) => FriendsParkMatchRoundGenerator.Create(FriendsParkMatchGameKind.WhatWouldYouDo, level, rng, prev),
                p => p.WhatWouldYouDoLevel, (p, v) => p.WhatWouldYouDoLevel = v, p => p.WhatWouldYouDoBuffer,
                FriendsParkMatchRoundGenerator.RoundsPerSession, "whatwouldyoudo_hint", "whatwouldyoudo_demo"));
            Navigator.Register(ScreenId.Empathy, new MatchScreen(ScreenId.Empathy, ScreenId.FriendsPark, "world/friendspark_bg",
                (level, rng, prev) => FriendsParkMatchRoundGenerator.Create(FriendsParkMatchGameKind.Empathy, level, rng, prev),
                p => p.EmpathyLevel, (p, v) => p.EmpathyLevel = v, p => p.EmpathyBuffer,
                FriendsParkMatchRoundGenerator.RoundsPerSession, "empathy_hint", "empathy_demo"));
            Navigator.Register(ScreenId.SocialSituations, new MatchScreen(ScreenId.SocialSituations, ScreenId.FriendsPark, "world/friendspark_bg",
                (level, rng, prev) => FriendsParkMatchRoundGenerator.Create(FriendsParkMatchGameKind.SocialSituations, level, rng, prev),
                p => p.SocialSituationsLevel, (p, v) => p.SocialSituationsLevel = v, p => p.SocialSituationsBuffer,
                FriendsParkMatchRoundGenerator.RoundsPerSession, "socialsituations_hint", "socialsituations_demo"));
            Navigator.Register(ScreenId.ListenAndChoose, new MatchScreen(ScreenId.ListenAndChoose, ScreenId.FriendsPark, "world/friendspark_bg",
                (level, rng, prev) => FriendsParkMatchRoundGenerator.Create(FriendsParkMatchGameKind.ListenAndChoose, level, rng, prev),
                p => p.ListenAndChooseLevel, (p, v) => p.ListenAndChooseLevel = v, p => p.ListenAndChooseBuffer,
                FriendsParkMatchRoundGenerator.RoundsPerSession, "listenandchoose_hint", "listenandchoose_demo"));
            Navigator.Register(ScreenId.ListenForDetails, new MatchScreen(ScreenId.ListenForDetails, ScreenId.FriendsPark, "world/friendspark_bg",
                (level, rng, prev) => FriendsParkMatchRoundGenerator.Create(FriendsParkMatchGameKind.ListenForDetails, level, rng, prev),
                p => p.ListenForDetailsLevel, (p, v) => p.ListenForDetailsLevel = v, p => p.ListenForDetailsBuffer,
                FriendsParkMatchRoundGenerator.RoundsPerSession, "listenfordetails_hint", "listenfordetails_demo"));
            Navigator.Register(ScreenId.Follow1Instruction, new SequenceScreen(ScreenId.Follow1Instruction, ScreenId.FriendsPark, "world/friendspark_bg",
                FollowInstructionsRoundGenerator.TileSpritePrefix,
                (level, rng) => FollowInstructionsRoundGenerator.Create(1, level, rng),
                p => p.Follow1InstructionLevel, (p, v) => p.Follow1InstructionLevel = v, p => p.Follow1InstructionBuffer,
                FollowInstructionsRoundGenerator.RoundsPerSession, "follow1instruction_prompt", "follow1instruction_hint", "follow1instruction_demo"));
            Navigator.Register(ScreenId.Follow2Instructions, new SequenceScreen(ScreenId.Follow2Instructions, ScreenId.FriendsPark, "world/friendspark_bg",
                FollowInstructionsRoundGenerator.TileSpritePrefix,
                (level, rng) => FollowInstructionsRoundGenerator.Create(2, level, rng),
                p => p.Follow2InstructionsLevel, (p, v) => p.Follow2InstructionsLevel = v, p => p.Follow2InstructionsBuffer,
                FollowInstructionsRoundGenerator.RoundsPerSession, "follow2instructions_prompt", "follow2instructions_hint", "follow2instructions_demo"));
            Navigator.Register(ScreenId.Follow3Instructions, new SequenceScreen(ScreenId.Follow3Instructions, ScreenId.FriendsPark, "world/friendspark_bg",
                FollowInstructionsRoundGenerator.TileSpritePrefix,
                (level, rng) => FollowInstructionsRoundGenerator.Create(3, level, rng),
                p => p.Follow3InstructionsLevel, (p, v) => p.Follow3InstructionsLevel = v, p => p.Follow3InstructionsBuffer,
                FollowInstructionsRoundGenerator.RoundsPerSession, "follow3instructions_prompt", "follow3instructions_hint", "follow3instructions_demo"));
            Navigator.Register(ScreenId.RoadSafety, new MatchScreen(ScreenId.RoadSafety, ScreenId.FriendsPark, "world/friendspark_bg",
                (level, rng, prev) => FriendsParkMatchRoundGenerator.Create(FriendsParkMatchGameKind.RoadSafety, level, rng, prev),
                p => p.RoadSafetyLevel, (p, v) => p.RoadSafetyLevel = v, p => p.RoadSafetyBuffer,
                FriendsParkMatchRoundGenerator.RoundsPerSession, "roadsafety_hint", "roadsafety_demo"));
            Navigator.Register(ScreenId.SafetyScenarios, new MatchScreen(ScreenId.SafetyScenarios, ScreenId.FriendsPark, "world/friendspark_bg",
                (level, rng, prev) => FriendsParkMatchRoundGenerator.Create(FriendsParkMatchGameKind.SafetyScenarios, level, rng, prev),
                p => p.SafetyScenariosLevel, (p, v) => p.SafetyScenariosLevel = v, p => p.SafetyScenariosBuffer,
                FriendsParkMatchRoundGenerator.RoundsPerSession, "safetyscenarios_hint", "safetyscenarios_demo"));

            Navigator.Register(ScreenId.Arcade, new BuildingScreen(BuildingId.Arcade));
            Navigator.Register(ScreenId.BalloonPopping, new BalloonPoppingScreen());
            Navigator.Register(ScreenId.WhackAMole, new WhackAMoleScreen());
            Navigator.Register(ScreenId.Fishing, new FishingScreen());
            Navigator.Register(ScreenId.SpaceShooter, new SpaceShooterScreen());
            Navigator.Register(ScreenId.FruitCatcher, new FruitCatcherScreen());
            Navigator.Register(ScreenId.TreasureHunt, new TreasureHuntScreen());
            Navigator.Register(ScreenId.Platformer, new PlatformerScreen());
            Navigator.Register(ScreenId.JetpackCat, new JetpackCatScreen());

            var hudRoot = CreateSafeAreaPanel(canvas.transform, "HudRoot");
            Hud = hudRoot.gameObject.AddComponent<Hud>();
            Hud.Build(this, hudRoot);
            Hud.SetCoins(Progress.Coins);

            // Built last (and so drawn on top of every screen and the Hud, by plain sibling order under the
            // canvas - no screen ever needs to reach past its own Root to see it) and under its own safe-area
            // panel, matching every screen's Root exactly (same anchors, same offsets), so a canvas-unit
            // position from any screen (e.g. MapScreen.ScreenPositionOf) lines up here without translation.
            var guideRoot = CreateSafeAreaPanel(canvas.transform, "GuideRoot");
            TutorialGuide = new TutorialGuide(this, guideRoot);

            // First run (no saved character yet) opens the Creator instead of the Map; every later launch
            // goes straight to the Map since CreatorScreen.Confirm sets HasCharacter before it Commits.
            Navigator.Show(Progress.HasCharacter ? ScreenId.Map : ScreenId.Creator);
        }

        // Whether music should play (the Music switch in Settings).
        public bool MusicEnabled { get; private set; } = true;

        // Pushes the saved Music, Sound effects and Voice switches to the things they control.
        public void ApplyAudioSettings()
        {
            MusicEnabled = Progress.MusicEnabled;
            if (Music != null) Music.Enabled = MusicEnabled;
            Sfx.Enabled = Progress.SfxEnabled;
            Haptics.Enabled = Progress.SfxEnabled;
            Voice.Enabled = Progress.VoiceEnabled;
        }

        // Saves the progress and refreshes the coin counter; call after every change to Progress.
        public void Commit()
        {
            _save.Save(Progress);
            Hud.SetCoins(Progress.Coins);
        }

        private void OnDestroy()
        {
            if (EvaUi.Sfx == Sfx) EvaUi.Sfx = null;
        }

        // Pixels a finger may wobble before a press counts as a drag: 0.1 inch, never below Unity's default 10 px.
        public static int DragThreshold(float dpi) => Mathf.RoundToInt(Mathf.Max(10f, dpi * 0.1f));

        private static RectTransform CreateSafeAreaPanel(Transform parent, string name)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(SafeAreaPanel));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<SafeAreaPanel>().Apply(true);
            return (RectTransform)panel.transform;
        }
    }
}
