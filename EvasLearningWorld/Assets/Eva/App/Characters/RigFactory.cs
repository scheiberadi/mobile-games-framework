using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Builds the uGUI cutout rig: Root(Animator) > LegL, LegR, [Tail], Torso > ArmL, ArmR, Head. Child order
    // is draw order (later children in front), matching the M0 SpriteRenderer pattern this proves out on
    // RectTransforms. Every part is laid out at a fixed canonical size (CanonicalHeight) so the one shared
    // controller and clips, authored against that size, read the same on every instance; Root.localScale
    // then resizes the whole rig to the requested on-screen height without touching any animated value
    // (the animated properties, e.g. the torso's anchored Y, are absolute and shared by every rig instance).
    public static class RigFactory
    {
        public const float CanonicalHeight = 224f;
        public const float LegLength = 70f;
        public const float LegWidth = 34f;
        public const float LegGap = 17f;
        public const float TorsoWidth = 78f;
        public const float TorsoHeight = 90f;
        public const float ArmWidth = 24f;
        public const float ArmLength = 78f;
        public const float ShoulderX = 33f;
        public const float ShoulderYDrop = 6f;
        public const float HeadSize = 64f;
        public const float TailWidth = 18f;
        public const float TailLength = 64f;

        public static CharacterRig CreatePlayer(Transform parent, CharacterLook look, float height)
        {
            var headIndex = Mathf.Clamp(look.Head, 0, CharacterLook.HeadCount - 1);
            var rig = Build(parent, "char", "char_head_" + headIndex, height, false);
            rig.ApplyLook(look);
            return rig;
        }

        public static CharacterRig CreateEva(Transform parent, float height) =>
            Build(parent, "eva", "eva_head", height, true);

        private static CharacterRig Build(Transform parent, string prefix, string headSprite, float height, bool hasTail)
        {
            var rootObject = new GameObject("Root", typeof(RectTransform), typeof(Animator));
            rootObject.transform.SetParent(parent, false);
            var root = (RectTransform)rootObject.transform;
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = Vector2.zero;
            root.localScale = Vector3.one * (height / CanonicalHeight);

            var animator = rootObject.GetComponent<Animator>();
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Anim/Rig");

            var legL = Limb(root, "LegL", -LegGap);
            var legR = Limb(root, "LegR", LegGap);

            Image tail = null;
            if (hasTail) tail = Tail(root);

            var torsoGo = Part(root, "Torso", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, LegLength), new Vector2(TorsoWidth, TorsoHeight));
            var torsoRect = (RectTransform)torsoGo.transform;

            var armL = Part(torsoRect, "ArmL", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-ShoulderX, -ShoulderYDrop), new Vector2(ArmWidth, ArmLength));
            var armR = Part(torsoRect, "ArmR", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(ShoulderX, -ShoulderYDrop), new Vector2(ArmWidth, ArmLength));
            var head = Part(torsoRect, "Head", new Vector2(0.5f, 1f), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(HeadSize, HeadSize));

            legL.sprite = EvaUi.Sprite("characters/" + prefix + "_leg");
            legR.sprite = EvaUi.Sprite("characters/" + prefix + "_leg");
            var torsoImage = torsoGo.GetComponent<Image>();
            torsoImage.sprite = EvaUi.Sprite("characters/" + prefix + "_torso");
            var armLImage = armL.GetComponent<Image>();
            armLImage.sprite = EvaUi.Sprite("characters/" + prefix + "_arm");
            var armRImage = armR.GetComponent<Image>();
            armRImage.sprite = EvaUi.Sprite("characters/" + prefix + "_arm");
            var headImage = head.GetComponent<Image>();
            headImage.sprite = EvaUi.Sprite("characters/" + headSprite);

            return new CharacterRig(animator, root, torsoImage, armLImage, armRImage, headImage, legL, legR, tail, prefix == "char");
        }

        // LegL/LegR hang from a top pivot at the hip line (y = LegLength above the ground, which is Root's origin).
        private static Image Limb(RectTransform root, string name, float x)
        {
            var go = Part(root, name, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                new Vector2(x, LegLength), new Vector2(LegWidth, LegLength));
            return go.GetComponent<Image>();
        }

        // The tail is a direct child of Root (not the torso), peeking out to one side near the hip.
        private static Image Tail(RectTransform root)
        {
            var go = Part(root, "Tail", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                new Vector2(TorsoWidth * 0.42f, LegLength + 8f), new Vector2(TailWidth, TailLength));
            return go.GetComponent<Image>();
        }

        private static GameObject Part(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.preserveAspect = false;
            image.raycastTarget = false;
            return go;
        }
    }
}
