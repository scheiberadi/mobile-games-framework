using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

namespace EvasLearningWorld.Tests
{
    public class ProjectSettingsTests
    {
        [Test]
        public void OrientationIsLandscapeOnly()
        {
            Assert.AreEqual(UIOrientation.AutoRotation, PlayerSettings.defaultInterfaceOrientation);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToPortrait);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToPortraitUpsideDown);
            Assert.IsTrue(PlayerSettings.allowedAutorotateToLandscapeLeft);
            Assert.IsTrue(PlayerSettings.allowedAutorotateToLandscapeRight);
        }

        [Test]
        public void ProjectUsesTheInputSystemOnly()
        {
            // UiFactory builds an InputSystemUIInputModule, which receives no touch under the old Input Manager (0).
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            Assert.AreEqual(1, settings.FindProperty("activeInputHandler").intValue);
        }

        [Test]
        public void RunInBackgroundIsOff()
        {
            Assert.IsFalse(PlayerSettings.runInBackground);
        }

        [Test]
        public void AndroidIsArm64Il2CppApi26AndInternetIsNotForced()
        {
            Assert.AreEqual(ScriptingImplementation.IL2CPP, PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android));
            Assert.AreEqual(AndroidArchitecture.ARM64, PlayerSettings.Android.targetArchitectures);
            Assert.GreaterOrEqual((int)PlayerSettings.Android.minSdkVersion, (int)AndroidSdkVersions.AndroidApiLevel26);
            Assert.IsFalse(PlayerSettings.Android.forceInternetPermission);
        }
    }
}
