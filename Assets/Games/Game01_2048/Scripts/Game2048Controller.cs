using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using MobileGamesFramework.Grid;
using MobileGamesFramework.Persistence;
using MobileGamesFramework.Monetization;
using MobileGamesFramework.UI;

namespace Game01_2048
{
    public class Game2048Controller : MonoBehaviour
    {
        private const int BoardSize = 4;
        private const string GameId = "2048";
        private const int InterstitialCadence = 3;
        private const string RemoveAdsProductId = "remove_ads";

        private Game2048Game _game;
        private Game2048SaveService _saveService;
        private HighScoreStore _highScoreStore;
        private IAdProvider _adProvider;
        private IIapProvider _iapProvider;
        private InterstitialCadenceTracker _cadenceTracker;
        private Text[,] _cellLabels;
        private Image[,] _cellImages;
        private Text _scoreText;
        private Text _highScoreText;
        private Text _statusText;
        private Button _undoButton;
        private Button _watchAdButton;
        private Vector2? _dragStart;
        private int?[,] _previousValues;

        private GameObject _endScreenPanel;
        private Text _endScreenTitleText;
        private Text _endScreenScoreText;
        private Text _endScreenHighScoreText;

        private void Start()
        {
            var store = new PlayerPrefsStore();
            _saveService = new Game2048SaveService(store);
            _highScoreStore = new HighScoreStore(store);
            _cadenceTracker = new InterstitialCadenceTracker(store);
            _adProvider = new AdMobAdProvider();
            _iapProvider = new UnityIapProvider(new[] { RemoveAdsProductId });

            var spawner = new Game2048SpawnStrategy(new System.Random());
            if (GameSessionIntent.ResumeFromSave && _saveService.TryLoad(spawner, out var loadedGame))
                _game = loadedGame;
            else
                _game = new Game2048Game(spawner, BoardSize, BoardSize);

            BuildUi();

            if (!GameSessionIntent.ResumeFromSave)
                _game.NewGame();

            Refresh();
        }

        private void Update()
        {
            var direction = ReadKeyboardDirection() ?? ReadDragDirection();
            if (!direction.HasValue) return;

            var wasPlaying = _game.State == GameState.Playing;
            if (!_game.ApplyMove(direction.Value)) return;

            Refresh();

            if (wasPlaying && _game.State != GameState.Playing)
                OnGameCompleted();
        }

        private void OnGameCompleted()
        {
            if (_iapProvider.IsPurchased(RemoveAdsProductId)) return;
            if (_cadenceTracker.ShouldShowInterstitial(GameId, InterstitialCadence))
                _adProvider.ShowInterstitial();
        }

        private Direction? ReadKeyboardDirection()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return null;

            if (keyboard.upArrowKey.wasPressedThisFrame) return Direction.Up;
            if (keyboard.downArrowKey.wasPressedThisFrame) return Direction.Down;
            if (keyboard.leftArrowKey.wasPressedThisFrame) return Direction.Left;
            if (keyboard.rightArrowKey.wasPressedThisFrame) return Direction.Right;
            return null;
        }

        private Direction? ReadDragDirection()
        {
            var pointer = Pointer.current;
            if (pointer == null) return null;

            if (pointer.press.wasPressedThisFrame)
                _dragStart = pointer.position.ReadValue();

            if (pointer.press.wasReleasedThisFrame && _dragStart.HasValue)
            {
                var end = pointer.position.ReadValue();
                var delta = end - _dragStart.Value;
                _dragStart = null;
                return SwipeInputInterpreter.FromDelta(delta.x, -delta.y);
            }

            return null;
        }

        private void Restart()
        {
            _game.NewGame();
            Refresh();
        }

        private void ReturnToMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }

        private void UndoMove()
        {
            if (_game.Undo())
                Refresh();
        }

        private void WatchAdForUndo()
        {
            _adProvider.ShowRewarded(granted =>
            {
                if (!granted) return;
                _game.GrantExtraUndo();
                Refresh();
            });
        }

        private void Refresh()
        {
            for (var row = 0; row < BoardSize; row++)
            for (var col = 0; col < BoardSize; col++)
            {
                var value = _game.Grid.Get(new GridPosition(row, col));
                _cellLabels[row, col].text = value?.ToString() ?? "";
                _cellImages[row, col].color = Game2048TileColors.ForValue(value);

                if (value != null && value != _previousValues[row, col])
                    StartCoroutine(PopCell(_cellImages[row, col].rectTransform));
                _previousValues[row, col] = value;
            }

            _highScoreStore.ReportScore(GameId, _game.Score);

            _scoreText.text = $"Score: {_game.Score}";
            _highScoreText.text = $"Best: {_highScoreStore.GetHighScore(GameId)}";
            _statusText.text = _game.State switch
            {
                GameState.Won => "You win!",
                GameState.Lost => "Game over",
                _ => ""
            };
            _undoButton.interactable = _game.CanUndo;
            _watchAdButton.interactable = _game.UndoCredits == 0 && _adProvider.IsRewardedReady;

            if (_game.State == GameState.Playing)
                _saveService.Save(_game);
            else
                _saveService.ClearSave();

            var gameOver = _game.State != GameState.Playing;
            _endScreenPanel.SetActive(gameOver);
            if (gameOver)
            {
                _endScreenTitleText.text = _game.State == GameState.Won ? "You Win!" : "Game Over";
                _endScreenScoreText.text = $"Score: {_game.Score}";
                _endScreenHighScoreText.text = $"Best: {_highScoreStore.GetHighScore(GameId)}";
            }
        }

        private static IEnumerator PopCell(RectTransform rect)
        {
            const float duration = 0.12f;
            var elapsed = 0f;
            rect.localScale = Vector3.one * 0.7f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                rect.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, elapsed / duration);
                yield return null;
            }

            rect.localScale = Vector3.one;
        }

        private void BuildUi()
        {
            var canvas = UiFactory.CreateCanvas();

            _scoreText = UiFactory.CreateText(canvas.transform, "ScoreText", 32, TextAnchor.UpperCenter);
            UiFactory.SetRect(_scoreText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(400, 60));

            _highScoreText = UiFactory.CreateText(canvas.transform, "HighScoreText", 22, TextAnchor.UpperCenter);
            UiFactory.SetRect(_highScoreText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -75), new Vector2(400, 40));

            _statusText = UiFactory.CreateText(canvas.transform, "StatusText", 28, TextAnchor.UpperCenter);
            UiFactory.SetRect(_statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(400, 40));

            var boardBackground = new GameObject("BoardBackground", typeof(Image));
            boardBackground.transform.SetParent(canvas.transform, false);
            UiFactory.SetRect(boardBackground.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460, 460));
            var backgroundImage = boardBackground.GetComponent<Image>();
            backgroundImage.sprite = RoundedRectSprite.Get();
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.color = new Color(0.73f, 0.68f, 0.63f);

            var gridObject = new GameObject("Grid", typeof(GridLayoutGroup));
            gridObject.transform.SetParent(canvas.transform, false);
            var gridRect = gridObject.GetComponent<RectTransform>();
            UiFactory.SetRect(gridRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440, 440));

            var layout = gridObject.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(100, 100);
            layout.spacing = new Vector2(10, 10);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = BoardSize;

            _cellLabels = new Text[BoardSize, BoardSize];
            _cellImages = new Image[BoardSize, BoardSize];
            _previousValues = new int?[BoardSize, BoardSize];

            for (var row = 0; row < BoardSize; row++)
            for (var col = 0; col < BoardSize; col++)
            {
                var cell = new GameObject($"Cell_{row}_{col}", typeof(Image));
                cell.transform.SetParent(gridObject.transform, false);
                var cellImage = cell.GetComponent<Image>();
                cellImage.sprite = RoundedRectSprite.Get();
                cellImage.type = Image.Type.Sliced;
                _cellImages[row, col] = cellImage;

                var label = UiFactory.CreateText(cell.transform, "Label", 28, TextAnchor.MiddleCenter);
                UiFactory.SetRect(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                _cellLabels[row, col] = label;
            }

            _undoButton = UiFactory.CreateButton(canvas.transform, "Undo", new Vector2(-90, -400), new Vector2(160, 50), false, UndoMove);
            UiFactory.CreateButton(canvas.transform, "Restart", new Vector2(90, -400), new Vector2(160, 50), true, Restart);
            _watchAdButton = UiFactory.CreateButton(canvas.transform, "Watch Ad +1 Undo", new Vector2(0, -460), new Vector2(220, 50), false, WatchAdForUndo);
            UiFactory.CreateButton(canvas.transform, "Menu", new Vector2(0, -520), new Vector2(160, 50), true, ReturnToMenu);

            BuildEndScreen(canvas.transform);
        }

        private void BuildEndScreen(Transform parent)
        {
            _endScreenPanel = new GameObject("EndScreen", typeof(Image));
            _endScreenPanel.transform.SetParent(parent, false);
            UiFactory.SetRect(_endScreenPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _endScreenPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            var panelBackground = new GameObject("Panel", typeof(Image));
            panelBackground.transform.SetParent(_endScreenPanel.transform, false);
            UiFactory.SetRect(panelBackground.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 320));
            var panelImage = panelBackground.GetComponent<Image>();
            panelImage.sprite = RoundedRectSprite.Get();
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.96f, 0.94f, 0.90f);

            _endScreenTitleText = UiFactory.CreateText(panelBackground.transform, "Title", 36, TextAnchor.MiddleCenter);
            UiFactory.SetRect(_endScreenTitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(320, 50));

            _endScreenScoreText = UiFactory.CreateText(panelBackground.transform, "Score", 26, TextAnchor.MiddleCenter);
            UiFactory.SetRect(_endScreenScoreText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(320, 40));

            _endScreenHighScoreText = UiFactory.CreateText(panelBackground.transform, "HighScore", 22, TextAnchor.MiddleCenter);
            UiFactory.SetRect(_endScreenHighScoreText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -145), new Vector2(320, 30));

            UiFactory.CreateButton(panelBackground.transform, "Undo", new Vector2(-85, -230), new Vector2(150, 50), true, UndoMove);
            UiFactory.CreateButton(panelBackground.transform, "Restart", new Vector2(85, -230), new Vector2(150, 50), true, Restart);
            UiFactory.CreateButton(panelBackground.transform, "Watch Ad +1 Undo", Vector2.zero, new Vector2(220, 50), true, WatchAdForUndo);

            _endScreenPanel.SetActive(false);
        }
    }
}
