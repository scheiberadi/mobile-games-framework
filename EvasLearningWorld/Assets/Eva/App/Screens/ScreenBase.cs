using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    public abstract class ScreenBase
    {
        public RectTransform Root { get; private set; }

        internal void Attach(RectTransform root) => Root = root;

        public abstract void Build(EvaGame game);
        public virtual void OnShow() { }
        public virtual void OnHide() { }

        // The shared "player character left, Eva right" pairing (M5 Task 7). Returns Eva so the screen can keep
        // driving her (talking, cheering) exactly as before; the player rig is decorative and refreshed by Navigator.
        protected CharacterRig AddCompanionPair(EvaGame game, CompanionLayout layout) =>
            CompanionPair.Create(Root, game, layout).Eva;

        // A background that also covers the parts of the canvas outside the safe area (Root only spans the safe area).
        protected void AddBackground(Color color)
        {
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-1500f, -1500f);
            rect.offsetMax = new Vector2(1500f, 1500f);
            var image = background.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        // A large non-interactive picture that says which screen this is until real art exists.
        protected void AddTitle(string spriteName, float size)
        {
            var title = new GameObject("Title", typeof(RectTransform), typeof(Image));
            title.transform.SetParent(Root, false);
            var rect = (RectTransform)title.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(size, size);
            var image = title.GetComponent<Image>();
            image.sprite = EvaUi.Sprite(spriteName);
            image.preserveAspect = true;
            image.raycastTarget = false;
        }
    }
}
