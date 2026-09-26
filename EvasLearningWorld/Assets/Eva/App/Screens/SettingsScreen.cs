using System;
using EvasLearningWorld.Rules;
using MobileGamesFramework.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Adult-facing settings in the style of the Sudoku settings: rows on rounded cards, label on the left and an on/off switch on
    // the right (Music, Sound effects, Voice), and a separate warm-red row that erases the saved game (after a confirmation).
    // Every text goes through Loc. Each row is one tap target of at least MinTap in both directions.
    public sealed class SettingsScreen : ScreenBase
    {
        private static readonly Vector2 RowSize = new Vector2(640f, EvaUi.MinTap);
        private const float LeftX = -355f, RightX = 355f, UpperY = 10f, LowerY = -260f;
        private static readonly Color Ink = new Color(0.2f, 0.15f, 0.1f);
        private static readonly Color SwitchOn = new Color(0.33f, 0.70f, 0.40f);
        private static readonly Color SwitchOff = new Color(0.70f, 0.67f, 0.62f);
        private static readonly Color ResetRed = new Color(0.80f, 0.27f, 0.22f);

        private EvaGame _game;
        private GameObject _confirmPanel;
        private SwitchView _music, _sfx, _voice;

        private sealed class SwitchView
        {
            public Image Track;
            public RectTransform Knob;
            public Func<bool> Get;

            public void Show()
            {
                var on = Get();
                Track.color = on ? SwitchOn : SwitchOff;
                Knob.anchoredPosition = new Vector2(on ? 36f : -36f, 0f);
            }
        }

        public override void Build(EvaGame game)
        {
            _game = game;
            AddBackground(new Color(0.96f, 0.93f, 0.85f));

            Label(Root, "Title", Loc.Get("settings.title"), 90, Ink, new Vector2(0f, 290f), new Vector2(900f, 130f), TextAlignmentOptions.Center);

            Card(Root, "MusicSfxCard", new Vector2(LeftX, -125f), new Vector2(680f, 550f));
            Card(Root, "VoiceCard", new Vector2(RightX, 10f), new Vector2(680f, 280f));

            _music = ToggleRow("Music", Loc.Get("settings.music"), new Vector2(LeftX, UpperY), () => _game.Progress.MusicEnabled, on => _game.Progress.MusicEnabled = on);
            _sfx = ToggleRow("Sfx", Loc.Get("settings.sfx"), new Vector2(LeftX, LowerY), () => _game.Progress.SfxEnabled, on => _game.Progress.SfxEnabled = on);
            _voice = ToggleRow("Voice", Loc.Get("settings.voice"), new Vector2(RightX, UpperY), () => _game.Progress.VoiceEnabled, on => _game.Progress.VoiceEnabled = on);

            var reset = Row("ResetProgress", ResetRed, new Vector2(RightX, LowerY), () => _confirmPanel.SetActive(true));
            Label(reset.transform, "Label", Loc.Get("settings.reset"), 50, Color.white, Vector2.zero, RowSize - new Vector2(60f, 20f), TextAlignmentOptions.Center);

            BuildConfirmPanel();
        }

        public override void OnShow()
        {
            _confirmPanel.SetActive(false);
            _music.Show();
            _sfx.Show();
            _voice.Show();
        }

        private void BuildConfirmPanel()
        {
            _confirmPanel = new GameObject("ConfirmPanel", typeof(RectTransform), typeof(Image));
            _confirmPanel.transform.SetParent(Root, false);
            var panelRect = (RectTransform)_confirmPanel.transform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = new Vector2(-1500f, -1500f);
            panelRect.offsetMax = new Vector2(1500f, 1500f);
            _confirmPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f); // also blocks taps on everything behind it

            Card(panelRect, "Card", Vector2.zero, new Vector2(1000f, 640f)).color = new Color(1f, 0.98f, 0.92f);
            Label(panelRect, "Question", Loc.Get("settings.resetConfirm"), 60, Ink, new Vector2(0f, 140f), new Vector2(900f, 240f), TextAlignmentOptions.Center);
            PlateButton(panelRect, "Yes", Loc.Get("common.yes"), new Vector2(-200f, -130f), () => _game.StartOver());
            PlateButton(panelRect, "No", Loc.Get("common.no"), new Vector2(200f, -130f), () => _confirmPanel.SetActive(false));
            _confirmPanel.SetActive(false);
        }

        // One tappable rounded row with a label on the left and a switch on the right.
        private SwitchView ToggleRow(string name, string label, Vector2 position, Func<bool> get, Action<bool> set)
        {
            var view = new SwitchView { Get = get };
            var row = Row(name, new Color(0.95f, 0.91f, 0.82f), position, () =>
            {
                set(!get());
                _game.ApplyAudioSettings();
                _game.Commit();
                view.Show();
            });
            var labelText = Label(row.transform, "Label", label, 54, Ink, new Vector2(40f, 0f), new Vector2(RowSize.x - 260f, RowSize.y - 20f), TextAlignmentOptions.MidlineLeft);
            SetAnchor((RectTransform)labelText.transform, new Vector2(0f, 0.5f), new Vector2(40f, 0f));

            var track = Rounded(row.transform, "Track", Color.white, new Vector2(1f, 0.5f), new Vector2(-40f, 0f), new Vector2(160f, 88f));
            var knob = Rounded(track.transform, "Knob", Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(68f, 68f));
            view.Track = track;
            view.Knob = (RectTransform)knob.transform;
            view.Show();
            return view;
        }

        private Button Row(string name, Color color, Vector2 position, UnityAction onClick)
        {
            var button = EvaUi.IconButton(Root, name, RoundedRectSprite.Get(), new Vector2(0.5f, 0.5f), position, RowSize, onClick);
            var image = (Image)button.targetGraphic;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 0.25f;
            image.preserveAspect = false;
            image.color = color;
            return button;
        }

        private static void SetAnchor(RectTransform rect, Vector2 anchor, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
        }

        private static Image Card(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var image = Rounded(parent, name, new Color(1f, 0.985f, 0.94f), new Vector2(0.5f, 0.5f), position, size);
            image.pixelsPerUnitMultiplier = 0.2f;
            return image;
        }

        private static Image Rounded(Transform parent, string name, Color color, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            SetAnchor(rect, anchor, position);
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = RoundedRectSprite.Get();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 0.2f;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void PlateButton(Transform parent, string name, string label, Vector2 position, UnityAction onClick)
        {
            var size = new Vector2(300f, EvaUi.MinTap);
            var button = EvaUi.IconButton(parent, name, EvaUi.Sprite("icons/tile"), new Vector2(0.5f, 0.5f), position, size, onClick);
            Label(button.transform, "Label", label, 56, Ink, Vector2.zero, size, TextAlignmentOptions.Center);
        }

        // Text that shrinks to fit its box, so a long translation never spills out of its row.
        private static TextMeshProUGUI Label(Transform parent, string name, string value, int fontSize, Color color, Vector2 position, Vector2 size, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            SetAnchor(rect, new Vector2(0.5f, 0.5f), position);
            rect.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMax = fontSize;
            text.fontSizeMin = fontSize * 0.5f;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }
    }
}
