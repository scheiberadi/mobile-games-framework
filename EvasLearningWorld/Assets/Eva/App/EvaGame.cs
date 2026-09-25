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
        public Hud Hud { get; private set; }
        public RectTransform ScreenRoot { get; private set; }
        public PlayerProgress Progress { get; private set; }
        public TutorialGuide TutorialGuide { get; private set; }

        private SaveStore _save;

        // Pass an existing canvas to build under it (tests); by default this creates the app canvas and its EventSystem.
        // Pass a store to keep the save away from PlayerPrefs (tests); by default the save lives in PlayerPrefs.
        public void Build(Canvas canvas = null, IKeyValueStore store = null)
        {
            _save = new SaveStore(store ?? new PlayerPrefsStore());
            Progress = _save.Load();

            if (canvas == null) canvas = UiFactory.CreateCanvas(new Vector2(EvaLayout.DesignWidth, EvaLayout.DesignHeight), 1f);
            UiFactory.CreateBackground(canvas.transform, new Color(0.75f, 0.91f, 1f), new Color(0.91f, 0.97f, 0.88f));

            ScreenRoot = CreateSafeAreaPanel(canvas.transform, "ScreenRoot");

            Sfx = new GameObject("Sfx", typeof(Sfx)).GetComponent<Sfx>();
            Sfx.transform.SetParent(transform, false);
            EvaUi.Sfx = Sfx;

            Voice = new GameObject("Voice", typeof(Voice)).GetComponent<Voice>();
            Voice.transform.SetParent(transform, false);

            Navigator = new Navigator(this);
            Navigator.Register(ScreenId.Creator, new CreatorScreen());
            Navigator.Register(ScreenId.Map, new MapScreen());
            Navigator.Register(ScreenId.House, new HouseScreen());
            Navigator.Register(ScreenId.School, new BuildingScreen(BuildingId.School));
            Navigator.Register(ScreenId.Count, new CountScreen());
            Navigator.Register(ScreenId.Store, new StoreScreen());

            var hudRoot = CreateSafeAreaPanel(canvas.transform, "HudRoot");
            Hud = hudRoot.gameObject.AddComponent<Hud>();
            Hud.Build(this, hudRoot);
            Hud.SetCoins(Progress.Coins);

            // Built last (and so drawn on top of every screen and the Hud, by plain sibling order under the
            // canvas - no screen ever needs to reach past its own Root to see it) and under its own safe-area
            // panel, matching every screen's Root exactly (same anchors, same offsets), so a canvas-unit
            // position from any screen (e.g. MapScreen.HouseButtonPosition) lines up here without translation.
            var guideRoot = CreateSafeAreaPanel(canvas.transform, "GuideRoot");
            TutorialGuide = new TutorialGuide(this, guideRoot);

            // First run (no saved character yet) opens the Creator instead of the Map; every later launch
            // goes straight to the Map since CreatorScreen.Confirm sets HasCharacter before it Commits.
            Navigator.Show(Progress.HasCharacter ? ScreenId.Map : ScreenId.Creator);
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

        private static RectTransform CreateSafeAreaPanel(Transform parent, string name)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(SafeAreaPanel));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<SafeAreaPanel>().Apply(true);
            return (RectTransform)panel.transform;
        }
    }
}
