using System.Collections;
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
        // Kept in sync with SudokuController.AdsEnabled - hides/disables the ad and IAP
        // surface without deleting it, per standing instruction to keep it re-enableable.
        private const bool AdsEnabled = false;
        private const string RemoveAdsProductId = "remove_ads";

        private static readonly Difficulty[] Difficulties =
        {
            Difficulty.Easy, Difficulty.Medium, Difficulty.Hard, Difficulty.Expert
        };

        private AdsTestSettings _adsTestSettings;
        private IIapProvider _iapProvider;
        private Button _adsTestToggleButton;
        private Button _removeAdsButton;
        private GameObject _resetConfirmPopup;

        private void Start()
        {
            _adsTestSettings = new AdsTestSettings(new PlayerPrefsStore());
            BuildUi();
            if (AdsEnabled)
            {
                // Deferred a frame so the built UI is already on screen before the IAP
                // SDK's native init runs, which can briefly stall the render thread.
                StartCoroutine(InitializeIap());
            }
        }

        private IEnumerator InitializeIap()
        {
            yield return null;
            _iapProvider = new UnityIapProvider(new[] { RemoveAdsProductId });

            var alreadyRemovedAds = _iapProvider.IsPurchased(RemoveAdsProductId);
            if (alreadyRemovedAds)
            {
                UiFactory.SetInteractable(_removeAdsButton, false);
                _removeAdsButton.GetComponentInChildren<Text>().text = "Ads Removed";
            }
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

            _removeAdsButton = UiFactory.CreateButton(canvas.transform, "Remove Ads",
                new Vector2(0, 120), new Vector2(260, 50), true, () =>
                {
                    if (_iapProvider == null) return;
                    _iapProvider.Purchase(RemoveAdsProductId, success =>
                    {
                        if (!success) return;
                        UiFactory.SetInteractable(_removeAdsButton, false);
                        _removeAdsButton.GetComponentInChildren<Text>().text = "Ads Removed";
                    });
                });
            _removeAdsButton.gameObject.SetActive(AdsEnabled);

            UiFactory.CreateButton(canvas.transform, "Reset Data", new Vector2(0, 55), new Vector2(260, 50), true, () =>
            {
                _resetConfirmPopup.SetActive(true);
            });

            if (Application.isEditor || Debug.isDebugBuild)
            {
                _adsTestToggleButton = UiFactory.CreateButton(canvas.transform, AdsToggleLabel(), new Vector2(0, -10), new Vector2(260, 50), true, ToggleAdsForTesting);
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
