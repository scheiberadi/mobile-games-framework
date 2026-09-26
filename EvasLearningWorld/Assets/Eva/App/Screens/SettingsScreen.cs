using EvasLearningWorld.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Adult-facing settings, kept to what exists: voice volume in three steps and start over (with a second
    // confirmation). The app has no music, so there is no music switch.
    public sealed class SettingsScreen : ScreenBase
    {
        private static readonly Vector2 ButtonSize = new Vector2(300f, 240f);
        private static readonly string[] VolumeNames = { "VolumeLow", "VolumeMedium", "VolumeHigh" };
        private static readonly string[] VolumeLabels = { "Low", "Medium", "High" };

        private EvaGame _game;
        private readonly Button[] _volumeButtons = new Button[3];
        private GameObject _confirmPanel;

        public override void Build(EvaGame game)
        {
            _game = game;
            AddBackground(new Color(0.96f, 0.93f, 0.85f));

            Text(Root, "VolumeTitle", "Voice volume", 70, new Vector2(0f, 300f), new Vector2(1000f, 120f));
            for (var i = 0; i < 3; i++)
            {
                var step = i;
                _volumeButtons[i] = PlateButton(Root, VolumeNames[i], VolumeLabels[i], new Vector2((i - 1) * 380f, 130f), () => SetVolume(step));
            }

            PlateButton(Root, "StartOver", "Start over", new Vector2(0f, -170f), () => _confirmPanel.SetActive(true));

            _confirmPanel = new GameObject("ConfirmPanel", typeof(RectTransform), typeof(Image));
            _confirmPanel.transform.SetParent(Root, false);
            var panelRect = (RectTransform)_confirmPanel.transform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1000f, 640f);
            _confirmPanel.GetComponent<Image>().color = new Color(1f, 0.98f, 0.92f);
            Text(panelRect, "Question", "Erase everything and start again?", 64, new Vector2(0f, 140f), new Vector2(900f, 200f));
            PlateButton(panelRect, "Yes", "Yes", new Vector2(-200f, -130f), () => _game.StartOver());
            PlateButton(panelRect, "No", "No", new Vector2(200f, -130f), () => _confirmPanel.SetActive(false));
            _confirmPanel.SetActive(false);
        }

        public override void OnShow()
        {
            _confirmPanel.SetActive(false);
            ShowVolume();
        }

        private void SetVolume(int step)
        {
            _game.Progress.VoiceVolumeStep = VoiceSettings.Clamp(step);
            _game.Voice.Volume = VoiceSettings.Volume(_game.Progress.VoiceVolumeStep);
            _game.Commit();
            ShowVolume();
        }

        private void ShowVolume()
        {
            for (var i = 0; i < 3; i++)
                ((Image)_volumeButtons[i].targetGraphic).color = i == _game.Progress.VoiceVolumeStep ? Color.white : new Color(1f, 1f, 1f, 0.5f);
        }

        private static Button PlateButton(Transform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            var button = EvaUi.IconButton(parent, name, EvaUi.Sprite("icons/tile"), new Vector2(0.5f, 0.5f), position, ButtonSize, onClick);
            Text(button.transform, "Label", label, 56, Vector2.zero, ButtonSize);
            return button;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string value, int fontSize, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.2f, 0.15f, 0.1f);
            text.raycastTarget = false;
            return text;
        }
    }
}
