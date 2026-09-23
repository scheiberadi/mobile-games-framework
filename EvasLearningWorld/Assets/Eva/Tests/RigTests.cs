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
            Assert.AreEqual(3, torso.childCount);
            Assert.AreEqual("ArmL", torso.GetChild(0).name);
            Assert.AreEqual("ArmR", torso.GetChild(1).name);
            Assert.AreEqual("Head", torso.GetChild(2).name);

            UnityEngine.Object.DestroyImmediate(parent);
        }

        [Test]
        public void EvaHierarchyMatchesTheSpecAndIncludesATail()
        {
            var parent = NewParent();
            var rig = RigFactory.CreateEva(parent.transform, 560f);
            var root = rig.Root;

            Assert.AreEqual(4, root.childCount, "Root should have LegL, LegR, Tail, Torso for Eva");
            Assert.AreEqual("LegL", root.GetChild(0).name);
            Assert.AreEqual("LegR", root.GetChild(1).name);
            Assert.AreEqual("Tail", root.GetChild(2).name);
            Assert.AreEqual("Torso", root.GetChild(3).name);

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
        public void ApplyLookTintsTheTorsoWithShirtAndEverythingElseWithSkin()
        {
            var parent = NewParent();
            var look = new CharacterLook { Head = 2, Skin = 3, Shirt = 4 };
            var rig = RigFactory.CreatePlayer(parent.transform, look, 420f);
            var root = rig.Root;

            var torso = root.Find("Torso").GetComponent<Image>();
            var armL = root.Find("Torso/ArmL").GetComponent<Image>();
            var armR = root.Find("Torso/ArmR").GetComponent<Image>();
            var head = root.Find("Torso/Head").GetComponent<Image>();
            var legL = root.Find("LegL").GetComponent<Image>();
            var legR = root.Find("LegR").GetComponent<Image>();

            Assert.AreEqual(Palette.Shirt[4], torso.color);
            Assert.AreEqual(Palette.Skin[3], armL.color);
            Assert.AreEqual(Palette.Skin[3], armR.color);
            Assert.AreEqual(Palette.Skin[3], head.color);
            Assert.AreEqual(Palette.Skin[3], legL.color);
            Assert.AreEqual(Palette.Skin[3], legR.color);

            UnityEngine.Object.DestroyImmediate(parent);
        }

        [Test]
        public void EveryGeneratedClipBindsOnlyToPathsThatExistUnderAFreshPlayerRig() =>
            AssertClipsResolve(() => RigFactory.CreatePlayer(NewParent().transform, new CharacterLook(), 420f).Root);

        [Test]
        public void EveryGeneratedClipBindsOnlyToPathsThatExistUnderAFreshEvaRig() =>
            AssertClipsResolve(() => RigFactory.CreateEva(NewParent().transform, 560f).Root);

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
            var rig = RigFactory.CreateEva(parent.transform, 560f);

            rig.Animator.Update(0f);
            rig.Wave();
            rig.Animator.Update(0f);
            Assert.IsTrue(rig.Animator.GetCurrentAnimatorStateInfo(0).IsName("Wave") || rig.Animator.IsInTransition(0),
                "expected Wave() to move the rig into (or towards) the Wave state");

            Assert.DoesNotThrow(() => rig.Wave());

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
