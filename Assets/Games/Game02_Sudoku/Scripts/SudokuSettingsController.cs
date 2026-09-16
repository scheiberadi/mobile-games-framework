using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MobileGamesFramework.Localization;
using MobileGamesFramework.Monetization;
using MobileGamesFramework.Persistence;
using MobileGamesFramework.UI;

namespace Game02_Sudoku
{
    public class SudokuSettingsController : MonoBehaviour
    {
        private static readonly Difficulty[] Difficulties =
        {
            Difficulty.Easy, Difficulty.Medium, Difficulty.Hard, Difficulty.Expert
        };

        private AdsTestSettings _adsTestSettings;
        private SudokuAudioSettings _audioSettings;
        private Button _adsTestToggleButton;
        private Button _musicToggleButton;
        private Button _sfxToggleButton;
        private GameObject _resetConfirmPopup;

        private void Start()
        {
            _adsTestSettings = new AdsTestSettings(new PlayerPrefsStore());
            _audioSettings = new SudokuAudioSettings(new PlayerPrefsStore());
            BuildUi();
        }

        private void ToggleAdsForTesting()
        {
            _adsTestSettings.SetAdsDisabledForTesting(!_adsTestSettings.AdsDisabledForTesting);
            _adsTestToggleButton.GetComponentInChildren<Text>().text = AdsToggleLabel();
        }

        private string AdsToggleLabel() => _adsTestSettings.AdsDisabledForTesting ? Loc.Get("settings.adsTestOff") : Loc.Get("settings.adsTestOn");

        private void ToggleMusic()
        {
            var enabled = !_audioSettings.MusicEnabled;
            _audioSettings.SetMusicEnabled(enabled);
            SudokuMusicPlayer.Instance?.SetMusicEnabled(enabled);
            _musicToggleButton.GetComponentInChildren<Text>().text = MusicToggleLabel();
        }

        private void ToggleSfx()
        {
            _audioSettings.SetSfxEnabled(!_audioSettings.SfxEnabled);
            _sfxToggleButton.GetComponentInChildren<Text>().text = SfxToggleLabel();
        }

        // Cycles through the 11 languages in enum declaration order and reloads this
        // scene so every string on screen - here and on every other screen - rebuilds
        // in the new language, the same way the Back button already reloads scenes.
        private void CycleLanguage()
        {
            var languages = (Language[])System.Enum.GetValues(typeof(Language));
            var currentIndex = System.Array.IndexOf(languages, Loc.CurrentLanguage);
            var next = languages[(currentIndex + 1) % languages.Length];
            Loc.SetLanguage(next);
            SceneManager.LoadScene("SudokuSettings");
        }

        private string LanguageButtonLabel() => $"{Loc.Get("settings.language")}: {LanguageInfo.NativeName(Loc.CurrentLanguage)}";

        private string MusicToggleLabel() => _audioSettings.MusicEnabled ? Loc.Get("settings.musicOn") : Loc.Get("settings.musicOff");
        private string SfxToggleLabel() => _audioSettings.SfxEnabled ? Loc.Get("settings.sfxOn") : Loc.Get("settings.sfxOff");

        private static void ResetAllData()
        {
            var store = new PlayerPrefsStore();
            new SudokuSaveService(store).ClearSave();
            var leaderboardStore = new SudokuLeaderboardStore(store);
            foreach (var difficulty in Difficulties) leaderboardStore.ClearTimes(difficulty);
        }

        private void BuildUi()
        {
            var canvas = UiFactory.CreateCanvas();
            UiFactory.CreateBackground(canvas.transform, new Color(0.75f, 0.85f, 0.97f), new Color(0.98f, 0.98f, 1f));

            SudokuUi.CreateBackButton(canvas.transform, () =>
            {
                SceneManager.LoadScene("SudokuMenu");
            }, Loc.Get("common.back"));

            var title = UiFactory.CreateText(canvas.transform, "Title", 40, TextAnchor.MiddleCenter);
            title.text = Loc.Get("settings.title");
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 190), new Vector2(400, 60));

            SudokuUi.CreateButton(canvas.transform, Loc.Get("settings.resetData"), new Vector2(0, 120), new Vector2(260, 50), true, () =>
            {
                _resetConfirmPopup.SetActive(true);
            });

            _musicToggleButton = SudokuUi.CreateButton(canvas.transform, MusicToggleLabel(), new Vector2(0, 55), new Vector2(260, 50), true, ToggleMusic);
            _sfxToggleButton = SudokuUi.CreateButton(canvas.transform, SfxToggleLabel(), new Vector2(0, -10), new Vector2(260, 50), true, ToggleSfx);
            SudokuUi.CreateButton(canvas.transform, LanguageButtonLabel(), new Vector2(0, -75), new Vector2(260, 50), true, CycleLanguage);

            if (Application.isEditor || Debug.isDebugBuild)
            {
                _adsTestToggleButton = SudokuUi.CreateButton(canvas.transform, AdsToggleLabel(), new Vector2(0, -140), new Vector2(260, 50), true, ToggleAdsForTesting);
            }

            BuildResetConfirmPopup(canvas.transform);
        }

        private void BuildResetConfirmPopup(Transform parent)
        {
            _resetConfirmPopup = new GameObject("ResetConfirmPopup", typeof(Image));
            _resetConfirmPopup.transform.SetParent(parent, false);
            UiFactory.SetRect(_resetConfirmPopup.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _resetConfirmPopup.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            var panel = new GameObject("Panel", typeof(Image));
            panel.transform.SetParent(_resetConfirmPopup.transform, false);
            UiFactory.SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 260));
            var panelImage = panel.GetComponent<Image>();
            panelImage.sprite = RoundedRectSprite.Get();
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.96f, 0.94f, 0.90f);

            var label = UiFactory.CreateText(panel.transform, "Label", 20, TextAnchor.MiddleCenter);
            label.text = Loc.Get("settings.resetConfirmBody");
            UiFactory.SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(320, 90));

            SudokuUi.CreateButton(panel.transform, Loc.Get("settings.reset"), new Vector2(0, -30), new Vector2(220, 50), true, () =>
            {
                ResetAllData();
                _resetConfirmPopup.SetActive(false);
            });

            SudokuUi.CreateButton(panel.transform, Loc.Get("common.cancel"), new Vector2(0, -95), new Vector2(220, 44), true, () =>
            {
                _resetConfirmPopup.SetActive(false);
            });

            _resetConfirmPopup.SetActive(false);
        }
    }
}
