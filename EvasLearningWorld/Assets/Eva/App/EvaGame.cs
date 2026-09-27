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
        public Voice Voice { get; private set; }
        public Sfx Sfx { get; private set; }
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
            Navigator.Register(ScreenId.ZooFarmHabitat, new MatchScreen(ScreenId.ZooFarmHabitat, ScreenId.ZooFarm, "world/zoofarm_bg",
                (level, rng, prev) => ZooFarmRoundGenerator.Create(ZooFarmGameKind.Habitat, level, rng, prev),
                p => p.ZooFarmHabitatLevel, (p, v) => p.ZooFarmHabitatLevel = v, p => p.ZooFarmHabitatBuffer,
                ZooFarmRoundGenerator.RoundsPerSession, "zoofarm_habitat_hint", "zoofarm_habitat_demo"));
            Navigator.Register(ScreenId.ZooFarmMother, new MatchScreen(ScreenId.ZooFarmMother, ScreenId.ZooFarm, "world/zoofarm_bg",
                (level, rng, prev) => ZooFarmRoundGenerator.Create(ZooFarmGameKind.Mother, level, rng, prev),
                p => p.ZooFarmMotherLevel, (p, v) => p.ZooFarmMotherLevel = v, p => p.ZooFarmMotherBuffer,
                ZooFarmRoundGenerator.RoundsPerSession, "zoofarm_mother_hint", "zoofarm_mother_demo"));
            Navigator.Register(ScreenId.ZooFarmFood, new MatchScreen(ScreenId.ZooFarmFood, ScreenId.ZooFarm, "world/zoofarm_bg",
                (level, rng, prev) => ZooFarmRoundGenerator.Create(ZooFarmGameKind.Food, level, rng, prev),
                p => p.ZooFarmFoodLevel, (p, v) => p.ZooFarmFoodLevel = v, p => p.ZooFarmFoodBuffer,
                ZooFarmRoundGenerator.RoundsPerSession, "zoofarm_food_hint", "zoofarm_food_demo"));
            Navigator.Register(ScreenId.ZooFarmFootprint, new MatchScreen(ScreenId.ZooFarmFootprint, ScreenId.ZooFarm, "world/zoofarm_bg",
                (level, rng, prev) => ZooFarmRoundGenerator.Create(ZooFarmGameKind.Footprint, level, rng, prev),
                p => p.ZooFarmFootprintLevel, (p, v) => p.ZooFarmFootprintLevel = v, p => p.ZooFarmFootprintBuffer,
                ZooFarmRoundGenerator.RoundsPerSession, "zoofarm_footprint_hint", "zoofarm_footprint_demo"));
            Navigator.Register(ScreenId.ZooFarmCovering, new MatchScreen(ScreenId.ZooFarmCovering, ScreenId.ZooFarm, "world/zoofarm_bg",
                (level, rng, prev) => ZooFarmRoundGenerator.Create(ZooFarmGameKind.Covering, level, rng, prev),
                p => p.ZooFarmCoveringLevel, (p, v) => p.ZooFarmCoveringLevel = v, p => p.ZooFarmCoveringBuffer,
                ZooFarmRoundGenerator.RoundsPerSession, "zoofarm_covering_hint", "zoofarm_covering_demo"));
            Navigator.Register(ScreenId.ZooFarmSound, new MatchScreen(ScreenId.ZooFarmSound, ScreenId.ZooFarm, "world/zoofarm_bg",
                (level, rng, prev) => ZooFarmRoundGenerator.Create(ZooFarmGameKind.Sound, level, rng, prev),
                p => p.ZooFarmSoundLevel, (p, v) => p.ZooFarmSoundLevel = v, p => p.ZooFarmSoundBuffer,
                ZooFarmRoundGenerator.RoundsPerSession, "zoofarm_sound_hint", "zoofarm_sound_demo"));
            Navigator.Register(ScreenId.DomesticVsWild, new MatchScreen(ScreenId.DomesticVsWild, ScreenId.ZooFarm, "world/zoofarm_bg",
                (level, rng, prev) => ZooFarmRoundGenerator.Create(ZooFarmGameKind.DomesticVsWild, level, rng, prev),
                p => p.DomesticVsWildLevel, (p, v) => p.DomesticVsWildLevel = v, p => p.DomesticVsWildBuffer,
                ZooFarmRoundGenerator.RoundsPerSession, "domesticvswild_hint", "domesticvswild_demo"));
            Navigator.Register(ScreenId.LandSeaAir, new MatchScreen(ScreenId.LandSeaAir, ScreenId.ZooFarm, "world/zoofarm_bg",
                (level, rng, prev) => ZooFarmRoundGenerator.Create(ZooFarmGameKind.LandSeaAir, level, rng, prev),
                p => p.LandSeaAirLevel, (p, v) => p.LandSeaAirLevel = v, p => p.LandSeaAirBuffer,
                ZooFarmRoundGenerator.RoundsPerSession, "landseaair_hint", "landseaair_demo"));
            Navigator.Register(ScreenId.AnimalBabies, new MatchScreen(ScreenId.AnimalBabies, ScreenId.ZooFarm, "world/zoofarm_bg",
                (level, rng, prev) => ZooFarmRoundGenerator.Create(ZooFarmGameKind.Babies, level, rng, prev),
                p => p.ZooFarmBabiesLevel, (p, v) => p.ZooFarmBabiesLevel = v, p => p.ZooFarmBabiesBuffer,
                ZooFarmRoundGenerator.RoundsPerSession, "animalbabies_hint", "animalbabies_demo"));
            Navigator.Register(ScreenId.AnimalClassification, new MatchScreen(ScreenId.AnimalClassification, ScreenId.ZooFarm, "world/zoofarm_bg",
                (level, rng, prev) => ZooFarmRoundGenerator.Create(ZooFarmGameKind.Classification, level, rng, prev),
                p => p.AnimalClassificationLevel, (p, v) => p.AnimalClassificationLevel = v, p => p.AnimalClassificationBuffer,
                ZooFarmRoundGenerator.RoundsPerSession, "animalclassification_hint", "animalclassification_demo"));
            Navigator.Register(ScreenId.Geography, new MatchScreen(ScreenId.Geography, ScreenId.ZooFarm, "world/zoofarm_bg",
                (level, rng, prev) => GeographyRoundGenerator.Create(level, rng, prev),
                p => p.GeographyLevel, (p, v) => p.GeographyLevel = v, p => p.GeographyBuffer,
                GeographyRoundGenerator.RoundsPerSession, "geography_hint", "geography_demo"));
            Navigator.Register(ScreenId.ScienceLab, new BuildingScreen(BuildingId.ScienceLab));
            Navigator.Register(ScreenId.SinkOrFloat, new MatchScreen(ScreenId.SinkOrFloat, ScreenId.ScienceLab, "world/sciencelab_bg",
                (level, rng, prev) => ScienceLabRoundGenerator.Create(ScienceLabGameKind.SinkOrFloat, level, rng, prev),
                p => p.SinkOrFloatLevel, (p, v) => p.SinkOrFloatLevel = v, p => p.SinkOrFloatBuffer,
                ScienceLabRoundGenerator.RoundsPerSession, "sinkorfloat_hint", "sinkorfloat_demo"));
            Navigator.Register(ScreenId.Magnet, new MatchScreen(ScreenId.Magnet, ScreenId.ScienceLab, "world/sciencelab_bg",
                (level, rng, prev) => ScienceLabRoundGenerator.Create(ScienceLabGameKind.Magnet, level, rng, prev),
                p => p.MagnetLevel, (p, v) => p.MagnetLevel = v, p => p.MagnetBuffer,
                ScienceLabRoundGenerator.RoundsPerSession, "magnet_hint", "magnet_demo"));
            Navigator.Register(ScreenId.LivingVsNonLiving, new MatchScreen(ScreenId.LivingVsNonLiving, ScreenId.ScienceLab, "world/sciencelab_bg",
                (level, rng, prev) => ScienceLabRoundGenerator.Create(ScienceLabGameKind.LivingVsNonLiving, level, rng, prev),
                p => p.LivingVsNonLivingLevel, (p, v) => p.LivingVsNonLivingLevel = v, p => p.LivingVsNonLivingBuffer,
                ScienceLabRoundGenerator.RoundsPerSession, "livingvsnonliving_hint", "livingvsnonliving_demo"));
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
            Navigator.Register(ScreenId.Seasons, new MatchScreen(ScreenId.Seasons, ScreenId.ScienceLab, "world/sciencelab_bg",
                (level, rng, prev) => ScienceLabRoundGenerator.Create(ScienceLabGameKind.Seasons, level, rng, prev),
                p => p.SeasonsLevel, (p, v) => p.SeasonsLevel = v, p => p.SeasonsBuffer,
                ScienceLabRoundGenerator.RoundsPerSession, "seasons_hint", "seasons_demo"));
            Navigator.Register(ScreenId.DayNight, new MatchScreen(ScreenId.DayNight, ScreenId.ScienceLab, "world/sciencelab_bg",
                (level, rng, prev) => ScienceLabRoundGenerator.Create(ScienceLabGameKind.DayNight, level, rng, prev),
                p => p.DayNightLevel, (p, v) => p.DayNightLevel = v, p => p.DayNightBuffer,
                ScienceLabRoundGenerator.RoundsPerSession, "daynight_hint", "daynight_demo"));
            Navigator.Register(ScreenId.Space, new MatchScreen(ScreenId.Space, ScreenId.ScienceLab, "world/sciencelab_bg",
                (level, rng, prev) => ScienceLabRoundGenerator.Create(ScienceLabGameKind.Space, level, rng, prev),
                p => p.SpaceLevel, (p, v) => p.SpaceLevel = v, p => p.SpaceBuffer,
                ScienceLabRoundGenerator.RoundsPerSession, "space_hint", "space_demo"));

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

        // Whether music should play. Eva has no music player yet; one added later reads this flag.
        public bool MusicEnabled { get; private set; } = true;

        // Pushes the saved Music, Sound effects and Voice switches to the things they control.
        public void ApplyAudioSettings()
        {
            MusicEnabled = Progress.MusicEnabled;
            Sfx.Enabled = Progress.SfxEnabled;
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
