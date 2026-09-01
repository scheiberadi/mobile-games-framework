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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != "SudokuSettings") return;
            new GameObject("SudokuSettingsController").AddComponent<SudokuSettingsController>();
        }

        private void Start()
        {
            _adsTestSettings = new AdsTestSettings(new PlayerPrefsStore());
            _iapProvider = new UnityIapProvider(new[] { RemoveAdsProductId });
            BuildUi();
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

            var title = UiFactory.CreateText(canvas.transform, "Title", 40, TextAnchor.MiddleCenter);
            title.text = "Settings";
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 190), new Vector2(400, 60));

            var alreadyRemovedAds = _iapProvider.IsPurchased(RemoveAdsProductId);
            _removeAdsButton = UiFactory.CreateButton(canvas.transform, alreadyRemovedAds ? "Ads Removed" : "Remove Ads",
                new Vector2(0, 120), new Vector2(260, 50), !alreadyRemovedAds, () =>
                {
                    _iapProvider.Purchase(RemoveAdsProductId, success =>
                    {
                        if (!success) return;
                        _removeAdsButton.interactable = false;
                        _removeAdsButton.GetComponentInChildren<Text>().text = "Ads Removed";
                    });
                });

            if (Application.isEditor || Debug.isDebugBuild)
            {
                _adsTestToggleButton = UiFactory.CreateButton(canvas.transform, AdsToggleLabel(), new Vector2(0, 60), new Vector2(260, 50), true, ToggleAdsForTesting);
            }

            UiFactory.CreateButton(canvas.transform, "Back", new Vector2(0, -60), new Vector2(220, 50), true, () =>
            {
                SceneManager.LoadScene("SudokuMenu");
            });
        }
    }
}
