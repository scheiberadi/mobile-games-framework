using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using MobileGamesFramework.UI;

namespace Game02_Sudoku
{
    // Thin recoloring wrapper around UiFactory: every "active" button in Sudoku's own
    // screens renders in this game's calming teal palette instead of UiFactory's shared
    // gold default, without touching UiFactory itself - which stays gold for every other
    // game (2048) that uses it.
    public static class SudokuUi
    {
        public static Color ActiveTop => SudokuTheme.Palette.ActiveTop;
        public static Color ActiveBottom => SudokuTheme.Palette.ActiveBottom;

        public static Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size, bool interactable, UnityAction onClick, Vector2? anchor = null)
        {
            var button = UiFactory.CreateButton(parent, label, position, size, interactable, onClick, anchor);
            if (interactable) Retint(button);
            return button;
        }

        public static Button CreateBackButton(Transform parent, UnityAction onClick, string label)
        {
            var button = UiFactory.CreateBackButton(parent, onClick, label);
            Retint(button);
            return button;
        }

        public static void SetInteractable(Button button, bool interactable)
        {
            UiFactory.SetInteractable(button, interactable);
            if (interactable) Retint(button);
        }

        private static void Retint(Button button)
        {
            button.GetComponent<Image>().sprite = RoundedRectSprite.GetGradient(ActiveTop, ActiveBottom);
        }
    }
}
