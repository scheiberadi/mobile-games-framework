using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
        private Button _adsTestToggleButton;
        private GameObject _resetConfirmPopup;

        private void Start()
        {
            _adsTestSettings = new AdsTestSettings(new PlayerPrefsStore());
            BuildUi();
        }

        private void ToggleAdsForTesting()
        {
            _adsTestSettings.SetAdsDisabledForTesting(!_adsTestSettings.AdsDisabledForTesting);
            _adsTestToggleButton.GetComponentInChildren<Text>().text = AdsToggleLabel();
        }

        private string AdsToggleLabel() => _adsTestSettings.AdsDisabledForTesting ? "Ads (testing): Off" : "Ads (testing): On";

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

            UiFactory.CreateBackButton(canvas.transform, () =>
            {
                SceneManager.LoadScene("SudokuMenu");
            });

            var title = UiFactory.CreateText(canvas.transform, "Title", 40, TextAnchor.MiddleCenter);
            title.text = "Settings";
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 190), new Vector2(400, 60));

            UiFactory.CreateButton(canvas.transform, "Reset Data", new Vector2(0, 120), new Vector2(260, 50), true, () =>
            {
                _resetConfirmPopup.SetActive(true);
            });

            if (Application.isEditor || Debug.isDebugBuild)
            {
                _adsTestToggleButton = UiFactory.CreateButton(canvas.transform, AdsToggleLabel(), new Vector2(0, 55), new Vector2(260, 50), true, ToggleAdsForTesting);
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
            label.text = "Reset your saved game and all\nhigh scores? This can't be undone.";
            UiFactory.SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(320, 90));

            UiFactory.CreateButton(panel.transform, "Reset", new Vector2(0, -30), new Vector2(220, 50), true, () =>
            {
                ResetAllData();
                _resetConfirmPopup.SetActive(false);
            });

            UiFactory.CreateButton(panel.transform, "Cancel", new Vector2(0, -95), new Vector2(220, 44), true, () =>
            {
                _resetConfirmPopup.SetActive(false);
            });

            _resetConfirmPopup.SetActive(false);
        }
    }
}
