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
    //
    // M5 (docs/superpowers/plans/2026-09-27-m5-character-system.md, Task 1) added real wardrobe/hair/eye/
    // glasses layers under Torso and each Leg, all still crude placeholder shapes - no real wardrobe art
    // exists yet (Task 2/3). The names "ArmL", "ArmR" and "Head" and their "Torso/..." paths are load-bearing:
    // EvaRigAssets-generated animation clips (Resources/Anim/*.anim) bind curves to those exact paths, so they
    // are never renamed or moved out from directly under Torso, even though new siblings (Bottom/Top/Dress/
    // HairBack/Glasses) now sit alongside them - Transform.Find works by name, not by sibling index, so this
    // holds regardless of where the new layers are inserted. "Head" keeps its name for the same reason even
    // though its Image now shows Face art and it has grown its own EyeIris/HairFront children.
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

        // No real per-gender/per-Face-index art exists yet (Task 2/3) - every Face index stands in with one
        // of the four original placeholder head sprites, cycling, so a fresh CharacterLook still renders
        // something recognisable today.
        private const int LegacyFaceSpriteCount = 4;

        // Placeholder hair "shapes": only the front-coverage fraction (how far down over the forehead the
        // front piece reaches) varies until real haircut art exists. This alone is enough to prove Hair isn't
        // a single simple "always behind" or "always in front" layer (Task 1's occlusion spike) - HairStyle 2
        // is the case that most exercises it. CharacterLook.HairStyle indexes this array, clamped.
        private readonly struct HairShape
        {
            public HairShape(float frontCoverage) => FrontCoverage = frontCoverage;
            public float FrontCoverage { get; } // fraction of the head's own height covered at the front
        }

        private static readonly HairShape[] HairShapes =
        {
            new HairShape(0.12f), // short / no fringe
            new HairShape(0.35f), // a small fringe
            new HairShape(0.60f), // full bangs
        };

        public static int HairStyleCount => HairShapes.Length;

        public static CharacterRig CreatePlayer(Transform parent, CharacterLook look, float height)
        {
            var rig = Build(parent, "char", height);
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

        private static CharacterRig Build(Transform parent, string prefix, float height)
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
            var shoeL = Shoe(legL);
            var shoeR = Shoe(legR);

            var torsoGo = Part(root, "Torso", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, LegLength), new Vector2(TorsoWidth, TorsoHeight));
            var torsoRect = (RectTransform)torsoGo.transform;
            var torsoImage = torsoGo.GetComponent<Image>();

            // Clothing sits behind the arms (added before ArmL/ArmR, so it draws behind them - arms swing in
            // front of a shirt/jacket, not through it). Bottom and Top always both exist so ApplyLook can just
            // toggle them; Dress is a third, separate piece shown instead of the other two, never a Bottom
            // variant (see CharacterLook.SetDress) - each shaped to overlap past the torso's own bounds and up
            // toward where the arms attach, not just sit flush behind it (Task 1's occlusion spike).
            var bottom = Part(torsoRect, "Bottom", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(TorsoWidth + 16f, TorsoHeight * 0.55f)).GetComponent<Image>();
            var top = Part(torsoRect, "Top", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(TorsoWidth + ShoulderX * 1.2f, TorsoHeight + 10f)).GetComponent<Image>();
            var dress = Part(torsoRect, "Dress", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(TorsoWidth + ShoulderX * 1.2f, TorsoHeight + LegLength * 0.6f)).GetComponent<Image>();

            var armL = Part(torsoRect, "ArmL", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-ShoulderX, -ShoulderYDrop), new Vector2(ArmWidth, ArmLength));
            var armR = Part(torsoRect, "ArmR", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(ShoulderX, -ShoulderYDrop), new Vector2(ArmWidth, ArmLength));

            // Hair is two pieces, not one: HairBack (created before Head, so it draws behind the face) peeks
            // out past the head silhouette, HairFront is nested inside Head itself so it inherits the head's
            // own bob/tilt (see EvaRigAssets' Idle/Talk clips) and draws over the forehead (Task 1's occlusion
            // spike: hair is never a single simple "always behind" or "always in front" layer).
            var hairBack = Part(torsoRect, "HairBack", new Vector2(0.5f, 1f), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(HeadSize * 1.25f, HeadSize * 1.25f)).GetComponent<Image>();

            var head = Part(torsoRect, "Head", new Vector2(0.5f, 1f), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(HeadSize, HeadSize));
            var headImage = head.GetComponent<Image>();
            // EyeIris then HairFront, in that order, so the fringe draws in front of the eyes, not just the
            // face's own base art.
            var eyeIris = Band(head.transform, "EyeIris", 0.32f, 0.52f);
            var hairFront = Band(head.transform, "HairFront", 1f - HairShapes[0].FrontCoverage, 1f);

            // Glasses is a sibling of Head, not its child, added last so it draws in front of Head's ENTIRE
            // subtree - including HairFront - regardless of hairstyle (Task 1's occlusion spike: glasses over
            // the face must never fight a fringe drawn in front of it). Same rect as Head so it lines up.
            var glasses = Part(torsoRect, "Glasses", new Vector2(0.5f, 1f), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(HeadSize, HeadSize)).GetComponent<Image>();

            legL.sprite = EvaUi.Sprite("characters/" + prefix + "_leg");
            legR.sprite = EvaUi.Sprite("characters/" + prefix + "_leg");
            torsoImage.sprite = EvaUi.Sprite("characters/" + prefix + "_torso");
            var armLImage = armL.GetComponent<Image>();
            armLImage.sprite = EvaUi.Sprite("characters/" + prefix + "_arm");
            var armRImage = armR.GetComponent<Image>();
            armRImage.sprite = EvaUi.Sprite("characters/" + prefix + "_arm");

            var parts = new CharacterRig.PlayerParts
            {
                Torso = torsoImage, ArmL = armLImage, ArmR = armRImage, Face = headImage, LegL = legL, LegR = legR,
                HairBack = hairBack, HairFront = hairFront, EyeIris = eyeIris, Glasses = glasses,
                Top = top, Bottom = bottom, Dress = dress, ShoeL = shoeL, ShoeR = shoeR,
            };
            return new CharacterRig(animator, root, parts);
        }

        // LegL/LegR hang from a top pivot at the hip line (y = LegLength above the ground, which is Root's origin).
        private static Image Limb(RectTransform root, string name, float x)
        {
            var go = Part(root, name, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                new Vector2(x, LegLength), new Vector2(LegWidth, LegLength));
            return go.GetComponent<Image>();
        }

        // A foot overlay on this leg's own Image, added as its child so it moves and rotates with the leg and
        // draws in front of the leg's own art (Task 1's occlusion spike: shoes must overlap the bottom of the
        // leg art, not just sit cleanly below it).
        private static Image Shoe(Image leg) => Band(leg.transform, "Shoe", 0f, 0.35f);

        // A full-width horizontal band inside `parent`'s own rect, from yMin to yMax as a fraction of its
        // height (0 = parent's bottom edge, 1 = its top edge - standard RectTransform anchor semantics). No
        // sprite: a plain tinted rectangle, "crude placeholder shapes are fine" per the M5 plan's Task 1, until
        // real wardrobe/hair art exists.
        private static Image Band(Transform parent, string name, float yMin, float yMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, yMin);
            rect.anchorMax = new Vector2(1f, yMax);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
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

        internal static int FaceSpriteIndex(int face) => Mathf.Clamp(face, 0, CharacterLook.FaceCount - 1) % LegacyFaceSpriteCount;
        internal static HairShapeInfo HairShapeFor(int hairStyle)
        {
            var shape = HairShapes[Mathf.Clamp(hairStyle, 0, HairShapes.Length - 1)];
            return new HairShapeInfo(shape.FrontCoverage);
        }

        // A tiny public-facing readout of HairShape, since HairShape itself is private (an implementation
        // placeholder, not part of RigFactory's public surface).
        internal readonly struct HairShapeInfo
        {
            public HairShapeInfo(float frontCoverage) => FrontCoverage = frontCoverage;
            public float FrontCoverage { get; }
        }
    }
}
