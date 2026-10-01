using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // The shared "player character left, Eva right" pairing a gameplay screen opts into (M5 Task 6, spec's
    // "Screen integration"). A screen calls CompanionPair.Create(Root, game, CompanionLayout.Corner) instead of
    // hand-rolling its own rig positions and sizes, and talks to Eva through .Eva as it does today.
    //
    // Everything under it is non-interactive: raycasts are turned off on every Graphic so the pairing can never
    // swallow a tap meant for the screen's own buttons, and it adds no TapTarget. Eva is mirrored to face the
    // player (the rig faces right by default); the player faces the front, so the two read as a pair.
    public sealed class CompanionPair
    {
        public RectTransform Root { get; }
        public CharacterRig Player { get; }
        public CharacterRig Eva { get; }
        public CompanionLayout Layout { get; }

        private CompanionPair(RectTransform root, CharacterRig player, CharacterRig eva, CompanionLayout layout)
        {
            Root = root; Player = player; Eva = eva; Layout = layout;
        }

        public static CompanionPair Create(Transform parent, EvaGame game, CompanionLayout layout)
        {
            var go = new GameObject("CompanionPair", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var root = (RectTransform)go.transform;
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = Vector2.zero;

            var player = RigFactory.CreatePlayer(Anchor(root, "PlayerAnchor", layout.PlayerX, layout.FeetY), game.Progress.Look, layout.PlayerHeight);
            var eva = RigFactory.CreateEva(Anchor(root, "EvaAnchor", layout.EvaX, layout.FeetY), layout.EvaHeight);
            var s = eva.Root.localScale;
            eva.Root.localScale = new Vector3(-Mathf.Abs(s.x), s.y, s.z);

            foreach (var graphic in go.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            return new CompanionPair(root, player, eva, layout);
        }

        // Re-reads the child's saved look (call after it changes, e.g. when a screen is shown again).
        public void Refresh(CharacterLook look) => Player.ApplyLook(look);

        private static RectTransform Anchor(RectTransform parent, string name, float x, float y)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = Vector2.zero;
            return rect;
        }
    }
}
