using System.Collections.Generic;
using UnityEngine;

namespace EvasLearningWorld.App
{
    public enum ScreenId { Creator, Map, House, School, Store, Count, NumberHunt, ParentGate, Settings, Playground, PatternCompletion, OddOneOut, WhatsMissing, WhichDoesntMakeSense, ItemToShadow, FingerMaze, FollowNumbersInOrder, FollowLettersInOrder }

    // Owns the screens and shows exactly one at a time under EvaGame.ScreenRoot. A screen is built the first time it is shown.
    public sealed class Navigator
    {
        private readonly EvaGame _game;
        private readonly Dictionary<ScreenId, ScreenBase> _screens = new Dictionary<ScreenId, ScreenBase>();

        public Navigator(EvaGame game) => _game = game;

        // Null until the first Show.
        public ScreenId? Current { get; private set; }

        public void Register(ScreenId id, ScreenBase screen) => _screens[id] = screen;

        public void Show(ScreenId id)
        {
            if (!_screens.TryGetValue(id, out var next))
            {
                Debug.LogError("Navigator: no screen registered for " + id);
                return;
            }

            if (Current.HasValue && Current.Value != id)
            {
                var previous = _screens[Current.Value];
                previous.OnHide();
                previous.Root.gameObject.SetActive(false);
            }

            if (next.Root == null)
            {
                var rootObject = new GameObject(next.GetType().Name, typeof(RectTransform));
                rootObject.transform.SetParent(_game.ScreenRoot, false);
                var rect = (RectTransform)rootObject.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                next.Attach(rect);
                next.Build(_game);
            }

            next.Root.gameObject.SetActive(true);
            Current = id;
            if (_game.Hud != null)
            {
                _game.Hud.SetHomeVisible(id != ScreenId.Map);
                _game.Hud.SetBubbleButtonVisible(id != ScreenId.Creator);
                _game.Hud.SetFpsVisible(id != ScreenId.ParentGate && id != ScreenId.Settings);
            }
            next.OnShow();
        }
    }
}
