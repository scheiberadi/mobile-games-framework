using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Builds the uGUI cutout rigs. The player (humanoid): Root(Animator) > LegL, LegR, Torso > ArmL, ArmR, Head. Child order
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

        public static CharacterRig CreatePlayer(Transform parent, CharacterLook look, float height)
        {
            var headIndex = Mathf.Clamp(look.Head, 0, CharacterLook.HeadCount - 1);
            var rig = Build(parent, "char", "char_head_" + headIndex, height);
            rig.ApplyLook(look);
            return rig;
        }

        // Eva is a layered real-cat cutout (art/eva/cat, 1000x1000 shared canvas) driven by CatMotion, not the Animator.
        // Layout: Root > Hop > Shadow, Tail, Body > (LegL, LegR, Chest, Head > (EarL, EarR, Eyes, Mouth)). Pivots are
        // the art-space points each layer rotates/scales around; the ground line sits at art y=940.
        public const float CatCanvas = 740f;      // 1000 art units shown at 740 px -> cat about CatDisplayHeight tall
        public const float CatDisplayHeight = 560f;
        private const float CatGroundPivot = 0.06f;

        public static CharacterRig CreateEva(Transform parent, float height)
        {
            var rootObject = new GameObject("Root", typeof(RectTransform), typeof(CatMotion));
            rootObject.transform.SetParent(parent, false);
            var root = (RectTransform)rootObject.transform;
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0f);
            root.pivot = new Vector2(0.5f, CatGroundPivot);
            root.sizeDelta = new Vector2(CatCanvas, CatCanvas);
            root.localScale = Vector3.one * (height / CatDisplayHeight);

            var hop = CatLayer(null, "Hop", root, 500f, 940f);
            CatLayer("cat_shadow", "Shadow", hop, 500f, 948f);
            var tail = CatLayer("cat_tail", "Tail", hop, 700f, 915f);
            var body = CatLayer("cat_body", "Body", hop, 500f, 940f);
            var legL = CatLayer("cat_legL", "LegL", body, 452f, 740f);
            var legR = CatLayer("cat_legR", "LegR", body, 548f, 740f);
            CatLayer("cat_chest", "Chest", body, 500f, 700f);
            var head = CatLayer("cat_head", "Head", body, 500f, 510f);
            // Ears rotate with the head but draw behind the head image.
            var earL = CatLayer("cat_earL", "EarL", head, 390f, 320f);
            var earR = CatLayer("cat_earR", "EarR", head, 610f, 320f);
            earL.SetAsFirstSibling();
            earR.SetAsFirstSibling();
            var eyes = CatLayer("cat_eyes", "Eyes", head, 500f, 380f);
            var mouth = CatLayer("cat_mouth", "Mouth", head, 500f, 478f);

            var motion = rootObject.GetComponent<CatMotion>();
            motion.Init(hop, tail, body, legL, legR, head, earL, earR, eyes, mouth.GetComponent<Image>());
            return new CharacterRig(motion, root);
        }

        // Full-rect layer whose pivot (art coordinates, y down) is where it rotates and scales; no sprite for a pure container.
        private static RectTransform CatLayer(string sprite, string name, Transform parent, float px, float py)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(px / 1000f, 1f - py / 1000f);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            if (sprite != null) image.sprite = EvaUi.Sprite("cat/" + sprite);
            else image.enabled = false;
            return rect;
        }

        private static CharacterRig Build(Transform parent, string prefix, string headSprite, float height)
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

            return new CharacterRig(animator, root, torsoImage, armLImage, armRImage, headImage, legL, legR);
        }

        // LegL/LegR hang from a top pivot at the hip line (y = LegLength above the ground, which is Root's origin).
        private static Image Limb(RectTransform root, string name, float x)
        {
            var go = Part(root, name, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                new Vector2(x, LegLength), new Vector2(LegWidth, LegLength));
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
