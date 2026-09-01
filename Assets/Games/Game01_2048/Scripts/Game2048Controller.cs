using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using MobileGamesFramework.Grid;

namespace Game01_2048
{
    public class Game2048Controller : MonoBehaviour
    {
        private const int BoardSize = 4;

        private Game2048Game _game;
        private Text[,] _cellLabels;
        private Image[,] _cellImages;
        private Text _scoreText;
        private Text _statusText;
        private Vector2? _dragStart;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var host = new GameObject("Game2048Controller");
            host.AddComponent<Game2048Controller>();
        }

        private void Start()
        {
            _game = new Game2048Game(new Game2048SpawnStrategy(new System.Random()), BoardSize, BoardSize);
            BuildUi();
            _game.NewGame();
            Refresh();
        }

        private void Update()
        {
            var direction = ReadKeyboardDirection() ?? ReadDragDirection();
            if (direction.HasValue && _game.ApplyMove(direction.Value))
                Refresh();
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

        private void Refresh()
        {
            for (var row = 0; row < BoardSize; row++)
            for (var col = 0; col < BoardSize; col++)
            {
                var value = _game.Grid.Get(new GridPosition(row, col));
                _cellLabels[row, col].text = value?.ToString() ?? "";
                _cellImages[row, col].color = Game2048TileColors.ForValue(value);
            }

            _scoreText.text = $"Score: {_game.Score}";
            _statusText.text = _game.State switch
            {
                GameState.Won => "You win!",
                GameState.Lost => "Game over",
                _ => ""
            };
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

            _scoreText = CreateText(canvasObject.transform, "ScoreText", 32, TextAnchor.UpperCenter);
            SetRect(_scoreText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(400, 60));

            _statusText = CreateText(canvasObject.transform, "StatusText", 28, TextAnchor.UpperCenter);
            SetRect(_statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(400, 40));

            var gridObject = new GameObject("Grid", typeof(GridLayoutGroup));
            gridObject.transform.SetParent(canvasObject.transform, false);
            var gridRect = gridObject.GetComponent<RectTransform>();
            SetRect(gridRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440, 440));

            var layout = gridObject.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(100, 100);
            layout.spacing = new Vector2(10, 10);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = BoardSize;

            _cellLabels = new Text[BoardSize, BoardSize];
            _cellImages = new Image[BoardSize, BoardSize];

            for (var row = 0; row < BoardSize; row++)
            for (var col = 0; col < BoardSize; col++)
            {
                var cell = new GameObject($"Cell_{row}_{col}", typeof(Image));
                cell.transform.SetParent(gridObject.transform, false);
                _cellImages[row, col] = cell.GetComponent<Image>();

                var label = CreateText(cell.transform, "Label", 28, TextAnchor.MiddleCenter);
                SetRect(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                _cellLabels[row, col] = label;
            }
        }

        private static Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.black;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
