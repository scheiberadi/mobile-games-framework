using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Free Drawing (Art Studio, M4.7, docs/kids-games/full-catalogue-plan.md "9. Art Studio") - the one game in
    // the whole catalogue with no goal, round or help ladder (per the plan's own design note): an open canvas
    // where the child picks a color and a stamp, then taps the canvas to place it, as many times as they like.
    // Reward is a flat per-session coin on exit instead of the usual per-round payout, and Eva gives a gentle
    // idle nudge after a period of inactivity instead of ever running a Hint/Demonstrate ladder.
    public sealed class FreeDrawingScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        // Reports a tap's local point back to the screen - the canvas's only interactive behaviour.
        private sealed class CanvasTap : MonoBehaviour, IPointerClickHandler
        {
            public System.Action<Vector2> Tapped;
            private RectTransform _rect;
            private void Awake() => _rect = (RectTransform)transform;

            public void OnPointerClick(PointerEventData eventData)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, eventData.position, eventData.pressEventCamera, out var local);
                Tapped?.Invoke(local);
            }
        }


        private const float PaletteButtonSize = EvaUi.MinTap;
        private static readonly float[] PaletteX = { -600f, -360f, -120f, 120f, 360f, 600f };
        private const float ColorRowY = 310f;
        private const float StampRowY = -310f;

        private const float StampSize = 160f;
        private const int MaxStamps = 40;

        private const float EndButtonSize = EvaUi.MinTap;
        // Side by side on the left: the right column is where the companion pair stands and the colour and stamp rows
        // (240 tall, kept inside the 900 frame) leave no room to stack them. They sit between the two rows.
        private static readonly Vector2 ClearButtonPosition = new Vector2(-600f, 0f);
        private static readonly Vector2 HomeButtonPosition = new Vector2(-300f, 0f);

        // Flat reward on leaving a session - Free Drawing has no per-round payout to accumulate instead.
        private const int SessionCoinPayout = CoinPayout.Clean;

        private const float IdleSeconds = 14f;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _canvasField, _stampField;
        private Button[] _colorButtons, _stampButtons;
        private readonly List<Image> _placedStamps = new List<Image>();

        private int _selectedColor;
        private int _selectedStamp;
        private Coroutine _idleRoutine;
        private int _idleLineIndex;
        private bool _coinPaid;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddPictureBackground();
            BuildEva();
            BuildCanvas();
            BuildStampField();
            BuildPalette();
            BuildStampPicker();
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.FreeDrawing);
            _selectedColor = 0;
            _selectedStamp = 0;
            _coinPaid = false;
            ClearCanvas();
            HighlightSelection();
            RestartIdleTimer();
        }

        public override void OnHide()
        {
            StopIdleTimer();
            PayFlatCoinIfNeeded();
        }

        // --- Canvas -----------------------------------------------------------------------------------------

        private void BuildCanvas()
        {
            _canvasField = CreateFullRectContainer("CanvasField");
            var tapObject = new GameObject("CanvasTap", typeof(RectTransform), typeof(Image), typeof(CanvasTap));
            tapObject.transform.SetParent(_canvasField, false);
            var rect = (RectTransform)tapObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = tapObject.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;

            var tap = tapObject.GetComponent<CanvasTap>();
            tap.Tapped = OnCanvasTapped;
        }

        private void BuildStampField() => _stampField = CreateFullRectContainer("StampField");

        private void OnCanvasTapped(Vector2 localPoint)
        {
            RestartIdleTimer();
            if (_placedStamps.Count >= MaxStamps) return;

            var go = new GameObject("Stamp" + _placedStamps.Count, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_stampField, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = localPoint;
            rect.sizeDelta = new Vector2(StampSize, StampSize);

            var image = go.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("artstudio/stamp_" + FreeDrawingCatalog.Stamps[_selectedStamp]);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = ColorFor(FreeDrawingCatalog.Colors[_selectedColor]);
            _placedStamps.Add(image);

            _game.Sfx.Tap();
            _runner.StartCoroutine(PopIn(rect));
        }

        private void ClearCanvas()
        {
            for (var i = _stampField.childCount - 1; i >= 0; i--)
            {
                var child = _stampField.GetChild(i).gameObject;
                if (Application.isPlaying) Object.Destroy(child);
                else Object.DestroyImmediate(child);
            }
            _placedStamps.Clear();
        }

        // Placeholder tint until real per-color stamp art exists - same "generated stand-in" reasoning as
        // EvaUi.Sprite's own placeholder path, just applied as a color multiply instead of a whole sprite.
        private static Color ColorFor(string name)
        {
            switch (name)
            {
                case "red": return new Color(0.90f, 0.25f, 0.25f);
                case "orange": return new Color(0.95f, 0.55f, 0.15f);
                case "yellow": return new Color(0.95f, 0.85f, 0.20f);
                case "green": return new Color(0.30f, 0.75f, 0.35f);
                case "blue": return new Color(0.25f, 0.55f, 0.95f);
                case "purple": return new Color(0.65f, 0.35f, 0.85f);
                default: return Color.white;
            }
        }

        // --- Palette / stamp picker ---------------------------------------------------------------------------

        private void BuildPalette()
        {
            _colorButtons = new Button[FreeDrawingCatalog.Colors.Length];
            var panel = CreateFullRectContainer("Palette");
            for (var i = 0; i < FreeDrawingCatalog.Colors.Length; i++)
            {
                var index = i;
                var button = EvaUi.IconButton(panel, "Color_" + FreeDrawingCatalog.Colors[i], EvaUi.Sprite("artstudio/swatch_" + FreeDrawingCatalog.Colors[i]),
                    new Vector2(0.5f, 0.5f), new Vector2(PaletteX[i], ColorRowY), PaletteButtonSize, () => SelectColor(index));
                _colorButtons[i] = button;
            }
        }

        private void BuildStampPicker()
        {
            _stampButtons = new Button[FreeDrawingCatalog.Stamps.Length];
            var panel = CreateFullRectContainer("StampPicker");
            for (var i = 0; i < FreeDrawingCatalog.Stamps.Length; i++)
            {
                var index = i;
                var button = EvaUi.IconButton(panel, "Stamp_" + FreeDrawingCatalog.Stamps[i], EvaUi.Sprite("artstudio/stamp_" + FreeDrawingCatalog.Stamps[i]),
                    new Vector2(0.5f, 0.5f), new Vector2(PaletteX[i], StampRowY), PaletteButtonSize, () => SelectStamp(index));
                _stampButtons[i] = button;
            }
        }

        private void SelectColor(int index)
        {
            _selectedColor = index;
            RestartIdleTimer();
            HighlightSelection();
        }

        private void SelectStamp(int index)
        {
            _selectedStamp = index;
            RestartIdleTimer();
            HighlightSelection();
        }

        private void HighlightSelection()
        {
            for (var i = 0; i < _colorButtons.Length; i++)
                _colorButtons[i].transform.localScale = Vector3.one * (i == _selectedColor ? 1.2f : 1f);
            for (var i = 0; i < _stampButtons.Length; i++)
                _stampButtons[i].transform.localScale = Vector3.one * (i == _selectedStamp ? 1.2f : 1f);
        }

        // --- Idle prompt --------------------------------------------------------------------------------------

        private void RestartIdleTimer()
        {
            StopIdleTimer();
            _idleRoutine = _runner.StartCoroutine(IdleTimer());
        }

        private void StopIdleTimer()
        {
            if (_idleRoutine != null) _runner.StopCoroutine(_idleRoutine);
            _idleRoutine = null;
        }

        private IEnumerator IdleTimer()
        {
            while (true)
            {
                yield return new WaitForSeconds(IdleSeconds);
                _idleLineIndex = _idleLineIndex % 3 + 1;
                _eva.SetTalking(true);
                yield return _game.Voice.SayAndWait("freedrawing_idle_" + _idleLineIndex);
                _eva.SetTalking(false);
            }
        }

        // --- Exit / coin --------------------------------------------------------------------------------------

        private void PayFlatCoinIfNeeded()
        {
            if (_coinPaid) return;
            _coinPaid = true;
            var before = _game.Progress.Coins;
            _game.Progress.AddCoins(SessionCoinPayout);
            _game.Commit();
            _game.Hud.AnimateCoins(before, before + SessionCoinPayout, _game.Sfx, Vector2.zero);
        }

        private void BuildEndButtons()
        {
            var panel = CreateFullRectContainer("ExitPanel");
            EvaUi.IconButton(panel, "ClearButton", EvaUi.Sprite("icons/replay"), new Vector2(0.5f, 0.5f),
                ClearButtonPosition, EndButtonSize, ClearCanvas);
            EvaUi.IconButton(panel, "HomeButton", EvaUi.Sprite("icons/home"), new Vector2(0.5f, 0.5f),
                HomeButtonPosition, EndButtonSize, GoHome);
        }

        private void GoHome()
        {
            PayFlatCoinIfNeeded();
            _game.Navigator.Show(ScreenId.ArtStudio);
        }

        // --- Eva ------------------------------------------------------------------------------------------

        private void BuildEva()
        {
            _eva = AddCompanionPair(_game, CompanionLayout.Side);
        }

        // --- Small tweens --------------------------------------------------------------------------------------

        private static IEnumerator PopIn(RectTransform target)
        {
            const float duration = 0.2f;
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                target.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, t / duration);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        // --- Helpers ----------------------------------------------------------------------------------------

        private RectTransform CreateFullRectContainer(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(Root, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private void AddPictureBackground()
        {
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);
            var image = background.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("world/artstudio_canvas_bg");
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
    }
}
