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
        private const string RemoveAdsProductId = "remove_ads";

        private AdsTestSettings _adsTestSettings;
        private IIapProvider _iapProvider;
        private Button _adsTestToggleButton;
        private Button _removeAdsButton;

        private void Start()
        {
            _adsTestSettings = new AdsTestSettings(new PlayerPrefsStore());
            BuildUi();
            // Deferred a frame so the built UI is already on screen before the IAP SDK's
            // native init runs, which can briefly stall the render thread on real devices.
            StartCoroutine(InitializeIap());
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

        private void BuildUi()
        {
            var canvas = UiFactory.CreateCanvas();
            UiFactory.CreateBackground(canvas.transform, new Color(0.75f, 0.85f, 0.97f), new Color(0.98f, 0.98f, 1f));

            UiFactory.CreateButton(canvas.transform, "Back", new Vector2(330, 400), new Vector2(110, 50), true, () =>
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

            if (Application.isEditor || Debug.isDebugBuild)
            {
                _adsTestToggleButton = UiFactory.CreateButton(canvas.transform, AdsToggleLabel(), new Vector2(0, 60), new Vector2(260, 50), true, ToggleAdsForTesting);
            }
        }
    }
}
