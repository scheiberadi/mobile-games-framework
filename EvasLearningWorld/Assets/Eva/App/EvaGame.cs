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
