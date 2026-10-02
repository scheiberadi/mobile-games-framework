using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // The progress display of a real-time Arcade game, at the top middle of the screen: a bar for what the current level needs and a row
    // of stars for the levels. No text, so a child who cannot read still sees how far along it is.
    public sealed class ArcadeProgress
    {
        private const float BarWidth = 520f;
        private const float BarHeight = 38f;
        private const float BarY = 395f;
        private const float StarSize = 60f;
        private const float StarY = 335f;
        private const float StarSpacing = 68f;

        private RectTransform _fill;
        private Image[] _stars;

        public GameObject Root { get; private set; }

        public static ArcadeProgress Create(RectTransform parent, int levels)
        {
            var progress = new ArcadeProgress();
            var container = new GameObject("Progress", typeof(RectTransform));
            container.transform.SetParent(parent, false);
            progress.Root = container;
            var containerRect = (RectTransform)container.transform;
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = containerRect.offsetMax = Vector2.zero;

            var back = NewBox(containerRect, "BarBack", new Vector2(0f, BarY), new Vector2(BarWidth, BarHeight), new Color(0f, 0f, 0f, 0.55f));
            var fill = NewBox(back, "BarFill", new Vector2(4f, 0f), new Vector2(0f, BarHeight - 8f), new Color(1f, 0.82f, 0.15f, 1f));
            fill.anchorMin = fill.anchorMax = new Vector2(0f, 0.5f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.anchoredPosition = new Vector2(4f, 0f);
            progress._fill = fill;

            NewBox(containerRect, "StarsBack", new Vector2(0f, StarY), new Vector2(BarWidth, StarSize + 10f), new Color(0f, 0f, 0f, 0.4f));
            progress._stars = new Image[levels];
            for (var i = 0; i < levels; i++)
            {
                var x = (i - (levels - 1) / 2f) * StarSpacing;
                var star = NewBox(containerRect, "Level" + (i + 1), new Vector2(x, StarY), new Vector2(StarSize, StarSize), Color.white);
                var image = star.GetComponent<Image>();
                image.sprite = EvaUi.Sprite("arcade/prop_sparkle");
                image.preserveAspect = true;
                progress._stars[i] = image;
            }
            return progress;
        }

        // `level` is 1-based; `fraction` is how much of this level's hits are done (0-1).
        public void Show(int level, float fraction)
        {
            _fill.sizeDelta = new Vector2((BarWidth - 8f) * Mathf.Clamp01(fraction), BarHeight - 8f);
            for (var i = 0; i < _stars.Length; i++)
                _stars[i].color = i < level ? Color.white : new Color(1f, 1f, 1f, 0.28f);
        }

        private static RectTransform NewBox(RectTransform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }
    }
}
