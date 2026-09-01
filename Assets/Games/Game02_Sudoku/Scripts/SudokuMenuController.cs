using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MobileGamesFramework.Monetization;
using MobileGamesFramework.Persistence;
using MobileGamesFramework.UI;

namespace Game02_Sudoku
{
    public class SudokuMenuController : MonoBehaviour
    {
        private const string RemoveAdsProductId = "remove_ads";

        private static readonly Difficulty[] Difficulties =
        {
            Difficulty.Easy, Difficulty.Medium, Difficulty.Hard, Difficulty.Expert
        };

        private IIapProvider _iapProvider;
        private Button _removeAdsButton;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != "SudokuMenu") return;
            new GameObject("SudokuMenuController").AddComponent<SudokuMenuController>();
        }

        private void Start()
        {
            var store = new PlayerPrefsStore();
            var saveService = new SudokuSaveService(store);
            var statsStore = new SudokuStatsStore(store);
            _iapProvider = new UnityIapProvider(new[] { RemoveAdsProductId });

            BuildUi(saveService, saveService.HasSave(), statsStore);
        }

        private void BuildUi(SudokuSaveService saveService, bool hasSave, SudokuStatsStore statsStore)
        {
            var canvas = UiFactory.CreateCanvas();

            var title = UiFactory.CreateText(canvas.transform, "Title", 48, TextAnchor.MiddleCenter);
            title.text = "Sudoku";
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(400, 60));

            var statsText = UiFactory.CreateText(canvas.transform, "StatsText", 20, TextAnchor.MiddleCenter);
            statsText.text = BuildStatsText(statsStore);
            UiFactory.SetRect(statsText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(440, 100));

            var newGameLabel = UiFactory.CreateText(canvas.transform, "NewGameLabel", 22, TextAnchor.MiddleCenter);
            newGameLabel.text = "New Game";
            UiFactory.SetRect(newGameLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(300, 30));

            for (var i = 0; i < Difficulties.Length; i++)
            {
                var difficulty = Difficulties[i];
                var x = -165 + i * 110;
                UiFactory.CreateButton(canvas.transform, difficulty.ToString(), new Vector2(x, 20), new Vector2(100, 50), true, () =>
                {
                    saveService.ClearSave();
                    SudokuSessionIntent.Difficulty = difficulty;
                    SudokuSessionIntent.ResumeFromSave = false;
                    SudokuSessionIntent.EnterCustom = false;
                    SceneManager.LoadScene("Sudoku");
                });
            }

            UiFactory.CreateButton(canvas.transform, "Continue", new Vector2(0, -50), new Vector2(220, 50), hasSave, () =>
            {
                SudokuSessionIntent.ResumeFromSave = true;
                SudokuSessionIntent.EnterCustom = false;
                SceneManager.LoadScene("Sudoku");
            });

            UiFactory.CreateButton(canvas.transform, "Custom", new Vector2(0, -120), new Vector2(220, 50), true, () =>
            {
                SudokuSessionIntent.ResumeFromSave = false;
                SudokuSessionIntent.EnterCustom = true;
                SceneManager.LoadScene("Sudoku");
            });

            var alreadyRemovedAds = _iapProvider.IsPurchased(RemoveAdsProductId);
            _removeAdsButton = UiFactory.CreateButton(canvas.transform, alreadyRemovedAds ? "Ads Removed" : "Remove Ads",
                new Vector2(-115, -190), new Vector2(220, 50), !alreadyRemovedAds, () =>
                {
                    _iapProvider.Purchase(RemoveAdsProductId, success =>
                    {
                        if (!success) return;
                        _removeAdsButton.interactable = false;
                        _removeAdsButton.GetComponentInChildren<Text>().text = "Ads Removed";
                    });
                });

            UiFactory.CreateButton(canvas.transform, "Settings", new Vector2(115, -190), new Vector2(220, 50), true, () =>
            {
                SceneManager.LoadScene("SudokuSettings");
            });
        }

        private static string BuildStatsText(SudokuStatsStore statsStore)
        {
            var lines = new System.Text.StringBuilder();
            lines.AppendLine($"Completed: {statsStore.GetTotalCompleted()}");
            foreach (var difficulty in Difficulties)
            {
                var best = statsStore.GetBestTimeSeconds(difficulty);
                lines.AppendLine($"{difficulty} best: {(best.HasValue ? FormatTime(best.Value) : "--:--")}");
            }
            return lines.ToString();
        }

        private static string FormatTime(float seconds)
        {
            var total = Mathf.FloorToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
