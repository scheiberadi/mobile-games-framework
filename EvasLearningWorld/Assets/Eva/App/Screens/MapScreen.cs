using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // The child's home base: three big buildings, each opening one place.
    public sealed class MapScreen : ScreenBase
    {
        private const float BuildingSize = 300f;

        // Fixed building centres (see AddBuilding below): shared with TutorialGuide, which points the hand at
        // one of these without needing to know MapScreen's own layout otherwise.
        public static readonly Vector2 HouseButtonPosition = new Vector2(-480f, 0f);
        public static readonly Vector2 SchoolButtonPosition = new Vector2(0f, 0f);
        public static readonly Vector2 StoreButtonPosition = new Vector2(480f, 0f);

        // Eva stands on the right, clear of the buildings; a tap plays Wave (guarded against mashing by
        // CharacterRig itself). The tap zone sits over her, not on her Root, because Root is scaled to her
        // on-screen height and the no-reading audit measures a TapTarget's own unscaled rect.
        private const float EvaHeight = 560f;
        private static readonly Vector2 EvaAnchor = new Vector2(1f, 0f);
        private static readonly Vector2 EvaOffset = new Vector2(-260f, 40f);

        // The child's own character (Task 6/9), standing beside Eva. 420 units per Task 6's report - the
        // Creator screen's own preview is bigger (520) since it is the sole focus there; here Eva still reads
        // as the taller, more central figure. Purely decorative: no TapTarget, nothing to tap or navigate.
        private const float PlayerHeight = 420f;
        private static readonly Vector2 PlayerOffset = new Vector2(-620f, 40f);

        private EvaGame _game;

        public override void Build(EvaGame game)
        {
            _game = game;
            AddMapBackground();
            AddBuilding(game, "HouseButton", "world/house_icon", HouseButtonPosition.x, ScreenId.House);
            AddBuilding(game, "SchoolButton", "world/school_icon", SchoolButtonPosition.x, ScreenId.School);
            AddBuilding(game, "StoreButton", "world/store_icon", StoreButtonPosition.x, ScreenId.Store);
            AddEva();
            AddPlayer(game);
        }

        // Task 12: the guide says its line (if any) and points at the right building; after Done, Eva greets
        // the child once per app run instead - a separate one-time-per-session flag, not part of the
        // TutorialStep state machine (see HouseScreen's _saidPlacedThisSession / StoreScreen's
        // _saidWelcomeThisSession for the same pattern: the screen instance lives for the whole session, so a
        // plain instance flag already means "first time this session").
        private bool _saidWelcomeThisSession;

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.Map);
            if (_game.Progress.Tutorial == TutorialStep.Done && !_saidWelcomeThisSession)
            {
                _saidWelcomeThisSession = true;
                _game.Voice.Say("map_welcome");
            }
        }

        private void AddPlayer(EvaGame game)
        {
            var rig = RigFactory.CreatePlayer(Root, game.Progress.Look, PlayerHeight);
            var root = rig.Root;
            root.anchorMin = root.anchorMax = EvaAnchor;
            root.anchoredPosition = PlayerOffset;
        }

        private void AddEva()
        {
            var rig = RigFactory.CreateEva(Root, EvaHeight);
            var root = rig.Root;
            root.anchorMin = root.anchorMax = EvaAnchor;
            root.anchoredPosition = EvaOffset;

            var tapZone = new GameObject("EvaTapZone", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TapTarget), typeof(PressFeedback));
            tapZone.transform.SetParent(Root, false);
            var tapRect = (RectTransform)tapZone.transform;
            tapRect.anchorMin = tapRect.anchorMax = EvaAnchor;
            tapRect.pivot = new Vector2(0.5f, 0f);
            tapRect.anchoredPosition = EvaOffset;
            tapRect.sizeDelta = new Vector2(320f, EvaHeight);

            var image = tapZone.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;

            var button = tapZone.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                if (EvaUi.Sfx != null) EvaUi.Sfx.Tap();
                rig.Wave();
            });
        }

        // The landscape image, filling exactly the safe area (the canvas-wide colour behind it, from
        // UiFactory.CreateBackground, already covers the strip outside the safe area, e.g. behind a notch).
        // Unlike ScreenBase.AddBackground's solid colour, a real image must not be stretched to a huge
        // rect (-1500..1500) or it zooms into a tiny centre crop, hiding most of the picture.
        private void AddMapBackground()
        {
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = background.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("world/map_bg");
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }

        private void AddBuilding(EvaGame game, string name, string spriteName, float x, ScreenId target)
        {
            EvaUi.IconButton(Root, name, EvaUi.Sprite(spriteName), new Vector2(0.5f, 0.5f), new Vector2(x, 0f), BuildingSize,
                () => game.Navigator.Show(target));
        }
    }
}
