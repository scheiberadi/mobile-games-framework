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
        private AdsTestSettings _adsTestSettings;
        private Button _adsTestToggleButton;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != "SudokuSettings") return;
            new GameObject("SudokuSettingsController").AddComponent<SudokuSettingsController>();
        }

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

        private void BuildUi()
        {
            var canvas = UiFactory.CreateCanvas();

            var title = UiFactory.CreateText(canvas.transform, "Title", 40, TextAnchor.MiddleCenter);
            title.text = "Settings";
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(400, 60));

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
