using System.Collections.Generic;
using MobileGamesFramework.Persistence;
using UnityEngine;

namespace Game02_Sudoku
{
    public enum ThemeKind { Classic, Ocean, Sunset, Dark }

    public readonly struct ThemePalette
    {
        public readonly Color BackgroundTop;
        public readonly Color BackgroundBottom;
        public readonly Color PanelColor;
        public readonly Color ActiveTop;
        public readonly Color ActiveBottom;
        public readonly Color TextColor;

        public ThemePalette(Color backgroundTop, Color backgroundBottom, Color panelColor, Color activeTop, Color activeBottom, Color textColor)
        {
            BackgroundTop = backgroundTop;
            BackgroundBottom = backgroundBottom;
            PanelColor = panelColor;
            ActiveTop = activeTop;
            ActiveBottom = activeBottom;
            TextColor = textColor;
        }
    }

    public class SudokuThemeSettings
    {
        private const string ThemeKey = "settings.theme";

        private readonly IKeyValueStore _store;

        public SudokuThemeSettings(IKeyValueStore store)
        {
            _store = store;
        }

        public ThemeKind CurrentTheme
        {
            get
            {
                var raw = _store.GetString(ThemeKey, "");
                return System.Enum.TryParse<ThemeKind>(raw, out var theme) ? theme : ThemeKind.Classic;
            }
        }

        public void SetTheme(ThemeKind theme) => _store.SetString(ThemeKey, theme.ToString());
    }

    // Central palette lookup for every Sudoku screen's chrome - page background gradient,
    // popup panel color, active-button gradient and primary text color. Grid gameplay
    // colors (given/selected/mistake/highlight cells) are semantic indicators, not chrome,
    // and stay constant across themes by design.
    public static class SudokuTheme
    {
        private static readonly Dictionary<ThemeKind, ThemePalette> Palettes = new Dictionary<ThemeKind, ThemePalette>
        {
            [ThemeKind.Classic] = new ThemePalette(
                new Color(0.75f, 0.85f, 0.97f), new Color(0.98f, 0.98f, 1f),
                new Color(0.96f, 0.94f, 0.90f),
                new Color(0.56f, 0.88f, 0.82f), new Color(0.31f, 0.66f, 0.60f),
                Color.black),
            [ThemeKind.Ocean] = new ThemePalette(
                new Color(0.55f, 0.75f, 0.90f), new Color(0.85f, 0.93f, 0.97f),
                new Color(0.90f, 0.95f, 0.97f),
                new Color(0.40f, 0.75f, 0.85f), new Color(0.15f, 0.50f, 0.65f),
                new Color(0.05f, 0.15f, 0.25f)),
            [ThemeKind.Sunset] = new ThemePalette(
                new Color(0.98f, 0.75f, 0.55f), new Color(1f, 0.90f, 0.80f),
                new Color(0.99f, 0.93f, 0.87f),
                new Color(0.98f, 0.60f, 0.45f), new Color(0.85f, 0.35f, 0.30f),
                new Color(0.30f, 0.12f, 0.08f)),
            [ThemeKind.Dark] = new ThemePalette(
                new Color(0.14f, 0.15f, 0.18f), new Color(0.08f, 0.09f, 0.11f),
                new Color(0.20f, 0.21f, 0.24f),
                new Color(0.45f, 0.70f, 0.65f), new Color(0.25f, 0.50f, 0.46f),
                new Color(0.92f, 0.92f, 0.94f)),
        };

        private static ThemeKind? _current;

        private static SudokuThemeSettings Settings => new SudokuThemeSettings(new PlayerPrefsStore());

        public static ThemeKind CurrentTheme
        {
            get
            {
                _current ??= Settings.CurrentTheme;
                return _current.Value;
            }
        }

        public static ThemePalette Palette => Palettes[CurrentTheme];

        // Called by the Settings screen's theme-cycling button. Persists the choice; the
        // caller is still responsible for reloading the active scene so on-screen colors
        // actually refresh (see SudokuSettingsController), same as SetLanguage in Loc.
        public static void SetTheme(ThemeKind theme)
        {
            Settings.SetTheme(theme);
            _current = theme;
        }
    }
}
