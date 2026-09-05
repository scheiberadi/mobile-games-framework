using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MobileGamesFramework.Persistence;
using MobileGamesFramework.UI;

namespace Game02_Sudoku
{
    public class SudokuMenuController : MonoBehaviour
    {
        private static readonly Difficulty[] Difficulties =
        {
            Difficulty.Easy, Difficulty.Medium, Difficulty.Hard, Difficulty.Expert
        };

        private GameObject _difficultyPopup;

        private void Start()
        {
            var store = new PlayerPrefsStore();
            var saveService = new SudokuSaveService(store);

            BuildUi(saveService, saveService.HasSave());
        }

        private void BuildUi(SudokuSaveService saveService, bool hasSave)
        {
            var canvas = UiFactory.CreateCanvas();
            UiFactory.CreateBackground(canvas.transform, new Color(0.75f, 0.85f, 0.97f), new Color(0.98f, 0.98f, 1f));

            BuildSettingsGearButton(canvas.transform);
            BuildBrandHeader(canvas.transform);

            UiFactory.CreateButton(canvas.transform, "New Game", new Vector2(0, 100), new Vector2(320, 68), true, () =>
            {
                _difficultyPopup.SetActive(true);
            });

            UiFactory.CreateButton(canvas.transform, "Continue", new Vector2(0, 15), new Vector2(320, 68), hasSave, () =>
            {
                SudokuSessionIntent.ResumeFromSave = true;
                SudokuSessionIntent.EnterCustom = false;
                SceneManager.LoadScene("Sudoku");
            });

            UiFactory.CreateButton(canvas.transform, "High Scores", new Vector2(0, -70), new Vector2(320, 68), true, () =>
            {
                SceneManager.LoadScene("SudokuHighScores");
            });

            UiFactory.CreateButton(canvas.transform, "Exit Game", new Vector2(0, -155), new Vector2(320, 68), true, () =>
            {
                Application.Quit();
            });

            BuildDifficultyPopup(canvas.transform, saveService);
        }

        private void BuildBrandHeader(Transform parent)
        {
            // Uses the designed icon mark directly (Assets/Resources/Branding/icon_mark.png
            // - the same transparent adaptive-icon foreground layer used for the app icon
            // itself) rather than the flattened splash artwork, whose flat "paper"
            // background can't be separated from the icon's own white cells. Text is
            // rendered natively so it always matches the app's own background exactly,
            // with no baked-in backdrop to fight.
            var icon = new GameObject("IconMark", typeof(Image));
            icon.transform.SetParent(parent, false);
            var iconImage = icon.GetComponent<Image>();
            iconImage.sprite = Resources.Load<Sprite>("Branding/icon_mark");
            iconImage.preserveAspect = true;
            UiFactory.SetRect(icon.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 420), new Vector2(220, 220));

            var title = UiFactory.CreateText(parent, "Title", 36, TextAnchor.MiddleCenter);
            title.text = "NoAdsGuy's Sudoku";
            title.color = new Color(0.125f, 0.118f, 0.114f);
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 275), new Vector2(600, 50));

            var tagline = UiFactory.CreateText(parent, "Tagline", 20, TextAnchor.MiddleCenter);
            tagline.text = "No ads. Ever.";
            tagline.fontStyle = FontStyle.Italic;
            tagline.color = new Color(0.667f, 0.043f, 0.337f);
            UiFactory.SetRect(tagline.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(500, 34));
        }

        private void BuildSettingsGearButton(Transform parent)
        {
            var button = UiFactory.CreateButton(parent, "", new Vector2(-20, -20), new Vector2(56, 56), true, () =>
            {
                SceneManager.LoadScene("SudokuSettings");
            }, new Vector2(1f, 1f));
            button.GetComponentInChildren<Text>().text = "";

            var icon = new GameObject("GearIcon", typeof(Image));
            icon.transform.SetParent(button.transform, false);
            UiFactory.SetRect(icon.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36, 36));
            icon.GetComponent<Image>().sprite = GearIconSprite.Get(new Color(0.35f, 0.30f, 0.15f));
        }

        private void BuildDifficultyPopup(Transform parent, SudokuSaveService saveService)
        {
            _difficultyPopup = new GameObject("DifficultyPopup", typeof(Image));
            _difficultyPopup.transform.SetParent(parent, false);
            UiFactory.SetRect(_difficultyPopup.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _difficultyPopup.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            var panel = new GameObject("Panel", typeof(Image));
            panel.transform.SetParent(_difficultyPopup.transform, false);
            UiFactory.SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 380));
            var panelImage = panel.GetComponent<Image>();
            panelImage.sprite = RoundedRectSprite.Get();
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.96f, 0.94f, 0.90f);

            // All content below is center-anchored (matches UiFactory.CreateButton) on a uniform
            // 65px row step, so panel height only needs to track content - no top-anchored label
            // drifting independently of the buttons as rows are added.
            var label = UiFactory.CreateText(panel.transform, "Label", 24, TextAnchor.MiddleCenter);
            label.text = "Choose Difficulty";
            UiFactory.SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(320, 40));

            for (var i = 0; i < Difficulties.Length; i++)
            {
                var difficulty = Difficulties[i];
                var row = i / 2;
                var col = i % 2;
                var x = col == 0 ? -85 : 85;
                var y = 85 - row * 65;
                UiFactory.CreateButton(panel.transform, difficulty.ToString(), new Vector2(x, y), new Vector2(150, 50), true, () =>
                {
                    saveService.ClearSave();
                    SudokuSessionIntent.Difficulty = difficulty;
                    SudokuSessionIntent.ResumeFromSave = false;
                    SudokuSessionIntent.EnterCustom = false;
                    SceneManager.LoadScene("Sudoku");
                });
            }

            UiFactory.CreateButton(panel.transform, "Custom", new Vector2(0, -45), new Vector2(150, 50), true, () =>
            {
                SudokuSessionIntent.ResumeFromSave = false;
                SudokuSessionIntent.EnterCustom = true;
                SceneManager.LoadScene("Sudoku");
            });

            UiFactory.CreateButton(panel.transform, "Cancel", new Vector2(0, -110), new Vector2(150, 40), true, () =>
            {
                _difficultyPopup.SetActive(false);
            });

            _difficultyPopup.SetActive(false);
        }
    }
}
