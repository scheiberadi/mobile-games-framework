using System;
using System.Linq;
using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.Tests
{
    // The technical gate for the M1 character rig: does the M0 cutout pattern (proven on SpriteRenderers)
    // hold on uGUI RectTransforms? These tests check the exact hierarchy the clips bind by path to, the
    // pivots the spec requires, ApplyLook's tinting, and that every generated clip's curve bindings resolve
    // under a freshly built rig for both Eva and the player. Two more tests confirm, by evaluating a real
    // AnimationClip against a real RectTransform, that "m_AnchoredPosition.y" and "localEulerAnglesRaw.z"
    // actually drive RectTransform properties before EvaRigAssets.Generate is trusted to build clips with them.
    public class RigTests
    {
        [Test]
        public void PlayerHierarchyMatchesTheSpecAndHasNoTail()
        {
            var parent = NewParent();
            var rig = RigFactory.CreatePlayer(parent.transform, new CharacterLook(), 420f);
            var root = rig.Root;

            Assert.AreEqual(3, root.childCount, "Root should have exactly LegL, LegR, Torso for the player");
            Assert.AreEqual("LegL", root.GetChild(0).name);
            Assert.AreEqual("LegR", root.GetChild(1).name);
            Assert.AreEqual("Torso", root.GetChild(2).name);
            Assert.IsNull(root.Find("Tail"), "the player has no tail");

            var torso = root.Find("Torso");
            foreach (var name in new[] { "Bottom", "Top", "Dress", "ArmL", "ArmR", "Head", "HairBack", "Glasses" })
                Assert.IsNotNull(torso.Find(name), name + " should exist directly under Torso");
            Assert.IsNotNull(torso.Find("Head/EyeIris"));
            Assert.IsNotNull(torso.Find("Head/HairFront"));
            foreach (var legName in new[] { "LegL", "LegR" })
                Assert.IsNotNull(root.Find(legName + "/Shoe"), legName + " should carry its own Shoe overlay");

            // Draw-order proof (M5 Task 1's occlusion spike): later sibling index = drawn in front, per the
            // class comment on RigFactory ("child order is draw order, later children in front").
            int SiblingOf(string name) => torso.Find(name).GetSiblingIndex();
            Assert.Less(SiblingOf("HairBack"), SiblingOf("Head"), "hair-back must be drawn behind the head/face");
            Assert.Less(SiblingOf("Head"), SiblingOf("Glasses"), "glasses must draw in front of the face and its hair-front fringe");
            Assert.Less(SiblingOf("Top"), SiblingOf("ArmL"), "clothing sits behind the arms, not in front of them");
            Assert.Less(SiblingOf("Bottom"), SiblingOf("ArmL"));

            var head = torso.Find("Head");
            Assert.Less(head.Find("EyeIris").GetSiblingIndex(), head.Find("HairFront").GetSiblingIndex(),
                "the hair-front fringe must draw in front of the eyes, not the other way round");

            UnityEngine.Object.DestroyImmediate(parent);
        }

        [Test]
        public void EvaIsALayeredCatWithAHopContainerAndNoAnimator()
        {
            var parent = NewParent();
            var rig = RigFactory.CreateEva(parent.transform, 560f);
            var root = rig.Root;

            Assert.IsNull(rig.Animator, "Eva's cat is driven by CatMotion, not an Animator");
            Assert.IsNotNull(root.GetComponent<CatMotion>());
            Assert.AreEqual(1, root.childCount, "Root holds only the Hop container");
            var hop = root.GetChild(0);
            Assert.AreEqual("Hop", hop.name);
            Assert.AreEqual("Shadow", hop.GetChild(0).name);
            Assert.AreEqual("Tail", hop.GetChild(1).name);
            Assert.AreEqual("Body", hop.GetChild(2).name);
            foreach (var path in new[] { "Body/LegL", "Body/LegR", "Body/Chest", "Body/Head", "Body/Head/EarL", "Body/Head/EarR", "Body/Head/Eyes", "Body/Head/Mouth" })
                Assert.IsNotNull(hop.Find(path), path);
            Assert.IsTrue(hop.Find("Body/Head/EarL").GetSiblingIndex() < hop.Find("Body/Head/Eyes").GetSiblingIndex() && hop.Find("Body/Head/EarR").GetSiblingIndex() < hop.Find("Body/Head/Eyes").GetSiblingIndex(), "ears are ordered before the eyes/mouth");
            Assert.IsNull(hop.Find("Body/ArmL"), "no humanoid arms");
            foreach (var image in root.GetComponentsInChildren<Image>())
                if (image.enabled) Assert.IsNotNull(image.sprite, image.name + " has no sprite");

            UnityEngine.Object.DestroyImmediate(parent);
        }

        [Test]
        public void ArmsAndLegsPivotAtTheTopAndTorsoAtItsBottomCentre()
        {
            var parent = NewParent();
            var rig = RigFactory.CreatePlayer(parent.transform, new CharacterLook(), 420f);
            var root = rig.Root;

            AssertPivot(root.Find("LegL"), 0.5f, 1f);
            AssertPivot(root.Find("LegR"), 0.5f, 1f);
            AssertPivot(root.Find("Torso/ArmL"), 0.5f, 1f);
            AssertPivot(root.Find("Torso/ArmR"), 0.5f, 1f);
            AssertPivot(root.Find("Torso"), 0.5f, 0f);

            UnityEngine.Object.DestroyImmediate(parent);
        }

        [Test]
        public void ApplyLookTintsSkinPartsAndHairAndTogglesWornWardrobeSlots()
        {
            var parent = NewParent();
            var look = new CharacterLook { Gender = Gender.Girl, Face = 7, Skin = 3, HairColor = 2, EyeColor = 5 };
            look.SetTop("top_a");
            look.SetBottom("bottom_a");
            var rig = RigFactory.CreatePlayer(parent.transform, look, 420f);
            var root = rig.Root;

            var torso = root.Find("Torso").GetComponent<Image>();
            var armL = root.Find("Torso/ArmL").GetComponent<Image>();
            var armR = root.Find("Torso/ArmR").GetComponent<Image>();
            var head = root.Find("Torso/Head").GetComponent<Image>();
            var legL = root.Find("LegL").GetComponent<Image>();
            var legR = root.Find("LegR").GetComponent<Image>();
            var hairBack = root.Find("Torso/HairBack").GetComponent<Image>();
            var hairFront = root.Find("Torso/Head/HairFront").GetComponent<Image>();
            var eyeIris = root.Find("Torso/Head/EyeIris").GetComponent<Image>();

            Assert.AreEqual(Palette.Skin[3], torso.color, "the bare torso is skin-coloured until something covers it");
            Assert.AreEqual(Palette.Skin[3], armL.color);
            Assert.AreEqual(Palette.Skin[3], armR.color);
            Assert.AreEqual(Palette.Skin[3], head.color);
            Assert.AreEqual(Palette.Skin[3], legL.color);
            Assert.AreEqual(Palette.Skin[3], legR.color);
            Assert.AreEqual(Palette.HairColor[2], hairBack.color);
            Assert.AreEqual(Palette.HairColor[2], hairFront.color);
            Assert.AreEqual(Palette.EyeColor[5], eyeIris.color);

            var top = root.Find("Torso/Top").GetComponent<Image>();
            var bottom = root.Find("Torso/Bottom").GetComponent<Image>();
            var dress = root.Find("Torso/Dress").GetComponent<Image>();
            var glasses = root.Find("Torso/Glasses").GetComponent<Image>();
            var shoeL = root.Find("LegL/Shoe").GetComponent<Image>();
            Assert.IsTrue(top.gameObject.activeSelf, "Top was set, so it should be shown");
            Assert.IsTrue(bottom.gameObject.activeSelf);
            Assert.IsFalse(dress.gameObject.activeSelf, "Dress was never set, so it stays hidden even though Gender is Girl");
            Assert.IsFalse(glasses.gameObject.activeSelf, "no Glasses were picked");
            Assert.IsFalse(shoeL.gameObject.activeSelf, "no Shoes were picked");

            UnityEngine.Object.DestroyImmediate(parent);
        }

        [Test]
        public void DressReplacesTopAndBottomInsteadOfLayeringWithThem()
        {
            var parent = NewParent();
            var look = new CharacterLook { Gender = Gender.Girl };
            look.SetTop("top_a");
            look.SetDress("dress_a"); // per the data model, setting Dress must clear Top/Bottom
            Assert.IsNull(look.Top, "SetDress must clear Top at the data level");

            var rig = RigFactory.CreatePlayer(parent.transform, look, 420f);
            var root = rig.Root;

            Assert.IsTrue(root.Find("Torso/Dress").GetComponent<Image>().gameObject.activeSelf);
            Assert.IsFalse(root.Find("Torso/Top").GetComponent<Image>().gameObject.activeSelf);
            Assert.IsFalse(root.Find("Torso/Bottom").GetComponent<Image>().gameObject.activeSelf);

            UnityEngine.Object.DestroyImmediate(parent);
        }

        [Test]
        public void EveryGeneratedClipBindsOnlyToPathsThatExistUnderAFreshPlayerRig() =>
            AssertClipsResolve(() => RigFactory.CreatePlayer(NewParent().transform, new CharacterLook(), 420f).Root);

        private static void AssertClipsResolve(Func<RectTransform> buildRig)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Eva/Resources/Anim/Rig.controller");
            Assert.IsNotNull(controller, "Rig.controller must exist (run EvaRigAssets.Generate) before this test can pass");

            var root = buildRig();
            var clips = controller.animationClips.Where(c => c != null).Distinct().ToArray();
            Assert.Greater(clips.Length, 0, "the controller should reference at least one clip");

            foreach (var clip in clips)
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                var target = binding.path.Length == 0 ? root.transform : root.Find(binding.path);
                Assert.IsNotNull(target, clip.name + " binds to a path that does not exist in the rig: '" + binding.path + "'");
            }

            UnityEngine.Object.DestroyImmediate(root.transform.parent.gameObject);
        }

        // Confirms "m_AnchoredPosition.y" (the serialized-field name UnityEngine.UI backs RectTransform.anchoredPosition
        // with) actually drives a RectTransform when set through AnimationClip.SetCurve, by evaluating the clip
        // against a real RectTransform with SampleAnimation and reading the transform back.
        [Test]
        public void AnchoredPositionYCurveNameDrivesARectTransform()
        {
            var root = new GameObject("Root", typeof(RectTransform));
            var child = new GameObject("Torso", typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            var rect = (RectTransform)child.transform;
            rect.anchoredPosition = Vector2.zero;

            var clip = new AnimationClip();
            clip.SetCurve("Torso", typeof(RectTransform), "m_AnchoredPosition.y", new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 6f)));
            clip.SampleAnimation(root, 1f);

            Assert.AreEqual(6f, rect.anchoredPosition.y, 0.01f, "m_AnchoredPosition.y did not drive RectTransform.anchoredPosition.y");
            UnityEngine.Object.DestroyImmediate(root);
        }

        // Confirms "localEulerAnglesRaw.z" drives RectTransform rotation the same way it drives a plain Transform.
        [Test]
        public void LocalEulerAnglesRawZCurveNameDrivesARectTransform()
        {
            var root = new GameObject("Root", typeof(RectTransform));
            var child = new GameObject("ArmR", typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            var rect = (RectTransform)child.transform;

            var clip = new AnimationClip();
            clip.SetCurve("ArmR", typeof(RectTransform), "localEulerAnglesRaw.z", new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 150f)));
            clip.SampleAnimation(root, 1f);

            Assert.AreEqual(150f, rect.localEulerAngles.z, 0.01f, "localEulerAnglesRaw.z did not drive RectTransform rotation");
            UnityEngine.Object.DestroyImmediate(root);
        }

        // Mashing Wave must not throw and must not restart the animation while it is already playing or blending in.
        [Test]
        public void WaveIgnoresATapWhileAlreadyPlayingOrInTransition()
        {
            var parent = NewParent();
            var rig = RigFactory.CreatePlayer(parent.transform, new CharacterLook(), 420f);

            rig.Animator.Update(0f);
            rig.Wave();
            rig.Animator.Update(0f);
            Assert.IsTrue(rig.Animator.GetCurrentAnimatorStateInfo(0).IsName("Wave") || rig.Animator.IsInTransition(0),
                "expected Wave() to move the rig into (or towards) the Wave state");

            Assert.DoesNotThrow(() => rig.Wave());

            UnityEngine.Object.DestroyImmediate(parent);
        }

        // Greeting and cheer are bounces: mashing must not restart one that is already running.
        [Test]
        public void EvaGreetAndCheerAreIgnoredWhileABounceIsRunning()
        {
            var parent = NewParent();
            var rig = RigFactory.CreateEva(parent.transform, 560f);
            var motion = rig.Root.GetComponent<CatMotion>();

            Assert.IsFalse(motion.IsBouncing);
            rig.Wave();
            Assert.IsTrue(motion.IsBouncing);
            Assert.DoesNotThrow(() => { rig.Wave(); rig.Cheer(); rig.Angry(); rig.SetTalking(true); });
            Assert.DoesNotThrow(() => rig.ApplyLook(new CharacterLook()), "ApplyLook is a no-op for Eva");

            UnityEngine.Object.DestroyImmediate(parent);
        }

        private static void AssertPivot(Transform t, float x, float y)
        {
            Assert.IsNotNull(t);
            var rect = (RectTransform)t;
            Assert.AreEqual(x, rect.pivot.x, 0.001f, t.name + " pivot.x");
            Assert.AreEqual(y, rect.pivot.y, 0.001f, t.name + " pivot.y");
        }

        private static GameObject NewParent() => new GameObject("Parent", typeof(RectTransform));
    }
}
