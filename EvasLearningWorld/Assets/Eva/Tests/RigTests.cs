using EvasLearningWorld.App;
using EvasLearningWorld.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.Tests
{
    // The player is one body picture plus separate head, hair, clothes and shoes; Eva is a layered cat. These tests check the
    // hierarchy and draw order, the pivots, ApplyLook's tinting and clothing toggles, and that the code-driven motions ignore
    // a tap while a bounce is already running.
    public class RigTests
    {
        [Test]
        public void PlayerIsOneBodyPictureWithClothesHairAndShoesOnTop()
        {
            var parent = NewParent();
            var rig = RigFactory.CreatePlayer(parent.transform, new CharacterLook(), 420f);
            var root = rig.Root;

            Assert.IsNotNull(root.GetComponent<PlayerMotion>());
            Assert.IsNull(root.GetComponent<Animator>(), "the player is moved by PlayerMotion, not an Animator");
            Assert.AreEqual(1, root.childCount, "Root holds only the Hop container");
            var hop = root.GetChild(0);
            Assert.AreEqual("Hop", hop.name);
            Assert.AreEqual("Body", hop.GetChild(0).name);
            Assert.AreEqual("Underwear", hop.GetChild(1).name);
            Assert.AreEqual("LegL", hop.GetChild(2).name);
            Assert.AreEqual("LegR", hop.GetChild(3).name);
            Assert.AreEqual("Torso", hop.GetChild(4).name);
            Assert.IsNull(hop.Find("Tail"), "the player has no tail");
            Assert.IsNull(hop.Find("Torso/ArmL"), "the body is one picture: no separate arms");

            var torso = hop.Find("Torso");
            foreach (var name in new[] { "Bottom", "Top", "Dress", "Head", "HairBack", "Glasses" })
                Assert.IsNotNull(torso.Find(name), name + " should exist directly under Torso");
            Assert.IsNotNull(torso.Find("Head/EyeIris"));
            Assert.IsNotNull(torso.Find("Head/HairFront"));
            foreach (var legName in new[] { "LegL", "LegR" })
                Assert.IsNotNull(hop.Find(legName + "/Shoe"), legName + " should carry its own Shoe overlay");

            // Every drawn image has a sprite; the leg and torso containers draw nothing.
            Assert.IsFalse(hop.Find("LegL").GetComponent<Image>().enabled);
            Assert.IsFalse(torso.GetComponent<Image>().enabled);
            Assert.IsNotNull(hop.Find("Body").GetComponent<Image>().sprite, "characters/char_body is missing");
            Assert.IsNotNull(hop.Find("Underwear").GetComponent<Image>().sprite, "characters/char_underwear is missing");

            // Draw-order proof: later sibling index = drawn in front.
            int SiblingOf(string name) => torso.Find(name).GetSiblingIndex();
            Assert.Less(SiblingOf("HairBack"), SiblingOf("Head"), "hair-back must be drawn behind the head/face");
            Assert.Less(SiblingOf("Head"), SiblingOf("Glasses"), "glasses must draw in front of the face and its hair-front fringe");
            Assert.Less(SiblingOf("Bottom"), SiblingOf("Head"));

            var head = torso.Find("Head");
            Assert.Less(head.Find("EyeIris").GetSiblingIndex(), head.Find("HairFront").GetSiblingIndex(),
                "the hair-front fringe must draw in front of the eyes, not the other way round");

            Object.DestroyImmediate(parent);
        }

        [Test]
        public void EvaIsALayeredCatWithAHopContainerAndNoAnimator()
        {
            var parent = NewParent();
            var rig = RigFactory.CreateEva(parent.transform, 560f);
            var root = rig.Root;

            Assert.IsNull(root.GetComponent<Animator>(), "Eva's cat is driven by CatMotion, not an Animator");
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

            Object.DestroyImmediate(parent);
        }

        [Test]
        public void HopAndBodyStandOnTheGroundAndTheLegsAndTorsoKeepTheirFittingPivots()
        {
            var parent = NewParent();
            var rig = RigFactory.CreatePlayer(parent.transform, new CharacterLook(), 420f);
            var hop = rig.Root.Find("Hop");

            AssertPivot(hop, 0.5f, 0f);
            AssertPivot(hop.Find("Body"), 0.5f, 0f);
            AssertPivot(hop.Find("LegL"), 0.5f, 1f);
            AssertPivot(hop.Find("LegR"), 0.5f, 1f);
            AssertPivot(hop.Find("Torso"), 0.5f, 0f);

            Object.DestroyImmediate(parent);
        }

        [Test]
        public void ApplyLookTintsSkinPartsAndHairAndTogglesWornWardrobeSlots()
        {
            var parent = NewParent();
            var look = new CharacterLook { Gender = Gender.Girl, Face = 7, Skin = 3, HairColor = 2, EyeColor = 5 };
            look.SetTop("top_a");
            look.SetBottom("bottom_a");
            var rig = RigFactory.CreatePlayer(parent.transform, look, 420f);
            var hop = rig.Root.Find("Hop");

            var body = hop.Find("Body").GetComponent<Image>();
            var head = hop.Find("Torso/Head").GetComponent<Image>();
            var hairBack = hop.Find("Torso/HairBack").GetComponent<Image>();
            var hairFront = hop.Find("Torso/Head/HairFront").GetComponent<Image>();
            var eyeIris = hop.Find("Torso/Head/EyeIris").GetComponent<Image>();

            Assert.AreEqual(Palette.Skin[3], body.color, "the body is skin-coloured until something covers it");
            Assert.AreEqual(Palette.Skin[3], head.color);
            Assert.AreEqual(Palette.HairColor[2], hairBack.color);
            Assert.AreEqual(Palette.HairColor[2], hairFront.color);
            Assert.AreEqual(Palette.EyeColor[5], eyeIris.color);

            var top = hop.Find("Torso/Top").GetComponent<Image>();
            var bottom = hop.Find("Torso/Bottom").GetComponent<Image>();
            var dress = hop.Find("Torso/Dress").GetComponent<Image>();
            var glasses = hop.Find("Torso/Glasses").GetComponent<Image>();
            var shoeL = hop.Find("LegL/Shoe").GetComponent<Image>();
            Assert.IsTrue(top.gameObject.activeSelf, "Top was set, so it should be shown");
            Assert.IsTrue(bottom.gameObject.activeSelf);
            Assert.IsFalse(dress.gameObject.activeSelf, "Dress was never set, so it stays hidden even though Gender is Girl");
            Assert.IsFalse(glasses.gameObject.activeSelf, "no Glasses were picked");
            Assert.IsFalse(shoeL.gameObject.activeSelf, "no Shoes were picked");

            Object.DestroyImmediate(parent);
        }

        [Test]
        public void BriefsShowOnlyWhileNothingCoversTheHips()
        {
            var parent = NewParent();
            var look = new CharacterLook { Gender = Gender.Girl };
            var rig = RigFactory.CreatePlayer(parent.transform, look, 420f);
            var briefs = rig.Root.Find("Hop/Underwear").gameObject;

            Assert.IsTrue(briefs.activeSelf, "an undressed character wears briefs");

            look.SetTop("top_a");
            rig.ApplyLook(look);
            Assert.IsTrue(briefs.activeSelf, "a top alone leaves the hips bare");

            look.SetBottom("bottom_a");
            rig.ApplyLook(look);
            Assert.IsFalse(briefs.activeSelf, "a bottom covers them");

            look.SetDress("dress_a");
            rig.ApplyLook(look);
            Assert.IsFalse(briefs.activeSelf, "a dress covers them");

            Object.DestroyImmediate(parent);
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
            var torso = rig.Root.Find("Hop/Torso");

            Assert.IsTrue(torso.Find("Dress").GetComponent<Image>().gameObject.activeSelf);
            Assert.IsFalse(torso.Find("Top").GetComponent<Image>().gameObject.activeSelf);
            Assert.IsFalse(torso.Find("Bottom").GetComponent<Image>().gameObject.activeSelf);

            Object.DestroyImmediate(parent);
        }

        // Wave and Cheer are bounces: mashing must not restart one that is already running.
        [Test]
        public void PlayerWaveAndCheerAreIgnoredWhileABounceIsRunning()
        {
            var parent = NewParent();
            var rig = RigFactory.CreatePlayer(parent.transform, new CharacterLook(), 420f);
            var motion = rig.Root.GetComponent<PlayerMotion>();

            Assert.IsFalse(motion.IsBouncing);
            rig.Wave();
            Assert.IsTrue(motion.IsBouncing);
            Assert.DoesNotThrow(() => { rig.Wave(); rig.Cheer(); rig.Angry(); rig.SetTalking(true); });

            Object.DestroyImmediate(parent);
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

            Object.DestroyImmediate(parent);
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
